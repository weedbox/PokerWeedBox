using Code.Helper;
using UnityEngine;

namespace Code.Base
{
    public class BaseSoundEffect : MonoBehaviour
    {
        protected static void PlaySound(AudioSource audioSource)
        {
            var volume = CommonHelper.GetSoundEffectVolume();
            if (volume == 0f) return;

            audioSource.volume = volume;
            audioSource.Play();
        }
    }
}