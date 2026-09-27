using UnityEngine;

namespace IceFishing.Model
{
    /// <summary>
    /// 16 种鱼的列表。刷鱼只从这里抽，不扫盘。
    /// </summary>
    [CreateAssetMenu(menuName = "IceFishing/Fish Catalog", fileName = "FishCatalog")]
    public sealed class FishCatalog : ScriptableObject
    {
        public const string AssetPath = "Assets/Data/Fish/FishCatalog.asset";

        public FishDefinition[] Items;
    }
}
