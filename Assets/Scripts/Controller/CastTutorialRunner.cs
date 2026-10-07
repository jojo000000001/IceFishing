using System.Collections;
using IceFishing.Core;
using IceFishing.Model;
using IceFishing.View;
using UnityEngine;

namespace IceFishing.Controller
{
    /// <summary>
    /// 首次教程：95 米切入。先停约 1 秒看清钩和下面的鱼，
    /// 再下潜一小段刚好碰到引导鱼；挂鱼后镜头跟钩再下一小段，然后一起上浮约 10 米。
    /// </summary>
    public static class CastTutorialRunner
    {
        const float AutoReturnSeconds = 2f;

        public static IEnumerator Run(
            CastSession session,
            WorldView world,
            FishingController fishing,
            CastTutorialView view,
            System.Func<bool> introComplete,
            System.Action onUnderwaterReady,
            System.Action onFinished)
        {
            if (session == null || world == null || fishing == null || view == null)
            {
                yield break;
            }

            try
            {
                while (introComplete != null && !introComplete())
                {
                    yield return null;
                }

                onUnderwaterReady?.Invoke();
                world.SetTutorialSpawnFrozen(true);
                world.ClearFish();
                var startDepth = Mathf.Min(FishingRules.TutorialStartDepthMeters, session.MaxDepth);
                session.Depth = startDepth;
                session.ObservePeakDepth();
                session.Phase = CastPhase.Descending;
                session.ProtectionLeft = 0;
                session.DescendAfterCatchMeters = 0f;
                world.UnlockTutorialCamera();
                view.CaptureCardRestPose();
                view.ResetCardPose();
                ApplyCoupled(world, view, session);

                view.SetCaption(CastTutorialView.CaptionAvoidFish);
                world.SpawnTutorialBaitBelowHook(session, FishingRules.TutorialBaitBelowWorld);
                world.SpawnTutorialAmbientSchool(session, 4);
                world.SpawnTutorialRiseSchool(session, FishingRules.TutorialAscendMeters, 10);
                yield return HoldStill(session, world, view, FishingRules.TutorialHoldSeconds);
                yield return DropOntoBait(session, world, view, fishing);
                if (session.CaughtCount <= 0)
                {
                    yield return DescendToMax(session, world, view, fishing);
                }

                if (session.CaughtCount > 0)
                {
                    yield return DescendAfterCatch(session, world, view, fishing);
                }

                view.SetCaption(CastTutorialView.CaptionAscendCatch);
                view.CaptureCardRestPose();
                var ascendFromDepth = session.Depth;
                var finalAscendDepth = Mathf.Max(0f, ascendFromDepth - FishingRules.TutorialAscendMeters);

                yield return RiseTogether(
                    world,
                    view,
                    session,
                    fishing,
                    ascendFromDepth,
                    finalAscendDepth,
                    session.AscendCatchMetersPerSecond);

                yield return WaitAfterAscend(
                    AutoReturnSeconds,
                    world,
                    view,
                    session,
                    fishing,
                    ascendFromDepth,
                    finalAscendDepth);
                onFinished?.Invoke();
            }
            finally
            {
                world.UnlockTutorialCamera();
                world.SetTutorialSpawnFrozen(false);
            }
        }

        static IEnumerator HoldStill(
            CastSession session,
            WorldView world,
            CastTutorialView view,
            float seconds)
        {
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                ApplyCoupled(world, view, session);
                TickLiveFish(world, session, null);
                yield return null;
            }
        }

        static IEnumerator DropOntoBait(
            CastSession session,
            WorldView world,
            CastTutorialView view,
            FishingController fishing)
        {
            var dropMeters = world.MetersForWorldDrop(FishingRules.TutorialBaitBelowWorld);
            var targetDepth = Mathf.Min(session.MaxDepth, session.Depth + dropMeters);
            var mps = Mathf.Clamp(dropMeters / FishingRules.TutorialDropSeconds, 0.8f, 3f);
            session.Phase = CastPhase.Descending;
            while (session.Depth < targetDepth - 0.01f && session.CaughtCount <= 0)
            {
                session.Depth = Mathf.MoveTowards(session.Depth, targetDepth, mps * Time.deltaTime);
                session.ObservePeakDepth();
                ApplyCoupled(world, view, session);
                TickLiveFish(world, session, fishing);
                if (TryCatchSwimming(session, world, fishing, view))
                {
                    yield break;
                }

                yield return null;
            }

            if (session.CaughtCount <= 0)
            {
                session.Depth = targetDepth;
                session.ObservePeakDepth();
                ApplyCoupled(world, view, session);
                TryCatchSwimming(session, world, fishing, view);
            }
        }

        static IEnumerator DescendToMax(
            CastSession session,
            WorldView world,
            CastTutorialView view,
            FishingController fishing)
        {
            var mps = Mathf.Max(0.5f, session.DescentMetersPerSecond);
            var targetDepth = session.MaxDepth;
            session.Phase = CastPhase.Descending;
            while (session.Depth < targetDepth - 0.05f)
            {
                session.Depth = Mathf.MoveTowards(session.Depth, targetDepth, mps * Time.deltaTime);
                session.ObservePeakDepth();
                ApplyCoupled(world, view, session);
                TickLiveFish(world, session, fishing);
                if (TryCatchSwimming(session, world, fishing, view))
                {
                    yield break;
                }

                yield return null;
            }

            session.Depth = targetDepth;
            session.ObservePeakDepth();
            session.Phase = CastPhase.Ascending;
            ApplyCoupled(world, view, session);
        }

        /// <summary>
        /// 挂鱼后按正常规则再下潜一小段：镜头继续跟钩，避免锁镜导致钩子闪现。
        /// </summary>
        static IEnumerator DescendAfterCatch(
            CastSession session,
            WorldView world,
            CastTutorialView view,
            FishingController fishing)
        {
            if (session.CaughtCount <= 0)
            {
                ApplyCoupled(world, view, session);
                yield break;
            }

            if (session.DescendAfterCatchMeters <= 0f)
            {
                session.DescendAfterCatchMeters = FishingRules.DescendAfterHeadCatchMeters;
            }

            session.Phase = CastPhase.Descending;
            view.CaptureCardRestPose();
            var mps = Mathf.Max(0.5f, session.DescentMetersPerSecond);

            while (session.DescendAfterCatchMeters > 0f)
            {
                var step = mps * Time.deltaTime;
                session.Depth = Mathf.Min(session.MaxDepth, session.Depth + step);
                session.ObservePeakDepth();
                session.DescendAfterCatchMeters -= step;
                ApplyCoupled(world, view, session);
                TickLiveFish(world, session, fishing);
                yield return null;
            }

            session.DescendAfterCatchMeters = 0f;
            session.Phase = CastPhase.Ascending;
            ApplyCoupled(world, view, session);
        }

        /// <summary>卡片和钩一起上浮：镜头跟钩深，卡片按上升进度上移。</summary>
        static IEnumerator RiseTogether(
            WorldView world,
            CastTutorialView view,
            CastSession session,
            FishingController fishing,
            float ascendFromDepth,
            float targetDepth,
            float metersPerSecond)
        {
            session.Phase = CastPhase.Ascending;
            session.DescendAfterCatchMeters = 0f;
            var mps = Mathf.Max(0.5f, metersPerSecond);
            var span = Mathf.Max(0.01f, ascendFromDepth - targetDepth);
            view.CaptureCardRestPose();
            view.ResetCardPose();

            while (session.Depth > targetDepth + 0.05f)
            {
                session.Depth = Mathf.MoveTowards(session.Depth, targetDepth, mps * Time.deltaTime);
                var progress = (ascendFromDepth - session.Depth) / span;
                view.ApplyAscendProgress(progress);
                ApplyRising(world, session);
                TryHookHeadCatch(world, session, fishing);
                TickLiveFish(world, session, fishing);
                yield return null;
            }

            session.Depth = targetDepth;
            session.Phase = CastPhase.Ascending;
            view.ApplyAscendProgress(1f);
            ApplyRising(world, session);
            TryHookHeadCatch(world, session, fishing);
        }

        static IEnumerator WaitAfterAscend(
            float seconds,
            WorldView world,
            CastTutorialView view,
            CastSession session,
            FishingController fishing,
            float ascendFromDepth,
            float frozenDepth)
        {
            session.Depth = frozenDepth;
            session.Phase = CastPhase.Ascending;
            var span = Mathf.Max(0.01f, ascendFromDepth - frozenDepth);
            view.ApplyAscendProgress(Mathf.Clamp01((ascendFromDepth - frozenDepth) / span));
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                ApplyRising(world, session);
                TryHookHeadCatch(world, session, fishing);
                TickLiveFish(world, session, fishing);
                yield return null;
            }
        }

        static void TickLiveFish(WorldView world, CastSession session, FishingController fishing)
        {
            if (world == null)
            {
                return;
            }

            world.TickFish(Time.deltaTime, session, fishing != null && fishing.IsPaused);
        }

        static void TryHookHeadCatch(WorldView world, CastSession session, FishingController fishing)
        {
            if (world == null || session == null || fishing == null)
            {
                return;
            }

            if (!world.TryHookHeadHit(session, out var fish) || fish == null)
            {
                return;
            }

            if (fishing.TryHandleHeadHit(fish, out var catchSlot))
            {
                session.RecordCatch(fish.Definition);
                world.AttachCaughtFish(fish, catchSlot);
            }
        }

        static void ApplyCoupled(WorldView world, CastTutorialView view, CastSession session)
        {
            world.UnlockTutorialCamera();
            session.ObservePeakDepth();
            world.ApplyCast(session);
            view.ResetCardPose();
        }

        static void ApplyRising(WorldView world, CastSession session)
        {
            world.UnlockTutorialCamera();
            session.ObservePeakDepth();
            world.ApplyCast(session);
        }

        static bool TryCatchSwimming(
            CastSession session,
            WorldView world,
            FishingController fishing,
            CastTutorialView view)
        {
            if (session == null || session.CaughtCount > 0)
            {
                return false;
            }

            session.ProtectionLeft = 0;
            session.Phase = CastPhase.Descending;
            if (!world.TryTutorialHeadHit(session, out var fish) || fish == null)
            {
                return false;
            }

            if (fishing.TryHandleHeadHit(fish, out var slot))
            {
                session.RecordCatch(fish.Definition);
                world.AttachCaughtFish(fish, slot);
            }
            else
            {
                world.AttachCaughtFish(fish, Mathf.Max(0, session.CaughtCount - 1));
            }

            view?.SetCaption(CastTutorialView.CaptionAscendCatch);
            return true;
        }
    }
}
