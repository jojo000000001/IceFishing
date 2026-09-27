using UnityEngine;

namespace IceFishing.View
{
    /// <summary>
    /// 挂在 Hook 预制体上：编辑模式也按与运行时相同的世界高度缩放，避免 Scene 里过小。
    /// </summary>
    [ExecuteAlways]
    public sealed class FishingHookVisual : MonoBehaviour
    {
        [SerializeField] float _worldHeight = HookVisualScale.DefaultWorldHeight;
        [SerializeField] SpriteRenderer _sprite;

        void Reset()
        {
            _sprite = GetComponent<SpriteRenderer>();
        }

        void OnValidate()
        {
            Apply();
        }

        void OnEnable()
        {
            Apply();
        }

        public void Apply()
        {
            if (_sprite == null)
            {
                _sprite = GetComponent<SpriteRenderer>();
            }

            if (_sprite == null)
            {
                return;
            }

            HookVisualScale.Apply(transform, _sprite.sprite, _worldHeight);
            var shield = transform.Find("HookShield");
            if (shield != null)
            {
                HookShieldSetup.ApplyLayout(transform, shield);
            }
        }
    }
}
