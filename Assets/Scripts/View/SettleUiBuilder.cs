using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IceFishing.View
{
    /// <summary>
    /// 结算界面：只实例化预制体并绑定引用，不生成按钮或布局。
    /// </summary>
    public static class SettleUiBuilder
    {
        public const string PrefabPath = "Assets/Prefabs/UI/SettleView.prefab";
        public const string PrefabResourcePath = "UI/SettleView";

        /// <summary>
        /// 仅绑定预制体层级上的引用，不改动 RectTransform / LayoutGroup。
        /// </summary>
        public static SettleView BindFromHierarchy(Transform root)
        {
            if (root == null)
            {
                return null;
            }

            var layout = root.Find("SettleLayout") as RectTransform;
            var depth = layout != null ? layout.Find("Depth")?.GetComponent<Text>() : null;
            var grid = layout != null ? layout.Find("FishScroll/Viewport/Grid") as RectTransform : null;
            var empty = layout != null ? layout.Find("FishScroll/Empty")?.GetComponent<Text>() : null;
            var totalCoinText = layout != null ? layout.Find("Totals/TotalCoin/Value")?.GetComponent<Text>() : null;
            var totalShellText = layout != null ? layout.Find("Totals/TotalShell/Value")?.GetComponent<Text>() : null;
            var exit = layout != null ? layout.Find("ExitButton")?.GetComponent<Button>() : null;
            var cont = layout != null ? layout.Find("ContinueButton")?.GetComponent<Button>() : null;
            var speciesDropdown = layout != null ? layout.Find("SpeciesDropdown")?.GetComponent<Dropdown>() : null;
            var fishScroll = layout != null ? layout.Find("FishScroll")?.GetComponent<ScrollRect>() : null;

            var settle = root.GetComponent<SettleView>();
            if (settle == null)
            {
                settle = root.gameObject.AddComponent<SettleView>();
            }

            settle.Configure(
                depth,
                grid,
                empty,
                totalCoinText,
                totalShellText,
                exit,
                cont,
                layout,
                speciesDropdown,
                fishScroll);
            return settle;
        }

        public static SettleView Build(Transform canvas)
        {
            if (canvas == null)
            {
                return null;
            }

            var existing = canvas.Find(SettleView.NodeName);
            if (existing != null)
            {
                return BindFromHierarchy(existing);
            }

            var prefab = LoadPrefab();
            if (prefab == null)
            {
                Debug.LogError("SettleView prefab missing. Expected " + PrefabPath);
                return null;
            }

            var instance = Object.Instantiate(prefab, canvas, false);
            instance.name = SettleView.NodeName;
            var rt = instance.GetComponent<RectTransform>();
            if (rt != null)
            {
                UiFactory.Stretch(rt);
            }

            return BindFromHierarchy(instance.transform);
        }

        public static GameObject LoadPrefab()
        {
#if UNITY_EDITOR
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (asset != null)
            {
                return asset;
            }
#endif
            return Resources.Load<GameObject>(PrefabResourcePath);
        }
    }
}
