using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IceFishing.View
{
    /// <summary>
    /// 首次开局教程卡切图。
    /// </summary>
    public static class TutorialUiSprites
    {
        public const string FramePath = "Assets/Art/UI/TutorialFrame.png";
        public const string HeaderPath = "Assets/Art/UI/TutorialHeader.png";
        public const string ClosePath = "Assets/Art/UI/TutorialClose.png";
        public const string BubblesPath = "Assets/Art/UI/TutorialBubbles.png";

        static Sprite _frame;
        static Sprite _header;
        static Sprite _close;
        static Sprite _bubbles;

        public static Sprite Frame { get { return Load(ref _frame, FramePath, "UI/TutorialFrame"); } }
        public static Sprite Header { get { return Load(ref _header, HeaderPath, "UI/TutorialHeader"); } }
        public static Sprite Close { get { return Load(ref _close, ClosePath, "UI/TutorialClose"); } }
        public static Sprite Bubbles { get { return Load(ref _bubbles, BubblesPath, "UI/TutorialBubbles"); } }

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
