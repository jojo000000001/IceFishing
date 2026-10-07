using IceFishing.Model;
using UnityEngine;

namespace IceFishing.View
{
    /// <summary>
    /// 一条鱼的表现：贴图、头/身碰撞、水平游。规则在 Controller / SO，这里不算捕获。
    /// 贴图朝右；朝左时翻转根节点 X。
    /// </summary>
    public sealed class FishView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer _sprite;
        [SerializeField] BoxCollider2D _head;
        [SerializeField] BoxCollider2D _body;
        [SerializeField] float _worldHeight = 0.9f;
        int _dir = 1;
        float _speed;
        int _poolId;
        FishDefinition _definition;
        bool _consumed;
        bool _hooked;
        bool _hookAnimating;
        float _hookAnimElapsed;
        Vector3 _hookStartLocalPos;
        Quaternion _hookStartLocalRot;
        Vector3 _hookStartLocalScale;
        Vector3 _hookTargetLocalScale;
        Quaternion _hookTargetLocalRot;
        Transform _hookStack;
        int _hookSortOrder;
        float _hookSwayDeg;

        const float HookAnimDuration = 0.32f;
        const float HookSwayDegPerWorldSpeed = 2.35f;
        const float HookSwayMaxDeg = 36f;
        const float HookSwayResponse = 16f;
        const float HookedHangLocalZ = 90f;

        public BoxCollider2D Head
        {
            get { return _head; }
        }

        public BoxCollider2D Body
        {
            get { return _body; }
        }

        public int Dir
        {
            get { return _dir; }
        }

        public float SwimSpeed
        {
            get { return _speed; }
        }

        public int PoolId
        {
            get { return _poolId; }
        }

        public FishDefinition Definition
        {
            get { return _definition; }
        }

        public bool Consumed
        {
            get { return _consumed; }
        }

        public bool Hooked
        {
            get { return _hooked; }
        }

        public bool IsHookAnimating
        {
            get { return _hookAnimating; }
        }

        /// <summary>教程引导鱼：不改游向、不挤开、不回收，保证按设定速度撞钩。</summary>
        public bool TutorialGuided { get; set; }

        /// <summary>90～100 米深带补鱼：下潜时放宽「过顶回收」。</summary>
        public bool DeepBandAnchored { get; set; }

        public float WorldHalfWidth
        {
            get
            {
                if (_sprite != null && _sprite.sprite != null)
                {
                    return 0.5f * _sprite.sprite.bounds.size.x * Mathf.Abs(transform.lossyScale.x);
                }

                return Mathf.Max(0.35f, _worldHeight * 0.75f);
            }
        }

        public float WorldHalfHeight
        {
            get
            {
                if (_sprite != null && _sprite.sprite != null)
                {
                    return 0.5f * _sprite.sprite.bounds.size.y * Mathf.Abs(transform.lossyScale.y);
                }

                return Mathf.Max(0.25f, _worldHeight * 0.5f);
            }
        }

        public void Bind(FishDefinition definition, int dir)
        {
            _definition = definition;
            _consumed = false;
            _hooked = false;
            _hookAnimating = false;
            TutorialGuided = false;
            DeepBandAnchored = false;
            _poolId = definition != null && definition.Prefab != null
                ? definition.Prefab.GetInstanceID()
                : 0;
            if (definition != null)
            {
                SetWorldHeight(definition.WorldHeight);
                SetSwim(dir, definition.MoveSpeed);
            }
            else
            {
                SetSwim(dir, _speed);
            }
        }

        public void MarkHit()
        {
            _consumed = true;
            _hooked = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 开始挂钩动画：从当前姿态平滑转到竖直咬钩。
        /// </summary>
        public void BeginHookTo(Transform hookStack, Vector3 mouthAnchorWorld, int sortingOrder)
        {
            if (hookStack == null)
            {
                MarkHit();
                return;
            }

            var lossyScale = transform.lossyScale;
            _consumed = true;
            _hooked = true;
            _speed = 0f;
            _hookStack = hookStack;
            _hookSortOrder = sortingOrder;
            _hookAnimating = true;
            _hookAnimElapsed = 0f;
            _hookSwayDeg = 0f;
            transform.SetParent(hookStack, true);
            ApplyLossyScale(transform, lossyScale);
            _hookStartLocalPos = transform.localPosition;
            _hookStartLocalRot = transform.localRotation;
            _hookStartLocalScale = transform.localScale;
            _hookTargetLocalScale = _hookStartLocalScale;
            _hookTargetLocalScale.x = Mathf.Abs(_hookTargetLocalScale.x);
            _hookTargetLocalRot = Quaternion.Euler(0f, 0f, 90f);
            if (_sprite != null)
            {
                _sprite.sortingOrder = sortingOrder;
            }

            gameObject.SetActive(true);
            if (HookAnimDuration <= 0.001f)
            {
                FinishHookTo(mouthAnchorWorld);
            }
        }

        public void TickHookTo(Vector3 mouthAnchorWorld, float hookVelX, float dt)
        {
            if (!_hookAnimating || _hookStack == null || dt <= 0f)
            {
                return;
            }

            _hookAnimElapsed += dt;
            var t = Mathf.Clamp01(_hookAnimElapsed / HookAnimDuration);
            t = Mathf.SmoothStep(0f, 1f, t);
            UpdateHookSway(hookVelX, dt);
            transform.localScale = Vector3.Lerp(_hookStartLocalScale, _hookTargetLocalScale, t);
            var hangRot = Quaternion.Euler(0f, 0f, HookedHangLocalZ + _hookSwayDeg);
            transform.localRotation = Quaternion.Slerp(_hookStartLocalRot, hangRot, t);
            SnapMouthTo(mouthAnchorWorld);
            if (t >= 1f)
            {
                FinishHookTo(mouthAnchorWorld);
            }
        }

        /// <summary>挂鱼后：嘴在钩上，身体随左右移动反向摆。</summary>
        public void TickHookedSway(Vector3 mouthAnchorWorld, float hookVelX, float dt)
        {
            if (!_hooked || _hookStack == null || _hookAnimating || dt <= 0f)
            {
                return;
            }

            UpdateHookSway(hookVelX, dt);
            transform.localScale = _hookTargetLocalScale;
            transform.localRotation = Quaternion.Euler(0f, 0f, HookedHangLocalZ + _hookSwayDeg);
            SnapMouthTo(mouthAnchorWorld);
            if (_sprite != null)
            {
                _sprite.sortingOrder = _hookSortOrder;
            }
        }

        void UpdateHookSway(float hookVelX, float dt)
        {
            var target = Mathf.Clamp(
                -hookVelX * HookSwayDegPerWorldSpeed,
                -HookSwayMaxDeg,
                HookSwayMaxDeg);
            var k = 1f - Mathf.Exp(-HookSwayResponse * dt);
            _hookSwayDeg = Mathf.Lerp(_hookSwayDeg, target, k);
        }

        void FinishHookTo(Vector3 mouthAnchorWorld)
        {
            _hookAnimating = false;
            transform.localScale = _hookTargetLocalScale;
            transform.localRotation = Quaternion.Euler(0f, 0f, HookedHangLocalZ + _hookSwayDeg);
            SnapMouthTo(mouthAnchorWorld);
            if (_sprite != null)
            {
                _sprite.sortingOrder = _hookSortOrder;
            }
        }

        void SnapMouthTo(Vector3 mouthAnchorWorld)
        {
            transform.position = mouthAnchorWorld;
            transform.position += mouthAnchorWorld - GetMouthWorldPosition();
            if (_hookStack != null)
            {
                var p = transform.position;
                p.z = _hookStack.position.z - 0.02f;
                transform.position = p;
            }
        }

        Vector3 GetMouthWorldPosition()
        {
            if (_head != null)
            {
                return _head.transform.TransformPoint(_head.offset);
            }

            return transform.position + transform.right * (WorldHalfWidth * 0.65f);
        }

        static void ApplyLossyScale(Transform target, Vector3 lossyScale)
        {
            var parent = target.parent;
            if (parent == null)
            {
                target.localScale = lossyScale;
                return;
            }

            var parentLossy = parent.lossyScale;
            target.localScale = new Vector3(
                DivLossy(lossyScale.x, parentLossy.x),
                DivLossy(lossyScale.y, parentLossy.y),
                DivLossy(lossyScale.z, parentLossy.z));
        }

        static float DivLossy(float value, float parentAxis)
        {
            return Mathf.Approximately(parentAxis, 0f) ? value : value / parentAxis;
        }

        public void PrepareForPool(Transform poolRoot)
        {
            _consumed = false;
            _hooked = false;
            _hookAnimating = false;
            TutorialGuided = false;
            DeepBandAnchored = false;
            _hookStack = null;
            _speed = 0f;
            if (poolRoot != null)
            {
                transform.SetParent(poolRoot, false);
            }

            transform.localRotation = Quaternion.identity;
        }

        public void EditorAssign(SpriteRenderer sprite, BoxCollider2D head, BoxCollider2D body)
        {
            _sprite = sprite;
            _head = head;
            _body = body;
        }

        public void SetSprite(Sprite sprite)
        {
            if (_sprite != null)
            {
                _sprite.sprite = sprite;
            }

            FitAndLayout();
        }

        public void SetWorldHeight(float worldHeight)
        {
            if (worldHeight > 0.05f && worldHeight < 0.6f)
            {
                worldHeight *= 1.35f;
            }
            else if (worldHeight >= 0.9f)
            {
                worldHeight *= 1.35f;
            }

            if (worldHeight > 0.05f)
            {
                _worldHeight = worldHeight;
            }

            FitAndLayout();
        }

        public void SetSwim(int dir, float speed)
        {
            _dir = dir >= 0 ? 1 : -1;
            _speed = Mathf.Max(0f, speed);
            ApplyFacing();
        }

        public void Tick(float dt)
        {
            if (_hooked || dt <= 0f || _speed <= 0f)
            {
                return;
            }

            var p = transform.position;
            p.x += _dir * _speed * dt;
            transform.position = p;
        }

        public void FitAndLayout()
        {
            FitScale();
            LayoutColliders();
            ApplyFacing();
        }

        void FitScale()
        {
            if (_sprite == null || _sprite.sprite == null)
            {
                return;
            }

            var size = _sprite.sprite.bounds.size;
            if (size.y < 0.001f)
            {
                return;
            }

            var scale = _worldHeight / size.y;
            var x = Mathf.Abs(transform.localScale.x) < 0.0001f ? 1f : Mathf.Sign(transform.localScale.x);
            transform.localScale = new Vector3(x * scale, scale, 1f);
        }

        void LayoutColliders()
        {
            if (_sprite == null || _sprite.sprite == null)
            {
                return;
            }

            var b = _sprite.sprite.bounds;
            if (_head != null)
            {
                _head.isTrigger = true;
                _head.offset = new Vector2(b.center.x + b.size.x * 0.28f, b.center.y);
                _head.size = new Vector2(Mathf.Max(0.04f, b.size.x * 0.32f), Mathf.Max(0.04f, b.size.y * 0.65f));
            }

            if (_body != null)
            {
                _body.isTrigger = true;
                _body.offset = new Vector2(b.center.x - b.size.x * 0.08f, b.center.y);
                _body.size = new Vector2(Mathf.Max(0.04f, b.size.x * 0.72f), Mathf.Max(0.04f, b.size.y * 0.5f));
            }
        }

        void ApplyFacing()
        {
            var scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * _dir;
            transform.localScale = scale;
        }
    }
}
