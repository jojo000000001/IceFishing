using IceFishing.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace IceFishing.EditorTools
{
    /// <summary>
    /// 打开营地 / 钓鱼 HUD 预制体时，给 Prefab 舞台环境 Canvas 套上和场景 UICanvas 同一套缩放。
    /// 不把 Canvas 写进预制体，避免场景里出现嵌套 Canvas。
    /// </summary>
    [InitializeOnLoad]
    static class IceFishingHudPrefabStage
    {
        static IceFishingHudPrefabStage()
        {
            PrefabStage.prefabStageOpened += stage =>
            {
                EditorApplication.delayCall += () => Apply(stage);
            };
        }

        static void Apply(PrefabStage stage)
        {
            if (stage == null || !stage.scene.IsValid())
            {
                return;
            }

            if (stage.assetPath != IceFishingSceneBuilder.HudPrefabPath
                && stage.assetPath != IceFishingSceneBuilder.HubPrefabPath)
            {
                return;
            }

            var root = stage.prefabContentsRoot;
            if (root == null)
            {
                return;
            }

            var canvas = FindEnvironmentCanvas(stage, root);
            if (canvas == null || canvas.gameObject == root)
            {
                return;
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }

            UiFactory.ApplyHudCanvasScaler(scaler);
            var hud = root.GetComponent<FishingHudView>();
            if (hud != null)
            {
                hud.RefreshLayout();
            }

            var hub = root.GetComponent<HubView>();
            if (hub != null)
            {
                hub.RefreshLayout();
            }
        }

        static Canvas FindEnvironmentCanvas(PrefabStage stage, GameObject root)
        {
            var canvas = root.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                return canvas;
            }

            foreach (var go in stage.scene.GetRootGameObjects())
            {
                canvas = go.GetComponentInChildren<Canvas>(true);
                if (canvas != null)
                {
                    return canvas;
                }
            }

            return null;
        }
    }
}
