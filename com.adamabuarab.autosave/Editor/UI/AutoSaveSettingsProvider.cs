using DevTools.AutoSave.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DevTools.AutoSave.UI
{
    /// <summary>
    /// Registers a "Auto Save" panel inside Unity's Project Settings window
    /// (Edit → Project Settings → Auto Save).
    ///
    /// Uses IMGUI for broadest Unity version compatibility (2021.3 LTS+).
    /// </summary>
    internal sealed class AutoSaveSettingsProvider : SettingsProvider
    {
        private const string SettingsPath = "Project/Auto Save";

        private AutoSaveSettings _settings;
        private bool             _isDirty;

        // ── Registration ──────────────────────────────────────────────────────────

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new AutoSaveSettingsProvider(SettingsPath, SettingsScope.Project);
        }

        private AutoSaveSettingsProvider(string path, SettingsScope scopes)
            : base(path, scopes)
        {
            keywords = new System.Collections.Generic.HashSet<string>
            {
                "auto", "save", "autosave", "delay", "focus", "compile",
                "play", "mode", "notification", "verbosity", "smart"
            };
        }

        // ── SettingsProvider overrides ────────────────────────────────────────────

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            // Load a working copy so we can detect changes before committing.
            _settings = AutoSaveEditorHooks.CurrentSettings?.Clone()
                        ?? AutoSaveSettingsRepository.Load();
            _isDirty = false;
        }

        public override void OnDeactivate()
        {
            // Auto-apply pending changes when the user navigates away.
            if (_isDirty)
                CommitSettings();
        }

        public override void OnGUI(string searchContext)
        {
            DrawSettingsGUI();
        }

        // ── IMGUI Drawing ─────────────────────────────────────────────────────────

        private void DrawSettingsGUI()
        {
            EditorGUILayout.Space(6);

            // Header
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 13,
                alignment = TextAnchor.MiddleLeft
            };
            EditorGUILayout.LabelField("Auto Save", headerStyle);
            EditorGUILayout.Space(2);

            var subtitleStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true
            };
            EditorGUILayout.LabelField(
                "Saves scenes and assets based on configurable triggers — " +
                "only when something has changed.", subtitleStyle);

            EditorGUILayout.Space(12);
            DrawHorizontalRule();
            EditorGUILayout.Space(8);

            // Master toggle
            EditorGUI.BeginChangeCheck();
            bool newEnabled = EditorGUILayout.Toggle(
                new GUIContent("Enable Auto Save",
                    "Master switch. When disabled, no automatic saves are performed."),
                _settings.Enabled);
            if (EditorGUI.EndChangeCheck())
            {
                _settings.Enabled = newEnabled;
                MarkDirty();
            }

            EditorGUILayout.Space(8);

            // Grey out the rest if disabled
            using (new EditorGUI.DisabledGroupScope(!_settings.Enabled))
            {
                // ── Save Mode ──────────────────────────────────────────────────
                DrawSection("Save Mode");

                EditorGUI.BeginChangeCheck();
                var newMode = (SaveMode)EditorGUILayout.EnumPopup(
                    new GUIContent("Trigger Mode",
                        "Determines when auto saves are triggered.\n\n" +
                        "After Delay: saves after the configured idle delay.\n" +
                        "On Focus Lost: saves when the Unity window loses OS focus.\n" +
                        "After Delay + On Focus Lost: both strategies combined."),
                    _settings.SaveMode);
                if (EditorGUI.EndChangeCheck())
                {
                    _settings.SaveMode = newMode;
                    MarkDirty();
                }

                if (_settings.SaveMode == SaveMode.AfterDelay ||
                    _settings.SaveMode == SaveMode.AfterDelayAndOnFocusLost)
                {
                    EditorGUI.BeginChangeCheck();
                    float newDelay = EditorGUILayout.Slider(
                        new GUIContent("Delay (seconds)",
                            "How long to wait after the last detected change before saving. " +
                            "Lower values save more frequently; higher values are less intrusive."),
                        _settings.DelaySeconds, 0.5f, 300f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        _settings.DelaySeconds = newDelay;
                        MarkDirty();
                    }
                }

                EditorGUILayout.Space(10);

                // ── Trigger Events ─────────────────────────────────────────────
                DrawSection("Trigger Events");

                DrawToggle(
                    ref _settings,
                    s => s.SaveOnPlayMode,
                    (s, v) => s.SaveOnPlayMode = v,
                    "Save Before Play Mode",
                    "Saves all dirty assets and scenes before the editor enters Play Mode.");

                DrawToggle(
                    ref _settings,
                    s => s.SaveOnCompile,
                    (s, v) => s.SaveOnCompile = v,
                    "Save Before Compile",
                    "Saves all dirty assets and scenes before a script compilation starts.");

                EditorGUILayout.Space(10);

                // ── Scope ──────────────────────────────────────────────────────
                DrawSection("Save Scope");

                DrawToggle(
                    ref _settings,
                    s => s.SaveScenes,
                    (s, v) => s.SaveScenes = v,
                    "Save Scenes",
                    "Include open, modified scenes in auto save operations.\n" +
                    "Untitled scenes (not yet saved to disk) are skipped to avoid interrupting your workflow.");

                DrawToggle(
                    ref _settings,
                    s => s.SaveAssets,
                    (s, v) => s.SaveAssets = v,
                    "Save Assets",
                    "Include dirty project assets (prefabs, ScriptableObjects, materials, etc.) " +
                    "in auto save operations.");

                EditorGUILayout.Space(10);

                // ── Notifications ──────────────────────────────────────────────
                DrawSection("Notifications");

                EditorGUI.BeginChangeCheck();
                var newVerbosity = (NotificationVerbosity)EditorGUILayout.EnumPopup(
                    new GUIContent("Console Output",
                        "Controls how much feedback is shown in the Unity Console.\n\n" +
                        "Silent: no messages.\n" +
                        "Minimal: a brief 'Auto saved.' message when a save occurs.\n" +
                        "Verbose: detailed output including which assets were saved."),
                    _settings.Verbosity);
                if (EditorGUI.EndChangeCheck())
                {
                    _settings.Verbosity = newVerbosity;
                    MarkDirty();
                }

                DrawToggle(
                    ref _settings,
                    s => s.ShowStatusBarIcon,
                    (s, v) => s.ShowStatusBarIcon = v,
                    "Show Toolbar Status",
                    "Display a small auto save indicator in the editor toolbar.");

                EditorGUILayout.Space(14);
            }

            // ── Apply / Reset buttons ──────────────────────────────────────────
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledGroupScope(!_isDirty))
                {
                    if (GUILayout.Button("Apply", GUILayout.Width(80)))
                        CommitSettings();
                }

                if (GUILayout.Button("Reset Defaults", GUILayout.Width(110)))
                    ResetToDefaults();

                GUILayout.Space(4);
            }

            EditorGUILayout.Space(6);

            // Status line
            if (_isDirty)
            {
                EditorGUILayout.HelpBox(
                    "You have unsaved setting changes. Click Apply or navigate away to commit.",
                    MessageType.Info);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void DrawSection(string title)
        {
            var style = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };
            EditorGUILayout.LabelField(title, style);
            EditorGUILayout.Space(2);
        }

        private static void DrawHorizontalRule()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        }

        private void DrawToggle(
            ref AutoSaveSettings settings,
            System.Func<AutoSaveSettings, bool> getter,
            System.Action<AutoSaveSettings, bool> setter,
            string label, string tooltip)
        {
            EditorGUI.BeginChangeCheck();
            bool newVal = EditorGUILayout.Toggle(new GUIContent(label, tooltip), getter(settings));
            if (EditorGUI.EndChangeCheck())
            {
                setter(settings, newVal);
                MarkDirty();
            }
        }

        private void MarkDirty() => _isDirty = true;

        private void CommitSettings()
        {
            AutoSaveSettingsRepository.Save(_settings);
            AutoSaveEditorHooks.NotifySettingsChanged(_settings.Clone());
            _isDirty = false;
        }

        private void ResetToDefaults()
        {
            if (!EditorUtility.DisplayDialog(
                "Reset Settings",
                "Reset all Auto Save settings to defaults?",
                "Reset", "Cancel"))
                return;

            _settings.ResetToDefaults();
            CommitSettings();
            Repaint();
        }
    }
}
