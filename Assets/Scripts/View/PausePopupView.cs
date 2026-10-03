using System;
using UnityEngine;
using UnityEngine.UI;

namespace IceFishing.View
{
    /// <summary>
    /// 暂停弹窗。M1 的「返回营地」对应设计文档中的撤退，不扣饵。
    /// </summary>
    public sealed class PausePopupView : MonoBehaviour
    {
        [SerializeField] Button _resumeButton;
        [SerializeField] Button _retreatButton;

        bool _wired;

        public event Action ResumeClicked;
        public event Action RetreatClicked;

        public void Configure(Button resume, Button retreat)
        {
            _resumeButton = resume;
            _retreatButton = retreat;
            Wire();
        }

        void Awake()
        {
            EnsurePresentation();
            Wire();
        }

        public void Show()
        {
            EnsurePresentation();
            gameObject.SetActive(true);
        }

        void EnsurePresentation()
        {
            if (transform.Find("Panel/Icon") == null)
            {
                UiFactory.PopulatePause(transform);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void Wire()
        {
            if (_wired || _resumeButton == null)
            {
                return;
            }

            _resumeButton.onClick.AddListener(() => ResumeClicked?.Invoke());
            _retreatButton.onClick.AddListener(() => RetreatClicked?.Invoke());
            _wired = true;
        }
    }
}
