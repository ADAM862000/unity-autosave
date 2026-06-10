using UnityEditor;
using UnityEditor.Compilation;

namespace DevTools.AutoSave.Core
{
    /// <summary>
    /// Bridges Unity Editor static lifecycle events to <see cref="AutoSaveController"/>.
    ///
    /// This class is intentionally thin: it owns the controller instance, wires
    /// Unity callbacks, and exposes a minimal API for the UI layer. All save
    /// logic lives in the controller.
    ///
    /// [InitializeOnLoad] causes the static constructor to run on every domain
    /// reload (startup, script recompile, entering/exiting Play Mode).
    /// </summary>
    [InitializeOnLoad]
    internal static class AutoSaveEditorHooks
    {
        private static AutoSaveController _controller;
        private static AutoSaveSettings   _currentSettings;

        // ── Domain reload entry point ─────────────────────────────────────────────

        static AutoSaveEditorHooks()
        {
            _currentSettings = AutoSaveSettingsRepository.Load();
            _controller      = new AutoSaveController(_currentSettings);

            EditorApplication.update               += OnEditorUpdate;
            EditorApplication.focusChanged         += OnFocusChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            CompilationPipeline.compilationStarted += OnCompilationStarted;

            // Dispose the controller cleanly before domain reload tears everything down.
            // Without this, the EditorApplication.update delegate would be orphaned
            // and could throw on the next tick.
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        // ── Internal API (used by UI layer) ───────────────────────────────────────

        /// <summary>
        /// Pushes updated settings into the live controller immediately.
        /// No domain reload required — changes take effect on the next editor tick.
        /// </summary>
        internal static void NotifySettingsChanged(AutoSaveSettings newSettings)
        {
            _currentSettings = newSettings;
            _controller?.ApplySettings(newSettings);
        }

        /// <summary>Current active settings. Never null after static construction.</summary>
        internal static AutoSaveSettings CurrentSettings => _currentSettings;

        // ── Unity callbacks ───────────────────────────────────────────────────────

        private static void OnEditorUpdate()
        {
            _controller?.OnEditorUpdate();
        }

        private static void OnFocusChanged(bool hasFocus)
        {
            if (!hasFocus)
                _controller?.OnFocusLost();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // ExitingEditMode fires before domain reload; the safest moment to save.
            if (state == PlayModeStateChange.ExitingEditMode)
                _controller?.OnBeforePlayMode();
        }

        private static void OnCompilationStarted(object context)
        {
            _controller?.OnBeforeCompile();
        }

        private static void OnBeforeAssemblyReload()
        {
            // Unsubscribe explicitly to prevent delegate leaks across reloads.
            EditorApplication.update               -= OnEditorUpdate;
            EditorApplication.focusChanged         -= OnFocusChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            CompilationPipeline.compilationStarted -= OnCompilationStarted;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;

            _controller?.Dispose();
            _controller = null;
        }
    }
}
