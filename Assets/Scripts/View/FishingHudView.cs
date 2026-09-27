using System;
using IceFishing.Core;
using IceFishing.Model;
using UnityEngine;
using UnityEngine.UI;

namespace IceFishing.View
{
    /// <summary>
    /// 钓鱼 HUD。只根据 CastSession 刷新文字，不推进深度、不处理碰撞。
    /// Prefab 预览和场景共用 1080×2160 HudLayout，按父画布等比缩放。
    /// </summary>
    [ExecuteAlways]
    public sealed class FishingHudView : MonoBehaviour
    {
        [SerializeField] Text _phaseText;
        [SerializeField] Text _depthText;
        [SerializeField] Text _protectionText;
        [Tooltip("已钓数量 / 可携带上限（第二条统计）")]
        [SerializeField] Text _haulText;
        [SerializeField] Button _pauseButton;
        [SerializeField] RectTransform _layout;

        bool _wired;
        bool _fitting;

        public event Action PauseClicked;

        public void Configure(Text phase, Text depth, Text protection, Text haul, Button pause, RectTransform layout)
        {
            _phaseText = phase;
            _depthText = depth;
            _protectionText = protection;
            _haulText = haul;
            _pauseButton = pause;
            _layout = layout;
            Wire();
            FitLayout();
        }

        void Awake()
        {
            Wire();
            FitLayout();
        }

        void OnEnable()
        {
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
            FitLayout();
        }

        public void SetFade(float alpha)
        {
            var group = GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = gameObject.AddComponent<CanvasGroup>();
            }

            group.alpha = Mathf.Clamp01(alpha);
            group.blocksRaycasts = alpha > 0.85f;
        }

        public void RefreshLayout()
        {
            FitLayout();
        }

        void FitLayout()
        {
            UiFactory.FitFixedLayout(transform as RectTransform, ref _layout, "HudLayout", ref _fitting);
        }

        public void Hide()
        {
            SetFade(1f);
            gameObject.SetActive(false);
        }

        public void Bind(CastSession session)
        {
            if (session == null)
            {
                return;
            }

            ApplyFont();
            if (_phaseText != null)
            {
                _phaseText.text = PhaseLabel(session.Phase);
            }

            if (_depthText != null)
            {
                _depthText.text = Mathf.RoundToInt(session.Depth) + "/" + Mathf.RoundToInt(session.MaxDepth);
            }

            if (_protectionText != null)
            {
                _protectionText.text = session.ProtectionLeft + "/" + session.ProtectionMax;
            }

            if (_haulText != null)
            {
                _haulText.text = session.CaughtCount + "/" + session.Capacity;
            }
        }

        void Wire()
        {
            if (_wired || _pauseButton == null)
            {
                return;
            }

            _pauseButton.onClick.AddListener(() => PauseClicked?.Invoke());
            _wired = true;
        }

        void ApplyFont()
        {
            var font = UiFactory.ResolveFont();
            SetFont(_phaseText, font);
            SetFont(_depthText, font);
            SetFont(_protectionText, font);
            SetFont(_haulText, font);
        }

        static void SetFont(Text text, Font font)
        {
            if (text != null && text.font != font)
            {
                text.font = font;
            }
        }

        static string PhaseLabel(CastPhase phase)
        {
            switch (phase)
            {
                case CastPhase.Descending:
                    return "下潜躲避";
                case CastPhase.Ascending:
                    return "上浮捕获";
                case CastPhase.Returning:
                    return "返回冰洞";
                case CastPhase.Paused:
                    return "已暂停";
                case CastPhase.Settle:
                    return "回到冰洞";
                default:
                    return "准备中";
            }
        }
    }
}
