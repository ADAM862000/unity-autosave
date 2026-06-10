using System;

namespace DevTools.AutoSave.Core
{
    /// <summary>
    /// Central controller that wires together settings, dirty-state detection,
    /// debouncing, and save execution.
    ///
    /// Timing model:
    ///   The debounce timer is armed ONCE on the clean→dirty edge, then runs
    ///   uninterrupted. It fires N seconds after the first detected change.
    ///   After a save (or cancellation) the edge detector resets so the next
    ///   edit cycle arms the timer fresh.
    ///
    /// Thread safety:
    ///   All methods are called from the Unity main thread via EditorApplication.update
    ///   and synchronous editor callbacks. No locking is required.
    /// </summary>
    internal sealed class AutoSaveController : IDisposable
    {
        // ── Dependencies ──────────────────────────────────────────────────────────

        private readonly IDirtyStateDetector _dirtyDetector;
        private readonly ISaveExecutor       _saveExecutor;
        private readonly Debouncer           _debouncer;

        // ── State ─────────────────────────────────────────────────────────────────

        private AutoSaveSettings _settings;
        private bool             _disposed;
        private bool             _wasCleanLastFrame = true;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised after every save attempt, regardless of outcome.</summary>
        internal event Action<SaveResult> SaveCompleted;

        // ── Construction ──────────────────────────────────────────────────────────

        internal AutoSaveController(
            AutoSaveSettings    settings,
            IDirtyStateDetector dirtyDetector = null,
            ISaveExecutor       saveExecutor  = null,
            Func<double>        timeProvider  = null)
        {
            _settings      = settings ?? throw new ArgumentNullException(nameof(settings));
            _dirtyDetector = dirtyDetector ?? new UnityDirtyStateDetector();
            _saveExecutor  = saveExecutor  ?? new UnitySaveExecutor();
            _debouncer     = new Debouncer(settings.DelaySeconds, timeProvider);
        }

        // ── Settings hot-reload ───────────────────────────────────────────────────

        /// <summary>
        /// Applies new settings without requiring a domain reload.
        /// Safe to call from any editor callback; takes effect immediately.
        /// </summary>
        internal void ApplySettings(AutoSaveSettings newSettings)
        {
            _settings = newSettings ?? throw new ArgumentNullException(nameof(newSettings));
            _debouncer.DelaySeconds = newSettings.DelaySeconds;

            // Cancel any queued save if the user just disabled the system.
            if (!newSettings.Enabled)
            {
                _debouncer.Cancel();
                _wasCleanLastFrame = true;
            }
        }

        // ── Tick (editor update) ──────────────────────────────────────────────────

        /// <summary>
        /// Called every editor update frame (~100 Hz).
        /// Hot path — kept minimal: two bool checks + one dirty query + one Tick().
        /// </summary>
        internal void OnEditorUpdate()
        {
            if (_disposed || !_settings.Enabled || !_settings.IsDelayModeActive)
                return;

            bool isDirtyNow = _dirtyDetector.HasAnyDirtyState();

            // Arm the debounce ONLY on the clean→dirty transition.
            // Arm() is a no-op if already pending, so repeated dirty frames do not
            // reset the clock — the save fires N seconds after the FIRST change.
            if (isDirtyNow && _wasCleanLastFrame)
                _debouncer.Arm();

            _wasCleanLastFrame = !isDirtyNow;

            _debouncer.Tick(ExecuteSave);
        }

        // ── Focus loss ────────────────────────────────────────────────────────────

        /// <summary>
        /// Triggered synchronously when the Unity Editor loses OS focus.
        /// Saves immediately, bypassing the debounce delay.
        /// </summary>
        internal void OnFocusLost()
        {
            if (_disposed || !_settings.Enabled || !_settings.IsFocusLostModeActive)
                return;

            if (!_dirtyDetector.HasAnyDirtyState())
                return;

            _debouncer.Cancel();
            ExecuteSave();
        }

        // ── Play Mode ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Triggered before the editor transitions from Edit Mode to Play Mode.
        /// </summary>
        internal void OnBeforePlayMode()
        {
            if (_disposed || !_settings.Enabled || !_settings.SaveOnPlayMode)
                return;

            if (!_dirtyDetector.HasAnyDirtyState())
                return;

            _debouncer.Cancel();
            ExecuteSave();
        }

        // ── Compilation ───────────────────────────────────────────────────────────

        /// <summary>
        /// Triggered before script compilation begins (before domain reload).
        /// </summary>
        internal void OnBeforeCompile()
        {
            if (_disposed || !_settings.Enabled || !_settings.SaveOnCompile)
                return;

            if (!_dirtyDetector.HasAnyDirtyState())
                return;

            _debouncer.Cancel();
            ExecuteSave();
        }

        // ── Core save ─────────────────────────────────────────────────────────────

        private void ExecuteSave()
        {
            AutoSaveLogger.LogVerbose("Executing auto save\u2026", _settings.Verbosity);

            var result = _saveExecutor.Execute(_settings);

            AutoSaveLogger.LogSaveResult(in result, _settings.Verbosity);

            if (!result.Success)
                AutoSaveLogger.LogWarning($"Auto save encountered an error: {result.ErrorMessage}");

            // Reset edge state so the next change cycle arms the timer fresh.
            _wasCleanLastFrame = true;
            _debouncer.Cancel();

            SaveCompleted?.Invoke(result);
        }

        // ── IDisposable ───────────────────────────────────────────────────────────

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _debouncer.Cancel();
        }
    }
}
