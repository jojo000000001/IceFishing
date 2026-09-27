using UnityEngine;

namespace IceFishing.View
{
    /// <summary>
    /// 钓鱼水域：一张蓝底上下复用，每个 WaterTile 挂一块边冰。
    /// 结构在预制体里；运行时只按镜头滚动瓦片。
    /// </summary>
    public sealed class UnderwaterField : MonoBehaviour
    {
        const float IceScreenWidth = 0.12f;
        const float IceHeightScale = 1.25f;
        const float TileOverlap = 1.04f;
        const int IceRowsPerTile = 1;
        const string TileIceName = "TileIce";

        [SerializeField] Sprite _waterTile;
        [SerializeField] Sprite[] _leftIce;
        [SerializeField] Sprite[] _rightIce;
        [SerializeField] Transform _tileA;
        [SerializeField] Transform _tileB;
        [SerializeField] Transform _tileC;
        [SerializeField] Transform _iceRoot;
        bool _visible;
        bool _iceVisible = true;
        float _waterlineY = float.PositiveInfinity;

        public void Configure(Sprite waterTile, Sprite[] leftIce, Sprite[] rightIce)
        {
            _waterTile = waterTile;
            _leftIce = leftIce ?? new Sprite[0];
            _rightIce = rightIce ?? new Sprite[0];
            EnsureNodes();
        }

        public void Populate(Sprite waterTile, Sprite[] leftIce, Sprite[] rightIce)
        {
            Configure(waterTile, leftIce, rightIce);
            ClearSideIceRoot();
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            _visible = visible;
            if (_tileA != null)
            {
                _tileA.gameObject.SetActive(visible);
            }

            if (_tileB != null)
            {
                _tileB.gameObject.SetActive(visible);
            }

            if (_tileC != null)
            {
                _tileC.gameObject.SetActive(visible);
            }

            if (_iceRoot != null)
            {
                _iceRoot.gameObject.SetActive(false);
            }
        }

        public void SetIceVisible(bool visible)
        {
            _iceVisible = visible;
            ApplyIceVisible(_tileA);
            ApplyIceVisible(_tileB);
            ApplyIceVisible(_tileC);
        }

        public void PlaceColumn(float centerX, float topY, Camera worldCamera)
        {
            if (worldCamera == null)
            {
                return;
            }

            EnsureNodes();
            _visible = true;
            _waterlineY = topY - 0.12f;
            if (_tileA != null)
            {
                _tileA.gameObject.SetActive(true);
            }

            if (_tileB != null)
            {
                _tileB.gameObject.SetActive(true);
            }

            if (_tileC != null)
            {
                _tileC.gameObject.SetActive(true);
            }

            if (_waterTile == null)
            {
                return;
            }

            var aspect = CampField.ViewAspect(worldCamera.aspect);
            var spriteSize = _waterTile.bounds.size;
            if (spriteSize.x < 0.001f || spriteSize.y < 0.001f)
            {
                return;
            }

            var scale = CampField.CoverScale(_waterTile, aspect) * TileOverlap;
            var tileHeight = spriteSize.y * scale;
            var worldWidth = CampField.WorldOrtho * 2f * aspect;
            PlaceTile(_tileA, centerX, topY - tileHeight * 0.5f, scale);
            PlaceTile(_tileB, centerX, topY - tileHeight * 1.5f, scale);
            PlaceTile(_tileC, centerX, topY - tileHeight * 2.5f, scale);
            LayoutAllIce(worldWidth, tileHeight);
        }

        public void Tick(Camera worldCamera)
        {
            if (!_visible || worldCamera == null)
            {
                return;
            }

            EnsureNodes();
            UpdateTiles(worldCamera);
        }

        void EnsureNodes()
        {
            _tileA = ResolveSpriteChild(_tileA, "WaterTileA", -30);
            _tileB = ResolveSpriteChild(_tileB, "WaterTileB", -30);
            _tileC = ResolveSpriteChild(_tileC, "WaterTileC", -30);
            if (_iceRoot == null)
            {
                var found = transform.Find("SideIce");
                if (found != null)
                {
                    _iceRoot = found;
                }
            }

            if (_iceRoot != null)
            {
                _iceRoot.gameObject.SetActive(false);
            }

            Paint(_tileA, _waterTile);
            Paint(_tileB, _waterTile);
            Paint(_tileC, _waterTile);
        }

        Transform ResolveSpriteChild(Transform current, string objectName, int order)
        {
            if (current != null)
            {
                return current;
            }

            var found = transform.Find(objectName);
            if (found != null)
            {
                return found;
            }

            return CreateSprite(objectName, order);
        }

        void UpdateTiles(Camera cam)
        {
            if (_waterTile == null)
            {
                return;
            }

            var aspect = CampField.ViewAspect(cam.aspect);
            var spriteSize = _waterTile.bounds.size;
            if (spriteSize.x < 0.001f || spriteSize.y < 0.001f)
            {
                return;
            }

            var scale = CampField.CoverScale(_waterTile, aspect) * TileOverlap;
            var tileHeight = spriteSize.y * scale;
            var worldWidth = CampField.WorldOrtho * 2f * aspect;
            var camX = cam.transform.position.x;
            var viewBottom = cam.transform.position.y - cam.orthographicSize;
            var origin = Mathf.Floor(viewBottom / tileHeight) * tileHeight;
            PlaceTile(_tileA, camX, origin + tileHeight * 0.5f, scale);
            PlaceTile(_tileB, camX, origin + tileHeight * 1.5f, scale);
            PlaceTile(_tileC, camX, origin + tileHeight * 2.5f, scale);
            LayoutAllIce(worldWidth, tileHeight);
        }

        void LayoutAllIce(float worldWidth, float tileHeight)
        {
            LayoutTileIce(_tileA, worldWidth, tileHeight);
            LayoutTileIce(_tileB, worldWidth, tileHeight);
            LayoutTileIce(_tileC, worldWidth, tileHeight);
        }

        void LayoutTileIce(Transform tile, float worldWidth, float tileHeight)
        {
            if (tile == null || tileHeight < 0.001f)
            {
                return;
            }

            HideLegacyIce(tile);
            var rowHeight = tileHeight / IceRowsPerTile;
            var index = 0;
            for (var row = 0; row < IceRowsPerTile; row++)
            {
                var yL = tile.position.y - tileHeight * 0.5f + (row + 0.5f) * rowHeight;
                var yR = yL + rowHeight * 0.38f;
                LayoutOneIce(tile, index++, worldWidth, yL, true);
                LayoutOneIce(tile, index++, worldWidth, yR, false);
            }

            HideExtraIce(tile, index);
        }

        void LayoutOneIce(Transform tile, int index, float worldWidth, float y, bool left)
        {
            var ice = EnsureIceChild(tile, index);
            if (ice == null)
            {
                return;
            }

            var renderer = ice.GetComponent<SpriteRenderer>();
            Sprite sprite;
            if (!TryGetIce(y, left, out sprite) || renderer == null || sprite == null)
            {
                ice.gameObject.SetActive(false);
                return;
            }

            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = 1;
            var size = sprite.bounds.size;
            var targetWidth = worldWidth * IceScreenWidth;
            var worldScaleX = size.x > 0.001f ? targetWidth / size.x : 1f;
            var worldScaleY = worldScaleX * IceHeightScale;
            var lossy = tile.lossyScale;
            ice.localScale = new Vector3(
                Mathf.Abs(lossy.x) > 0.0001f ? worldScaleX / lossy.x : worldScaleX,
                Mathf.Abs(lossy.y) > 0.0001f ? worldScaleY / lossy.y : worldScaleY,
                1f);
            var half = size.x * worldScaleX * 0.5f;
            var x = left
                ? tile.position.x - worldWidth * 0.5f + half
                : tile.position.x + worldWidth * 0.5f - half;
            ice.position = new Vector3(x, y, 0.8f);
            ice.gameObject.SetActive(_iceVisible && y < _waterlineY - 0.35f);
        }

        Transform EnsureIceChild(Transform tile, int index)
        {
            if (tile == null)
            {
                return null;
            }

            var objectName = TileIceName + index;
            var found = tile.Find(objectName);
            if (found != null)
            {
                return found;
            }

            var go = new GameObject(objectName);
            go.transform.SetParent(tile, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 1;
            renderer.color = Color.white;
            return go.transform;
        }

        bool TryGetIce(float y, bool left, out Sprite sprite)
        {
            sprite = null;
            var set = left ? _leftIce : _rightIce;
            if (set == null || set.Length == 0)
            {
                return false;
            }

            if (!left)
            {
                sprite = set[0];
                return sprite != null;
            }

            var pick = 0;
            for (var i = 0; i < set.Length; i++)
            {
                if (set[i] != null)
                {
                    pick++;
                }
            }

            if (pick == 0)
            {
                return false;
            }

            var cell = Mathf.Abs(Mathf.FloorToInt(y * 4f));
            var want = cell % pick;
            var seen = 0;
            for (var i = 0; i < set.Length; i++)
            {
                if (set[i] == null)
                {
                    continue;
                }

                if (seen == want)
                {
                    sprite = set[i];
                    return true;
                }

                seen++;
            }

            sprite = set[0];
            return sprite != null;
        }

        static void HideLegacyIce(Transform tile)
        {
            var legacy = tile.Find(TileIceName);
            if (legacy != null)
            {
                legacy.gameObject.SetActive(false);
            }
        }

        static void HideExtraIce(Transform tile, int used)
        {
            var i = used;
            while (true)
            {
                var extra = tile.Find(TileIceName + i);
                if (extra == null)
                {
                    break;
                }

                extra.gameObject.SetActive(false);
                i++;
            }
        }

        void ApplyIceVisible(Transform tile)
        {
            if (tile == null)
            {
                return;
            }

            for (var i = 0; i < tile.childCount; i++)
            {
                var child = tile.GetChild(i);
                if (child.name.StartsWith(TileIceName, System.StringComparison.Ordinal))
                {
                    child.gameObject.SetActive(_iceVisible && _visible);
                }
            }
        }

        void ClearSideIceRoot()
        {
            if (_iceRoot == null)
            {
                return;
            }

            for (var i = _iceRoot.childCount - 1; i >= 0; i--)
            {
                var child = _iceRoot.GetChild(i).gameObject;
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

        Transform CreateSprite(string objectName, int order)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = order;
            return go.transform;
        }

        static void Paint(Transform target, Sprite sprite)
        {
            if (target == null)
            {
                return;
            }

            var renderer = target.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                return;
            }

            renderer.sprite = sprite;
            renderer.color = Color.white;
        }

        static void PlaceTile(Transform tile, float x, float y, float worldScale)
        {
            if (tile == null)
            {
                return;
            }

            tile.position = new Vector3(x, y, 3f);
            var lossy = tile.parent != null ? tile.parent.lossyScale : Vector3.one;
            tile.localScale = new Vector3(
                Mathf.Abs(lossy.x) > 0.0001f ? worldScale / lossy.x : worldScale,
                Mathf.Abs(lossy.y) > 0.0001f ? worldScale / lossy.y : worldScale,
                1f);
        }
    }
}
