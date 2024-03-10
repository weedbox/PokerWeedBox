using Code.Base;
using Code.Helper;
using UnityEngine;

namespace Code.Prefab.Game
{
    public class GameSoundEffect : BaseSoundEffect
    {
        [SerializeField] private AudioSource audioSourceActionFold;
        [SerializeField] private AudioSource audioSourceActionCheck;
        [SerializeField] private AudioSource audioSourceActionCall;
        [SerializeField] private AudioSource audioSourceActionBet;
        [SerializeField] private AudioSource audioSourceActionRaise;
        [SerializeField] private AudioSource audioSourceActionAllIn;
        [SerializeField] private AudioSource audioSourceCountdown;
        [SerializeField] private AudioSource audioSourceCheering;

        public void StopAll()
        {
            audioSourceActionFold.Stop();
            audioSourceActionCheck.Stop();
            audioSourceActionCall.Stop();
            audioSourceActionBet.Stop();
            audioSourceActionRaise.Stop();
            audioSourceActionAllIn.Stop();
            audioSourceCountdown.Stop();
            audioSourceCheering.Stop();
        }

        public bool IsCountdownPlaying()
        {
            return audioSourceCountdown.isPlaying;
        }

        public void PlayCountdown()
        {
            PlaySound(audioSourceCountdown);
        }

        public void StopCountdown()
        {
            audioSourceCountdown.Stop();
        }

        public void PlayCheering()
        {
            PlaySound(audioSourceCheering);
        }

        public void PlayDidAction(string didAction)
        {
            switch (didAction)
            {
                case Constant.GameStatusPlayerAction.Fold:
                    PlaySound(audioSourceActionFold);
                    break;

                case Constant.GameStatusPlayerAction.Call:
                    PlaySound(audioSourceActionCall);
                    break;

                case Constant.GameStatusPlayerAction.Check:
                    PlaySound(audioSourceActionCheck);
                    break;

                case Constant.GameStatusPlayerAction.Bet:
                    PlaySound(audioSourceActionBet);
                    break;

                case Constant.GameStatusPlayerAction.Raise:
                    PlaySound(audioSourceActionRaise);
                    break;

                case Constant.GameStatusPlayerAction.Allin:
                    PlaySound(audioSourceActionAllIn);
                    break;
            }
        }
    }
}