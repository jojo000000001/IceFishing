using System;
using IceFishing.Core;
using IceFishing.Model;
using IceFishing.View;

namespace IceFishing.Controller
{
    /// <summary>
    /// 钓鱼流程：推进下潜/上浮、暂停、撤退。鱼头碰撞语义在这里：下潜扣保护，上浮捕获。
    /// </summary>
    public sealed class FishingController
    {
        readonly FishingHudView _hud;
        readonly PausePopupView _pause;
        readonly Action _returnToHub;

        CastSession _session;
        bool _paused;

        public CastSession Session
        {
            get { return _session; }
        }

        public bool IsPaused
        {
            get { return _paused; }
        }

        public FishingController(FishingHudView hud, PausePopupView pause, Action returnToHub)
        {
            _hud = hud;
            _pause = pause;
            _returnToHub = returnToHub;
            _hud.PauseClicked += OnPause;
            _pause.ResumeClicked += OnResume;
            _pause.RetreatClicked += OnRetreat;
        }

        public void Enter(
            PlayerProfile profile,
            int protectionHits,
            float lineLengthMeters,
            float descentMetersPerSecond,
            float ascendCatchMetersPerSecond,
            float ascentMetersPerSecond)
        {
            _session = CastSession.CreatePlaceholder(
                profile,
                protectionHits,
                lineLengthMeters,
                descentMetersPerSecond,
                ascendCatchMetersPerSecond,
                ascentMetersPerSecond);
            _hud.StatsProfile?.ApplyToSession(_session);
            _paused = false;
            _pause.Hide();
            _hud.Show();
            _hud.Bind(_session);
        }

        public void Exit()
        {
            _paused = false;
            _pause.Hide();
            _hud.Hide();
            _session = null;
        }

        public void Tick(float dt, float minX, float maxX, float? targetX)
        {
            if (_session == null || _paused)
            {
                return;
            }

            var x = targetX.HasValue ? targetX.Value : _session.HookX;
            _session.Tick(dt, x, minX, maxX);
            _hud.Bind(_session);
        }

        /// <summary>
        /// 鱼头碰撞。返回 true 表示计入捕获、应挂到钩上（勿 MarkHit）。
        /// </summary>
        public bool TryHandleHeadHit(FishView fish, out int catchSlot)
        {
            catchSlot = -1;
            if (_session == null || _paused || fish == null || fish.Consumed)
            {
                return false;
            }

            if (_session.Phase == CastPhase.Returning || _session.CaughtCount >= _session.Capacity)
            {
                return false;
            }

            var countBefore = _session.CaughtCount;
            _session.ApplyHeadHit();
            _hud.Bind(_session);

            if (_session.CaughtCount > countBefore)
            {
                catchSlot = countBefore;
                return true;
            }

            fish.MarkHit();
            return false;
        }

        void OnPause()
        {
            if (_session == null)
            {
                return;
            }

            _paused = true;
            _pause.Show();
        }

        void OnResume()
        {
            _paused = false;
            _pause.Hide();
        }

        void OnRetreat()
        {
            _paused = false;
            _pause.Hide();
            _returnToHub?.Invoke();
        }
    }
}
