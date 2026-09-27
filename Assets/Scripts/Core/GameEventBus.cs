using System;
using IceFishing.Model;

namespace IceFishing.Core
{
    /// <summary>
    /// 观察者总线：Model / Controller 发事件，View 订阅。
    /// 不负责改数值，也不持有场景引用。禁止在回调里再同步抛出同名事件。
    /// </summary>
    public sealed class GameEventBus
    {
        public event Action<AppScreen> ScreenChanged;
        public event Action<PlayerProfile> ProfileChanged;
        public event Action<CastSession> SessionChanged;

        public void RaiseScreenChanged(AppScreen screen)
        {
            ScreenChanged?.Invoke(screen);
        }

        public void RaiseProfileChanged(PlayerProfile profile)
        {
            ProfileChanged?.Invoke(profile);
        }

        public void RaiseSessionChanged(CastSession session)
        {
            SessionChanged?.Invoke(session);
        }
    }
}
