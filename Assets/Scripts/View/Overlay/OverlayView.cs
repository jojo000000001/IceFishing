using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IceFishing.View
{
    /// <summary>
    /// 通用说明弹窗。布局来自预制体。
    /// </summary>
    public sealed class OverlayView : MonoBehaviour
    {
        public const string NodeName = "OverlayView";
        public const string PrefabPath = "Assets/Prefabs/UI/OverlayView.prefab";
        public const string PrefabResourcePath = "UI/OverlayView";

        [SerializeField] Text _title;
        [SerializeField] Text _body;
        [SerializeField] Button _closeButton;

        bool _wired;

        public static OverlayView InstantiateOn(Transform canvas)
        {
            if (canvas == null)
            {
                return null;
            }

            var existing = canvas.Find(NodeName);
            if (existing != null)
            {
                return existing.GetComponent<OverlayView>();
            }

            var prefab = LoadPrefab();
            if (prefab == null)
            {
                Debug.LogError("OverlayView prefab missing. Expected " + PrefabPath);
                return null;
            }

            var instance = Instantiate(prefab, canvas, false);
            instance.name = NodeName;
            var rt = instance.GetComponent<RectTransform>();
            if (rt != null)
            {
                UiFactory.Stretch(rt);
            }

            return instance.GetComponent<OverlayView>();
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

        [ContextMenu("Bind Prefab Refs")]
        public void BindPrefabRefs()
        {
            ResolveMissingRefs();
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
            }
#endif
        }

        void Awake()
        {
            ResolveMissingRefs();
            Wire();
        }

        public void Show(string title, string body)
        {
            ResolveMissingRefs();
            ApplyFont();
            if (_title != null)
            {
                _title.text = title;
            }

            if (_body != null)
            {
                _body.text = body;
            }

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void Wire()
        {
            if (_wired || _closeButton == null)
            {
                return;
            }

            _closeButton.onClick.AddListener(Hide);
            _wired = true;
        }

        void ResolveMissingRefs()
        {
            var panel = transform.Find("Panel");
            if (_title == null && panel != null)
            {
                _title = panel.Find("Title")?.GetComponent<Text>();
            }

            if (_body == null && panel != null)
            {
                _body = panel.Find("Body")?.GetComponent<Text>();
            }

            if (_closeButton == null && panel != null)
            {
                _closeButton = panel.Find("CloseButton")?.GetComponent<Button>();
            }
        }

        void ApplyFont()
        {
            var font = UiFactory.ResolveFont();
            if (_title != null)
            {
                _title.font = font;
            }

            if (_body != null)
            {
                _body.font = font;
            }
        }
    }
}
