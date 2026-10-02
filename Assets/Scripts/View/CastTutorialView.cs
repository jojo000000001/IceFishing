using System;
using UnityEngine;
using UnityEngine.UI;

namespace IceFishing.View
{
    /// <summary>
    /// 首次开局教程卡：展示框、关闭钮和字幕。时间轴里可点右上角退出回营地。
    /// </summary>
    public sealed class CastTutorialView : MonoBehaviour
    {
        public const string NodeName = "CastTutorialView";

        public const string CaptionAvoidFish =
            "鱼钩下潜时长按屏幕可左右移动，下潜至极致前不要碰到鱼。";

        public const string CaptionAscendCatch =
            "鱼钩向上回收过程，尽量捕获珍稀鱼类确保收益最大化。";

        const string DefaultCaption = CaptionAvoidFish;

        [SerializeField] RectTransform _card;
        [SerializeField] RectTransform _window;
        [SerializeField] Text _captionText;
        [SerializeField] Button _closeButton;

        bool _wired;
        bool _visible;
        Action _onDismissed;
        Material _frostMat;
        Image _rootImage;
        Vector2 _restCardPos;
        bool _hasRestCardPos;
        static readonly int HoleId = Shader.PropertyToID("_Hole");

        public bool IsVisible
        {
            get { return _visible; }
        }

        public RectTransform Card
        {
            get { return _card; }
        }

        public RectTransform Window
        {
            get { return _window; }
        }

        public Text CaptionText
        {
            get { return _captionText; }
        }

        public Button CloseButton
        {
            get { return _closeButton; }
        }

        void Awake()
        {
            ResolveMissingRefs();
            Wire();
            EnsurePresentation();
            gameObject.SetActive(false);
        }

        public void Configure(RectTransform card, Text captionText, Button closeButton)
        {
            _card = card;
            _captionText = captionText;
            _closeButton = closeButton;
            ResolveMissingRefs();
            Wire();
        }

        public void Show(Action onDismissed, string caption = null)
        {
            ResolveMissingRefs();
            Wire();
            ApplyFont();
            _onDismissed = onDismissed;
            SetCloseVisible(true);
            SetCaption(string.IsNullOrEmpty(caption) ? DefaultCaption : caption);
            _visible = true;
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            EnsurePresentation();
        }

        /// <summary>时间轴引导：显示退出钮，点了就回营地。</summary>
        public void ShowTimeline(Action onDismissed)
        {
            ResolveMissingRefs();
            Wire();
            ApplyFont();
            _onDismissed = onDismissed;
            SetCloseVisible(true);
            SetCaption(CaptionAvoidFish);
            _visible = true;
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            EnsurePresentation();
            CaptureCardRestPose();
        }

        void LateUpdate()
        {
            if (!_visible)
            {
                return;
            }

            UpdateFrostHole();
        }

        void EnsurePresentation()
        {
            ResolveMissingRefs();
            _rootImage = GetComponent<Image>();
            if (_rootImage != null)
            {
                _rootImage.color = Color.white;
                _rootImage.raycastTarget = true;
                var shader = Shader.Find("IceFishing/TutorialFrost");
                if (shader != null)
                {
                    if (_frostMat == null || _frostMat.shader != shader)
                    {
                        _frostMat = new Material(shader);
                    }

                    _frostMat.SetColor("_Color", new Color(0.03f, 0.10f, 0.18f, 0.55f));
                    _rootImage.material = _frostMat;
                }
                else
                {
                    _rootImage.color = Color.clear;
                }
            }

            if (_card != null)
            {
                var frame = _card.Find("Frame")?.GetComponent<Image>();
                if (frame != null)
                {
                    frame.fillCenter = false;
                }
            }

            UpdateFrostHole();
        }

        void UpdateFrostHole()
        {
            if (_frostMat == null)
            {
                return;
            }

            var holeRt = _window != null ? _window : _card;
            var rootRt = transform as RectTransform;
            if (holeRt == null || rootRt == null)
            {
                return;
            }

            var uiCamera = UiCamera();
            var root = ScreenRect(rootRt, uiCamera);
            var hole = ScreenRect(holeRt, uiCamera);
            if (root.width < 1f || root.height < 1f)
            {
                return;
            }

            var xmin = Mathf.Clamp01((hole.xMin - root.xMin) / root.width);
            var xmax = Mathf.Clamp01((hole.xMax - root.xMin) / root.width);
            var ymin = Mathf.Clamp01((hole.yMin - root.yMin) / root.height);
            var ymax = Mathf.Clamp01((hole.yMax - root.yMin) / root.height);
            _frostMat.SetVector(HoleId, new Vector4(xmin, ymin, xmax, ymax));
        }

        public void SetCloseVisible(bool visible)
        {
            if (_closeButton != null)
            {
                _closeButton.gameObject.SetActive(visible);
            }
        }

        public void Hide()
        {
            _visible = false;
            _onDismissed = null;
            ResetCardPose();
            gameObject.SetActive(false);
        }

        public void CaptureCardRestPose()
        {
            ResolveMissingRefs();
            if (_card != null)
            {
                _restCardPos = _card.anchoredPosition;
                _hasRestCardPos = true;
            }
        }

        public void ResetCardPose()
        {
            if (_card != null && _hasRestCardPos)
            {
                _card.anchoredPosition = _restCardPos;
            }
        }

        /// <summary>按上浮进度 0～1 平移教程卡（与 10 米上升同步，避免一帧跳满）。</summary>
        public void ApplyAscendProgress(float progress01)
        {
            ResolveMissingRefs();
            if (_card == null)
            {
                return;
            }

            if (!_hasRestCardPos)
            {
                CaptureCardRestPose();
            }

            var hole = _window != null ? _window : _card;
            var holeHeight = hole != null ? hole.rect.height : Screen.height * 0.55f;
            var maxDy = Mathf.Max(72f, holeHeight * 0.14f);
            var dy = Mathf.Clamp01(progress01) * maxDy;
            _card.anchoredPosition = _restCardPos + new Vector2(0f, dy);
        }

        public void SetCaption(string caption)
        {
            if (_captionText != null)
            {
                _captionText.text = caption ?? string.Empty;
            }
        }

        /// <summary>
        /// 钩子下降挂钩点：方框中下，字幕上方留出挂鱼空间。
        /// </summary>
        public bool TryGetCatchHookScreen(out Vector2 screen)
        {
            screen = Vector2.zero;
            ResolveMissingRefs();
            var hole = _window != null ? _window : _card;
            if (hole == null)
            {
                return false;
            }

            var uiCamera = UiCamera();
            var window = ScreenRect(hole, uiCamera);
            var y = window.yMin + window.height * 0.36f;
            if (_captionText != null)
            {
                var caption = ScreenRect(_captionText.rectTransform, uiCamera);
                y = Mathf.Max(y, caption.yMax + window.height * 0.1f);
            }

            y = Mathf.Min(y, window.center.y - window.height * 0.04f);
            y = Mathf.Clamp(y, window.yMin + window.height * 0.2f, window.center.y);
            screen = new Vector2(window.center.x, y);
            return window.width > 1f && window.height > 1f;
        }

        /// <summary>上浮时钩子锚点：方框中上，挂鱼仍留在框内。</summary>
        public bool TryGetAscendHookAnchorScreen(out Vector2 screen)
        {
            screen = Vector2.zero;
            ResolveMissingRefs();
            var hole = _window != null ? _window : _card;
            if (hole == null)
            {
                return false;
            }

            var uiCamera = UiCamera();
            var window = ScreenRect(hole, uiCamera);
            var pad = Mathf.Max(28f, window.height * 0.06f);
            var y = Mathf.Lerp(window.center.y, window.yMax - pad, 0.38f);
            if (_captionText != null)
            {
                var caption = ScreenRect(_captionText.rectTransform, uiCamera);
                y = Mathf.Max(y, caption.yMax + window.height * 0.08f);
            }

            y = Mathf.Clamp(y, window.center.y + window.height * 0.02f, window.yMax - pad);
            screen = new Vector2(window.center.x, y);
            return window.width > 1f && window.height > 1f;
        }

        /// <summary>钩子必须落在此屏幕 Y 区间内，保证不出方框。</summary>
        public bool TryGetHookScreenYRange(out float minScreenY, out float maxScreenY)
        {
            minScreenY = 0f;
            maxScreenY = 0f;
            ResolveMissingRefs();
            var hole = _window != null ? _window : _card;
            if (hole == null)
            {
                return false;
            }

            var uiCamera = UiCamera();
            var window = ScreenRect(hole, uiCamera);
            var pad = Mathf.Max(28f, window.height * 0.05f);
            minScreenY = window.yMin + pad;
            maxScreenY = window.yMax - pad;
            if (_captionText != null)
            {
                var caption = ScreenRect(_captionText.rectTransform, uiCamera);
                minScreenY = Mathf.Max(minScreenY, caption.yMax + pad);
            }

            return maxScreenY > minScreenY + 8f;
        }

        Camera UiCamera()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                return canvas.worldCamera;
            }

            return null;
        }

        static Rect ScreenRect(RectTransform rt, Camera uiCamera)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            var min = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[0]);
            var max = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[2]);
            return Rect.MinMaxRect(
                Mathf.Min(min.x, max.x),
                Mathf.Min(min.y, max.y),
                Mathf.Max(min.x, max.x),
                Mathf.Max(min.y, max.y));
        }

        void ResolveMissingRefs()
        {
            if (_card == null)
            {
                _card = transform.Find("Card") as RectTransform;
            }

            if (_window == null && _card != null)
            {
                _window = _card.Find("Window") as RectTransform;
            }

            if (_captionText == null && _card != null)
            {
                _captionText = _card.Find("Caption")?.GetComponent<Text>();
            }

            if (_closeButton == null && _card != null)
            {
                _closeButton = _card.Find("CloseButton")?.GetComponent<Button>();
            }
        }

        void Wire()
        {
            if (_wired || _closeButton == null)
            {
                return;
            }

            _closeButton.onClick.AddListener(OnCloseClicked);
            _wired = true;
        }

        void OnCloseClicked()
        {
            if (!_visible)
            {
                return;
            }

            var callback = _onDismissed;
            Hide();
            callback?.Invoke();
        }

        void ApplyFont()
        {
            if (_captionText != null)
            {
                _captionText.font = UiFactory.ResolveFont();
            }
        }

        void OnDestroy()
        {
            if (_frostMat == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_frostMat);
            }
            else
            {
                DestroyImmediate(_frostMat);
            }

            _frostMat = null;
        }
    }
}
