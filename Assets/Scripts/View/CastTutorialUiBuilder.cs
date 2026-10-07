using UnityEngine;
using UnityEngine.UI;

namespace IceFishing.View
{
    /// <summary>抛竿教程卡片层级。</summary>
    public static class CastTutorialUiBuilder
    {
        public static CastTutorialView Build(Transform canvas)
        {
            var root = UiFactory.CreatePanel(canvas, CastTutorialView.NodeName, Color.clear);
            UiFactory.Stretch(root.rectTransform);
            root.raycastTarget = false;
            return Populate(root.transform);
        }

        public static CastTutorialView Populate(Transform root)
        {
            var card = UiFactory.CreatePanel(root, "Card", Color.clear);
            card.raycastTarget = false;
            var cardRt = card.rectTransform;
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = new Vector2(0f, -36f);
            cardRt.sizeDelta = new Vector2(920f, 1320f);

            var window = UiFactory.CreatePanel(card.transform, "Window", Color.clear);
            window.raycastTarget = false;
            UiFactory.Stretch(window.rectTransform);
            window.rectTransform.offsetMin = new Vector2(36f, 36f);
            window.rectTransform.offsetMax = new Vector2(-36f, -36f);
            window.gameObject.AddComponent<RectMask2D>();

            var header = UiFactory.CreatePanel(card.transform, "Header", Color.white);
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

            var frame = UiFactory.CreatePanel(card.transform, "Frame", Color.white);
            frame.raycastTarget = false;
            if (TutorialUiSprites.Frame != null)
            {
                frame.sprite = TutorialUiSprites.Frame;
                frame.type = Image.Type.Sliced;
                frame.fillCenter = false;
            }

            UiFactory.Stretch(frame.rectTransform);

            var bubblesL = UiFactory.CreatePanel(card.transform, "BubblesLeft", Color.white);
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

            var bubblesR = UiFactory.CreatePanel(card.transform, "BubblesRight", Color.white);
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

            var close = UiFactory.CreateIconButton(card.transform, "CloseButton", TutorialUiSprites.Close, new Vector2(88f, 88f));
            var closeRt = close.transform as RectTransform;
            closeRt.anchorMin = closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(0.5f, 0.5f);
            closeRt.anchoredPosition = new Vector2(18f, 28f);

            var caption = UiFactory.CreateText(
                card.transform,
                "Caption",
                CastTutorialView.CaptionAvoidFish,
                34,
                Color.white,
                TextAnchor.MiddleCenter);
            caption.horizontalOverflow = HorizontalWrapMode.Wrap;
            caption.verticalOverflow = VerticalWrapMode.Overflow;
            UiFactory.AddOutline(caption);
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
    }
}
