using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IceFishing.View
{
    /// <summary>
    /// 结算页切图加载（编辑器 AssetDatabase + Resources 兜底）。
    /// </summary>
    public static class SettleUiSprites
    {
        public const string CardPath = "Assets/Art/UI/SettleCard.png";
        public const string StarPath = "Assets/Art/UI/SettleStar.png";
        public const string CountBadgePath = "Assets/Art/UI/SettleCountBadge.png";
        public const string TotalFramePath = "Assets/Art/UI/SettleTotalFrame.png";
        public const string ButtonNormalPath = "Assets/Art/UI/SettleButtonNormal.png";
        public const string ButtonPressedPath = "Assets/Art/UI/SettleButtonPressed.png";

        static Sprite _card;
        static Sprite _star;
        static Sprite _countBadge;
        static Sprite _totalFrame;
        static Sprite _buttonNormal;
        static Sprite _buttonPressed;

        public static Sprite Card { get { return Load(ref _card, CardPath, "UI/SettleCard"); } }
        public static Sprite Star { get { return Load(ref _star, StarPath, "UI/SettleStar"); } }
        public static Sprite CountBadge { get { return Load(ref _countBadge, CountBadgePath, "UI/SettleCountBadge"); } }
        public static Sprite TotalFrame { get { return Load(ref _totalFrame, TotalFramePath, "UI/SettleTotalFrame"); } }
        public static Sprite ButtonNormal { get { return Load(ref _buttonNormal, ButtonNormalPath, "UI/SettleButtonNormal"); } }
        public static Sprite ButtonPressed { get { return Load(ref _buttonPressed, ButtonPressedPath, "UI/SettleButtonPressed"); } }

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
