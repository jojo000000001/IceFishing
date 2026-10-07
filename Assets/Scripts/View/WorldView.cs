using IceFishing.Core;
using IceFishing.Model;
using UnityEngine;

namespace IceFishing.View
{
    /// <summary>
    /// 营地冰面固定。钩子挂在冰层下沿，点开始后镜头从营地沉到水下；之后钩子钉在屏幕上，只左右移动。
    /// </summary>
    public sealed class WorldView : MonoBehaviour
    {
        [SerializeField] Camera _worldCamera;
        [SerializeField] Sprite _hubBackground;
        [SerializeField] Transform _background;
        [SerializeField] CampField _camp;
        [SerializeField] Transform _hook;
        [SerializeField] GameObject _hookPrefab;
        Transform _hookCatchStack;
        [SerializeField] LineRenderer _line;
        [SerializeField] UnderwaterField _underwater;
        [SerializeField] FishField _fishField;
        [SerializeField] FishCatalog _fishCatalog;
        [SerializeField] AppScreen _screen;
        [SerializeField] Sprite _hookSprite;
        [Tooltip("由场景中 Hook 的世界 XY 自动同步；下落起点以场景/预制体摆位为准。")]
        public Vector2 hookDropPosition = new Vector2(0.08f, 6.5f);
        [Tooltip("钩子保护次数。下潜撞鱼头扣 1，用完后上浮。")]
        public int hookProtectionHits = 2;
        [Tooltip("钓绳长度（米）。到达最大深度后上浮。")]
        public float lineLengthMeters = 180f;
        [Tooltip("绳子/深度下潜速度（米/秒）。越大钩子沉得越快。")]
        public float descentMetersPerSecond = 8f;
        [Tooltip("到底或无保护罩挂鱼后的回拉速度（米/秒）。")]
        public float ascendCatchMetersPerSecond = 3.5f;
        [Tooltip("已满鱼回营时的上浮速度（米/秒）。")]
        public float ascentMetersPerSecond = 10f;
        [Tooltip("1 米深度对应的世界距离。越大镜头往下卷得越快。")]
        public float worldUnitsPerMeter = 0.42f;
        [Tooltip("水中同时存在的鱼数量上限。")]
        public int fishMaxAlive = 40;
        [Tooltip("沿深度至少每隔多少米有一条鱼。")]
        public float fishEveryMeters = 3f;
        [Tooltip("泳道垂直间距倍率。在 fishEveryMeters 基础上再放大高度间隔。")]
        public float fishLaneHeightScale = 1.65f;
        [Tooltip("距最大深度还剩多少米时镜头不再下沉，仅鱼线/钩子继续下潜。")]
        public float cameraStopBeforeMaxMeters = 5f;
        [Tooltip("所有上钩鱼嘴对齐的点（Hook 本地坐标：X 负=左，Y 负=下）。")]
        public Vector2 hookMouthAnchorLocal = new Vector2(-0.1f, -0.02f);
        float _lineWidth = 0.07f;
        float _lastHookX;
        float _lastDepthMeters;
        float _lastHookMouthX;
        bool _hookMouthVelReady;
        float _introT = 1f;
        float _introEndDepth;
        float _introEndSlope = 1f;
        bool _revealHook = true;
        bool _hookWarmed;
        bool _hookParked;
        bool _hideWorldLine = true;
        bool _joining;
        bool _rodLineActive;
        bool _tutorialCameraLocked;
        float _tutorialCameraDepth;
        float _joinT;
        Vector3 _joinFrom;
        float _joinX;
        Vector2 _dropStart;
        Color _hubSky = new Color(0.62f, 0.82f, 0.94f);
        Color _waterSky = new Color(0f, 0.455f, 0.757f);

        Sprite _square;
        Sprite _circle;
        Transform _hookShield;
        const float HookWorldHeight = 1f;
        const float HookWorldZ = -0.12f;
        const string HookSpritePath = "Assets/Art/Sprites/FishingAnchor.png";
        public const string DefaultHookPrefabPath = "Assets/Prefabs/World/FishingHook.prefab";
        public const string DefaultWorldPrefabPath = "Assets/Prefabs/World/IceFishingWorld.prefab";

        public void AssignRuntimeCamera(Camera worldCamera)
        {
            _worldCamera = worldCamera;
            if (_worldCamera == null)
            {
                return;
            }

            EnsureVisuals();
            ApplyScreen(_screen == AppScreen.Diving ? AppScreen.Hub : _screen);
        }

        public void Build(Camera worldCamera, Sprite hubBackground, Sprite hookSprite = null)
        {
            _worldCamera = worldCamera;
            _hubBackground = hubBackground;
            if (hookSprite != null)
            {
                _hookSprite = hookSprite;
            }

            ClearChildren();
            _background = null;
            _camp = null;
            _hook = null;
            _line = null;
            EnsureHookReference();
            if (_hook == null)
            {
                _hook = CreateSpriteObject("Hook", new Vector3(0f, 3.05f, -0.12f), Vector3.one, 20);
                _line = _hook.gameObject.AddComponent<LineRenderer>();
                SetupLine(_line);
            }

            _underwater = null;
            EnsureVisuals();
        }

        public static void SetupLineForEditor(LineRenderer line)
        {
            SetupLine(line);
        }

        public void EditorAssignCamp(CampField camp)
        {
            _camp = camp;
            if (camp != null)
            {
                _background = camp.transform;
            }
        }

        public void EditorAssignUnderwater(UnderwaterField underwater)
        {
            _underwater = underwater;
        }

        public void EditorAssignFish(FishField fishField, FishCatalog catalog)
        {
            _fishField = fishField;
            if (catalog != null)
            {
                _fishCatalog = catalog;
            }
        }

        void OnEnable()
        {
            if (_worldCamera == null)
            {
                return;
            }

            EnsureVisuals();
            ApplyScreen(_screen == AppScreen.Diving ? AppScreen.Hub : _screen);
        }

        void Awake()
        {
            EnsureHookReference();
            CaptureDropFromHook();
        }

        void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (_hook == null)
            {
                _hook = FindHookTransform();
            }

            if (_hook == null)
            {
                return;
            }

            if (_line == null)
            {
                _line = _hook.GetComponent<LineRenderer>();
            }

            SyncHookVisualInEditor();
        }

        void SyncHookVisualInEditor()
        {
            if (_hook == null)
            {
                return;
            }

            EnsureSprites();
            var hookSprite = ResolveHookSprite();
            Paint(_hook, hookSprite != null ? hookSprite : _square, hookSprite != null ? Color.white : UiTheme.Hook);
            FitHookScale();
            EnsureHookShield();
            UpdateHookShieldSize();
        }

        void LateUpdate()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (_screen == AppScreen.Hub)
            {
                LayoutWorld();
                LookAtHub();
                TickUnderwater();
                if (!_rodLineActive && !_hookParked)
                {
                    PlaceHubHook();
                }

                if (_line != null)
                {
                    _line.enabled = false;
                }

                return;
            }

            if (_screen == AppScreen.Fishing)
            {
                TickUnderwater();
                if (_introT < 1f)
                {
                    PlaceIntro(_lastHookX);
                }
                else
                {
                    PlaceFishingHook(_lastHookX, _lastDepthMeters);
                }
            }

            if (_hideWorldLine && _line != null)
            {
                _line.enabled = false;
            }
        }

        public void ApplyScreen(AppScreen screen)
        {
            _screen = screen;
            if (_worldCamera == null)
            {
                _worldCamera = Camera.main;
            }

            if (_worldCamera == null)
            {
                return;
            }

            _worldCamera.orthographic = true;
            _worldCamera.orthographicSize = CampField.WorldOrtho;
            _worldCamera.rect = new Rect(0f, 0f, 1f, 1f);
            LayoutWorld();
            if (screen == AppScreen.Hub)
            {
                CaptureDropFromHook();
                _introT = 1f;
                LookAtHub();
                _lastHookX = hookDropPosition.x;
                ShowHook();
                SyncHubHookFromScene();
            }
            else
            {
                if (_camp != null)
                {
                    _camp.gameObject.SetActive(true);
                }

                _lastHookX = hookDropPosition.x;
                ShowHook();
                if (_introT < 1f)
                {
                    PlaceIntro(_lastHookX);
                    PlaceHubHook();
                }
                else
                {
                    ApplyCastCamera(0f);
                    PlaceFishingHook(hookDropPosition.x, 0f);
                }

                if (_underwater != null)
                {
                    _underwater.SetVisible(true);
                    _underwater.SetIceVisible(true);
                    _underwater.Tick(_worldCamera);
                }
            }

            EnsureVisuals();
            if (screen == AppScreen.Hub)
            {
                UpdateHookShield(null);
            }
        }

        public void ApplyCast(IceFishing.Model.CastSession session)
        {
            if (session == null || _worldCamera == null)
            {
                return;
            }

            _lastHookX = session.HookX;
            _lastDepthMeters = session.Depth;
            if (_joining)
            {
                UpdateHookShield(session);
                TickUnderwater();
                return;
            }

            if (_introT >= 1f)
            {
                ApplyCastCamera(CameraFollowDepthMeters(session));
                PlaceFishingHook(session.HookX, session.Depth);
            }

            UpdateHookShield(session);
            TickUnderwater();
        }

        public void SetIntroEndSlope(float depthMetersPerSecond)
        {
            var camFrom = _camp != null ? _camp.HubCameraY : CampField.WorldOrtho * 1.2f;
            var camTo = -_introEndDepth * UnitsPerMeter;
            var travel = Mathf.Max(0.01f, camFrom - camTo);
            var cameraSpeed = Mathf.Max(0.1f, depthMetersPerSecond) * UnitsPerMeter;
            _introEndSlope = cameraSpeed * FishingRules.IntroDuration / travel;
        }

        public float CurrentIntroEase
        {
            get { return EaseIntoDescent(_introT, _introEndSlope); }
        }

        static float EaseIntoDescent(float linearT, float endSlope)
        {
            var s = Mathf.Clamp(endSlope, 0.05f, 2.5f);
            var t = Mathf.Clamp01(linearT);
            var a = 3f - s;
            var b = s - 2f;
            return (a + b * t) * t * t;
        }

        public void BeginDive(float endDepth)
        {
            _introEndDepth = Mathf.Max(0f, endDepth);
            _introT = 0f;
            _revealHook = false;
            _rodLineActive = false;
            CaptureDropFromHook();
            _dropStart = hookDropPosition;
            _lastHookX = _dropStart.x;
            ApplyHookReveal();
        }

        public bool IsLineJoining
        {
            get { return _joining; }
        }

        public float JoinHookX
        {
            get { return _joinX; }
        }

        public void SetWorldLineHidden(bool hidden)
        {
            _hideWorldLine = hidden;
            if (hidden && _line != null)
            {
                _line.enabled = false;
            }
        }

        public void SetLineWidth(float width)
        {
            _lineWidth = Mathf.Clamp(width, 0.02f, 0.25f);
            if (_line != null)
            {
                ApplyLineWidth(_line);
            }
        }

        void ApplyLineWidth(LineRenderer line)
        {
            line.startWidth = _lineWidth;
            line.endWidth = _lineWidth;
        }

        public void PrepareHook(Vector3 worldPoint)
        {
            EnsureHookReference();
            if (_hook == null)
            {
                return;
            }

            if (!_hook.gameObject.activeSelf)
            {
                _hook.gameObject.SetActive(true);
            }

            if (!_hookWarmed)
            {
                EnsureSprites();
                var hookSprite = ResolveHookSprite();
                Paint(_hook, hookSprite != null ? hookSprite : _square, hookSprite != null ? Color.white : UiTheme.Hook);
                FitHookScale();
                EnsureHookShield();
                WarmLine();
                _hookWarmed = true;
            }

            _hookParked = true;
            _hook.position = new Vector3(worldPoint.x, worldPoint.y, HookWorldZ);
            if (!_revealHook && !_rodLineActive)
            {
                var renderer = _hook.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.enabled = false;
                }

                if (_hookShield != null)
                {
                    _hookShield.gameObject.SetActive(false);
                }

                if (_line != null)
                {
                    _line.enabled = false;
                }
            }
        }

        void WarmLine()
        {
            if (_line == null)
            {
                return;
            }

            var shader = Shader.Find("Sprites/Default");
            if (shader != null && (_line.sharedMaterial == null || _line.sharedMaterial.shader != shader))
            {
                _line.material = new Material(shader);
            }

            ApplyLineWidth(_line);
            _line.startColor = Color.black;
            _line.endColor = Color.black;
            _line.sortingOrder = 20;
        }

        public void ShowRodLine(Vector3 lineEnd, float diveT, float hookT, bool showHook)
        {
            if (_worldCamera == null)
            {
                _worldCamera = Camera.main;
            }

            if (_line == null || _worldCamera == null)
            {
                EnsureHookReference();
            }

            if (_line == null || _worldCamera == null)
            {
                return;
            }

            WarmLine();
            _hookParked = false;
            _revealHook = showHook;
            _rodLineActive = true;
            var startY = lineEnd.y;
            var targetY = -Mathf.Max(_introEndDepth, FishingRules.IntroDepthMeters) * UnitsPerMeter
                + _worldCamera.orthographicSize * FishingRules.HookScreenY;
            var endY = Mathf.Lerp(lineEnd.y, targetY, Mathf.Clamp01(hookT));
            if (endY > startY)
            {
                endY = startY;
            }

            if (_hook != null)
            {
                _hook.gameObject.SetActive(true);
                _hook.position = new Vector3(lineEnd.x, endY, HookWorldZ);
                ApplyHookReveal();
            }

            _line.enabled = !_hideWorldLine;
            ApplyLineWidth(_line);
            _line.startColor = Color.black;
            _line.endColor = Color.black;
            _line.sortingOrder = 20;
            var attach = GetHookLineAttachWorld();
            _line.positionCount = 2;
            _line.SetPosition(0, new Vector3(attach.x, startY, attach.z));
            _line.SetPosition(1, attach);
            _lastHookX = lineEnd.x;
        }

        public void EndRodSplice()
        {
            _rodLineActive = false;
            _joining = false;
            _revealHook = true;
        }

        public void MoveExistingHookTo(Vector3 worldPoint)
        {
            if (_hook == null)
            {
                _hook = FindHookTransform();
            }

            if (_hook == null || _worldCamera == null)
            {
                return;
            }

            _joining = false;
            _revealHook = true;
            _rodLineActive = true;
            PlaceHookAt(worldPoint.x, worldPoint.y);
            ApplyHookReveal();
        }

        public bool TickLineJoin(float dt, out float depthMeters)
        {
            depthMeters = 0f;
            if (!_joining || _worldCamera == null)
            {
                return !_joining;
            }

            _joinT = Mathf.Clamp01(_joinT + dt / 0.85f);
            var targetY = -_introEndDepth * UnitsPerMeter + _worldCamera.orthographicSize * FishingRules.HookScreenY;
            var y = Mathf.Lerp(_joinFrom.y, targetY, Mathf.SmoothStep(0f, 1f, _joinT));
            PlaceHookAt(_joinX, y);
            if (_joinT < 1f)
            {
                return false;
            }

            _joining = false;
            _revealHook = true;
            ApplyHookReveal();
            depthMeters = Mathf.Max(0f, (_worldCamera.orthographicSize * FishingRules.HookScreenY - y) / UnitsPerMeter);
            return true;
        }

        public void RefreshDropFromSceneHook()
        {
            CaptureDropFromHook();
        }

        public void SetDiveT(float t)
        {
            _introT = Mathf.Clamp01(t);
            if (_worldCamera == null)
            {
                _worldCamera = Camera.main;
            }

            if (_worldCamera == null || _screen != AppScreen.Fishing)
            {
                return;
            }

            var eased = CurrentIntroEase;
            var camFrom = _camp != null ? _camp.HubCameraY : CampField.WorldOrtho * 1.2f;
            var camTo = -_introEndDepth * UnitsPerMeter;
            SetCameraY(Mathf.Lerp(camFrom, camTo, eased));
        }

        public void BeginFish()
        {
            EnsureFishField();
            if (_fishField == null)
            {
                return;
            }

            var waterline = _camp != null ? _camp.WaterlineY : CampField.WorldOrtho;
            _fishField.BeginCast(_worldCamera, waterline);
        }

        public void TickFish(float dt, CastSession session, bool paused)
        {
            if (_screen != AppScreen.Fishing)
            {
                return;
            }

            EnsureFishField();
            if (_fishField != null)
            {
                var hookY = _hook != null ? _hook.position.y : 0f;
                _fishField.Tick(
                    dt,
                    session,
                    paused,
                    hookY,
                    UnitsPerMeter,
                    FishMaxAlive,
                    FishEveryMeters,
                    FishLaneHeightScale);
                var mouth = GetHookMouthAnchorWorld();
                var hookVelX = 0f;
                if (_hookMouthVelReady && dt > 0f)
                {
                    hookVelX = (mouth.x - _lastHookMouthX) / dt;
                }

                _lastHookMouthX = mouth.x;
                _hookMouthVelReady = true;
                _fishField.TickHooked(dt, mouth, hookVelX, paused);
            }
        }

        public void ClearFish()
        {
            _hookMouthVelReady = false;
            if (_hookCatchStack != null)
            {
                for (var i = _hookCatchStack.childCount - 1; i >= 0; i--)
                {
                    _hookCatchStack.GetChild(i).SetParent(null, false);
                }
            }

            if (_fishField != null)
            {
                _fishField.Clear();
            }
        }

        public bool TryHookHeadHit(CastSession session, out FishView fish)
        {
            fish = null;
            if (_fishField == null || _hook == null || session == null)
            {
                return false;
            }

            if (session.Phase == CastPhase.Returning
                || session.CaughtCount >= session.Capacity
                || session.DescendAfterCatchMeters > 0f)
            {
                return false;
            }

            return _fishField.TryGetHeadHit(GetHookBodyCenterWorld(), FishingRules.HookHitRadius, out fish);
        }

        public void LockTutorialCamera(float depthMeters)
        {
            _tutorialCameraLocked = true;
            _tutorialCameraDepth = Mathf.Max(0f, depthMeters);
        }

        public void UnlockTutorialCamera()
        {
            _tutorialCameraLocked = false;
        }

        public void SyncTutorialCameraDepth(float depthMeters)
        {
            _tutorialCameraLocked = true;
            _tutorialCameraDepth = Mathf.Max(0f, depthMeters);
        }

        public float TutorialLockedCameraDepth
        {
            get { return _tutorialCameraDepth; }
        }

        public float GetHookScreenY()
        {
            return GetHookScreenPosition().y;
        }

        public Vector2 GetHookScreenPosition()
        {
            if (_hook == null || _worldCamera == null)
            {
                return Vector2.zero;
            }

            var p = _worldCamera.WorldToScreenPoint(_hook.position);
            return new Vector2(p.x, p.y);
        }

        public void SetTutorialSpawnFrozen(bool frozen)
        {
            EnsureFishField();
            if (_fishField != null)
            {
                _fishField.SetSpawnFrozen(frozen);
            }
        }

        public float ResolveTutorialDescendDepth(CastTutorialView view, CastSession session)
        {
            var fallback = session != null ? session.Depth + 10f : FishingRules.TutorialStartDepthMeters + 10f;
            if (view == null || !view.TryGetCatchHookScreen(out var hookScreen))
            {
                return ClampDepthInsideTutorialFrame(view, session, fallback, 0.55f);
            }

            var world = ScreenToWorld(hookScreen);
            var depth = DepthFromHookWorldY(world.y);
            if (session != null)
            {
                depth = Mathf.Clamp(depth, session.Depth + 2f, session.MaxDepth);
            }

            return ClampDepthInsideTutorialFrame(view, session, depth, 0.55f);
        }

        public float ClampDepthInsideTutorialFrame(
            CastTutorialView view,
            CastSession session,
            float depth,
            float extraBottomWorld = 0.55f)
        {
            if (view == null || _worldCamera == null || session == null)
            {
                return depth;
            }

            if (!view.TryGetHookScreenYRange(out var minScreenY, out var maxScreenY))
            {
                return depth;
            }

            var minWorld = ScreenToWorld(new Vector2(Screen.width * 0.5f, minScreenY));
            var maxWorld = ScreenToWorld(new Vector2(Screen.width * 0.5f, maxScreenY));
            var hookFloorY = minWorld.y + extraBottomWorld;
            var hookCeilY = maxWorld.y - 0.35f;
            var maxDepth = DepthFromHookWorldY(hookFloorY);
            var minDepth = DepthFromHookWorldY(hookCeilY);
            var lo = Mathf.Min(minDepth, maxDepth);
            var hi = Mathf.Max(minDepth, maxDepth);
            return Mathf.Clamp(depth, Mathf.Max(0f, lo), Mathf.Min(session.MaxDepth, hi));
        }

        public bool TryTutorialHeadHit(CastSession session, out FishView fish)
        {
            fish = null;
            if (_fishField == null || _hook == null || session == null)
            {
                return false;
            }

            return _fishField.TryGetHeadHit(GetHookBodyCenterWorld(), FishingRules.HookHitRadius * 1.7f, out fish);
        }

        Vector3 ScreenToWorld(Vector2 screen)
        {
            var distance = Mathf.Abs(_worldCamera.transform.position.z + HookWorldZ);
            var world = _worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, distance));
            world.z = 0f;
            return world;
        }

        float DepthFromHookWorldY(float hookY)
        {
            return Mathf.Max(0f, (_worldCamera.orthographicSize * FishingRules.HookScreenY - hookY) / UnitsPerMeter);
        }

        public float DepthForHookWorldY(float hookWorldY)
        {
            return DepthFromHookWorldY(hookWorldY);
        }

        public Vector3 ScreenPointToHookWorld(Vector2 screen)
        {
            return ScreenToWorld(screen);
        }

        public FishView SpawnTutorialFishAtHook(CastSession session)
        {
            if (_hook == null || session == null)
            {
                return null;
            }

            EnsureFishField();
            if (_fishField == null)
            {
                return null;
            }

            var below = _hook.position.y - Mathf.Max(0.35f, 0.55f);
            return _fishField.SpawnTutorialAtHook(session, session.HookX, below);
        }

        public FishView SpawnTutorialBaitBelowHook(CastSession session, float belowWorld)
        {
            if (session == null)
            {
                return null;
            }

            EnsureHookReference();
            EnsureFishField();
            if (_fishField == null)
            {
                return null;
            }

            var waterline = _camp != null ? _camp.WaterlineY : CampField.WorldOrtho;
            _fishField.EnsureCamera(_worldCamera != null ? _worldCamera : Camera.main, waterline);
            ApplyCast(session);
            var body = GetHookBodyCenterWorld();
            var y = body.y - Mathf.Max(0.35f, belowWorld);
            var travel = FishingRules.TutorialHoldSeconds + FishingRules.TutorialDropSeconds;
            return _fishField.SpawnTutorialOncoming(session, body.x, y, travel);
        }

        public float MetersForWorldDrop(float worldDistance)
        {
            return Mathf.Max(0.05f, worldDistance) / UnitsPerMeter;
        }

        public FishView SpawnTutorialOncomingFish(CastSession session, float travelSeconds)
        {
            if (session == null)
            {
                return null;
            }

            EnsureHookReference();
            EnsureFishField();
            if (_fishField == null)
            {
                return null;
            }

            var waterline = _camp != null ? _camp.WaterlineY : CampField.WorldOrtho;
            _fishField.EnsureCamera(_worldCamera != null ? _worldCamera : Camera.main, waterline);
            ApplyCast(session);
            var body = GetHookBodyCenterWorld();
            return _fishField.SpawnTutorialOncoming(session, body.x, body.y, travelSeconds);
        }

        public void SpawnTutorialAmbientSchool(CastSession session, int count)
        {
            if (session == null)
            {
                return;
            }

            EnsureHookReference();
            EnsureFishField();
            if (_fishField == null)
            {
                return;
            }

            var waterline = _camp != null ? _camp.WaterlineY : CampField.WorldOrtho;
            _fishField.EnsureCamera(_worldCamera != null ? _worldCamera : Camera.main, waterline);
            ApplyCast(session);
            float hookX;
            float hookY;
            ResolveTutorialHookLane(session, out hookX, out hookY);
            _fishField.SpawnTutorialSchoolBelow(session, hookY - FishingRules.TutorialBaitBelowWorld, count);
        }

        public void SpawnTutorialRiseSchool(CastSession session, float riseMeters, int count)
        {
            if (session == null)
            {
                return;
            }

            EnsureHookReference();
            EnsureFishField();
            if (_fishField == null)
            {
                return;
            }

            var waterline = _camp != null ? _camp.WaterlineY : CampField.WorldOrtho;
            _fishField.EnsureCamera(_worldCamera != null ? _worldCamera : Camera.main, waterline);
            ApplyCast(session);
            float hookX;
            float hookY;
            ResolveTutorialHookLane(session, out hookX, out hookY);
            _fishField.SpawnTutorialSchoolAbove(session, hookY, riseMeters * UnitsPerMeter, count);
        }

        void ResolveTutorialHookLane(CastSession session, out float hookX, out float hookY)
        {
            hookX = session != null ? session.HookX : 0f;
            hookY = _hook != null ? _hook.position.y : GetHookBodyCenterWorld().y;
            if (_worldCamera == null)
            {
                return;
            }

            var camY = _worldCamera.transform.position.y;
            var ortho = _worldCamera.orthographicSize;
            var expectedY = camY + ortho * FishingRules.HookScreenY;
            if (hookY < camY - ortho + 0.2f || hookY > camY + ortho - 0.2f)
            {
                hookY = expectedY;
            }
        }

        public static bool TutorialFishPassedHook(FishView fish, CastSession session)
        {
            if (fish == null || session == null)
            {
                return false;
            }

            if (fish.Consumed || fish.Hooked)
            {
                return true;
            }

            var headX = fish.Head != null ? fish.Head.bounds.center.x : fish.transform.position.x;
            return fish.Dir * (headX - session.HookX) > 0.45f;
        }

        public void AttachCaughtFish(FishView fish, int slotIndex)
        {
            if (fish == null || _hook == null)
            {
                return;
            }

            EnsureFishField();
            EnsureHookCatchStack();
            if (_fishField != null)
            {
                _fishField.RegisterHooked(fish);
            }

            fish.BeginHookTo(_hookCatchStack, GetHookMouthAnchorWorld(), 22 + slotIndex);
        }

        public Vector3 GetHookMouthAnchorWorld()
        {
            if (_hook == null)
            {
                return Vector3.zero;
            }

            return _hook.TransformPoint(new Vector3(hookMouthAnchorLocal.x, hookMouthAnchorLocal.y, 0f));
        }

        Vector3 GetHookTipWorld()
        {
            if (_hook == null)
            {
                return Vector3.zero;
            }

            var renderer = _hook.GetComponent<SpriteRenderer>();
            if (renderer != null && renderer.sprite != null)
            {
                var bounds = renderer.sprite.bounds;
                return _hook.TransformPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
            }

            return _hook.position + Vector3.down * (HookWorldHeight * 0.5f * Mathf.Abs(_hook.lossyScale.y));
        }

        Sprite ResolveHookSpriteForAnchor()
        {
            var renderer = _hook != null ? _hook.GetComponent<SpriteRenderer>() : null;
            if (renderer != null && renderer.sprite != null)
            {
                return renderer.sprite;
            }

            return ResolveHookSprite();
        }

        Vector3 GetHookBodyCenterWorld()
        {
            if (_hook == null)
            {
                return Vector3.zero;
            }

            var sprite = ResolveHookSpriteForAnchor();
            return _hook.TransformPoint(HookVisualAnchor.GetBodyCenterLocal(sprite));
        }

        Vector3 GetHookLineAttachWorld()
        {
            if (_hook == null)
            {
                return Vector3.zero;
            }

            var sprite = ResolveHookSpriteForAnchor();
            return _hook.TransformPoint(HookVisualAnchor.GetLineAttachLocal(sprite));
        }

        void EnsureHookCatchStack()
        {
            if (_hook == null)
            {
                return;
            }

            if (_hookCatchStack != null)
            {
                return;
            }

            var go = new GameObject("HookCatchStack");
            _hookCatchStack = go.transform;
            _hookCatchStack.SetParent(_hook, false);
            _hookCatchStack.localPosition = Vector3.zero;
            _hookCatchStack.localRotation = Quaternion.identity;
            _hookCatchStack.localScale = Vector3.one;
        }

        public float HubCameraHeight
        {
            get { return _camp != null ? _camp.HubCameraY : CampField.WorldOrtho * 1.2f; }
        }

        public Camera WorldCamera
        {
            get { return _worldCamera; }
        }

        public float UnitsPerMeter
        {
            get { return Mathf.Max(0.05f, worldUnitsPerMeter); }
        }

        public float WorldCameraOrthographicSize
        {
            get { return _worldCamera != null ? _worldCamera.orthographicSize : CampField.WorldOrtho; }
        }

        public float DescentMetersPerSecond
        {
            get { return Mathf.Max(0.1f, descentMetersPerSecond); }
        }

        public float AscendCatchMetersPerSecond
        {
            get { return Mathf.Max(0.1f, ascendCatchMetersPerSecond); }
        }

        public float AscentMetersPerSecond
        {
            get { return Mathf.Max(0.1f, ascentMetersPerSecond); }
        }

        public int FishMaxAlive
        {
            get { return Mathf.Max(1, fishMaxAlive); }
        }

        public float FishEveryMeters
        {
            get { return Mathf.Max(0.5f, fishEveryMeters); }
        }

        public float FishLaneHeightScale
        {
            get { return Mathf.Max(0.5f, fishLaneHeightScale); }
        }

        void ApplyCastCamera(float depth)
        {
            SetCameraY(-depth * UnitsPerMeter);
        }

        float CameraFollowDepthMeters(CastSession session)
        {
            if (_tutorialCameraLocked)
            {
                return _tutorialCameraDepth;
            }

            if (session == null)
            {
                return 0f;
            }

            var stop = Mathf.Max(0f, cameraStopBeforeMaxMeters);
            var cap = Mathf.Max(0f, session.MaxDepth - stop);
            return Mathf.Min(session.Depth, cap);
        }

        public void GetHookLane(out float minX, out float maxX)
        {
            if (_worldCamera == null)
            {
                minX = -4f;
                maxX = 4f;
                return;
            }

            var aspect = CampField.ViewAspect(_worldCamera.aspect);
            var half = _worldCamera.orthographicSize * aspect * FishingRules.LaneScreenFraction;
            minX = -half;
            maxX = half;
        }

        void LayoutWorld()
        {
            EnsureCamp();
            if (_worldCamera == null)
            {
                return;
            }

            var aspect = CampField.ViewAspect(_worldCamera.aspect);
            if (_camp != null)
            {
                _camp.gameObject.SetActive(true);
                _camp.Place(aspect);
            }

            if (_underwater == null)
            {
                return;
            }

            var waterline = _camp != null ? _camp.WaterlineY : CampField.WorldOrtho;
            _underwater.SetVisible(true);
            _underwater.SetIceVisible(true);
            _underwater.PlaceColumn(0f, waterline + 0.12f, _worldCamera);
            _underwater.Tick(_worldCamera);
        }

        void EnsureCamp()
        {
            if (_camp == null)
            {
                _camp = GetComponentInChildren<CampField>(true);
            }

            if (_camp == null && _background != null)
            {
                _camp = _background.GetComponent<CampField>();
                if (_camp == null)
                {
                    _camp = _background.gameObject.AddComponent<CampField>();
                }
            }

            if (_camp == null && _hubBackground != null)
            {
                var go = new GameObject("CampField");
                go.transform.SetParent(transform, false);
                var spriteRenderer = go.AddComponent<SpriteRenderer>();
                spriteRenderer.sortingOrder = 5;
                _camp = go.AddComponent<CampField>();
                _background = go.transform;
            }

            if (_camp != null)
            {
                _background = _camp.transform;
                _camp.Configure(_hubBackground);
            }
        }

        void EnsureFishField()
        {
            if (_fishField == null)
            {
                _fishField = GetComponentInChildren<FishField>(true);
            }

            if (_fishField == null)
            {
                var go = new GameObject("FishField");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Vector3.zero;
                _fishField = go.AddComponent<FishField>();
            }

            if (_fishCatalog == null)
            {
#if UNITY_EDITOR
                _fishCatalog = UnityEditor.AssetDatabase.LoadAssetAtPath<FishCatalog>(FishCatalog.AssetPath);
#endif
            }

            if (_fishField != null)
            {
                _fishField.Configure(_fishCatalog);
            }
        }

        void TickUnderwater()
        {
            if (_underwater != null && _worldCamera != null)
            {
                _underwater.Tick(_worldCamera);
            }
        }

        void EnsureVisuals()
        {
            EnsureHookReference();
            EnsureSprites();
            var hookSprite = ResolveHookSprite();
            Paint(_hook, hookSprite != null ? hookSprite : _square, hookSprite != null ? Color.white : UiTheme.Hook);
            FitHookScale();
            EnsureHookShield();
            if (_line != null)
            {
                var showLine = !_hideWorldLine
                    && (_rodLineActive
                    || (_screen == AppScreen.Fishing && (_revealHook || _joining) && _hook != null && _hook.gameObject.activeSelf));
                _line.enabled = showLine;
            }
        }

        public void SetHookRevealed(bool revealed)
        {
            _revealHook = revealed;
            ApplyHookReveal();
        }

        void ShowHook()
        {
            if (_hook != null)
            {
                _hook.gameObject.SetActive(true);
                ApplyHookReveal();
            }
        }

        void ApplyHookReveal()
        {
            if (_hook == null)
            {
                return;
            }

            var renderer = _hook.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.enabled = _revealHook;
            }

            if (!_revealHook && _hookShield != null)
            {
                _hookShield.gameObject.SetActive(false);
            }
        }

        void EnsureHookReference()
        {
            if (_hook != null)
            {
                if (_line == null)
                {
                    _line = _hook.GetComponent<LineRenderer>();
                }

                return;
            }

            _hook = FindHookTransform();
            if (_hook != null)
            {
                if (_line == null)
                {
                    _line = _hook.GetComponent<LineRenderer>();
                }

                return;
            }

            var prefab = ResolveHookPrefabAsset();
            if (prefab == null)
            {
                return;
            }

            var instance = Instantiate(prefab, transform);
            instance.name = "Hook";
            _hook = instance.transform;
            _line = _hook.GetComponent<LineRenderer>();
        }

        Transform FindHookTransform()
        {
            var direct = transform.Find("Hook");
            if (direct != null)
            {
                return direct;
            }

            var hooks = transform.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < hooks.Length; i++)
            {
                if (hooks[i] != null && hooks[i].name == "Hook")
                {
                    return hooks[i];
                }
            }

            return null;
        }

        GameObject ResolveHookPrefabAsset()
        {
            if (_hookPrefab != null)
            {
                return _hookPrefab;
            }

#if UNITY_EDITOR
            _hookPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(DefaultHookPrefabPath);
            return _hookPrefab;
#else
            return null;
#endif
        }

        void CaptureDropFromHook()
        {
            EnsureHookReference();
            if (_hook == null)
            {
                return;
            }

            _hook.position = new Vector3(hookDropPosition.x, hookDropPosition.y, HookWorldZ);
            hookDropPosition = new Vector2(_hook.position.x, _hook.position.y);
            _dropStart = hookDropPosition;
        }

        void LookAtHub()
        {
            var hubY = _camp != null ? _camp.HubCameraY : CampField.WorldOrtho * 1.2f;
            SetCameraY(hubY);
            _worldCamera.backgroundColor = _hubSky;
        }

        void SetCameraY(float y)
        {
            _worldCamera.orthographicSize = CampField.WorldOrtho;
            _worldCamera.transform.position = new Vector3(0f, y, -10f);
            if (_screen == AppScreen.Fishing)
            {
                _worldCamera.backgroundColor = _waterSky;
                return;
            }

            var waterline = _camp != null ? _camp.WaterlineY : CampField.WorldOrtho;
            var ortho = _worldCamera.orthographicSize;
            var mix = Mathf.InverseLerp(waterline, waterline - ortho, y + ortho);
            _worldCamera.backgroundColor = Color.Lerp(_hubSky, _waterSky, mix);
        }

        void PlaceHubHook()
        {
            SyncHubHookFromScene();
        }

        void SyncHubHookFromScene()
        {
            CaptureDropFromHook();
            if (_hook == null || _worldCamera == null)
            {
                return;
            }

            FitHookScale();
            UpdateHookShieldSize();
            RefreshHookLine();
        }

        void RefreshHookLine()
        {
            if (_line == null || _hook == null || _worldCamera == null)
            {
                return;
            }

            if (_hideWorldLine || (_screen != AppScreen.Fishing && !_rodLineActive))
            {
                _line.enabled = false;
                return;
            }

            var shader = Shader.Find("Sprites/Default");
            if (shader != null && (_line.sharedMaterial == null || _line.sharedMaterial.shader != shader))
            {
                _line.material = new Material(shader);
            }

            var attach = GetHookLineAttachWorld();
            var waterline = _camp != null ? _camp.WaterlineY : CampField.WorldOrtho;
            var viewTop = _worldCamera.transform.position.y + _worldCamera.orthographicSize;
            var topY = Mathf.Min(viewTop, waterline);
            var x = _joining ? _joinX : attach.x;
            var endY = attach.y;
            var startY = _joining ? Mathf.Max(topY, _joinFrom.y) : topY;
            if (!_revealHook && !_joining)
            {
                _line.enabled = false;
                return;
            }

            _line.enabled = true;
            ApplyLineWidth(_line);
            _line.startColor = Color.black;
            _line.endColor = Color.black;
            _line.sortingOrder = 20;
            _line.positionCount = 2;
            _line.SetPosition(0, new Vector3(x, startY, attach.z));
            _line.SetPosition(1, new Vector3(x, endY, attach.z));
        }

        void PlaceIntro(float hookX)
        {
            if (_worldCamera == null)
            {
                return;
            }

            var t = CurrentIntroEase;
            var camFrom = _camp != null ? _camp.HubCameraY : CampField.WorldOrtho * 1.2f;
            var camTo = -_introEndDepth * UnitsPerMeter;
            SetCameraY(Mathf.Lerp(camFrom, camTo, t));
            if (_rodLineActive)
            {
                return;
            }
            var hookTo = camTo + _worldCamera.orthographicSize * IceFishing.Model.FishingRules.HookScreenY;
            PlaceHookAt(hookX, Mathf.Lerp(_dropStart.y, hookTo, t));
        }

        void PlaceFishingHook(float hookX, float depthMeters)
        {
            if (_worldCamera == null)
            {
                return;
            }

            var hookY = -depthMeters * UnitsPerMeter +
                _worldCamera.orthographicSize * FishingRules.HookScreenY;
            PlaceHookAt(hookX, hookY);
        }

        void PlaceHookAt(float hookX, float hookY)
        {
            if (_hook == null || _worldCamera == null)
            {
                return;
            }

            var hookPos = new Vector3(hookX, hookY, HookWorldZ);
            _hook.position = hookPos;
            FitHookScale();
            UpdateHookShieldSize();
            RefreshHookLine();
        }

        void FitHookScale()
        {
            if (_hook == null)
            {
                return;
            }

            var renderer = _hook.GetComponent<SpriteRenderer>();
            var sprite = renderer != null ? renderer.sprite : null;
            if (sprite == null)
            {
                sprite = ResolveHookSprite();
            }

            HookVisualScale.Apply(_hook, sprite, HookWorldHeight);
        }

        Sprite ResolveHookSprite()
        {
            if (_hookSprite != null)
            {
                return _hookSprite;
            }

#if UNITY_EDITOR
            _hookSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(HookSpritePath);
#endif
            return _hookSprite;
        }

        void EnsureSprites()
        {
            if (_square == null)
            {
                _square = MakeSprite(false);
            }

            if (_circle == null)
            {
                _circle = HookPrototypeSprites.Circle;
            }
        }

        void EnsureHookShield()
        {
            if (_hook == null)
            {
                return;
            }

            if (_hookShield == null)
            {
                _hookShield = _hook.Find("HookShield");
            }

            if (_hookShield == null)
            {
                _hookShield = HookShieldSetup.EnsureChild(_hook);
            }
            else
            {
                HookShieldSetup.ApplyLayout(_hook, _hookShield);
            }
        }

        void UpdateHookShield(CastSession session)
        {
            EnsureHookShield();
            if (_hookShield == null)
            {
                return;
            }

            var show = _revealHook
                && _screen == AppScreen.Fishing
                && session != null
                && session.ProtectionLeft > 0
                && session.Phase == CastPhase.Descending;
            _hookShield.gameObject.SetActive(show);
            if (show)
            {
                UpdateHookShieldSize();
            }
        }

        void UpdateHookShieldSize()
        {
            if (_hookShield == null || _hook == null)
            {
                return;
            }

            HookShieldSetup.ApplyLayout(_hook, _hookShield);
        }

        Transform CreateSpriteObject(string objectName, Vector3 position, Vector3 scale, int order)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = order;
            return go.transform;
        }

        void Paint(Transform target, Sprite sprite, Color color)
        {
            if (target == null)
            {
                return;
            }

            var spriteRenderer = target.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
        }

        static void SetupLine(LineRenderer line)
        {
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = 0.07f;
            line.endWidth = line.startWidth;
            line.numCapVertices = 2;
            line.textureMode = LineTextureMode.Stretch;
            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                line.material = new Material(shader);
            }

            line.startColor = Color.black;
            line.endColor = Color.black;
        }

        void ClearChildren()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        static Sprite MakeSprite(bool circle)
        {
            return circle ? HookPrototypeSprites.Circle : MakeSquareSprite();
        }

        static Sprite MakeSquareSprite()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            texture.filterMode = FilterMode.Bilinear;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, Color.white);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
