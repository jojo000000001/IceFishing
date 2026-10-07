using UnityEngine;

namespace IceFishing.View
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class HookShieldVisual : MonoBehaviour
    {
        public void Apply()
        {
            ApplyFull();
        }

        public void ApplyFull()
        {
            var hook = transform.parent;
            if (hook == null)
            {
                return;
            }

            HookShieldSetup.ApplyLayout(hook, transform);
        }

        void OnEnable()
        {
            var hook = transform.parent;
            if (hook != null && transform.Find(HookShieldSetup.FillChildName) != null)
            {
                HookShieldSetup.ApplyLayout(hook, transform);
            }
        }

        void OnValidate()
        {
            var hook = transform.parent;
            if (hook != null && transform.Find(HookShieldSetup.FillChildName) != null)
            {
                HookShieldSetup.ApplyLayout(hook, transform);
            }
        }
    }

    public static class HookShieldSetup
    {
        /// <summary>相对钩身最大边长的直径倍率，参考图需完整包住钩身。</summary>
        public const float VisualPadding = 1.28f;

        public const string RootName = "HookShield";
        public const string FillChildName = "BubbleFill";
        public const string GemChildName = "LineGemGlow";

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

            var gem = shieldRoot.Find(GemChildName);
            if (gem == null || hookSprite == null || diameter < 0.0001f)
            {
                return;
            }

            var attach = HookVisualAnchor.GetLineAttachLocal(hookSprite);
            var center = HookVisualAnchor.GetBodyCenterLocal(hookSprite);
            var offset = attach - center;
            gem.localPosition = new Vector3(offset.x / diameter, offset.y / diameter, 0f);
        }
    }
}
