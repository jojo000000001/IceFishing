using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace IceFishing.EditorTools
{
    /// <summary>
    /// 从绿幕冰块表抠出左右边缘碎冰，保存为独立 Sprite。
    /// </summary>
    public static class IceSheetSlicer
    {
        public const string SheetPath = "Assets/Art/Sprites/IceEdgeThinSheet.png";
        public const string OutputFolder = "Assets/Art/Sprites/IceChunks";

        public struct SliceResult
        {
            public Sprite[] Left;
            public Sprite[] Right;
        }

        public static SliceResult EnsureSprites()
        {
            Directory.CreateDirectory(OutputFolder);
            MakeReadable(SheetPath);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SheetPath);
            if (texture == null)
            {
                return new SliceResult { Left = new Sprite[0], Right = new Sprite[0] };
            }

            var blobs = ExtractBlobs(texture);
            var left = new List<Sprite>();
            var right = new List<Sprite>();
            var midX = texture.width * 0.5f;
            var index = 0;
            for (var i = 0; i < blobs.Count; i++)
            {
                var blob = blobs[i];
                var path = OutputFolder + "/Ice_" + index.ToString("00") + ".png";
                File.WriteAllBytes(path, blob.Png);
                AssetDatabase.ImportAsset(path);
                SetSpriteImporter(path);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                {
                    continue;
                }

                if (blob.CenterX < midX)
                {
                    left.Add(sprite);
                }
                else
                {
                    right.Add(sprite);
                }

                index++;
            }

            AssetDatabase.SaveAssets();
            return new SliceResult
            {
                Left = left.ToArray(),
                Right = right.ToArray()
            };
        }

        static void MakeReadable(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
        }

        static void SetSpriteImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
        }

        struct Blob
        {
            public byte[] Png;
            public float CenterX;
            public int Area;
        }

        static List<Blob> ExtractBlobs(Texture2D texture)
        {
            var width = texture.width;
            var height = texture.height;
            var pixels = texture.GetPixels32();
            var seen = new bool[pixels.Length];
            var result = new List<Blob>();
            var minArea = Mathf.Max(400, width * height / 80);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var i = y * width + x;
                    if (seen[i] || IsGreen(pixels[i]))
                    {
                        continue;
                    }

                    var blob = Flood(pixels, seen, width, height, x, y, minArea);
                    if (blob.Png != null)
                    {
                        result.Add(blob);
                    }
                }
            }

            result.Sort((a, b) => b.Area.CompareTo(a.Area));
            if (result.Count > 8)
            {
                result.RemoveRange(8, result.Count - 8);
            }

            return result;
        }

        static Blob Flood(Color32[] pixels, bool[] seen, int width, int height, int startX, int startY, int minArea)
        {
            var queue = new Queue<int>();
            var start = startY * width + startX;
            queue.Enqueue(start);
            seen[start] = true;
            var points = new List<Vector2Int>();
            var minX = startX;
            var maxX = startX;
            var minY = startY;
            var maxY = startY;
            var sumX = 0;

            while (queue.Count > 0)
            {
                var index = queue.Dequeue();
                var x = index % width;
                var y = index / width;
                points.Add(new Vector2Int(x, y));
                sumX += x;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
                TryEnqueue(queue, seen, pixels, width, height, x + 1, y);
                TryEnqueue(queue, seen, pixels, width, height, x - 1, y);
                TryEnqueue(queue, seen, pixels, width, height, x, y + 1);
                TryEnqueue(queue, seen, pixels, width, height, x, y - 1);
            }

            if (points.Count < minArea)
            {
                return default;
            }

            var pad = 4;
            minX = Mathf.Max(0, minX - pad);
            minY = Mathf.Max(0, minY - pad);
            maxX = Mathf.Min(width - 1, maxX + pad);
            maxY = Mathf.Min(height - 1, maxY + pad);
            var w = maxX - minX + 1;
            var h = maxY - minY + 1;
            var crop = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var cropPixels = new Color32[w * h];
            for (var i = 0; i < cropPixels.Length; i++)
            {
                cropPixels[i] = new Color32(0, 0, 0, 0);
            }

            for (var i = 0; i < points.Count; i++)
            {
                var p = points[i];
                var src = pixels[p.y * width + p.x];
                src.a = 255;
                cropPixels[(p.y - minY) * w + (p.x - minX)] = src;
            }

            crop.SetPixels32(cropPixels);
            crop.Apply();
            return new Blob
            {
                Png = crop.EncodeToPNG(),
                CenterX = sumX / (float)points.Count,
                Area = points.Count
            };
        }

        static void TryEnqueue(Queue<int> queue, bool[] seen, Color32[] pixels, int width, int height, int x, int y)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
            {
                return;
            }

            var index = y * width + x;
            if (seen[index] || IsGreen(pixels[index]))
            {
                return;
            }

            seen[index] = true;
            queue.Enqueue(index);
        }

        static bool IsGreen(Color32 c)
        {
            return c.g > 90 && c.g > c.r + 40 && c.g > c.b + 40;
        }
    }
}
