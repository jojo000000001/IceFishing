using System;
using System.Collections;
using IceFishing.Model;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IceFishing.View
{
    /// <summary>
    /// 营地界面。只刷新显示与转发按钮事件，不消耗饵料、不改渔具。
    /// 布局来自预制体；Prefab 预览和场景共用 1080×2160 HubLayout。
    /// </summary>
    [ExecuteAlways]
    public sealed class HubView : MonoBehaviour
    {
        public const string NodeName = "HubView";
        public const string PrefabPath = "Assets/Prefabs/UI/HubView.prefab";
        public const string PrefabResourcePath = "UI/HubView";

        [SerializeField] Text _baitText;
        [SerializeField] Text _tokenText;
        [SerializeField] Text _fishCoinCountText;
        [SerializeField] Text _shellCountText;
        [SerializeField] Image _fishCoinIcon;
        [SerializeField] Text _regenText;
        [SerializeField] Button _startButton;
        [SerializeField] Button _gearButton;
        [SerializeField] Button _collectionButton;
        [SerializeField] Button _settingsButton;
        [SerializeField] RectTransform _layout;
        [SerializeField] Image _campBackdrop;
        [SerializeField] Sprite _campMap;
        [SerializeField] Sprite _fisherBody;
        [SerializeField] Sprite _fisherGrip;
        [SerializeField] RectTransform _gripBone;
        [SerializeField, Tooltip("人物脚的位置。X 0 左 1 右，Y 0 底 1 顶。")]
        Vector2 _fisherFoot = new Vector2(0.2f, 0.46f);
        [SerializeField, Range(0.15f, 0.7f), Tooltip("人物高度占画面的比例。")]
        float _fisherHeight = 0.34f;
        [SerializeField, Tooltip("握竿的手接在袖口的位置。X 0 左 1 右，Y 0 脚 1 头。")]
        Vector2 _gripOnBody = new Vector2(0.72f, 0.4f);
        [SerializeField, Range(0.15f, 2.5f), Tooltip("手的大小。改这个数值，场景里的手会马上跟着变。")]
        float _gripWidth = 0.95f;
        [SerializeField] Sprite _lineSprite;
        [SerializeField, Tooltip("鱼线接在鱼竿上的位置。X 0 左 1 右，Y 0 下 1 上。")]
        Vector2 _lineOnGrip = new Vector2(0.97f, 0.94f);
        [SerializeField, Range(0.2f, 4f), Tooltip("鱼线长度，相对身高。")]
        float _lineLength = 1.2f;
        [SerializeField, Range(0f, 0.8f), Tooltip("鱼线下垂时往旁边弯多少，相对身高。")]
        float _lineSag = 0.22f;
        [SerializeField, Range(0.02f, 0.25f), Tooltip("水下后来接上的鱼线粗细。")]
        float _underwaterLineWidth = 0.07f;

        bool _wired;
        bool _fitting;
        bool _casting;
        bool _sinking;
        bool _gripQueued;
        bool _useWorldLine;
        bool _lineHookVisible = true;
        float _lineGrow;
        float _sinkAmount;
        float _lineDrop;
        Vector3 _lineTip;
        bool _hasLineTip;

        public float UnderwaterLineWidth
        {
            get { return _underwaterLineWidth; }
        }

        public event Action StartClicked;
        public event Action GearClicked;
        public event Action CollectionClicked;
        public event Action SettingsClicked;

        public static HubView InstantiateOn(Transform canvas)
        {
            if (canvas == null)
            {
                return null;
            }

            var existing = canvas.Find(NodeName);
            if (existing != null)
            {
                return existing.GetComponent<HubView>();
            }

            var prefab = LoadPrefab();
            if (prefab == null)
            {
                Debug.LogError("HubView prefab missing. Expected " + PrefabPath);
                return null;
            }

            var instance = Instantiate(prefab, canvas, false);
            instance.name = NodeName;
            var rt = instance.GetComponent<RectTransform>();
            if (rt != null)
            {
                UiFactory.Stretch(rt);
            }

            return instance.GetComponent<HubView>();
        }

        public static GameObject LoadPrefab()
        {
#if UNITY_EDITOR
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (asset != null)
            {
                return asset;
            }
#endif
            return Resources.Load<GameObject>(PrefabResourcePath);
        }

        void Awake()
        {
            Wire();
            EnsureCampBackdrop();
            ApplyStartButtonArt();
            FitLayout();
        }

        void OnEnable()
        {
            EnsureCampBackdrop();
            ApplyStartButtonArt();
            FitLayout();
        }

        void OnValidate()
        {
            if (_casting || _gripQueued)
            {
                return;
            }

            _gripQueued = true;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null || _casting || !isActiveAndEnabled || Application.isPlaying)
                {
                    if (this != null)
                    {
                        _gripQueued = false;
                    }

                    return;
                }

                _gripQueued = false;
                ShowGrip(0f);
            };
#endif
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
            _casting = false;
            _sinking = false;
            _sinkAmount = 0f;
            _lineGrow = 1f;
            _lineDrop = 0f;
            _lineHookVisible = true;
            gameObject.SetActive(true);
            SetFade(1f);
            SetInteractable(true);
            ApplyStartButtonArt();
            SetSinkPixels(0f);
            FitLayout();
        }

        void ApplyStartButtonArt()
        {
            if (_startButton == null)
            {
                _startButton = transform.Find("HubLayout/StartButton")?.GetComponent<Button>();
            }

            UiFactory.StyleSpriteSwapButton(
                _startButton,
                PauseUiSprites.HubStart,
                PauseUiSprites.HubStartPressed,
                false);
        }

        public void SetLineHookVisible(bool visible)
        {
            _lineHookVisible = visible;
            if (_lineRoot == null)
            {
                return;
            }

            var hook = _lineRoot.Find("LineHook");
            if (hook == null)
            {
                return;
            }

            var image = hook.GetComponent<Image>();
            if (image != null)
            {
                image.enabled = visible;
            }
        }

        public void Hide()
        {
            _casting = false;
            gameObject.SetActive(false);
            SetInteractable(true);
            SetFade(1f);
        }

        public void ShowWorldBehind(bool show)
        {
            _useWorldLine = show;
            var image = GetComponent<Image>();
            if (image != null)
            {
                image.enabled = !show;
            }

            if (_campBackdrop != null)
            {
                _campBackdrop.enabled = !show;
            }

            var letterbox = transform.Find(HubUiBuilder.CampLetterboxName);
            if (letterbox != null)
            {
                var letterboxImage = letterbox.GetComponent<Image>();
                if (letterboxImage != null)
                {
                    letterboxImage.enabled = !show;
                }
            }

            var rig = FindFisherRig();
            var line = rig != null ? rig.Find("GripBone/Grip/FisherLine") : null;
            if (line != null)
            {
                line.gameObject.SetActive(!show);
            }
        }

        public bool TryGetRodTip(out Vector3 canvasWorld)
        {
            canvasWorld = default;
            if (_gripBone == null)
            {
                return false;
            }

            var grip = _gripBone.Find("Grip") as RectTransform;
            if (grip == null)
            {
                return false;
            }

            var local = new Vector3(
                (_lineOnGrip.x - grip.pivot.x) * grip.rect.width,
                (_lineOnGrip.y - grip.pivot.y) * grip.rect.height,
                0f);
            canvasWorld = grip.TransformPoint(local);
            return true;
        }

        public void DropLineAndHook(float pixels)
        {
            _lineDrop = Mathf.Max(0f, pixels);
        }

        public bool IsLineHookBelowScreen()
        {
            var hook = _lineRoot != null ? _lineRoot.Find("LineHook") as RectTransform : null;
            if (hook == null)
            {
                return false;
            }

            var canvas = GetComponentInParent<Canvas>();
            Camera uiCamera = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                uiCamera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            }

            var corners = new Vector3[4];
            hook.GetWorldCorners(corners);
            var top = float.MinValue;
            for (var i = 0; i < corners.Length; i++)
            {
                var screen = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[i]);
                if (screen.y > top)
                {
                    top = screen.y;
                }
            }

            return top < 0f;
        }

        public void SetDiveAmount(float t)
        {
            _sinkAmount = Mathf.Clamp01(t);
            _sinking = _sinkAmount > 0.001f;
            _lineGrow = 1f;
            if (_gripBone == null)
            {
                return;
            }

            var grip = _gripBone.Find("Grip") as RectTransform;
            var rig = _gripBone.parent as RectTransform;
            if (grip != null && rig != null)
            {
                PlaceLine(grip, rig);
            }
        }

        public void SetSinkPixels(float pixels)
        {
            var rt = transform as RectTransform;
            if (rt == null)
            {
                return;
            }

            _sinking = pixels > 1f;
            rt.offsetMin = new Vector2(0f, pixels);
            rt.offsetMax = new Vector2(0f, pixels);
        }

        public void MatchCameraDrop(float fromY, float nowY, float worldHeight)
        {
            var rt = transform as RectTransform;
            var screenHeight = rt != null && rt.rect.height > 1f ? rt.rect.height : 2160f;
            var pixels = worldHeight > 0.01f ? (fromY - nowY) / worldHeight * screenHeight : 0f;
            SetSinkPixels(pixels);
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

        /// <summary>
        /// 结算页盖住营地时：保留营地背景，隐藏 Hub 控件与人物，略放大并柔化背景。
        /// </summary>
        public void SetSettleOverlayMode(bool active)
        {
            EnsureCampBackdrop();
            SetFade(1f);
            SetInteractable(!active);

            if (_layout == null)
            {
                _layout = transform.Find("HubLayout") as RectTransform;
            }

            if (_layout != null)
            {
                var layoutGroup = _layout.GetComponent<CanvasGroup>();
                if (layoutGroup == null)
                {
                    layoutGroup = _layout.gameObject.AddComponent<CanvasGroup>();
                }

                layoutGroup.alpha = active ? 0f : 1f;
                layoutGroup.interactable = !active;
                layoutGroup.blocksRaycasts = !active;
            }

            var rig = FindFisherRig();
            if (rig != null)
            {
                rig.gameObject.SetActive(!active);
            }

            if (_campBackdrop != null)
            {
                _campBackdrop.rectTransform.localScale = active
                    ? new Vector3(1.05f, 1.05f, 1f)
                    : Vector3.one;
                _campBackdrop.color = active
                    ? new Color(0.62f, 0.68f, 0.78f, 1f)
                    : Color.white;
                HubUiBuilder.FitCampBackdrop(_campBackdrop, transform as RectTransform);
            }
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
            if (_sinking)
            {
                return;
            }
            EnsureCampBackdrop();
            HubUiBuilder.EnsureCampLetterboxFill(transform as RectTransform);
            UiFactory.FitFixedLayout(transform as RectTransform, ref _layout, "HubLayout", ref _fitting);
            EnsureCurrencyBar();
            HubUiBuilder.FitCampBackdrop(_campBackdrop, transform as RectTransform);
            OrderHubStage();
            FitFisherToStage(FindFisherRig());
            if (!_casting)
            {
                ShowGrip(0f);
            }
        }

        void EnsureCurrencyBar()
        {
            BindCurrencyRefs();
            if (_tokenText != null && _fishCoinCountText != null)
            {
                _tokenText.gameObject.SetActive(false);
            }
        }

        void BindCurrencyRefs()
        {
            if (_layout == null)
            {
                _layout = transform.Find("HubLayout") as RectTransform;
            }

            if (_layout == null)
            {
                return;
            }

            var bar = _layout.Find("CurrencyBar");
            if (bar == null)
            {
                return;
            }

            if (_fishCoinIcon == null)
            {
                _fishCoinIcon = bar.Find("FishCoinRow/Icon")?.GetComponent<Image>();
            }

            if (_fishCoinCountText == null)
            {
                _fishCoinCountText = bar.Find("FishCoinRow/Count")?.GetComponent<Text>();
            }

            if (_shellCountText == null)
            {
                _shellCountText = bar.Find("ShellRow/Count")?.GetComponent<Text>();
            }
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

            if (_campBackdrop != null && _campMap != null && _campBackdrop.sprite != _campMap)
            {
                HubUiBuilder.ApplyHubCampBackdrop(_campBackdrop, _campMap);
            }
        }

        public void SetCampMap(Sprite campMap)
        {
            _campMap = campMap;
            EnsureCampBackdrop();
        }

        /// <summary>结算鱼币飞向的锚点（右上角鱼币图标）。</summary>
        public RectTransform TokenLabelRect
        {
            get
            {
                if (_fishCoinIcon != null)
                {
                    return _fishCoinIcon.rectTransform;
                }

                return _tokenText != null ? _tokenText.rectTransform : null;
            }
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
                _baitText.gameObject.SetActive(false);
            }

            if (_fishCoinCountText != null)
            {
                _fishCoinCountText.text = profile.Tokens.ToString();
            }
            else if (_tokenText != null)
            {
                _tokenText.text = "纪念币 " + profile.Tokens;
            }

            if (_shellCountText != null)
            {
                _shellCountText.text = profile.Shells.ToString();
            }

            if (_regenText != null)
            {
                _regenText.gameObject.SetActive(false);
            }
        }

        void Update()
        {
            if (!Application.isPlaying || _casting || _gripBone == null)
            {
                return;
            }

            var rate = _sinking ? 0.85f : 0.32f;
            _lineGrow = Mathf.MoveTowards(_lineGrow, 1f, Time.deltaTime * rate);
            var grip = _gripBone.Find("Grip") as RectTransform;
            var rig = _gripBone.parent as RectTransform;
            if (grip != null && rig != null)
            {
                PlaceLine(grip, rig);
            }
        }

        public void PlayCast(Action onComplete)
        {
            if (_casting)
            {
                return;
            }

            EnsureGripSprites();
            if (_fisherBody == null || _fisherGrip == null)
            {
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(PlayGripCast(onComplete));
        }

        void ShowGrip(float angle)
        {
            EnsureGripSprites();
            if (_fisherBody == null || _fisherGrip == null)
            {
                return;
            }

            var rig = EnsureRig();
            if (rig == null)
            {
                return;
            }

            var body = EnsurePart(rig, "FisherBody");
            if (body == null)
            {
                return;
            }

            body.sprite = _fisherBody;
            body.enabled = true;
            Stretch(body.rectTransform);
            var bone = EnsureGripBone(rig);
            if (bone == null)
            {
                return;
            }

            bone.localEulerAngles = new Vector3(0f, 0f, angle);
            var grip = EnsurePart(bone, "Grip");
            if (grip == null)
            {
                return;
            }

            grip.sprite = _fisherGrip;
            grip.enabled = true;
            PlaceGrip(grip.rectTransform, rig);
            PlaceLine(grip.rectTransform, rig);
        }

        IEnumerator PlayGripCast(Action onComplete)
        {
            _casting = true;
            SetInteractable(false);
            yield return TurnGrip(0f, 28f, 0.45f);
            yield return TurnGrip(28f, -18f, 0.22f);
            onComplete?.Invoke();
            yield return TurnGrip(-18f, 0f, 0.35f);
            _casting = false;
        }

        IEnumerator TurnGrip(float from, float to, float seconds)
        {
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / seconds));
                ShowGrip(Mathf.Lerp(from, to, t));
                yield return null;
            }

            ShowGrip(to);
        }

        RectTransform FindFisherRig()
        {
            var found = transform.Find("FisherRig") as RectTransform;
            if (found == null && _layout != null)
            {
                found = _layout.Find("FisherRig") as RectTransform;
            }

            return found;
        }

        void OrderHubStage()
        {
            var letterbox = transform.Find(HubUiBuilder.CampLetterboxName);
            if (letterbox != null)
            {
                letterbox.SetAsFirstSibling();
            }

            if (_campBackdrop != null)
            {
                _campBackdrop.transform.SetSiblingIndex(letterbox != null ? 1 : 0);
            }
        }

        RectTransform EnsureRig()
        {
            var found = FindFisherRig();
            if (found == null)
            {
                return null;
            }

            var rig = found;
            if (rig.parent != transform)
            {
                rig.SetParent(transform, false);
            }

            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child == rig)
                {
                    continue;
                }

                if (child.name != "FisherRig" && child.name != "CastActor")
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            FitFisherToStage(rig);
            return rig;
        }

        void FitFisherToStage(RectTransform rig)
        {
            if (rig == null)
            {
                return;
            }

            var center = new Vector2(0.5f, 0.5f);
            rig.anchorMin = center;
            rig.anchorMax = center;
            rig.pivot = new Vector2(0.5f, 0.02f);
            var scale = 1f;
            if (_layout != null)
            {
                scale = _layout.localScale.x;
            }
            else
            {
                var parent = transform as RectTransform;
                if (parent != null && parent.rect.width > 1f && parent.rect.height > 1f)
                {
                    scale = Mathf.Min(
                        parent.rect.width / UiFactory.HudReferenceWidth,
                        parent.rect.height / UiFactory.HudReferenceHeight);
                    if (scale < 0.1f)
                    {
                        scale = 1f;
                    }
                }
            }

            rig.localScale = new Vector3(scale, scale, 1f);
            rig.anchoredPosition = new Vector2(
                (_fisherFoot.x - 0.5f) * UiFactory.HudReferenceWidth,
                (_fisherFoot.y - 0.5f) * UiFactory.HudReferenceHeight);
            var height = UiFactory.HudReferenceHeight * _fisherHeight;
            var aspect = _fisherBody != null && _fisherBody.rect.height > 1f
                ? _fisherBody.rect.width / _fisherBody.rect.height
                : 0.7f;
            rig.sizeDelta = new Vector2(height * aspect, height);
        }

        RectTransform EnsureGripBone(RectTransform rig)
        {
            if (_gripBone == null)
            {
                var found = rig.Find("GripBone");
                if (found != null)
                {
                    _gripBone = found as RectTransform;
                }
            }

            if (_gripBone == null)
            {
                return null;
            }

            _gripBone.anchorMin = _gripOnBody;
            _gripBone.anchorMax = _gripOnBody;
            _gripBone.pivot = new Vector2(0.5f, 0.5f);
            _gripBone.anchoredPosition = Vector2.zero;
            _gripBone.sizeDelta = Vector2.zero;
            _gripBone.SetAsLastSibling();
            return _gripBone;
        }

        static Image EnsurePart(RectTransform parent, string partName)
        {
            if (parent == null)
            {
                return null;
            }

            var found = parent.Find(partName);
            if (found == null)
            {
                return null;
            }

            var image = found.GetComponent<Image>();
            if (image == null)
            {
                return null;
            }

            image.raycastTarget = false;
            image.preserveAspect = true;
            return image;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        void PlaceGrip(RectTransform rt, RectTransform rig)
        {
            rt.pivot = new Vector2(0.16f, 0.48f);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = rt.anchorMin;
            rt.anchoredPosition = Vector2.zero;
            var height = rig.sizeDelta.y > 1f ? rig.sizeDelta.y : 400f;
            var aspect = _fisherGrip.rect.width / Mathf.Max(1f, _fisherGrip.rect.height);
            var width = height * _gripWidth;
            rt.sizeDelta = new Vector2(width, width / aspect);
        }

        void PlaceLine(RectTransform grip, RectTransform rig)
        {
            if (_useWorldLine)
            {
                var uiLine = grip.Find("FisherLine");
                if (uiLine != null)
                {
                    uiLine.gameObject.SetActive(false);
                }

                return;
            }
            var root = EnsureLineRoot(grip);
            if (root == null)
            {
                return;
            }

            var bend = _gripBone != null ? _gripBone.localEulerAngles.z : 0f;
            if (bend > 180f)
            {
                bend -= 360f;
            }

            root.localEulerAngles = new Vector3(0f, 0f, -bend);
            root.anchoredPosition = Vector2.zero;
            var bodyHeight = rig.sizeDelta.y > 1f ? rig.sizeDelta.y : 400f;
            var sway = Application.isPlaying ? Mathf.Sin(Time.time * 0.85f) * bodyHeight * 0.02f : 0f;
            var settle = bodyHeight * _lineLength;
            var diveExtra = bodyHeight * 1.05f * _sinkAmount;
            var grow = Application.isPlaying ? Mathf.SmoothStep(0f, 1f, _lineGrow) : 1f;
            var bendDrop = bodyHeight * 0.45f;
            var tail = Mathf.Lerp(0f, Mathf.Max(0f, settle - bendDrop) + diveExtra, grow) + _lineDrop;
            var bow = bodyHeight * _lineSag;
            var hangX = bow * 0.35f + sway * 0.25f;
            var p0 = Vector2.zero;
            var p1 = new Vector2(bow * 0.2f, -bendDrop * 0.28f);
            var p2 = new Vector2(hangX, -bendDrop);
            var p3 = new Vector2(hangX, -bendDrop - tail);
            const int count = 18;
            var thickness = Mathf.Max(2.2f, bodyHeight * 0.011f);
            var sprite = LineSprite();
            var oldTail = root.Find("UnderwaterLine");
            if (oldTail != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(oldTail.gameObject);
                }
                else
                {
                    DestroyImmediate(oldTail.gameObject);
                }
            }

            for (var i = 0; i < count; i++)
            {
                var a = Curve(p0, p1, p2, p3, i / (float)count);
                var b = Curve(p0, p1, p2, p3, (i + 1) / (float)count);
                var seg = EnsureSegment(root, i);
                if (seg == null)
                {
                    continue;
                }

                seg.color = Color.black;
                if (sprite != null && seg.sprite != sprite)
                {
                    seg.sprite = sprite;
                }

                var delta = b - a;
                var length = delta.magnitude + thickness * 0.45f;
                var rt = seg.rectTransform;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = rt.anchorMin;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = (a + b) * 0.5f;
                rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                rt.sizeDelta = new Vector2(length, thickness);
            }

            PlaceHookOnLine(root, p3, thickness);
            HideStaleLinePieces(root, count);
            _lineTip = root.TransformPoint(new Vector3(p3.x, p3.y, 0f));
            _hasLineTip = true;
        }

        void HideStaleLinePieces(RectTransform root, int count)
        {
            var seen = new bool[count];
            Transform keepHook = null;
            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name == "LineHook")
                {
                    keepHook = child;
                }
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name == "LineHook")
                {
                    child.gameObject.SetActive(child == keepHook);
                    continue;
                }

                if (!child.name.StartsWith("Seg"))
                {
                    continue;
                }

                int index;
                if (!int.TryParse(child.name.Substring(3), out index) || index < 0 || index >= count || seen[index])
                {
                    child.gameObject.SetActive(false);
                    continue;
                }

                seen[index] = true;
                child.gameObject.SetActive(true);
            }
        }

        void PlaceHookOnLine(RectTransform root, Vector2 tip, float thickness)
        {
            var found = root.Find("LineHook");
            if (found == null)
            {
                return;
            }

            var hook = found.GetComponent<Image>();
            if (hook == null)
            {
                return;
            }

            hook.raycastTarget = false;
            hook.preserveAspect = true;
            hook.color = Color.white;
            hook.enabled = _lineHookVisible;
            hook.gameObject.SetActive(true);
#if UNITY_EDITOR
            if (hook.sprite == null)
            {
                hook.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/FishingAnchor.png");
            }
#endif
            var rt = hook.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = rt.anchorMin;
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = tip;
            rt.localEulerAngles = Vector3.zero;
            var size = Mathf.Max(48f, thickness * 16f);
            rt.sizeDelta = new Vector2(size, size);
            hook.transform.SetAsLastSibling();
        }

        public bool TryGetLineTip(out Vector3 canvasWorld)
        {
            canvasWorld = _lineTip;
            return _hasLineTip;
        }

        public bool IsLineHookOutsideView()
        {
            var hook = _lineRoot != null ? _lineRoot.Find("LineHook") as RectTransform : null;
            if (hook == null)
            {
                return false;
            }

            var hub = transform as RectTransform;
            if (hub == null)
            {
                return false;
            }

            var canvas = GetComponentInParent<Canvas>();
            Camera uiCamera = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                uiCamera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            }

            var corners = new Vector3[4];
            hook.GetWorldCorners(corners);
            for (var i = 0; i < corners.Length; i++)
            {
                var screen = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[i]);
                if (RectTransformUtility.RectangleContainsScreenPoint(hub, screen, uiCamera))
                {
                    return false;
                }
            }

            return true;
        }

        RectTransform _lineRoot;

        RectTransform EnsureLineRoot(RectTransform grip)
        {
            var found = _lineRoot != null ? _lineRoot : grip.Find("FisherLine");
            if (found == null)
            {
                var canvas = GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    found = canvas.transform.Find("FisherLine");
                }
            }

            if (found == null)
            {
                return null;
            }

            var root = found as RectTransform;
            if (root == null)
            {
                return null;
            }

            var bar = found.GetComponent<Image>();
            if (bar != null)
            {
                bar.enabled = false;
            }

            _lineRoot = root;
            if (root.parent != grip)
            {
                root.SetParent(grip, false);
                root.anchorMin = _lineOnGrip;
                root.anchorMax = _lineOnGrip;
                root.pivot = new Vector2(0.5f, 0.5f);
                root.anchoredPosition = Vector2.zero;
                root.sizeDelta = Vector2.zero;
            }
            else
            {
                root.anchorMin = _lineOnGrip;
                root.anchorMax = _lineOnGrip;
                root.pivot = new Vector2(0.5f, 0.5f);
                root.anchoredPosition = Vector2.zero;
                root.sizeDelta = Vector2.zero;
            }

            root.gameObject.SetActive(true);
            for (var i = grip.childCount - 1; i >= 0; i--)
            {
                var child = grip.GetChild(i);
                if (child != root && child.name == "FisherLine")
                {
                    if (Application.isPlaying)
                    {
                        Destroy(child.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }
            }

            return root;
        }

        Image EnsureSegment(RectTransform root, int index)
        {
            var partName = "Seg" + index;
            var found = root.Find(partName);
            if (found == null)
            {
                return null;
            }

            var image = found.GetComponent<Image>();
            if (image == null)
            {
                return null;
            }

            image.raycastTarget = false;
            image.preserveAspect = false;
            image.color = Color.black;
            image.enabled = true;
            return image;
        }

        static Vector2 Curve(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            var u = 1f - t;
            return u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
        }

        Sprite LineSprite()
        {
#if UNITY_EDITOR
            if (_lineSprite == null)
            {
                _lineSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/FisherLine.png");
            }
#endif
            return _lineSprite;
        }

        void EnsureGripSprites()
        {
#if UNITY_EDITOR
            if (_fisherBody == null)
            {
                _fisherBody = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Cast/fisher_body_rig.png");
            }

            if (_fisherGrip == null)
            {
                _fisherGrip = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Cast/fisher_grip.png");
            }
#endif
        }

        void Wire()
        {
            if (_wired || _startButton == null)
            {
                return;
            }

            _startButton.onClick.AddListener(() => StartClicked?.Invoke());
            if (_gearButton != null)
            {
                _gearButton.onClick.AddListener(() => GearClicked?.Invoke());
            }

            if (_collectionButton != null)
            {
                _collectionButton.onClick.AddListener(() => CollectionClicked?.Invoke());
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(() => SettingsClicked?.Invoke());
            }

            _wired = true;
        }

        void ApplyFont()
        {
            var font = UiFactory.ResolveFont();
            SetFont(_baitText, font);
            SetFont(_tokenText, font);
            SetFont(_fishCoinCountText, font);
            SetFont(_shellCountText, font);
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
