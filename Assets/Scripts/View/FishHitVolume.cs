using UnityEngine;

namespace IceFishing.View
{
    public enum FishPart
    {
        Body = 0,
        Head = 1
    }

    /// <summary>
    /// 标在鱼头/鱼身上，给碰撞查询用。捕获语义由阶段决定，不写在这里。
    /// </summary>
    public sealed class FishHitVolume : MonoBehaviour
    {
        [SerializeField] FishPart _part;
        [SerializeField] FishView _fish;

        public FishPart Part
        {
            get { return _part; }
        }

        public FishView Fish
        {
            get { return _fish; }
        }

        public void EditorAssign(FishView fish, FishPart part)
        {
            _fish = fish;
            _part = part;
        }
    }
}
