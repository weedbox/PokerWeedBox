using Code.Base;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Code.Prefab.Common
{
    public class CommonDialog : BasePrefabWithCommonSound
    {
        [SerializeField] private TMP_Text textTitle;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private TMP_Text textDesc;
        [SerializeField] private Button buttonPositive;
        [SerializeField] private TMP_Text textPositive;
        [SerializeField] private Button buttonNegative;
        [SerializeField] private TMP_Text textNegative;

        private UnityAction _positiveCallback;
        private UnityAction _negativeCallback;

        private bool _playErrorSound;

        protected override void Start()
        {
            base.Start();
            
            buttonPositive.onClick.AddListener(() => _positiveCallback?.Invoke());
            buttonNegative.onClick.AddListener(() => _negativeCallback?.Invoke());

            if (_playErrorSound)
            {
                PlayError();   
            }
        }

        public void SetValues(
            [CanBeNull] string title,
            string message, [CanBeNull] string positive,
            [CanBeNull] string negative)
        {
            if (!string.IsNullOrEmpty(title))
            {
                textTitle.text = title;
            }

            textDesc.text = message;

            if (!string.IsNullOrEmpty(positive))
            {
                textPositive.text = positive;
                buttonPositive.gameObject.SetActive(true);
            }
            else
            {
                buttonPositive.gameObject.SetActive(false);
            }

            if (!string.IsNullOrEmpty(negative))
            {
                textNegative.text = negative;
                buttonNegative.gameObject.SetActive(true);
            }
            else
            {
                buttonNegative.gameObject.SetActive(false);
            }
        }

        public void SetCallbacks([CanBeNull] UnityAction positiveCallback, [CanBeNull] UnityAction negativeCallback)
        {
            _positiveCallback = positiveCallback;
            _negativeCallback = negativeCallback;
        }

        public void SetPlayerSound(bool playErrorSound)
        {
            _playErrorSound = playErrorSound;
        }

        public void ScrollContentToTop()
        {
            scrollRect.verticalNormalizedPosition = 1;
        }
    }
}