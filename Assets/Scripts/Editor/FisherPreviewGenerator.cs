using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 角色预览生成器 - 在编辑模式下生成角色预览图
/// </summary>
public class FisherPreviewGenerator : MonoBehaviour
{
    [Header("Preview Settings")]
    public int previewWidth = 512;
    public int previewHeight = 512;
    public Color backgroundColor = new Color(0.2f, 0.25f, 0.35f);
    
    [Header("Character Colors")]
    public Color bodyColor = new Color(0.6f, 0.4f, 0.3f);
    public Color skinColor = new Color(1f, 0.85f, 0.7f);
    public Color hairColor = new Color(0.3f, 0.3f, 0.5f);
    public Color pantsColor = new Color(0.3f, 0.3f, 0.5f);
    public Color bootsColor = new Color(0.2f, 0.2f, 0.3f);
    public Color rodColor = new Color(0.5f, 0.3f, 0.1f);
    
    private GameObject characterRoot;
    private Camera previewCamera;
    private RenderTexture renderTexture;
    
    void Start()
    {
        GeneratePreview();
    }
    
    public void GeneratePreview()
    {
        // Create character
        CreateCharacter();
        
        // Setup camera
        SetupCamera();
        
        // Render preview
        RenderPreview();
    }
    
    private void CreateCharacter()
    {
        // Clean up existing
        if (characterRoot != null)
        {
            DestroyImmediate(characterRoot);
        }
        
        // Create root
        characterRoot = new GameObject("Fisher_Preview");
        characterRoot.transform.position = new Vector3(0, -0.5f, 0);
        characterRoot.transform.localScale = Vector3.one * 2f;
        
        // Create body parts with simple shapes
        CreateBodyPart("Body", bodyColor, new Vector3(0, 0, 0), new Vector3(0.8f, 1f, 0.3f));
        CreateBodyPart("Head", skinColor, new Vector3(0, 0.9f, 0), new Vector3(0.6f, 0.6f, 0.3f));
        CreateBodyPart("Hair", hairColor, new Vector3(0, 1.15f, 0.05f), new Vector3(0.65f, 0.35f, 0.2f));
        
        // Arms
        CreateBodyPart("ArmRight", skinColor, new Vector3(0.5f, 0.2f, 0), new Vector3(0.25f, 0.6f, 0.2f));
        CreateBodyPart("ArmLeft", skinColor, new Vector3(-0.5f, 0.2f, 0), new Vector3(0.25f, 0.6f, 0.2f));
        
        // Legs
        CreateBodyPart("LegRight", pantsColor, new Vector3(0.2f, -0.7f, 0), new Vector3(0.25f, 0.7f, 0.2f));
        CreateBodyPart("LegLeft", pantsColor, new Vector3(-0.2f, -0.7f, 0), new Vector3(0.25f, 0.7f, 0.2f));
        
        // Boots
        CreateBodyPart("Boots", bootsColor, new Vector3(0, -1.15f, 0), new Vector3(0.6f, 0.25f, 0.2f));
        
        // Fishing Rod
        CreateRod();
        
        // Face features
        CreateFace();
    }
    
    private void CreateBodyPart(string name, Color color, Vector3 localPos, Vector3 scale)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(characterRoot.transform);
        part.transform.localPosition = localPos;
        part.transform.localScale = scale;
        
        var renderer = part.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Standard"));
        renderer.material.color = color;
        
        // Disable shadow for cleaner preview
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }
    
    private void CreateRod()
    {
        var rodRoot = new GameObject("FishingRod");
        rodRoot.transform.SetParent(characterRoot.transform.Find("ArmRight"));
        rodRoot.transform.localPosition = new Vector3(0.2f, 0.5f, 0);
        rodRoot.transform.localRotation = Quaternion.Euler(0, 0, -45f);
        
        // Rod handle
        var handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        handle.name = "Handle";
        handle.transform.SetParent(rodRoot.transform);
        handle.transform.localPosition = new Vector3(0, 0.15f, 0);
        handle.transform.localScale = new Vector3(0.08f, 0.3f, 0.08f);
        SetPartMaterial(handle, rodColor);
        
        // Rod body
        var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "Body";
        body.transform.SetParent(rodRoot.transform);
        body.transform.localPosition = new Vector3(0, 0.8f, 0);
        body.transform.localScale = new Vector3(0.04f, 1.2f, 0.04f);
        SetPartMaterial(body, new Color(0.6f, 0.5f, 0.3f));
        
        // Rod tip
        var tip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tip.name = "Tip";
        tip.transform.SetParent(rodRoot.transform);
        tip.transform.localPosition = new Vector3(0, 1.7f, 0);
        tip.transform.localScale = new Vector3(0.02f, 0.5f, 0.02f);
        SetPartMaterial(tip, rodColor);
        
        // Fishing line
        var line = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        line.name = "Line";
        line.transform.SetParent(rodRoot.transform);
        line.transform.localPosition = new Vector3(0, -0.8f, 0);
        line.transform.localScale = new Vector3(0.005f, 1.5f, 0.005f);
        SetPartMaterial(line, Color.white);
        
        // Hook
        var hook = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hook.name = "Hook";
        hook.transform.SetParent(rodRoot.transform);
        hook.transform.localPosition = new Vector3(0, -1.6f, 0);
        hook.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        SetPartMaterial(hook, new Color(0.7f, 0.7f, 0.7f));
    }
    
    private void CreateFace()
    {
        var head = characterRoot.transform.Find("Head");
        
        // Eyes
        CreateFaceFeature("EyeLeft", head, new Vector3(-0.12f, 0.1f, 0.16f), 0.08f, Color.black);
        CreateFaceFeature("EyeRight", head, new Vector3(0.12f, 0.1f, 0.16f), 0.08f, Color.black);
        
        // Eye highlights
        CreateFaceFeature("EyeHL_L", head, new Vector3(-0.10f, 0.12f, 0.17f), 0.03f, Color.white);
        CreateFaceFeature("EyeHL_R", head, new Vector3(0.14f, 0.12f, 0.17f), 0.03f, Color.white);
        
        // Mouth
        CreateFaceFeature("Mouth", head, new Vector3(0, -0.1f, 0.16f), 0.06f, new Color(0.8f, 0.4f, 0.4f));
        
        // Blush
        CreateFaceFeature("BlushL", head, new Vector3(-0.18f, -0.05f, 0.15f), 0.06f, new Color(1f, 0.7f, 0.7f));
        CreateFaceFeature("BlushR", head, new Vector3(0.18f, -0.05f, 0.15f), 0.06f, new Color(1f, 0.7f, 0.7f));
    }
    
    private void CreateFaceFeature(string name, Transform parent, Vector3 localPos, float size, Color color)
    {
        var feature = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        feature.name = name;
        feature.transform.SetParent(parent);
        feature.transform.localPosition = localPos;
        feature.transform.localScale = Vector3.one * size;
        SetPartMaterial(feature, color);
    }
    
    private void SetPartMaterial(GameObject obj, Color color)
    {
        var renderer = obj.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Standard"));
        renderer.material.color = color;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }
    
    private void SetupCamera()
    {
        // Find or create preview camera
        var camObj = GameObject.Find("PreviewCamera");
        if (camObj != null)
        {
            DestroyImmediate(camObj);
        }
        
        var camGO = new GameObject("PreviewCamera");
        previewCamera = camGO.AddComponent<Camera>();
        previewCamera.transform.position = new Vector3(0, 0.5f, -5);
        previewCamera.transform.LookAt(new Vector3(0, 0, 0));
        previewCamera.backgroundColor = backgroundColor;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        
        // Orthographic for 2D look
        previewCamera.orthographic = true;
        previewCamera.orthographicSize = 2.5f;
    }
    
    private void RenderPreview()
    {
        // Create render texture
        if (renderTexture != null)
        {
            renderTexture.Release();
        }
        
        renderTexture = new RenderTexture(previewWidth, previewHeight, 24);
        previewCamera.targetTexture = renderTexture;
        
        // Render
        previewCamera.Render();
        
        // Read pixels
        RenderTexture.active = renderTexture;
        Texture2D preview = new Texture2D(previewWidth, previewHeight, TextureFormat.RGB24, false);
        preview.ReadPixels(new Rect(0, 0, previewWidth, previewHeight), 0, 0);
        preview.Apply();
        
        // Save to file
        byte[] bytes = preview.EncodeToPNG();
        string path = Application.dataPath + "/Art/Sprites/Character/Fisher_Preview.png";
        System.IO.File.WriteAllBytes(path, bytes);
        
        Debug.Log("Preview saved to: " + path);
        
        // Cleanup
        RenderTexture.active = null;
        
        // Refresh asset database
        UnityEditor.AssetDatabase.Refresh();
    }
    
    void OnDestroy()
    {
        if (characterRoot != null) DestroyImmediate(characterRoot);
        if (previewCamera != null) DestroyImmediate(previewCamera.gameObject);
        if (renderTexture != null) renderTexture.Release();
    }
}
