using UnityEngine;

namespace IceFishing.Model
{
    /// <summary>
    /// 钓鱼 HUD 左侧一条统计胶囊：图标、布局与 CastSession 字段绑定。
    /// </summary>
    [CreateAssetMenu(menuName = "IceFishing/Fishing HUD Stat", fileName = "FishingHudStat")]
    public sealed class FishingHudStatDefinition : ScriptableObject
    {
        public FishingHudStatKind Kind = FishingHudStatKind.Protection;
        public Sprite Icon;
        [Tooltip("留空则使用 FishingHudStatsDefinition 里的共用胶囊底图")]
        public Sprite PillOverride;
        [Tooltip("相对 HudLayout 左上角的 Y（负值向下）")]
        public float TopOffsetY = -24f;

        [Header("数值文案")]
        [Tooltip("编辑器预览、营地 HUD 占位用的当前值（分子）")]
        public float PreviewCurrent;
        [Tooltip("上限（分母）。>0 时开局写入 CastSession，HUD 仍读 Session 实时刷新")]
        public float MaxValue;

        [SerializeField, HideInInspector]
        string PreviewValue = "0/0";

        public string GetPreviewText()
        {
            MigrateLegacyPreview();
            var max = GetPreviewMax();
            var current = Mathf.RoundToInt(PreviewCurrent);
            return current + "/" + Mathf.RoundToInt(max);
        }

        public string Format(CastSession session)
        {
            if (session == null)
            {
                return GetPreviewText();
            }

            switch (Kind)
            {
                case FishingHudStatKind.Protection:
                    return session.ProtectionLeft + "/" + session.ProtectionMax;
                case FishingHudStatKind.Haul:
                    return session.CaughtCount + "/" + session.Capacity;
                case FishingHudStatKind.Depth:
                    return Mathf.RoundToInt(session.Depth) + "/" + Mathf.RoundToInt(session.MaxDepth);
                default:
                    return GetPreviewText();
            }
        }

        /// <summary>
        /// 将 SO 里配置的上限写入本局 Session（仅 MaxValue &gt; 0 的项）。
        /// </summary>
        public void ApplyToSession(CastSession session)
        {
            if (session == null || MaxValue <= 0f)
            {
                return;
            }

            switch (Kind)
            {
                case FishingHudStatKind.Protection:
                    var protection = Mathf.Max(0, Mathf.RoundToInt(MaxValue));
                    session.ProtectionMax = protection;
                    session.ProtectionLeft = protection;
                    break;
                case FishingHudStatKind.Haul:
                    session.Capacity = Mathf.Max(1, Mathf.RoundToInt(MaxValue));
                    break;
                case FishingHudStatKind.Depth:
                    session.MaxDepth = Mathf.Max(1f, MaxValue);
                    session.Depth = Mathf.Min(session.Depth, session.MaxDepth);
                    break;
            }
        }

        float GetPreviewMax()
        {
            if (MaxValue > 0f)
            {
                return MaxValue;
            }

            switch (Kind)
            {
                case FishingHudStatKind.Protection:
                    return 5f;
                case FishingHudStatKind.Haul:
                    return 2f;
                case FishingHudStatKind.Depth:
                    return 150f;
                default:
                    return 1f;
            }
        }

        void MigrateLegacyPreview()
        {
            if (MaxValue > 0f || string.IsNullOrEmpty(PreviewValue))
            {
                return;
            }

            var slash = PreviewValue.IndexOf('/');
            if (slash <= 0 || slash >= PreviewValue.Length - 1)
            {
                return;
            }

            if (float.TryParse(PreviewValue.Substring(0, slash), out var current))
            {
                PreviewCurrent = current;
            }

            if (float.TryParse(PreviewValue.Substring(slash + 1), out var max))
            {
                MaxValue = max;
            }
        }
    }
}
