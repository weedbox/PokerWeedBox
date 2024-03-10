using UnityEngine;
using UnityEngine.UI;

namespace Code.Prefab.Common
{
    public class Card : MonoBehaviour
    {
        [SerializeField] private Image imageCard;
        [SerializeField] private Image imageNumber;
        [SerializeField] private Image imageTypeSmall;
        [SerializeField] private Image imageTypeLarge;
        
        [Header("Background")]
        [SerializeField] private Sprite imageCardBack;
        [SerializeField] private Sprite imageCardFront;

        [Header("Number")] 
        [SerializeField] private Sprite[] imageNumbersBlack = new Sprite[13];
        [SerializeField] private Sprite[] imageNumbersRed = new Sprite[13];
        
        [Header("Type")]
        [SerializeField] private Sprite imageTypeSpade;
        [SerializeField] private Sprite imageTypeHeart;
        [SerializeField] private Sprite imageTypeDiamond;
        [SerializeField] private Sprite imageTypeClub;

        private void Start()
        {
            Hide();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetBlank()
        {
            imageCard.sprite = imageCardBack;
            imageNumber.gameObject.SetActive(false);
            imageTypeSmall.gameObject.SetActive(false);
            imageTypeLarge.gameObject.SetActive(false);
            gameObject.SetActive(true); 
        }

        public void SetCard(string card)
        {
            if (card.Length == 2)
            {
                imageCard.sprite = imageCardFront;
                gameObject.SetActive(true);

                var colorRed = Equals(card[0], 'H') || Equals(card[0], 'D');
                var type = card[0] switch
                {
                    'S' => imageTypeSpade,
                    'H' => imageTypeHeart,
                    'D' => imageTypeDiamond,
                    'C' => imageTypeClub,
                    _ => null
                };

                var number = card[1] switch
                {
                    'A' => colorRed ? imageNumbersRed[0] : imageNumbersBlack[0],
                    '2' => colorRed ? imageNumbersRed[1] : imageNumbersBlack[1],
                    '3' => colorRed ? imageNumbersRed[2] : imageNumbersBlack[2],
                    '4' => colorRed ? imageNumbersRed[3] : imageNumbersBlack[3],
                    '5' => colorRed ? imageNumbersRed[4] : imageNumbersBlack[4],
                    '6' => colorRed ? imageNumbersRed[5] : imageNumbersBlack[5],
                    '7' => colorRed ? imageNumbersRed[6] : imageNumbersBlack[6],
                    '8' => colorRed ? imageNumbersRed[7] : imageNumbersBlack[7],
                    '9' => colorRed ? imageNumbersRed[8] : imageNumbersBlack[8],
                    'T' => colorRed ? imageNumbersRed[9] : imageNumbersBlack[9],
                    'J' => colorRed ? imageNumbersRed[10] : imageNumbersBlack[10],
                    'Q' => colorRed ? imageNumbersRed[11] : imageNumbersBlack[11],
                    'K' => colorRed ? imageNumbersRed[12] : imageNumbersBlack[12],
                    _ => null
                };
                
                imageNumber.gameObject.SetActive(number);
                imageNumber.sprite = number;
                
                imageTypeSmall.gameObject.SetActive(type);
                imageTypeSmall.sprite = type;
                
                imageTypeLarge.gameObject.SetActive(type);
                imageTypeLarge.sprite = type;
            }
            else
            {
                SetBlank();
            }
        }
    }
}