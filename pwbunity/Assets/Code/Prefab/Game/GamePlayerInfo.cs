using System;
using System.Collections.Generic;
using Code.Helper;
using Code.Model.Game.NotificationEvent;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Code.Prefab.Game
{
    public class GamePlayerInfo : MonoBehaviour
    {
        [SerializeField] private GameObject gameObjectAvatar;
        [SerializeField] private Image imageAvatarBackground;
        [SerializeField] private Image imageAvatar;
        [SerializeField] private Image imageCountDown;
        [SerializeField] private GameObject gameObjectNameAndPoint;
        [SerializeField] private TMP_Text textName;
        [SerializeField] private TMP_Text textPoint;
        [SerializeField] private GameObject gameObjectDidActionBg;
        [SerializeField] private Image imageDidActionBg;
        [SerializeField] private TMP_Text textDidAction;

        [Header("Did Action Background")] 
        [SerializeField] private Sprite spriteFold;
        [SerializeField] private Sprite spriteAllIn;

        [Header("Avatar")] 
        [SerializeField] private Sprite spriteAvatar1;
        [SerializeField] private Sprite spriteAvatar2;
        [SerializeField] private Sprite spriteAvatar3;
        [SerializeField] private Sprite spriteAvatar4;
        [SerializeField] private Sprite spriteAvatar5;
        [SerializeField] private Sprite spriteAvatar6;
        [SerializeField] private Sprite spriteAvatar7;
        [SerializeField] private Sprite spriteAvatar8;
        [SerializeField] private Sprite spriteAvatar9;

        [CanBeNull] private GameSoundEffect _gameSoundEffect;
        private string _myId = "";
        private string _latestUserId = "";
        private string _name = "";
        private TimerHelper _countDownTimer;

        private void Start()
        {
            gameObjectAvatar.SetActive(false);
            imageAvatar.gameObject.SetActive(false);
            imageCountDown.gameObject.SetActive(false);
            imageAvatar.sprite = null;
            gameObjectNameAndPoint.gameObject.SetActive(false);
            gameObjectDidActionBg.gameObject.SetActive(false);

            _myId = "";
            _latestUserId = "";
            _countDownTimer = null;
        }

        public void SetCompetitionPlayer([NotNull] CompetitionPlayer player, UnityAction knockoutCallback)
        {
            CheckUpdateMyID();
            if (string.Equals(player.PlayerID, _myId) && IsKnockout(player))
            {
                knockoutCallback.Invoke();
            }
            else
            {
                CheckUpdateName(player.PlayerID, player.Seat);
                textPoint.text = player.Chips.ToString();
            }
        }

        private void CheckUpdateMyID()
        {
            if (string.IsNullOrEmpty(_myId))
            {
                _myId = PlayerPrefs.GetString(Constant.PfKeyUserID);
            }
        }

        private static bool IsKnockout([NotNull] CompetitionPlayer player)
        {
            switch (player.Status)
            {
                case Constant.PlayerStatus.ReBuyWaiting:
                case Constant.PlayerStatus.Knockout:
                case Constant.PlayerStatus.CashLeaving:
                    return true;

                default:
                    return false;
            }
        }

        public void SetTablePlayer(TablePlayerState player)
        {
            CheckUpdateName(player.PlayerID, player.Seat);
            textPoint.text = player.Bankroll.ToString();
            SetIsParticipated(player.IsParticipated);
        }

        private void CheckUpdateName(string playerID, int seat)
        {
            if (string.Equals(playerID, _latestUserId) && !string.IsNullOrEmpty(_name)) return;

            _latestUserId = playerID;

            gameObjectAvatar.SetActive(false);
            imageAvatar.gameObject.SetActive(false);
            SetAvatar(seat);

            ConnectionHelper.Instance.SendGetPlayer(
                playerID, 
                null,
                resp =>
                {
                    if (resp.Error != null)
                    {
                        CommonHelper.LogError(resp.Method + ",  Error. [Code]" + resp.Error.Code + ", [Message]" + resp.Error.Message);
                    }
                    else
                    {
                        // StartCoroutine(SetAvatar(resp.Result?.AvatarURL));
                        _name = resp.Result?.DisplayName;
                        textName.text = CommonHelper.SubString(_name, 7);
                        gameObjectNameAndPoint.gameObject.SetActive(true);
                    }
                });

            // player has changed, reset did action and timer
            UpdateDidAction(null);
            _countDownTimer?.StopTimer();
            if (_gameSoundEffect) _gameSoundEffect.StopCountdown();
            imageCountDown.gameObject.SetActive(false);
        }

        private void SetIsParticipated(bool isParticipated)
        {
            var color = imageAvatarBackground.color;
            color.a = isParticipated ? 1f : 0.4f;
            imageAvatarBackground.color = color;

            color = imageAvatar.color;
            color.a = isParticipated ? 1f : 0.4f;
            imageAvatar.color = color;
        }

        private void SetAvatar(int seatIndex)
        {
            var targetSprite = seatIndex switch
            {
                0 => spriteAvatar1,
                1 => spriteAvatar2,
                2 => spriteAvatar3,
                3 => spriteAvatar4,
                4 => spriteAvatar5,
                5 => spriteAvatar6,
                6 => spriteAvatar7,
                7 => spriteAvatar8,
                8 => spriteAvatar9,
                _ => null
            };

            imageAvatar.sprite = targetSprite;

            gameObjectAvatar.SetActive(targetSprite);
            imageAvatar.gameObject.SetActive(targetSprite);
        }

        public void SetGameStatePlayerState([CanBeNull] PlayerState player, int actionTimeInSecond,
            long actionEndAtInMilliseconds, UnityAction readyActionsCallback,
            UnityAction<List<string>, long> allowedActionsCallback, [CanBeNull] GameSoundEffect gameSoundEffect)
        {
            _gameSoundEffect = gameSoundEffect;
            UpdateDidAction(player?.DidAction);

            _countDownTimer?.StopTimer();
            if (_gameSoundEffect) _gameSoundEffect.StopCountdown();
            imageCountDown.gameObject.SetActive(false);

            CheckUpdateMyID();

            if (player == null)
            {
                _countDownTimer?.StopTimer();
                if (_gameSoundEffect) _gameSoundEffect.StopCountdown();
                imageCountDown.gameObject.SetActive(false);
            }
            else
            {
                if (string.Equals(_latestUserId, _myId))
                {
                    allowedActionsCallback.Invoke(player.AllowedActions, player.Bankroll);

                    if (player.AllowedActions != null && player.AllowedActions.Contains(Constant.GameStatusPlayerActionReady))
                    {
                        readyActionsCallback.Invoke();
                    }
                }

                if (!CommonHelper.IsAvailableActionType(player.AllowedActions)) return;

                var remainTimeInSecond =
                    (actionEndAtInMilliseconds - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) / 1000f;
                if (remainTimeInSecond > 0)
                {
                    if (remainTimeInSecond > actionTimeInSecond)
                    {
                        remainTimeInSecond = actionTimeInSecond;
                    }

                    SetCountDown(remainTimeInSecond, actionTimeInSecond);
                    _countDownTimer ??= new TimerHelper(this);
                    _countDownTimer.StopTimer();
                    _countDownTimer.StartCountDown(remainTimeInSecond, 0.1f,
                        untilFinished => { SetCountDown(untilFinished, actionTimeInSecond); },
                        () => { imageCountDown.gameObject.SetActive(false); });
                }
                else
                {
                    // already finish
                    imageCountDown.gameObject.SetActive(false);
                }
            }
        }

        public void SetWinner()
        {
            // todo move winner indicate, and process when to remove it
            UpdateDidAction("Winner");
        }

        private void SetCountDown(float remainTimeInSecond, int actionTimeInSecond)
        {
            if (remainTimeInSecond <= 5 && _gameSoundEffect && !_gameSoundEffect.IsCountdownPlaying())
            {
                _gameSoundEffect.PlayCountdown();
            }
            
            imageCountDown.fillAmount = remainTimeInSecond / actionTimeInSecond;
            imageCountDown.gameObject.SetActive(true);
        }

        private void UpdateDidAction([CanBeNull] string didAction)
        {
            if (string.IsNullOrEmpty(didAction))
            {
                gameObjectDidActionBg.gameObject.SetActive(false);
            }
            else
            {
                if (_gameSoundEffect) _gameSoundEffect.PlayDidAction(didAction);
                
                var sprite = didAction switch
                {
                    Constant.GameStatusPlayerAction.Fold => spriteFold,
                    Constant.GameStatusPlayerAction.Allin => spriteAllIn,
                    _ => null
                };

                imageDidActionBg.sprite = sprite;

                // todo remove it when all action has background
                imageDidActionBg.color = sprite ? Color.white : Color.black;

                textDidAction.text = didAction;

                gameObjectDidActionBg.gameObject.SetActive(true);
            }
        }
    }
}