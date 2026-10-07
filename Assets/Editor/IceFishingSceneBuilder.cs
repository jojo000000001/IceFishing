using IceFishing.Controller;
using IceFishing.Model;
using IceFishing.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IceFishing.EditorTools
{
    /// <summary>
    /// 把 M1 单场景、世界占位和基础 UGUI 烘焙进 IceFishing.unity。
    /// 菜单：IceFishing / Rebuild M1 Scene。
    /// </summary>
    public static class IceFishingSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/IceFishing.unity";
        public const string HudPrefabPath = "Assets/Prefabs/UI/FishingHudView.prefab";
        public const string HubPrefabPath = "Assets/Prefabs/UI/HubView.prefab";
        public const string SettlePrefabPath = "Assets/Prefabs/UI/SettleView.prefab";
        public const string TutorialPrefabPath = "Assets/Prefabs/UI/CastTutorialView.prefab";
        public const string UnderwaterPrefabPath = "Assets/Prefabs/World/UnderwaterField.prefab";
        public const string CampPrefabPath = "Assets/Prefabs/World/CampField.prefab";
        public const string HudArtFolder = "Assets/Art/UI";

        [MenuItem("IceFishing/Rebuild M1 Scene")]
        public static void BuildFromMenu()
        {
            Build();
        }

        [MenuItem("IceFishing/Rebuild Hub Prefab")]
        public static void RebuildHubPrefabFromMenu()
        {
            BakeHubPrefab();
            ReplaceHubInOpenScene();
        }

        [MenuItem("IceFishing/Rebuild Settle Prefab")]
        public static void RebuildSettlePrefabFromMenu()
        {
            BakeSettlePrefab();
            ReplaceSettleInOpenScene();
        }

        [MenuItem("IceFishing/Rebuild Tutorial Card Prefab")]
        public static void RebuildTutorialPrefabFromMenu()
        {
            BakeTutorialPrefab();
        }

        [MenuItem("IceFishing/Rebuild Camp Prefab")]
        public static void RebuildCampPrefabFromMenu()
        {
            BakeCampPrefab();
            ReplaceCampInOpenScene();
        }

        [MenuItem("IceFishing/Rebuild Underwater Prefab")]
        public static void RebuildUnderwaterPrefabFromMenu()
        {
            BakeUnderwaterPrefab();
            ReplaceUnderwaterInOpenScene();
        }

        [MenuItem("IceFishing/Rebuild Fishing HUD Prefab")]
        public static void RebuildHudPrefabFromMenu()
        {
            ImportHudSprites();
            BakeHudPrefab();
        }

        public static string Build()
        {
            ApplyPlayerSettings();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.38f, 0.66f);
            camera.transform.position = new Vector3(0f, 9.6f, -10f);
            cameraObject.AddComponent<AudioListener>();

            WorldView world = null;
            var worldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(IceFishingWorldPrefab.PrefabPath);
            if (worldPrefab == null)
            {
                var worldObject = new GameObject("World");
                world = worldObject.AddComponent<WorldView>();
                var hubBackground = AssetDatabase.LoadAssetAtPath<Sprite>(HubUiBuilder.CampMapPath);
                var hookSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/FishingAnchor.png");
                world.Build(camera, hubBackground, hookSprite);
                var camp = PlaceCampPrefab(world.transform, hubBackground);
                world.EditorAssignCamp(camp);
                var underwater = PlaceUnderwaterPrefab(world.transform);
                world.EditorAssignUnderwater(underwater);
                var fishGo = new GameObject("FishField");
                fishGo.transform.SetParent(world.transform, false);
                var fishField = fishGo.AddComponent<FishField>();
                var catalog = AssetDatabase.LoadAssetAtPath<FishCatalog>(FishCatalog.AssetPath);
                world.EditorAssignFish(fishField, catalog);
                var hookPrefab = IceFishingHookPrefab.LoadOrBakePrefab();
                if (hookPrefab != null)
                {
                    var hook = IceFishingHookPrefab.PlaceUnderWorld(world.transform, hookPrefab);
                    var serializedWorld = new SerializedObject(world);
                    serializedWorld.FindProperty("_hook").objectReferenceValue = hook;
                    serializedWorld.FindProperty("_line").objectReferenceValue =
                        hook != null ? hook.GetComponent<LineRenderer>() : null;
                    serializedWorld.FindProperty("_hookPrefab").objectReferenceValue = hookPrefab;
                    serializedWorld.ApplyModifiedPropertiesWithoutUndo();
                }

                IceFishingWorldPrefab.BakeFromOpenScene();
            }

            var eventObject = new GameObject("EventSystem");
            eventObject.AddComponent<EventSystem>();
            eventObject.AddComponent<StandaloneInputModule>();

            var canvasObject = new GameObject("UICanvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            canvas.pixelPerfect = false;
            canvas.sortingOrder = 10;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            UiFactory.ApplyHudCanvasScaler(scaler);
            canvasObject.AddComponent<GraphicRaycaster>();

            var hub = PlaceHubPrefab(canvasObject.transform);
            var hud = PlaceHudPrefab(canvasObject.transform);
            var ui = UiFactory.Build(canvasObject.transform, hub, hud);
            var tutorial = PlaceCastTutorialPrefab(canvasObject.transform);

            var bootstrap = new GameObject("Bootstrap");
            var music = new GameObject(MusicController.NodeName).AddComponent<MusicController>();
            music.EditorAssign(
                AssetDatabase.LoadAssetAtPath<AudioClip>(MusicController.CampBgmPath),
                AssetDatabase.LoadAssetAtPath<AudioClip>(MusicController.UnderwaterBgmPath),
                AssetDatabase.LoadAssetAtPath<AudioClip>(MusicController.ProtectionHitSfxPath));
            var app = bootstrap.AddComponent<AppController>();
            app.EditorAssign(ui.Hub, ui.Hud, ui.Pause, ui.Overlay, ui.Settle, world, worldPrefab, tutorial);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            AssetDatabase.SaveAssets();
            return ScenePath;
        }

        static CastTutorialView PlaceCastTutorialPrefab(Transform canvas)
        {
            var existing = canvas.Find(CastTutorialView.NodeName);
            if (existing != null)
            {
                return existing.GetComponent<CastTutorialView>();
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TutorialPrefabPath);
            if (prefab == null)
            {
                prefab = BakeTutorialPrefab();
            }

            if (prefab == null)
            {
                return CastTutorialUiBuilder.Build(canvas);
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas);
            instance.SetActive(false);
            return instance.GetComponent<CastTutorialView>();
        }

        static HubView PlaceHubPrefab(Transform canvas)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HubPrefabPath);
            if (prefab == null)
            {
                prefab = BakeHubPrefab();
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas);
            instance.SetActive(true);
            return instance.GetComponent<HubView>();
        }

        public static GameObject BakeHubPrefab()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/UI");
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(HubPrefabPath);
            if (existing != null)
            {
                var root = PrefabUtility.LoadPrefabContents(HubPrefabPath);
                try
                {
                    HubUiBuilder.PopulateHub(root.transform);
                    PrefabUtility.SaveAsPrefabAsset(root, HubPrefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }

                return AssetDatabase.LoadAssetAtPath<GameObject>(HubPrefabPath);
            }

            var bakeRoot = new GameObject("HubPrefabBake", typeof(RectTransform));
            try
            {
                var hub = HubUiBuilder.BuildHub(bakeRoot.transform);
                return PrefabUtility.SaveAsPrefabAsset(hub.gameObject, HubPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(bakeRoot);
            }
        }

        static void ReplaceHubInOpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play Mode before replacing HubView in the scene.");
                return;
            }

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath);
            }

            var canvas = GameObject.Find("UICanvas");
            if (canvas == null)
            {
                Debug.LogError("UICanvas not found in IceFishing scene.");
                return;
            }

            var old = canvas.GetComponentInChildren<HubView>(true);
            var sibling = 0;
            if (old != null)
            {
                sibling = old.transform.GetSiblingIndex();
                Undo.DestroyObjectImmediate(old.gameObject);
            }

            var hub = PlaceHubPrefab(canvas.transform);
            hub.transform.SetSiblingIndex(sibling);
            hub.gameObject.SetActive(true);

            var app = UnityEngine.Object.FindObjectOfType<AppController>();
            if (app != null)
            {
                var so = new SerializedObject(app);
                so.FindProperty("_hubView").objectReferenceValue = hub;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        static SettleView PlaceSettlePrefab(Transform canvas)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SettlePrefabPath);
            if (prefab == null)
            {
                prefab = BakeSettlePrefab();
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas);
            instance.SetActive(false);
            return instance.GetComponent<SettleView>();
        }

        [MenuItem("IceFishing/Ensure Settle Fish Card Slots")]
        public static void EnsureSettleFishCardSlotsFromMenu()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(SettlePrefabPath);
            if (existing == null)
            {
                Debug.LogWarning("Settle prefab not found at " + SettlePrefabPath);
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(SettlePrefabPath);
            try
            {
                EnsureSettleFishCardSlots(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, SettlePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Ensured " + SettleView.FishCardSlotCount + " fish card slots on SettleView prefab.");
        }

        static void EnsureSettleFishCardSlots(Transform root)
        {
            var grid = root.Find("SettleLayout/FishScroll/Viewport/Grid") as RectTransform;
            if (grid == null)
            {
                Debug.LogWarning("SettleView grid not found; cannot create fish card slots.");
                return;
            }

            var gridLayout = grid.GetComponent<GridLayoutGroup>();
            var cellSize = gridLayout != null
                ? gridLayout.cellSize
                : new Vector2(228f, 266f);

            for (var i = grid.childCount - 1; i >= 0; i--)
            {
                var child = grid.GetChild(i);
                if (child.GetComponent<SettleFishCardSlot>() != null)
                {
                    continue;
                }

                Object.DestroyImmediate(child.gameObject);
            }

            for (var i = 0; i < SettleView.FishCardSlotCount; i++)
            {
                var slotName = SettleFishCardSlot.SlotName(i);
                var found = grid.Find(slotName);
                if (found == null)
                {
                    Debug.LogWarning("SettleView prefab missing " + slotName + "; add it in the prefab instead of generating it.");
                    continue;
                }

                var rt = found as RectTransform;
                if (rt != null)
                {
                    rt.sizeDelta = cellSize;
                }

                var slot = found.GetComponent<SettleFishCardSlot>();
                if (slot == null)
                {
                    slot = found.gameObject.AddComponent<SettleFishCardSlot>();
                }

                slot.ClearPlaceholder();
            }
        }

        public static GameObject BakeSettlePrefab()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/UI");
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(SettlePrefabPath);
            if (existing != null)
            {
                var root = PrefabUtility.LoadPrefabContents(SettlePrefabPath);
                try
                {
                    SettleUiBuilder.BindFromHierarchy(root.transform);
                    EnsureSettleFishCardSlots(root.transform);
                    PrefabUtility.SaveAsPrefabAsset(root, SettlePrefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }

                return AssetDatabase.LoadAssetAtPath<GameObject>(SettlePrefabPath);
            }

            Debug.LogError("SettleView prefab missing at " + SettlePrefabPath + ". Layout lives in the prefab; do not generate it from code.");
            return null;
        }

        public static GameObject BakeTutorialPrefab()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/UI");
            ImportTutorialSprites();

            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(TutorialPrefabPath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(TutorialPrefabPath);
            }

            var bakeRoot = new GameObject("TutorialPrefabBake", typeof(RectTransform));
            try
            {
                var tutorial = CastTutorialUiBuilder.Build(bakeRoot.transform);
                var saved = PrefabUtility.SaveAsPrefabAsset(tutorial.gameObject, TutorialPrefabPath);
                Debug.Log("Baked CastTutorialView prefab at " + TutorialPrefabPath);
                return saved;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(bakeRoot);
            }
        }

        static void ImportTutorialSprites()
        {
            ImportUiSprite(HudArtFolder + "/TutorialFrame.png", new Vector4(80f, 80f, 80f, 80f));
            ImportUiSprite(HudArtFolder + "/TutorialHeader.png", Vector4.zero);
            ImportUiSprite(HudArtFolder + "/TutorialClose.png", Vector4.zero);
            ImportUiSprite(HudArtFolder + "/TutorialBubbles.png", Vector4.zero);
        }

        static void ReplaceSettleInOpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play Mode before replacing SettleView in the scene.");
                return;
            }

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath);
            }

            var canvas = GameObject.Find("UICanvas");
            if (canvas == null)
            {
                Debug.LogError("UICanvas not found in IceFishing scene.");
                return;
            }

            var old = canvas.GetComponentInChildren<SettleView>(true);
            var sibling = canvas.transform.childCount;
            if (old != null)
            {
                sibling = old.transform.GetSiblingIndex();
                Undo.DestroyObjectImmediate(old.gameObject);
            }

            var settle = PlaceSettlePrefab(canvas.transform);
            settle.transform.SetSiblingIndex(sibling);
            settle.gameObject.SetActive(false);

            var app = UnityEngine.Object.FindObjectOfType<AppController>();
            if (app != null)
            {
                var so = new SerializedObject(app);
                so.FindProperty("_settleView").objectReferenceValue = settle;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        static CampField PlaceCampPrefab(Transform world, Sprite art)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CampPrefabPath);
            if (prefab == null)
            {
                prefab = BakeCampPrefab();
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, world);
            instance.SetActive(true);
            var camp = instance.GetComponent<CampField>();
            if (camp == null)
            {
                camp = instance.AddComponent<CampField>();
            }

            camp.Configure(art);
            return camp;
        }

        public static GameObject BakeCampPrefab()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/World");
            var art = AssetDatabase.LoadAssetAtPath<Sprite>(HubUiBuilder.CampMapPath);
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(CampPrefabPath);
            if (existing != null)
            {
                var root = PrefabUtility.LoadPrefabContents(CampPrefabPath);
                try
                {
                    var camp = root.GetComponent<CampField>();
                    if (camp == null)
                    {
                        camp = root.AddComponent<CampField>();
                    }

                    if (root.GetComponent<SpriteRenderer>() == null)
                    {
                        var renderer = root.AddComponent<SpriteRenderer>();
                        renderer.sortingOrder = 5;
                    }

                    camp.Configure(art);
                    PrefabUtility.SaveAsPrefabAsset(root, CampPrefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }

                return AssetDatabase.LoadAssetAtPath<GameObject>(CampPrefabPath);
            }

            var go = new GameObject("CampField");
            try
            {
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = 5;
                var camp = go.AddComponent<CampField>();
                camp.Configure(art);
                return PrefabUtility.SaveAsPrefabAsset(go, CampPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void ReplaceCampInOpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play Mode before replacing CampField in the scene.");
                return;
            }

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath);
            }

            var world = UnityEngine.Object.FindObjectOfType<WorldView>();
            if (world == null)
            {
                Debug.LogError("WorldView not found in IceFishing scene.");
                return;
            }

            var art = AssetDatabase.LoadAssetAtPath<Sprite>(HubUiBuilder.CampMapPath);
            var oldCamps = world.GetComponentsInChildren<CampField>(true);
            for (var i = 0; i < oldCamps.Length; i++)
            {
                Undo.DestroyObjectImmediate(oldCamps[i].gameObject);
            }

            var leftover = world.transform.Find("HubBackground");
            if (leftover != null)
            {
                Undo.DestroyObjectImmediate(leftover.gameObject);
            }

            leftover = world.transform.Find("CampField");
            if (leftover != null && leftover.GetComponent<CampField>() == null)
            {
                Undo.DestroyObjectImmediate(leftover.gameObject);
            }

            var camp = PlaceCampPrefab(world.transform, art);
            world.EditorAssignCamp(camp);
            var so = new SerializedObject(world);
            so.FindProperty("_camp").objectReferenceValue = camp;
            so.FindProperty("_background").objectReferenceValue = camp.transform;
            so.FindProperty("_hubBackground").objectReferenceValue = art;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        static UnderwaterField PlaceUnderwaterPrefab(Transform world)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnderwaterPrefabPath);
            if (prefab == null)
            {
                prefab = BakeUnderwaterPrefab();
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, world);
            instance.SetActive(true);
            var field = instance.GetComponent<UnderwaterField>();
            if (field != null)
            {
                field.SetVisible(true);
            }

            return field;
        }

        public static GameObject BakeUnderwaterPrefab()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/World");
            var waterTile = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Backgrounds/WaterTileA.png");
            var leftIce = new[]
            {
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/IceChunks/Ice_02.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/IceChunks/Ice_04.png")
            };
            var rightIce = new[]
            {
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/IceChunks/Ice_03.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/IceChunks/Ice_05.png")
            };
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(UnderwaterPrefabPath);
            if (existing != null)
            {
                var root = PrefabUtility.LoadPrefabContents(UnderwaterPrefabPath);
                try
                {
                    var field = root.GetComponent<UnderwaterField>();
                    if (field == null)
                    {
                        field = root.AddComponent<UnderwaterField>();
                    }

                    field.Populate(waterTile, leftIce, rightIce);
                    PrefabUtility.SaveAsPrefabAsset(root, UnderwaterPrefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }

                return AssetDatabase.LoadAssetAtPath<GameObject>(UnderwaterPrefabPath);
            }

            var go = new GameObject("Underwater");
            try
            {
                var field = go.AddComponent<UnderwaterField>();
                field.Populate(waterTile, leftIce, rightIce);
                return PrefabUtility.SaveAsPrefabAsset(go, UnderwaterPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void ReplaceUnderwaterInOpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play Mode before replacing UnderwaterField in the scene.");
                return;
            }

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath);
            }

            var world = UnityEngine.Object.FindObjectOfType<WorldView>();
            if (world == null)
            {
                Debug.LogError("WorldView not found in IceFishing scene.");
                return;
            }

            var old = world.GetComponentInChildren<UnderwaterField>(true);
            if (old != null)
            {
                Undo.DestroyObjectImmediate(old.gameObject);
            }

            var field = PlaceUnderwaterPrefab(world.transform);
            world.EditorAssignUnderwater(field);
            var so = new SerializedObject(world);
            so.FindProperty("_underwater").objectReferenceValue = field;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        static FishingHudView PlaceHudPrefab(Transform canvas)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            if (prefab == null)
            {
                prefab = BakeHudPrefab();
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas);
            instance.SetActive(false);
            return instance.GetComponent<FishingHudView>();
        }

        public static GameObject BakeHudPrefab()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/UI");
            EnsureFolder(HudArtFolder);
            ImportHudSprites();
            var statsProfile = FishingHudStatsAssetBuilder.Ensure();
            var sprites = LoadHudSprites();
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            if (existing != null)
            {
                var root = PrefabUtility.LoadPrefabContents(HudPrefabPath);
                try
                {
                    FishingHudUiBuilder.PopulateHud(root.transform, sprites, statsProfile);
                    PrefabUtility.SaveAsPrefabAsset(root, HudPrefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }

                return AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            }

            var bakeRoot = new GameObject("HudPrefabBake", typeof(RectTransform));
            try
            {
                var hud = FishingHudUiBuilder.BuildHud(bakeRoot.transform, sprites);
                return PrefabUtility.SaveAsPrefabAsset(hud.gameObject, HudPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(bakeRoot);
            }
        }

        static UiFactory.HudSprites LoadHudSprites()
        {
            return new UiFactory.HudSprites
            {
                Pause = AssetDatabase.LoadAssetAtPath<Sprite>(HudArtFolder + "/HudPause.png"),
                Pill = AssetDatabase.LoadAssetAtPath<Sprite>(HudArtFolder + "/HudPill.png"),
                Haul = AssetDatabase.LoadAssetAtPath<Sprite>(HudArtFolder + "/HudIconReel.png"),
                Protection = AssetDatabase.LoadAssetAtPath<Sprite>(HudArtFolder + "/HudIconHook.png"),
                Depth = AssetDatabase.LoadAssetAtPath<Sprite>(HudArtFolder + "/HudIconLine.png")
            };
        }

        static void ImportHudSprites()
        {
            ImportUiSprite(HudArtFolder + "/HudPause.png", Vector4.zero);
            ImportUiSprite(HudArtFolder + "/HudIconReel.png", Vector4.zero);
            ImportUiSprite(HudArtFolder + "/HudIconHook.png", Vector4.zero);
            ImportUiSprite(HudArtFolder + "/HudIconLine.png", Vector4.zero);
            ImportUiSprite(HudArtFolder + "/HudPill.png", new Vector4(32f, 32f, 32f, 32f));
        }

        static void ImportUiSprite(string path, Vector4 border)
        {
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
            importer.spritePixelsPerUnit = 100f;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
            var name = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "IceFishing";
            PlayerSettings.productName = "IceFishing";
            PlayerSettings.defaultScreenWidth = 1080;
            PlayerSettings.defaultScreenHeight = 2160;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.icefishing.game");
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, "com.icefishing.game");
        }
    }
}
