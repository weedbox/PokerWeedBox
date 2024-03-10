using System.Collections.Generic;
using System.Linq;
using Code.Base;
using Code.Helper;
using Code.Model;
using Code.Model.Game.NotificationEvent;
using Code.Prefab.Common;
using Code.Prefab.Game;
using JetBrains.Annotations;
using NativeWebSocket;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Code.Scene
{
    public class GameScene : BaseScene
    {
        [SerializeField] private GameSoundEffect gameSoundEffect;

        [SerializeField] private TMP_Text textJitterInfo;
        [SerializeField] private TMP_Text textTableSetting;
        [SerializeField] private Button buttonTopLeft;
        [SerializeField] private Button buttonTopRight;

        [Header("Player")] [SerializeField] private GamePlayer gamePlayerLeft1;
        [SerializeField] private GamePlayer gamePlayerLeft2;
        [SerializeField] private GamePlayer gamePlayerLeft3;
        [SerializeField] private GamePlayer gamePlayerLeft4;
        [SerializeField] private GamePlayer gameMainPlayer;
        [SerializeField] private GamePlayer gamePlayerRight1;
        [SerializeField] private GamePlayer gamePlayerRight2;
        [SerializeField] private GamePlayer gamePlayerRight3;
        [SerializeField] private GamePlayer gamePlayerRight4;

        [Header("Actions")] [SerializeField] private GameObject playerActions;
        [SerializeField] private GameObject commonActions;
        [SerializeField] private Button buttonFold;
        [SerializeField] private Button buttonCall;
        [SerializeField] private Button buttonCheck;
        [SerializeField] private Button buttonBet;
        [SerializeField] private Button buttonRaise;

        [SerializeField] private GameObject betActions;
        [SerializeField] private Button buttonBetClose;
        [SerializeField] private Button buttonBetOneHalf;
        [SerializeField] private TMP_Text textBetOneHalf;
        [SerializeField] private Button buttonBetTwoThirds;
        [SerializeField] private TMP_Text textBetTwoThirds;
        [SerializeField] private Button buttonBetOneTime;
        [SerializeField] private TMP_Text textBetOneTime;

        [SerializeField] private GameObject raiseActions;
        [SerializeField] private Button buttonRaiseClose;
        [SerializeField] private Button buttonRaiseTwice;
        [SerializeField] private TMP_Text textRaiseTwice;
        [SerializeField] private Button buttonRaiseThreeTimes;
        [SerializeField] private TMP_Text textRaiseThreeTimes;
        [SerializeField] private Button buttonRaiseFourTimes;
        [SerializeField] private TMP_Text textRaiseFourTimes;
        [SerializeField] private Button buttonRaiseFiveTimes;
        [SerializeField] private TMP_Text textRaiseFiveTimes;

        [SerializeField] private Button buttonAllin;

        [Header("Auto Mode")] [SerializeField] private GameObject autoMode;
        [SerializeField] private Button buttonDisableAutoMode;

        [Header("Table")] [SerializeField] private GameObject gameObjectBoardCard;
        [SerializeField] private Card[] boardCards = new Card[5];
        [SerializeField] private GameObject gameObjectMainPot;
        [SerializeField] private TMP_Text textMainPot;
        [SerializeField] private TMP_Text textSidePots;

        private GameObject _canvas;

        private readonly List<GamePlayer> _sortedGamePlayers = new();

        private string _myPlayerID;
        private string _competitionID;
        private string _competitionName;
        private string _tableID;
        private string _tableName;

        [CanBeNull] private Competition _latestCompetition;
        [CanBeNull] private Table _latestTable;

        [CanBeNull] private Jitter _jitter;

        private bool _disconnectIndicatorShown;

        private TimerHelper _settledDelayTimer;
        private const float SettledDelay = 6f; // at least 3f, server settled default delay time
        private bool _isInSettledDelay;

        protected override void Start()
        {
            base.Start();

            _canvas = GameObject.Find("Canvas");

            textJitterInfo.text = "";
            DisableAllGamePlayer();
            DisableActionButtons();
            autoMode.SetActive(false);
            ClearBoardCards();
            UpdatePots(null);

            // todo modify button behavior
            buttonTopLeft.onClick.AddListener(() => { BackToLobby(true); });
            buttonTopRight.onClick.AddListener(() =>
            {
                var gameSubMenu = Resources.Load<GameSubMenu>("Prefabs/Game/GameSubMenu");
                Instantiate(gameSubMenu, _canvas.transform, false).SetRefreshCallback(RefreshAllData);
            });

            buttonFold.onClick.AddListener(() => { SendPlayerWager(Constant.GameStatusPlayerAction.Fold); });
            buttonCall.onClick.AddListener(() => { SendPlayerWager(Constant.GameStatusPlayerAction.Call); });
            buttonCheck.onClick.AddListener(() => { SendPlayerWager(Constant.GameStatusPlayerAction.Check); });
            buttonBet.onClick.AddListener(() =>
            {
                commonActions.SetActive(false);
                betActions.SetActive(true);
                raiseActions.SetActive(false);
            });
            buttonRaise.onClick.AddListener(() =>
            {
                commonActions.SetActive(false);
                betActions.SetActive(false);
                raiseActions.SetActive(true);
            });
            buttonBetClose.onClick.AddListener(() =>
            {
                commonActions.SetActive(true);
                betActions.SetActive(false);
                raiseActions.SetActive(false);
            });
            buttonBetOneHalf.onClick.AddListener(() =>
            {
                var latestPots = _latestTable?.State?.GameState?.Status?.Pots;
                if (latestPots is not { Count: > 0 }) return;

                var totalPot = latestPots.Sum(item => item.Total);
                SendPlayerWager(Constant.GameStatusPlayerAction.Bet, totalPot * 1 / 2);
            });
            buttonBetTwoThirds.onClick.AddListener(() =>
            {
                var latestPots = _latestTable?.State?.GameState?.Status?.Pots;
                if (latestPots is not { Count: > 0 }) return;

                var totalPot = latestPots.Sum(item => item.Total);
                SendPlayerWager(Constant.GameStatusPlayerAction.Bet, totalPot * 2 / 3);
            });
            buttonBetOneTime.onClick.AddListener(() =>
            {
                var latestPots = _latestTable?.State?.GameState?.Status?.Pots;
                if (latestPots is not { Count: > 0 }) return;

                var totalPot = latestPots.Sum(item => item.Total);
                SendPlayerWager(Constant.GameStatusPlayerAction.Bet, totalPot);
            });
            buttonRaiseClose.onClick.AddListener(() =>
            {
                commonActions.SetActive(true);
                betActions.SetActive(false);
                raiseActions.SetActive(false);
            });
            buttonRaiseTwice.onClick.AddListener(() =>
            {
                var latestAction = _latestTable?.State?.GameState?.Status?.LastAction;
                if (latestAction is { Value: not null })
                {
                    SendPlayerWager(Constant.GameStatusPlayerAction.Raise, (long)latestAction.Value * 2);
                }
            });
            buttonRaiseThreeTimes.onClick.AddListener(() =>
            {
                var latestAction = _latestTable?.State?.GameState?.Status?.LastAction;
                if (latestAction is { Value: not null })
                {
                    SendPlayerWager(Constant.GameStatusPlayerAction.Raise, (long)latestAction.Value * 3);
                }
            });
            buttonRaiseFourTimes.onClick.AddListener(() =>
            {
                var latestAction = _latestTable?.State?.GameState?.Status?.LastAction;
                if (latestAction is { Value: not null })
                {
                    SendPlayerWager(Constant.GameStatusPlayerAction.Raise, (long)latestAction.Value * 4);
                }
            });
            buttonRaiseFiveTimes.onClick.AddListener(() =>
            {
                var latestAction = _latestTable?.State?.GameState?.Status?.LastAction;
                if (latestAction is { Value: not null })
                {
                    SendPlayerWager(Constant.GameStatusPlayerAction.Raise, (long)latestAction.Value * 5);
                }
            });

            buttonAllin.onClick.AddListener(() => { SendPlayerWager(Constant.GameStatusPlayerAction.Allin); });

            buttonDisableAutoMode.onClick.AddListener(() =>
            {
                ConnectionHelper.Instance.SendGamePlayerAutoMode(
                    _competitionID,
                    _tableID,
                    false,
                    resp =>
                    {
                        if (resp.Error != null)
                        {
                            CommonHelper.LogError(resp.Method + ",  Error. [Code]" + resp.Error.Code + ", [Message]" +
                                                  resp.Error.Message);
                        }
                    });
            });

            _myPlayerID = PlayerPrefs.GetString(Constant.PfKeyUserID);
            _competitionID = PlayerPrefs.GetString(Constant.PfKeyCompetitionID);
            _competitionName = PlayerPrefs.GetString(Constant.PfKeyCompetitionName);
            _tableID = PlayerPrefs.GetString(Constant.PfKeyTableID);
            _tableName = PlayerPrefs.GetString(Constant.PfKeyTableName);

            if (string.IsNullOrEmpty(_competitionID))
            {
                CommonHelper.ShowCommonDialog(
                    canvas: _canvas,
                    title: "Error Occur",
                    message: "Competition ID not found",
                    positive: "Close",
                    positiveCallback: () => BackToLobby());
            }
            else
            {
                CheckConnectionAndInit();
            }
        }

        private void CheckConnectionAndInit()
        {
            if (ConnectionHelper.Instance.IsConnected())
            {
                CommonHelper.Log("Socket is connected");
                ModifyJitterIndicator(null);
                GameInit();
            }
            else
            {
                ConnectionHelper.Instance.ConnectWebSocket();
            }
        }

        protected override void SocketOnOpen()
        {
            base.SocketOnOpen();
            CommonHelper.Log("OnOpen");
            ModifyJitterIndicator(null);
            GameInit();
        }

        protected override void SocketOnError(string errorMsg)
        {
            base.SocketOnError(errorMsg);
            CommonHelper.Log("OnError:" + errorMsg);
            ModifyJitterIndicator(null);
        }

        protected override void SocketOnClose(WebSocketCloseCode closeCode)
        {
            base.SocketOnClose(closeCode);
            CommonHelper.Log("OnClose:" + closeCode);
            ModifyJitterIndicator(null);
            GameUnInit();
        }

        private void BackToLobby(bool withTableLeave = false)
        {
            GameUnInit(() =>
            {
                if (withTableLeave)
                {
                    CommonHelper.ShowLoading(_canvas);
                    ConnectionHelper.Instance.SendTableLeave(
                        _competitionID,
                        _tableID,
                        _ =>
                        {
                            CommonHelper.HideLoading();
                            SceneManager.LoadScene(nameof(HomeScene));
                        });
                }
                else
                {
                    SceneManager.LoadScene(nameof(HomeScene));
                }
            });
        }

        private void ModifyJitterIndicator([CanBeNull] Jitter jitter)
        {
            _jitter = jitter;
            if (jitter != null)
            {
                if (jitter.DelayMilliseconds == Constant.TimeoutJitterDelayValue)
                {
                    _disconnectIndicatorShown = true;
                    CommonHelper.ShowDisconnect(_canvas);
                }
                else
                {
                    if (_disconnectIndicatorShown)
                    {
                        CommonHelper.HideDisconnect();
                        _disconnectIndicatorShown = false;
                    }
                }
            }

            UpdateJitterAndUpdateSerial();
        }

        private void UpdateJitterAndUpdateSerial()
        {
            string value;
            if (_jitter == null)
            {
                value = "----(----)";
            }
            else
            {
                value = _jitter.Rate + "(" + _jitter.DelayMilliseconds + ")";
            }

            textJitterInfo.text = value + "\n" + _latestCompetition?.UpdateSerial + "|" + _latestTable?.UpdateSerial +
                                  (_isInSettledDelay ? "(SD)" : "");
        }

        private void CompetitionUpdated(Competition competition)
        {
            if (competition.UpdateSerial - _latestCompetition?.UpdateSerial > 1)
            {
                CommonHelper.LogWarning("Jump number occur, latest competition serial [" +
                                        _latestCompetition?.UpdateSerial + "], current competition serial [" +
                                        competition.UpdateSerial + "]");
            }

            if (_latestCompetition?.UpdateSerial > competition.UpdateSerial)
            {
                CommonHelper.LogWarning("latest competition serial [" + _latestCompetition?.UpdateSerial +
                                        "] is large then current serial [" + competition.UpdateSerial + "]");
                return;
            }

            _latestCompetition = competition;

            // skip all event when in settled delay
            if (_isInSettledDelay) return;

            UpdateJitterAndUpdateSerial();
            UpdateCompetitionPlayers(competition);
            CheckCompetitionState(competition);
        }

        private void TableUpdated(Table table)
        {
            if (table.UpdateSerial - _latestTable?.UpdateSerial > 1)
            {
                CommonHelper.LogWarning("Jump number occur, latest table serial [" + _latestTable?.UpdateSerial +
                                        "], current table serial [" + table.UpdateSerial + "]");
            }

            if (_latestTable?.UpdateSerial > table.UpdateSerial)
            {
                CommonHelper.LogWarning("latest table serial [" + _latestTable?.UpdateSerial +
                                        "] is large then current serial [" + table.UpdateSerial + "]");
                return;
            }

            _latestTable = table;
            UpdateJitterAndUpdateSerial();

            var setting = _competitionName + " (" + _tableName + ")";
            if (table.State is { BlindState: not null })
            {
                setting += "\n盲注: " + table.State.BlindState.Sb + "/" + table.State.BlindState.Bb;
            }

            setting += "\n動作時間: " + table.Meta.ActionTime;

            var totalTime = table.Meta.MaxDuration;
            var hour = totalTime / 3600;
            var min = (totalTime - hour * 3600) / 60;
            var second = totalTime % 60;
            setting += "\n比賽時間總長: " + hour.ToString("00") + ":" + min.ToString("00") + ":" + second.ToString("00");

            textTableSetting.text = setting;

            // skip all event when in settled delay
            if (_isInSettledDelay) return;

            UpdateTablePlayers(table);
            CheckTableStatus(table);
            CheckGameStateCurrentState(table);
            UpdatePots(table);
        }

        private void SetOnAutoMode(AutoModeUpdated autoModeUpdated)
        {
            if (!string.Equals(autoModeUpdated.CompetitionID, _competitionID) ||
                !string.Equals(autoModeUpdated.TableID, _tableID)) return;

            autoMode.SetActive(autoModeUpdated.IsOn);
            if (autoMode.activeSelf)
            {
                DisableActionButtons();
            }
        }

        private void RefreshAllData()
        {
            GameUnInit(CheckConnectionAndInit);
        }

        private void GameInit()
        {
            ConnectionHelper.Instance.SetOnJitter(ModifyJitterIndicator);

            _sortedGamePlayers.Clear();
            DisableAllGamePlayer();

            CommonHelper.ShowLoading(_canvas);
            ConnectionHelper.Instance.SendAuthenticate(
                PlayerPrefs.GetString(Constant.PfKeyUserToken),
                resp =>
                {
                    if (!CommonHelper.CheckResponseIsSuccess(_canvas, resp.Method, resp.Error, () => BackToLobby()))
                        return;

                    ConnectionHelper.Instance.SendReady(
                        readyResp =>
                        {
                            if (!CommonHelper.CheckResponseIsSuccess(_canvas, readyResp.Method, readyResp.Error,
                                    () => BackToLobby()))
                                return;

                            ConnectionHelper.Instance.SendListPlayerActiveCompetitions(
                                listActiveCompetitionResp =>
                                {
                                    if (listActiveCompetitionResp.Error != null ||
                                        listActiveCompetitionResp.Result == null)
                                    {
                                        CommonHelper.ShowCommonDialog(
                                            canvas: _canvas,
                                            title: "Error Occur",
                                            message: "No active competition found",
                                            positive: "Close",
                                            positiveCallback: () => BackToLobby());
                                    }
                                    else
                                    {
                                        var isReBuyWaiting = false;
                                        if (!string.IsNullOrEmpty(_competitionID))
                                        {
                                            foreach (var item in listActiveCompetitionResp.Result
                                                         .ActiveCompetitions.Where(item =>
                                                             string.Equals(item.CompetitionID, _competitionID)))
                                            {
                                                if (!IsAvailableCompetitions(item.PlayerStatus))
                                                    continue;

                                                isReBuyWaiting = string.Equals(item.PlayerStatus,
                                                    Constant.PlayerStatus.ReBuyWaiting);

                                                _competitionName = item.CompetitionName;
                                                _tableID = item.TableID;
                                                _tableName = item.TableName;
                                                break;
                                            }
                                        }

                                        if (isReBuyWaiting)
                                        {
                                            // todo show buy in dialog when design support
                                            BackToLobby();
                                        }
                                        else
                                        {
                                            UpdateCompetitionTableDataAndJoin();
                                        }
                                    }
                                });
                        });
                });
        }

        private static bool IsAvailableCompetitions(string playerStatus)
        {
            var whiteList = new List<string>
            {
                Constant.PlayerStatus.WaitingTableBalancing,
                Constant.PlayerStatus.Playing,
                Constant.PlayerStatus.ReBuyWaiting
            };

            return whiteList.Contains(playerStatus);
        }

        private void UpdateCompetitionTableDataAndJoin()
        {
            ConnectionHelper.Instance.SendCompetitionGetLatest(
                _competitionID,
                respCompetitionLatest =>
                {
                    if (!CommonHelper.CheckResponseIsSuccess(_canvas, respCompetitionLatest.Method,
                            respCompetitionLatest.Error, () => BackToLobby()))
                        return;

                    if (respCompetitionLatest.Result?.Competition == null)
                    {
                        CommonHelper.ShowCommonDialog(
                            canvas: _canvas,
                            title: "Error Occur",
                            message: "Competition Not found",
                            positive: "Close",
                            positiveCallback: () => { BackToLobby(); });
                    }
                    else
                    {
                        CompetitionUpdated(respCompetitionLatest.Result.Competition);

                        ConnectionHelper.Instance.SendTableGetLatest(
                            _competitionID,
                            _tableID,
                            respTableLatest =>
                            {
                                if (!CommonHelper.CheckResponseIsSuccess(_canvas, respTableLatest.Method,
                                        respTableLatest.Error, () => BackToLobby()))
                                    return;

                                if (respTableLatest.Result != null)
                                {
                                    TableUpdated(respTableLatest.Result.Table);
                                }
                            });

                        // no need to wait RPC Match.CompetitionGetLatest response
                        ConnectionHelper.Instance.SendTableJoin(
                            _competitionID,
                            _tableID,
                            tableJoinResp =>
                            {
                                CommonHelper.HideLoading();

                                CommonHelper.CheckResponseIsSuccess(_canvas, tableJoinResp.Method, tableJoinResp.Error,
                                    () => { BackToLobby(); });

                                // on event after table join
                                ConnectionHelper.Instance.SetOnCompetition(CompetitionUpdated);
                                ConnectionHelper.Instance.SetOnTable(TableUpdated);
                                ConnectionHelper.Instance.SetOnAutoMode(SetOnAutoMode);
                            });
                    }
                });
        }

        private void GameUnInit(UnityAction finishCallback = null)
        {
            ConnectionHelper.Instance.SetOnJitter(null);
            ConnectionHelper.Instance.SetOnCompetition(null);
            ConnectionHelper.Instance.SetOnTable(null);
            ConnectionHelper.Instance.SetOnAutoMode(null);

            CommonHelper.ShowLoading(_canvas);

            if (_isInSettledDelay)
            {
                _settledDelayTimer?.StopTimer();
                _isInSettledDelay = false;
            }

            ConnectionHelper.Instance.SendUpdateCompetitionEventSubscribeStates(
                _competitionID,
                false,
                resp =>
                {
                    CommonHelper.HideLoading();

                    if (resp.Error != null)
                    {
                        CommonHelper.LogError(resp.Method + ",  Error. [Code]" + resp.Error.Code + ", [Message]" +
                                              resp.Error.Message);
                    }


                    UpdateJitterAndUpdateSerial();
                    _sortedGamePlayers.Clear();
                    DisableAllGamePlayer();
                    _latestCompetition = null;
                    _latestTable = null;
                    _jitter = null;
                    UpdateJitterAndUpdateSerial();
                    DisableActionButtons();
                    ClearBoardCards();
                    UpdatePots(null);
                    StopAllSoundEffect();
                    gameSoundEffect.StopAll();
                    autoMode.SetActive(false);

                    finishCallback?.Invoke();
                });
        }

        private void DisableAllGamePlayer()
        {
            HideGamePlayer(gamePlayerLeft1);
            HideGamePlayer(gamePlayerLeft2);
            HideGamePlayer(gamePlayerLeft3);
            HideGamePlayer(gamePlayerLeft4);
            HideGamePlayer(gameMainPlayer);
            HideGamePlayer(gamePlayerRight1);
            HideGamePlayer(gamePlayerRight2);
            HideGamePlayer(gamePlayerRight3);
            HideGamePlayer(gamePlayerRight4);
        }

        private static void HideGamePlayer(Component gamePlayer)
        {
            if (!gamePlayer.IsUnityNull())
            {
                gamePlayer.gameObject.SetActive(false);
            }
        }

        private void UpdateCompetitionPlayers(Competition competition)
        {
            // find all competition players with same table
            var sameTablePlayer = competition.State?.Players?.FindAll(player =>
                string.Equals(player.TableID, _tableID));
            if (sameTablePlayer == null) return;

            if (_sortedGamePlayers.Count == 0)
            {
                SortGamePlayers(sameTablePlayer);
            }

            if (_sortedGamePlayers.Count == 9)
            {
                for (var seat = 0; seat < _sortedGamePlayers.Count; seat++)
                {
                    _sortedGamePlayers[seat]
                        .SetCompetitionPlayer(sameTablePlayer.Find(player => Equals(player.Seat, seat)), () =>
                        {
                            CommonHelper.ShowCommonDialog(
                                canvas: _canvas,
                                title: "System",
                                message: "Has been knockout",
                                positive: "To Lobby",
                                positiveCallback: () => BackToLobby());
                        });
                }
            }
            else
            {
                DisableAllGamePlayer();
            }
        }


        private void SortGamePlayers([NotNull] List<CompetitionPlayer> sameTablePlayer)
        {
            var me = sameTablePlayer.Find(player =>
                string.Equals(player.PlayerID, _myPlayerID));
            if (me == null) return;

            var mySeat = me.Seat;
            const int defaultMySeatIndex = 4;
            var diff = defaultMySeatIndex - mySeat;

            var defaultGamePlayers = new List<GamePlayer>
            {
                gamePlayerLeft1,
                gamePlayerLeft2,
                gamePlayerLeft3,
                gamePlayerLeft4,
                gameMainPlayer,
                gamePlayerRight4,
                gamePlayerRight3,
                gamePlayerRight2,
                gamePlayerRight1
            };

            _sortedGamePlayers.Clear();
            for (var index = 0; index < defaultGamePlayers.Count; index++)
            {
                var targetIndex = index + diff;
                if (targetIndex < 0)
                {
                    targetIndex += defaultGamePlayers.Count;
                }

                targetIndex %= defaultGamePlayers.Count;

                _sortedGamePlayers.Add(defaultGamePlayers[targetIndex]);
            }
        }

        private void UpdateTablePlayers(Table table)
        {
            if (_sortedGamePlayers.Count != 9 || table.State?.PlayerStates == null) return;

            for (var seat = 0; seat < 9; seat++)
            {
                var playerStateIndex =
                    table.State.PlayerStates.FindIndex(player => Equals(player.Seat, seat));

                _sortedGamePlayers[seat].SetTablePlayer(playerStateIndex == -1
                    ? null
                    : table.State.PlayerStates[playerStateIndex]);

                if (table.State.GamePlayerIndexes == null ||
                    table.State.GameState is not { Players: not null }) continue;

                var gamePlayerIndex = table.State.GamePlayerIndexes.FindIndex(idx => idx == playerStateIndex);
                var playerState = gamePlayerIndex != -1 ? table.State.GameState.Players[gamePlayerIndex] : null;

                _sortedGamePlayers[seat].SetGameStatePlayerState(
                    table.State.GameState.Status.Round,
                    playerState,
                    table.Meta.ActionTime,
                    (table.UpdateAt + table.Meta.ActionTime) * 1000 -
                    ConnectionHelper.Instance.GetDiffTimeInMillisecondsWithServer(),
                    table.State.GameState.Result,
                    () =>
                    {
                        if (!_isInSettledDelay)
                        {
                            ConnectionHelper.Instance.SendGamePlayerReady(
                                _competitionID,
                                _tableID,
                                resp =>
                                {
                                    if (resp.Error != null)
                                    {
                                        CommonHelper.LogError(resp.Method + ",  Error. [Code]" + resp.Error.Code +
                                                              ", [Message]" + resp.Error.Message);
                                    }
                                });
                        }
                    },
                    (allowedActions, point) =>
                    {
                        if (CommonHelper.IsAvailableActionType(allowedActions) && !autoMode.activeSelf)
                        {
                            var latestPots = _latestTable?.State?.GameState?.Status?.Pots;
                            var latestAction = _latestTable?.State?.GameState?.Status?.LastAction;

                            playerActions.SetActive(true);
                            commonActions.SetActive(true);
                            betActions.SetActive(false);
                            raiseActions.SetActive(false);

                            buttonFold.interactable =
                                allowedActions.Contains(Constant.GameStatusPlayerAction.Fold);
                            buttonCall.interactable =
                                allowedActions.Contains(Constant.GameStatusPlayerAction.Call);
                            buttonCheck.interactable =
                                allowedActions.Contains(Constant.GameStatusPlayerAction.Check);
                            if (latestPots is { Count: > 0 } &&
                                allowedActions.Contains(Constant.GameStatusPlayerAction.Bet))
                            {
                                buttonBet.interactable = true;

                                var totalPot = latestPots.Sum(item => item.Total);

                                UpdateBetRaiseInfo(buttonBetOneHalf, textBetOneHalf, "Bet \u00bdx\n", point,
                                    totalPot / 2, table.Meta.MinChipUnit);
                                UpdateBetRaiseInfo(buttonBetTwoThirds, textBetTwoThirds, "Bet \u2154x\n", point,
                                    totalPot * 2 / 3, table.Meta.MinChipUnit);
                                UpdateBetRaiseInfo(buttonBetOneTime, textBetOneTime, "Bet 1x\n", point, totalPot,
                                    table.Meta.MinChipUnit);
                            }
                            else
                            {
                                buttonBet.interactable = false;
                            }

                            if (latestAction is { Value: not null } &&
                                allowedActions.Contains(Constant.GameStatusPlayerAction.Raise))
                            {
                                buttonRaise.interactable = true;

                                UpdateBetRaiseInfo(buttonRaiseTwice, textRaiseTwice, "Raise 2x\n", point,
                                    (latestAction.Value ?? 0L) * 2, table.Meta.MinChipUnit);
                                UpdateBetRaiseInfo(buttonRaiseThreeTimes, textRaiseThreeTimes, "Raise 3x\n", point,
                                    (latestAction.Value ?? 0L) * 3, table.Meta.MinChipUnit);
                                UpdateBetRaiseInfo(buttonRaiseFourTimes, textRaiseFourTimes, "Raise 4x\n", point,
                                    (latestAction.Value ?? 0L) * 4, table.Meta.MinChipUnit);
                                UpdateBetRaiseInfo(buttonRaiseFiveTimes, textRaiseFiveTimes, "Raise 5x\n", point,
                                    (latestAction.Value ?? 0L) * 5, table.Meta.MinChipUnit);
                            }
                            else
                            {
                                buttonRaise.interactable = false;
                            }

                            buttonAllin.interactable =
                                allowedActions.Contains(Constant.GameStatusPlayerAction.Allin);
                        }
                        else
                        {
                            DisableActionButtons();
                        }
                    },
                    gameSoundEffect);
            }
        }

        private static void UpdateBetRaiseInfo(Selectable button, TMP_Text text, string description, long currentPoint,
            long targetPoint, long minChipUnit)
        {
            button.interactable = currentPoint >= targetPoint && targetPoint > minChipUnit;
            text.text = description + targetPoint;
        }

        private void CheckCompetitionState(Competition competition)
        {
            if (competition.State == null) return;

            switch (competition.State.Status)
            {
                case Constant.CompetitionStatus.End:
                case Constant.CompetitionStatus.AutoEnd:
                case Constant.CompetitionStatus.ForceEnd:
                    CommonHelper.ShowCommonDialog(
                        canvas: _canvas,
                        title: "System",
                        message: "Competition ended",
                        positive: "Close",
                        positiveCallback: () => BackToLobby());
                    break;
            }
        }

        private void CheckTableStatus(Table table)
        {
            if (table.State == null) return;

            switch (table.State.Status)
            {
                case Constant.TableStatus.TableGameSettled:
                    _settledDelayTimer ??= new TimerHelper(this);
                    _settledDelayTimer.StopTimer();

                    _isInSettledDelay = true;
                    _settledDelayTimer.StartTimer(SettledDelay, () =>
                    {
                        _isInSettledDelay = false;

                        // check has new table event
                        if (_latestTable == null || Equals(table.UpdateSerial, _latestTable.UpdateSerial)) return;

                        CompetitionUpdated(_latestCompetition);
                        TableUpdated(_latestTable);
                    });

                    gameSoundEffect.PlayCheering();
                    break;
            }
        }

        private void CheckGameStateCurrentState(Table table)
        {
            if (table.State?.GameState == null) return;

            bool disableActions;
            bool clearBoardCard;
            switch (table.State.GameState.Status.CurrentEvent)
            {
                case Constant.GameStatusCurrentEvent.ReadyRequested:
                    disableActions = true;
                    clearBoardCard = false;
                    break;

                case Constant.GameStatusCurrentEvent.BlindsRequested:
                    disableActions = true;
                    clearBoardCard = true;
                    break;

                case Constant.GameStatusCurrentEvent.RoundStarted:
                    disableActions = false;
                    clearBoardCard = false;
                    break;

                case Constant.GameStatusCurrentEvent.RoundClosed:
                    disableActions = true;
                    clearBoardCard = false;
                    break;

                case Constant.GameStatusCurrentEvent.GameClosed:
                    disableActions = true;
                    clearBoardCard = false;
                    break;

                default:
                    disableActions = true;
                    clearBoardCard = true;
                    break;
            }

            if (disableActions)
            {
                DisableActionButtons();
            }

            if (clearBoardCard)
            {
                ClearBoardCards();
            }
            else
            {
                UpdateBoardCards(table.State.GameState.Status.Board);
            }
        }

        private void DisableActionButtons()
        {
            playerActions.SetActive(false);
        }

        private void ClearBoardCards()
        {
            gameObjectBoardCard.SetActive(false);
            foreach (var item in boardCards)
            {
                item.gameObject.SetActive(false);
            }
        }

        private void UpdateBoardCards([CanBeNull] IReadOnlyList<string> board)
        {
            if (board == null)
            {
                ClearBoardCards();
                return;
            }

            gameObjectBoardCard.SetActive(board.Count > 1);
            for (var index = 0; index < boardCards.Length; index++)
            {
                if (board.Count > index)
                {
                    boardCards[index].SetCard(board[index]);
                    boardCards[index].gameObject.SetActive(true);
                }
                else
                {
                    boardCards[index].gameObject.SetActive(false);
                }
            }
        }

        private void SendPlayerWager(string action, long chips = 0)
        {
            ConnectionHelper.Instance.SendGamePlayerWager(
                _competitionID,
                _tableID,
                action,
                chips,
                resp =>
                {
                    if (resp.Error != null)
                    {
                        CommonHelper.LogError(resp.Method + ",  Error. [Code]" + resp.Error.Code + ", [Message]" +
                                              resp.Error.Message);
                    }
                });
        }

        private void UpdatePots([CanBeNull] Table table)
        {
            if (table?.State?.GameState?.Status.Pots is { Count: > 0 })
            {
                textMainPot.text = table.State.GameState.Status.Pots.First().Total.ToString();
                gameObjectMainPot.SetActive(true);
                if (table.State.GameState.Status.Pots.Count > 2)
                {
                    var sidePots = "Side Pots:";
                    for (var index = table.State.GameState.Status.Pots.Count - 1; index >= 0; index--)
                    {
                        sidePots += "\n" + table.State.GameState.Status.Pots[index].Total;
                    }

                    textSidePots.text = sidePots;
                }
                else
                {
                    textSidePots.text = "";
                }
            }
            else
            {
                gameObjectMainPot.SetActive(false);
                textSidePots.text = "";
            }
        }
    }
}