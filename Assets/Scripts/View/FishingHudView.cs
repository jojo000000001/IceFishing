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
        [SerializeField] Button _pauseButton;
        [SerializeField] RectTransform _layout;
        [SerializeField] FishingHudStatsDefinition _statsProfile;
        [SerializeField] HudStatPillView[] _statPills;

        bool _wired;
        bool _fitting;

        public event Action PauseClicked;

        public FishingHudStatsDefinition StatsProfile => _statsProfile;

        public void Configure(
            Text phase,
            HudStatPillView[] statPills,
            FishingHudStatsDefinition statsProfile,
            Button pause,
            RectTransform layout)
        {
            _phaseText = phase;
            _statPills = statPills;
            _statsProfile = statsProfile;
            _pauseButton = pause;
            _layout = layout;
            Wire();
            ApplyStatPresentation();
            FitLayout();
        }

        void Awake()
        {
            ResolveStatPills();
            Wire();
            ApplyStatPresentation();
            FitLayout();
        }

        void OnEnable()
        {
            ApplyStatPresentation();
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
            ApplyStatPresentation();
            FitLayout();
        }

        public void ShowFishingMode()
        {
            Show();
            SetPauseVisible(true);
            SetPhaseVisible(true);
        }

        /// <summary>
        /// 营地/回营：仅左上角三条统计，数值与下一局开局一致，无暂停与阶段文案。
        /// </summary>
        public void ShowCampStats(PlayerProfile profile, int protectionHits, float lineLengthMeters)
        {
            Show();
            SetPauseVisible(false);
            SetPhaseVisible(false);
            SetFade(1f);

            var group = GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = gameObject.AddComponent<CanvasGroup>();
            }

            group.blocksRaycasts = false;

            var session = CastSession.CreateCampDisplay(profile, protectionHits, lineLengthMeters);
            _statsProfile?.ApplyToSession(session);
            Bind(session);
        }

        public void SetPauseVisible(bool visible)
        {
            if (_pauseButton != null)
            {
                _pauseButton.gameObject.SetActive(visible);
            }
        }

        public void SetPhaseVisible(bool visible)
        {
            if (_phaseText != null)
            {
                _phaseText.gameObject.SetActive(visible);
            }
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

            ApplyStatPresentation();

            var font = UiFactory.ResolveFont();
            ApplyFont(font);
            if (_phaseText != null)
            {
                _phaseText.text = PhaseLabel(session.Phase);
            }

            ResolveStatPills();
            if (_statPills == null)
            {
                return;
            }

            for (var i = 0; i < _statPills.Length; i++)
            {
                if (_statPills[i] != null)
                {
                    _statPills[i].BindValue(session, font);
                }
            }
        }

        void ResolveStatPills()
        {
            if (_statPills != null && _statPills.Length > 0)
            {
                return;
            }

            _statPills = GetComponentsInChildren<HudStatPillView>(true);
        }

        void ApplyStatPresentation()
        {
            ResolveStatPills();
            if (_statPills == null || _statPills.Length == 0)
            {
                return;
            }

            var sharedPill = _statsProfile != null ? _statsProfile.SharedPill : null;
            var pillTint = _statsProfile != null
                ? _statsProfile.PillTint
                : FishingHudStatsDefinition.DefaultPillTint;
            for (var i = 0; i < _statPills.Length; i++)
            {
                var pill = _statPills[i];
                if (pill == null)
                {
                    continue;
                }

                pill.ResolveReferences();
                pill.SetDefinition(ResolveDefinitionForPill(pill, i));
                pill.ApplyPresentation(sharedPill, pillTint);
            }
        }

        FishingHudStatDefinition ResolveDefinitionForPill(HudStatPillView pill, int index)
        {
            var stats = _statsProfile != null ? _statsProfile.Stats : null;
            if (stats == null || stats.Length == 0)
            {
                return pill.Definition;
            }

            var kind = pill.Definition != null
                ? pill.Definition.Kind
                : index >= 0 && index < stats.Length && stats[index] != null
                    ? stats[index].Kind
                    : FishingHudStatKind.Protection;
            for (var i = 0; i < stats.Length; i++)
            {
                if (stats[i] != null && stats[i].Kind == kind)
                {
                    return stats[i];
                }
            }

            if (index >= 0 && index < stats.Length && stats[index] != null)
            {
                return stats[index];
            }

            return pill.Definition;
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

        void ApplyFont(Font font)
        {
            SetFont(_phaseText, font);
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
