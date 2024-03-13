using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Code.Base;
using Code.Helper;
using Code.Model.Game;
using Code.Prefab.Home;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Code.Scene
{
    public class HomeScene : BaseScene
    {
        [SerializeField] private Button buttonMenu;
        [SerializeField] private Image imageAvatar;
        [SerializeField] private TMP_Text textName;
        [SerializeField] private TMP_Text textID;
        [SerializeField] private TMP_Text textChip;
        [SerializeField] private RectTransform rectListViewContent;
        [SerializeField] private ItemListCompetition itemListCompetition;

        private GameObject _canvas;

        private readonly List<ListCompetition> _listCashCompetitions = new();
        private string _totalChip = "";

        private const float PollingInterval = 10f;
        private TimerHelper _pollingActiveCompetitions;

        protected override void Start()
        {
            base.Start();

            _canvas = GameObject.Find("Canvas");

            buttonMenu.onClick.AddListener(Logout);
            // buttonTopRight.onClick.AddListener(() =>
            //     Instantiate(Resources.Load<HomeSubMenu>("Prefabs/Home/HomeSubMenu"), _canvas.transform, false));

            if (string.IsNullOrEmpty(PlayerPrefs.GetString(Constant.PfKeyUserToken)))
            {
                Logout();
            }
            else
            {
                if (ConnectionHelper.Instance.IsConnected())
                {
                    SendAuthenticate();
                }
                else
                {
                    ConnectionHelper.Instance.ConnectWebSocket();
                }
            }
        }

        protected override void SocketOnOpen()
        {
            base.SocketOnOpen();

            SendAuthenticate();
        }

        private void SendAuthenticate()
        {
            CommonHelper.ShowLoading(_canvas);
            ConnectionHelper.Instance.SendAuthenticate(
                PlayerPrefs.GetString(Constant.PfKeyUserToken),
                resp =>
                {
                    if (!CommonHelper.CheckResponseIsSuccess(_canvas, resp.Method, resp.Error, () =>
                        {
                            CommonHelper.HideLoading();
                            Logout();
                        }))
                        return;

                    SendReady();
                });
        }

        private void SendReady()
        {
            ConnectionHelper.Instance.SendReady(
                resp =>
                {
                    if (!CommonHelper.CheckResponseIsSuccess(_canvas, resp.Method, resp.Error,
                            CommonHelper.HideLoading))
                        return;

                    SendGetPlayerLatestData();
                    SendListGames(false);
                });
        }

        private void SendGetPlayerLatestData()
        {
            ConnectionHelper.Instance.SendGetPlayerLatestData(
                resp =>
                {
                    if (!CommonHelper.CheckResponseIsSuccess(_canvas, resp.Method, resp.Error,
                            CommonHelper.HideLoading))
                        return;
                    
                    if (resp.Result == null)
                    {
                        CommonHelper.ShowCommonDialog(
                            canvas: _canvas,
                            title: "Error Occur",
                            message: resp.Method + "\n" + "Result is null",
                            positive: "Close");
                    }
                    else
                    {
                        PlayerPrefs.SetString(Constant.PfKeyUserID, resp.Result.ID);

                        _totalChip = resp.Result.Chip;

                        var displayName = resp.Result.DisplayName;
                        
                        var targetIndex = new List<string>(Constant.LoginNames).FindIndex(it => string.Equals(it, displayName));
                        if (targetIndex != -1)
                        {
                            imageAvatar.sprite = Resources.Load<Sprite>("Art/Image/Common/Avatar/" + Constant.LogonAvatars[targetIndex]);
                        }

                        textName.text = displayName;
                        textID.text = "ID:" + resp.Result.ID;
                        textChip.text = "Chip:" + _totalChip;
                    }
                });
        }

        private void SendListGames(bool withLoading)
        {
            if (withLoading)
            {
                CommonHelper.ShowLoading(_canvas);
            }
            
            ClearCashGameList();
            _listCashCompetitions.Clear();
            ConnectionHelper.Instance.SendListGames(
                resp =>
                {
                    if (!CommonHelper.CheckResponseIsSuccess(_canvas, resp.Method, resp.Error,
                            CommonHelper.HideLoading))
                        return;

                    if (resp.Result == null)
                    {
                        CommonHelper.HideLoading();
                        CommonHelper.ShowCommonDialog(
                            canvas: _canvas,
                            title: "Error Occur",
                            message: resp.Method + "\n" + "Result is null",
                            positive: "Close");
                    }
                    else
                    {
                        var ctCategoryID = "";
                        foreach (var game in resp.Result.Games.Where(game => string.Equals(game.Name, "HOLD'EM")))
                        {
                            foreach (var category in game.Categories.Where(
                                         category => string.Equals(category.Tag, "ct")))
                            {
                                ctCategoryID = category.ID;
                                break;
                            }
                        }

                        if (string.IsNullOrEmpty(ctCategoryID))
                        {
                            CommonHelper.HideLoading();
                            CommonHelper.ShowCommonDialog(
                                canvas: _canvas,
                                title: "Oops",
                                message: "List Games no ct tag found.",
                                positive: "Close");
                        }
                        else
                        {
                            SendListGameLevels(ctCategoryID);
                        }
                    }
                });
        }

        private void SendListGameLevels(string gameCategoryId)
        {
            ConnectionHelper.Instance.SendListGameLevels(
                1,
                100,
                gameCategoryId,
                resp =>
                {
                    if (!CommonHelper.CheckResponseIsSuccess(_canvas, resp.Method, resp.Error,
                            CommonHelper.HideLoading))
                        return;

                    if (resp.Result == null)
                    {
                        CommonHelper.HideLoading();
                        CommonHelper.ShowCommonDialog(
                            canvas: _canvas,
                            title: "Error Occur",
                            message: resp.Method + "\n" + "Result is null",
                            positive: "Close");
                    }
                    else
                    {
                        var targetGameLevelID = "";
                        foreach (var item in resp.Result.Data.Where(item => string.Equals(item.Name, "11000 夢幻館")))
                        {
                            targetGameLevelID = item.ID;
                            break;
                        }

                        if (string.IsNullOrEmpty(targetGameLevelID))
                        {
                            CommonHelper.HideLoading();
                        }
                        else
                        {
                            SendListCompetitions(gameCategoryId, targetGameLevelID);
                        }
                    }
                });
        }

        private void SendListCompetitions(string gameCategoryId, string gameLevelId)
        {
            var reqListCompetitions = new ReqListCompetitions(1, 100, gameCategoryId, gameLevelId, "0", new List<string>
            {
                "registering",
                "delayed_buy_in",
                "stopped_buy_in"
            });
            ConnectionHelper.Instance.SendListCompetitions(
                JsonConvert.SerializeObject(reqListCompetitions),
                resp =>
                {
                    if (!CommonHelper.CheckResponseIsSuccess(_canvas, resp.Method, resp.Error,
                            CommonHelper.HideLoading))
                        return;

                    if (resp.Result == null)
                    {
                        CommonHelper.HideLoading();
                        CommonHelper.ShowCommonDialog(
                            canvas: _canvas,
                            title: "Error Occur",
                            message: resp.Method + "\n" + "Result is null",
                            positive: "Close");
                    }
                    else
                    {
                        _listCashCompetitions.AddRange(resp.Result.Data.Where(item =>
                            string.Equals(item.Mode, Constant.CompetitionMode.CompetitionModeCash)));
                        if (_listCashCompetitions.Count == 0)
                        {
                            CommonHelper.HideLoading();
                            CommonHelper.ShowCommonDialog(
                                canvas: _canvas,
                                title: "Oops",
                                message: "no cash game found.",
                                positive: "Refresh",
                                positiveCallback: () => SendListGames(true),
                                negative: "Close");
                        }
                        else
                        {
                            SendListPlayerActiveCompetitions();
                        }
                    }
                });
        }

        private void SendListPlayerActiveCompetitions()
        {
            ConnectionHelper.Instance.SendListPlayerActiveCompetitions(
                resp =>
                {
                    ClearCashGameList();

                    if (!CommonHelper.CheckResponseIsSuccess(_canvas, resp.Method, resp.Error,
                            CommonHelper.HideLoading))
                        return;

                    if (resp.Result == null)
                    {
                        CommonHelper.HideLoading();
                        CommonHelper.ShowCommonDialog(
                            canvas: _canvas,
                            title: "Error Occur",
                            message: resp.Method + "\n" + "Result is null",
                            positive: "Close");
                    }
                    else
                    {
                        CommonHelper.HideLoading();

                        // var nonAfkCompetition = resp.Result.ActiveCompetitions.Find(it =>
                        //     !it.IsAfk &&
                        //     string.Equals(it.PlayerStatus, Constant.PlayerStatus.Playing) &&
                        //     string.Equals(it.CompetitionMode, Constant.CompetitionMode.CompetitionModeCash)
                        // );
                        // if (nonAfkCompetition != null)
                        // {
                        //     StartCoroutine(AutoEnterGame(nonAfkCompetition));
                        // }
                        // else
                        // {
                        var hasActiveCompetitions = false;
                        foreach (var item in _listCashCompetitions)
                        {
                            var activeCompetition = resp.Result.ActiveCompetitions.Find(competition =>
                                string.Equals(competition.CompetitionID, item.CompetitionID));
                            Instantiate(itemListCompetition, rectListViewContent.transform, false).SetData(
                                item,
                                activeCompetition,
                                (competitionId, competitionName) =>
                                {
                                    // todo find buyInChip
                                    const double buyInChip = 1000;
                                    if (double.Parse(_totalChip) <= buyInChip)
                                    {
                                        CommonHelper.ShowCommonDialog(
                                            canvas: _canvas,
                                            title: "Balance not enough",
                                            message: "Chip " + _totalChip + " not enough, at least " + buyInChip +
                                                     " is need",
                                            negative: "Close");
                                        return;
                                    }

                                    CommonHelper.ShowLoading(_canvas);
                                    ConnectionHelper.Instance.SendCompetitionCashBuyIn(
                                        competitionId,
                                        buyInChip.ToString(CultureInfo.CurrentCulture),
                                        cashBuyInResp =>
                                        {
                                            if (!CommonHelper.CheckResponseIsSuccess(_canvas, cashBuyInResp.Method,
                                                    cashBuyInResp.Error,
                                                    CommonHelper.HideLoading))
                                                return;

                                            PlayerPrefs.SetString(Constant.PfKeyCompetitionID, competitionId);
                                            PlayerPrefs.SetString(Constant.PfKeyCompetitionName, competitionName);
                                            SceneManager.LoadScene(nameof(GameScene));
                                        });
                                },
                                (competitionId, tableId) =>
                                {
                                    CommonHelper.ShowLoading(_canvas);
                                    ConnectionHelper.Instance.SendCompetitionCashOut(
                                        competitionId,
                                        tableId,
                                        cashOutResp =>
                                        {
                                            if (!CommonHelper.CheckResponseIsSuccess(_canvas, cashOutResp.Method,
                                                    cashOutResp.Error,
                                                    CommonHelper.HideLoading))
                                                return;

                                            SendListGames(false);
                                            SendGetPlayerLatestData();
                                        });
                                },
                                (competitionId, competitionName, tableId) =>
                                {
                                    PlayerPrefs.SetString(Constant.PfKeyCompetitionID, competitionId);
                                    PlayerPrefs.SetString(Constant.PfKeyCompetitionName, competitionName);
                                    PlayerPrefs.SetString(Constant.PfKeyTableID, tableId);
                                    SceneManager.LoadScene(nameof(GameScene));
                                });

                            if (!hasActiveCompetitions)
                            {
                                hasActiveCompetitions = activeCompetition != null;
                            }
                        }

                        _pollingActiveCompetitions?.StopTimer();

                        if (!hasActiveCompetitions) return;

                        _pollingActiveCompetitions ??= new TimerHelper(this);
                        _pollingActiveCompetitions.StartTimer(PollingInterval, SendListPlayerActiveCompetitions);
                        // }
                    }
                });
        }

        private void ClearCashGameList()
        {
            foreach (Transform child in rectListViewContent.transform)
            {
                Destroy(child.gameObject);
            }
        }

        // private static IEnumerator AutoEnterGame(ActiveCompetitions nonAfkCompetition)
        // {
        //     yield return new WaitForSeconds(0.5f);
        //     PlayerPrefs.SetString(Constant.PfKeyCompetitionID, nonAfkCompetition.CompetitionID);
        //     PlayerPrefs.SetString(Constant.PfKeyCompetitionName, nonAfkCompetition.CompetitionName);
        //     PlayerPrefs.SetString(Constant.PfKeyTableID, nonAfkCompetition.TableID);
        //     PlayerPrefs.SetString(Constant.PfKeyTableName, nonAfkCompetition.TableName);
        //     SceneManager.LoadScene(nameof(GameScene));
        // }

        private static void Logout()
        {
            PlayerPrefs.SetString(Constant.PfKeyUserToken, "");
            SceneManager.LoadScene(nameof(LoginScene));
        }
    }
}