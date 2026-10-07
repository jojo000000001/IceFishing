using UnityEngine;
using UnityEngine.UI;

namespace IceFishing.View
{
    /// <summary>营地背景与 letterbox。控件布局在 HubView 预制体里。</summary>
    public static class HubUiBuilder
    {
        public const string CampMapPath = "Assets/Art/Backgrounds/HubBackground_9x18.png";
        public const string CampLetterboxName = "CampLetterbox";
        public static readonly Color CampLetterboxColor = new Color(0.05f, 0.08f, 0.14f, 1f);

        public static void ApplyHubCampBackdrop(Image image, Sprite campMap)
        {
            if (image == null)
            {
                return;
            }

            if (campMap != null)
            {
                image.sprite = campMap;
            }

            image.preserveAspect = false;
            image.type = Image.Type.Simple;
            image.enabled = image.sprite != null;
            var fitter = image.GetComponent<AspectRatioFitter>();
            if (fitter != null)
            {
                fitter.enabled = false;
            }

            FitCampBackdrop(image, image.transform.parent as RectTransform);
        }

        /// <summary>
        /// 宽屏两侧（或竖屏上下）的补边，避免 9:18 背景 letterbox 后露出世界镜头。
        /// </summary>
        public static Image EnsureCampLetterboxFill(RectTransform root)
        {
            if (root == null)
            {
                return null;
            }

            var found = root.Find(CampLetterboxName);
            if (found == null)
            {
                return null;
            }

            var rt = found as RectTransform;
            UiFactory.Stretch(rt);
            found.SetAsFirstSibling();
            var image = found.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = false;
                image.color = CampLetterboxColor;
            }

            return image;
        }

        /// <summary>
        /// 背景与 HubLayout 同一套 1080×2160 letterbox，冰洞和人物不会因屏幕比例被裁偏。
        /// </summary>
        public static void FitCampBackdrop(Image image, RectTransform root)
        {
            if (image == null || root == null)
            {
                return;
            }

            var fitter = image.GetComponent<AspectRatioFitter>();
            if (fitter != null && fitter.enabled)
            {
                fitter.enabled = false;
            }

            var rt = image.rectTransform;
            var center = new Vector2(0.5f, 0.5f);
            if (rt.anchorMin != center)
            {
                rt.anchorMin = center;
            }

            if (rt.anchorMax != center)
            {
                rt.anchorMax = center;
            }

            if (rt.pivot != center)
            {
                rt.pivot = center;
            }

            if (rt.anchoredPosition != Vector2.zero)
            {
                rt.anchoredPosition = Vector2.zero;
            }

            var layoutSize = new Vector2(UiFactory.HudReferenceWidth, UiFactory.HudReferenceHeight);
            if (rt.sizeDelta != layoutSize)
            {
                rt.sizeDelta = layoutSize;
            }

            var parentSize = root.rect.size;
            var scale = 1f;
            if (parentSize.x > 1f && parentSize.y > 1f)
            {
                scale = Mathf.Min(parentSize.x / UiFactory.HudReferenceWidth, parentSize.y / UiFactory.HudReferenceHeight);
                if (scale < 0.1f)
                {
                    scale = 1f;
                }
            }

            var layoutScale = new Vector3(scale, scale, 1f);
            if (rt.localScale != layoutScale)
            {
                rt.localScale = layoutScale;
            }

            image.preserveAspect = false;
        }
    }
}
