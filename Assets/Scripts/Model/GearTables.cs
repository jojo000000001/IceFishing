using UnityEngine;

namespace IceFishing.Model
{
    /// <summary>
        /// 渔具数值表。等级 1～10 线性插值。只读查询，不保存玩家进度。
    /// </summary>
    public static class GearTables
    {
        public static float LineMaxDepth(int level)
        {
            return Mathf.Lerp(150f, 550f, T(level));
        }

        public static int HookCapacity(int level)
        {
            return Mathf.RoundToInt(Mathf.Lerp(5f, 20f, T(level)));
        }

        public static float SinkerStartDepth(int level)
        {
            return Mathf.Lerp(0f, 165f, T(level));
        }

        public static int TokensForStars(int stars)
        {
            switch (Mathf.Clamp(stars, 1, 5))
            {
                case 1:
                    return 10;
                case 2:
                    return 20;
                case 3:
                    return 40;
                case 4:
                    return 70;
                default:
                    return 150;
            }
        }

        static float T(int level)
        {
            return Mathf.Clamp01((Mathf.Clamp(level, 1, 10) - 1) / 9f);
        }
    }
}
