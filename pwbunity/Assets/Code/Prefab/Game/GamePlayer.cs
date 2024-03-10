using System.Collections.Generic;
using Code.Model.Game.NotificationEvent;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;

namespace Code.Prefab.Game
{
    public class GamePlayer : MonoBehaviour
    {
        [SerializeField] private GamePlayerInfo gamePlayerInfo;
        [SerializeField] private PlayerHoleCards playerHoleCards;

        public void SetCompetitionPlayer([CanBeNull] CompetitionPlayer player, UnityAction knockoutCallback)
        {
            if (player == null)
            {
                gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(true);
                gamePlayerInfo.SetCompetitionPlayer(player, knockoutCallback);
            }
        }

        public void SetTablePlayer([CanBeNull] TablePlayerState player)
        {
            if (player == null)
            {
                gameObject.SetActive(false);
            }
            else
            {
                gamePlayerInfo.SetTablePlayer(player);
            }
        }

        public void SetGameStatePlayerState(string gameStateRound, [CanBeNull] PlayerState player,
            int actionTimeInSecond, long actionEndAtInMilliseconds, [CanBeNull] Result gameStateResult,
            UnityAction readyActionsCallback, UnityAction<List<string>, long> allowedActionsCallback,
            [CanBeNull] GameSoundEffect gameSoundEffect)
        {
            gamePlayerInfo.SetGameStatePlayerState(player, actionTimeInSecond, actionEndAtInMilliseconds,
                readyActionsCallback, allowedActionsCallback, gameSoundEffect);

            if (player == null)
            {
                playerHoleCards.Hide();
            }
            else
            {
                if (string.IsNullOrEmpty(gameStateRound))
                {
                    playerHoleCards.Hide();
                }
                else
                {
                    playerHoleCards.SetCards(player.HoleCards);
                    
                    // check result for winner
                    var resultPlayer = gameStateResult?.Players.Find(it => Equals(it.Idx, player.Idx));
                    if (resultPlayer is { Changed: > 0 })
                    {
                        gamePlayerInfo.SetWinner();
                    }
                }    
            }
        }
    }
}