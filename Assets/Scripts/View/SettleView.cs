using System;
using System.Collections.Generic;
using IceFishing.Model;
using UnityEngine;
using UnityEngine.UI;

namespace IceFishing.View
{
    /// <summary>
    /// 鍥炶惀缁撶畻椤碉細鏈眬娣卞害銆佹笖鑾风绫绘暟閲忋€侀奔甯?璐濆３鍚堣銆?    /// </summary>
    [ExecuteAlways]
    public sealed class SettleView : MonoBehaviour
    {
        public const string NodeName = "SettleView";
        public const int FishCardSlotCount = 6;
        static readonly Color CampFrostDimmer = new Color(0.04f, 0.09f, 0.15f, 0.52f);

        [SerializeField] Text _depthText;
        [SerializeField] RectTransform _grid;
        [SerializeField] Text _emptyText;
        [SerializeField] Text _totalCoinText;
        [SerializeField] Text _totalShellText;
        [SerializeField] Button _exitButton;
        [SerializeField] Button _continueButton;
        [SerializeField] RectTransform _layout;
        [SerializeField] Dropdown _speciesDropdown;
        [SerializeField] ScrollRect _fishScroll;
        [SerializeField] SettleFishCardSlot[] _fishCardSlots;

        bool _wired;
        bool _fitting;
        List<FishSettleRow> _rows = new List<FishSettleRow>();

        public event Action ExitClicked;
        public event Action ContinueClicked;

        public void Configure(
            Text depthText,
            RectTransform grid,
            Text emptyText,
            Text totalCoinText,
            Text totalShellText,
            Button exitButton,
            Button continueButton,
            RectTransform layout = null,
            Dropdown speciesDropdown = null,
            ScrollRect fishScroll = null)
        {
            _depthText = depthText;
            _grid = grid;
            _emptyText = emptyText;
            _totalCoinText = totalCoinText;
            _totalShellText = totalShellText;
            _exitButton = exitButton;
            _continueButton = continueButton;
            _layout = layout;
            _speciesDropdown = speciesDropdown;
            _fishScroll = fishScroll;
            ResolveMissingRefs();
            Wire();
        }

        void Awake()
        {
            ResolveMissingRefs();
            Wire();
            ApplyCampOverlayPresentation();
            FitLayout();
        }

        void ApplyCampOverlayPresentation()
        {
            var image = GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            image.color = CampFrostDimmer;
            image.raycastTarget = true;
        }

        void OnEnable()
        {
            ResolveMissingRefs();
            FitLayout();
        }

        [ContextMenu("Bind Prefab Refs")]
        public void EnsureFishCardSlots()
        {
            ResolveMissingRefs();
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
        }

        void LateUpdate()
        {
            FitLayout();
        }

        void FitLayout()
        {
            UiFactory.FitFixedLayout(transform as RectTransform, ref _layout, "SettleLayout", ref _fitting);
        }

        void ResolveMissingRefs()
        {
            if (_layout == null)
            {
                _layout = transform.Find("SettleLayout") as RectTransform;
            }

            if (_layout == null)
            {
                return;
            }

            if (_depthText == null)
            {
                _depthText = _layout.Find("Depth")?.GetComponent<Text>();
            }

            if (_grid == null)
            {
                _grid = _layout.Find("FishScroll/Viewport/Grid") as RectTransform;
            }

            if (_emptyText == null)
            {
                _emptyText = _layout.Find("FishScroll/Empty")?.GetComponent<Text>();
            }

            if (_totalCoinText == null)
            {
                _totalCoinText = _layout.Find("Totals/TotalCoin/Value")?.GetComponent<Text>();
            }

            if (_totalShellText == null)
            {
                _totalShellText = _layout.Find("Totals/TotalShell/Value")?.GetComponent<Text>();
            }

            if (_exitButton == null)
            {
                _exitButton = _layout.Find("ExitButton")?.GetComponent<Button>();
            }

            if (_continueButton == null)
            {
                _continueButton = _layout.Find("ContinueButton")?.GetComponent<Button>();
            }

            if (_speciesDropdown == null)
            {
                _speciesDropdown = _layout.Find("SpeciesDropdown")?.GetComponent<Dropdown>();
            }

            if (_fishScroll == null)
            {
                _fishScroll = _layout.Find("FishScroll")?.GetComponent<ScrollRect>();
            }

            ResolveFishCardSlots();
        }

        void ResolveFishCardSlots()
        {
            if (_fishCardSlots != null && _fishCardSlots.Length == FishCardSlotCount)
            {
                var complete = true;
                for (var i = 0; i < _fishCardSlots.Length; i++)
                {
                    if (_fishCardSlots[i] == null)
                    {
                        complete = false;
                        break;
                    }
                }

                if (complete)
                {
                    return;
                }
            }

            if (_grid == null)
            {
                return;
            }

            var slots = new SettleFishCardSlot[FishCardSlotCount];
            for (var i = 0; i < FishCardSlotCount; i++)
            {
                var found = _grid.Find(SettleFishCardSlot.SlotName(i));
                if (found != null)
                {
                    slots[i] = found.GetComponent<SettleFishCardSlot>();
                }
            }

            _fishCardSlots = slots;
        }

        public void Show(CastSettleResult result)
        {
            ApplyFont();
            ApplyCampOverlayPresentation();
            FitLayout();
            Bind(result ?? new CastSettleResult());
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void Bind(CastSettleResult result)
        {
            if (_depthText != null)
            {
                _depthText.text = "下潜深度：" + result.PeakDepthMeters + "米";
            }

            if (_totalCoinText != null)
            {
                _totalCoinText.text = result.TotalCoins.ToString();
            }

            if (_totalShellText != null)
            {
                _totalShellText.text = result.TotalShells.ToString();
            }

            _rows.Clear();
            if (result.Rows != null)
            {
                _rows.AddRange(result.Rows);
            }

            var hasRows = _rows.Count > 0;
            if (_emptyText != null)
            {
                _emptyText.gameObject.SetActive(!hasRows);
            }

            HideSpeciesDropdown();
            RefreshFishGrid();
        }

        void HideSpeciesDropdown()
        {
            if (_speciesDropdown != null)
            {
                _speciesDropdown.gameObject.SetActive(false);
            }
        }

        void RefreshFishGrid()
        {
            if (_grid == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                SetLayoutPlaceholderSlotsActive(false);
                RemoveRuntimeFishCards();
                var template = GetCardTemplate();
                if (template == null)
                {
                    Debug.LogWarning("SettleView missing FishCardSlot_0 template on the prefab.");
                }
                else
                {
                    for (var i = 0; i < _rows.Count; i++)
                    {
                        var card = SettleFishCardSlot.CloneRuntimeCard(template, _grid, i);
                        card.Bind(_rows[i]);
                    }
                }
            }
            else
            {
                RemoveRuntimeFishCards();
                SetLayoutPlaceholderSlotsActive(true);
            }

            ResetFishScrollContent();
        }

        void SetLayoutPlaceholderSlotsActive(bool active)
        {
            ResolveFishCardSlots();
            if (_fishCardSlots == null)
            {
                return;
            }

            for (var i = 0; i < _fishCardSlots.Length; i++)
            {
                if (_fishCardSlots[i] != null)
                {
                    _fishCardSlots[i].SetActiveSlot(active);
                    if (active)
                    {
                        _fishCardSlots[i].ClearPlaceholder();
                    }
                }
            }
        }

        SettleFishCardSlot GetCardTemplate()
        {
            ResolveFishCardSlots();
            if (_fishCardSlots != null)
            {
                for (var i = 0; i < _fishCardSlots.Length; i++)
                {
                    if (_fishCardSlots[i] != null)
                    {
                        return _fishCardSlots[i];
                    }
                }
            }

            return _grid.Find(SettleFishCardSlot.SlotName(0))?.GetComponent<SettleFishCardSlot>();
        }

        void ResetFishScrollContent()
        {
            if (_grid == null || _fishScroll == null)
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_grid);
            Canvas.ForceUpdateCanvases();
            _fishScroll.normalizedPosition = new Vector2(0f, 1f);
        }

        void RemoveRuntimeFishCards()
        {
            if (_grid == null)
            {
                return;
            }

            for (var i = _grid.childCount - 1; i >= 0; i--)
            {
                var child = _grid.GetChild(i);
                if (SettleFishCardSlot.IsLayoutPlaceholderName(child.name))
                {
                    continue;
                }

                if (child.GetComponent<SettleFishCardSlot>() == null)
                {
                    continue;
                }

                var go = child.gameObject;
                if (Application.isPlaying)
                {
                    Destroy(go);
                }
                else
                {
                    DestroyImmediate(go);
                }
            }
        }

        void Wire()
        {
            if (_wired || _exitButton == null || _continueButton == null)
            {
                return;
            }

            _exitButton.onClick.AddListener(() => ExitClicked?.Invoke());
            _continueButton.onClick.AddListener(() => ContinueClicked?.Invoke());
            _wired = true;
        }

        void ApplyFont()
        {
            var font = UiFactory.ResolveFont();
            var texts = GetComponentsInChildren<Text>(true);
            for (var i = 0; i < texts.Length; i++)
            {
                texts[i].font = font;
            }
        }
    }
}
