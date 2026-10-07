using UnityEngine;
using UnityEngine.UI;

namespace IceFishing.View
{
    /// <summary>
    /// 全屏闪白（UI 最上层），用于保护挡鱼等反馈。
    /// </summary>
    public sealed class ScreenFlashView : MonoBehaviour
    {
        public const string NodeName = "ScreenFlash";

        const float FlashDuration = 0.38f;
        const float PeakAlpha = 0.46f;

        Image _image;
        float _remaining;

        public static ScreenFlashView Ensure(Transform canvasRoot)
        {
            if (canvasRoot == null)
            {
                return null;
            }

            var existing = canvasRoot.Find(NodeName);
            if (existing != null)
            {
                return existing.GetComponent<ScreenFlashView>();
            }

            var go = new GameObject(NodeName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScreenFlashView));
            go.transform.SetParent(canvasRoot, false);
            go.transform.SetAsLastSibling();
            return go.GetComponent<ScreenFlashView>();
        }

        void Awake()
        {
            _image = GetComponent<Image>();
            _image.raycastTarget = false;
            _image.color = Color.clear;
            Stretch(GetComponent<RectTransform>());
        }

        void Update()
        {
            if (_image == null)
            {
                return;
            }

            if (_remaining <= 0f)
            {
                _image.color = Color.clear;
                return;
            }

            _remaining -= Time.deltaTime;
            var u = Mathf.Clamp01(_remaining / FlashDuration);
            var fade = u * u;
            var ripple = 0.72f + 0.28f * Mathf.Sin(u * Mathf.PI * 1.6f);
            var alpha = fade * ripple * PeakAlpha;
            _image.color = new Color(1f, 1f, 1f, alpha);
        }

        public void PlayLightningFlash()
        {
            if (_image == null)
            {
                _image = GetComponent<Image>();
            }

            transform.SetAsLastSibling();
            _remaining = FlashDuration;
            _image.color = new Color(1f, 1f, 1f, PeakAlpha * 0.65f);
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
