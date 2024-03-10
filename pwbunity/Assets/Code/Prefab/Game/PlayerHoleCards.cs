using System.Collections.Generic;
using Code.Prefab.Common;
using JetBrains.Annotations;
using UnityEngine;

namespace Code.Prefab.Game
{
    public class PlayerHoleCards : MonoBehaviour
    {
        [SerializeField] private Card card1;
        [SerializeField] private Card card2;

        public void Hide()
        {
            card1.Hide();
            card2.Hide();
        }

        public void SetCards([CanBeNull] List<string> playerHoleCards)
        {
            if (playerHoleCards == null)
            {
                card1.SetBlank();
                card2.SetBlank();
            }
            else
            {
                switch (playerHoleCards.Count)
                {
                    case 1:
                        card1.SetCard(playerHoleCards[0]);
                        card2.SetBlank();
                        break;

                    case > 1:
                        card1.SetCard(playerHoleCards[0]);
                        card2.SetCard(playerHoleCards[1]);
                        break;
                }
            }
        }
    }
}