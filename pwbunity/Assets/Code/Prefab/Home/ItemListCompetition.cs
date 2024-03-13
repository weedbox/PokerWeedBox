using Code.Base;
using Code.Helper;
using Code.Model.Game;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Code.Prefab.Home
{
    public class ItemListCompetition : BasePrefabWithCommonSound
    {
        [SerializeField] private TMP_Text textName;
        [SerializeField] private TMP_Text textStatus;

        [SerializeField] private Button buttonCashBuyIn;
        [SerializeField] private Button buttonCashOut;
        [SerializeField] private Button buttonEnterGame;

        public void SetData(ListCompetition listCompetition, [CanBeNull] ActiveCompetitions activeCompetition,
            UnityAction<string, string> cashBuyInCallback, UnityAction<string, string> cashOutCallback,
            UnityAction<string, string, string> enterGameCallback)
        {
            buttonCashBuyIn.onClick.AddListener(() =>
            {
                cashBuyInCallback?.Invoke(listCompetition.CompetitionID, listCompetition.Name);
            });
            buttonCashOut.onClick.AddListener(() =>
            {
                if (activeCompetition != null)
                {
                    cashOutCallback?.Invoke(activeCompetition.CompetitionID, activeCompetition.TableID);
                }
            });
            buttonEnterGame.onClick.AddListener(() =>
            {
                if (activeCompetition != null)
                {
                    enterGameCallback?.Invoke(listCompetition.CompetitionID, listCompetition.Name,
                        activeCompetition.TableID);
                }
            });
            textName.text = listCompetition.Name;

            if (activeCompetition == null)
            {
                textStatus.text = "Available";
                buttonCashBuyIn.gameObject.SetActive(true);
                buttonCashOut.gameObject.SetActive(false);
                buttonEnterGame.gameObject.SetActive(false);
            }
            else
            {
                var unknownPlayerStatus = "";
                switch (activeCompetition.PlayerStatus)
                {
                    // 等待拆併桌中
                    case Constant.PlayerStatus.WaitingTableBalancing:
                        buttonCashBuyIn.gameObject.SetActive(false);
                        buttonCashOut.gameObject.SetActive(true);
                        buttonEnterGame.gameObject.SetActive(true);
                        break;

                    // 比賽中
                    case Constant.PlayerStatus.Playing:
                        buttonCashBuyIn.gameObject.SetActive(false);
                        buttonCashOut.gameObject.SetActive(true);
                        buttonEnterGame.gameObject.SetActive(true);
                        break;

                    // 等待補碼中 (已不再桌次內)
                    case Constant.PlayerStatus.ReBuyWaiting:
                        buttonCashBuyIn.gameObject.SetActive(true);
                        buttonCashOut.gameObject.SetActive(true);
                        buttonEnterGame.gameObject.SetActive(false);
                        break;

                    // 已淘汰
                    case Constant.PlayerStatus.Knockout:
                        buttonCashBuyIn.gameObject.SetActive(false);
                        buttonCashOut.gameObject.SetActive(false);
                        buttonEnterGame.gameObject.SetActive(false);
                        CommonHelper.LogError("PlayerStatus found knockout");
                        break;

                    // 現金桌離開中 (結算時就會離開)
                    case Constant.PlayerStatus.CashLeaving:
                        buttonCashBuyIn.gameObject.SetActive(false);
                        buttonCashOut.gameObject.SetActive(false);
                        buttonEnterGame.gameObject.SetActive(false);
                        break;

                    default:
                        buttonCashBuyIn.gameObject.SetActive(false);
                        buttonCashOut.gameObject.SetActive(false);
                        buttonEnterGame.gameObject.SetActive(false);

                        unknownPlayerStatus = "unknown player status:" +
                                              (string.IsNullOrEmpty(activeCompetition.PlayerStatus)
                                                  ? "(blank status)"
                                                  : activeCompetition.PlayerStatus);
                        CommonHelper.LogError(unknownPlayerStatus);
                        break;
                }

                if (string.IsNullOrEmpty(unknownPlayerStatus))
                {
                    textStatus.text = activeCompetition.PlayerStatus + " | " + activeCompetition.CompetitionStatus;
                    textStatus.color = Color.white;
                }
                else
                {
                    textStatus.text = unknownPlayerStatus + " | " + activeCompetition.CompetitionStatus;
                    textStatus.color = Color.red;
                }
            }
        }
    }
}