using System;
using Code.Helper;
using UnityEngine;

namespace Code.Base
{
    public class BackgroundMusic : MonoBehaviour
    {
        private AudioSource _audioSource;

        private void Awake()
        {
            var backgroundMusicObjects = GameObject.FindGameObjectsWithTag("BackgroundMusic");
            if (backgroundMusicObjects.Length > 1)
            {
                Destroy(gameObject);
            }

            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            var obj = GameObject.FindWithTag("BackgroundMusic");
            _audioSource = obj.GetComponent<AudioSource>();
            _audioSource.volume = CommonHelper.GetBGMVolume();
        }

        private void Update()
        {
            if (Math.Abs(_audioSource.volume - CommonHelper.GetBGMVolume()) > 0)
            {
                UpdateVolume(CommonHelper.GetBGMVolume());
            }
        }

        private void UpdateVolume(float volume)
        {
            _audioSource.volume = volume;
            if (volume == 0)
            {
                _audioSource.Stop();
            }
            else if (!_audioSource.isPlaying)
            {
                _audioSource.Play();
            }
        }
    }
}