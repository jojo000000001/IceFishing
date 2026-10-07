using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 渔夫角色IK骨骼提示组件
/// 提供动画编辑时的视觉提示和IK定位辅助
/// </summary>
public class FisherIKHints : MonoBehaviour
{
    [Header("IK Hint Transforms")]
    public Transform ArmRightHint;
    public Transform ArmLeftHint;
    public Transform RodTip;
    public Transform FishingLineEnd;
    
    [Header("Gizmo Settings")]
    public Color hintColor = Color.yellow;
    public Color rodTipColor = Color.cyan;
    public Color lineColor = Color.green;
    public float hintRadius = 0.1f;
    public float rodTipRadius = 0.08f;
    
    private Dictionary<string, GameObject> bodyParts;
    private LineRenderer fishingLine;
    
    /// <summary>
    /// 初始化IK骨骼提示
    /// </summary>
    public void Initialize(Dictionary<string, GameObject> parts)
    {
        bodyParts = parts;
        
        // Create IK hint objects
        ArmRightHint = CreateHint("IK_ArmRightHint", new Vector3(0.8f, 0.6f, 0), hintColor);
        ArmLeftHint = CreateHint("IK_ArmLeftHint", new Vector3(-0.5f, 0.3f, 0), hintColor);
        RodTip = CreateHint("IK_RodTip", new Vector3(1.2f, 1.2f, 0), rodTipColor);
        FishingLineEnd = CreateHint("IK_FishingLineEnd", new Vector3(1.5f, -0.5f, 0), lineColor);
        
        // Setup fishing line
        SetupFishingLine();
    }
    
    private Transform CreateHint(string name, Vector3 localPos, Color color)
    {
        var hint = new GameObject(name);
        hint.transform.SetParent(transform);
        hint.transform.localPosition = localPos;
        hint.hideFlags = HideFlags.DontSave | HideFlags.HideInInspector;
        
        // Add a small sphere mesh for visibility in editor
        var meshFilter = hint.AddComponent<MeshFilter>();
        var meshRenderer = hint.AddComponent<MeshRenderer>();
        meshRenderer.material = new Material(Shader.Find("Sprites/Default"));
        meshRenderer.material.color = color;
        
        // Create sphere mesh
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        meshFilter.sharedMesh = sphere.GetComponent<MeshFilter>().sharedMesh;
        meshFilter.transform.localScale = Vector3.one * 0.05f;
        if (Application.isPlaying)
            Destroy(sphere);
        else
            DestroyImmediate(sphere);
        
        return hint.transform;
    }
    
    private void SetupFishingLine()
    {
        var lineObj = new GameObject("FishingLine");
        lineObj.transform.SetParent(transform);
        lineObj.transform.localPosition = Vector3.zero;
        
        fishingLine = lineObj.AddComponent<LineRenderer>();
        fishingLine.material = new Material(Shader.Find("Sprites/Default"));
        fishingLine.startColor = lineColor;
        fishingLine.endColor = lineColor;
        fishingLine.startWidth = 0.02f;
        fishingLine.endWidth = 0.01f;
        fishingLine.positionCount = 2;
        fishingLine.useWorldSpace = true;
        fishingLine.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
    }
    
    /// <summary>
    /// 更新鱼线渲染
    /// </summary>
    public void UpdateFishingLine()
    {
        if (fishingLine == null) return;
        
        Vector3 startPos = GetRodTipWorldPosition();
        Vector3 endPos = FishingLineEnd != null ? FishingLineEnd.position : startPos + new Vector3(0.5f, -1.5f, 0);
        
        fishingLine.SetPosition(0, startPos);
        fishingLine.SetPosition(1, endPos);
    }
    
    /// <summary>
    /// 获取鱼竿顶端的世界位置
    /// </summary>
    public Vector3 GetRodTipWorldPosition()
    {
        Transform rod = transform.Find("FishingRod");
        if (rod != null)
        {
            // 鱼竿顶端大约在本地坐标(0.6, 0, 0)处
            return rod.TransformPoint(new Vector3(0.6f, 0f, 0f));
        }
        return RodTip != null ? RodTip.position : transform.position + new Vector3(1.2f, 1.2f, 0);
    }
    
    /// <summary>
    /// 获取鱼线末端的世界位置
    /// </summary>
    public Vector3 GetFishingLineEndPosition()
    {
        return FishingLineEnd != null ? FishingLineEnd.position : transform.position + new Vector3(1.5f, -0.5f, 0);
    }
    
    /// <summary>
    /// 设置抛竿目标点
    /// </summary>
    public void SetCastTarget(Vector3 worldPosition)
    {
        if (FishingLineEnd != null)
        {
            FishingLineEnd.position = worldPosition;
        }
    }
    
    /// <summary>
    /// 抛竿动画过程中更新鱼线（抛物线效果）
    /// </summary>
    public void UpdateCastLine(Vector3 start, Vector3 end, float progress)
    {
        if (fishingLine == null) return;
        
        // Add arc to the line
        Vector3 midPoint = Vector3.Lerp(start, end, 0.5f);
        midPoint.y += 0.5f * Mathf.Sin(progress * Mathf.PI); // Arc height
        
        fishingLine.positionCount = 3;
        fishingLine.SetPosition(0, start);
        fishingLine.SetPosition(1, midPoint);
        fishingLine.SetPosition(2, Vector3.Lerp(end, start + new Vector3(0, -1.5f, 0), progress));
    }
    
    /// <summary>
    /// 重置鱼线到待机状态
    /// </summary>
    public void ResetLine()
    {
        if (fishingLine == null) return;
        
        fishingLine.positionCount = 2;
        UpdateFishingLine();
    }
    
    void Update()
    {
        // Update fishing line in real-time
        UpdateFishingLine();
    }
    
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        // Draw rod tip
        Gizmos.color = rodTipColor;
        Gizmos.DrawWireSphere(GetRodTipWorldPosition(), rodTipRadius);
        
        // Draw fishing line end
        Gizmos.color = lineColor;
        Gizmos.DrawWireSphere(GetFishingLineEndPosition(), hintRadius * 0.5f);
        
        // Draw fishing line
        Gizmos.color = lineColor;
        Vector3 rodTip = GetRodTipWorldPosition();
        Vector3 lineEnd = GetFishingLineEndPosition();
        Gizmos.DrawLine(rodTip, lineEnd);
    }
    
    void OnValidate()
    {
        // Auto-find if not set
        if (ArmRightHint == null) ArmRightHint = transform.Find("IK_ArmRightHint");
        if (ArmLeftHint == null) ArmLeftHint = transform.Find("IK_ArmLeftHint");
        if (RodTip == null) RodTip = transform.Find("IK_RodTip");
        if (FishingLineEnd == null) FishingLineEnd = transform.Find("IK_FishingLineEnd");
    }
}
