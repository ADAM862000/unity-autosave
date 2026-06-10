using UnityEditor;
using UnityEngine;

namespace DevTools.AutoSave
{
    /// <summary>
    /// Handles loading and saving of <see cref="AutoSaveSettings"/> to persistent storage.
    /// Uses <see cref="EditorPrefs"/> with JSON serialisation so settings survive
    /// Unity reinstallation and are shared across the project (stored per-machine).
    ///
    /// For team-shared defaults, settings can optionally be committed as a JSON asset
    /// under ProjectSettings/ — but that workflow is opt-in only.
    /// </summary>
    internal static class AutoSaveSettingsRepository
    {
        private const string PrefsKey = "DevTools.AutoSave.Settings";

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Loads settings from EditorPrefs, or returns defaults if none exist.</summary>
        internal static AutoSaveSettings Load()
        {
            var settings = new AutoSaveSettings();

            if (!EditorPrefs.HasKey(PrefsKey))
                return settings;

            try
            {
                var json = EditorPrefs.GetString(PrefsKey, string.Empty);
                if (!string.IsNullOrWhiteSpace(json))
                    JsonUtility.FromJsonOverwrite(json, settings);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[AutoSave] Failed to load settings. Resetting to defaults. ({ex.Message})");
                settings.ResetToDefaults();
            }

            return settings;
        }

        /// <summary>Persists settings to EditorPrefs.</summary>
        internal static void Save(AutoSaveSettings settings)
        {
            if (settings == null)
            {
                Debug.LogWarning("[AutoSave] Attempted to save null settings — skipped.");
                return;
            }

            try
            {
                var json = JsonUtility.ToJson(settings, prettyPrint: false);
                EditorPrefs.SetString(PrefsKey, json);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AutoSave] Failed to persist settings. ({ex.Message})");
            }
        }

        /// <summary>Removes all persisted settings, restoring factory defaults on next load.</summary>
        internal static void Clear()
        {
            EditorPrefs.DeleteKey(PrefsKey);
        }
    }
}
