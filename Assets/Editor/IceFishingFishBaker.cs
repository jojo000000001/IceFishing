using IceFishing.Model;
using IceFishing.View;
using UnityEditor;
using UnityEngine;

namespace IceFishing.EditorTools
{
    /// <summary>
    /// 先按贴图烘焙 16 个鱼预制体（头/身碰撞），再写对应 SO。
    /// 菜单：IceFishing / Rebuild Fish Prefabs And Definitions。
    /// </summary>
    public static class IceFishingFishBaker
    {
        public const string SpriteFolder = "Assets/Art/Sprites/Fish";
        public const string PrefabFolder = "Assets/Prefabs/World/Fish";
        public const string DataFolder = "Assets/Data/Fish";

        struct Spec
        {
            public string File;
            public string Id;
            public string Name;
            public float MinDepth;
            public float MaxDepth;
            public int Stars;
            public int Weight;
            public float Speed;
            public float Height;
        }

        static readonly Spec[] Catalog =
        {
            SpecOf("Fish_01_IceShrimp", "ice_shrimp", "冰虾", 0f, 30f, 1, 100, 1.05f, 0.38f),
            SpecOf("Fish_02_SnowMinnow", "snow_minnow", "雪鲦", 0f, 35f, 1, 100, 1.2f, 0.32f),
            SpecOf("Fish_03_RockPerch", "rock_perch", "石鲈", 0f, 40f, 1, 90, 1f, 0.48f),
            SpecOf("Fish_04_IceCrucian", "ice_crucian", "冰鲫", 5f, 45f, 1, 90, 0.95f, 0.52f),
            SpecOf("Fish_05_BlueCod", "blue_cod", "蓝鳕", 10f, 55f, 2, 80, 1.0f, 0.85f),
            SpecOf("Fish_06_FrostSculpin", "frost_sculpin", "霜鲉", 15f, 60f, 2, 70, 0.75f, 0.9f),
            SpecOf("Fish_07_SilverHerring", "silver_herring", "银鲱", 15f, 55f, 2, 80, 1.35f, 0.4f),
            SpecOf("Fish_08_SpeckledSalmon", "speckled_salmon", "斑鲑", 20f, 70f, 2, 70, 1.05f, 0.85f),
            SpecOf("Fish_09_WinterTrout", "winter_trout", "冬鳟", 30f, 80f, 3, 50, 1.1f, 1.0f),
            SpecOf("Fish_10_IceBream", "ice_bream", "冰鲷", 35f, 85f, 3, 50, 0.9f, 1.05f),
            SpecOf("Fish_11_AuroraFish", "aurora_fish", "极光鱼", 45f, 95f, 3, 40, 1.15f, 0.95f),
            SpecOf("Fish_12_ArmorBass", "armor_bass", "铠鲈", 40f, 90f, 3, 40, 0.8f, 1.15f),
            SpecOf("Fish_13_WolfEel", "wolf_eel", "狼鳗", 50f, 100f, 4, 25, 1.0f, 0.75f),
            SpecOf("Fish_14_KingSalmon", "king_salmon", "皇鲑", 55f, 100f, 4, 20, 1.1f, 1.3f),
            SpecOf("Fish_15_LanternFish", "lantern_fish", "灯笼鱼", 65f, 100f, 5, 12, 0.95f, 1.0f),
            SpecOf("Fish_16_AbyssFish", "abyss_fish", "冰渊鱼", 75f, 100f, 5, 8, 0.85f, 1.25f)
        };

        static Spec SpecOf(
            string file,
            string id,
            string name,
            float minDepth,
            float maxDepth,
            int stars,
            int weight,
            float speed,
            float height)
        {
            return new Spec
            {
                File = file,
                Id = id,
                Name = name,
                MinDepth = minDepth,
                MaxDepth = maxDepth,
                Stars = stars,
                Weight = weight,
                Speed = speed,
                Height = height
            };
        }

        [MenuItem("IceFishing/Rebuild Fish Prefabs And Definitions")]
        public static void RebuildFromMenu()
        {
            Rebuild();
        }

        public static void Rebuild()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/World");
            EnsureFolder(PrefabFolder);
            EnsureFolder("Assets/Data");
            EnsureFolder(DataFolder);
            ImportSprites();

            for (var i = 0; i < Catalog.Length; i++)
            {
                var spec = Catalog[i];
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + "/" + spec.File + ".png");
                if (sprite == null)
                {
                    Debug.LogWarning("IceFishing fish sprite missing: " + spec.File);
                    continue;
                }

                var prefabPath = PrefabFolder + "/" + spec.File + ".prefab";
                var go = BuildPrefab(spec, sprite);
                var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                Object.DestroyImmediate(go);

                var view = prefab != null ? prefab.GetComponent<FishView>() : null;
                WriteDefinition(spec, view);
            }

            WriteCatalog();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("IceFishing: baked " + Catalog.Length + " fish prefabs and definitions.");
        }

        static GameObject BuildPrefab(Spec spec, Sprite sprite)
        {
            var go = new GameObject(spec.File);
            var view = go.AddComponent<FishView>();
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Discrete;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 2;
            renderer.color = Color.white;

            var head = new GameObject("Head");
            head.transform.SetParent(go.transform, false);
            var headCol = head.AddComponent<BoxCollider2D>();
            var headHit = head.AddComponent<FishHitVolume>();
            headHit.EditorAssign(view, FishPart.Head);

            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(go.transform, false);
            var bodyCol = bodyGo.AddComponent<BoxCollider2D>();
            var bodyHit = bodyGo.AddComponent<FishHitVolume>();
            bodyHit.EditorAssign(view, FishPart.Body);

            view.EditorAssign(renderer, headCol, bodyCol);
            view.SetWorldHeight(spec.Height);
            view.SetSwim(1, spec.Speed);
            return go;
        }

        static void WriteDefinition(Spec spec, FishView prefab)
        {
            var path = DataFolder + "/" + spec.File + ".asset";
            var so = AssetDatabase.LoadAssetAtPath<FishDefinition>(path);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<FishDefinition>();
                AssetDatabase.CreateAsset(so, path);
            }

            so.Id = spec.Id;
            so.DisplayName = spec.Name;
            so.MinDepthMeters = spec.MinDepth;
            so.MaxDepthMeters = spec.MaxDepth;
            so.Stars = spec.Stars;
            so.SpawnWeight = spec.Weight;
            so.MoveSpeed = spec.Speed;
            so.WorldHeight = spec.Height;
            so.CampCoinReward = 10;
            so.CampShellReward = 10;
            so.Prefab = prefab;
            EditorUtility.SetDirty(so);
        }

        static void WriteCatalog()
        {
            var path = FishCatalog.AssetPath;
            var catalog = AssetDatabase.LoadAssetAtPath<FishCatalog>(path);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<FishCatalog>();
                AssetDatabase.CreateAsset(catalog, path);
            }

            var items = new FishDefinition[Catalog.Length];
            for (var i = 0; i < Catalog.Length; i++)
            {
                items[i] = AssetDatabase.LoadAssetAtPath<FishDefinition>(DataFolder + "/" + Catalog[i].File + ".asset");
            }

            catalog.Items = items;
            EditorUtility.SetDirty(catalog);
        }

        static void ImportSprites()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SpriteFolder });
            for (var i = 0; i < (guids != null ? guids.Length : 0); i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                var dirty = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    dirty = true;
                }

                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    dirty = true;
                }

                if (importer.spritePixelsPerUnit != 100f)
                {
                    importer.spritePixelsPerUnit = 100f;
                    dirty = true;
                }

                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    dirty = true;
                }

                if (importer.alphaIsTransparency == false)
                {
                    importer.alphaIsTransparency = true;
                    dirty = true;
                }

                if (dirty)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            var name = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
