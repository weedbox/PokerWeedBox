using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Code.Model;
using Code.Prefab.Common;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace Code.Helper
{
    public abstract class CommonHelper
    {
        public static void Log(string message)
        {
            Log(message, LogType.Log);
        }

        public static void LogWarning(string message)
        {
            Log(message, LogType.Warning);
        }

        public static void LogError(string message)
        {
            Log(message, LogType.Error);
        }

        private static void Log(string message, LogType logType)
        {
            var currentTime = DateTime.Now.ToString("HH:mm:ss:fff");
            Debug.unityLogger.Log(logType, currentTime + " " + message);
        }

        // ReSharper disable once UnusedMember.Global
        public static void DownImageFromUrl(MonoBehaviour monoBehaviour, string avatarUrl,
            UnityAction<Sprite> successCallback, UnityAction<string> failCallback)
        {
            monoBehaviour.StartCoroutine(DownImageFromUrl(avatarUrl, successCallback, failCallback));
        }

        private static IEnumerator DownImageFromUrl(string avatarUrl, UnityAction<Sprite> successCallback,
            UnityAction<string> failCallback)
        {
            var request = UnityWebRequestTexture.GetTexture(avatarUrl);
            yield return request.SendWebRequest();
            if (request.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
            {
                failCallback?.Invoke("set avatar failed, url:" + avatarUrl);
            }
            else
            {
                var texture = DownloadHandlerTexture.GetContent(request);

                var rect = new Rect(0, 0, texture.width, texture.height);
                var sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));

                successCallback?.Invoke(sprite);
            }
        }

        public static bool IsAvailableActionType([CanBeNull] IEnumerable<string> allowedActions)
        {
            return allowedActions != null && allowedActions.Any(IsAvailableActionType);
        }

        private static bool IsAvailableActionType(string type)
        {
            switch (type)
            {
                case Constant.GameStatusPlayerAction.Fold:
                case Constant.GameStatusPlayerAction.Call:
                case Constant.GameStatusPlayerAction.Check:
                case Constant.GameStatusPlayerAction.Bet:
                case Constant.GameStatusPlayerAction.Raise:
                case Constant.GameStatusPlayerAction.Allin:
                    return true;

                default:
                    return false;
            }
        }

        public static string SubString(string inputString, int length)
        {
            var caret = new char[length * 2 + 1];
            var orgSs = inputString.ToCharArray();

            var n = 0;
            var m = 0;
            for (var i = 0; i < inputString.Length; i++)
            {
                int iChar = orgSs[i];
                if (iChar < 255) n++;
                else
                {
                    n += 2;
                }

                caret[m] = (char)iChar;
                m++;
                if (n > length * 2) break;
            }

            if (n <= length * 2) return new string(caret);
            m--;
            caret[m] = (char)0;

            // return Regex.Replace(new string(caret), @"\s", ""); //去除空格等
            return new string(caret);
        }

        #region Sound

        private static float _volumeBGM;
        private static float _volumeSoundEffect;

        public static void UpdateVolume()
        {
            _volumeBGM = PlayerPrefs.GetFloat(Constant.PfKeyBGMVolume, 1f);
            _volumeSoundEffect = PlayerPrefs.GetFloat(Constant.PfKeySoundEffectVolume, 1f);
        }

        public static float GetBGMVolume()
        {
            return _volumeBGM;
        }

        public static float GetSoundEffectVolume()
        {
            return _volumeSoundEffect;
        }

        #endregion

        #region Loading UI

        private static GameObject _loading;

        public static void ShowLoading([CanBeNull] GameObject canvas)
        {
            HideLoading();
            if (!_loading)
            {
                _loading = Resources.Load<GameObject>("Prefabs/Common/Loading");
            }

            if (_loading && canvas)
            {
                _loading = Object.Instantiate(_loading, canvas.transform, false);
            }
        }

        public static void HideLoading()
        {
            if (_loading)
            {
                Object.Destroy(_loading);
            }
        }

        private static GameObject _disconnect;

        public static void ShowDisconnect([CanBeNull] GameObject canvas)
        {
            HideDisconnect();
            if (!_disconnect)
            {
                _disconnect = Resources.Load<GameObject>("Prefabs/Common/Disconnected");
            }

            if (_disconnect && canvas)
            {
                _disconnect = Object.Instantiate(_disconnect, canvas.transform, false);
            }
        }

        public static void HideDisconnect()
        {
            if (_disconnect)
            {
                Object.Destroy(_disconnect);
            }
        }

        #endregion

        public static void ShowCommonDialog(
            [CanBeNull] GameObject canvas, [CanBeNull] string title, string message,
            [CanBeNull] string positive = null, [CanBeNull] UnityAction positiveCallback = null,
            [CanBeNull] string negative = null, [CanBeNull] UnityAction negativeCallback = null,
            bool playErrorSound = false
        )
        {
            var commonDialog = Resources.Load<CommonDialog>("Prefabs/Common/CommonDialog");

            if (!commonDialog || !canvas) return;

            commonDialog.SetValues(title, message, positive, negative);
            commonDialog = Object.Instantiate(commonDialog, canvas.transform, false);
            commonDialog.SetCallbacks(() =>
            {
                Object.Destroy(commonDialog.gameObject);
                positiveCallback?.Invoke();
            }, () =>
            {
                Object.Destroy(commonDialog.gameObject);
                negativeCallback?.Invoke();
            });
            commonDialog.SetPlayerSound(playErrorSound);
            commonDialog.ScrollContentToTop();
        }

        public static bool CheckResponseIsSuccess([CanBeNull] GameObject canvas, string method, [CanBeNull] Error error,
            [CanBeNull] UnityAction buttonClicked = null, bool showErrorDialog = true)
        {
            if (error == null) return true;

            if (!showErrorDialog) return false;
            
            ShowCommonDialog(
                message: method + "\n" + "Code:" + error.Code + ", Message:" + error.Message,
                canvas: canvas,
                title: "Error Occur",
                positive: "Ok",
                positiveCallback: buttonClicked,
                playErrorSound: true);
            
            return false;
        }
    }
}