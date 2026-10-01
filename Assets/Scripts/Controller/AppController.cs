using IceFishing.Core;
using IceFishing.Model;
using IceFishing.View;
using UnityEngine;
using UnityEngine.UI;

namespace IceFishing.Controller
{
    /// <summary>
    /// 应用入口：注册服务、打开营地。钩子挂在冰层下沿，点开始后镜头从营地往下沉。
    /// 不绘制 UI，也不在 M1 消耗饵料。
    /// </summary>
    public sealed class AppController : MonoBehaviour
    {
        [SerializeField] HubView _hubView;
        [SerializeField] FishingHudView _hudView;
        [SerializeField] PausePopupView _pauseView;
        [SerializeField] OverlayView _overlayView;
        [SerializeField] GameObject _worldPrefab;
        [SerializeField] WorldView _worldView;
        WorldView _spawnedWorld;

        PlayerProfile _profile;
        GameEventBus _bus;
        BaitRegen _regen;
        HubController _hubController;
        FishingController _fishingController;
        AppScreen _screen;
        float _introT = 1f;
        float _introFromDepth;
        float _introToDepth;
        float _fishingHookEase = -1f;
        float _lineDrop;
        bool _campLineOff;
        ScreenFlashView _screenFlash;

        public void EditorAssign(
            HubView hubView,
            FishingHudView hudView,
            PausePopupView pauseView,
            OverlayView overlayView,
            WorldView worldView,
            GameObject worldPrefab = null)
        {
            _hubView = hubView;
            _hudView = hudView;
            _pauseView = pauseView;
            _overlayView = overlayView;
            _worldView = worldView;
            if (worldPrefab != null)
            {
                _worldPrefab = worldPrefab;
            }
        }

        void Awake()
        {
            EnsureWorldFromPrefab();
            ServiceLocator.Clear();
            _bus = new GameEventBus();
            _profile = PlayerProfile.CreateNew();
            _regen = new BaitRegen();
            ServiceLocator.Register(_bus);
            ServiceLocator.Register(_profile);

            ApplyFonts();
            EnsureScreenFlash();
            _hubController = new HubController(_hubView, _overlayView, _profile, StartFishing, RefreshHub);
            _fishingController = new FishingController(_hudView, _pauseView, ReturnToHub);
            ShowHub();
        }

        void Update()
        {
            if (_screen == AppScreen.Hub)
            {
                _regen.Tick(Time.deltaTime, _profile);
                RefreshHub();
                ParkHookUnderLine();
                _worldView.SetWorldLineHidden(true);
                return;
            }

            if (_screen != AppScreen.Fishing || _fishingController.Session == null)
            {
                return;
            }

            _worldView.SetLineWidth(_hubView.UnderwaterLineWidth);
            _worldView.GetHookLane(out var minX, out var maxX);
            float? targetX = null;
            if (!_fishingController.IsPaused && PointerSteerInput.TryGetWorldX(Camera.main, out var pointerX))
            {
                targetX = pointerX;
            }

            var dt = Time.deltaTime;
            var paused = _fishingController.IsPaused;
            var session = _fishingController.Session;
            var x = targetX.HasValue ? targetX.Value : session.HookX;
            if (!paused && _introT < 1f)
            {
                TickIntro(dt, x, minX, maxX);
            }
            else if (!paused && _worldView.IsLineJoining)
            {
                float depth;
                if (_worldView.TickLineJoin(dt, out depth))
                {
                    session.Depth = depth;
                    session.HookX = _worldView.JoinHookX;
                }
            }
            else if (!paused)
            {
                _fishingController.Tick(dt, minX, maxX, targetX);
                if (session.Phase == CastPhase.Settle)
                {
                    ReturnToHub();
                    return;
                }
            }

            _worldView.ApplyCast(session);
            if (!_worldView.IsLineJoining)
            {
                if (_introT >= 1f && !paused && _worldView.TryHookHeadHit(session, out var hit))
                {
                    var protectionBefore = session.ProtectionLeft;
                    if (_fishingController.TryHandleHeadHit(hit, out var catchSlot))
                    {
                        _worldView.AttachCaughtFish(hit, catchSlot);
                    }

                    if (protectionBefore > session.ProtectionLeft)
                    {
                        PlayProtectionScreenFlash();
                    }
                }

                _worldView.TickFish(dt, session, paused);
            }

            TickFishingUiFade();
        }

        void OnDestroy()
        {
            ServiceLocator.Clear();
            if (_spawnedWorld != null)
            {
                Destroy(_spawnedWorld.gameObject);
                _spawnedWorld = null;
                _worldView = null;
            }
        }

        void EnsureWorldFromPrefab()
        {
            if (_worldView != null)
            {
                return;
            }

            if (_worldPrefab == null)
            {
                Debug.LogError("AppController: assign IceFishingWorld prefab on Bootstrap (_worldPrefab).");
                return;
            }

            var instance = Instantiate(_worldPrefab);
            instance.name = "World";
            _worldView = instance.GetComponent<WorldView>();
            if (_worldView == null)
            {
                Debug.LogError("IceFishingWorld prefab is missing WorldView.");
                Destroy(instance);
                return;
            }

            _spawnedWorld = _worldView;
            var camera = Camera.main;
            if (camera != null)
            {
                _worldView.AssignRuntimeCamera(camera);
            }
        }

        void StartFishing()
        {
            if (_screen == AppScreen.Fishing)
            {
                return;
            }

            _overlayView.Hide();
            _hubView.SetInteractable(false);
            _hubView.SetFade(1f);
            var protection = _worldView != null ? _worldView.hookProtectionHits : 2;
            var lineMeters = _worldView != null ? _worldView.lineLengthMeters : 180f;
            var descent = _worldView != null ? _worldView.DescentMetersPerSecond : FishingRules.DescentMetersPerSecond;
            var ascendCatch = _worldView != null
                ? _worldView.AscendCatchMetersPerSecond
                : descent;
            var ascent = _worldView != null ? _worldView.AscentMetersPerSecond : FishingRules.AscentMetersPerSecond;
            _fishingController.Enter(_profile, protection, lineMeters, descent, ascendCatch, ascent);
            var session = _fishingController.Session;
            _introFromDepth = session != null ? session.Depth : 0f;
            var introCap = session != null ? session.MaxDepth : lineMeters;
            _introToDepth = Mathf.Min(Mathf.Max(_introFromDepth, FishingRules.IntroDepthMeters), introCap);
            _introT = _introToDepth <= _introFromDepth + 0.01f ? 1f : 0f;
            _fishingHookEase = -1f;
            _fishingHookEase = -1f;
            _lineDrop = 0f;
            _campLineOff = false;
            _hubView.DropLineAndHook(0f);
            _hudView.SetFade(0f);
            _worldView.RefreshDropFromSceneHook();
            if (_introT < 1f)
            {
                _worldView.BeginDive(_introToDepth);
                _worldView.SetIntroEndSlope(_worldView.DescentMetersPerSecond);
            }
            else
            {
                _hubView.Hide();
                _worldView.SetHookRevealed(true);
            }

            if (session != null)
            {
                session.HookX = _worldView.hookDropPosition.x;
            }

            _screen = AppScreen.Fishing;
            _worldView.ApplyScreen(AppScreen.Fishing);
            _worldView.ApplyCast(_fishingController.Session);
            _worldView.BeginFish();
            _bus.RaiseScreenChanged(_screen);
        }

        void TickIntro(float dt, float targetX, float minX, float maxX)
        {
            var session = _fishingController.Session;
            if (session == null)
            {
                _introT = 1f;
                return;
            }

            session.Steer(dt, targetX, minX, maxX);
            _introT = Mathf.Clamp01(_introT + dt / FishingRules.IntroDuration);
            _worldView.SetDiveT(_introT);
            var t = _worldView.CurrentIntroEase;
            session.Depth = Mathf.Lerp(_introFromDepth, _introToDepth, t);
            session.Phase = CastPhase.Descending;
            if (!_campLineOff)
            {
                _lineDrop += Screen.height * dt * 1.8f;
            }

            _hubView.DropLineAndHook(_lineDrop);
            _hubView.SetDiveAmount(t);
            var fromY = _worldView.HubCameraHeight;
            var nowY = Mathf.Lerp(fromY, -_introToDepth * _worldView.UnitsPerMeter, t);
            _hubView.MatchCameraDrop(fromY, nowY, CampField.WorldOrtho * 2f);
            _hubView.SetFade(1f - Mathf.Clamp01((t - 0.72f) / 0.28f));
            if (!_campLineOff && (_hubView.IsLineHookBelowScreen() || _introT >= 1f))
            {
                _campLineOff = true;
                _fishingHookEase = t;
            }

            if (!_campLineOff)
            {
                ParkHookUnderLine();
            }
            else
            {
                SyncRodLine(t, Mathf.InverseLerp(_fishingHookEase, 1f, t), true);
            }
            if (_introT >= 1f)
            {
                _hubView.SetDiveAmount(1f);
                _worldView.EndRodSplice();
                _hubView.Hide();
                _worldView.SetWorldLineHidden(false);
            }
            else
            {
                _worldView.SetWorldLineHidden(!_campLineOff);
            }

            _hudView.Bind(session);
        }

        void LateUpdate()
        {
            if (_screen == AppScreen.Hub)
            {
                ParkHookUnderLine();
            }
        }

        void ParkHookUnderLine()
        {
            if (_worldView == null || _hubView == null || !_hubView.TryGetLineTip(out var tip))
            {
                return;
            }

            _worldView.PrepareHook(CanvasToWorld(tip));
        }

        void SyncRodLine(float diveT, float hookT, bool showHook)
        {
            if (!_hubView.TryGetLineTip(out var lineTip))
            {
                return;
            }

            var lineEnd = CanvasToWorld(lineTip);
            var session = _fishingController.Session;
            if (session != null)
            {
                session.HookX = lineEnd.x;
            }

            _worldView.ShowRodLine(lineEnd, diveT, hookT, showHook);
        }

        Vector3 CanvasToWorld(Vector3 canvasWorld)
        {
            var canvas = _hubView.GetComponentInParent<Canvas>();
            Camera uiCamera = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                uiCamera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            }

            var screen = RectTransformUtility.WorldToScreenPoint(uiCamera, canvasWorld);
            var worldCamera = Camera.main;
            if (worldCamera == null)
            {
                return canvasWorld;
            }

            var distance = Mathf.Abs(worldCamera.transform.position.z + 0.12f);
            return worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, distance));
        }

        void TickFishingUiFade()
        {
            var session = _fishingController.Session;
            if (session == null)
            {
                return;
            }

            var depth = session.Depth;
            _hudView.SetFade(Mathf.InverseLerp(0f, FishingRules.IntroDepthMeters * 0.45f, depth));
        }

        void ReturnToHub()
        {
            _fishingController.Exit();
            _introT = 1f;
            _worldView.ClearFish();
            ShowHub();
        }

        void ShowHub()
        {
            _hudView.Hide();
            _pauseView.Hide();
            _overlayView.Hide();
            _worldView.ClearFish();
            _worldView.ApplyScreen(AppScreen.Hub);
            _worldView.SetWorldLineHidden(true);
            _lineDrop = 0f;
            _campLineOff = false;
            _fishingHookEase = -1f;
            _hubView.Show();
            _worldView.SetHookRevealed(false);
            _screen = AppScreen.Hub;
            RefreshHub();
            _bus.RaiseScreenChanged(_screen);
        }

        void RefreshHub()
        {
            _hubView.Refresh(_profile, _regen.SecondsUntilNext);
            _bus.RaiseProfileChanged(_profile);
        }

        void ApplyFonts()
        {
            ApplyFontOn(_hubView);
            ApplyFontOn(_hudView);
            ApplyFontOn(_pauseView);
            ApplyFontOn(_overlayView);
        }

        static void ApplyFontOn(Component root)
        {
            if (root == null)
            {
                return;
            }

            var font = UiFactory.ResolveFont();
            var texts = root.GetComponentsInChildren<Text>(true);
            for (var i = 0; i < texts.Length; i++)
            {
                texts[i].font = font;
            }
        }

        void EnsureScreenFlash()
        {
            if (_screenFlash != null || _hubView == null)
            {
                return;
            }

            var canvas = _hubView.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            _screenFlash = ScreenFlashView.Ensure(canvas.transform);
        }

        void PlayProtectionScreenFlash()
        {
            EnsureScreenFlash();
            if (_screenFlash != null)
            {
                _screenFlash.PlayLightningFlash();
            }
        }
    }
}
