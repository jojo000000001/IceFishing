using IceFishing.Model;
using UnityEngine;
using UnityEngine.UI;

namespace IceFishing.View
{
    /// <summary>
    /// 结算渔获卡：预制体里摆好层级，运行时只 Bind 数据和显隐。
    /// </summary>
    public sealed class SettleFishCardSlot : MonoBehaviour
    {
        const int MaxStars = 5;

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

            BindStars(0);
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

        public void Bind(FishSettleRow row)
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

            BindStars(row.Stars);
            BindFish(row.Sprite);
            BindRewards(row.CoinEach, row.ShellEach);
            if (_countLabel != null)
            {
                _countLabel.text = "x" + Mathf.Max(1, row.Count);
            }
        }

        void BindStars(int stars)
        {
            if (_starsRoot == null)
            {
                return;
            }

            var starCount = Mathf.Clamp(stars, 0, MaxStars);
            _starsRoot.gameObject.SetActive(starCount > 0);
            var shown = 0;
            for (var i = 0; i < _starsRoot.childCount; i++)
            {
                var on = shown < starCount;
                _starsRoot.GetChild(i).gameObject.SetActive(on);
                if (on)
                {
                    shown++;
                }
            }
        }

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
    }
}
