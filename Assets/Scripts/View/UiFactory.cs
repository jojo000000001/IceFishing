using UnityEngine;
using UnityEngine.UI;

namespace IceFishing.View
{
    /// <summary>
    /// 运行时 / 编辑器共用的 UGUI 工厂。只创建控件，不写游戏规则。
    /// 中文字体走系统字体，避免 TMP 默认字体缺字形。
    /// </summary>
    public static class UiFactory
    {
        public const float HudReferenceWidth = 1080f;
        public const float HudReferenceHeight = 2160f;
        public const float HudScalerMatch = 0.5f;
        public const string HubCampMapPath = "Assets/Art/Backgrounds/HubBackground_9x18_632x1264.png";

        static Font _font;

        public sealed class BuiltUi
        {
            public HubView Hub;
            public FishingHudView Hud;
            public PausePopupView Pause;
            public OverlayView Overlay;
        }

        public struct HudSprites
        {
            public Sprite Pause;
            public Sprite Pill;
            public Sprite Haul;
            public Sprite Protection;
            public Sprite Depth;
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
            built.Hub = hub != null ? hub : BuildHub(canvas);
            if (built.Hub.transform.parent != canvas)
            {
                built.Hub.transform.SetParent(canvas, false);
            }

            built.Hud = hud != null ? hud : BuildHud(canvas);
            if (built.Hud.transform.parent != canvas)
            {
                built.Hud.transform.SetParent(canvas, false);
            }

            built.Pause = BuildPause(canvas);
            built.Overlay = BuildOverlay(canvas);
            built.Hud.gameObject.SetActive(false);
            built.Pause.gameObject.SetActive(false);
            built.Overlay.gameObject.SetActive(false);
            built.Hub.transform.SetSiblingIndex(0);
            built.Hud.transform.SetSiblingIndex(1);
            built.Pause.transform.SetSiblingIndex(2);
            built.Overlay.transform.SetSiblingIndex(3);
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

        static Font EditorSafeFont()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        static void SizeHudRoot(RectTransform rt)
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

        static RectTransform CreateFixedLayout(Transform root, string name)
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

        static void StripExtraCanvas(GameObject root)
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

        public static HubView BuildHub(Transform canvas)
        {
            var root = CreatePanel(canvas, "HubView", Color.clear);
            root.raycastTarget = false;
            return PopulateHub(root.transform);
        }

        public static Image CreateHubCampBackdrop(RectTransform root, Sprite campMap)
        {
            if (root == null)
            {
                return null;
            }

            var go = new GameObject("CampBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(AspectRatioFitter));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(root, false);
            rt.SetAsFirstSibling();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = Color.white;
            ApplyHubCampBackdrop(image, campMap);
            return image;
        }

        public static void ApplyHubCampBackdrop(Image image, Sprite campMap)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = campMap;
            image.preserveAspect = true;
            image.enabled = campMap != null;
            var fitter = image.GetComponent<AspectRatioFitter>();
            if (fitter == null)
            {
                fitter = image.gameObject.AddComponent<AspectRatioFitter>();
            }

            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = campMap != null && campMap.rect.height > 0.001f
                ? campMap.rect.width / campMap.rect.height
                : HudReferenceWidth / HudReferenceHeight;
        }

        public static Sprite LoadHubCampMap()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(HubCampMapPath);
#else
            return null;
#endif
        }

        public static HubView PopulateHub(Transform root)
        {
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

            SizeHudRoot(root as RectTransform);
            StripExtraCanvas(root.gameObject);
            var campMap = LoadHubCampMap();
            CreateHubCampBackdrop(root as RectTransform, campMap);
            var layout = CreateFixedLayout(root, "HubLayout");

            var title = CreateText(layout, "Title", "冰上钓鱼", 88, UiTheme.Text, TextAnchor.MiddleCenter);
            AnchorTop(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(980f, 110f));

            var subtitle = CreateText(layout, "Subtitle", "从冰洞下潜 · 复刻无尽冬日冰钓", 32, UiTheme.TextMuted, TextAnchor.MiddleCenter);
            AnchorTop(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(980f, 48f));

            var stats = CreatePanel(layout, "Stats", UiTheme.Panel);
            AnchorTop(stats.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -230f), new Vector2(920f, 96f));

            var bait = CreateText(stats.transform, "Bait", "饵料 8 / 10", 34, UiTheme.Text, TextAnchor.MiddleLeft);
            bait.rectTransform.anchorMin = new Vector2(0f, 0f);
            bait.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            bait.rectTransform.offsetMin = new Vector2(36f, 0f);
            bait.rectTransform.offsetMax = new Vector2(-12f, 0f);

            var tokens = CreateText(stats.transform, "Tokens", "纪念币 0", 34, UiTheme.Text, TextAnchor.MiddleRight);
            tokens.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            tokens.rectTransform.anchorMax = new Vector2(1f, 1f);
            tokens.rectTransform.offsetMin = new Vector2(12f, 0f);
            tokens.rectTransform.offsetMax = new Vector2(-36f, 0f);

            var regen = CreateText(layout, "Regen", "下次回复 15 秒", 28, UiTheme.TextMuted, TextAnchor.MiddleCenter);
            AnchorTop(regen.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -340f), new Vector2(920f, 40f));

            var start = CreateButton(layout, "StartButton", "开始钓鱼", UiTheme.StartButton, new Vector2(720f, 128f));
            AnchorBottom(start.transform as RectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 280f), new Vector2(720f, 128f));

            var gear = CreateButton(layout, "GearButton", "渔具", UiTheme.SecondaryButton, new Vector2(280f, 96f));
            var collection = CreateButton(layout, "CollectionButton", "图鉴", UiTheme.SecondaryButton, new Vector2(280f, 96f));
            var settings = CreateButton(layout, "SettingsButton", "设置", UiTheme.SecondaryButton, new Vector2(280f, 96f));
            PlaceBottomRow(gear.transform as RectTransform, -310f, 280f, 96f);
            PlaceBottomRow(collection.transform as RectTransform, 0f, 280f, 96f);
            PlaceBottomRow(settings.transform as RectTransform, 310f, 280f, 96f);

            var hub = root.GetComponent<HubView>();
            if (hub == null)
            {
                hub = root.gameObject.AddComponent<HubView>();
            }

            hub.Configure(bait, tokens, regen, start, gear, collection, settings, layout);
            hub.SetCampMap(campMap);
            layout.localScale = Vector3.one;
            return hub;
        }

        public static FishingHudView BuildHud(Transform canvas, HudSprites sprites = default)
        {
            var root = CreatePanel(canvas, "FishingHudView", Color.clear);
            root.raycastTarget = false;
            return PopulateHud(root.transform, sprites);
        }

        public static FishingHudView PopulateHud(Transform root, HudSprites sprites = default)
        {
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

            SizeHudRoot(root as RectTransform);
            StripExtraCanvas(root.gameObject);
            var layout = CreateFixedLayout(root, "HudLayout");

            var pause = CreateIconButton(layout, "PauseButton", sprites.Pause, new Vector2(96f, 96f));
            var pauseRt = pause.transform as RectTransform;
            pauseRt.anchorMin = new Vector2(1f, 1f);
            pauseRt.anchorMax = new Vector2(1f, 1f);
            pauseRt.pivot = new Vector2(1f, 1f);
            pauseRt.anchoredPosition = new Vector2(-24f, -24f);
            pauseRt.sizeDelta = new Vector2(96f, 96f);

            var protection = CreateStatPill(layout, "Protection", sprites.Pill, sprites.Haul, "2/2", -24f);
            var capacity = CreateStatPill(layout, "Capacity", sprites.Pill, sprites.Protection, "0/5", -96f);
            var depth = CreateStatPill(layout, "Depth", sprites.Pill, sprites.Depth, "0/150", -168f);

            var phase = CreateText(layout, "Phase", "下潜躲避", 32, UiTheme.Text, TextAnchor.MiddleCenter);
            AnchorTop(phase.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(420f, 48f));

            var hint = CreateText(layout, "Hint", "M1：点暂停可返回营地", 28, UiTheme.TextMuted, TextAnchor.MiddleCenter);
            AnchorBottom(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 48f), new Vector2(900f, 40f));

            var hud = root.GetComponent<FishingHudView>();
            if (hud == null)
            {
                hud = root.gameObject.AddComponent<FishingHudView>();
            }

            hud.Configure(phase, depth, protection, capacity, pause, layout);
            return hud;
        }

        static PausePopupView BuildPause(Transform canvas)
        {
            var dimmer = CreatePanel(canvas, "PausePopupView", UiTheme.Dimmer);
            Stretch(dimmer.rectTransform);

            var panel = CreatePanel(dimmer.transform, "Panel", new Color(0.08f, 0.14f, 0.20f, 0.96f));
            panel.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            panel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            panel.rectTransform.sizeDelta = new Vector2(720f, 520f);

            var title = CreateText(panel.transform, "Title", "暂停", 56, UiTheme.Text, TextAnchor.MiddleCenter);
            AnchorTop(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(640f, 80f));

            var resume = CreateButton(panel.transform, "ResumeButton", "继续", UiTheme.SecondaryButton, new Vector2(520f, 100f));
            AnchorBottom(resume.transform as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(520f, 100f));

            var retreat = CreateButton(panel.transform, "RetreatButton", "返回营地", UiTheme.DangerButton, new Vector2(520f, 100f));
            AnchorBottom(retreat.transform as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(520f, 100f));

            var pause = dimmer.gameObject.AddComponent<PausePopupView>();
            pause.Configure(resume, retreat);
            return pause;
        }

        static OverlayView BuildOverlay(Transform canvas)
        {
            var dimmer = CreatePanel(canvas, "OverlayView", UiTheme.Dimmer);
            Stretch(dimmer.rectTransform);

            var panel = CreatePanel(dimmer.transform, "Panel", new Color(0.08f, 0.14f, 0.20f, 0.96f));
            panel.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            panel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            panel.rectTransform.sizeDelta = new Vector2(820f, 760f);

            var title = CreateText(panel.transform, "Title", "标题", 48, UiTheme.Text, TextAnchor.MiddleCenter);
            AnchorTop(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(740f, 72f));

            var body = CreateText(panel.transform, "Body", "内容", 32, UiTheme.TextMuted, TextAnchor.UpperLeft);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;
            body.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            body.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            body.rectTransform.pivot = new Vector2(0.5f, 1f);
            body.rectTransform.anchoredPosition = new Vector2(0f, -120f);
            body.rectTransform.sizeDelta = new Vector2(700f, 360f);

            var toggleRoot = CreatePanel(panel.transform, "InfiniteBait", Color.clear);
            toggleRoot.raycastTarget = false;
            toggleRoot.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            toggleRoot.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            toggleRoot.rectTransform.pivot = new Vector2(0.5f, 0f);
            toggleRoot.rectTransform.anchoredPosition = new Vector2(0f, 150f);
            toggleRoot.rectTransform.sizeDelta = new Vector2(640f, 56f);

            var toggleGo = new GameObject("Toggle", typeof(RectTransform));
            toggleGo.transform.SetParent(toggleRoot.transform, false);
            var toggleRt = toggleGo.GetComponent<RectTransform>();
            Stretch(toggleRt);
            var toggle = toggleGo.AddComponent<Toggle>();
            toggle.targetGraphic = toggleRoot;

            var box = CreatePanel(toggleGo.transform, "Box", new Color(0.18f, 0.28f, 0.36f));
            box.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            box.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            box.rectTransform.pivot = new Vector2(0f, 0.5f);
            box.rectTransform.anchoredPosition = Vector2.zero;
            box.rectTransform.sizeDelta = new Vector2(44f, 44f);

            var check = CreatePanel(box.transform, "Checkmark", UiTheme.StartButton);
            Stretch(check.rectTransform);
            check.rectTransform.offsetMin = new Vector2(8f, 8f);
            check.rectTransform.offsetMax = new Vector2(-8f, -8f);
            toggle.graphic = check;

            var toggleLabel = CreateText(toggleGo.transform, "Label", "无限饵料（验收）", 32, UiTheme.Text, TextAnchor.MiddleLeft);
            toggleLabel.rectTransform.anchorMin = new Vector2(0f, 0f);
            toggleLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
            toggleLabel.rectTransform.offsetMin = new Vector2(64f, 0f);
            toggleLabel.rectTransform.offsetMax = Vector2.zero;

            var close = CreateButton(panel.transform, "CloseButton", "关闭", UiTheme.SecondaryButton, new Vector2(360f, 88f));
            AnchorBottom(close.transform as RectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(360f, 88f));

            var overlay = dimmer.gameObject.AddComponent<OverlayView>();
            overlay.Configure(title, body, close, toggleRoot.gameObject, toggle);
            return overlay;
        }

        static Button CreateIconButton(Transform parent, string name, Sprite sprite, Vector2 size)
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

        static Text CreateStatPill(Transform parent, string name, Sprite pill, Sprite icon, string value, float y)
        {
            var bar = CreatePanel(parent, name, pill != null ? Color.white : UiTheme.Panel);
            bar.raycastTarget = false;
            if (pill != null)
            {
                bar.sprite = pill;
                bar.type = Image.Type.Sliced;
                bar.fillCenter = true;
            }

            var rt = bar.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(24f, y);
            rt.sizeDelta = new Vector2(268f, 64f);

            var iconImage = CreatePanel(bar.transform, "Icon", Color.white);
            iconImage.raycastTarget = false;
            if (icon != null)
            {
                iconImage.sprite = icon;
                iconImage.type = Image.Type.Simple;
                iconImage.preserveAspect = true;
            }
            else
            {
                iconImage.color = new Color(0.55f, 0.7f, 0.82f, 0.9f);
            }

            var iconRt = iconImage.rectTransform;
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = new Vector2(34f, 0f);
            iconRt.sizeDelta = new Vector2(52f, 52f);

            var text = CreateText(bar.transform, "Value", value, 32, Color.white, TextAnchor.MiddleLeft);
            var textRt = text.rectTransform;
            textRt.anchorMin = new Vector2(0f, 0f);
            textRt.anchorMax = new Vector2(1f, 1f);
            textRt.offsetMin = new Vector2(70f, 0f);
            textRt.offsetMax = new Vector2(-18f, 0f);
            return text;
        }

        static void PlaceBottomRow(RectTransform rt, float x, float heightFromBottom, float height)
        {
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(x, 88f);
            rt.sizeDelta = new Vector2(280f, height);
        }
    }
}
