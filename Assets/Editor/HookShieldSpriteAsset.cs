using System.IO;
using IceFishing.View;
using UnityEditor;
using UnityEngine;

namespace IceFishing.EditorTools
{
    public static class HookShieldSpriteAsset
    {
        public static void RebuildAll(bool force)
        {
            EnsureFolder();
            WriteSprite(HookShieldSetup.FillSpritePath, HookShieldBubbleArt.CreateFillTexture(), force);
            WriteSprite(HookShieldSetup.GemSpritePath, HookShieldBubbleArt.CreateGemTexture(), force);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("IceFishing/Rebuild Hook Shield Sprites")]
        public static void RebuildFromMenu()
        {
            RebuildAll(true);
            Debug.Log("Rebuilt hook shield bubble sprites (fill / line gem).");
        }

        static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Art/Sprites"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Art"))
                {
                    AssetDatabase.CreateFolder("Assets", "Art");
                }

                AssetDatabase.CreateFolder("Assets/Art", "Sprites");
            }
        }

        static void WriteSprite(string path, Texture2D texture, bool force)
        {
            if (!force && AssetDatabase.LoadAssetAtPath<Sprite>(path) != null)
            {
                Object.DestroyImmediate(texture);
                return;
            }

            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.spritePivot = new Vector2(0.5f, 0.5f);
            importer.SaveAndReimport();
        }
    }
}
