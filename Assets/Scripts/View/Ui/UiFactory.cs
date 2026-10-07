using UnityEngine;
using UnityEngine.UI;

namespace IceFishing.View
{
    /// <summary>
    /// 共用 UGUI 缩放、字体和按钮样式。界面层级来自预制体，不在这里生成。
    /// </summary>
    public static class UiFactory
    {
        public const float HudReferenceWidth = 1080f;
        public const float HudReferenceHeight = 2160f;
        public const float HudScalerMatch = 0.5f;

        static Font _font;

        public sealed class BuiltUi
        {
            public HubView Hub;
            public FishingHudView Hud;
            public PausePopupView Pause;
            public OverlayView Overlay;
            public SettleView Settle;
        }

        public static Font ResolveFont()
        {
            if (_font != null)
            {
                return _font;
            }

            _font = Font.CreateDynamicFontFromOSFont(
                new[]
                {
                    "Microsoft YaHei UI",
                    "Microsoft YaHei",
                    "微软雅黑",
                    "PingFang SC",
                    "Noto Sans CJK SC",
                    "Source Han Sans SC",
                    "Arial Unicode MS",
                    "Arial"
                },
                32);
            return _font;
        }

        public static BuiltUi Build(
            Transform canvas,
            HubView hub = null,
            FishingHudView hud = null,
            OverlayView overlay = null,
            PausePopupView pause = null)
        {
            var built = new BuiltUi();
            built.Hub = hub != null ? hub : HubView.InstantiateOn(canvas);
            if (built.Hub != null && built.Hub.transform.parent != canvas)
            {
                built.Hub.transform.SetParent(canvas, false);
            }

            built.Hud = hud != null ? hud : FishingHudView.InstantiateOn(canvas);
            if (built.Hud != null && built.Hud.transform.parent != canvas)
            {
                built.Hud.transform.SetParent(canvas, false);
            }

            built.Pause = pause != null ? pause : PausePopupView.InstantiateOn(canvas);
            if (built.Pause != null && built.Pause.transform.parent != canvas)
            {
                built.Pause.transform.SetParent(canvas, false);
            }

            built.Overlay = overlay != null ? overlay : OverlayView.InstantiateOn(canvas);
            if (built.Overlay != null && built.Overlay.transform.parent != canvas)
            {
                built.Overlay.transform.SetParent(canvas, false);
            }

            built.Settle = SettleView.InstantiateOn(canvas);
            if (built.Hud != null)
            {
                built.Hud.gameObject.SetActive(false);
            }

            if (built.Pause != null)
            {
                built.Pause.gameObject.SetActive(false);
            }

            if (built.Overlay != null)
            {
                built.Overlay.gameObject.SetActive(false);
            }

            if (built.Settle != null)
            {
                built.Settle.gameObject.SetActive(false);
            }

            if (built.Hub != null)
            {
                built.Hub.transform.SetSiblingIndex(0);
            }
            if (built.Hud != null)
            {
                built.Hud.transform.SetSiblingIndex(1);
            }

            if (built.Pause != null)
            {
                built.Pause.transform.SetSiblingIndex(2);
            }

            if (built.Overlay != null)
            {
                built.Overlay.transform.SetSiblingIndex(3);
            }

            if (built.Settle != null)
            {
                built.Settle.transform.SetSiblingIndex(4);
            }

            return built;
        }

        public static void StyleSpriteSwapButton(Button button, Sprite normal, Sprite pressed, bool sliced)
        {
            if (button == null)
            {
                return;
            }

            var image = button.targetGraphic as Image;
            if (image == null)
            {
                image = button.GetComponent<Image>();
            }

            if (image != null && normal != null)
            {
                image.sprite = normal;
                image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
                image.preserveAspect = false;
                image.color = Color.white;
            }

            button.transition = Selectable.Transition.SpriteSwap;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
            button.colors = colors;

            if (normal != null)
            {
                var sprites = button.spriteState;
                sprites.highlightedSprite = normal;
                sprites.selectedSprite = normal;
                sprites.pressedSprite = pressed != null ? pressed : normal;
                sprites.disabledSprite = normal;
                button.spriteState = sprites;
            }
        }

        public static void FitFixedLayout(RectTransform root, ref RectTransform layout, string layoutName, ref bool fitting)
        {
            if (root == null || fitting)
            {
                return;
            }

            fitting = true;
            try
            {
                var zero = Vector2.zero;
                var one = Vector2.one;
                var pivot = new Vector2(0.5f, 0.5f);
                if (root.anchorMin != zero)
                {
                    root.anchorMin = zero;
                }

                if (root.anchorMax != one)
                {
                    root.anchorMax = one;
                }

                if (root.pivot != pivot)
                {
                    root.pivot = pivot;
                }

                if (root.offsetMin != zero)
                {
                    root.offsetMin = zero;
                }

                if (root.offsetMax != zero)
                {
                    root.offsetMax = zero;
                }

                if (root.localScale != Vector3.one)
                {
                    root.localScale = Vector3.one;
                }

                if (layout == null)
                {
                    var found = root.Find(layoutName);
                    if (found != null)
                    {
                        layout = found as RectTransform;
                    }
                }

                if (layout == null)
                {
                    return;
                }

                var center = new Vector2(0.5f, 0.5f);
                if (layout.anchorMin != center)
                {
                    layout.anchorMin = center;
                }

                if (layout.anchorMax != center)
                {
                    layout.anchorMax = center;
                }

                if (layout.pivot != center)
                {
                    layout.pivot = center;
                }

                if (layout.anchoredPosition != Vector2.zero)
                {
                    layout.anchoredPosition = Vector2.zero;
                }

                var layoutSize = new Vector2(HudReferenceWidth, HudReferenceHeight);
                if (layout.sizeDelta != layoutSize)
                {
                    layout.sizeDelta = layoutSize;
                }

                var width = root.rect.width;
                var height = root.rect.height;
                var scale = 1f;
                if (width > 1f && height > 1f)
                {
                    scale = Mathf.Min(width / HudReferenceWidth, height / HudReferenceHeight);
                    if (scale < 0.1f)
                    {
                        scale = 1f;
                    }
                }

                var layoutScale = new Vector3(scale, scale, 1f);
                if (layout.localScale != layoutScale)
                {
                    layout.localScale = layoutScale;
                }
            }
            finally
            {
                fitting = false;
            }
        }

        public static void ApplyHudCanvasScaler(CanvasScaler scaler)
        {
            if (scaler == null)
            {
                return;
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(HudReferenceWidth, HudReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = HudScalerMatch;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
