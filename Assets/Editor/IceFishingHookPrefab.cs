using IceFishing.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IceFishing.EditorTools
{
    /// <summary>
    /// 将场景 Hook 烘焙为预制体，并在 World 下以预制体实例摆位。
    /// </summary>
    public static class IceFishingHookPrefab
    {
        public const string PrefabPath = "Assets/Prefabs/World/FishingHook.prefab";

        [MenuItem("IceFishing/Bake Fishing Hook Prefab")]
        public static void BakeFromSceneOrTemplate()
        {
            EnsureFolder();
            var sceneHook = GameObject.Find("World/Hook");
            if (sceneHook != null && PrefabUtility.IsPartOfPrefabInstance(sceneHook))
            {
                var assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(sceneHook);
                if (!string.IsNullOrEmpty(assetPath) && assetPath == PrefabPath)
                {
                    SaveHookPrefabAssetFromSceneInstance(sceneHook);
                    return;
                }
            }

            if (sceneHook != null && !PrefabUtility.IsPartOfPrefabInstance(sceneHook))
            {
                SaveHookAsPrefab(sceneHook);
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                Debug.LogError("FishingHook prefab missing at " + PrefabPath + " and no scene Hook to bake.");
            }
        }

        [MenuItem("IceFishing/Apply Fishing Hook Prefab To Scene")]
        public static void ApplyToOpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play Mode before applying Fishing Hook prefab.");
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                BakeFromSceneOrTemplate();
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            }

            if (prefab == null)
            {
                Debug.LogError("FishingHook prefab missing at " + PrefabPath);
                return;
            }

            var world = Object.FindObjectOfType<WorldView>();
            if (world == null)
            {
                Debug.LogError("WorldView not found in open scene.");
                return;
            }

            var worldTransform = world.transform;
            var existing = worldTransform.Find("Hook");
            if (existing == null)
            {
                existing = worldTransform.Find("FishingHook");
            }

            Vector3 localPos = new Vector3(0.08f, 6.5f, -0.12f);
            Quaternion localRot = Quaternion.identity;
            if (existing != null)
            {
                localPos = existing.localPosition;
                localRot = existing.localRotation;
                Object.DestroyImmediate(existing.gameObject);
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, worldTransform);
            instance.name = "Hook";
            var hookTransform = instance.transform;
            hookTransform.localPosition = localPos;
            hookTransform.localRotation = localRot;
            NormalizeHookRoot(instance);

            var line = instance.GetComponent<LineRenderer>();
            var serializedWorld = new SerializedObject(world);
            serializedWorld.FindProperty("_hook").objectReferenceValue = hookTransform;
            serializedWorld.FindProperty("_line").objectReferenceValue = line;
            serializedWorld.FindProperty("_hookPrefab").objectReferenceValue = prefab;
            serializedWorld.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(world.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Fishing Hook prefab instance applied under World.");
        }

        public static GameObject LoadOrBakePrefab()
        {
            EnsureFolder();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab != null)
            {
                return prefab;
            }

            BakeFromSceneOrTemplate();
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        public static Transform PlaceUnderWorld(Transform world, GameObject prefab)
        {
            if (world == null || prefab == null)
            {
                return null;
            }

            var existing = world.Find("Hook");
            if (existing == null)
            {
                existing = world.Find("FishingHook");
            }

            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, world);
            instance.name = "Hook";
            return instance.transform;
        }

        static void SaveHookPrefabAssetFromSceneInstance(GameObject sceneHook)
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                root.transform.localPosition = sceneHook.transform.localPosition;
                root.transform.localRotation = sceneHook.transform.localRotation;
                NormalizeHookRoot(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out var success);
                if (!success)
                {
                    Debug.LogError("Failed to save FishingHook prefab.");
                    return;
                }

                AssetDatabase.SaveAssets();
                Debug.Log("Saved " + PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void SaveHookAsPrefab(GameObject hookRoot)
        {
            NormalizeHookRoot(hookRoot);
            PrefabUtility.SaveAsPrefabAsset(hookRoot, PrefabPath, out var success);
            if (!success)
            {
                Debug.LogError("Failed to save FishingHook prefab.");
                return;
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Saved " + PrefabPath);
        }

        static void NormalizeHookRoot(GameObject hookRoot)
        {
            if (hookRoot == null)
            {
                return;
            }

            var visual = hookRoot.GetComponent<FishingHookVisual>();
            if (visual == null)
            {
                visual = hookRoot.AddComponent<FishingHookVisual>();
            }

            visual.Apply();
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
