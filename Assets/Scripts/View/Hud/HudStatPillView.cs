using IceFishing.Model;
using UnityEngine;
using UnityEngine.UI;

namespace IceFishing.View
{
    /// <summary>
    /// 单条 HUD 统计胶囊：展示由 FishingHudStatDefinition 驱动的图标与数值。
    /// </summary>
    [ExecuteAlways]
    public sealed class HudStatPillView : MonoBehaviour
    {
        [SerializeField] FishingHudStatDefinition _definition;
        [SerializeField] Text _valueText;
        [SerializeField] Image _iconImage;
        [SerializeField] Image _pillImage;

        public FishingHudStatDefinition Definition => _definition;

        public void SetDefinition(FishingHudStatDefinition definition)
        {
            _definition = definition;
        }

        public void ResolveReferences()
        {
            if (_valueText == null)
            {
                _valueText = transform.Find("Value")?.GetComponent<Text>();
            }

            if (_iconImage == null)
            {
                _iconImage = transform.Find("Icon")?.GetComponent<Image>();
            }

            if (_pillImage == null)
            {
                _pillImage = GetComponent<Image>();
            }
        }

        void OnValidate()
        {
            ApplyPreviewText();
        }

        public void ApplyPresentation(Sprite sharedPill, Color pillTint)
        {
            if (_definition == null)
            {
                return;
            }

            var pillSprite = _definition.PillOverride != null ? _definition.PillOverride : sharedPill;
            if (_pillImage != null)
            {
                if (pillSprite != null)
                {
                    _pillImage.sprite = pillSprite;
                    _pillImage.type = Image.Type.Sliced;
                    _pillImage.fillCenter = true;
                    _pillImage.color = pillTint;
                }
                else
                {
                    _pillImage.sprite = null;
                    _pillImage.color = UiTheme.Panel;
                }
            }

            if (_iconImage != null && _definition.Icon != null)
            {
                _iconImage.sprite = _definition.Icon;
                _iconImage.preserveAspect = true;
            }

            var rt = transform as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(24f, _definition.TopOffsetY);
            }

            ApplyPreviewText();
        }

        public void ApplyPreviewText()
        {
            if (_valueText != null && _definition != null)
            {
                _valueText.text = _definition.GetPreviewText();
            }
        }

        public void BindValue(CastSession session, Font font)
        {
            if (_valueText == null || _definition == null)
            {
                return;
            }

            if (font != null && _valueText.font != font)
            {
                _valueText.font = font;
            }

            _valueText.text = _definition.Format(session);
        }
    }
}
