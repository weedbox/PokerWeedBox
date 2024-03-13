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
        [SerializeField] private GameObject objectIsDealer;
        [SerializeField] private Image imageDidAction;
        [SerializeField] private GameObject objectWinner;

        [Header("Did Action")] 
        [SerializeField] private Sprite spriteFold;
        [SerializeField] private Sprite spriteCall;
        [SerializeField] private Sprite spriteCheck;
        [SerializeField] private Sprite spriteBet;
        [SerializeField] private Sprite spriteRaise;
        [SerializeField] private Sprite spriteAllIn;

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
            objectWinner.SetActive(false);
            objectIsDealer.SetActive(false);
            imageDidAction.gameObject.SetActive(false);

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
                CheckUpdateName(player.PlayerID);
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
            UpdatePositionInfo(player.Positions);
            CheckUpdateName(player.PlayerID);
            textPoint.text = player.Bankroll.ToString();
            SetIsParticipated(player.IsParticipated);
        }

        private void CheckUpdateName(string playerID)
        {
            if (string.Equals(playerID, _latestUserId) && !string.IsNullOrEmpty(_name)) return;

            _latestUserId = playerID;

            gameObjectAvatar.SetActive(false);
            imageAvatar.gameObject.SetActive(false);

            ConnectionHelper.Instance.SendGetPlayer(
                playerID,
                null,
                resp =>
                {
                    if (resp.Error != null)
                    {
                        CommonHelper.LogError(resp.Method + ",  Error. [Code]" + resp.Error.Code + ", [Message]" +
                                              resp.Error.Message);
                    }
                    else
                    {
                        // StartCoroutine(SetAvatar(resp.Result?.AvatarURL));
                        _name = resp.Result?.DisplayName;
                        SetAvatar(_name);
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

        private void SetAvatar(string displayName)
        {
            var targetIndex = new List<string>(Constant.LoginNames).FindIndex(it => string.Equals(it, displayName));
            Sprite targetSprite = null;
            if (targetIndex != -1)
            {
                targetSprite = Resources.Load<Sprite>("Art/Image/Common/Avatar/" + Constant.LogonAvatars[targetIndex]);
            }
            
            imageAvatar.sprite = targetSprite;

            gameObjectAvatar.SetActive(targetSprite);
            imageAvatar.gameObject.SetActive(targetSprite);
        }

        private void UpdatePositionInfo([CanBeNull] ICollection<string> positions)
        {
            objectIsDealer.SetActive(positions != null && positions.Contains("dealer"));
        }

        public void SetGameStatePlayerState([CanBeNull] PlayerState player, int actionTimeInSecond,
            long actionEndAtInMilliseconds, UnityAction readyActionsCallback,
            UnityAction<List<string>, long> allowedActionsCallback, [CanBeNull] GameSoundEffect gameSoundEffect)
        {
            _gameSoundEffect = gameSoundEffect;
            // UpdatePositionInfo(player?.Positions);
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

                    if (player.AllowedActions != null &&
                        player.AllowedActions.Contains(Constant.GameStatusPlayerActionReady))
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
            if (didAction == "Winner")
            {
                objectWinner.SetActive(true);
                objectIsDealer.SetActive(false);
                imageDidAction.gameObject.SetActive(false);
            }
            else
            {
                objectWinner.SetActive(false);

                if (_gameSoundEffect) _gameSoundEffect.PlayDidAction(didAction);

                var sprite = didAction switch
                {
                    Constant.GameStatusPlayerAction.Fold => spriteFold,
                    Constant.GameStatusPlayerAction.Call => spriteCall,
                    Constant.GameStatusPlayerAction.Check => spriteCheck,
                    Constant.GameStatusPlayerAction.Bet => spriteBet,
                    Constant.GameStatusPlayerAction.Raise => spriteRaise,
                    Constant.GameStatusPlayerAction.Allin => spriteAllIn,
                    _ => null
                };
                imageDidAction.sprite = sprite;
                imageDidAction.gameObject.SetActive(sprite);
            }
        }
    }
}