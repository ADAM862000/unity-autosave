using DevTools.AutoSave.Core;
using UnityEditor;

namespace DevTools.AutoSave.UI
{
    /// <summary>
    /// Menu items under Tools → Auto Save.
    /// </summary>
    internal static class AutoSaveMenuItems
    {
        private const string MenuRoot         = "Tools/Auto Save/";
        private const string MenuOpenSettings = MenuRoot + "Open Settings";
        private const string MenuToggle       = MenuRoot + "Enable Auto Save";
        private const string MenuSaveNow      = MenuRoot + "Save Now";

        [MenuItem(MenuOpenSettings, priority = 1)]
        private static void OpenSettings()
        {
            SettingsService.OpenProjectSettings("Project/Auto Save");
        }

        [MenuItem(MenuToggle, priority = 2)]
        private static void ToggleAutoSave()
        {
            var settings = AutoSaveEditorHooks.CurrentSettings;
            if (settings == null) return;

            // Mutate a clone so we don't modify the live settings object directly
            // before the repository and controller have both been updated.
            var updated = settings.Clone();
            updated.Enabled = !updated.Enabled;

            AutoSaveSettingsRepository.Save(updated);
            AutoSaveEditorHooks.NotifySettingsChanged(updated);
        }

        [MenuItem(MenuToggle, validate = true)]
        private static bool ToggleAutoSaveValidate()
        {
            var settings = AutoSaveEditorHooks.CurrentSettings;
            Menu.SetChecked(MenuToggle, settings != null && settings.Enabled);
            return true;
        }

        [MenuItem(MenuSaveNow, priority = 3)]
        private static void SaveNow()
        {
            // Use Unity's built-in Ctrl+S command for scenes,
            // then SaveAssets for everything else.
            EditorApplication.ExecuteMenuItem("File/Save");
            AssetDatabase.SaveAssets();
        }
    }
}
