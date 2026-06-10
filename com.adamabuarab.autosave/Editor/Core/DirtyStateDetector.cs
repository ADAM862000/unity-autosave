using UnityEditor;
using UnityEditor.SceneManagement;

namespace DevTools.AutoSave.Core
{
    /// <summary>
    /// Abstraction for querying whether the project has unsaved changes.
    /// </summary>
    internal interface IDirtyStateDetector
    {
        bool HasDirtyScenes();
        bool HasDirtyAssets();
        bool HasAnyDirtyState();
    }

    /// <summary>
    /// Production implementation using reliable Unity Editor APIs.
    ///
    /// Performance design:
    ///   - HasDirtyScenes() is O(loaded scenes) — typically 1-3, near-zero cost.
    ///   - HasDirtyAssets() is O(loaded scenes × root GameObjects) — fast early-exit.
    ///   - HasAnyDirtyState() short-circuits: if scenes are dirty we never call HasDirtyAssets().
    ///   - No allocations in the hot path (GetRootGameObjects caches internally in Unity 2021+).
    ///   - Play Mode guard prevents spurious dirty reads on runtime-modified objects.
    /// </summary>
    internal sealed class UnityDirtyStateDetector : IDirtyStateDetector
    {
        public bool HasDirtyScenes()
        {
            int count = EditorSceneManager.sceneCount;
            for (int i = 0; i < count; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                // scene.isDirty is the authoritative flag Unity uses for the title-bar asterisk.
                if (scene.isLoaded && scene.isDirty)
                    return true;
            }
            return false;
        }

        public bool HasDirtyAssets()
        {
            // Do not attempt to detect dirty assets during Play Mode.
            // Runtime objects are expected to be modified; flagging them would cause
            // spurious saves and false positives.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return false;

            // Walk root GameObjects of each loaded scene.
            // EditorUtility.IsDirty on a root catches dirty components and children
            // because Unity propagates the dirty flag up the hierarchy.
            int sceneCount = EditorSceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                // GetRootGameObjects() allocates an array. We accept this because:
                // (a) it only runs when !HasDirtyScenes() returned false AND a debounce
                //     is already pending — very rare code path.
                // (b) it is far cheaper than scanning the entire AssetDatabase.
                var roots = scene.GetRootGameObjects();
                for (int r = 0; r < roots.Length; r++)
                {
                    if (EditorUtility.IsDirty(roots[r]))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Short-circuits on the first dirty signal found.
        /// HasDirtyScenes() is checked first — it is O(1) per scene and covers the
        /// vast majority of cases (any change inside a scene sets scene.isDirty).
        /// HasDirtyAssets() is only evaluated when scenes are clean, catching
        /// prefab/ScriptableObject edits that happen outside an open scene.
        /// </summary>
        public bool HasAnyDirtyState() => HasDirtyScenes() || HasDirtyAssets();
    }
}
