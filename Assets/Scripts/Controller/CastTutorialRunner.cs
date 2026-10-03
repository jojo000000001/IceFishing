using System.Collections;
using IceFishing.Core;
using IceFishing.Model;
using IceFishing.View;
using UnityEngine;

namespace IceFishing.Controller
{
    /// <summary>
    /// 首次教程：95 米切入；挂鱼后钩子上浮约 10 米（与正常对局相同：深度减小 + 镜头跟钩），停住后点退出或 5 秒回营地。
    /// </summary>
    public static class CastTutorialRunner
    {
        const float HoldSeconds = 2f;
        const float AutoReturnSeconds = 5f;
        const float WaitForFishSeconds = 8f;

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
                world.SetTutorialSpawnFrozen(false);
                var startDepth = Mathf.Min(FishingRules.TutorialStartDepthMeters, session.MaxDepth);
                session.Depth = startDepth;
                session.ObservePeakDepth();
                session.Phase = CastPhase.Descending;
                session.ProtectionLeft = 0;
                session.DescendAfterCatchMeters = 0f;
                world.LockTutorialCamera(startDepth);
                session.Depth = world.ClampDepthInsideTutorialFrame(view, session, session.Depth, 0.45f);
                world.ApplyCast(session);

                view.SetCaption(CastTutorialView.CaptionAvoidFish);
                yield return Hold(HoldSeconds, world, view, session, fishing, false);

                var catchDepth = world.ResolveTutorialDescendDepth(view, session);
                yield return MoveDepth(
                    session,
                    world,
                    view,
                    fishing,
                    catchDepth,
                    session.DescentMetersPerSecond * 0.5f,
                    true);
                if (session.CaughtCount <= 0)
                {
                    yield return WaitForSwimmingCatch(session, world, view, fishing, WaitForFishSeconds);
                }

                session.DescendAfterCatchMeters = 0f;
                session.Depth = world.ClampDepthInsideTutorialFrame(view, session, session.Depth, 0.7f);
                world.ApplyCast(session);
                yield return Tick(0.35f, world, session, view, fishing, false);

                view.SetCaption(CastTutorialView.CaptionAscendCatch);
                yield return Hold(HoldSeconds, world, view, session, fishing, false);

                view.CaptureCardRestPose();
                view.ResetCardPose();
                var ascendFromDepth = session.Depth;
                var finalAscendDepth = Mathf.Max(
                    0f,
                    ascendFromDepth - FishingRules.TutorialAscendMeters);
                var lockedCameraDepth = world.TutorialLockedCameraDepth;

                yield return RiseAfterCatch(
                    world,
                    view,
                    session,
                    fishing,
                    ascendFromDepth,
                    finalAscendDepth,
                    lockedCameraDepth,
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

        static IEnumerator Hold(
            float seconds,
            WorldView world,
            CastTutorialView view,
            CastSession session,
            FishingController fishing,
            bool catchFish)
        {
            yield return Tick(seconds, world, session, view, fishing, catchFish);
        }

        static IEnumerator Tick(
            float seconds,
            WorldView world,
            CastSession session,
            CastTutorialView view,
            FishingController fishing,
            bool catchFish)
        {
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                ApplyClamped(world, view, session);
                world.TickFish(Time.deltaTime, session, false);
                if (catchFish && TryCatchSwimming(session, world, fishing, view))
                {
                    yield break;
                }

                yield return null;
            }
        }

        static IEnumerator MoveDepth(
            CastSession session,
            WorldView world,
            CastTutorialView view,
            FishingController fishing,
            float targetDepth,
            float metersPerSecond,
            bool catchFish)
        {
            var mps = Mathf.Max(0.5f, metersPerSecond);
            targetDepth = world.ClampDepthInsideTutorialFrame(view, session, targetDepth, catchFish ? 0.45f : 0.7f);

            while (Mathf.Abs(session.Depth - targetDepth) > 0.05f)
            {
                session.Depth = Mathf.MoveTowards(session.Depth, targetDepth, mps * Time.deltaTime);
                session.ObservePeakDepth();
                ApplyClamped(world, view, session);
                world.TickFish(Time.deltaTime, session, false);
                if (catchFish && TryCatchSwimming(session, world, fishing, view))
                {
                    yield break;
                }

                yield return null;
            }

            session.Depth = targetDepth;
            session.ObservePeakDepth();
            ApplyClamped(world, view, session);
        }

        /// <summary>
        /// ① 镜头仍锁在教程深度：只减钩深，钩在框内上移到中上（与下潜对称，不闪现）。
        /// ② 到达中上后：与正常对局一样镜头跟钩深上浮，教程卡同步上移。
        /// </summary>
        static IEnumerator RiseAfterCatch(
            WorldView world,
            CastTutorialView view,
            CastSession session,
            FishingController fishing,
            float ascendFromDepth,
            float targetDepth,
            float lockedCameraDepth,
            float metersPerSecond)
        {
            session.Phase = CastPhase.Ascending;
            session.DescendAfterCatchMeters = 0f;
            session.Depth = ascendFromDepth;
            var mps = Mathf.Max(0.5f, metersPerSecond);
            var cardCoupled = false;
            var coupleStartDepth = ascendFromDepth;
            var coupleCamBlend = 0f;
            const float coupleCameraBlendSeconds = 0.35f;
            view.ResetCardPose();

            while (session.Depth > targetDepth + 0.05f)
            {
                session.Depth = Mathf.MoveTowards(session.Depth, targetDepth, mps * Time.deltaTime);

                if (!cardCoupled)
                {
                    ApplyTutorialLockedCast(world, lockedCameraDepth, session);
                    var forceCoupleDepth = ascendFromDepth - Mathf.Min(3f, ascendFromDepth - targetDepth);
                    if (HookReachedAscendAnchor(world, view) || session.Depth <= forceCoupleDepth)
                    {
                        cardCoupled = true;
                        coupleStartDepth = session.Depth;
                        view.CaptureCardRestPose();
                    }
                }
                else
                {
                    coupleCamBlend = Mathf.Min(
                        1f,
                        coupleCamBlend + Time.deltaTime / Mathf.Max(0.05f, coupleCameraBlendSeconds));
                    var coupledSpan = Mathf.Max(0.01f, coupleStartDepth - targetDepth);
                    var cardProgress = (coupleStartDepth - session.Depth) / coupledSpan;
                    view.ApplyAscendProgress(cardProgress);
                    var cameraDepth = Mathf.Lerp(lockedCameraDepth, session.Depth, coupleCamBlend);
                    world.SyncTutorialCameraDepth(cameraDepth);
                    session.ObservePeakDepth();
                    world.ApplyCast(session);
                }

                TryHookHeadCatch(world, session, fishing);
                world.TickFish(Time.deltaTime, session, false);
                yield return null;
            }

            session.Depth = targetDepth;
            session.Phase = CastPhase.Ascending;
            view.ApplyAscendProgress(1f);
            ApplyNormalAscendCast(world, session);
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
            var coupledSpan = Mathf.Max(0.01f, ascendFromDepth - frozenDepth);
            var cardProgress = coupledSpan > 0.01f
                ? (ascendFromDepth - frozenDepth) / coupledSpan
                : 1f;
            view.ApplyAscendProgress(Mathf.Clamp01(cardProgress));
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                ApplyNormalAscendCast(world, session);
                TryHookHeadCatch(world, session, fishing);
                world.TickFish(Time.deltaTime, session, false);
                yield return null;
            }
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

        static bool HookReachedAscendAnchor(WorldView world, CastTutorialView view)
        {
            if (!view.TryGetAscendHookAnchorScreen(out var anchor))
            {
                return false;
            }

            return world.GetHookScreenY() >= anchor.y - 6f;
        }

        static void ApplyTutorialLockedCast(WorldView world, float cameraDepthMeters, CastSession session)
        {
            world.SyncTutorialCameraDepth(cameraDepthMeters);
            session.ObservePeakDepth();
            world.ApplyCast(session);
        }

        static void ApplyNormalAscendCast(WorldView world, CastSession session)
        {
            world.SyncTutorialCameraDepth(session.Depth);
            session.ObservePeakDepth();
            world.ApplyCast(session);
        }

        static IEnumerator WaitForSwimmingCatch(
            CastSession session,
            WorldView world,
            CastTutorialView view,
            FishingController fishing,
            float seconds)
        {
            var elapsed = 0f;
            while (elapsed < seconds && session.CaughtCount <= 0)
            {
                elapsed += Time.deltaTime;
                ApplyClamped(world, view, session);
                world.TickFish(Time.deltaTime, session, false);
                if (TryCatchSwimming(session, world, fishing, view))
                {
                    yield break;
                }

                yield return null;
            }
        }

        static void ApplyClamped(WorldView world, CastTutorialView view, CastSession session)
        {
            session.Depth = world.ClampDepthInsideTutorialFrame(view, session, session.Depth, 0.55f);
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
            session.DescendAfterCatchMeters = 0f;
            session.Phase = CastPhase.Descending;
            if (!world.TryTutorialHeadHit(session, out var fish) || fish == null)
            {
                return false;
            }

            if (fishing.TryHandleHeadHit(fish, out var slot))
            {
                world.AttachCaughtFish(fish, slot);
            }
            else
            {
                world.AttachCaughtFish(fish, Mathf.Max(0, session.CaughtCount - 1));
            }

            session.DescendAfterCatchMeters = 0f;
            view?.SetCaption(CastTutorialView.CaptionAscendCatch);
            return true;
        }
    }
}
