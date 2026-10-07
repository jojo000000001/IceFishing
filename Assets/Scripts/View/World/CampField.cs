using UnityEngine;

namespace IceFishing.View
{
    /// <summary>
    /// 营地冰面：和水下一样是世界里的一块，钉在冰面高度，不跟着镜头走。
    /// 只用原图冰面以上的部分，水面以下交给 UnderwaterField。
    /// </summary>
    public sealed class CampField : MonoBehaviour
    {
        public const float WorldOrtho = 8f;
        public const float HubOrtho = WorldOrtho;
        public const float FishingOrtho = WorldOrtho;

        [SerializeField] Sprite _art;
        [SerializeField] float _waterlineUv = 0.33f;

        public float HubCameraY
        {
            get { return WorldOrtho * (2f - 2f * _waterlineUv); }
        }
        MeshFilter _filter;
        MeshRenderer _meshRenderer;
        Mesh _mesh;
        Material _material;

        public float WaterlineY
        {
            get { return transform.position.y; }
        }

        public void Configure(Sprite art)
        {
            if (art != null)
            {
                _art = art;
            }

            BuildMesh();
        }

        public void Place(float aspect)
        {
            if (_art == null)
            {
                return;
            }

            BuildMesh();
            aspect = ViewAspect(aspect);
            var size = _art.bounds.size;
            if (size.x < 0.001f || size.y < 0.001f)
            {
                return;
            }

            var scale = CoverScale(_art, aspect);
            transform.localScale = new Vector3(scale, scale, 1f);
            transform.position = new Vector3(0f, WorldOrtho, 2f);
        }

        public static float ViewAspect(float aspect)
        {
            return aspect < 0.15f ? 9f / 18f : aspect;
        }

        public static float CoverScale(Sprite sprite, float aspect)
        {
            if (sprite == null)
            {
                return 1f;
            }

            aspect = ViewAspect(aspect);
            var size = sprite.bounds.size;
            if (size.x < 0.001f || size.y < 0.001f)
            {
                return 1f;
            }

            var viewHeight = WorldOrtho * 2f;
            var viewWidth = viewHeight * aspect;
            return Mathf.Max(viewWidth / size.x, viewHeight / size.y);
        }

        void OnValidate()
        {
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.enabled)
            {
                spriteRenderer.enabled = false;
            }
        }

        void BuildMesh()
        {
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }

            if (_art == null)
            {
                return;
            }

            EnsureIceMesh();
            if (_filter == null || _meshRenderer == null)
            {
                return;
            }

            if (_material == null)
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    _material = new Material(shader);
                }
            }

            if (_material != null)
            {
                _material.mainTexture = _art.texture;
                _material.color = Color.white;
                _meshRenderer.sharedMaterial = _material;
            }

            _meshRenderer.sortingOrder = 5;
            _meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _meshRenderer.receiveShadows = false;

            var tex = _art.texture;
            var rect = _art.rect;
            var texW = tex != null ? tex.width : rect.width;
            var texH = tex != null ? tex.height : rect.height;
            if (texW < 1f || texH < 1f)
            {
                return;
            }

            var u0 = rect.x / texW;
            var u1 = (rect.x + rect.width) / texW;
            var v0 = rect.y / texH;
            var v1 = (rect.y + rect.height) / texH;
            var vWater = Mathf.Lerp(v0, v1, _waterlineUv);
            var full = _art.bounds.size;
            var width = full.x;
            var height = full.y * (1f - _waterlineUv);

            if (_mesh == null)
            {
                _mesh = new Mesh();
                _mesh.name = "CampIce";
            }

            _mesh.Clear();
            _mesh.vertices = new[]
            {
                new Vector3(-width * 0.5f, 0f, 0f),
                new Vector3(width * 0.5f, 0f, 0f),
                new Vector3(-width * 0.5f, height, 0f),
                new Vector3(width * 0.5f, height, 0f)
            };
            _mesh.uv = new[]
            {
                new Vector2(u0, vWater),
                new Vector2(u1, vWater),
                new Vector2(u0, v1),
                new Vector2(u1, v1)
            };
            _mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            _mesh.RecalculateBounds();
            _filter.sharedMesh = _mesh;
        }

        void EnsureIceMesh()
        {
            var ice = transform.Find("IceMesh");
            if (ice == null)
            {
                var go = new GameObject("IceMesh");
                ice = go.transform;
                ice.SetParent(transform, false);
                ice.localPosition = Vector3.zero;
                ice.localRotation = Quaternion.identity;
                ice.localScale = Vector3.one;
            }

            _filter = ice.GetComponent<MeshFilter>();
            if (_filter == null)
            {
                _filter = ice.gameObject.AddComponent<MeshFilter>();
            }

            _meshRenderer = ice.GetComponent<MeshRenderer>();
            if (_meshRenderer == null)
            {
                _meshRenderer = ice.gameObject.AddComponent<MeshRenderer>();
            }
        }
    }
}
