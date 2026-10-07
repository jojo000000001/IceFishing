using System.Collections;
using UnityEngine;

namespace IceFishing.Controller
{
    /// <summary>
    /// 全局音乐单例：营地 BGM、海底 BGM 与音效。开钓切海底，回营切营地。
    /// </summary>
    public sealed class MusicController : MonoBehaviour
    {
        public const string NodeName = "MusicController";
        public const string CampBgmPath = "Assets/Music/bgm/Ice Cave.mp3";
        public const string UnderwaterBgmPath = "Assets/Music/bgm/Kim Lightyear - Under The Sea.mp3";
        public const string ProtectionHitSfxPath = "Assets/Music/sounds/knifesharpener2.flac";
        public const string CatchSfxPath = "Assets/Music/sounds/Plop.ogg";
        public const string CoinSfxPath = "Assets/Music/sounds/hjm-coindrop_v1.wav";
        public const string SurfaceSfxPath = "Assets/Music/sounds/splash1.wav";
        public const string SettleSfxPath = "Assets/Music/sounds/splash2.wav";

        const float DefaultBgmVolume = 0.42f;
        const float DefaultSfxVolume = 0.8f;
        const float CrossfadeSeconds = 1.15f;

        [SerializeField] AudioClip _campBgm;
        [SerializeField] AudioClip _underwaterBgm;
        [SerializeField] AudioClip _protectionHitSfx;
        [SerializeField] AudioClip _catchSfx;
        [SerializeField] AudioClip _coinSfx;
        [SerializeField] AudioClip _surfaceSfx;
        [SerializeField] AudioClip _settleSfx;
        [SerializeField] AudioSource _bgmA;
        [SerializeField] AudioSource _bgmB;
        [SerializeField] AudioSource _sfxSource;
        [SerializeField] [Range(0f, 1f)] float _bgmVolume = DefaultBgmVolume;
        [SerializeField] [Range(0f, 1f)] float _sfxVolume = DefaultSfxVolume;

        AudioSource _activeBgm;
        Coroutine _fadeRoutine;
        AudioClip _requestedBgm;

        public static MusicController Instance { get; private set; }

        public static MusicController Ensure()
        {
            if (Instance != null)
            {
                return Instance;
            }

            var existing = FindObjectOfType<MusicController>();
            if (existing != null)
            {
                Instance = existing;
                existing.EnsureSources();
                existing.ResolveClips();
                return existing;
            }

            var go = new GameObject(NodeName);
            var created = go.AddComponent<MusicController>();
            created.EnsureSources();
            created.ResolveClips();
            return created;
        }

        public void EditorAssign(
            AudioClip campBgm,
            AudioClip underwaterBgm,
            AudioClip protectionHitSfx,
            AudioClip catchSfx = null,
            AudioClip coinSfx = null,
            AudioClip surfaceSfx = null,
            AudioClip settleSfx = null)
        {
            _campBgm = campBgm;
            _underwaterBgm = underwaterBgm;
            _protectionHitSfx = protectionHitSfx;
            _catchSfx = catchSfx;
            _coinSfx = coinSfx;
            _surfaceSfx = surfaceSfx;
            _settleSfx = settleSfx;
            EnsureSources();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureSources();
            ResolveClips();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void PlayCampBgm()
        {
            PlayBgm(ResolveCampClip());
        }

        public void PlayUnderwaterBgm()
        {
            PlayBgm(ResolveUnderwaterClip());
        }

        public void PlayProtectionHit()
        {
            PlaySfx(ResolveProtectionHitClip());
        }

        public void PlayCatch()
        {
            PlaySfx(ResolveCatchClip());
        }

        public void PlayCoin()
        {
            PlaySfx(ResolveCoinClip());
        }

        public void PlaySurface()
        {
            PlaySfx(ResolveSurfaceClip());
        }

        public void PlaySettle()
        {
            PlaySfx(ResolveSettleClip());
        }

        public void PlaySfx(AudioClip clip)
        {
            EnsureSources();
            if (_sfxSource == null || clip == null)
            {
                return;
            }

            _sfxSource.PlayOneShot(clip, _sfxVolume);
        }

        void PlayBgm(AudioClip clip)
        {
            EnsureSources();
            if (clip == null || _bgmA == null || _bgmB == null)
            {
                return;
            }

            if (_requestedBgm == clip && _activeBgm != null && _activeBgm.isPlaying && _activeBgm.clip == clip)
            {
                return;
            }

            _requestedBgm = clip;
            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
            }

            _fadeRoutine = StartCoroutine(CrossfadeTo(clip));
        }

        IEnumerator CrossfadeTo(AudioClip clip)
        {
            var incoming = _activeBgm == _bgmA ? _bgmB : _bgmA;
            var outgoing = _activeBgm;

            incoming.clip = clip;
            incoming.loop = true;
            incoming.volume = 0f;
            incoming.Play();

            var duration = Mathf.Max(0.05f, CrossfadeSeconds);
            var elapsed = 0f;
            var fromVolume = outgoing != null ? outgoing.volume : 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                incoming.volume = Mathf.Lerp(0f, _bgmVolume, t);
                if (outgoing != null && outgoing != incoming)
                {
                    outgoing.volume = Mathf.Lerp(fromVolume, 0f, t);
                }

                yield return null;
            }

            incoming.volume = _bgmVolume;
            if (outgoing != null && outgoing != incoming)
            {
                outgoing.Stop();
                outgoing.clip = null;
                outgoing.volume = 0f;
            }

            _activeBgm = incoming;
            _fadeRoutine = null;
        }

        public void EnsureSources()
        {
            _bgmA = EnsureBgmSource(_bgmA, "BgmA");
            _bgmB = EnsureBgmSource(_bgmB, "BgmB");
            if (_sfxSource == null)
            {
                _sfxSource = gameObject.AddComponent<AudioSource>();
            }

            ConfigureSfx(_sfxSource);
        }

        AudioSource EnsureBgmSource(AudioSource existing, string childName)
        {
            if (existing != null)
            {
                ConfigureBgm(existing);
                return existing;
            }

            var child = transform.Find(childName);
            if (child == null)
            {
                var go = new GameObject(childName);
                go.transform.SetParent(transform, false);
                child = go.transform;
            }

            var source = child.GetComponent<AudioSource>();
            if (source == null)
            {
                source = child.gameObject.AddComponent<AudioSource>();
            }

            ConfigureBgm(source);
            return source;
        }

        static void ConfigureBgm(AudioSource source)
        {
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
        }

        static void ConfigureSfx(AudioSource source)
        {
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
        }

        void ResolveClips()
        {
            if (_campBgm == null)
            {
                _campBgm = LoadClip(CampBgmPath);
            }

            if (_underwaterBgm == null)
            {
                _underwaterBgm = LoadClip(UnderwaterBgmPath);
            }

            if (_protectionHitSfx == null)
            {
                _protectionHitSfx = LoadClip(ProtectionHitSfxPath);
            }

            if (_catchSfx == null)
            {
                _catchSfx = LoadClip(CatchSfxPath);
            }

            if (_coinSfx == null)
            {
                _coinSfx = LoadClip(CoinSfxPath);
            }

            if (_surfaceSfx == null)
            {
                _surfaceSfx = LoadClip(SurfaceSfxPath);
            }

            if (_settleSfx == null)
            {
                _settleSfx = LoadClip(SettleSfxPath);
            }
        }

        AudioClip ResolveCampClip()
        {
            if (_campBgm == null)
            {
                _campBgm = LoadClip(CampBgmPath);
            }

            return _campBgm;
        }

        AudioClip ResolveUnderwaterClip()
        {
            if (_underwaterBgm == null)
            {
                _underwaterBgm = LoadClip(UnderwaterBgmPath);
            }

            return _underwaterBgm;
        }

        AudioClip ResolveProtectionHitClip()
        {
            if (_protectionHitSfx == null)
            {
                _protectionHitSfx = LoadClip(ProtectionHitSfxPath);
            }

            return _protectionHitSfx;
        }

        AudioClip ResolveCatchClip()
        {
            if (_catchSfx == null)
            {
                _catchSfx = LoadClip(CatchSfxPath);
            }

            return _catchSfx;
        }

        AudioClip ResolveCoinClip()
        {
            if (_coinSfx == null)
            {
                _coinSfx = LoadClip(CoinSfxPath);
            }

            return _coinSfx;
        }

        AudioClip ResolveSurfaceClip()
        {
            if (_surfaceSfx == null)
            {
                _surfaceSfx = LoadClip(SurfaceSfxPath);
            }

            return _surfaceSfx;
        }

        AudioClip ResolveSettleClip()
        {
            if (_settleSfx == null)
            {
                _settleSfx = LoadClip(SettleSfxPath);
            }

            return _settleSfx;
        }

        static AudioClip LoadClip(string assetPath)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
#else
            return null;
#endif
        }
    }
}
