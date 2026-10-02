using IceFishing.View;
using UnityEngine;

namespace IceFishing.Model
{
    /// <summary>
    /// 一种鱼的只读数据。表现在预制体上；生成权重和深度带给刷鱼用。
    /// </summary>
    [CreateAssetMenu(menuName = "IceFishing/Fish Definition", fileName = "FishDefinition")]
    public sealed class FishDefinition : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public float MinDepthMeters;
        public float MaxDepthMeters;
        public int Stars = 1;
        public int SpawnWeight = 50;
        public float MoveSpeed = 1f;
        public float WorldHeight = 0.9f;
        public FishView Prefab;
        [Tooltip("回营地抛鱼结算时该条鱼兑换的纪念币")]
        public int CampCoinReward = 10;
        [Tooltip("回营地结算时该条鱼兑换的贝壳")]
        public int CampShellReward = 10;

        public int TokenValue
        {
            get { return CampCoinReward > 0 ? CampCoinReward : 10; }
        }

        public int ShellValue
        {
            get { return CampShellReward > 0 ? CampShellReward : 10; }
        }
    }
}
