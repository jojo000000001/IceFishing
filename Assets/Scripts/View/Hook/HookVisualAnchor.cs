using UnityEngine;

namespace IceFishing.View
{
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
