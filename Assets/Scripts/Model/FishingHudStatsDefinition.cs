using UnityEngine;

namespace IceFishing.Model
{
    /// <summary>
    /// 钓鱼 HUD 三条统计的集合与共用胶囊底图。
    /// </summary>
    [CreateAssetMenu(menuName = "IceFishing/Fishing HUD Stats", fileName = "FishingHudStats")]
    public sealed class FishingHudStatsDefinition : ScriptableObject
    {
        public const string DefaultAssetPath = "Assets/Data/UI/FishingHudStats.asset";
        public static readonly Color DefaultPillTint = new Color(0.06f, 0.14f, 0.24f, 0.82f);

        public Sprite SharedPill;
        [Tooltip("胶囊底图着色，与改 SO 前预制体默认一致")]
        public Color PillTint = DefaultPillTint;
        [Tooltip("自上而下排列，与界面上的三条胶囊一致")]
        public FishingHudStatDefinition[] Stats = new FishingHudStatDefinition[3];

        public void ApplyToSession(CastSession session)
        {
            if (session == null || Stats == null)
            {
                return;
            }

            for (var i = 0; i < Stats.Length; i++)
            {
                if (Stats[i] != null)
                {
                    Stats[i].ApplyToSession(session);
                }
            }
        }
    }
}
