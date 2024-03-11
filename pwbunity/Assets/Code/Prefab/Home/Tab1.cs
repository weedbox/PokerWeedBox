using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Code.Base;
using Code.Helper;
using Code.Model.Game;
using Code.Prefab.Lobby;
using Code.Scene;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Code.Prefab.Home
{
    public class Tab1 : BasePrefabWithCommonSound
    {
        [SerializeField] private GameObject gameObjectType;
        [SerializeField] private Button btnGameTypeCash;

        [SerializeField] private GameObject gameObjectList;
        [SerializeField] private RectTransform rectListViewContent;
        [SerializeField] private ItemListCompetition itemListCompetition;

        private GameObject _canvas;

        private readonly List<ListCompetition> _listCashCompetitions = new();
        private string _totalChip = "";

        private const float PollingInterval = 10f;
        private TimerHelper _pollingActiveCompetitions;

        [CanBeNull] private UnityAction _onCashOutComplete;

        protected override void Start()
        {
            base.Start();

            _canvas = GameObject.Find("Canvas");

            ShowGameList(false);

            btnGameTypeCash.onClick.AddListener(() =>
            {
                ShowGameList(true);
                SendListGames();
            });
        }

        public void SetCashOutCompleteCallback(UnityAction value)
        {
            _onCashOutComplete = value;
        }

        public void ShowGameList(bool value)
        {
            gameObjectType.SetActive(!value);
            gameObjectList.SetActive(value);
        }

        public void UpdateChip(string value)
        {
            _totalChip = value;
        }

        private void SendListGames()
        {
            CommonHelper.ShowLoading(_canvas);
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
                                positiveCallback: SendListGames,
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

                        var nonAfkCompetition = resp.Result.ActiveCompetitions.Find(it =>
                            !it.IsAfk &&
                            string.Equals(it.PlayerStatus, Constant.PlayerStatus.Playing) &&
                            string.Equals(it.CompetitionMode, Constant.CompetitionMode.CompetitionModeCash)
                        );
                        if (nonAfkCompetition != null)
                        {
                            StartCoroutine(AutoEnterGame(nonAfkCompetition));
                        }
                        else
                        {
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
                                        const long buyInChip = 1000;
                                        if (long.Parse(_totalChip) <= buyInChip)
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
                                            buyInChip.ToString(),
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

                                                SendListGames();
                                                _onCashOutComplete?.Invoke();
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
                        }
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

        private static IEnumerator AutoEnterGame(ActiveCompetitions nonAfkCompetition)
        {
            yield return new WaitForSeconds(0.5f);
            PlayerPrefs.SetString(Constant.PfKeyCompetitionID, nonAfkCompetition.CompetitionID);
            PlayerPrefs.SetString(Constant.PfKeyCompetitionName, nonAfkCompetition.CompetitionName);
            PlayerPrefs.SetString(Constant.PfKeyTableID, nonAfkCompetition.TableID);
            PlayerPrefs.SetString(Constant.PfKeyTableName, nonAfkCompetition.TableName);
            SceneManager.LoadScene(nameof(GameScene));
        }
    }
}