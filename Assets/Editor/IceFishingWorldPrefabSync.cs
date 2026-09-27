using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IceFishing.EditorTools
{
    /// <summary>
    /// 保存 IceFishingWorld 预制体后，把打开着的 IceFishing 场景里的 World 实例重新对齐到预制体（去掉场景覆盖）。
    /// </summary>
    public static class IceFishingWorldPrefabSync
    {
        public const string WorldPrefabPath = IceFishingWorldPrefab.PrefabPath;
        public const string IceFishingScenePath = "Assets/Scenes/IceFishing.unity";

        static bool _pendingSceneSync;

        static IceFishingWorldPrefabSync()
        {
            EditorApplication.delayCall += FlushPendingSceneSync;
            PrefabStage.prefabStageClosing += OnPrefabStageClosing;
        }

        static void OnPrefabStageClosing(PrefabStage stage)
        {
            if (stage == null || stage.assetPath != WorldPrefabPath)
            {
                return;
            }

            IceFishingWorldPrefab.NormalizeWorldPrefabContents(stage.prefabContentsRoot);
        }

        public static void OnWorldPrefabImported()
        {
            if (!ShouldAutoSyncActiveScene())
            {
                return;
            }

            _pendingSceneSync = true;
            EditorApplication.delayCall += FlushPendingSceneSync;
        }

        static void FlushPendingSceneSync()
        {
            if (!_pendingSceneSync)
            {
                return;
            }

            _pendingSceneSync = false;
            if (!ShouldAutoSyncActiveScene())
            {
                return;
            }

            // 场景不再放置 World 实例；Play 时由 AppController 从预制体生成。
        }

        static bool ShouldAutoSyncActiveScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return false;
            }

            var scene = EditorSceneManager.GetActiveScene();
            return scene.IsValid() && scene.path == IceFishingScenePath;
        }
    }

    public class IceFishingWorldPrefabAssetPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            for (var i = 0; i < importedAssets.Length; i++)
            {
                if (importedAssets[i] == IceFishingWorldPrefabSync.WorldPrefabPath)
                {
                    IceFishingWorldPrefabSync.OnWorldPrefabImported();
                    return;
                }
            }
        }
    }
}
