using System;
using IceFishing.Model;
using IceFishing.View;

namespace IceFishing.Controller
{
    /// <summary>
    /// 营地流程：开始钓鱼。不直接改 UI 文字。
    /// </summary>
    public sealed class HubController
    {
        readonly HubView _view;
        readonly PlayerProfile _profile;
        readonly Action _startFishing;
        readonly Action _beginCastTutorial;

        public HubController(
            HubView view,
            PlayerProfile profile,
            Action startFishing,
            Action beginCastTutorial = null)
        {
            _view = view;
            _profile = profile;
            _startFishing = startFishing;
            _beginCastTutorial = beginCastTutorial;
            _view.StartClicked += OnStart;
        }

        void OnStart()
        {
            // M1：暂不消耗/校验饵料。
            if (!_profile.HasSeenCastTutorial && _beginCastTutorial != null)
            {
                _view.SetInteractable(false);
                _beginCastTutorial();
                return;
            }

            BeginCastThenFish();
        }

        void BeginCastThenFish()
        {
            _view.PlayCast(() => _startFishing?.Invoke());
        }
    }
}
