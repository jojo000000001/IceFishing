using IceFishing.Controller;
using IceFishing.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IceFishing.EditorTools
{
    /// <summary>
    /// 将场景中的 World（含 Camp / Hook / 水下 / 鱼场）烘焙为预制体，并在场景里用实例替换。
    /// </summary>
    public static class IceFishingWorldPrefab
    {
        public const string PrefabPath = "Assets/Prefabs/World/IceFishingWorld.prefab";

        [MenuItem("IceFishing/Bake Ice Fishing World Prefab")]
        public static void BakeFromOpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play Mode before baking IceFishingWorld prefab.");
                return;
            }

            var world = GameObject.Find("World");
            if (world == null)
            {
                Debug.LogError("GameObject 'World' not found in open scene.");
                return;
            }

            EnsureFolder();
            IceFishingHookPrefab.ApplyToOpenScene();

            var assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(world);
            if (!string.IsNullOrEmpty(assetPath) && assetPath == PrefabPath)
            {
                PrefabUtility.ApplyPrefabInstance(world, InteractionMode.UserAction);
                AssetDatabase.SaveAssets();
                Debug.Log("Applied scene World overrides to " + PrefabPath);
                return;
            }

            PrefabUtility.SaveAsPrefabAsset(world, PrefabPath, out var success);
            if (!success)
            {
                Debug.LogError("Failed to save " + PrefabPath);
                return;
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Saved " + PrefabPath);
        }

        [MenuItem("IceFishing/Wire World Prefab On Bootstrap")]
        public static void WireBootstrapWorldPrefabMenu()
        {
            WireBootstrapWorldPrefab();
        }

        public static void WireBootstrapWorldPrefab()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play Mode before wiring world prefab.");
                return;
            }

            var prefab = LoadOrBake();
            if (prefab == null)
            {
                return;
            }

            var bootstrap = Object.FindObjectOfType<AppController>();
            if (bootstrap == null)
            {
                Debug.LogError("AppController (Bootstrap) not found in open scene.");
                return;
            }

            var oldWorld = GameObject.Find("World");
            if (oldWorld != null)
            {
                Object.DestroyImmediate(oldWorld);
            }

            var serializedBootstrap = new SerializedObject(bootstrap);
            serializedBootstrap.FindProperty("_worldPrefab").objectReferenceValue = prefab;
            serializedBootstrap.FindProperty("_worldView").objectReferenceValue = null;
            serializedBootstrap.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(bootstrap.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Bootstrap uses IceFishingWorld prefab at Play; no World object kept in scene.");
        }

        /// <summary>
        /// 在 Prefab 模式下保存 World 资源后，规范化 Hook 引用与嵌套 FishingHook。
        /// </summary>
        public static void NormalizeWorldPrefabContents(GameObject worldRoot)
        {
            if (worldRoot == null)
            {
                return;
            }

            worldRoot.name = "World";
            var worldView = worldRoot.GetComponent<WorldView>();
            var hook = worldRoot.transform.Find("Hook");
            if (hook == null)
            {
                hook = worldRoot.transform.Find("FishingHook");
            }

            if (hook == null)
            {
                var visual = worldRoot.GetComponentInChildren<FishingHookVisual>(true);
                if (visual != null)
                {
                    hook = visual.transform;
                }
            }

            var hookPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(IceFishingHookPrefab.PrefabPath);
            if (hook == null && hookPrefab != null)
            {
                hook = IceFishingHookPrefab.PlaceUnderWorld(worldRoot.transform, hookPrefab);
            }

            if (hook != null)
            {
                var hookRoot = hook.gameObject;
                var visual = hookRoot.GetComponent<FishingHookVisual>();
                if (visual == null)
                {
                    visual = hookRoot.AddComponent<FishingHookVisual>();
                }

                visual.Apply();
                var shield = hook.Find("HookShield");
                if (shield != null)
                {
                    HookShieldSetup.ApplyLayout(hook, shield);
                }

                RemoveDuplicateHookShields(hook);
            }

            if (worldView != null)
            {
                var serialized = new SerializedObject(worldView);
                if (hook != null)
                {
                    serialized.FindProperty("_hook").objectReferenceValue = hook;
                    var line = hook.GetComponent<LineRenderer>();
                    serialized.FindProperty("_line").objectReferenceValue = line;
                }

                if (hookPrefab != null)
                {
                    serialized.FindProperty("_hookPrefab").objectReferenceValue = hookPrefab;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void RemoveDuplicateHookShields(Transform hook)
        {
            if (hook == null)
            {
                return;
            }

            Transform keep = null;
            for (var i = 0; i < hook.childCount; i++)
            {
                var child = hook.GetChild(i);
                if (child.name != "HookShield")
                {
                    continue;
                }

                if (keep == null)
                {
                    keep = child;
                    continue;
                }

                Object.DestroyImmediate(child.gameObject);
                i--;
            }
        }

        public static GameObject LoadOrBake()
        {
            EnsureFolder();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab != null)
            {
                return prefab;
            }

            BakeFromOpenScene();
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        public static WorldView InstantiateInScene()
        {
            var prefab = LoadOrBake();
            if (prefab == null)
            {
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "World";
            return instance.GetComponent<WorldView>();
        }

        static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/World"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "World");
            }
        }
    }
}
