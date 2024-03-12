using Code.Helper;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Code.Prefab.Login
{
    public class LoginAccount : MonoBehaviour
    {
        [SerializeField] private Button buttonGameObject;
        [SerializeField] private Image imageAvatarBackground;
        [SerializeField] private Image imageAvatar;
        [SerializeField] private Image imageNameBackground;
        [SerializeField] private TMP_Text textName;

        [SerializeField] private Sprite spriteAvatarSelected;
        [SerializeField] private Sprite spriteAvatarNormal;
        [SerializeField] private Sprite spriteNameSelected;
        [SerializeField] private Sprite spriteNameNormal;

        private int _index = -1;
        private UnityAction<int> _callback;

        private void Start()
        {
            buttonGameObject.onClick.AddListener(() => _callback?.Invoke(_index));
        }

        public void Setup(int value, UnityAction<int> onAccountSelected)
        {
            _index = value;
            _callback = onAccountSelected;

            textName.text = Constant.LoginNames[value];
            imageAvatar.sprite = Resources.Load<Sprite>("Art/Image/Common/Avatar/" + Constant.LogonAvatars[value]);
        }

        public void SetClickable(bool clickable)
        {
            buttonGameObject.interactable = clickable;
        }

        public void UpdateSelectStatus(int currentIndex)
        {
            imageAvatarBackground.sprite = _index == currentIndex ? spriteAvatarSelected : spriteAvatarNormal;
            imageNameBackground.sprite = _index == currentIndex ? spriteNameSelected : spriteNameNormal;
        }
    }
}