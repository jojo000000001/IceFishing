using System.Collections.Generic;
using IceFishing.Core;
using IceFishing.Model;
using UnityEngine;

namespace IceFishing.View
{
    /// <summary>
    /// 水里的鱼：对象池、按深度带从左右刷、只水平游。碰撞语义在 Controller。
    /// </summary>
    public sealed class FishField : MonoBehaviour
    {
        [SerializeField] FishCatalog _catalog;

        readonly List<FishView> _alive = new List<FishView>();
        readonly List<FishView> _hooked = new List<FishView>();
        readonly List<FishDefinition> _scratch = new List<FishDefinition>();
        readonly Dictionary<int, Stack<FishView>> _pool = new Dictionary<int, Stack<FishView>>();
        readonly Dictionary<int, int> _bandDirs = new Dictionary<int, int>();
        Camera _camera;
        float _waterlineY;
        float _hookY;
        float _spawnCd;
        float _unitsPerMeter = FishingRules.WorldUnitsPerMeter;
        int _maxAlive = FishingRules.FishMaxAlive;
        float _everyMeters = 3f;
        bool _seeded;
        const float OutsidePad = 0.4f;
        const float WakePad = 2.8f;
        const float RecyclePadX = 5f;
        const float RecyclePadY = 2.6f;
        const float LaneXFraction = 0.68f;
        const float FishSeparationPad = 0.22f;
        const float VerticalSeparationExtra = 0.35f;
        const int SpawnPlacementTries = 14;
        float _laneHeightScale = 1.65f;
        bool _spawnFrozen;

        public void Configure(FishCatalog catalog)
        {
            if (catalog != null)
            {
                _catalog = catalog;
            }
        }

        public void BeginCast(Camera camera, float waterlineY)
        {
            Clear();
            _camera = camera;
            _waterlineY = waterlineY;
            _spawnCd = 0.08f;
            _seeded = false;
            _bandDirs.Clear();
        }

        public void Clear()
        {
            for (var i = _alive.Count - 1; i >= 0; i--)
            {
                RecycleAt(i);
            }

            for (var i = _hooked.Count - 1; i >= 0; i--)
            {
                ReleaseToPool(_hooked[i]);
            }

            _hooked.Clear();
            _seeded = false;
            _spawnCd = 0f;
            _bandDirs.Clear();
        }

        public void RegisterHooked(FishView view)
        {
            if (view == null)
            {
                return;
            }

            for (var i = _alive.Count - 1; i >= 0; i--)
            {
                if (_alive[i] == view)
                {
                    _alive.RemoveAt(i);
                    break;
                }
            }

            if (!_hooked.Contains(view))
            {
                _hooked.Add(view);
            }
        }

        public void TickHooked(float dt, Vector3 mouthAnchorWorld, bool paused)
        {
            if (paused || dt <= 0f)
            {
                return;
            }

            for (var i = 0; i < _hooked.Count; i++)
            {
                var view = _hooked[i];
                if (view != null && view.IsHookAnimating)
                {
                    view.TickHookTo(mouthAnchorWorld, dt);
                }
            }
        }

        public void Tick(
            float dt,
            CastSession session,
            bool paused,
            float hookY,
            float unitsPerMeter,
            int maxAlive,
            float everyMeters,
            float laneHeightScale)
        {
            _unitsPerMeter = Mathf.Max(0.05f, unitsPerMeter);
            _maxAlive = Mathf.Max(1, maxAlive);
            _everyMeters = Mathf.Max(0.5f, everyMeters);
            _laneHeightScale = Mathf.Max(0.5f, laneHeightScale);
            if (_camera == null || session == null)
            {
                return;
            }

            _hookY = hookY;

            Sweep();
            if (paused)
            {
                return;
            }

            MoveAlive(dt);
            EnforceBandDirections();
            SeparateAlive(dt);
            RecycleOffscreen(session);
            if (_spawnFrozen)
            {
                return;
            }

            if (!_seeded)
            {
                var ahead = FillDensity(session, 2, false);
                var sides = FillDensity(session, 1, true);
                if (_alive.Count >= _maxAlive
                    || (session.Depth >= FishingRules.FishSpawnMinDepthMeters && !ahead && !sides))
                {
                    _seeded = true;
                }
            }

            if (session.Phase == CastPhase.Settle)
            {
                return;
            }

            if (session.Depth < FishingRules.FishSpawnMinDepthMeters)
            {
                return;
            }

            SpawnTick(dt, session);
            TryFillMidDepthGap(session);
            TryFillDenseBand(session);
        }

        public void SetSpawnFrozen(bool frozen)
        {
            _spawnFrozen = frozen;
        }

        /// <summary>引导：在指定位置刷一条 80～100 米带的静止目标鱼。</summary>
        public FishView SpawnTutorialBait(CastSession session, float x, float y)
        {
            if (_catalog == null || _catalog.Items == null || _catalog.Items.Length == 0 || session == null)
            {
                return null;
            }

            var def = PickTutorialBait(session.Depth) ?? Pick(session.Depth) ?? PickAny();
            if (def == null || def.Prefab == null)
            {
                return null;
            }

            var view = Rent(def);
            view.Bind(def, 1);
            view.SetSwim(0, 0f);
            view.gameObject.SetActive(true);
            view.transform.position = new Vector3(x, y, 0f);
            _alive.Add(view);
            return view;
        }

        FishDefinition PickTutorialBait(float depth)
        {
            _scratch.Clear();
            var items = _catalog.Items;
            for (var i = 0; i < items.Length; i++)
            {
                var def = items[i];
                if (def == null || def.Prefab == null)
                {
                    continue;
                }

                if (def.MaxDepthMeters < 80f || def.MinDepthMeters > 100f)
                {
                    continue;
                }

                if (depth < def.MinDepthMeters || depth > def.MaxDepthMeters)
                {
                    continue;
                }

                _scratch.Add(def);
            }

            if (_scratch.Count == 0)
            {
                return null;
            }

            FishDefinition best = _scratch[0];
            for (var i = 1; i < _scratch.Count; i++)
            {
                if (_scratch[i].WorldHeight < best.WorldHeight)
                {
                    best = _scratch[i];
                }
            }

            return best;
        }

        /// <summary>引导：在钩子附近刷一条静止鱼，便于脚本挂鱼/捕获。</summary>
        public FishView SpawnTutorialAtHook(CastSession session, float hookX, float hookY)
        {
            if (_catalog == null || _catalog.Items == null || _catalog.Items.Length == 0 || session == null)
            {
                return null;
            }

            var depth = session.Depth;
            var def = Pick(depth) ?? PickAny();
            if (def == null || def.Prefab == null)
            {
                return null;
            }

            var view = Rent(def);
            view.Bind(def, 1);
            view.SetSwim(0, 0f);
            view.gameObject.SetActive(true);
            var pos = new Vector3(hookX, hookY, 0f);
            view.transform.position = pos;
            if (view.Head != null)
            {
                var head = view.Head.bounds.center;
                pos.x += hookX - head.x;
                pos.y += hookY - head.y;
                view.transform.position = pos;
            }

            _alive.Add(view);
            return view;
        }

        public bool TryGetHeadHit(Vector2 hookPos, float radius, out FishView fish)
        {
            fish = null;
            var best = float.MaxValue;
            for (var i = 0; i < _alive.Count; i++)
            {
                var view = _alive[i];
                if (view == null || view.Consumed || view.Head == null || !view.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!CircleHitsBox(view.Head, hookPos, radius))
                {
                    continue;
                }

                var d = ((Vector2)view.transform.position - hookPos).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    fish = view;
                }
            }

            return fish != null;
        }

        static bool CircleHitsBox(BoxCollider2D box, Vector2 point, float radius)
        {
            var t = box.transform;
            var center = (Vector2)t.TransformPoint(box.offset);
            var lossy = t.lossyScale;
            var hx = Mathf.Abs(box.size.x * lossy.x) * 0.5f + radius;
            var hy = Mathf.Abs(box.size.y * lossy.y) * 0.5f + radius;
            return Mathf.Abs(point.x - center.x) <= hx && Mathf.Abs(point.y - center.y) <= hy;
        }

        void SpawnTick(float dt, CastSession session)
        {
            _spawnCd -= dt;
            if (_spawnCd > 0f)
            {
                return;
            }

            var ahead = FillDensity(session, 3, false);
            var sides = FillDensity(session, 2, true);
            var extra = _alive.Count < _maxAlive && TrySpawnBonus(session);
            _spawnCd = ahead || sides || extra ? 0.07f : 0.16f;
        }

        const float DenseBandLo = 50f;
        const float DenseBandHi = 60f;
        const int DensePerLane = 5;

        void TryFillDenseBand(CastSession session)
        {
            if (_alive.Count >= _maxAlive || session.MaxDepth < DenseBandLo)
            {
                return;
            }

            if (session.Depth < DenseBandLo - 8f)
            {
                return;
            }

            if (session.Depth > DenseBandHi + 14f)
            {
                return;
            }

            var spawned = 0;
            for (var depth = 55f; depth <= 60f; depth += 1.5f)
            {
                var y = -depth * _unitsPerMeter;
                var need = DensePerLane - CountNearY(y, _unitsPerMeter * 1.1f);
                for (var n = 0; n < need; n++)
                {
                    if (_alive.Count >= _maxAlive || spawned >= 2)
                    {
                        return;
                    }

                    if (!TrySpawnAt(session, y, true, true))
                    {
                        break;
                    }

                    spawned++;
                }
            }
        }

        int CountNearY(float y, float range)
        {
            var count = 0;
            for (var i = 0; i < _alive.Count; i++)
            {
                var view = _alive[i];
                if (view == null || !view.gameObject.activeSelf || view.Consumed)
                {
                    continue;
                }

                if (Mathf.Abs(view.transform.position.y - y) <= range)
                {
                    count++;
                }
            }

            return count;
        }

        const float MidDepthGapFillLo = 58f;
        const float MidDepthGapFillHi = 68f;

        void TryFillMidDepthGap(CastSession session)
        {
            if (_alive.Count >= _maxAlive || session.MaxDepth < MidDepthGapFillLo)
            {
                return;
            }

            if (session.Depth < MidDepthGapFillLo - 12f && session.Phase == CastPhase.Descending)
            {
                return;
            }

            for (var depth = 59f; depth <= 66f; depth += 1f)
            {
                if (_alive.Count >= _maxAlive)
                {
                    break;
                }

                if (depth < MidDepthGapFillLo || depth > MidDepthGapFillHi)
                {
                    continue;
                }

                var laneY = SnapToLane(-depth * _unitsPerMeter);
                if (LaneHasFish(laneY))
                {
                    continue;
                }

                TrySpawnAt(session, laneY);
            }
        }

        bool TrySpawnBonus(CastSession session)
        {
            float lo;
            float hi;
            DensityRange(session, false, out lo, out hi);
            if (hi < lo + 0.15f)
            {
                DensityRange(session, true, out lo, out hi);
            }

            if (hi < lo + 0.15f)
            {
                return false;
            }

            return TrySpawnAt(session, SnapToLane(Random.Range(lo, hi)));
        }

        bool FillDensity(CastSession session, int budget, bool inViewBand)
        {
            if (budget <= 0 || _alive.Count >= _maxAlive)
            {
                return false;
            }

            var spacing = VerticalLaneSpacing();

            float lo;
            float hi;
            DensityRange(session, inViewBand, out lo, out hi);
            if (hi < lo + 0.1f)
            {
                return false;
            }

            var spawned = 0;
            var start = Mathf.FloorToInt(lo / spacing);
            var end = Mathf.CeilToInt(hi / spacing);
            for (var slot = start; slot <= end; slot++)
            {
                if (spawned >= budget || _alive.Count >= _maxAlive)
                {
                    break;
                }

                var y = slot * spacing;
                if (y < lo || y > hi)
                {
                    continue;
                }

                var laneY = SnapToLane(y);
                if (LaneHasFish(laneY))
                {
                    continue;
                }

                if (TrySpawnAt(session, laneY))
                {
                    spawned++;
                }
            }

            return spawned > 0;
        }

        void DensityRange(CastSession session, bool inViewBand, out float lo, out float hi)
        {
            float viewBottom;
            float viewTop;
            GetViewRect(out viewBottom, out viewTop, out _);
            var waterMax = _waterlineY - 0.55f;
            viewTop = Mathf.Min(viewTop, waterMax);
            var ahead = FishingRules.FishSpawnAheadMaxMeters * _unitsPerMeter;
            if (IsDescending(session))
            {
                var belowHook = _hookY - 0.55f;
                if (inViewBand)
                {
                    hi = Mathf.Min(belowHook, viewTop);
                    lo = viewBottom;
                }
                else
                {
                    hi = Mathf.Min(belowHook, viewBottom - OutsidePad);
                    lo = Mathf.Min(hi, _hookY - ahead);
                }
            }
            else
            {
                var aboveHook = _hookY + 0.55f;
                if (inViewBand)
                {
                    lo = Mathf.Max(aboveHook, viewBottom);
                    hi = Mathf.Min(waterMax, viewTop);
                }
                else
                {
                    lo = Mathf.Max(aboveHook, viewTop + OutsidePad);
                    hi = Mathf.Min(waterMax, Mathf.Max(lo, _hookY + ahead));
                }
            }
        }

        void GetViewRect(out float viewBottom, out float viewTop, out float viewHalfW)
        {
            var cam = _camera.transform.position;
            var ortho = _camera.orthographicSize;
            viewHalfW = ortho * CampField.ViewAspect(_camera.aspect);
            viewBottom = cam.y - ortho;
            viewTop = cam.y + ortho;
        }

        bool LaneHasFish(float laneY)
        {
            laneY = SnapToLane(laneY);
            for (var i = 0; i < _alive.Count; i++)
            {
                var view = _alive[i];
                if (view == null || !view.gameObject.activeSelf || view.Consumed)
                {
                    continue;
                }

                if (Mathf.Abs(SnapToLane(view.transform.position.y) - laneY) < 0.05f)
                {
                    return true;
                }
            }

            return false;
        }

        bool TrySpawnAt(CastSession session, float y, bool allowPacked = false, bool keepDepth = false)
        {
            if (_catalog == null || _catalog.Items == null || _catalog.Items.Length == 0)
            {
                return false;
            }

            if (_alive.Count >= _maxAlive)
            {
                return false;
            }

            var waterMax = _waterlineY - 0.55f;
            if (y > waterMax - 0.05f)
            {
                return false;
            }

            var depth = Mathf.Max(0f, -y / _unitsPerMeter);
            if (depth < FishingRules.FishSpawnMinDepthMeters)
            {
                return false;
            }

            var def = Pick(depth);
            if (def == null || def.Prefab == null)
            {
                return false;
            }

            float viewBottom;
            float viewTop;
            float viewHalfW;
            GetViewRect(out viewBottom, out viewTop, out viewHalfW);
            var view = Rent(def);
            var swimSpeed = SwimSpeedFor(def);
            var halfH = Mathf.Max(0.2f, def.WorldHeight * 0.5f);
            var spawnY = keepDepth ? y : SnapToLane(y);
            if (!allowPacked && LaneHasFish(spawnY))
            {
                ReleaseToPool(view);
                return false;
            }

            var bandDir = ResolveBandDir(spawnY);
            for (var attempt = 0; attempt < SpawnPlacementTries; attempt++)
            {
                var dir = bandDir;
                view.Bind(def, dir);
                view.SetSwim(dir, swimSpeed);
                var halfW = Mathf.Max(0.35f, view.WorldHalfWidth);
                halfH = Mathf.Max(0.2f, view.WorldHalfHeight);
                var verticallyVisible = spawnY + halfH > viewBottom && spawnY - halfH < viewTop;
                float x;
                if (verticallyVisible)
                {
                    x = OffscreenX(dir, viewHalfW, halfW);
                }
                else
                {
                    x = Random.Range(-viewHalfW * LaneXFraction, viewHalfW * LaneXFraction);
                }

                if (Mathf.Abs(spawnY - _hookY) < 0.7f && Mathf.Abs(x - session.HookX) < 0.9f)
                {
                    x = OffscreenX(dir, viewHalfW, halfW);
                }

                if (!OverlapsOthers(x, spawnY, halfW, halfH, view))
                {
                    view.transform.position = new Vector3(x, spawnY, 0f);
                    view.gameObject.SetActive(true);
                    _alive.Add(view);
                    return true;
                }
            }

            ReleaseToPool(view);
            return false;
        }

        float VerticalLaneSpacing()
        {
            var spacing = _everyMeters * _unitsPerMeter;
            if (spacing < 0.2f)
            {
                spacing = 0.2f;
            }

            return spacing * _laneHeightScale;
        }

        float SnapToLane(float y)
        {
            var spacing = VerticalLaneSpacing();
            return Mathf.Round(y / spacing) * spacing;
        }

        int BandIndex(float y)
        {
            return Mathf.RoundToInt(SnapToLane(y) / VerticalLaneSpacing());
        }

        int ResolveBandDir(float y)
        {
            var band = BandIndex(y);
            int dir;
            if (_bandDirs.TryGetValue(band, out dir))
            {
                return dir;
            }

            for (var i = 0; i < _alive.Count; i++)
            {
                var view = _alive[i];
                if (view == null || view.Consumed || !view.gameObject.activeSelf)
                {
                    continue;
                }

                if (BandIndex(view.transform.position.y) != band)
                {
                    continue;
                }

                dir = view.Dir;
                _bandDirs[band] = dir;
                return dir;
            }

            dir = Random.value < 0.5f ? -1 : 1;
            _bandDirs[band] = dir;
            return dir;
        }

        void EnforceBandDirections()
        {
            for (var i = 0; i < _alive.Count; i++)
            {
                var view = _alive[i];
                if (view == null || view.Consumed || !view.gameObject.activeSelf)
                {
                    continue;
                }

                var dir = ResolveBandDir(view.transform.position.y);
                if (view.Dir != dir)
                {
                    view.SetSwim(dir, view.SwimSpeed);
                }
            }
        }

        static float SwimSpeedFor(FishDefinition def)
        {
            if (def == null)
            {
                return 1f;
            }

            var boost = def.WorldHeight <= 0.4f
                ? 2.05f
                : def.WorldHeight <= 0.55f
                    ? 1.75f
                    : def.WorldHeight <= 0.7f
                        ? 1.5f
                        : def.WorldHeight <= 0.85f
                            ? 1.25f
                            : 1f;
            return def.MoveSpeed * boost;
        }

        bool OverlapsOthers(float x, float y, float halfW, float halfH, FishView ignore)
        {
            for (var i = 0; i < _alive.Count; i++)
            {
                var other = _alive[i];
                if (other == null || other == ignore || other.Consumed || !other.gameObject.activeSelf)
                {
                    continue;
                }

                var p = other.transform.position;
                var ow = other.WorldHalfWidth;
                var oh = other.WorldHalfHeight;
                var yPad = FishSeparationPad + VerticalSeparationExtra;
                if (Mathf.Abs(x - p.x) < halfW + ow + FishSeparationPad &&
                    Mathf.Abs(y - p.y) < halfH + oh + yPad)
                {
                    return true;
                }
            }

            return false;
        }

        void SeparateAlive(float dt)
        {
            if (dt <= 0f)
            {
                return;
            }

            for (var i = 0; i < _alive.Count; i++)
            {
                var a = _alive[i];
                if (a == null || a.Consumed || !a.gameObject.activeSelf)
                {
                    continue;
                }

                var pa = a.transform.position;
                var aw = a.WorldHalfWidth;
                var ah = a.WorldHalfHeight;
                for (var j = i + 1; j < _alive.Count; j++)
                {
                    var b = _alive[j];
                    if (b == null || b.Consumed || !b.gameObject.activeSelf)
                    {
                        continue;
                    }

                    if (a.Dir != b.Dir)
                    {
                        continue;
                    }

                    var pb = b.transform.position;
                    var bw = b.WorldHalfWidth;
                    var bh = b.WorldHalfHeight;
                    var overlapX = aw + bw + FishSeparationPad - Mathf.Abs(pa.x - pb.x);
                    var overlapY = ah + bh + FishSeparationPad - Mathf.Abs(pa.y - pb.y);
                    if (overlapX <= 0f || overlapY <= 0f)
                    {
                        continue;
                    }

                    var push = Mathf.Min(overlapX, overlapY) * 0.55f;
                    var sign = pa.x <= pb.x ? -1f : 1f;
                    if (Mathf.Abs(pa.x - pb.x) < 0.001f)
                    {
                        sign = a.Dir >= 0 ? -1f : 1f;
                    }

                    pa.x += sign * push * 0.5f;
                    pb.x -= sign * push * 0.5f;
                    a.transform.position = pa;
                    b.transform.position = pb;
                }
            }
        }

        static float OffscreenX(int dir, float viewHalfW, float spriteHalfW)
        {
            var x = viewHalfW + spriteHalfW + OutsidePad;
            return dir > 0 ? -x : x;
        }

        FishDefinition Pick(float depth)
        {
            _scratch.Clear();
            var items = _catalog.Items;
            var total = 0;
            for (var i = 0; i < items.Length; i++)
            {
                var def = items[i];
                if (def == null || def.Prefab == null || def.SpawnWeight <= 0)
                {
                    continue;
                }

                if (depth < def.MinDepthMeters || depth > def.MaxDepthMeters)
                {
                    continue;
                }

                _scratch.Add(def);
                total += def.SpawnWeight;
            }

            if (total <= 0)
            {
                return null;
            }

            var roll = Random.Range(0, total);
            for (var i = 0; i < _scratch.Count; i++)
            {
                roll -= _scratch[i].SpawnWeight;
                if (roll < 0)
                {
                    return _scratch[i];
                }
            }

            return _scratch[_scratch.Count - 1];
        }

        FishDefinition PickAny()
        {
            var items = _catalog != null ? _catalog.Items : null;
            if (items == null)
            {
                return null;
            }

            for (var i = 0; i < items.Length; i++)
            {
                var def = items[i];
                if (def != null && def.Prefab != null && def.SpawnWeight > 0)
                {
                    return def;
                }
            }

            return null;
        }

        FishView Rent(FishDefinition def)
        {
            var id = def.Prefab.GetInstanceID();
            Stack<FishView> stack;
            if (_pool.TryGetValue(id, out stack) && stack.Count > 0)
            {
                return stack.Pop();
            }

            var created = Instantiate(def.Prefab, transform);
            created.name = def.Prefab.name;
            created.gameObject.SetActive(false);
            return created;
        }

        void MoveAlive(float dt)
        {
            float viewBottom;
            float viewTop;
            float viewHalfW;
            GetViewRect(out viewBottom, out viewTop, out viewHalfW);
            var wakeBottom = viewBottom - WakePad;
            var wakeTop = viewTop + WakePad;
            for (var i = 0; i < _alive.Count; i++)
            {
                var view = _alive[i];
                if (view == null || !view.gameObject.activeSelf || view.Consumed)
                {
                    continue;
                }

                var y = view.transform.position.y;
                if (y >= wakeBottom && y <= wakeTop)
                {
                    view.Tick(dt);
                }
            }
        }

        void RecycleOffscreen(CastSession session)
        {
            float viewBottom;
            float viewTop;
            float viewHalfW;
            GetViewRect(out viewBottom, out viewTop, out viewHalfW);
            var recycleHalfW = viewHalfW + RecyclePadX;
            var wakeBottom = viewBottom - WakePad;
            var wakeTop = viewTop + WakePad;
            var passedTop = viewTop + RecyclePadY;
            var passedBottom = viewBottom - RecyclePadY;
            var descending = IsDescending(session);
            for (var i = _alive.Count - 1; i >= 0; i--)
            {
                var view = _alive[i];
                if (view == null)
                {
                    RecycleAt(i);
                    continue;
                }

                var p = view.transform.position;
                var nearView = p.y >= wakeBottom && p.y <= wakeTop;
                if (nearView && Mathf.Abs(p.x) > recycleHalfW)
                {
                    RecycleAt(i);
                    continue;
                }

                if (descending && p.y > passedTop)
                {
                    RecycleAt(i);
                    continue;
                }

                if (!descending && p.y < passedBottom)
                {
                    RecycleAt(i);
                }
            }
        }

        void Sweep()
        {
            for (var i = _alive.Count - 1; i >= 0; i--)
            {
                var view = _alive[i];
                if (view == null)
                {
                    RecycleAt(i);
                    continue;
                }

                if (view.Hooked)
                {
                    continue;
                }

                if (view.Consumed || !view.gameObject.activeSelf)
                {
                    RecycleAt(i);
                }
            }
        }

        void RecycleAt(int index)
        {
            var view = _alive[index];
            _alive.RemoveAt(index);
            ReleaseToPool(view);
        }

        void ReleaseToPool(FishView view)
        {
            if (view == null)
            {
                return;
            }

            _hooked.Remove(view);
            view.PrepareForPool(transform);
            view.gameObject.SetActive(false);
            var id = view.PoolId;
            if (id == 0)
            {
                Destroy(view.gameObject);
                return;
            }

            Stack<FishView> stack;
            if (!_pool.TryGetValue(id, out stack))
            {
                stack = new Stack<FishView>();
                _pool[id] = stack;
            }

            stack.Push(view);
        }

        static bool IsDescending(CastSession session)
        {
            return session.Phase != CastPhase.Ascending &&
                session.Phase != CastPhase.Returning &&
                session.Phase != CastPhase.Settle;
        }
    }
}
