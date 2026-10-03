using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IceFishing.View
{
    /// <summary>
    /// 暂停弹窗与营地开始钓鱼按钮切图。
    /// </summary>
    public static class PauseUiSprites
    {
        public const string PanelPath = "Assets/Art/UI/PausePanel.png";
        public const string IconPath = "Assets/Art/UI/PauseIcon.png";
        public const string ResumePath = "Assets/Art/UI/PauseResume.png";
        public const string ResumePressedPath = "Assets/Art/UI/PauseResumePressed.png";
        public const string RetreatPath = "Assets/Art/UI/PauseRetreat.png";
        public const string RetreatPressedPath = "Assets/Art/UI/PauseRetreatPressed.png";
        public const string HubStartPath = "Assets/Art/UI/HubStartButton.png";
        public const string HubStartPressedPath = "Assets/Art/UI/HubStartButtonPressed.png";

        static Sprite _panel;
        static Sprite _icon;
        static Sprite _resume;
        static Sprite _resumePressed;
        static Sprite _retreat;
        static Sprite _retreatPressed;
        static Sprite _hubStart;
        static Sprite _hubStartPressed;

        public static Sprite Panel { get { return Load(ref _panel, PanelPath, "UI/PausePanel"); } }
        public static Sprite Icon { get { return Load(ref _icon, IconPath, "UI/PauseIcon"); } }
        public static Sprite Resume { get { return Load(ref _resume, ResumePath, "UI/PauseResume"); } }
        public static Sprite ResumePressed { get { return Load(ref _resumePressed, ResumePressedPath, "UI/PauseResumePressed"); } }
        public static Sprite Retreat { get { return Load(ref _retreat, RetreatPath, "UI/PauseRetreat"); } }
        public static Sprite RetreatPressed { get { return Load(ref _retreatPressed, RetreatPressedPath, "UI/PauseRetreatPressed"); } }
        public static Sprite HubStart { get { return Load(ref _hubStart, HubStartPath, "UI/HubStartButton"); } }
        public static Sprite HubStartPressed { get { return Load(ref _hubStartPressed, HubStartPressedPath, "UI/HubStartButtonPressed"); } }

        static Sprite Load(ref Sprite cache, string assetPath, string resourcePath)
        {
            if (cache != null)
            {
                return cache;
            }
#if UNITY_EDITOR
            cache = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (cache != null)
            {
                return cache;
            }

            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (assets != null)
            {
                for (var i = 0; i < assets.Length; i++)
                {
                    cache = assets[i] as Sprite;
                    if (cache != null)
                    {
                        return cache;
                    }
                }
            }
#endif
            cache = Resources.Load<Sprite>(resourcePath);
            return cache;
        }
    }
}
