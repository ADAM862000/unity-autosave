using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DevTools.AutoSave.Core
{
    /// <summary>
    /// Result of a save operation. Immutable value type — zero heap allocation.
    /// </summary>
    internal readonly struct SaveResult
    {
        public readonly bool   Success;
        public readonly int    ScenesSaved;
        public readonly int    AssetsSaved;
        public readonly string ErrorMessage;

        public SaveResult(bool success, int scenesSaved, int assetsSaved, string errorMessage = null)
        {
            Success      = success;
            ScenesSaved  = scenesSaved;
            AssetsSaved  = assetsSaved;
            ErrorMessage = errorMessage;
        }

        public bool AnythingSaved => ScenesSaved > 0 || AssetsSaved > 0;

        public override string ToString() =>
            Success
                ? $"Saved {ScenesSaved} scene(s), {AssetsSaved} asset(s)."
                : $"Save failed: {ErrorMessage}";
    }

    /// <summary>
    /// Abstraction for performing save operations.
    /// Decoupled from trigger logic for isolated unit testing.
    /// </summary>
    internal interface ISaveExecutor
    {
        SaveResult Execute(AutoSaveSettings settings);
    }

    /// <summary>
    /// Production save executor using Unity's standard Editor save APIs.
    /// </summary>
    internal sealed class UnitySaveExecutor : ISaveExecutor
    {
        public SaveResult Execute(AutoSaveSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            // Guard: never save while the editor is in a Play Mode transition.
            // Unity is in an unstable state during this window and SaveScene can
            // produce corrupt data. The SaveOnPlayMode flag is handled upstream by
            // OnBeforePlayMode() which fires before the transition begins — this
            // guard is a safety net for any unexpected call paths.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return new SaveResult(true, 0, 0);

            int scenesSaved = 0;
            int assetsSaved = 0;

            try
            {
                if (settings.SaveScenes)
                    scenesSaved = SaveDirtyScenes();

                if (settings.SaveAssets)
                    assetsSaved = SaveDirtyAssets();

                return new SaveResult(true, scenesSaved, assetsSaved);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AutoSave] Save operation threw an exception: {ex.Message}\n{ex.StackTrace}");
                return new SaveResult(false, scenesSaved, assetsSaved, ex.Message);
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private static int SaveDirtyScenes()
        {
            int count      = 0;
            int sceneCount = EditorSceneManager.sceneCount;

            for (int i = 0; i < sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);

                if (!scene.isLoaded || !scene.isDirty)
                    continue;

                // Skip untitled scenes — SaveScene on a scene without a path shows
                // a blocking OS dialog which is never acceptable during auto save.
                if (string.IsNullOrEmpty(scene.path))
                    continue;

                if (EditorSceneManager.SaveScene(scene))
                    count++;
            }

            return count;
        }

        private static int SaveDirtyAssets()
        {
            // AssetDatabase.SaveAssets() flushes all pending dirty asset writes
            // in a single batched operation. This is the same call Unity makes
            // internally when the user presses Ctrl+S with assets selected.
            AssetDatabase.SaveAssets();

            // Unity provides no stable per-call dirty-count delta API.
            // Return 1 as a signal that the operation was attempted; the logger
            // will only print if AnythingSaved is true (scenes OR assets > 0).
            return 1;
        }
    }
}
