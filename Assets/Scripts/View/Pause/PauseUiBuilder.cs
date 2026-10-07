using UnityEngine;
using UnityEngine.UI;

namespace IceFishing.View
{
    /// <summary>暂停弹窗层级。</summary>
    public static class PauseUiBuilder
    {
        public static PausePopupView Build(Transform canvas)
        {
            var dimmer = UiFactory.CreatePanel(canvas, "PausePopupView", new Color(0.02f, 0.07f, 0.12f, 0.62f));
            UiFactory.Stretch(dimmer.rectTransform);
            return Populate(dimmer.transform);
        }

        public static PausePopupView Populate(Transform root)
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

            var dimmer = root.GetComponent<Image>();
            if (dimmer != null)
            {
                dimmer.color = new Color(0.02f, 0.07f, 0.12f, 0.62f);
                dimmer.raycastTarget = true;
            }

            var panel = UiFactory.CreatePanel(root, "Panel", Color.white);
            panel.raycastTarget = true;
            if (PauseUiSprites.Panel != null)
            {
                panel.sprite = PauseUiSprites.Panel;
                panel.type = Image.Type.Sliced;
                panel.pixelsPerUnitMultiplier = 1.05f;
            }

            var panelRt = panel.rectTransform;
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(760f, 580f);

            var icon = UiFactory.CreatePanel(panel.transform, "Icon", Color.white);
            icon.raycastTarget = false;
            if (PauseUiSprites.Icon != null)
            {
                icon.sprite = PauseUiSprites.Icon;
                icon.preserveAspect = true;
            }

            var iconRt = icon.rectTransform;
            iconRt.anchorMin = new Vector2(0.5f, 1f);
            iconRt.anchorMax = new Vector2(0.5f, 1f);
            iconRt.pivot = new Vector2(0.5f, 1f);
            iconRt.anchoredPosition = new Vector2(0f, -36f);
            iconRt.sizeDelta = new Vector2(108f, 108f);

            var title = UiFactory.CreateText(panel.transform, "Title", "暂停", 58, UiTheme.Text, TextAnchor.MiddleCenter);
            UiFactory.AddOutline(title);
            UiFactory.AnchorTop(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -148f), new Vector2(640f, 72f));

            var subtitle = UiFactory.CreateText(panel.transform, "Subtitle", "钓鱼已暂停", 30, UiTheme.TextMuted, TextAnchor.MiddleCenter);
            UiFactory.AnchorTop(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -214f), new Vector2(640f, 40f));

            var resume = UiFactory.CreateSpriteButton(
                panel.transform,
                "ResumeButton",
                "继续",
                PauseUiSprites.Resume,
                PauseUiSprites.ResumePressed,
                new Vector2(560f, 108f));
            UiFactory.AnchorBottom(resume.transform as RectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 196f), new Vector2(560f, 108f));

            var retreat = UiFactory.CreateSpriteButton(
                panel.transform,
                "RetreatButton",
                "返回营地",
                PauseUiSprites.Retreat,
                PauseUiSprites.RetreatPressed,
                new Vector2(560f, 108f));
            UiFactory.AnchorBottom(retreat.transform as RectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(560f, 108f));

            var pause = root.GetComponent<PausePopupView>();
            if (pause == null)
            {
                pause = root.gameObject.AddComponent<PausePopupView>();
            }

            pause.Configure(resume, retreat);
            return pause;
        }
    }
}
