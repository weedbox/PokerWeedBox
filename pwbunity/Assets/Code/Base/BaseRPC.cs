using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Code.Helper;
using Code.Model;
using Code.Model.Game.NotificationEvent;
using Code.Model.System;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Code.Base
{
    public class BaseRPC : BaseSocket
    {
        private int _counterIdForRPC;

        private readonly Dictionary<int, Callback<string>> _callbacks = new();
        private readonly Dictionary<int, TimerHelper> _sendTimeout = new();

        private long _diffMillisecondsWithServer;

        #region listener

        private UnityAction<Jitter> _onJitter;
        private UnityAction<Competition> _onCompetition;
        private UnityAction<Table> _onTable;
        private UnityAction<AutoModeUpdated> _onAutoMode;

        private const float EventBufferSecond = 0.01f;
        private bool _isCompetitionBuffering;
        private readonly List<Competition> _bufferCompetitions = new();
        private TimerHelper _competitionBufferTimer;
        private bool _isTableBuffering;
        private readonly List<Table> _bufferTables = new();
        private TimerHelper _tableBufferTimer;

        #endregion

        #region Ping Value

        private const int ChangePingIntervalMs = 250;
        private const float PingIntervalNormal = 3f;
        private const float PingIntervalFast = 1f;
        private TimerHelper _pingRepeatedTimer;

        private const float PingTimeout = 5;
        private TimerHelper _pingTimeoutTimer;

        #endregion

        private const float CommonMessageTimeout = 10f;

        protected BaseRPC()
        {
            _counterIdForRPC = 1;

            AddOnOpenCallback(() => { SendPingTimer(PingIntervalFast); });
            SetOnMessageCallback(ReceiveMessage);
            AddOnErrorCallback(_ => { StopAllTimerAndCallbackError(); });
            AddOnCloseCallback(_ => { StopAllTimerAndCallbackError(); });
        }

        public void SetMonoBehaviour(MonoBehaviour value)
        {
            MonoBehaviour = value;

            // Stop and Clear all timer and callback when new scene is loaded
            MonoBehaviour.StopAllCoroutines();
            _callbacks.Clear();
            var keys = new List<int>(_sendTimeout.Keys);
            foreach (var key in keys)
            {
                ClearSendMessageTimeout(key);
            }

            _competitionBufferTimer = null;
            _tableBufferTimer = null;
            _pingRepeatedTimer = null;
            _pingTimeoutTimer = null;

            if (Connected)
            {
                SendPingTimer(PingIntervalFast);
            }
        }

        public long GetDiffTimeInMillisecondsWithServer()
        {
            return _diffMillisecondsWithServer;
        }

        public void SetOnJitter(UnityAction<Jitter> value)
        {
            _onJitter = value;
        }

        public void SetOnCompetition(UnityAction<Competition> value)
        {
            _isCompetitionBuffering = false;
            _bufferCompetitions.Clear();
            _competitionBufferTimer?.StopTimer();
            _onCompetition = value;
        }

        public void SetOnTable(UnityAction<Table> value)
        {
            _isTableBuffering = false;
            _bufferTables.Clear();
            _tableBufferTimer?.StopTimer();
            _onTable = value;
        }

        public void SetOnAutoMode(UnityAction<AutoModeUpdated> value)
        {
            _onAutoMode = value;
        }

        private void StopAllTimerAndCallbackError()
        {
            _competitionBufferTimer?.StopTimer();
            _tableBufferTimer?.StopTimer();
            _pingRepeatedTimer?.StopTimer();
            _pingTimeoutTimer?.StopTimer();

            _sendTimeout.Clear();
            foreach (var item in _callbacks)
            {
                item.Value.Action.Invoke(JsonConvert.SerializeObject(new RPCResponse<object>
                {
                    JsonRpc = "2.0",
                    Id = -1,
                    Method = item.Value.Method,
                    Error = new Error(503, "Service Unavailable")
                }));
            }

            _callbacks.Clear();
        }

        private void ReceiveMessage(byte[] bytes)
        {
            // getting the message as a string
            var rawData = Encoding.UTF8.GetString(bytes);

            if (!IsInLogBlockList(rawData))
            {
                CommonHelper.Log("<<< [Receive] <<<\n" + rawData + "\n");
            }

            try
            {
                var resp = JsonConvert.DeserializeObject<RPCResponse<object>>(rawData);

                // Notification
                if (resp.Id == 0)
                {
                    if (resp.Result == null) return;

                    var updateEvent =
                        JsonConvert.DeserializeObject<UpdateEvent<object>>(resp.Result.ToString().Replace("\n", ""));
                    if (updateEvent.Event == null) return;

                    if (string.Equals(updateEvent.EventName, Constant.SocketOnCompetitionUpdated))
                    {
                        _bufferCompetitions.Add(
                            JsonConvert.DeserializeObject<Competition>(updateEvent.Event.ToString()));
                        if (_isCompetitionBuffering) return;
                        StartCompetitionBufferTimer();
                    }
                    else if (string.Equals(updateEvent.EventName, Constant.SocketOnTableUpdated))
                    {
                        _bufferTables.Add(JsonConvert.DeserializeObject<Table>(updateEvent.Event.ToString()));
                        if (_isTableBuffering) return;
                        StartTableBufferTimer();
                    }
                    else if (string.Equals(updateEvent.EventName, Constant.SocketOnAutoModeUpdated))
                    {
                        _onAutoMode?.Invoke(
                            JsonConvert.DeserializeObject<AutoModeUpdated>(updateEvent.Event.ToString()));
                    }

                    return;
                }

                // Clear Timeout Timer
                ClearSendMessageTimeout(resp.Id);

                // CallBack
                if (!_callbacks.TryGetValue(resp.Id, out var callback)) return;

                if (callback != null)
                {
                    // using string add for method name, not using SerializeObject to prevent null object transform
                    const string header = "{\"jsonrpc\":\"2.0\"";
                    if (rawData.StartsWith(header))
                    {
                        rawData = rawData.Replace(header, header + ",\"name\":\"" + callback.Method + "\"");
                    }

                    callback.Action.Invoke(rawData);
                }

                _callbacks.Remove(resp.Id);
            }
            catch (Exception e)
            {
                CommonHelper.LogError("ReceiveMessage Error: " + e.Message);
            }
        }

        private void StartCompetitionBufferTimer()
        {
            _isCompetitionBuffering = true;
            _competitionBufferTimer ??= new TimerHelper(MonoBehaviour);
            _competitionBufferTimer.StartTimer(EventBufferSecond, PopCompetitionBufferItem);
        }

        private void PopCompetitionBufferItem()
        {
            if (_bufferCompetitions.Count > 0)
            {
                _bufferCompetitions.Sort((x, y) => x.UpdateSerial.CompareTo(y.UpdateSerial));

                var targetCompetition = _bufferCompetitions.First();
                _onCompetition?.Invoke(targetCompetition);
                _bufferCompetitions.Remove(targetCompetition);

                if (_bufferCompetitions.Count > 0)
                {
                    StartCompetitionBufferTimer();
                }
                else
                {
                    _isCompetitionBuffering = false;
                }
            }
            else
            {
                _isCompetitionBuffering = false;
            }
        }

        private void StartTableBufferTimer()
        {
            _isTableBuffering = true;
            _tableBufferTimer ??= new TimerHelper(MonoBehaviour);
            _tableBufferTimer.StartTimer(EventBufferSecond, PopTableBufferItem);
        }

        private void PopTableBufferItem()
        {
            if (_bufferTables.Count > 0)
            {
                _bufferTables.Sort((x, y) => x.UpdateSerial.CompareTo(y.UpdateSerial));
                var targetTable = _bufferTables.First();
                _onTable?.Invoke(targetTable);
                _bufferTables.Remove(targetTable);

                if (_bufferTables.Count > 0)
                {
                    StartTableBufferTimer();
                }
                else
                {
                    _isTableBuffering = false;
                }
            }
            else
            {
                _isTableBuffering = false;
            }
        }

        private int GeneratedId()
        {
            return _counterIdForRPC++;
        }

        protected void SendMessage<T>(RPCRequest rpcRequest, [CanBeNull] UnityAction<RPCResponse<T>> callback)
        {
            rpcRequest.JsonRpc = "2.0";
            rpcRequest.Id = GeneratedId();

            var json = JsonConvert.SerializeObject(rpcRequest);

            if (Connected)
            {
                if (callback != null)
                {
                    _callbacks.TryAdd(
                        rpcRequest.Id,
                        new Callback<string>(
                            method: rpcRequest.Method,
                            action: rawData =>
                            {
                                try
                                {
                                    callback.Invoke(JsonConvert.DeserializeObject<RPCResponse<T>>(rawData));
                                }
                                catch (Exception e)
                                {
                                    callback.Invoke(new RPCResponse<T>
                                    {
                                        JsonRpc = "2.0",
                                        Id = -1,
                                        Method = rpcRequest.Method,
                                        Error = new Error(503, "Deserialize Error: " + e.Message)
                                    });
                                }
                            })
                    );

                    // add timer for send message
                    ClearSendMessageTimeout(rpcRequest.Id);

                    var timeout = new TimerHelper(MonoBehaviour);
                    timeout.StartTimer(CommonMessageTimeout, () =>
                    {
                        // Clear Callback
                        if (_callbacks.ContainsKey(rpcRequest.Id))
                        {
                            _callbacks.Remove(rpcRequest.Id);
                        }

                        // Send Timeout back
                        callback.Invoke(new RPCResponse<T>
                        {
                            JsonRpc = "2.0",
                            Id = -1,
                            Method = rpcRequest.Method,
                            Error = new Error(408, "Request Timeout")
                        });
                    });
                    _sendTimeout.TryAdd(rpcRequest.Id, timeout);
                }

                try
                {
                    SendText(json);
                }
                catch (Exception e)
                {
                    ClearSendMessageTimeout(rpcRequest.Id);
                    callback?.Invoke(new RPCResponse<T>
                    {
                        JsonRpc = "2.0",
                        Id = -1,
                        Method = rpcRequest.Method,
                        Error = new Error(503, e.Message)
                    });
                }
            }
            else
            {
                callback?.Invoke(new RPCResponse<T>
                {
                    JsonRpc = "2.0",
                    Id = -1,
                    Method = rpcRequest.Method,
                    Error = new Error(503, "Service Unavailable")
                });
            }

            if (!IsInLogBlockList(json))
            {
                CommonHelper.Log(">>> [Sent] >>>\n" + json + "\n");
            }
        }

        private void ClearSendMessageTimeout(int key)
        {
            if (!_sendTimeout.TryGetValue(key, out var timeoutTimer)) return;
            timeoutTimer.StopTimer();
            _sendTimeout.Remove(key);
        }

        private static bool IsInLogBlockList(string value)
        {
            var blockList = new List<string>
            {
                "System.DeepPing", // Deep Ping Method
                // "\"result\":\"\"", // Empty Result
                "\"result\":{\"client_timestamp\":" // Deep Ping Result
            };
            return blockList.Any(value.Contains);
        }

        #region Ping

        protected void SendDeepPing([CanBeNull] UnityAction<RPCResponse<RespDeepPing>> callback = null)
        {
            var rpc = new RPCRequest
            {
                Method = "System.DeepPing",
                Parameters = new List<object> { DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString() }
            };

            SendMessage<RespDeepPing>(rpc, resp =>
            {
                if (resp.Error != null || resp.Result == null) return;

                const long oneSecondInMilliseconds = 1000;
                var transmissionConsumeTime =
                    (long)(0.5f * (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - resp.Result.ClientTimestamp));
                var diff = resp.Result.ServerTimestamp - (resp.Result.ClientTimestamp + transmissionConsumeTime);
                _diffMillisecondsWithServer =
                    Math.Abs(diff) < oneSecondInMilliseconds
                        ? 0
                        : diff; // using local time if diff less then one second

                callback?.Invoke(resp);
            });
        }

        private void SendPingTimer(float interval)
        {
            _pingRepeatedTimer ??= new TimerHelper(MonoBehaviour);
            _pingRepeatedTimer.StopTimer();
            _pingRepeatedTimer.StartTimer(interval, () =>
            {
                if (Connected)
                {
                    KeepAlive();
                }
            });
        }

        private void KeepAlive()
        {
            _pingTimeoutTimer ??= new TimerHelper(MonoBehaviour);
            _pingTimeoutTimer.StopTimer();
            _pingTimeoutTimer.StartTimer(PingTimeout, OnPingTimout);

            SendDeepPing(PingResponse);
        }

        private void OnPingTimout()
        {
            _onJitter?.Invoke(new Jitter(Constant.TimeoutJitterDelayValue));
            DisconnectWebSocket(false);
        }

        private void PingResponse(RPCResponse<RespDeepPing> resp)
        {
            _pingTimeoutTimer.StopTimer();

            if (resp.Error != null || resp.Result == null)
            {
                SendPingTimer(PingIntervalFast);
            }
            else
            {
                var delayMilliseconds = resp.Result.ServerTimestamp - resp.Result.ClientTimestamp -
                                        _diffMillisecondsWithServer;
                _onJitter?.Invoke(new Jitter(delayMilliseconds));
                SendPingTimer(delayMilliseconds >= ChangePingIntervalMs
                    ? PingIntervalFast
                    : PingIntervalNormal);
            }
        }

        #endregion
    }
}