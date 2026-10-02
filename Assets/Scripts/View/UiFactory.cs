using System.Collections.Generic;
using IceFishing.Model;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
        public const string HubCampMapPath = "Assets/Art/Backgrounds/HubBackground_9x18.png";

        static Font _font;

        public sealed class BuiltUi
        {
            public HubView Hub;
            public FishingHudView Hud;
            public PausePopupView Pause;
            public OverlayView Overlay;
            public SettleView Settle;
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
            built.Settle = BuildSettle(canvas);
            built.Hud.gameObject.SetActive(false);
            built.Pause.gameObject.SetActive(false);
            built.Overlay.gameObject.SetActive(false);
            built.Settle.gameObject.SetActive(false);
            built.Hub.transform.SetSiblingIndex(0);
            built.Hud.transform.SetSiblingIndex(1);
            built.Pause.transform.SetSiblingIndex(2);
            built.Overlay.transform.SetSiblingIndex(3);
            built.Settle.transform.SetSiblingIndex(4);
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
            StyleSettleActionButton(button, normal, pressed);

            var text = CreateText(image.transform, "Label", label, 40, Color.white, TextAnchor.MiddleCenter);
            AddOutline(text);
            Stretch(text.rectTransform);
            return button;
        }

        public static void StyleSettleActionButton(Button button, Sprite normal = null, Sprite pressed = null)
        {
            if (button == null)
            {
                return;
            }

            normal = normal ?? SettleUiSprites.ButtonNormal;
            pressed = pressed ?? SettleUiSprites.ButtonPressed;

            var image = button.targetGraphic as Image;
            if (image == null)
            {
                image = button.GetComponent<Image>();
            }

            if (image != null && normal != null)
            {
                image.sprite = normal;
                image.type = Image.Type.Simple;
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

        /// <summary>
        /// 仅绑定预制体层级上的引用，不改动 RectTransform / LayoutGroup（布局以预制体为准）。
        /// </summary>
        public static SettleView BindSettleFromHierarchy(Transform root)
        {
            if (root == null)
            {
                return null;
            }

            var layout = root.Find("SettleLayout") as RectTransform;
            var depth = layout != null ? layout.Find("Depth")?.GetComponent<Text>() : null;
            var grid = layout != null ? layout.Find("FishScroll/Viewport/Grid") as RectTransform : null;
            var empty = layout != null ? layout.Find("FishScroll/Empty")?.GetComponent<Text>() : null;
            var totalCoinText = layout != null ? layout.Find("Totals/TotalCoin/Value")?.GetComponent<Text>() : null;
            var totalShellText = layout != null ? layout.Find("Totals/TotalShell/Value")?.GetComponent<Text>() : null;
            var exit = layout != null ? layout.Find("ExitButton")?.GetComponent<Button>() : null;
            var cont = layout != null ? layout.Find("ContinueButton")?.GetComponent<Button>() : null;
            var speciesDropdown = layout != null ? layout.Find("SpeciesDropdown")?.GetComponent<Dropdown>() : null;
            var fishScroll = layout != null ? layout.Find("FishScroll")?.GetComponent<ScrollRect>() : null;

            var settle = root.GetComponent<SettleView>();
            if (settle == null)
            {
                settle = root.gameObject.AddComponent<SettleView>();
            }

            settle.Configure(
                depth,
                grid,
                empty,
                totalCoinText,
                totalShellText,
                exit,
                cont,
                layout,
                speciesDropdown,
                fishScroll);
            return settle;
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

        public static void EnsureHubCurrencyBar(RectTransform layout)
        {
            if (layout == null || layout.Find("CurrencyBar") != null)
            {
                return;
            }

            BuildHubCurrencyBar(layout);
        }

        public static void BuildHubCurrencyBar(RectTransform layout)
        {
            var barGo = new GameObject("CurrencyBar", typeof(RectTransform));
            var bar = barGo.GetComponent<RectTransform>();
            bar.SetParent(layout, false);
            CreateHubCurrencyRow(bar, "FishCoinRow", CampUiSprites.FishCoin, "0");
            CreateHubCurrencyRow(bar, "ShellRow", CampUiSprites.Shell, "0");
            ApplyHubCurrencyBarLayout(bar);
        }

        public static void ApplyHubCurrencyBarLayout(RectTransform bar)
        {
            if (bar == null)
            {
                return;
            }

            bar.anchorMin = new Vector2(1f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(1f, 1f);
            bar.anchoredPosition = new Vector2(-132f, -24f);
            bar.sizeDelta = new Vector2(452f, 64f);
            bar.localScale = Vector3.one;

            var coin = bar.Find("FishCoinRow") as RectTransform;
            var shell = bar.Find("ShellRow") as RectTransform;
            LayoutHubCurrencyChip(coin, new Vector2(-228f, 0f));
            LayoutHubCurrencyChip(shell, Vector2.zero);
        }

        static void LayoutHubCurrencyChip(RectTransform row, Vector2 anchoredPosition)
        {
            if (row == null)
            {
                return;
            }

            row.anchorMin = new Vector2(1f, 0.5f);
            row.anchorMax = new Vector2(1f, 0.5f);
            row.pivot = new Vector2(1f, 0.5f);
            row.anchoredPosition = anchoredPosition;
            row.sizeDelta = new Vector2(216f, 64f);
            row.localScale = Vector3.one;

            var image = row.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = false;
                image.color = FishingHudStatsDefinition.DefaultPillTint;
                if (image.sprite == null)
                {
                    image.sprite = ResolveHudPillSprite();
                }

                if (image.sprite != null)
                {
                    image.type = Image.Type.Sliced;
                    image.fillCenter = true;
                }
            }

            var icon = row.Find("Icon") as RectTransform;
            if (icon != null)
            {
                icon.anchorMin = new Vector2(0f, 0.5f);
                icon.anchorMax = new Vector2(0f, 0.5f);
                icon.pivot = new Vector2(0.5f, 0.5f);
                icon.anchoredPosition = new Vector2(34f, 0f);
                icon.sizeDelta = new Vector2(48f, 48f);
                icon.localScale = Vector3.one;
            }

            var count = row.Find("Count") as RectTransform;
            if (count != null)
            {
                count.anchorMin = Vector2.zero;
                count.anchorMax = Vector2.one;
                count.pivot = new Vector2(0.5f, 0.5f);
                count.anchoredPosition = Vector2.zero;
                count.offsetMin = new Vector2(66f, 0f);
                count.offsetMax = new Vector2(-16f, 0f);
                count.localScale = Vector3.one;
                var text = count.GetComponent<Text>();
                if (text != null)
                {
                    text.alignment = TextAnchor.MiddleLeft;
                    text.fontSize = 32;
                    text.horizontalOverflow = HorizontalWrapMode.Overflow;
                    text.verticalOverflow = VerticalWrapMode.Overflow;
                }
            }
        }

        static Sprite ResolveHudPillSprite()
        {
            return Resources.Load<Sprite>("UI/HudPill")
#if UNITY_EDITOR
                ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/HudPill.png")
#endif
                ;
        }

        static void CreateHubCurrencyRow(RectTransform bar, string rowName, Sprite icon, string preview)
        {
            var row = CreatePanel(bar, rowName, FishingHudStatsDefinition.DefaultPillTint);
            row.raycastTarget = false;
            var pill = ResolveHudPillSprite();
            if (pill != null)
            {
                row.sprite = pill;
                row.type = Image.Type.Sliced;
                row.fillCenter = true;
            }

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGo.transform.SetParent(row.transform, false);
            var image = iconGo.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.sprite = icon;
            image.color = Color.white;

            var count = CreateText(row.transform, "Count", preview, 32, Color.white, TextAnchor.MiddleLeft);
            var outline = count.gameObject.GetComponent<Outline>();
            if (outline == null)
            {
                outline = count.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = new Color(0f, 0f, 0f, 0.55f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
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
        /// 背景铺满父节点，多出来的边裁掉，不留黑边。
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

            var sprite = image.sprite;
            var aspect = sprite != null && sprite.rect.height > 1f
                ? sprite.rect.width / sprite.rect.height
                : HudReferenceWidth / HudReferenceHeight;
            var parentSize = root.rect.size;
            if (parentSize.x < 1f || parentSize.y < 1f)
            {
                return;
            }

            float width;
            float height;
            if (parentSize.x / parentSize.y > aspect)
            {
                width = parentSize.x;
                height = width / aspect;
            }
            else
            {
                height = parentSize.y;
                width = height * aspect;
            }

            var rt = image.rectTransform;
            var center = new Vector2(0.5f, 0.5f);
            rt.anchorMin = center;
            rt.anchorMax = center;
            rt.pivot = center;
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.sizeDelta = new Vector2(width, height);
            image.preserveAspect = false;
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
            bait.gameObject.SetActive(false);

            var tokens = CreateText(stats.transform, "Tokens", "纪念币 0", 34, UiTheme.Text, TextAnchor.MiddleRight);
            tokens.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            tokens.rectTransform.anchorMax = new Vector2(1f, 1f);
            tokens.rectTransform.offsetMin = new Vector2(12f, 0f);
            tokens.rectTransform.offsetMax = new Vector2(-36f, 0f);
            tokens.gameObject.SetActive(false);

            EnsureHubCurrencyBar(layout);

            var regen = CreateText(layout, "Regen", "下次回复 15 秒", 28, UiTheme.TextMuted, TextAnchor.MiddleCenter);
            AnchorTop(regen.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -340f), new Vector2(920f, 40f));
            regen.gameObject.SetActive(false);

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

        public static FishingHudView PopulateHud(
            Transform root,
            HudSprites sprites = default,
            FishingHudStatsDefinition statsProfile = null)
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

            statsProfile = statsProfile ?? ResolveHudStatsProfile();
            var statPills = new List<HudStatPillView>(3);
            if (statsProfile != null && statsProfile.Stats != null && statsProfile.Stats.Length > 0)
            {
                var sharedPill = statsProfile.SharedPill != null ? statsProfile.SharedPill : sprites.Pill;
                for (var i = 0; i < statsProfile.Stats.Length; i++)
                {
                    var definition = statsProfile.Stats[i];
                    if (definition == null)
                    {
                        continue;
                    }

                    statPills.Add(CreateStatPill(layout, definition, sharedPill));
                }
            }
            else
            {
                statPills.Add(CreateStatPill(
                    layout,
                    BuildFallbackStat(FishingHudStatKind.Protection, sprites.Protection, "0/5", -24f),
                    sprites.Pill));
                statPills.Add(CreateStatPill(
                    layout,
                    BuildFallbackStat(FishingHudStatKind.Haul, sprites.Haul, "2/2", -96f),
                    sprites.Pill));
                statPills.Add(CreateStatPill(
                    layout,
                    BuildFallbackStat(FishingHudStatKind.Depth, sprites.Depth, "0/150", -168f),
                    sprites.Pill));
            }

            var phase = CreateText(layout, "Phase", "下潜躲避", 32, UiTheme.Text, TextAnchor.MiddleCenter);
            AnchorTop(phase.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(420f, 48f));

            var hint = CreateText(layout, "Hint", "M1：点暂停可返回营地", 28, UiTheme.TextMuted, TextAnchor.MiddleCenter);
            AnchorBottom(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 48f), new Vector2(900f, 40f));

            var hud = root.GetComponent<FishingHudView>();
            if (hud == null)
            {
                hud = root.gameObject.AddComponent<FishingHudView>();
            }

            hud.Configure(phase, statPills.ToArray(), statsProfile, pause, layout);
            return hud;
        }

        static void ParsePreviewFraction(string preview, out float current, out float max)
        {
            current = 0f;
            max = 0f;
            if (string.IsNullOrEmpty(preview))
            {
                return;
            }

            var slash = preview.IndexOf('/');
            if (slash <= 0 || slash >= preview.Length - 1)
            {
                return;
            }

            float.TryParse(preview.Substring(0, slash), out current);
            float.TryParse(preview.Substring(slash + 1), out max);
        }

        static FishingHudStatDefinition BuildFallbackStat(
            FishingHudStatKind kind,
            Sprite icon,
            string preview,
            float topOffsetY)
        {
            var definition = ScriptableObject.CreateInstance<FishingHudStatDefinition>();
            definition.Kind = kind;
            definition.Icon = icon;
            ParsePreviewFraction(preview, out var previewCurrent, out var previewMax);
            definition.PreviewCurrent = previewCurrent;
            definition.MaxValue = previewMax;
            definition.TopOffsetY = topOffsetY;
            return definition;
        }

        public static FishingHudStatsDefinition ResolveHudStatsProfile()
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<FishingHudStatsDefinition>(FishingHudStatsDefinition.DefaultAssetPath);
#else
            return null;
#endif
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

        public static SettleView BuildSettle(Transform canvas)
        {
            var dimmer = CreatePanel(canvas, SettleView.NodeName, new Color(0.04f, 0.09f, 0.15f, 0.52f));
            Stretch(dimmer.rectTransform);
            return PopulateSettle(dimmer.transform);
        }

        public static CastTutorialView BuildCastTutorial(Transform canvas)
        {
            var root = CreatePanel(canvas, CastTutorialView.NodeName, Color.clear);
            Stretch(root.rectTransform);
            root.raycastTarget = false;
            return PopulateCastTutorial(root.transform);
        }

        public static CastTutorialView PopulateCastTutorial(Transform root)
        {
            var card = CreatePanel(root, "Card", Color.clear);
            card.raycastTarget = false;
            var cardRt = card.rectTransform;
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = new Vector2(0f, -36f);
            cardRt.sizeDelta = new Vector2(920f, 1320f);

            var window = CreatePanel(card.transform, "Window", Color.clear);
            window.raycastTarget = false;
            Stretch(window.rectTransform);
            window.rectTransform.offsetMin = new Vector2(36f, 36f);
            window.rectTransform.offsetMax = new Vector2(-36f, -36f);
            window.gameObject.AddComponent<RectMask2D>();

            var header = CreatePanel(card.transform, "Header", Color.white);
            header.raycastTarget = false;
            if (TutorialUiSprites.Header != null)
            {
                header.sprite = TutorialUiSprites.Header;
                header.type = Image.Type.Simple;
                header.preserveAspect = true;
            }

            var headerRt = header.rectTransform;
            headerRt.anchorMin = new Vector2(0.5f, 1f);
            headerRt.anchorMax = new Vector2(0.5f, 1f);
            headerRt.pivot = new Vector2(0.5f, 0.12f);
            headerRt.anchoredPosition = new Vector2(0f, 18f);
            headerRt.sizeDelta = new Vector2(980f, 280f);

            var frame = CreatePanel(card.transform, "Frame", Color.white);
            frame.raycastTarget = false;
            if (TutorialUiSprites.Frame != null)
            {
                frame.sprite = TutorialUiSprites.Frame;
                frame.type = Image.Type.Sliced;
                frame.fillCenter = false;
            }

            Stretch(frame.rectTransform);

            var bubblesL = CreatePanel(card.transform, "BubblesLeft", Color.white);
            bubblesL.raycastTarget = false;
            bubblesL.preserveAspect = true;
            if (TutorialUiSprites.Bubbles != null)
            {
                bubblesL.sprite = TutorialUiSprites.Bubbles;
            }

            var bubblesLRt = bubblesL.rectTransform;
            bubblesLRt.anchorMin = bubblesLRt.anchorMax = new Vector2(0f, 1f);
            bubblesLRt.pivot = new Vector2(0.5f, 0.5f);
            bubblesLRt.anchoredPosition = new Vector2(88f, -70f);
            bubblesLRt.sizeDelta = new Vector2(120f, 165f);

            var bubblesR = CreatePanel(card.transform, "BubblesRight", Color.white);
            bubblesR.raycastTarget = false;
            bubblesR.preserveAspect = true;
            if (TutorialUiSprites.Bubbles != null)
            {
                bubblesR.sprite = TutorialUiSprites.Bubbles;
            }

            var bubblesRRt = bubblesR.rectTransform;
            bubblesRRt.anchorMin = bubblesRRt.anchorMax = new Vector2(1f, 1f);
            bubblesRRt.pivot = new Vector2(0.5f, 0.5f);
            bubblesRRt.anchoredPosition = new Vector2(-70f, -96f);
            bubblesRRt.sizeDelta = new Vector2(110f, 150f);

            var close = CreateIconButton(card.transform, "CloseButton", TutorialUiSprites.Close, new Vector2(88f, 88f));
            var closeRt = close.transform as RectTransform;
            closeRt.anchorMin = closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(0.5f, 0.5f);
            closeRt.anchoredPosition = new Vector2(18f, 28f);

            var caption = CreateText(
                card.transform,
                "Caption",
                CastTutorialView.CaptionAvoidFish,
                34,
                Color.white,
                TextAnchor.MiddleCenter);
            caption.horizontalOverflow = HorizontalWrapMode.Wrap;
            caption.verticalOverflow = VerticalWrapMode.Overflow;
            AddOutline(caption);
            var captionRt = caption.rectTransform;
            captionRt.anchorMin = new Vector2(0f, 0f);
            captionRt.anchorMax = new Vector2(1f, 0f);
            captionRt.pivot = new Vector2(0.5f, 0f);
            captionRt.anchoredPosition = new Vector2(0f, 56f);
            captionRt.sizeDelta = new Vector2(-96f, 160f);

            var tutorial = root.GetComponent<CastTutorialView>();
            if (tutorial == null)
            {
                tutorial = root.gameObject.AddComponent<CastTutorialView>();
            }

            tutorial.Configure(cardRt, caption, close);
            return tutorial;
        }

        public static SettleView PopulateSettle(Transform root)
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

            Stretch(root as RectTransform);
            var layout = CreateFixedLayout(root, "SettleLayout");

            var title = CreateText(layout, "Title", "本次收获", 72, UiTheme.Text, TextAnchor.MiddleCenter);
            AnchorTop(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(920f, 96f));
            AddOutline(title);

            var depth = CreateText(layout, "Depth", "下潜深度：0米", 34, UiTheme.TextMuted, TextAnchor.MiddleCenter);
            AnchorTop(depth.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -168f), new Vector2(920f, 48f));
            AddOutline(depth);

            var speciesDropdown = CreateSimpleDropdown(layout, "SpeciesDropdown", "鱼种 1-9");
            var speciesRt = speciesDropdown.transform as RectTransform;
            speciesRt.anchorMin = new Vector2(0.5f, 1f);
            speciesRt.anchorMax = new Vector2(0.5f, 1f);
            speciesRt.pivot = new Vector2(0.5f, 1f);
            speciesRt.anchoredPosition = new Vector2(0f, -228f);
            speciesRt.sizeDelta = new Vector2(560f, 64f);
            speciesDropdown.gameObject.SetActive(false);

            var scroll = CreatePanel(layout, "FishScroll", Color.clear);
            scroll.raycastTarget = true;
            var scrollRt = scroll.rectTransform;
            scrollRt.anchorMin = new Vector2(0.5f, 0f);
            scrollRt.anchorMax = new Vector2(0.5f, 1f);
            scrollRt.pivot = new Vector2(0.5f, 0.5f);
            scrollRt.anchoredPosition = new Vector2(0f, 40f);
            scrollRt.sizeDelta = new Vector2(760f, -660f);

            var viewport = CreatePanel(scroll.transform, "Viewport", Color.clear);
            viewport.raycastTarget = true;
            Stretch(viewport.rectTransform);
            viewport.gameObject.AddComponent<RectMask2D>();

            var gridGo = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            var grid = gridGo.GetComponent<RectTransform>();
            grid.SetParent(viewport.transform, false);
            grid.anchorMin = new Vector2(0f, 1f);
            grid.anchorMax = new Vector2(1f, 1f);
            grid.pivot = new Vector2(0.5f, 1f);
            grid.anchoredPosition = Vector2.zero;
            grid.sizeDelta = new Vector2(0f, 0f);
            var gridLayout = gridGo.GetComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(228f, 266f);
            gridLayout.spacing = new Vector2(14f, 16f);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 3;
            gridLayout.childAlignment = TextAnchor.UpperCenter;
            gridLayout.padding = new RectOffset(8, 8, 6, 16);
            var fitter = gridGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport.rectTransform;
            scrollRect.content = grid;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.scrollSensitivity = 48f;

            var empty = CreateText(scroll.transform, "Empty", "没有钓到鱼", 36, UiTheme.TextMuted, TextAnchor.MiddleCenter);
            Stretch(empty.rectTransform);
            empty.gameObject.SetActive(false);

            var totals = CreatePanel(layout, "Totals", Color.clear);
            totals.raycastTarget = false;
            var totalsRt = totals.rectTransform;
            totalsRt.anchorMin = totalsRt.anchorMax = new Vector2(0.5f, 0f);
            totalsRt.pivot = new Vector2(0.5f, 0f);
            totalsRt.anchoredPosition = new Vector2(0f, 236f);
            totalsRt.sizeDelta = new Vector2(780f, 88f);

            var totalCoinText = CreateSettleTotalChip(totalsRt, "TotalCoin", CampUiSprites.FishCoin, -190f);
            var totalShellText = CreateSettleTotalChip(totalsRt, "TotalShell", CampUiSprites.Shell, 190f);

            var exit = CreateSpriteButton(
                layout,
                "ExitButton",
                "退出",
                SettleUiSprites.ButtonNormal,
                SettleUiSprites.ButtonPressed,
                new Vector2(360f, 108f));
            var cont = CreateSpriteButton(
                layout,
                "ContinueButton",
                "继续体验",
                SettleUiSprites.ButtonNormal,
                SettleUiSprites.ButtonPressed,
                new Vector2(360f, 108f));
            AnchorBottom(exit.transform as RectTransform, new Vector2(0.5f, 0f), new Vector2(-210f, 88f), new Vector2(360f, 108f));
            AnchorBottom(cont.transform as RectTransform, new Vector2(0.5f, 0f), new Vector2(210f, 88f), new Vector2(360f, 108f));

            var settle = root.GetComponent<SettleView>();
            if (settle == null)
            {
                settle = root.gameObject.AddComponent<SettleView>();
            }

            settle.Configure(
                depth,
                grid,
                empty,
                totalCoinText,
                totalShellText,
                exit,
                cont,
                layout,
                speciesDropdown,
                scrollRect);
            layout.localScale = Vector3.one;
            return settle;
        }

        static Text CreateSettleTotalChip(RectTransform parent, string name, Sprite icon, float x)
        {
            var rootGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var root = rootGo.GetComponent<RectTransform>();
            root.SetParent(parent, false);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = new Vector2(x, 0f);
            root.sizeDelta = new Vector2(300f, 80f);
            var frame = rootGo.GetComponent<Image>();
            frame.raycastTarget = false;
            frame.color = Color.white;
            if (SettleUiSprites.TotalFrame != null)
            {
                frame.sprite = SettleUiSprites.TotalFrame;
                frame.type = Image.Type.Sliced;
            }
            else
            {
                frame.color = new Color(0.08f, 0.18f, 0.28f, 0.85f);
            }

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGo.transform.SetParent(root, false);
            var image = iconGo.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.sprite = icon;
            image.color = Color.white;
            var iconRt = image.rectTransform;
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.anchoredPosition = new Vector2(16f, 0f);
            iconRt.sizeDelta = new Vector2(52f, 52f);

            var value = CreateText(root, "Value", "0", 42, UiTheme.Text, TextAnchor.MiddleLeft);
            AddOutline(value);
            var valueRt = value.rectTransform;
            valueRt.anchorMin = new Vector2(0f, 0f);
            valueRt.anchorMax = new Vector2(1f, 1f);
            valueRt.offsetMin = new Vector2(78f, 0f);
            valueRt.offsetMax = new Vector2(-16f, 0f);
            return value;
        }

        static void AddOutline(Text text)
        {
            if (text == null || text.GetComponent<Outline>() != null)
            {
                return;
            }

            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.6f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        public static Dropdown CreateSimpleDropdown(Transform parent, string name, string caption)
        {
            var root = CreatePanel(parent, name, UiTheme.SecondaryButton);
            var rootRt = root.rectTransform;
            Stretch(rootRt);
            root.raycastTarget = true;

            var label = CreateText(root.transform, "Label", caption, 30, UiTheme.Text, TextAnchor.MiddleLeft);
            var labelRt = label.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(24f, 0f);
            labelRt.offsetMax = new Vector2(-48f, 0f);

            var arrow = CreateText(root.transform, "Arrow", "▼", 28, UiTheme.TextMuted, TextAnchor.MiddleRight);
            var arrowRt = arrow.rectTransform;
            arrowRt.anchorMin = new Vector2(1f, 0f);
            arrowRt.anchorMax = new Vector2(1f, 1f);
            arrowRt.offsetMin = new Vector2(-44f, 0f);
            arrowRt.offsetMax = new Vector2(-12f, 0f);

            var template = CreatePanel(root.transform, "Template", new Color(0.08f, 0.14f, 0.20f, 0.98f));
            template.gameObject.SetActive(false);
            var templateRt = template.rectTransform;
            templateRt.anchorMin = new Vector2(0f, 0f);
            templateRt.anchorMax = new Vector2(1f, 0f);
            templateRt.pivot = new Vector2(0.5f, 1f);
            templateRt.anchoredPosition = new Vector2(0f, 2f);
            templateRt.sizeDelta = new Vector2(0f, 220f);

            var viewport = CreatePanel(template.transform, "Viewport", Color.clear);
            Stretch(viewport.rectTransform);
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewport.transform, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 56f);

            var item = CreatePanel(content, "Item", new Color(0.12f, 0.22f, 0.32f, 1f));
            var itemRt = item.rectTransform;
            itemRt.anchorMin = new Vector2(0f, 0.5f);
            itemRt.anchorMax = new Vector2(1f, 0.5f);
            itemRt.pivot = new Vector2(0.5f, 0.5f);
            itemRt.sizeDelta = new Vector2(0f, 52f);
            var toggle = item.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = item;
            toggle.isOn = true;

            var itemLabel = CreateText(item.transform, "Item Label", caption, 28, UiTheme.Text, TextAnchor.MiddleLeft);
            var itemLabelRt = itemLabel.rectTransform;
            itemLabelRt.anchorMin = Vector2.zero;
            itemLabelRt.anchorMax = Vector2.one;
            itemLabelRt.offsetMin = new Vector2(20f, 0f);
            itemLabelRt.offsetMax = new Vector2(-12f, 0f);
            toggle.graphic = item;

            var dropdown = root.gameObject.AddComponent<Dropdown>();
            dropdown.targetGraphic = root;
            dropdown.captionText = label;
            dropdown.itemText = itemLabel;
            dropdown.template = templateRt;
            return dropdown;
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

        static HudStatPillView CreateStatPill(Transform parent, FishingHudStatDefinition definition, Sprite sharedPill)
        {
            var pillSprite = definition != null && definition.PillOverride != null
                ? definition.PillOverride
                : sharedPill;
            var name = definition != null ? definition.Kind.ToString() : "Stat";
            var preview = definition != null ? definition.GetPreviewText() : "0/0";
            var topOffsetY = definition != null ? definition.TopOffsetY : -24f;
            var icon = definition != null ? definition.Icon : null;

            var pillTint = FishingHudStatsDefinition.DefaultPillTint;
            var bar = CreatePanel(parent, name, pillSprite != null ? pillTint : UiTheme.Panel);
            bar.raycastTarget = false;
            if (pillSprite != null)
            {
                bar.sprite = pillSprite;
                bar.type = Image.Type.Sliced;
                bar.fillCenter = true;
            }

            var rt = bar.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(24f, topOffsetY);
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

            var text = CreateText(bar.transform, "Value", preview, 32, Color.white, TextAnchor.MiddleLeft);
            var textRt = text.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.pivot = new Vector2(0.5f, 0.5f);
            textRt.anchoredPosition = Vector2.zero;
            textRt.offsetMin = new Vector2(70f, 0f);
            textRt.offsetMax = new Vector2(-18f, 0f);

            var pillView = bar.gameObject.AddComponent<HudStatPillView>();
            var profile = ResolveHudStatsProfile();
            var tint = profile != null ? profile.PillTint : FishingHudStatsDefinition.DefaultPillTint;
            pillView.Configure(definition, text, iconImage, bar);
            pillView.ApplyPresentation(pillSprite, tint);
            return pillView;
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
