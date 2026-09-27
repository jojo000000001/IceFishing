using IceFishing.Core;

namespace IceFishing.Model
{
    /// <summary>
    /// 营地饵料回复：每 15 秒 +1，上限 10。
    /// InfiniteBait 打开时维持满饵，方便验收。
    /// </summary>
    public sealed class BaitRegen
    {
        public const float IntervalSeconds = 15f;

        float _elapsed;

        public float SecondsUntilNext
        {
            get
            {
                if (DebugFlags.InfiniteBait)
                {
                    return 0f;
                }

                return IntervalSeconds - _elapsed;
            }
        }

        public bool Tick(float deltaTime, PlayerProfile profile)
        {
            if (profile == null)
            {
                return false;
            }

            if (DebugFlags.InfiniteBait)
            {
                if (profile.Bait != PlayerProfile.MaxBait)
                {
                    profile.Bait = PlayerProfile.MaxBait;
                    return true;
                }

                return false;
            }

            if (profile.Bait >= PlayerProfile.MaxBait)
            {
                _elapsed = 0f;
                return false;
            }

            _elapsed += deltaTime;
            var changed = false;
            while (_elapsed >= IntervalSeconds && profile.Bait < PlayerProfile.MaxBait)
            {
                _elapsed -= IntervalSeconds;
                profile.Bait++;
                changed = true;
            }

            return changed;
        }

        public void Reset()
        {
            _elapsed = 0f;
        }
    }
}
