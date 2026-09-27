using UnityEngine;

namespace IceFishing.View
{
    /// <summary>
    /// 界面与世界共用的颜色。View 专用，不含玩法数值。
    /// </summary>
    public static class UiTheme
    {
        public static readonly Color Ice = new Color(0.86f, 0.94f, 0.99f);
        public static readonly Color IceShadow = new Color(0.62f, 0.78f, 0.90f);
        public static readonly Color Water = new Color(0.07f, 0.32f, 0.52f);
        public static readonly Color DeepWater = new Color(0.02f, 0.10f, 0.18f);
        public static readonly Color Hole = new Color(0.03f, 0.12f, 0.20f);
        public static readonly Color HoleRim = new Color(0.45f, 0.62f, 0.74f);
        public static readonly Color Hook = new Color(0.92f, 0.78f, 0.28f);

        public static readonly Color Text = Color.white;
        public static readonly Color TextMuted = new Color(0.82f, 0.90f, 0.96f, 0.85f);
        public static readonly Color Panel = new Color(0.07f, 0.12f, 0.18f, 0.72f);
        public static readonly Color StartButton = new Color(0.93f, 0.52f, 0.16f);
        public static readonly Color SecondaryButton = new Color(0.16f, 0.34f, 0.48f);
        public static readonly Color PauseButton = new Color(0.12f, 0.18f, 0.26f, 0.88f);
        public static readonly Color DangerButton = new Color(0.72f, 0.26f, 0.20f);
        public static readonly Color Dimmer = new Color(0f, 0f, 0f, 0.55f);
    }
}
