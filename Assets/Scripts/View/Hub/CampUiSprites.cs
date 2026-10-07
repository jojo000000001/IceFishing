using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IceFishing.View
{
    /// <summary>
    /// 营地结算 UI 图标加载（编辑器 AssetDatabase + Resources 兜底）。
    /// </summary>
    public static class CampUiSprites
    {
        const string FishCoinAssetPath = CampRewardPresentationView.FishCoinSpritePath;
        const string FishCoinResourcePath = "UI/IconFishCoin";
        const string ShellResourcePath = "UI/IconShell";

        static Sprite _fishCoin;
        static Sprite _shell;

        public static Sprite FishCoin
        {
            get
            {
                if (_fishCoin == null)
                {
                    _fishCoin = Load(FishCoinAssetPath, FishCoinResourcePath);
                }

                return _fishCoin;
            }
        }

        public static Sprite Shell
        {
            get
            {
                if (_shell == null)
                {
                    _shell = Load(CampRewardPresentationView.ShellSpritePath, ShellResourcePath);
                }

                return _shell;
            }
        }

        static Sprite Load(string assetPath, string resourcePath)
        {
#if UNITY_EDITOR
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null)
            {
                return sprite;
            }

            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (assets != null)
            {
                for (var i = 0; i < assets.Length; i++)
                {
                    sprite = assets[i] as Sprite;
                    if (sprite != null)
                    {
                        return sprite;
                    }
                }
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture != null)
            {
                return Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }
#endif
            return Resources.Load<Sprite>(resourcePath);
        }
    }
}
