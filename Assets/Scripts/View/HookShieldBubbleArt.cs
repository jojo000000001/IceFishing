using System;
using UnityEngine;

namespace IceFishing.View
{
    /// <summary>
    /// 保护罩气泡与线挂点光晕的程序纹理。
    /// </summary>
    public static class HookShieldBubbleArt
    {
        public const int TextureSize = 128;

        public static Texture2D CreateFillTexture()
        {
            return CreateRadial((d, rim) =>
            {
                if (d > 1f)
                {
                    return Color.clear;
                }

                var fill = (1f - d) * 0.11f;
                var edge = Mathf.SmoothStep(0.68f, 0.9f, d) * (1f - Mathf.SmoothStep(0.9f, 1f, d));
                var alpha = fill + edge * 0.35f;
                var rgb = Color.Lerp(new Color(0.38f, 0.72f, 0.98f), new Color(0.55f, 0.9f, 1f), edge);
                rgb.a = alpha;
                return rgb;
            });
        }

        public static Texture2D CreateGemTexture()
        {
            return CreateRadial((d, rim) =>
            {
                if (d > 1f)
                {
                    return Color.clear;
                }

                var core = 1f - Mathf.SmoothStep(0f, 0.45f, d);
                var glow = (1f - d) * 0.35f;
                var alpha = Mathf.Clamp01(core * 0.95f + glow);
                var rgb = Color.Lerp(new Color(0.35f, 0.95f, 0.45f), new Color(0.65f, 1f, 0.55f), core);
                rgb.a = alpha;
                return rgb;
            });
        }

        static Texture2D CreateRadial(System.Func<float, float, Color> paint)
        {
            var size = TextureSize;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            var radius = size * 0.5f - 1f;
            var center = new Vector2(size * 0.5f, size * 0.5f);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    var d = distance / radius;
                    texture.SetPixel(x, y, paint(d, d));
                }
            }

            texture.Apply();
            return texture;
        }
    }
}
