using System;
using IceFishing.Model;
using IceFishing.View;

namespace IceFishing.Controller
{
    /// <summary>
    /// 营地流程：开始钓鱼、打开占位说明。不直接改 UI 文字。
    /// </summary>
    public sealed class HubController
    {
        readonly HubView _view;
        readonly OverlayView _overlay;
        readonly PlayerProfile _profile;
        readonly Action _startFishing;
        readonly Action _beginCastTutorial;
        readonly Action _profileChanged;

        public HubController(
            HubView view,
            OverlayView overlay,
            PlayerProfile profile,
            Action startFishing,
            Action profileChanged,
            Action beginCastTutorial = null)
        {
            _view = view;
            _overlay = overlay;
            _profile = profile;
            _startFishing = startFishing;
            _beginCastTutorial = beginCastTutorial;
            _profileChanged = profileChanged;

            _view.StartClicked += OnStart;
            _view.GearClicked += OnGear;
            _view.CollectionClicked += OnCollection;
            _view.SettingsClicked += OnSettings;
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

        void OnGear()
        {
            _overlay.Show(
                "渔具",
                "鱼线  Lv." + _profile.LineLevel + "    最大深度 " +
                UnityEngine.Mathf.RoundToInt(GearTables.LineMaxDepth(_profile.LineLevel)) + " m\n" +
                "鱼钩  Lv." + _profile.HookLevel + "    承重 " +
                GearTables.HookCapacity(_profile.HookLevel) + " 条\n" +
                "渔坠  Lv." + _profile.SinkerLevel + "    起始深度 " +
                UnityEngine.Mathf.RoundToInt(GearTables.SinkerStartDepth(_profile.SinkerLevel)) + " m\n\n" +
                "升级将在成长阶段开放。");
        }

        void OnCollection()
        {
            _overlay.Show("图鉴", "首次钓到的鱼会点亮图鉴。当前还没有捕获记录。");
        }

        void OnSettings()
        {
            _overlay.Show(
                "设置",
                "操作：触屏拖拽或鼠标控制鱼钩左右移动。",
                false);
        }
    }
}
