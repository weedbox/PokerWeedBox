using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Code.Base;
using Code.Helper;
using Code.Model.Game;
using Code.Prefab.Lobby;
using NativeWebSocket;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Code.Scene
{
    public class LobbyScene : BaseScene
    {
        [SerializeField] private Image imageMask;
        [SerializeField] private Image imageAvatar;
        [SerializeField] private TMP_Text textUserInfo;
        [SerializeField] private Toggle toggleBGM;
        [SerializeField] private Toggle toggleSoundEffect;
        [SerializeField] private TMP_Text textPrintInfo;
        [SerializeField] private RectTransform rectListViewContent;
        [SerializeField] private ItemListCompetition itemListCompetition;
        [SerializeField] private Button btnRefresh;
        [SerializeField] private Button btnLogout;

        private GameObject _canvas;

        private readonly List<ListCompetition> _listCashCompetitions = new();
        private string _displayName = "";
        private string _playerId = "";
        private string _totalCPP = "";

        private const float PollingInterval = 10f;
        private TimerHelper _pollingActiveCompetitions;

        protected override void Start()
        {
            base.Start();
            
            _canvas = GameObject.Find("Canvas");
            
            imageMask.gameObject.SetActive(false);
            toggleBGM.isOn = CommonHelper.GetBGMVolume() > 0;
            toggleSoundEffect.isOn = CommonHelper.GetSoundEffectVolume() > 0;
            toggleBGM.onValueChanged.AddListener(enable =>
            {
                PlayerPrefs.SetFloat(Constant.PfKeyBGMVolume, enable ? 1 : 0);
                CommonHelper.UpdateVolume();
            });
            toggleSoundEffect.onValueChanged.AddListener(enable =>
            {
                PlayerPrefs.SetFloat(Constant.PfKeySoundEffectVolume, enable ? 1 : 0);
                CommonHelper.UpdateVolume();
            });
            btnRefresh.onClick.AddListener(() =>
            {
                textPrintInfo.text = "Refreshing...";
                CommonHelper.ShowLoading(_canvas);
                SendListGames();
            });
            btnLogout.onClick.AddListener(Logout);
            
            if (string.IsNullOrEmpty(PlayerPrefs.GetString(Constant.PfKeyUserToken)))
            {
                Logout();
            }
            else
            {
                if (ConnectionHelper.Instance.IsConnected())
                {
                    textPrintInfo.text = "Socket is connected";
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

            textPrintInfo.text = "SocketOnOpen";
            SendAuthenticate();
        }

        protected override void SocketOnError(string errorMsg)
        {
            base.SocketOnError(errorMsg);

            textPrintInfo.text = "SocketOnError:" + errorMsg;
        }

        protected override void SocketOnClose(WebSocketCloseCode closeCode)
        {
            base.SocketOnClose(closeCode);

            textPrintInfo.text = "SocketOnClose:" + closeCode;
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

                    textPrintInfo.text = "Authenticate Success";
                    SendReady();
                });
        }

        private void SendReady()
        {
            ConnectionHelper.Instance.SendReady(
                resp =>
                {
                    if (!CommonHelper.CheckResponseIsSuccess(_canvas, resp.Method, resp.Error, CommonHelper.HideLoading))
                        return;

                    textPrintInfo.text = "Ready Success";
                    SendGetCurrentPlayer();
                    SendListGames();
                });
        }

        private void SendGetCurrentPlayer()
        {
            textUserInfo.text = "Updating Info...";
            ConnectionHelper.Instance.SendGetCurrentPlayer(
                resp =>
                {
                    if (!CommonHelper.CheckResponseIsSuccess(_canvas, resp.Method, resp.Error, CommonHelper.HideLoading))
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
                        textPrintInfo.text = "Get Current Player Success";
                        CommonHelper.DownImageFromUrl(this, resp.Result.AvatarURL, sprite =>
                        {
                            imageAvatar.sprite = sprite;
                            imageAvatar.gameObject.SetActive(true);

                            var color = imageMask.color;
                            color.a = 1f;
                            imageMask.color = color;
                            imageMask.gameObject.SetActive(true);
                        }, errorMessage =>
                        {
                            textPrintInfo.text = errorMessage;
                            SetAvatarToDefault();
                        });
                        _displayName = resp.Result.DisplayName;
                        _playerId = resp.Result.ID;
                        textUserInfo.text = _displayName + " (" + _playerId + ")";
                        SendGetPlayerLatestData();
                    }
                });
        }

        private void SetAvatarToDefault()
        {
            // todo implement avatar placeholder ?
            imageAvatar.gameObject.SetActive(false);

            var color = imageMask.color;
            color.a = 0.2f;
            imageMask.color = color;
            imageMask.gameObject.SetActive(true);
        }

        private void SendGetPlayerLatestData()
        {
            ConnectionHelper.Instance.SendGetPlayerLatestData(
                resp =>
                {
                    if (!CommonHelper.CheckResponseIsSuccess(_canvas, resp.Method, resp.Error, CommonHelper.HideLoading))
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
                        PlayerPrefs.SetString(Constant.PfKeyUserID, resp.Result.ID);

                        _totalCPP = resp.Result.CPP;
                        textUserInfo.text = _displayName + " (" + _playerId + ")" +
                                            "\nCPP:" + _totalCPP + " | Tickets:" + resp.Result.Tickets;
                    }
                });
        }

        private void SendListGames()
        {
            ClearCashGameList();
            _listCashCompetitions.Clear();
            ConnectionHelper.Instance.SendListGames(
                resp =>
                {
                    if (!CommonHelper.CheckResponseIsSuccess(_canvas, resp.Method, resp.Error, CommonHelper.HideLoading))
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
                            textPrintInfo.text = "List Games no ct tag found.";
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
                    if (!CommonHelper.CheckResponseIsSuccess(_canvas, resp.Method, resp.Error, CommonHelper.HideLoading))
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
                    if (!CommonHelper.CheckResponseIsSuccess(_canvas, resp.Method, resp.Error, CommonHelper.HideLoading))
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
                            textPrintInfo.text = "no cash game found.";
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
                            textPrintInfo.text = "Latest competition found\nEnter game scene now...";
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
                                        // todo find buyInCpp
                                        const long buyInCpp = 1000;
                                        if (long.Parse(_totalCPP) <= buyInCpp)
                                        {
                                            CommonHelper.ShowCommonDialog(
                                                canvas: _canvas,
                                                title: "Balance not enough",
                                                message: "Cpp " + _totalCPP + " not enough, at least " + buyInCpp +
                                                         " is need",
                                                negative: "Close");
                                            return;
                                        }

                                        CommonHelper.ShowLoading(_canvas);
                                        ConnectionHelper.Instance.SendCompetitionCashBuyIn(
                                            competitionId,
                                            buyInCpp.ToString(),
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

        private static void Logout()
        {
            PlayerPrefs.SetString(Constant.PfKeyUserToken, "");
            SceneManager.LoadScene(nameof(LoginScene));
        }
    }
}