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

            if (IsUnderFishingHud())
            {
                if (!Application.isPlaying)
                {
                    PlaceOnHud();
                }

                return;
            }

            HookVisualScale.Apply(transform, _sprite.sprite, _worldHeight);
            LayoutShield();
        }

        void PlaceOnHud()
        {
            var parentScale = transform.parent != null ? Mathf.Abs(transform.parent.lossyScale.y) : 1f;
            if (parentScale > 0.0001f && parentScale < 0.2f)
            {
                var camera = Camera.main;
                var ortho = camera != null && camera.orthographic
                    ? camera.orthographicSize
                    : CampField.WorldOrtho;
                var shownHeight = _worldHeight * (ortho / CampField.WorldOrtho);
                HookVisualScale.Apply(transform, _sprite.sprite, shownHeight);
                LayoutShield();
                return;
            }

            var hud = GetComponentInParent<FishingHudView>();
            var rect = hud != null ? hud.transform as RectTransform : null;
            var width = rect != null && rect.rect.width > 1f ? rect.rect.width : 1080f;
            var height = rect != null && rect.rect.height > 1f ? rect.rect.height : 2160f;
            var viewHeight = CampField.WorldOrtho * 2f;
            var viewWidth = viewHeight * (9f / 18f);
            var cameraY = CampField.WorldOrtho * (2f - 2f * 0.33f);
            var bottom = cameraY - CampField.WorldOrtho;
            var nx = hookDropX / viewWidth + 0.5f;
            var ny = (hookDropY - bottom) / viewHeight;
            transform.localPosition = new Vector3((nx - 0.5f) * width, (ny - 0.5f) * height, 0f);
            var spriteHeight = _sprite.sprite != null ? _sprite.sprite.bounds.size.y : 1f;
            if (spriteHeight < 0.001f)
            {
                spriteHeight = 1f;
            }

            var pixelHeight = height * (_worldHeight / viewHeight);
            var scale = pixelHeight / spriteHeight;
            transform.localScale = new Vector3(scale, scale, 1f);
            LayoutShield();
        }

        const float hookDropX = 0.08f;
        const float hookDropY = 6.5f;

        void LayoutShield()
        {
            var shield = transform.Find("HookShield");
            if (shield != null)
            {
                HookShieldSetup.ApplyLayout(transform, shield);
            }
        }

        bool IsUnderFishingHud()
        {
            var parent = transform.parent;
            while (parent != null)
            {
                if (parent.GetComponent<FishingHudView>() != null)
                {
                    return true;
                }

                parent = parent.parent;
            }

            return false;
        }
    }
}
