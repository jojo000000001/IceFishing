using IceFishing.Model;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IceFishing.View
{
    /// <summary>营地界面层级。只搭控件，不写营地规则。</summary>
    public static class HubUiBuilder
    {
        public const string CampMapPath = "Assets/Art/Backgrounds/HubBackground_9x18.png";
        public const string CampLetterboxName = "CampLetterbox";
        public static readonly Color CampLetterboxColor = new Color(0.05f, 0.08f, 0.14f, 1f);

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

        private static void LayoutHubCurrencyChip(RectTransform row, Vector2 anchoredPosition)
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

        private static Sprite ResolveHudPillSprite()
        {
            return Resources.Load<Sprite>("UI/HudPill")
#if UNITY_EDITOR
                ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/HudPill.png")
#endif
                ;
        }

        private static void CreateHubCurrencyRow(RectTransform bar, string rowName, Sprite icon, string preview)
        {
            var row = UiFactory.CreatePanel(bar, rowName, FishingHudStatsDefinition.DefaultPillTint);
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

            var count = UiFactory.CreateText(row.transform, "Count", preview, 32, Color.white, TextAnchor.MiddleLeft);
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
            var root = UiFactory.CreatePanel(canvas, "HubView", Color.clear);
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
        /// 宽屏两侧（或竖屏上下）的补边，避免 9:18 背景 letterbox 后露出世界镜头。
        /// </summary>
        public static Image EnsureCampLetterboxFill(RectTransform root)
        {
            if (root == null)
            {
                return null;
            }

            var found = root.Find(CampLetterboxName);
            Image image;
            if (found == null)
            {
                var go = new GameObject(CampLetterboxName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                found = go.transform;
                found.SetParent(root, false);
                image = go.GetComponent<Image>();
            }
            else
            {
                image = found.GetComponent<Image>();
            }

            var rt = found as RectTransform;
            UiFactory.Stretch(rt);
            found.SetAsFirstSibling();
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

        public static Sprite LoadHubCampMap()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(CampMapPath);
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

            UiFactory.SizeScreenRoot(root as RectTransform);
            UiFactory.StripExtraCanvas(root.gameObject);
            var campMap = LoadHubCampMap();
            CreateHubCampBackdrop(root as RectTransform, campMap);
            var layout = UiFactory.CreateFixedLayout(root, "HubLayout");

            var title = UiFactory.CreateText(layout, "Title", "冰上钓鱼", 88, UiTheme.Text, TextAnchor.MiddleCenter);
            UiFactory.AnchorTop(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(980f, 110f));

            var subtitle = UiFactory.CreateText(layout, "Subtitle", "从冰洞下潜 · 复刻无尽冬日冰钓", 32, UiTheme.TextMuted, TextAnchor.MiddleCenter);
            UiFactory.AnchorTop(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(980f, 48f));

            var stats = UiFactory.CreatePanel(layout, "Stats", UiTheme.Panel);
            UiFactory.AnchorTop(stats.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -230f), new Vector2(920f, 96f));

            var bait = UiFactory.CreateText(stats.transform, "Bait", "饵料 8 / 10", 34, UiTheme.Text, TextAnchor.MiddleLeft);
            bait.rectTransform.anchorMin = new Vector2(0f, 0f);
            bait.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            bait.rectTransform.offsetMin = new Vector2(36f, 0f);
            bait.rectTransform.offsetMax = new Vector2(-12f, 0f);
            bait.gameObject.SetActive(false);

            var tokens = UiFactory.CreateText(stats.transform, "Tokens", "纪念币 0", 34, UiTheme.Text, TextAnchor.MiddleRight);
            tokens.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            tokens.rectTransform.anchorMax = new Vector2(1f, 1f);
            tokens.rectTransform.offsetMin = new Vector2(12f, 0f);
            tokens.rectTransform.offsetMax = new Vector2(-36f, 0f);
            tokens.gameObject.SetActive(false);

            EnsureHubCurrencyBar(layout);

            var regen = UiFactory.CreateText(layout, "Regen", "下次回复 15 秒", 28, UiTheme.TextMuted, TextAnchor.MiddleCenter);
            UiFactory.AnchorTop(regen.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -340f), new Vector2(920f, 40f));
            regen.gameObject.SetActive(false);

            var start = UiFactory.CreateSpriteButton(
                layout,
                "StartButton",
                "开始钓鱼",
                PauseUiSprites.HubStart,
                PauseUiSprites.HubStartPressed,
                new Vector2(720f, 136f));
            UiFactory.AnchorBottom(start.transform as RectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 272f), new Vector2(720f, 136f));

            var gear = UiFactory.CreateButton(layout, "GearButton", "渔具", UiTheme.SecondaryButton, new Vector2(280f, 96f));
            var collection = UiFactory.CreateButton(layout, "CollectionButton", "图鉴", UiTheme.SecondaryButton, new Vector2(280f, 96f));
            var settings = UiFactory.CreateButton(layout, "SettingsButton", "设置", UiTheme.SecondaryButton, new Vector2(280f, 96f));
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
        private static void PlaceBottomRow(RectTransform rt, float x, float heightFromBottom, float height)
        {
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(x, 88f);
            rt.sizeDelta = new Vector2(280f, height);
        }
    }
}
