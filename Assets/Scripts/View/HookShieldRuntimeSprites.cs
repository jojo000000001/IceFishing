using System.Collections.Generic;
using UnityEngine;

namespace IceFishing.View
{
    /// <summary>
    /// 打包后无 AssetDatabase 时用程序纹理兜底。
    /// </summary>
    public static class HookShieldRuntimeSprites
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            Sprite cached;
            if (Cache.TryGetValue(path, out cached) && cached != null)
            {
                return cached;
            }

            Texture2D texture;
            if (path == HookShieldSetup.GemSpritePath)
            {
                texture = HookShieldBubbleArt.CreateGemTexture();
            }
            else
            {
                texture = HookShieldBubbleArt.CreateFillTexture();
            }

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                HookShieldBubbleArt.TextureSize);
            Cache[path] = sprite;
            return sprite;
        }
    }
}
