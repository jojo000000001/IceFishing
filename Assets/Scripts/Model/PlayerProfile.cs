using IceFishing.Core;

namespace IceFishing.Model
{
    /// <summary>
    /// 玩家持久状态。M1 暂不写盘，只给营地 UI 显示饵料与渔具等级。
    /// 不含 Unity 场景对象。
    /// </summary>
    public sealed class PlayerProfile
    {
        public const int MaxBait = 10;

        public int Bait;
        public int Tokens;
        public int Shells;
        public int LineLevel = 1;
        public int HookLevel = 1;
        public int SinkerLevel = 1;
        public int StabilizerCount;
        public int LanternCount;
        public int ScannerCount;

        public static PlayerProfile CreateNew()
        {
            return new PlayerProfile
            {
                Bait = 8,
                Tokens = 0,
                Shells = 0,
                LineLevel = 1,
                HookLevel = 1,
                SinkerLevel = 1,
                StabilizerCount = 2,
                LanternCount = 2,
                ScannerCount = 2
            };
        }

        public int DisplayBait
        {
            get { return DebugFlags.InfiniteBait ? MaxBait : Bait; }
        }
    }
}
