using IceFishing.Model;
using UnityEngine;
using UnityEngine.UI;

namespace IceFishing.View
{
    /// <summary>
    /// 结算页单格渔获卡：预制体里摆好占位，运行时 Bind 填充数据。
    /// </summary>
    public sealed class SettleFishCardSlot : MonoBehaviour
    {
        const float ArtWidth = 292f;
        const float ArtHeight = 340f;
        const float StarIconBaseSize = 30f;
        const float RewardIconBaseSize = 38f;

        [SerializeField] Image _background;
        [SerializeField] RectTransform _starsRoot;
        [SerializeField] Image _fish;
        [SerializeField] Text _coinValue;
        [SerializeField] Text _shellValue;
        [SerializeField] Text _countLabel;

        public static string SlotName(int index)
        {
            return "FishCardSlot_" + index;
        }

        public static bool IsLayoutPlaceholderName(string objectName)
        {
            if (string.IsNullOrEmpty(objectName) || !objectName.StartsWith("FishCardSlot_"))
            {
                return false;
            }

            var suffix = objectName.Substring("FishCardSlot_".Length);
            return int.TryParse(suffix, out var index) && index >= 0 && index < 32;
        }

        /// <summary>运行时列表项：从预制体占位克隆，保留你在编辑器里调好的尺寸与边距。</summary>
        public static SettleFishCardSlot CloneRuntimeCard(SettleFishCardSlot template, Transform grid, int index)
        {
            var go = Object.Instantiate(template.gameObject, grid);
            go.name = "FishCard_" + index;
            var slot = go.GetComponent<SettleFishCardSlot>();
            slot.ClearPlaceholder();
            return slot;
        }

        public void SetActiveSlot(bool active)
        {
            gameObject.SetActive(active);
        }

        public void ClearPlaceholder()
        {
            ResolveRefs();
            if (_background != null)
            {
                _background.sprite = null;
                _background.type = Image.Type.Simple;
                _background.color = Color.white;
            }

            if (_starsRoot != null)
            {
                _starsRoot.gameObject.SetActive(false);
            }

            if (_fish != null)
            {
                _fish.sprite = null;
                _fish.gameObject.SetActive(false);
            }

            if (_coinValue != null)
            {
                _coinValue.text = string.Empty;
            }

            if (_shellValue != null)
            {
                _shellValue.text = string.Empty;
            }

            if (_countLabel != null)
            {
                _countLabel.text = string.Empty;
            }
        }

        public void Bind(FishSettleRow row, float layoutScale)
        {
            if (row == null)
            {
                ClearPlaceholder();
                return;
            }

            ResolveRefs();
            gameObject.SetActive(true);

            if (_background != null)
            {
                if (SettleUiSprites.Card != null)
                {
                    _background.sprite = SettleUiSprites.Card;
                    _background.type = Image.Type.Sliced;
                    _background.color = Color.white;
                }
                else
                {
                    _background.sprite = null;
                    _background.color = new Color(0.18f, 0.48f, 0.72f, 0.92f);
                }
            }

            BindStars(row.Stars, layoutScale);
            BindFish(row.Sprite);
            BindRewards(row.CoinEach, row.ShellEach);
            if (_countLabel != null)
            {
                _countLabel.text = "x" + Mathf.Max(1, row.Count);
            }
        }

        void BindStars(int stars, float scale)
        {
            if (_starsRoot == null)
            {
                return;
            }

            var starCount = Mathf.Clamp(stars, 1, 5);
            _starsRoot.gameObject.SetActive(true);
            var starSize = StarIconBaseSize * scale;
            var starPadX = 14f * scale;
            var starPadY = 12f * scale;
            _starsRoot.anchorMin = new Vector2(0f, 1f);
            _starsRoot.anchorMax = new Vector2(0f, 1f);
            _starsRoot.pivot = new Vector2(0f, 1f);
            _starsRoot.anchoredPosition = new Vector2(starPadX, -starPadY);
            _starsRoot.sizeDelta = new Vector2(starSize * starCount, starSize + 2f);

            for (var i = _starsRoot.childCount - 1; i >= 0; i--)
            {
                var child = _starsRoot.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }

            for (var i = 0; i < starCount; i++)
            {
                var star = CreateIcon(_starsRoot, "Star" + i, SettleUiSprites.Star, new Vector2(starSize, starSize));
                var starRt = star.rectTransform;
                starRt.anchorMin = starRt.anchorMax = new Vector2(0f, 0.5f);
                starRt.pivot = new Vector2(0f, 0.5f);
                starRt.anchoredPosition = new Vector2(i * starSize, 0f);
                if (star.sprite == null)
                {
                    star.color = new Color(1f, 0.84f, 0.25f, 1f);
                }
            }
        }

        /// <summary>只换鱼图；位置与大小请在预制体里调 Fish 节点的 RectTransform。</summary>
        void BindFish(Sprite sprite)
        {
            if (_fish == null)
            {
                return;
            }

            _fish.gameObject.SetActive(sprite != null);
            _fish.sprite = sprite;
            _fish.preserveAspect = true;
            _fish.color = Color.white;
        }

        void BindRewards(int coin, int shell)
        {
            if (_coinValue != null)
            {
                _coinValue.text = coin.ToString();
            }

            if (_shellValue != null)
            {
                _shellValue.text = shell.ToString();
            }
        }

        void ResolveRefs()
        {
            if (_background == null)
            {
                _background = GetComponent<Image>();
            }

            if (_starsRoot == null)
            {
                _starsRoot = transform.Find("Stars") as RectTransform;
            }

            if (_fish == null)
            {
                _fish = transform.Find("Fish")?.GetComponent<Image>();
            }

            if (_coinValue == null)
            {
                _coinValue = transform.Find("Rewards/Coin/Value")?.GetComponent<Text>();
            }

            if (_shellValue == null)
            {
                _shellValue = transform.Find("Rewards/Shell/Value")?.GetComponent<Text>();
            }

            if (_countLabel == null)
            {
                _countLabel = transform.Find("CountBadge/Label")?.GetComponent<Text>();
            }
        }

        public static SettleFishCardSlot CreatePlaceholder(Transform grid, int index, Vector2 cellSize)
        {
            var go = new GameObject(SlotName(index), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(SettleFishCardSlot));
            go.transform.SetParent(grid, false);
            var slot = go.GetComponent<SettleFishCardSlot>();
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = Color.white;
            image.sprite = null;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = cellSize;

            BuildStaticHierarchy(go.transform, cellSize);
            slot.ResolveRefs();
            slot.ClearPlaceholder();
            return slot;
        }

        static void BuildStaticHierarchy(Transform root, Vector2 cellSize)
        {
            var scale = Mathf.Min(cellSize.x / ArtWidth, cellSize.y / ArtHeight);

            var stars = new GameObject("Stars", typeof(RectTransform)).GetComponent<RectTransform>();
            stars.SetParent(root, false);
            stars.gameObject.SetActive(false);

            var fishGo = new GameObject("Fish", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fishGo.transform.SetParent(root, false);
            var fish = fishGo.GetComponent<Image>();
            fish.raycastTarget = false;
            fish.preserveAspect = true;
            fish.gameObject.SetActive(false);
            ApplyDefaultFishLayout(fish.rectTransform, scale);

            var rewards = new GameObject("Rewards", typeof(RectTransform)).GetComponent<RectTransform>();
            rewards.SetParent(root, false);
            rewards.anchorMin = new Vector2(0f, 0f);
            rewards.anchorMax = new Vector2(1f, 0f);
            rewards.pivot = new Vector2(0.5f, 0f);
            rewards.anchoredPosition = new Vector2(0f, 52f * scale);
            rewards.sizeDelta = new Vector2(-16f * scale, 40f * scale);

            CreateRewardChip(rewards, "Coin", CampUiSprites.FishCoin, -52f * scale, scale);
            CreateRewardChip(rewards, "Shell", CampUiSprites.Shell, 52f * scale, scale);

            var badge = UiFactory.CreatePanel(root, "CountBadge", Color.white);
            badge.raycastTarget = false;
            if (SettleUiSprites.CountBadge != null)
            {
                badge.sprite = SettleUiSprites.CountBadge;
                badge.type = Image.Type.Sliced;
            }
            else
            {
                badge.color = new Color(0.93f, 0.52f, 0.16f, 1f);
            }

            var badgeRt = badge.rectTransform;
            badgeRt.anchorMin = badgeRt.anchorMax = new Vector2(0.5f, 0f);
            badgeRt.pivot = new Vector2(0.5f, 0.5f);
            badgeRt.anchoredPosition = new Vector2(0f, 8f * scale);
            badgeRt.sizeDelta = new Vector2(124f * scale, 40f * scale);
            var countFont = Mathf.RoundToInt(26f * scale);
            var count = UiFactory.CreateText(badge.transform, "Label", string.Empty, countFont, Color.white, TextAnchor.MiddleCenter);
            Stretch(count.rectTransform);
        }

        static void ApplyDefaultFishLayout(RectTransform fishRt, float layoutScale)
        {
            var fishSize = 168f * layoutScale;
            fishRt.anchorMin = fishRt.anchorMax = new Vector2(0.5f, 0.58f);
            fishRt.pivot = new Vector2(0.5f, 0.5f);
            fishRt.anchoredPosition = Vector2.zero;
            fishRt.sizeDelta = new Vector2(fishSize, fishSize);
        }

        static void CreateRewardChip(RectTransform parent, string name, Sprite icon, float x, float scale)
        {
            var chip = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            chip.SetParent(parent, false);
            chip.anchorMin = chip.anchorMax = new Vector2(0.5f, 0.5f);
            chip.pivot = new Vector2(0.5f, 0.5f);
            chip.anchoredPosition = new Vector2(x, 0f);
            chip.sizeDelta = new Vector2(120f * scale, 40f * scale);

            var iconSize = RewardIconBaseSize * scale;
            var image = CreateIcon(chip, "Icon", icon, new Vector2(iconSize, iconSize));
            var imageRt = image.rectTransform;
            imageRt.anchorMin = imageRt.anchorMax = new Vector2(0f, 0.5f);
            imageRt.pivot = new Vector2(0f, 0.5f);
            imageRt.anchoredPosition = Vector2.zero;

            var label = UiFactory.CreateText(chip, "Value", string.Empty, Mathf.RoundToInt(26f * scale), Color.white, TextAnchor.MiddleLeft);
            var labelRt = label.rectTransform;
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.offsetMin = new Vector2(iconSize + 4f, 0f);
            labelRt.offsetMax = Vector2.zero;
        }

        static Image CreateIcon(Transform parent, string name, Sprite sprite, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.sprite = sprite;
            image.color = Color.white;
            image.rectTransform.sizeDelta = size;
            return image;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
