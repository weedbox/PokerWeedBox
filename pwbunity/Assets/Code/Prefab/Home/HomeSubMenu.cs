using Code.Base;
using Code.Helper;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Code.Prefab.Home
{
    public class HomeSubMenu : BasePrefabWithCommonSound
    {
        [SerializeField] private Button buttonBlankArea;
        [SerializeField] private Toggle toggleBGM;
        [SerializeField] private Toggle toggleSoundEffect;

        private UnityAction _refreshCallback;

        protected override void Start()
        {
            base.Start();

            buttonBlankArea.onClick.AddListener(() => { Destroy(gameObject); });
            toggleBGM.isOn = CommonHelper.GetBGMVolume() > 0;
            toggleSoundEffect.isOn = CommonHelper.GetSoundEffectVolume() > 0;
            toggleBGM.onValueChanged.AddListener(enable =>
            {
                PlayerPrefs.SetFloat(Constant.PfKeyBGMVolume, enable ? 1 : 0);
                CommonHelper.UpdateVolume();
            });
            toggleSoundEffect.onValueChanged.AddListener(enable =>
            {
                PlayerPrefs.SetFloat(Constant.PfKeySoundEffectVolume, enable ? 1 : 0);
                CommonHelper.UpdateVolume();
            });
        }
    }
}