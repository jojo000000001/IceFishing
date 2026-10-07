using System;
using System.Collections;
using System.Collections.Generic;
using IceFishing.Controller;
using IceFishing.Model;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IceFishing.View
{
    /// <summary>
    /// 回营地时：全部渔获同时飞到天空散开，缩小后变成鱼币飞向 Hub 右上角鱼币图标。
    /// 飞出的钩、鱼、数字从预制体模板克隆，播完销毁。
    /// </summary>
    public sealed class CampRewardPresentationView : MonoBehaviour
    {
        public const string NodeName = "CampRewardPresentation";
        public const string PrefabPath = "Assets/Prefabs/UI/CampRewardPresentation.prefab";
        public const string PrefabResourcePath = "UI/CampRewardPresentation";
        public const string FishCoinSpritePath = "Assets/Art/UI/IconFishCoin.png";
        public const string ShellSpritePath = "Assets/Art/UI/IconShell.png";

        const float PullDuration = 0.45f;
        const float FlyDuration = 0.7f;
        const float SpreadHold = 0.16f;
        const float ShrinkDuration = 0.28f;
        const float CoinSfxLead = 0.15f;
        const float CoinPopDuration = 0.16f;
        const float CoinFlyDuration = 0.42f;
        const float SpawnAnchorY = 0.2f;
        const float SkyAnchorY = 0.72f;

        [SerializeField] Image _imageTemplate;
        [SerializeField] Text _labelTemplate;

        RectTransform _root;
        Canvas _canvas;
        bool _playing;

        public bool IsPlaying
        {
            get { return _playing; }
        }

        public static CampRewardPresentationView InstantiateOn(Transform canvasRoot)
        {
            if (canvasRoot == null)
            {
                return null;
            }

            var existing = canvasRoot.Find(NodeName);
            if (existing != null)
            {
                return existing.GetComponent<CampRewardPresentationView>();
            }

            var prefab = LoadPrefab();
            if (prefab == null)
            {
                Debug.LogError("CampRewardPresentation prefab missing. Expected " + PrefabPath);
                return null;
            }

            var instance = Instantiate(prefab, canvasRoot, false);
            instance.name = NodeName;
            var rt = instance.GetComponent<RectTransform>();
            Stretch(rt);
            return instance.GetComponent<CampRewardPresentationView>();
        }

        public static GameObject LoadPrefab()
        {
#if UNITY_EDITOR
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (asset != null)
            {
                return asset;
            }
#endif
            return Resources.Load<GameObject>(PrefabResourcePath);
        }

        void Awake()
        {
            _root = transform as RectTransform;
            _canvas = GetComponentInParent<Canvas>();
            Stretch(_root);
            HideTemplates();
        }

        public void Play(
            IReadOnlyList<FishDefinition> caughtFish,
            PlayerProfile profile,
            RectTransform tokenAnchor,
            Action onComplete)
        {
            StopAllCoroutines();
            StartCoroutine(PlayRoutine(caughtFish, profile, tokenAnchor, onComplete));
        }

        IEnumerator PlayRoutine(
            IReadOnlyList<FishDefinition> caughtFish,
            PlayerProfile profile,
            RectTransform tokenAnchor,
            Action onComplete)
        {
            _playing = true;
            transform.SetAsLastSibling();
            ClearChildren();

            var font = UiFactory.ResolveFont();
            yield return PullLineRoutine();

            var totalCoins = 0;
            var totalShells = 0;
            if (caughtFish != null && caughtFish.Count > 0)
            {
                for (var i = 0; i < caughtFish.Count; i++)
                {
                    var def = caughtFish[i];
                    totalCoins += def != null ? def.TokenValue : 0;
                    totalShells += def != null ? def.ShellValue : 0;
                }

                yield return ThrowAllFishRoutine(caughtFish, tokenAnchor, font);
            }
            else
            {
                yield return new WaitForSeconds(0.12f);
            }

            if (profile != null)
            {
                if (totalCoins > 0)
                {
                    profile.Tokens += totalCoins;
                }

                if (totalShells > 0)
                {
                    profile.Shells += totalShells;
                }
            }

            ClearChildren();
            _playing = false;
            onComplete?.Invoke();
        }

        IEnumerator PullLineRoutine()
        {
            MusicController.Ensure().PlaySurface();
            var hook = SpawnImage("PullHook", Color.white, 72f);
            if (hook == null)
            {
                yield break;
            }

            var sprite = LoadHookSprite();
            if (sprite != null)
            {
                hook.sprite = sprite;
                hook.preserveAspect = true;
            }
            else
            {
                hook.color = new Color(0.72f, 0.48f, 0.28f, 0.95f);
            }

            var rt = hook.rectTransform;
            var start = AnchorToLocal(new Vector2(0.5f, SpawnAnchorY));
            var end = start + new Vector2(0f, 280f);
            rt.anchoredPosition = start;
            var elapsed = 0f;
            while (elapsed < PullDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / PullDuration);
                var ease = Mathf.SmoothStep(0f, 1f, t);
                rt.anchoredPosition = Vector2.Lerp(start, end, ease);
                hook.color = new Color(hook.color.r, hook.color.g, hook.color.b, 1f - ease * 0.35f);
                yield return null;
            }

            Destroy(hook.gameObject);
        }

        IEnumerator ThrowAllFishRoutine(
            IReadOnlyList<FishDefinition> caughtFish,
            RectTransform tokenAnchor,
            Font font)
        {
            var count = caughtFish.Count;
            var hole = AnchorToLocal(new Vector2(0.5f, SpawnAnchorY));
            var sky = AnchorToLocal(new Vector2(0.5f, SkyAnchorY));
            var token = tokenAnchor != null
                ? LocalPointInRoot(tokenAnchor)
                : AnchorToLocal(new Vector2(0.12f, 0.92f));
            var spreadX = _root.rect.width * Mathf.Lerp(0.2f, 0.34f, Mathf.InverseLerp(1f, 8f, count));
            var spreadY = _root.rect.height * 0.055f;
            var coinSprite = CampUiSprites.FishCoin;
            var hasCoinSprite = coinSprite != null;

            var items = new List<FlyItem>(count);
            for (var i = 0; i < count; i++)
            {
                var def = caughtFish[i];
                var size = FishUiSize(def);
                var fish = SpawnImage("ThrowFish", Color.white, size);
                if (fish == null)
                {
                    continue;
                }

                var sprite = ResolveFishSprite(def);
                if (sprite != null)
                {
                    fish.sprite = sprite;
                    fish.preserveAspect = true;
                }
                else
                {
                    fish.color = new Color(0.45f, 0.72f, 0.95f, 1f);
                }

                var start = hole + new Vector2(UnityEngine.Random.Range(-36f, 36f), UnityEngine.Random.Range(-10f, 18f));
                var peak = sky + ClusterOffset(i, count, spreadX, spreadY);
                var rt = fish.rectTransform;
                rt.anchoredPosition = start;
                rt.localScale = Vector3.one * 0.55f;
                rt.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-18f, 18f));
                items.Add(new FlyItem
                {
                    Image = fish,
                    Rt = rt,
                    Start = start,
                    Peak = peak,
                    Reward = def != null ? def.TokenValue : 0,
                    Sway = UnityEngine.Random.Range(-70f, 70f),
                    Spin = UnityEngine.Random.Range(-40f, 40f)
                });
            }

            var elapsed = 0f;
            while (elapsed < FlyDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / FlyDuration);
                var ease = Mathf.SmoothStep(0f, 1f, t);
                for (var i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    var pos = Vector2.Lerp(item.Start, item.Peak, ease);
                    pos.x += Mathf.Sin(t * Mathf.PI) * item.Sway;
                    item.Rt.anchoredPosition = pos;
                    item.Rt.localScale = Vector3.one * Mathf.Lerp(0.55f, 1f, ease);
                    item.Rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(item.Spin * 0.35f, item.Spin, ease));
                }

                yield return null;
            }

            if (SpreadHold > 0f)
            {
                yield return new WaitForSeconds(SpreadHold);
            }

            elapsed = 0f;
            var coinSfxPlayed = false;
            while (elapsed < ShrinkDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / ShrinkDuration);
                var ease = t * t;
                for (var i = 0; i < items.Count; i++)
                {
                    items[i].Rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.12f, ease);
                }

                if (!coinSfxPlayed && items.Count > 0 && elapsed >= ShrinkDuration - CoinSfxLead)
                {
                    MusicController.Ensure().PlayCoin();
                    coinSfxPlayed = true;
                }

                yield return null;
            }

            if (!coinSfxPlayed && items.Count > 0)
            {
                MusicController.Ensure().PlayCoin();
            }

            var labels = new List<Text>(count);
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                ApplyCoinLook(item.Image, coinSprite, hasCoinSprite);
                item.Rt.sizeDelta = new Vector2(88f, 88f);
                item.Rt.localScale = Vector3.one * 0.2f;
                item.Rt.localRotation = Quaternion.identity;
                if (item.Reward > 0)
                {
                    var label = SpawnLabel("CoinLabel", "+" + item.Reward, font, 32);
                    if (label == null)
                    {
                        continue;
                    }
                    label.rectTransform.anchoredPosition = item.Rt.anchoredPosition + new Vector2(36f, 6f);
                    labels.Add(label);
                }
            }

            elapsed = 0f;
            while (elapsed < CoinPopDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / CoinPopDuration);
                var pop = Mathf.SmoothStep(0.2f, 0.95f, t);
                for (var i = 0; i < items.Count; i++)
                {
                    items[i].Rt.localScale = Vector3.one * pop;
                }

                yield return null;
            }

            var coinStarts = new Vector2[items.Count];
            var labelStarts = new Vector2[labels.Count];
            for (var i = 0; i < items.Count; i++)
            {
                coinStarts[i] = items[i].Rt.anchoredPosition;
            }

            for (var i = 0; i < labels.Count; i++)
            {
                labelStarts[i] = labels[i].rectTransform.anchoredPosition;
            }

            elapsed = 0f;
            while (elapsed < CoinFlyDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / CoinFlyDuration);
                var ease = t * t;
                for (var i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    item.Rt.anchoredPosition = Vector2.Lerp(coinStarts[i], token, ease);
                    item.Rt.localScale = Vector3.one * Mathf.Lerp(0.95f, 0.45f, ease);
                    var c = item.Image.color;
                    item.Image.color = new Color(c.r, c.g, c.b, 1f - ease * 0.2f);
                }

                for (var i = 0; i < labels.Count; i++)
                {
                    labels[i].rectTransform.anchoredPosition = Vector2.Lerp(
                        labelStarts[i],
                        token + new Vector2(36f, 0f),
                        ease);
                    var c = labels[i].color;
                    labels[i].color = new Color(c.r, c.g, c.b, 1f - ease * 0.35f);
                }

                yield return null;
            }

            for (var i = 0; i < items.Count; i++)
            {
                if (items[i].Image != null)
                {
                    Destroy(items[i].Image.gameObject);
                }
            }

            for (var i = 0; i < labels.Count; i++)
            {
                if (labels[i] != null)
                {
                    Destroy(labels[i].gameObject);
                }
            }
        }

        static Vector2 ClusterOffset(int index, int count, float spreadX, float spreadY)
        {
            if (count <= 1)
            {
                return new Vector2(UnityEngine.Random.Range(-22f, 22f), UnityEngine.Random.Range(-12f, 12f));
            }

            var angle = index * 2.399963f + count * 0.13f;
            var radius = Mathf.Sqrt((index + 0.55f) / count);
            return new Vector2(
                Mathf.Cos(angle) * radius * spreadX + UnityEngine.Random.Range(-22f, 22f),
                Mathf.Sin(angle) * radius * spreadY + UnityEngine.Random.Range(-16f, 16f));
        }

        static float FishUiSize(FishDefinition definition)
        {
            var world = definition != null ? definition.WorldHeight : 0.9f;
            return Mathf.Lerp(88f, 168f, Mathf.InverseLerp(0.35f, 1.2f, world));
        }

        static void ApplyCoinLook(Image image, Sprite coinSprite, bool hasCoinSprite)
        {
            if (image == null)
            {
                return;
            }

            if (hasCoinSprite)
            {
                image.sprite = coinSprite;
                image.preserveAspect = true;
                image.color = Color.white;
            }
            else
            {
                image.sprite = null;
                image.color = new Color(1f, 0.86f, 0.35f, 1f);
            }
        }

        sealed class FlyItem
        {
            public Image Image;
            public RectTransform Rt;
            public Vector2 Start;
            public Vector2 Peak;
            public int Reward;
            public float Sway;
            public float Spin;
        }

        Vector2 AnchorToLocal(Vector2 anchor)
        {
            if (_root == null)
            {
                return Vector2.zero;
            }

            var size = _root.rect.size;
            return new Vector2(
                (anchor.x - _root.pivot.x) * size.x,
                (anchor.y - _root.pivot.y) * size.y);
        }

        Vector2 LocalPointInRoot(RectTransform target)
        {
            if (_root == null || target == null)
            {
                return Vector2.zero;
            }

            var world = target.TransformPoint(target.rect.center);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _root,
                RectTransformUtility.WorldToScreenPoint(_canvas != null ? _canvas.worldCamera : null, world),
                _canvas != null ? _canvas.worldCamera : null,
                out var local);
            return local;
        }

        Image SpawnImage(string name, Color color, float size)
        {
            if (_imageTemplate == null || _root == null)
            {
                return null;
            }

            var image = Instantiate(_imageTemplate, _root, false);
            image.gameObject.SetActive(true);
            image.gameObject.name = name;
            image.color = color;
            image.rectTransform.sizeDelta = new Vector2(size, size);
            return image;
        }

        Text SpawnLabel(string name, string text, Font font, int fontSize)
        {
            if (_labelTemplate == null || _root == null)
            {
                return null;
            }

            var label = Instantiate(_labelTemplate, _root, false);
            label.gameObject.SetActive(true);
            label.gameObject.name = name;
            if (font != null)
            {
                label.font = font;
            }

            label.fontSize = fontSize;
            label.text = text;
            return label;
        }

        void HideTemplates()
        {
            if (_imageTemplate != null)
            {
                _imageTemplate.gameObject.SetActive(false);
            }

            if (_labelTemplate != null)
            {
                _labelTemplate.gameObject.SetActive(false);
            }
        }

        static Sprite ResolveFishSprite(FishDefinition definition)
        {
            if (definition == null || definition.Prefab == null)
            {
                return null;
            }

            var renderer = definition.Prefab.GetComponentInChildren<SpriteRenderer>(true);
            return renderer != null ? renderer.sprite : null;
        }

        static Sprite LoadHookSprite()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/FishingAnchor.png");
#else
            return null;
#endif
        }

        void ClearChildren()
        {
            if (_root == null)
            {
                return;
            }

            for (var i = _root.childCount - 1; i >= 0; i--)
            {
                var child = _root.GetChild(i);
                if (IsTemplate(child))
                {
                    continue;
                }

                Destroy(child.gameObject);
            }
        }

        bool IsTemplate(Transform child)
        {
            return (_imageTemplate != null && child == _imageTemplate.transform)
                || (_labelTemplate != null && child == _labelTemplate.transform);
        }

        static void Stretch(RectTransform rt)
        {
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
