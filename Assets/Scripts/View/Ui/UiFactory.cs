using UnityEngine;
using UnityEngine.UI;

namespace IceFishing.View
{
    /// <summary>
    /// 共用 UGUI 控件和 1080×2160 布局。各界面层级在对应的 *UiBuilder 里。
    /// 中文字体走系统字体，避免 TMP 默认字体缺字形。
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

        public static BuiltUi Build(Transform canvas, HubView hub = null, FishingHudView hud = null)
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

            built.Pause = PauseUiBuilder.Build(canvas);
            built.Overlay = OverlayUiBuilder.Build(canvas);
            built.Settle = SettleView.InstantiateOn(canvas);
            if (built.Hud != null)
            {
                built.Hud.gameObject.SetActive(false);
            }

            built.Pause.gameObject.SetActive(false);
            built.Overlay.gameObject.SetActive(false);
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

            built.Pause.transform.SetSiblingIndex(2);
            built.Overlay.transform.SetSiblingIndex(3);
            if (built.Settle != null)
            {
                built.Settle.transform.SetSiblingIndex(4);
            }

            return built;
        }

        public static Text CreateText(Transform parent, string name, string content, int size, Color color, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = EditorSafeFont();
            if (text.font == null)
            {
                text.font = ResolveFont();
            }
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static Image CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return image;
        }

        public static Button CreateButton(Transform parent, string name, string label, Color color, Vector2 size)
        {
            var image = CreatePanel(parent, name, color);
            var rt = image.rectTransform;
            rt.sizeDelta = size;

            var button = image.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            var text = CreateText(image.transform, "Label", label, 36, Color.white, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return button;
        }

        public static Button CreateSpriteButton(
            Transform parent,
            string name,
            string label,
            Sprite normal,
            Sprite pressed,
            Vector2 size)
        {
            var image = CreatePanel(parent, name, Color.white);
            image.raycastTarget = true;
            if (normal != null)
            {
                image.sprite = normal;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
            }

            var rt = image.rectTransform;
            rt.sizeDelta = size;

            var button = image.gameObject.AddComponent<Button>();
            StyleSpriteSwapButton(button, normal, pressed, false);

            var text = CreateText(image.transform, "Label", label, 40, Color.white, TextAnchor.MiddleCenter);
            AddOutline(text);
            Stretch(text.rectTransform);
            return button;
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


        static Font EditorSafeFont()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public static void SizeScreenRoot(RectTransform rt)
        {
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        public static RectTransform CreateFixedLayout(Transform root, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(root, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(HudReferenceWidth, HudReferenceHeight);
            rt.localScale = Vector3.one;
            return rt;
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

        public static void StripExtraCanvas(GameObject root)
        {
            var raycaster = root.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                Object.DestroyImmediate(raycaster);
            }

            var scaler = root.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                Object.DestroyImmediate(scaler);
            }

            var canvas = root.GetComponent<Canvas>();
            if (canvas != null)
            {
                Object.DestroyImmediate(canvas);
            }
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void AnchorTop(RectTransform rt, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        public static void AnchorBottom(RectTransform rt, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        public static void AddOutline(Text text)
        {
            if (text == null || text.GetComponent<Outline>() != null)
            {
                return;
            }

            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.6f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }









        public static Button CreateIconButton(Transform parent, string name, Sprite sprite, Vector2 size)
        {
            var image = CreatePanel(parent, name, sprite != null ? Color.white : UiTheme.PauseButton);
            image.raycastTarget = true;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
            }

            var rt = image.rectTransform;
            rt.sizeDelta = size;
            var button = image.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            return button;
        }


        public static void ClearChildren(Transform root)
        {
            if (root == null)
            {
                return;
            }

            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Object.Destroy(child);
                }
                else
                {
                    Object.DestroyImmediate(child);
                }
            }
        }

    }
}
