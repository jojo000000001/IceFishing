namespace IceFishing.Core
{
    /// <summary>
    /// 一局冰钓的互斥阶段。碰撞语义由当前阶段决定，而不是由鱼自己决定。
    /// 本阶段（M1）只用于 HUD 文案，真正的下潜/上浮规则在后续里程碑接入。
    /// </summary>
    public enum CastPhase
    {
        None = 0,
        Ready,
        Descending,
        Ascending,
        Returning,
        Settle,
        Paused
    }

    /// <summary>
    /// 应用级界面。营地与钓鱼之间有一段镜头下沉。
    /// </summary>
    public enum AppScreen
    {
        Hub = 0,
        Fishing = 1,
        Diving = 2
    }

    /// <summary>
    /// 仅开发/验收使用的开关。正式规则仍以 PlayerProfile 为准。
    /// </summary>
    public static class DebugFlags
    {
        public static bool InfiniteBait;
    }
}
