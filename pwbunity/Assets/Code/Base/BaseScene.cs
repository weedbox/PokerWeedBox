using Code.Helper;
using Code.Prefab.Common;
using NativeWebSocket;
using UnityEngine;

namespace Code.Base
{
    public class BaseScene : MonoBehaviour
    {
        [SerializeField] protected CommonSoundEffect commonSoundEffect;
        
        private CommonSoundEffect _commonSoundEffect;
        
        protected virtual void Start()
        {
            CommonHelper.UpdateVolume();
            
            _commonSoundEffect = GameObject.FindWithTag("CommonSoundEffect").GetComponent<CommonSoundEffect>();
            
            ConnectionHelper.Instance.SetMonoBehaviour(this);
            ConnectionHelper.Instance.AddOnOpenCallback(SocketOnOpen);
            ConnectionHelper.Instance.AddOnErrorCallback(SocketOnError);
            ConnectionHelper.Instance.AddOnCloseCallback(SocketOnClose);
        }

        private void OnDestroy()
        {
            ConnectionHelper.Instance.RemoveOnOpenCallback(SocketOnOpen);
            ConnectionHelper.Instance.RemoveOnErrorCallback(SocketOnError);
            ConnectionHelper.Instance.RemoveOnCloseCallback(SocketOnClose);
        }

        // Update is called once per frame
        protected virtual void Update()
        {
            ConnectionHelper.Instance.DispatchMessageQueue();
        }

        protected virtual void OnApplicationQuit()
        {
            ConnectionHelper.Instance.DisconnectWebSocket();
        }

        protected virtual void SocketOnOpen()
        {
            CommonHelper.Log("SocketOnOpen");
        }

        protected virtual void SocketOnError(string errorMsg)
        {
            CommonHelper.Log("SocketOnError:" + errorMsg);
        }

        protected virtual void SocketOnClose(WebSocketCloseCode closeCode)
        {
            CommonHelper.Log("SocketOnClose:" + closeCode);
        }

        public void PlayButtonClick()
        {
            _commonSoundEffect.PlayButtonClick();
        }

        protected void StopAllSoundEffect()
        {
            _commonSoundEffect.StopAll();
        }
    }
}