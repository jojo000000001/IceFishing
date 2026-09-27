using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 渔夫角色2D骨骼动画系统设置工具
/// 菜单位置: IceFishing > Setup Fisher Character
/// </summary>
public class FisherCharacterSetup : EditorWindow
{
    private static GameObject fisherRoot;
    private static Dictionary<string, GameObject> bodyParts = new Dictionary<string, GameObject>();
    
    [MenuItem("IceFishing/Setup Fisher Character")]
    public static void SetupFisherCharacter()
    {
        // Create main character root
        fisherRoot = new GameObject("Fisher");
        fisherRoot.transform.position = new Vector3(0, -2f, 0);
        
        // Add Animator component
        var animator = fisherRoot.AddComponent<Animator>();
        var rootRenderer = fisherRoot.AddComponent<SpriteRenderer>();
        rootRenderer.sortingOrder = 10;
        rootRenderer.sortingLayerName = "Character";
        
        // Create body hierarchy
        string[] partNames = { "Body", "Head", "Hair", "ArmRight", "ArmLeft", "LegRight", "LegLeft", "Boots", "FishingRod", "Face" };
        
        for (int i = 0; i < partNames.Length; i++)
        {
            string partName = partNames[i];
            GameObject part = new GameObject(partName);
            part.transform.SetParent(fisherRoot.transform);
            
            var sr = part.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 10 + i;
            sr.sortingLayerName = "Character";
            
            SetPartPosition(sr.transform, partName);
            bodyParts[partName] = part;
        }
        
        // Add IK hints component
        var ikHints = fisherRoot.AddComponent<FisherIKHints>();
        ikHints.Initialize(bodyParts);
        
        // Create Animator Controller
        RuntimeAnimatorController controller = CreateAnimatorController();
        
        // Create animations
        AnimationClip idleClip = CreateIdleAnimation();
        AnimationClip castClip = CreateCastAnimation();
        
        // Assign animations to controller states
        AssignAnimationsToController(controller, idleClip, castClip);
        
        // Assign controller to animator
        animator.runtimeAnimatorController = controller;
        
        // Create a simple material for visible sprites (placeholder)
        CreatePlaceholderSprites();
        
        // Save prefab
        SavePrefab();
        
        Selection.activeGameObject = fisherRoot;
        
        Debug.Log("Fisher Character created successfully!");
        Debug.Log("- Root: Fisher");
        Debug.Log("- Parts: " + string.Join(", ", partNames));
        Debug.Log("- Animator Controller: FisherController");
        Debug.Log("- Animations: Idle, Cast");
    }
    
    private static void SetPartPosition(Transform t, string partName)
    {
        switch (partName)
        {
            case "Body":
                t.localPosition = new Vector3(0, 0, 0);
                break;
            case "Head":
                t.localPosition = new Vector3(0, 0.8f, 0);
                break;
            case "Hair":
                t.localPosition = new Vector3(0, 1.1f, 0.01f);
                break;
            case "Face":
                t.localPosition = new Vector3(0, 0.85f, 0.02f);
                break;
            case "ArmRight":
                t.localPosition = new Vector3(0.4f, 0.1f, 0);
                break;
            case "ArmLeft":
                t.localPosition = new Vector3(-0.4f, 0.1f, 0);
                break;
            case "LegRight":
                t.localPosition = new Vector3(0.15f, -0.7f, 0);
                break;
            case "LegLeft":
                t.localPosition = new Vector3(-0.15f, -0.7f, 0);
                break;
            case "Boots":
                t.localPosition = new Vector3(0, -1.1f, 0);
                break;
            case "FishingRod":
                t.localPosition = new Vector3(0.6f, 0.3f, 0);
                t.localRotation = Quaternion.Euler(0, 0, -30f);
                break;
        }
    }
    
    private static UnityEditor.Animations.AnimatorController CreateAnimatorController()
    {
        string path = "Assets/Animations/Character/FisherController.controller";
        
        // Ensure directory exists
        if (!AssetDatabase.IsValidFolder("Assets/Animations/Character"))
        {
            AssetDatabase.CreateFolder("Assets/Animations", "Character");
        }
        
        var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(path);
        
        // Add parameters
        var castParam = new AnimatorControllerParameter();
        castParam.name = "Cast";
        castParam.type = AnimatorControllerParameterType.Trigger;
        controller.AddParameter(castParam);
        
        var idleParam = new AnimatorControllerParameter();
        idleParam.name = "IsIdle";
        idleParam.type = AnimatorControllerParameterType.Bool;
        idleParam.defaultBool = true;
        controller.AddParameter(idleParam);
        
        // Get layers
        var baseLayer = controller.layers[0];
        var stateMachine = baseLayer.stateMachine;
        
        // Create states
        var idleState = stateMachine.AddState("Idle");
        var castState = stateMachine.AddState("Cast");
        castState.speed = 1.5f; // Faster cast animation
        
        // Set default state
        stateMachine.defaultState = idleState;
        
        // Create transitions
        var idleToCast = idleState.AddTransition(castState);
        idleToCast.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "Cast");
        idleToCast.duration = 0.1f;
        
        var castToIdle = castState.AddTransition(idleState);
        castToIdle.duration = 0.2f;
        castToIdle.hasExitTime = true;
        castToIdle.exitTime = 0.8f;
        
        AssetDatabase.SaveAssets();
        return controller;
    }
    
    private static AnimationClip CreateIdleAnimation()
    {
        var clip = new AnimationClip();
        clip.name = "Idle";
        clip.wrapMode = WrapMode.Loop;
        
        // Subtle breathing animation for body
        var bodyPosCurve = new AnimationCurve(
            new Keyframe(0, 0),
            new Keyframe(0.5f, 0.02f),
            new Keyframe(1, 0),
            new Keyframe(1.5f, -0.01f),
            new Keyframe(2, 0)
        );
        AnimationUtility.SetEditorCurve(clip, 
            AnimationUtility.CalculateTransformPath(bodyParts["Body"].transform, fisherRoot.transform),
            typeof(Transform), "m_LocalPosition.y", bodyPosCurve);
        
        // Head subtle movement
        var headPosCurve = new AnimationCurve(
            new Keyframe(0, 0),
            new Keyframe(1, 0.01f),
            new Keyframe(2, 0)
        );
        AnimationUtility.SetEditorCurve(clip, 
            AnimationUtility.CalculateTransformPath(bodyParts["Head"].transform, fisherRoot.transform),
            typeof(Transform), "m_LocalPosition.y", headPosCurve);
        
        string clipPath = "Assets/Animations/Character/Idle.anim";
        AssetDatabase.CreateAsset(clip, clipPath);
        AssetDatabase.SaveAssets();
        
        return clip;
    }
    
    private static AnimationClip CreateCastAnimation()
    {
        var clip = new AnimationClip();
        clip.name = "Cast";
        clip.wrapMode = WrapMode.Once;
        
        // Cast animation keyframes
        // Phase 1: Wind up (0-0.3s) - arm pulls back
        // Phase 2: Cast (0.3-0.5s) - arm swings forward
        // Phase 3: Release (0.5-0.7s) - rod tip follows through
        // Phase 4: Settle (0.7-1s) - return to idle
        
        float[] times = { 0f, 0.2f, 0.35f, 0.5f, 0.7f, 1f };
        float[] armAngles = { 0f, -60f, 120f, 90f, 30f, 0f };
        float[] rodAngles = { -30f, -80f, 60f, 40f, 10f, -15f };
        
        var armRotationCurve = new AnimationCurve();
        var rodRotationCurve = new AnimationCurve();
        
        for (int i = 0; i < times.Length; i++)
        {
            armRotationCurve.AddKey(new Keyframe(times[i], armAngles[i]));
            rodRotationCurve.AddKey(new Keyframe(times[i], rodAngles[i]));
        }
        
        // Smooth the curves
        for (int i = 0; i < armRotationCurve.keys.Length; i++)
        {
            armRotationCurve.SmoothTangents(i, 0.5f);
            rodRotationCurve.SmoothTangents(i, 0.5f);
        }
        
        string armPath = AnimationUtility.CalculateTransformPath(bodyParts["ArmRight"].transform, fisherRoot.transform);
        string rodPath = AnimationUtility.CalculateTransformPath(bodyParts["FishingRod"].transform, fisherRoot.transform);
        
        AnimationUtility.SetEditorCurve(clip, armPath, typeof(Transform), "localEulerAngles.z", armRotationCurve);
        AnimationUtility.SetEditorCurve(clip, rodPath, typeof(Transform), "localEulerAngles.z", rodRotationCurve);
        
        // Add body lean animation
        var bodyLeanCurve = new AnimationCurve(
            new Keyframe(0, 0),
            new Keyframe(0.2f, -5f),
            new Keyframe(0.5f, 10f),
            new Keyframe(1f, 0)
        );
        AnimationUtility.SetEditorCurve(clip, 
            AnimationUtility.CalculateTransformPath(bodyParts["Body"].transform, fisherRoot.transform),
            typeof(Transform), "localEulerAngles.z", bodyLeanCurve);
        
        string clipPath = "Assets/Animations/Character/Cast.anim";
        AssetDatabase.CreateAsset(clip, clipPath);
        AssetDatabase.SaveAssets();
        
        return clip;
    }
    
    private static void AssignAnimationsToController(RuntimeAnimatorController controller, AnimationClip idle, AnimationClip cast)
    {
        var unityController = controller as UnityEditor.Animations.AnimatorController;
        if (unityController != null)
        {
            var states = unityController.layers[0].stateMachine.states;
            if (states.Length > 0) states[0].state.motion = idle;  // Idle
            if (states.Length > 1) states[1].state.motion = cast;  // Cast
            AssetDatabase.SaveAssets();
        }
    }
    
    private static void CreatePlaceholderSprites()
    {
        // Create placeholder colored sprites for visualization
        // In production, replace these with actual art assets
        
        Sprite[] placeholderSprites = new Sprite[bodyParts.Count];
        Color[] partColors = new Color[]
        {
            new Color(0.6f, 0.4f, 0.3f),   // Body - brown
            new Color(1f, 0.85f, 0.7f),    // Head - skin tone
            new Color(0.3f, 0.3f, 0.5f),  // Hair - dark blue
            new Color(1f, 0.85f, 0.7f),   // ArmRight - skin
            new Color(1f, 0.85f, 0.7f),   // ArmLeft - skin
            new Color(0.3f, 0.3f, 0.5f),  // LegRight - pants
            new Color(0.3f, 0.3f, 0.5f),  // LegLeft - pants
            new Color(0.2f, 0.2f, 0.3f),  // Boots - dark
            new Color(0.5f, 0.3f, 0.1f),  // FishingRod - wood
            new Color(1f, 0.9f, 0.8f)     // Face - skin light
        };
        
        int i = 0;
        foreach (var kvp in bodyParts)
        {
            var sr = kvp.Value.GetComponent<SpriteRenderer>();
            // Create a simple placeholder - in production, assign actual sprites here
            sr.color = partColors[i];
            i++;
        }
    }
    
    private static void SavePrefab()
    {
        string prefabFolder = "Assets/Prefabs/World/Character";
        
        // Ensure directory exists
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/World/Character"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/World"))
                AssetDatabase.CreateFolder("Assets/Prefabs", "World");
            AssetDatabase.CreateFolder("Assets/Prefabs/World", "Character");
        }
        
        string prefabPath = prefabFolder + "/Fisher.prefab";
        PrefabUtility.SaveAsPrefabAsset(fisherRoot, prefabPath);
        Debug.Log("Prefab saved to: " + prefabPath);
    }
}
