using UnityEngine;
using UnityEngine.UI;

namespace IceFishing.View
{
    /// <summary>营地说明弹层层级。</summary>
    public static class OverlayUiBuilder
    {
        public static OverlayView Build(Transform canvas)
        {
            var dimmer = UiFactory.CreatePanel(canvas, "OverlayView", UiTheme.Dimmer);
            UiFactory.Stretch(dimmer.rectTransform);

            var panel = UiFactory.CreatePanel(dimmer.transform, "Panel", new Color(0.08f, 0.14f, 0.20f, 0.96f));
            panel.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            panel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            panel.rectTransform.sizeDelta = new Vector2(820f, 760f);

            var title = UiFactory.CreateText(panel.transform, "Title", "标题", 48, UiTheme.Text, TextAnchor.MiddleCenter);
            UiFactory.AnchorTop(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(740f, 72f));

            var body = UiFactory.CreateText(panel.transform, "Body", "内容", 32, UiTheme.TextMuted, TextAnchor.UpperLeft);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;
            body.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            body.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            body.rectTransform.pivot = new Vector2(0.5f, 1f);
            body.rectTransform.anchoredPosition = new Vector2(0f, -120f);
            body.rectTransform.sizeDelta = new Vector2(700f, 360f);

            var toggleRoot = UiFactory.CreatePanel(panel.transform, "InfiniteBait", Color.clear);
            toggleRoot.raycastTarget = false;
            toggleRoot.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            toggleRoot.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            toggleRoot.rectTransform.pivot = new Vector2(0.5f, 0f);
            toggleRoot.rectTransform.anchoredPosition = new Vector2(0f, 150f);
            toggleRoot.rectTransform.sizeDelta = new Vector2(640f, 56f);

            var toggleGo = new GameObject("Toggle", typeof(RectTransform));
            toggleGo.transform.SetParent(toggleRoot.transform, false);
            var toggleRt = toggleGo.GetComponent<RectTransform>();
            UiFactory.Stretch(toggleRt);
            var toggle = toggleGo.AddComponent<Toggle>();
            toggle.targetGraphic = toggleRoot;

            var box = UiFactory.CreatePanel(toggleGo.transform, "Box", new Color(0.18f, 0.28f, 0.36f));
            box.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            box.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            box.rectTransform.pivot = new Vector2(0f, 0.5f);
            box.rectTransform.anchoredPosition = Vector2.zero;
            box.rectTransform.sizeDelta = new Vector2(44f, 44f);

            var check = UiFactory.CreatePanel(box.transform, "Checkmark", UiTheme.StartButton);
            UiFactory.Stretch(check.rectTransform);
            check.rectTransform.offsetMin = new Vector2(8f, 8f);
            check.rectTransform.offsetMax = new Vector2(-8f, -8f);
            toggle.graphic = check;

            var toggleLabel = UiFactory.CreateText(toggleGo.transform, "Label", "无限饵料（验收）", 32, UiTheme.Text, TextAnchor.MiddleLeft);
            toggleLabel.rectTransform.anchorMin = new Vector2(0f, 0f);
            toggleLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
            toggleLabel.rectTransform.offsetMin = new Vector2(64f, 0f);
            toggleLabel.rectTransform.offsetMax = Vector2.zero;

            var close = UiFactory.CreateButton(panel.transform, "CloseButton", "关闭", UiTheme.SecondaryButton, new Vector2(360f, 88f));
            UiFactory.AnchorBottom(close.transform as RectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(360f, 88f));

            var overlay = dimmer.gameObject.AddComponent<OverlayView>();
            overlay.Configure(title, body, close, toggleRoot.gameObject, toggle);
            return overlay;
        }
    }
}
