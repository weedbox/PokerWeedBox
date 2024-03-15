using System.Collections;
using System.Collections.Generic;
using System.Text;
using Code.Base;
using Code.Helper;
using Code.Model.Auth;
using Code.Model.Base;
using Code.Model.ClientAgent;
using Code.Prefab.Login;
using NativeWebSocket;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Code.Scene
{
    public class LoginScene : BaseScene
    {
        [SerializeField] private LoginAccount[] loginAccounts = new LoginAccount[9];
        [SerializeField] private Button btnLogin;
        [SerializeField] private TMP_Text textLoginAccount;

        private GameObject _canvas;

        private int _selectAccountIndex = -1;

        protected override void Start()
        {
            base.Start();

            _canvas = GameObject.Find("Canvas");

            var existToken = PlayerPrefs.GetString(Constant.PfKeyUserToken);
            if (!string.IsNullOrEmpty(existToken))
            {
                SetButtonsVisibility(false);

                CommonHelper.Log("Token found, start authenticate");

                if (ConnectionHelper.Instance.IsConnected())
                {
                    CommonHelper.Log("Socket already connected");
                    StartCoroutine(SendAuthenticate());
                }
                else
                {
                    ConnectionHelper.Instance.ConnectWebSocket();
                }
            }

            for (var index = 0; index < loginAccounts.Length; index++)
            {
                loginAccounts[index].Setup(index, selectIndex =>
                {
                    _selectAccountIndex = selectIndex;
                    foreach (var item in loginAccounts)
                    {
                        item.UpdateSelectStatus(_selectAccountIndex);
                    }
                    btnLogin.interactable = true;
                    textLoginAccount.text = ("LOG IN WITH " + Constant.LoginNames[_selectAccountIndex]).ToUpper();
                });
            }
            
            btnLogin.interactable = false;
            btnLogin.onClick.AddListener(() => StartLoginFlow(Constant.LoginPhones[_selectAccountIndex]));
        }

        protected override void SocketOnOpen()
        {
            base.SocketOnOpen();

            CommonHelper.Log("SocketOnOpen");
            StartCoroutine(SendAuthenticate());
        }

        protected override void SocketOnError(string errorMsg)
        {
            base.SocketOnError(errorMsg);

            ErrorOccur("SocketOnError:" + errorMsg);
            SetButtonsVisibility(true);
        }

        protected override void SocketOnClose(WebSocketCloseCode closeCode)
        {
            base.SocketOnClose(closeCode);

            ErrorOccur("SocketOnClose:" + closeCode);
            SetButtonsVisibility(true);
        }

        private void SetButtonsVisibility(bool visible)
        {
            foreach (var item in loginAccounts)
            {
                item.SetClickable(visible);
            }
        }

        private IEnumerator SendAuthenticate()
        {
            CommonHelper.ShowLoading(_canvas);

            yield return new WaitForSeconds(0.5f);

            ConnectionHelper.Instance.SendAuthenticate(
                PlayerPrefs.GetString(Constant.PfKeyUserToken),
                resp =>
                {
                    CommonHelper.HideLoading();

                    if (resp.Error != null)
                    {
                        ErrorOccur("Error. [Code]" + resp.Error.Code + ", [Message]" + resp.Error.Message);
                        SetButtonsVisibility(true);
                    }
                    else
                    {
                        CommonHelper.Log("Authenticate Success\nEnter Home scene now...");
                        StartCoroutine(EnterHome(0.5f));
                    }
                });
        }

        private static IEnumerator EnterHome(float delay)
        {
            yield return new WaitForSeconds(delay);
            SceneManager.LoadScene(nameof(HomeScene));
        }

        private void StartLoginFlow(string phone)
        {
            PlayerPrefs.SetString(Constant.PfKeyUserToken, "");

            CommonHelper.Log("login for " + phone);
            
            SetButtonsVisibility(false);
            btnLogin.interactable = false;
            StartCoroutine(CheckLoginStatus(phone));
        }

        private IEnumerator CheckLoginStatus(string phone)
        {
            CommonHelper.ShowLoading(_canvas);
            const string api = "client-agent/online-players";
            
            var url = $"{Constant.ServerURL.Replace("/api", "")}/{api}";
            
            var json = JsonConvert.SerializeObject(new ReqCheckLoginStatus { Phones = new List<string> { phone } });

            var unityWebRequest = UnityWebRequest.Post(url, json, "application/json;charset=utf-8");
            unityWebRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            
            yield return unityWebRequest.SendWebRequest();
            CommonHelper.HideLoading();
            if (unityWebRequest.result == UnityWebRequest.Result.Success)
            {
                var resp = JsonConvert.DeserializeObject<RespCheckLoginStatus>(unityWebRequest.downloadHandler.text);
                if (resp.OnlineStates.ContainsKey(phone))
                {
                    resp.OnlineStates.TryGetValue(phone, out var isOnline);
                    if (isOnline)
                    {
                        CommonHelper.ShowCommonDialog(
                            canvas: _canvas,
                            title: "Warring",
                            message: "This account has already logged at another device, continue to login?",
                            positive: "Continue",
                            positiveCallback: () => StartCoroutine(AuthLoginCodeSend(phone)),
                            negative: "Cancel",
                            negativeCallback: () =>
                            {
                                SetButtonsVisibility(true);
                                btnLogin.interactable = true;  
                            }
                        );
                    }
                    else
                    {
                        StartCoroutine(AuthLoginCodeSend(phone));
                    }
                }
                else
                {
                    CommonHelper.LogError("phone not found");
                    StartCoroutine(AuthLoginCodeSend(phone));
                }
            }
            else
            {
                CommonHelper.ShowErrorDialog(_canvas, "Result:" + unityWebRequest.result,
                    () => StartCoroutine(AuthLoginCodeSend(phone)));
            }
        }

        private IEnumerator AuthLoginCodeSend(string phone)
        {
            CommonHelper.ShowLoading(_canvas);
            const string api = "auth/ge/login-codes/send";
            var url = $"{Constant.ServerURL}/{api}";

            var json = JsonConvert.SerializeObject(new ReqLoginCodeSend
            {
                Phone = phone,
                Language = "zh-TW"
            });

            var unityWebRequest = UnityWebRequest.Put(url, json);
            unityWebRequest.SetRequestHeader("Content-Type", "application/json;charset=utf-8");

            yield return unityWebRequest.SendWebRequest();
            CommonHelper.HideLoading();
            if (unityWebRequest.result == UnityWebRequest.Result.Success)
            {
                var resp = JsonConvert.DeserializeObject<BaseResponse<RespLoginCodeSend>>(unityWebRequest
                    .downloadHandler.text);
                if (resp.Data.IsSuccess)
                {
                    StartCoroutine(resp.Data.IsSignUp ? AuthSignup(phone) : AuthLoginCodeVerify(phone));
                }
                else
                {
                    ErrorOccur("IsSuccess is false");
                }
            }
            else
            {
                CommonHelper.Log("Result:" + unityWebRequest.result);
            }
        }

        private IEnumerator AuthSignup(string phone)
        {
            CommonHelper.ShowLoading(_canvas);
            const string api = "auth/ge/sign-up";
            var url = $"{Constant.ServerURL}/{api}";

            var json = JsonConvert.SerializeObject(new ReqSignUp
            {
                Phone = phone
            });

            var unityWebRequest = UnityWebRequest.Post(url, json, "application/json;charset=utf-8");
            unityWebRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));

            yield return unityWebRequest.SendWebRequest();
            CommonHelper.HideLoading();
            if (unityWebRequest.result == UnityWebRequest.Result.Success)
            {
                CommonHelper.Log(unityWebRequest.downloadHandler.text);
            }
            else
            {
                CommonHelper.Log("Result:" + unityWebRequest.result);
            }
        }

        private IEnumerator AuthLoginCodeVerify(string phone)
        {
            CommonHelper.ShowLoading(_canvas);
            const string api = "auth/ge/verify/logincode";
            var url = $"{Constant.ServerURL}/{api}";

            var json = JsonConvert.SerializeObject(new ReqVerifyLoginCode
            {
                Phone = phone,
                // ReSharper disable once StringLiteralTypo
                Code = "BOTTOB"
            });

            var unityWebRequest = UnityWebRequest.Post(url, json, "application/json;charset=utf-8");
            unityWebRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));

            yield return unityWebRequest.SendWebRequest();
            CommonHelper.HideLoading();
            if (unityWebRequest.result == UnityWebRequest.Result.Success)
            {
                CommonHelper.Log(unityWebRequest.downloadHandler.text);
                var resp = JsonConvert.DeserializeObject<BaseResponse<RespVerifyLoginCode>>(unityWebRequest
                    .downloadHandler.text);
                CommonHelper.Log(phone + " Login Success!\nToken: " + resp.Data.Token);

                PlayerPrefs.SetString(Constant.PfKeyUserToken, resp.Data.Token);
                StartCoroutine(EnterHome(0f));
            }
            else
            {
                ErrorOccur("Error:" + unityWebRequest.downloadHandler.text);
            }
        }

        private void ErrorOccur(string errorMessage)
        {
            CommonHelper.LogError(errorMessage);
            commonSoundEffect.PlayError();
        }
    }
}