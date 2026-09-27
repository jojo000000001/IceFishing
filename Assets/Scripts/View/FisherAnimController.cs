using UnityEngine;

/// <summary>
/// 渔夫角色动画控制器
/// 管理角色的动画状态和抛竿动作触发
/// </summary>
[RequireComponent(typeof(Animator))]
public class FisherAnimController : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("抛竿动画速度倍率")]
    [Range(0.5f, 3f)]
    public float castSpeed = 1.5f;
    
    [Tooltip("是否允许抛竿")]
    public bool canCast = true;
    
    [Header("IK Settings")]
    [Tooltip("抛竿时目标点的世界坐标")]
    public Vector3 castTargetPoint = new Vector3(5f, 0f, 0f);
    
    [Header("Events")]
    public System.Action OnCastStart;
    public System.Action OnCastComplete;
    
    private Animator animator;
    private bool isCasting = false;
    private static readonly int CastHash = Animator.StringToHash("Cast");
    private static readonly int IsIdleHash = Animator.StringToHash("IsIdle");
    
    void Awake()
    {
        animator = GetComponent<Animator>();
    }
    
    void Start()
    {
        // Set default idle state
        animator.SetBool(IsIdleHash, true);
    }
    
    /// <summary>
    /// 触发抛竿动画
    /// </summary>
    /// <param name="targetPoint">抛竿目标点（可选）</param>
    public void Cast(Vector3? targetPoint = null)
    {
        if (!canCast || isCasting) return;
        
        // Update target point if provided
        if (targetPoint.HasValue)
        {
            castTargetPoint = targetPoint.Value;
        }
        
        StartCoroutine(CastSequence());
    }
    
    private System.Collections.IEnumerator CastSequence()
    {
        isCasting = true;
        canCast = false;
        
        // Trigger cast animation
        animator.SetTrigger(CastHash);
        
        // Fire start event
        OnCastStart?.Invoke();
        
        // Get animation length
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float animLength = 1f / castSpeed; // Approximate
        
        // Wait for animation to complete
        yield return new WaitForSeconds(animLength);
        
        // Animation complete
        isCasting = false;
        OnCastComplete?.Invoke();
        
        // Allow next cast after short delay
        yield return new WaitForSeconds(0.3f);
        canCast = true;
    }
    
    /// <summary>
    /// 播放待机动画
    /// </summary>
    public void PlayIdle()
    {
        animator.SetBool(IsIdleHash, true);
    }
    
    /// <summary>
    /// 检查是否正在播放抛竿动画
    /// </summary>
    public bool IsCasting => isCasting;
    
    /// <summary>
    /// 检查是否可以抛竿
    /// </summary>
    public bool CanCast => canCast && !isCasting;
    
    /// <summary>
    /// 设置抛竿目标点并触发抛竿
    /// </summary>
    public void CastToPoint(Vector3 worldPoint)
    {
        castTargetPoint = worldPoint;
        Cast();
    }
    
    /// <summary>
    /// 获取当前动画状态名称
    /// </summary>
    public string GetCurrentStateName()
    {
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        return info.shortNameHash.ToString();
    }
    
    /// <summary>
    /// 获取手臂末端的的世界位置（用于鱼线渲染）
    /// </summary>
    public Vector3 GetRodTipWorldPosition()
    {
        Transform rod = transform.Find("FishingRod");
        if (rod != null)
        {
            return rod.TransformPoint(new Vector3(0.6f, 0f, 0f));
        }
        return transform.position + new Vector3(1f, 0.5f, 0f);
    }
    
    /// <summary>
    /// 获取右手的世界位置
    /// </summary>
    public Vector3 GetRightHandWorldPosition()
    {
        Transform arm = transform.Find("ArmRight");
        if (arm != null)
        {
            return arm.position;
        }
        return transform.position + new Vector3(0.4f, 0.1f, 0f);
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw cast target point
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(castTargetPoint, 0.2f);
        
        // Draw rod tip position
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(GetRodTipWorldPosition(), 0.1f);
    }
}
