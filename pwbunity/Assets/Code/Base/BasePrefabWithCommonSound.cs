using Code.Prefab.Common;
using UnityEngine;

namespace Code.Base
{
    public class BasePrefabWithCommonSound : MonoBehaviour
    {
        private CommonSoundEffect _commonSoundEffect;

        protected virtual void Start()
        {
            _commonSoundEffect = GameObject.FindWithTag("CommonSoundEffect").GetComponent<CommonSoundEffect>();
        }

        public void PlayButtonClick()
        {
            _commonSoundEffect.PlayButtonClick();
        }

        protected void PlayError()
        {
            _commonSoundEffect.PlayError();
        }
    }
}