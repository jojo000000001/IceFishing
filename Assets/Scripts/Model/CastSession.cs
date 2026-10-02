using System.Collections.Generic;
using IceFishing.Core;
using UnityEngine;

namespace IceFishing.Model
{
    /// <summary>
    /// 下潜/上浮手感常数。深度用米；表现层再换成世界坐标。
    /// </summary>
    public static class FishingRules
    {
        public const float DescentMetersPerSecond = 12f;
        public const float AscentMetersPerSecond = 10f;
        public const float HookWorldSpeed = 14f;
        public const float WorldUnitsPerMeter = 0.25f;
        public const float LaneScreenFraction = 0.72f;
        public const float HookScreenY = 0.55f;
        public const float IntroDepthMeters = 10f;
        public const float FishSpawnMinDepthMeters = 10f;
        public const float IntroDuration = 1.4f;
        public const int FishMaxAlive = 12;
        public const int FishSeedCount = 6;
        public const float FishSpawnInterval = 0.42f;
        public const float FishSpawnAheadMinMeters = 8f;
        public const float FishSpawnAheadMaxMeters = 48f;
        public const float HookHitRadius = 0.22f;
        /// <summary>无保护罩下潜挂鱼后，继续下潜的深度（米），再切上浮。</summary>
        public const float DescendAfterHeadCatchMeters = 4f;
    }

    /// <summary>
    /// 一局冰钓的可变状态：深度、钩子水平位置、阶段。
    /// 不含 Transform。暂停由控制器挡掉 Tick，不改 Phase。
    /// </summary>
    public sealed class CastSession
    {
        public CastPhase Phase;
        public float Depth;
        public float PeakDepth;
        public float MaxDepth;
        public float HookX;
        public int ProtectionLeft;
        public int ProtectionMax;
        public int CaughtCount;
        public int Capacity;
        public float DescentMetersPerSecond;
        public float AscendCatchMetersPerSecond;
        public float AscentMetersPerSecond;
        /// <summary>挂鱼后剩余的下潜距离（米）；&gt;0 时仍算下潜阶段。</summary>
        public float DescendAfterCatchMeters;
        readonly List<FishDefinition> _caughtFish = new List<FishDefinition>();

        public IReadOnlyList<FishDefinition> CaughtFish
        {
            get { return _caughtFish; }
        }

        public void RecordCatch(FishDefinition definition)
        {
            if (definition != null)
            {
                _caughtFish.Add(definition);
            }
        }

        public static CastSession CreatePlaceholder(
            PlayerProfile profile,
            int protectionHits,
            float lineLengthMeters,
            float descentMetersPerSecond,
            float ascendCatchMetersPerSecond,
            float ascentMetersPerSecond)
        {
            var protection = Mathf.Max(0, protectionHits);
            var maxDepth = Mathf.Max(1f, lineLengthMeters);
            var startDepth = GearTables.SinkerStartDepth(profile != null ? profile.SinkerLevel : 1);
            return new CastSession
            {
                Phase = CastPhase.Descending,
                Depth = Mathf.Min(startDepth, maxDepth),
                PeakDepth = Mathf.Min(startDepth, maxDepth),
                MaxDepth = maxDepth,
                HookX = 0f,
                ProtectionMax = protection,
                ProtectionLeft = protection,
                CaughtCount = 0,
                Capacity = GearTables.HookCapacity(profile != null ? profile.HookLevel : 1),
                DescentMetersPerSecond = Mathf.Max(0.1f, descentMetersPerSecond),
                AscendCatchMetersPerSecond = Mathf.Max(0.1f, ascendCatchMetersPerSecond),
                AscentMetersPerSecond = Mathf.Max(0.1f, ascentMetersPerSecond)
            };
        }

        public void Steer(float dt, float targetX, float minX, float maxX)
        {
            HookX = Mathf.MoveTowards(HookX, Mathf.Clamp(targetX, minX, maxX), FishingRules.HookWorldSpeed * dt);
        }

        public void Tick(float dt, float targetX, float minX, float maxX)
        {
            if (dt <= 0f || Phase == CastPhase.None)
            {
                return;
            }

            Steer(dt, targetX, minX, maxX);
            if (Phase == CastPhase.Settle)
            {
                return;
            }

            if (Phase == CastPhase.Descending)
            {
                Depth += DescentMetersPerSecond * dt;
                ObservePeakDepth();
                if (DescendAfterCatchMeters > 0f)
                {
                    DescendAfterCatchMeters -= DescentMetersPerSecond * dt;
                    if (DescendAfterCatchMeters <= 0f)
                    {
                        DescendAfterCatchMeters = 0f;
                        Phase = CaughtCount >= Capacity ? CastPhase.Returning : CastPhase.Ascending;
                    }
                }
                else if (Depth >= MaxDepth)
                {
                    Depth = MaxDepth;
                    Phase = CastPhase.Ascending;
                }

                return;
            }

            if (Phase == CastPhase.Ascending || Phase == CastPhase.Returning)
            {
                var ascentMps = Phase == CastPhase.Returning
                    ? AscentMetersPerSecond
                    : AscendCatchMetersPerSecond;
                Depth -= ascentMps * dt;
                if (Depth <= 0f)
                {
                    Depth = 0f;
                    Phase = CastPhase.Settle;
                }
            }
        }

        public void ObservePeakDepth()
        {
            if (Depth > PeakDepth)
            {
                PeakDepth = Depth;
            }
        }

        public void ApplyHeadHit()
        {
            if (Phase == CastPhase.Returning || Phase == CastPhase.Settle || CaughtCount >= Capacity)
            {
                return;
            }

            if (Phase == CastPhase.Descending)
            {
                if (DescendAfterCatchMeters > 0f)
                {
                    return;
                }

                if (ProtectionLeft > 0)
                {
                    ProtectionLeft--;
                    return;
                }

                if (CaughtCount >= Capacity)
                {
                    Phase = CastPhase.Ascending;
                    return;
                }

                CaughtCount++;
                DescendAfterCatchMeters = FishingRules.DescendAfterHeadCatchMeters;
                if (CaughtCount >= Capacity)
                {
                    // 承重满：下潜一小段后 Tick 切 Returning。
                }

                return;
            }

            if (Phase == CastPhase.Ascending && CaughtCount < Capacity)
            {
                CaughtCount++;
                if (CaughtCount >= Capacity)
                {
                    Phase = CastPhase.Returning;
                }
            }
        }
    }
}
