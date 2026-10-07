using System;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IceFishing.View
{
    /// <summary>
    /// 暂停弹窗。布局来自预制体。M1 的「返回营地」对应设计文档中的撤退，不扣饵。
    /// </summary>
    public sealed class PausePopupView : MonoBehaviour
    {
        public const string NodeName = "PausePopupView";
        public const string PrefabPath = "Assets/Prefabs/UI/PausePopupView.prefab";
        public const string PrefabResourcePath = "UI/PausePopupView";

        [SerializeField] Button _resumeButton;
        [SerializeField] Button _retreatButton;

        bool _wired;

        public event Action ResumeClicked;
        public event Action RetreatClicked;

        public static PausePopupView InstantiateOn(Transform canvas)
        {
            if (canvas == null)
            {
                return null;
            }

            var existing = canvas.Find(NodeName);
            if (existing != null)
            {
                return existing.GetComponent<PausePopupView>();
            }

            var prefab = LoadPrefab();
            if (prefab == null)
            {
                Debug.LogError("PausePopupView prefab missing. Expected " + PrefabPath);
                return null;
            }

            var instance = Instantiate(prefab, canvas, false);
            instance.name = NodeName;
            var rt = instance.GetComponent<RectTransform>();
            if (rt != null)
            {
                UiFactory.Stretch(rt);
            }

            return instance.GetComponent<PausePopupView>();
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

        public void Show()
        {
            ResolveMissingRefs();
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void Wire()
        {
            if (_wired || _resumeButton == null || _retreatButton == null)
            {
                return;
            }

            _resumeButton.onClick.AddListener(() => ResumeClicked?.Invoke());
            _retreatButton.onClick.AddListener(() => RetreatClicked?.Invoke());
            _wired = true;
        }

        void ResolveMissingRefs()
        {
            var panel = transform.Find("Panel");
            if (_resumeButton == null && panel != null)
            {
                _resumeButton = panel.Find("ResumeButton")?.GetComponent<Button>();
            }

            if (_retreatButton == null && panel != null)
            {
                _retreatButton = panel.Find("RetreatButton")?.GetComponent<Button>();
            }
        }
    }
}
