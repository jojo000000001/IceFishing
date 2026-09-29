using System;
using IceFishing.Model;
using UnityEngine;
using UnityEngine.UI;

namespace IceFishing.View
{
    /// <summary>
    /// 营地界面。只刷新显示与转发按钮事件，不消耗饵料、不改渔具。
    /// Prefab 预览和场景共用 1080×2160 HubLayout，按父画布等比缩放。
    /// </summary>
    [ExecuteAlways]
    public sealed class HubView : MonoBehaviour
    {
        [SerializeField] Text _baitText;
        [SerializeField] Text _tokenText;
        [SerializeField] Text _regenText;
        [SerializeField] Button _startButton;
        [SerializeField] Button _gearButton;
        [SerializeField] Button _collectionButton;
        [SerializeField] Button _settingsButton;
        [SerializeField] RectTransform _layout;
        [SerializeField] Image _campBackdrop;
        [SerializeField] Sprite _campMap;

        bool _wired;
        bool _fitting;

        public event Action StartClicked;
        public event Action GearClicked;
        public event Action CollectionClicked;
        public event Action SettingsClicked;

        public void Configure(
            Text baitText,
            Text tokenText,
            Text regenText,
            Button startButton,
            Button gearButton,
            Button collectionButton,
            Button settingsButton,
            RectTransform layout = null)
        {
            _baitText = baitText;
            _tokenText = tokenText;
            _regenText = regenText;
            _startButton = startButton;
            _gearButton = gearButton;
            _collectionButton = collectionButton;
            _settingsButton = settingsButton;
            _layout = layout;
            Wire();
            FitLayout();
        }

        void Awake()
        {
            Wire();
            EnsureCampBackdrop();
            FitLayout();
        }

        void OnEnable()
        {
            EnsureCampBackdrop();
            FitLayout();
        }

        void OnRectTransformDimensionsChange()
        {
            if (_fitting || !isActiveAndEnabled)
            {
                return;
            }

            FitLayout();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            SetFade(1f);
            SetInteractable(true);
            FitLayout();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            SetInteractable(true);
            SetFade(1f);
        }

        public void SetFade(float alpha)
        {
            var group = EnsureGroup();
            group.alpha = Mathf.Clamp01(alpha);
        }

        public void SetInteractable(bool interactable)
        {
            var group = EnsureGroup();
            group.interactable = interactable;
            group.blocksRaycasts = interactable;
        }

        CanvasGroup EnsureGroup()
        {
            var group = GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = gameObject.AddComponent<CanvasGroup>();
            }

            return group;
        }

        public void RefreshLayout()
        {
            FitLayout();
        }

        void FitLayout()
        {
            EnsureCampBackdrop();
            UiFactory.FitFixedLayout(transform as RectTransform, ref _layout, "HubLayout", ref _fitting);
            UiFactory.FitCampBackdrop(_campBackdrop, transform as RectTransform);
        }

        void EnsureCampBackdrop()
        {
            if (_campBackdrop == null)
            {
                var found = transform.Find("CampBackdrop");
                if (found != null)
                {
                    _campBackdrop = found.GetComponent<Image>();
                }
            }

            if (_campBackdrop == null)
            {
                _campBackdrop = UiFactory.CreateHubCampBackdrop(transform as RectTransform, _campMap);
            }
            else if (_campMap != null && _campBackdrop.sprite != _campMap)
            {
                UiFactory.ApplyHubCampBackdrop(_campBackdrop, _campMap);
            }
        }

        public void SetCampMap(Sprite campMap)
        {
            _campMap = campMap;
            EnsureCampBackdrop();
        }

        public void Refresh(PlayerProfile profile, float secondsToNext)
        {
            if (profile == null)
            {
                return;
            }

            ApplyFont();
            if (_baitText != null)
            {
                _baitText.text = "饵料 " + profile.DisplayBait + " / " + PlayerProfile.MaxBait;
            }

            if (_tokenText != null)
            {
                _tokenText.text = "纪念币 " + profile.Tokens;
            }

            if (_regenText != null)
            {
                if (profile.DisplayBait >= PlayerProfile.MaxBait)
                {
                    _regenText.text = "饵料已满";
                }
                else
                {
                    _regenText.text = "下次回复 " + Mathf.CeilToInt(secondsToNext) + " 秒";
                }
            }
        }

        void Wire()
        {
            if (_wired || _startButton == null)
            {
                return;
            }

            _startButton.onClick.AddListener(() => StartClicked?.Invoke());
            _gearButton.onClick.AddListener(() => GearClicked?.Invoke());
            _collectionButton.onClick.AddListener(() => CollectionClicked?.Invoke());
            _settingsButton.onClick.AddListener(() => SettingsClicked?.Invoke());
            _wired = true;
        }

        void ApplyFont()
        {
            var font = UiFactory.ResolveFont();
            SetFont(_baitText, font);
            SetFont(_tokenText, font);
            SetFont(_regenText, font);
        }

        static void SetFont(Text text, Font font)
        {
            if (text != null && text.font != font)
            {
                text.font = font;
            }
        }
    }
}
