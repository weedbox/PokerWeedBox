using Code.Helper;
using JetBrains.Annotations;
using NativeWebSocket;
using UnityEngine;
using UnityEngine.Events;

namespace Code.Base
{
    public class BaseSocket
    {
        protected bool Connected;

        private WebSocket _nativeWebsocket;
        private bool _isManualDisconnected;

        private UnityAction _onOpen;
        private UnityAction<byte[]> _onMessage;
        private UnityAction<string> _onError;
        private UnityAction<WebSocketCloseCode> _onClose;
        
        protected MonoBehaviour MonoBehaviour;
        [CanBeNull] protected GameObject Canvas;

        public bool IsConnected()
        {
            return Connected;
        }

        public void AddOnOpenCallback(UnityAction value)
        {
            _onOpen += value;
        }

        public void RemoveOnOpenCallback(UnityAction value)
        {
            _onOpen -= value;
        }

        protected void SetOnMessageCallback(UnityAction<byte[]> value)
        {
            _onMessage = value;
        }

        public void AddOnErrorCallback(UnityAction<string> value)
        {
            _onError += value;
        }

        public void RemoveOnErrorCallback(UnityAction<string> value)
        {
            _onError -= value;
        }

        public void AddOnCloseCallback(UnityAction<WebSocketCloseCode> value)
        {
            _onClose += value;
        }

        public void RemoveOnCloseCallback(UnityAction<WebSocketCloseCode> value)
        {
            _onClose -= value;
        }

        public void DispatchMessageQueue()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            _nativeWebsocket?.DispatchMessageQueue();
#endif
        }

        public async void ConnectWebSocket()
        {
            if (Connected) return;

            _nativeWebsocket = new WebSocket(Constant.SocketURL);
            
            _nativeWebsocket.OnOpen += () =>
            {
                Connected = true;
                _isManualDisconnected = false;
                _onOpen?.Invoke();
            };

            _nativeWebsocket.OnMessage += bytes => { _onMessage?.Invoke(bytes); };

            _nativeWebsocket.OnError += e =>
            {
                DisconnectBehavior();
                _onError?.Invoke(e);
            };

            _nativeWebsocket.OnClose += e =>
            {
                DisconnectBehavior();
                _onClose?.Invoke(e);
            };

            _reconnectTimer?.StopTimer();
            await _nativeWebsocket.Connect();
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        public void DisconnectWebSocket(bool manual = true)
#else
        public async void DisconnectWebSocket(bool manual = true)
#endif
        {
            _isManualDisconnected = manual;
            if (_nativeWebsocket != null && Connected)
            {
#if !UNITY_WEBGL || UNITY_EDITOR
                _nativeWebsocket.CancelConnection();
#else
                await _nativeWebsocket.Close();
#endif
            }
        }

        protected async void SendText(string value)
        {
            await _nativeWebsocket.SendText(value);
        }

        private const float ReconnectDelaySecond = 3f; 
        private TimerHelper _reconnectTimer;

        private void DisconnectBehavior()
        {
            Connected = false;
            _nativeWebsocket = null;
            
            if (_isManualDisconnected) return;
            
            _reconnectTimer ??= new TimerHelper(MonoBehaviour);
            _reconnectTimer.StopTimer();
            _reconnectTimer.StartTimer(ReconnectDelaySecond, ConnectWebSocket);
        }
    }
}