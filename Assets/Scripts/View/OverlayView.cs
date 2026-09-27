using System;
using UnityEngine;
using UnityEngine.UI;

namespace IceFishing.View
{
    /// <summary>
    /// 通用说明弹窗。渔具 / 图鉴 / 设置在 M1 用文案占位，不进入独立界面。
    /// </summary>
    public sealed class OverlayView : MonoBehaviour
    {
        [SerializeField] Text _title;
        [SerializeField] Text _body;
        [SerializeField] Button _closeButton;
        [SerializeField] GameObject _toggleRoot;
        [SerializeField] Toggle _infiniteBaitToggle;

        bool _wired;
        Action<bool> _toggleChanged;

        public void Configure(Text title, Text body, Button close, GameObject toggleRoot, Toggle infiniteBaitToggle)
        {
            _title = title;
            _body = body;
            _closeButton = close;
            _toggleRoot = toggleRoot;
            _infiniteBaitToggle = infiniteBaitToggle;
            Wire();
        }

        void Awake()
        {
            Wire();
        }

        public void Show(string title, string body, bool showInfiniteBait = false, bool infiniteBaitOn = false, Action<bool> onToggle = null)
        {
            ApplyFont();
            if (_title != null)
            {
                _title.text = title;
            }

            if (_body != null)
            {
                _body.text = body;
            }

            _toggleChanged = onToggle;
            if (_toggleRoot != null)
            {
                _toggleRoot.SetActive(showInfiniteBait);
            }

            if (_infiniteBaitToggle != null)
            {
                _infiniteBaitToggle.isOn = infiniteBaitOn;
            }

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            _toggleChanged = null;
        }

        void Wire()
        {
            if (_wired || _closeButton == null)
            {
                return;
            }

            _closeButton.onClick.AddListener(Hide);
            if (_infiniteBaitToggle != null)
            {
                _infiniteBaitToggle.onValueChanged.AddListener(OnToggle);
            }

            _wired = true;
        }

        void OnToggle(bool isOn)
        {
            _toggleChanged?.Invoke(isOn);
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
