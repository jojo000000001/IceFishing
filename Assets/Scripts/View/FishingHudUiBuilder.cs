using System.Collections.Generic;
using IceFishing.Model;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IceFishing.View
{
    /// <summary>钓鱼 HUD 层级。只搭控件，不推进钓鱼会话。</summary>
    public static class FishingHudUiBuilder
    {
        public static FishingHudView BuildHud(Transform canvas, UiFactory.HudSprites sprites = default)
        {
            var root = UiFactory.CreatePanel(canvas, "FishingHudView", Color.clear);
            root.raycastTarget = false;
            return PopulateHud(root.transform, sprites);
        }

        public static FishingHudView PopulateHud(
            Transform root,
            UiFactory.HudSprites sprites = default,
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

            UiFactory.SizeScreenRoot(root as RectTransform);
            UiFactory.StripExtraCanvas(root.gameObject);
            var layout = UiFactory.CreateFixedLayout(root, "HudLayout");

            var pause = UiFactory.CreateIconButton(layout, "PauseButton", sprites.Pause, new Vector2(96f, 96f));
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

            var phase = UiFactory.CreateText(layout, "Phase", "下潜躲避", 32, UiTheme.Text, TextAnchor.MiddleCenter);
            UiFactory.AnchorTop(phase.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(420f, 48f));

            var hint = UiFactory.CreateText(layout, "Hint", "M1：点暂停可返回营地", 28, UiTheme.TextMuted, TextAnchor.MiddleCenter);
            UiFactory.AnchorBottom(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 48f), new Vector2(900f, 40f));

            var hud = root.GetComponent<FishingHudView>();
            if (hud == null)
            {
                hud = root.gameObject.AddComponent<FishingHudView>();
            }

            hud.Configure(phase, statPills.ToArray(), statsProfile, pause, layout);
            return hud;
        }

        private static void ParsePreviewFraction(string preview, out float current, out float max)
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

        private static FishingHudStatDefinition BuildFallbackStat(
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
        private static HudStatPillView CreateStatPill(Transform parent, FishingHudStatDefinition definition, Sprite sharedPill)
        {
            var pillSprite = definition != null && definition.PillOverride != null
                ? definition.PillOverride
                : sharedPill;
            var name = definition != null ? definition.Kind.ToString() : "Stat";
            var preview = definition != null ? definition.GetPreviewText() : "0/0";
            var topOffsetY = definition != null ? definition.TopOffsetY : -24f;
            var icon = definition != null ? definition.Icon : null;

            var pillTint = FishingHudStatsDefinition.DefaultPillTint;
            var bar = UiFactory.CreatePanel(parent, name, pillSprite != null ? pillTint : UiTheme.Panel);
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

            var iconImage = UiFactory.CreatePanel(bar.transform, "Icon", Color.white);
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

            var text = UiFactory.CreateText(bar.transform, "Value", preview, 32, Color.white, TextAnchor.MiddleLeft);
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
    }
}
