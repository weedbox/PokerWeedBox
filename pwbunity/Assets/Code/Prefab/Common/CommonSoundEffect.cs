using Code.Base;
using UnityEngine;

namespace Code.Prefab.Common
{
    public class CommonSoundEffect : BaseSoundEffect
    {
        [SerializeField] private AudioSource audioSourceButtonClick;
        [SerializeField] private AudioSource audioSourceError;
        
        private void Awake()
        {
            var soundEffectObjects = GameObject.FindGameObjectsWithTag("CommonSoundEffect");
            if (soundEffectObjects.Length > 1)
            {
                Destroy(gameObject);
            }

            DontDestroyOnLoad(gameObject);
        }

        public void StopAll()
        {
            audioSourceButtonClick.Stop();
            audioSourceError.Stop();
        }

        public void PlayButtonClick()
        {
            PlaySound(audioSourceButtonClick);
        }
        
        public void PlayError()
        {
            PlaySound(audioSourceError);
        }
    }
}