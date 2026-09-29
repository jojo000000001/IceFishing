using UnityEngine;

namespace IceFishing.View
{
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
}
