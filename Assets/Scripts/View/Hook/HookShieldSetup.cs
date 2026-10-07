using UnityEngine;

namespace IceFishing.View
{
    public static class HookShieldSetup
    {
        public const string FillSpritePath = "Assets/Art/Sprites/HookShieldBubbleFill.png";
        public const string GemSpritePath = "Assets/Art/Sprites/HookShieldLineGem.png";

        /// <summary>相对钩身最大边长的直径倍率，参考图需完整包住钩身。</summary>
        public const float VisualPadding = 1.28f;
        public const float GemLocalScale = 0.2f;

        public static readonly Color FillColor = Color.white;
        public static readonly Color GemColor = Color.white;

        public const int FillSortingOrder = 18;
        public const int GemSortingOrder = 22;

        public const string RootName = "HookShield";
        public const string FillChildName = "BubbleFill";
        public const string GemChildName = "LineGemGlow";
        const string LegacyRimChildName = "BubbleRim";

        public static float ComputeShieldDiameterLocal(Transform hook)
        {
            if (hook == null)
            {
                return HookVisualScale.DefaultWorldHeight * VisualPadding;
            }

            var renderer = hook.GetComponent<SpriteRenderer>();
            var sprite = renderer != null ? renderer.sprite : null;
            if (sprite == null)
            {
                return HookVisualScale.DefaultWorldHeight * VisualPadding;
            }

            var size = sprite.bounds.size;
            return Mathf.Max(size.x, size.y) * VisualPadding;
        }

        public static Transform EnsureChild(Transform hook)
        {
            if (hook == null)
            {
                return null;
            }

            var root = hook.Find(RootName);
            if (root == null)
            {
                var go = new GameObject(RootName);
                root = go.transform;
                root.SetParent(hook, false);
                root.localRotation = Quaternion.identity;
                go.AddComponent<HookShieldVisual>();
                go.SetActive(false);
            }
            else if (root.GetComponent<HookShieldVisual>() == null)
            {
                root.gameObject.AddComponent<HookShieldVisual>();
            }

            MigrateLegacySingleRenderer(root);
            RemoveLegacyRimLayer(root);
            EnsureLayer(root, FillChildName, FillSpritePath, FillColor, FillSortingOrder, 1f);
            EnsureLayer(root, GemChildName, GemSpritePath, GemColor, GemSortingOrder, GemLocalScale);
            ApplyLayout(hook, root);
            return root;
        }

        static void RemoveLegacyRimLayer(Transform root)
        {
            if (root == null)
            {
                return;
            }

            var rim = root.Find(LegacyRimChildName);
            if (rim == null)
            {
                return;
            }

#if UNITY_EDITOR
            Object.DestroyImmediate(rim.gameObject);
#else
            Object.Destroy(rim.gameObject);
#endif
        }

        static void MigrateLegacySingleRenderer(Transform root)
        {
            var legacy = root.GetComponent<SpriteRenderer>();
            if (legacy == null)
            {
                return;
            }

#if UNITY_EDITOR
            Object.DestroyImmediate(legacy);
#else
            Object.Destroy(legacy);
#endif
        }

        static void EnsureLayer(
            Transform root,
            string childName,
            string spritePath,
            Color color,
            int sortingOrder,
            float localScale)
        {
            var child = root.Find(childName);
            if (child == null)
            {
                var go = new GameObject(childName);
                child = go.transform;
                child.SetParent(root, false);
                child.localRotation = Quaternion.identity;
                go.AddComponent<SpriteRenderer>();
            }

            child.localPosition = Vector3.zero;
            child.localScale = new Vector3(localScale, localScale, 1f);
            var renderer = child.GetComponent<SpriteRenderer>();
            RefreshLayerRenderer(renderer, spritePath, color, sortingOrder);
        }

        public static void RefreshLayerRenderer(SpriteRenderer renderer, string spritePath, Color color, int sortingOrder)
        {
            if (renderer == null)
            {
                return;
            }

            var sprite = LoadSprite(spritePath);
            if (sprite != null)
            {
                renderer.sprite = sprite;
            }

            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
        }

        public static void ApplyLayout(Transform hook, Transform shieldRoot)
        {
            if (hook == null || shieldRoot == null)
            {
                return;
            }

            var hookRenderer = hook.GetComponent<SpriteRenderer>();
            var hookSprite = hookRenderer != null ? hookRenderer.sprite : null;
            shieldRoot.localPosition = HookVisualAnchor.GetBodyCenterLocal(hookSprite);
            var diameter = ComputeShieldDiameterLocal(hook);
            shieldRoot.localScale = new Vector3(diameter, diameter, 1f);

            var fill = shieldRoot.Find(FillChildName);
            var gem = shieldRoot.Find(GemChildName);
            RefreshLayerRenderer(fill != null ? fill.GetComponent<SpriteRenderer>() : null, FillSpritePath, FillColor, FillSortingOrder);
            RefreshLayerRenderer(gem != null ? gem.GetComponent<SpriteRenderer>() : null, GemSpritePath, GemColor, GemSortingOrder);

            if (gem != null && hookSprite != null)
            {
                var attach = HookVisualAnchor.GetLineAttachLocal(hookSprite);
                var center = HookVisualAnchor.GetBodyCenterLocal(hookSprite);
                var offset = attach - center;
                gem.localPosition = new Vector3(
                    offset.x / diameter,
                    offset.y / diameter,
                    0f);
            }
        }

        static Sprite LoadSprite(string path)
        {
#if UNITY_EDITOR
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (asset != null)
            {
                return asset;
            }
#endif
            return HookShieldRuntimeSprites.Get(path);
        }
    }
}
