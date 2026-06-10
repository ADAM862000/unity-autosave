using System;
using UnityEngine;

namespace DevTools.AutoSave
{
    /// <summary>
    /// Defines how and when the auto save system triggers a save operation.
    /// </summary>
    public enum SaveMode
    {
        /// <summary>Save after a debounced delay following the first detected change.</summary>
        AfterDelay = 0,

        /// <summary>Save only when the Unity Editor window loses OS focus.</summary>
        OnFocusLost = 1,

        /// <summary>Save both after a delay and when focus is lost.</summary>
        AfterDelayAndOnFocusLost = 2
    }

    /// <summary>
    /// Controls how much feedback is shown in the Unity Console.
    /// </summary>
    public enum NotificationVerbosity
    {
        /// <summary>No console messages.</summary>
        Silent = 0,

        /// <summary>Show a brief message only when a save actually occurs.</summary>
        Minimal = 1,

        /// <summary>Show scene/asset counts and the trigger that caused the save.</summary>
        Verbose = 2
    }

    /// <summary>
    /// Persistent settings for the Auto Save system.
    /// Serialised to/from JSON via <see cref="AutoSaveSettingsRepository"/>.
    ///
    /// This is a plain serialisable class — not a ScriptableObject — so no
    /// Unity asset file is created and source control stays clean.
    /// </summary>
    [Serializable]
    public sealed class AutoSaveSettings
    {
        // ── Defaults ──────────────────────────────────────────────────────────────

        public const bool                DefaultEnabled           = true;
        public const SaveMode            DefaultSaveMode          = SaveMode.AfterDelayAndOnFocusLost;
        public const float               DefaultDelaySeconds      = 3f;
        public const bool                DefaultSaveOnPlayMode    = true;
        public const bool                DefaultSaveOnCompile     = true;
        public const bool                DefaultSaveScenes        = true;
        public const bool                DefaultSaveAssets        = true;
        public const bool                DefaultShowStatusBarIcon = true;
        public const NotificationVerbosity DefaultVerbosity       = NotificationVerbosity.Minimal;

        // ── Serialised backing fields ─────────────────────────────────────────────

        [SerializeField] private bool               _enabled           = DefaultEnabled;
        [SerializeField] private SaveMode           _saveMode          = DefaultSaveMode;
        [SerializeField] private float              _delaySeconds      = DefaultDelaySeconds;
        [SerializeField] private bool               _saveOnPlayMode    = DefaultSaveOnPlayMode;
        [SerializeField] private bool               _saveOnCompile     = DefaultSaveOnCompile;
        [SerializeField] private bool               _saveScenes        = DefaultSaveScenes;
        [SerializeField] private bool               _saveAssets        = DefaultSaveAssets;
        [SerializeField] private bool               _showStatusBarIcon = DefaultShowStatusBarIcon;
        [SerializeField] private NotificationVerbosity _verbosity      = DefaultVerbosity;

        // ── Properties ────────────────────────────────────────────────────────────

        /// <summary>Master switch. When false no automatic saves are performed.</summary>
        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        /// <summary>Strategy that determines when saves are triggered.</summary>
        public SaveMode SaveMode
        {
            get => _saveMode;
            set => _saveMode = value;
        }

        /// <summary>
        /// Seconds to wait after the first detected change before saving.
        /// Clamped to [0.5, 300].
        /// </summary>
        public float DelaySeconds
        {
            get => _delaySeconds;
            set => _delaySeconds = Mathf.Clamp(value, 0.5f, 300f);
        }

        /// <summary>Save all dirty assets before entering Play Mode.</summary>
        public bool SaveOnPlayMode
        {
            get => _saveOnPlayMode;
            set => _saveOnPlayMode = value;
        }

        /// <summary>Save all dirty assets before script compilation begins.</summary>
        public bool SaveOnCompile
        {
            get => _saveOnCompile;
            set => _saveOnCompile = value;
        }

        /// <summary>Include open scenes in auto save operations.</summary>
        public bool SaveScenes
        {
            get => _saveScenes;
            set => _saveScenes = value;
        }

        /// <summary>Include dirty project assets (prefabs, ScriptableObjects, etc.).</summary>
        public bool SaveAssets
        {
            get => _saveAssets;
            set => _saveAssets = value;
        }

        /// <summary>Show the auto save status indicator in the Scene View toolbar.</summary>
        public bool ShowStatusBarIcon
        {
            get => _showStatusBarIcon;
            set => _showStatusBarIcon = value;
        }

        /// <summary>How much information is printed to the Console.</summary>
        public NotificationVerbosity Verbosity
        {
            get => _verbosity;
            set => _verbosity = value;
        }

        // ── Computed ──────────────────────────────────────────────────────────────

        /// <summary>True when the after-delay save strategy is active.</summary>
        public bool IsDelayModeActive =>
            _saveMode == SaveMode.AfterDelay ||
            _saveMode == SaveMode.AfterDelayAndOnFocusLost;

        /// <summary>True when the focus-lost save strategy is active.</summary>
        public bool IsFocusLostModeActive =>
            _saveMode == SaveMode.OnFocusLost ||
            _saveMode == SaveMode.AfterDelayAndOnFocusLost;

        // ── Utility ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates an independent deep copy of this settings object.
        /// Use when you need to mutate settings without affecting the live instance.
        /// </summary>
        public AutoSaveSettings Clone() => new AutoSaveSettings
        {
            _enabled           = _enabled,
            _saveMode          = _saveMode,
            _delaySeconds      = _delaySeconds,
            _saveOnPlayMode    = _saveOnPlayMode,
            _saveOnCompile     = _saveOnCompile,
            _saveScenes        = _saveScenes,
            _saveAssets        = _saveAssets,
            _showStatusBarIcon = _showStatusBarIcon,
            _verbosity         = _verbosity
        };

        /// <summary>Resets all fields to factory defaults in place.</summary>
        public void ResetToDefaults()
        {
            _enabled           = DefaultEnabled;
            _saveMode          = DefaultSaveMode;
            _delaySeconds      = DefaultDelaySeconds;
            _saveOnPlayMode    = DefaultSaveOnPlayMode;
            _saveOnCompile     = DefaultSaveOnCompile;
            _saveScenes        = DefaultSaveScenes;
            _saveAssets        = DefaultSaveAssets;
            _showStatusBarIcon = DefaultShowStatusBarIcon;
            _verbosity         = DefaultVerbosity;
        }
    }
}
