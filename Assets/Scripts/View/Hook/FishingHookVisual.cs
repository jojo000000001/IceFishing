using UnityEngine;

namespace IceFishing.View
{
    /// <summary>
    /// 挂在 Hook 预制体上：编辑模式也按与运行时相同的世界高度缩放，避免 Scene 里过小。
    /// </summary>
    [ExecuteAlways]
    public sealed class FishingHookVisual : MonoBehaviour
    {
        [SerializeField] float _worldHeight = HookVisualScale.DefaultWorldHeight;
        [SerializeField] SpriteRenderer _sprite;

        void Reset()
        {
            _sprite = GetComponent<SpriteRenderer>();
        }

        void OnValidate()
        {
            Apply();
        }

        void OnEnable()
        {
            Apply();
        }

        public void Apply()
        {
            if (_sprite == null)
            {
                _sprite = GetComponent<SpriteRenderer>();
            }

            if (_sprite == null)
            {
                return;
            }

            HookVisualScale.Apply(transform, _sprite.sprite, _worldHeight);
            LayoutShield();
        }

        void LayoutShield()
        {
            var shield = transform.Find("HookShield");
            if (shield != null)
            {
                HookShieldSetup.ApplyLayout(transform, shield);
            }
        }
    }

    /// <summary>
    /// 钩子精灵统一世界高度，编辑器和运行时共用同一套缩放。
    /// </summary>
    public static class HookVisualScale
    {
        public const float DefaultWorldHeight = 1f;

        public static float ComputeUniformScale(Sprite sprite, float worldHeight = DefaultWorldHeight)
        {
            if (sprite == null || worldHeight <= 0f)
            {
                return 0.78f;
            }

            var size = sprite.bounds.size;
            if (size.y < 0.001f)
            {
                return 0.78f;
            }

            return worldHeight / size.y;
        }

        public static void Apply(Transform hook, Sprite sprite, float worldHeight = DefaultWorldHeight)
        {
            if (hook == null)
            {
                return;
            }

            var scale = ComputeUniformScale(sprite, worldHeight);
            var parentX = 1f;
            var parentY = 1f;
            if (hook.parent != null)
            {
                parentX = Mathf.Abs(hook.parent.lossyScale.x);
                parentY = Mathf.Abs(hook.parent.lossyScale.y);
            }

            if (parentX < 0.0001f)
            {
                parentX = 1f;
            }

            if (parentY < 0.0001f)
            {
                parentY = 1f;
            }

            hook.localScale = new Vector3(scale / parentX, scale / parentY, 1f);
        }
    }

    /// <summary>
    /// 锚点精灵 pivot 在中心时：Transform 对齐钩身中心，绳线挂环在 bounds 顶部。
    /// </summary>
    public static class HookVisualAnchor
    {
        public static Vector3 GetBodyCenterLocal(Sprite sprite)
        {
            return sprite != null ? sprite.bounds.center : Vector3.zero;
        }

        public static Vector3 GetLineAttachLocal(Sprite sprite)
        {
            if (sprite == null)
            {
                return Vector3.zero;
            }

            var bounds = sprite.bounds;
            return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
        }
    }
}
