using System.Collections;
using System.Text;
using Code.Base;
using Code.Helper;
using Code.Model.Auth;
using Code.Model.Base;
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
        [SerializeField] private TMP_Text txtPrintInfo;
        [SerializeField] private TMP_Text txtSelectAccount;
        [SerializeField] private Button btnLogin300;
        [SerializeField] private Button btnLogin301;
        [SerializeField] private Button btnLogin302;
        [SerializeField] private Button btnLogin303;
        [SerializeField] private Button btnLogin304;
        [SerializeField] private Button btnLogin305;
        [SerializeField] private Button btnLogin306;
        [SerializeField] private Button btnLogin307;
        [SerializeField] private Button btnLogin308;
        [SerializeField] private Button btnLogin309;
        [SerializeField] private Button btnLogin310;

        private GameObject _canvas;
        
        protected override void Start()
        {
            base.Start();
            
            _canvas = GameObject.Find("Canvas");

            var existToken = PlayerPrefs.GetString(Constant.PfKeyUserToken);
            if (!string.IsNullOrEmpty(existToken))
            {
                SetButtonsVisibility(false);

                txtPrintInfo.text = "Token found, start authenticate";

                if (ConnectionHelper.Instance.IsConnected())
                {
                    txtPrintInfo.text = "Socket already connected";
                    StartCoroutine(SendAuthenticate());
                }
                else
                {
                    ConnectionHelper.Instance.ConnectWebSocket();
                }
            }

            btnLogin300.onClick.AddListener(() => { StartLoginFlow("+886912000300"); });
            btnLogin301.onClick.AddListener(() => { StartLoginFlow("+886912000301"); });
            btnLogin302.onClick.AddListener(() => { StartLoginFlow("+886912000302"); });
            btnLogin303.onClick.AddListener(() => { StartLoginFlow("+886912000303"); });
            btnLogin304.onClick.AddListener(() => { StartLoginFlow("+886912000304"); });
            btnLogin305.onClick.AddListener(() => { StartLoginFlow("+886912000305"); });
            btnLogin306.onClick.AddListener(() => { StartLoginFlow("+886912000306"); });
            btnLogin307.onClick.AddListener(() => { StartLoginFlow("+886912000307"); });
            btnLogin308.onClick.AddListener(() => { StartLoginFlow("+886912000308"); });
            btnLogin309.onClick.AddListener(() => { StartLoginFlow("+886912000309"); });
            btnLogin310.onClick.AddListener(() => { StartLoginFlow("+886912000310"); });
        }

        protected override void SocketOnOpen()
        {
            base.SocketOnOpen();

            txtPrintInfo.text = "SocketOnOpen";
            StartCoroutine(SendAuthenticate());
        }

        protected override void SocketOnError(string errorMsg)
        {
            base.SocketOnError(errorMsg);

            txtPrintInfo.text = "SocketOnError:" + errorMsg;
            SetButtonsVisibility(true);
        }

        protected override void SocketOnClose(WebSocketCloseCode closeCode)
        {
            base.SocketOnClose(closeCode);

            txtPrintInfo.text = "SocketOnClose:" + closeCode;
            SetButtonsVisibility(true);
        }

        private void SetButtonsVisibility(bool visible)
        {
            txtSelectAccount.gameObject.SetActive(visible);
            btnLogin300.gameObject.SetActive(visible);
            btnLogin301.gameObject.SetActive(visible);
            btnLogin302.gameObject.SetActive(visible);
            btnLogin303.gameObject.SetActive(visible);
            btnLogin304.gameObject.SetActive(visible);
            btnLogin305.gameObject.SetActive(visible);
            btnLogin306.gameObject.SetActive(visible);
            btnLogin307.gameObject.SetActive(visible);
            btnLogin308.gameObject.SetActive(visible);
            btnLogin309.gameObject.SetActive(visible);
            btnLogin310.gameObject.SetActive(visible);
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
                        txtPrintInfo.text = "Authenticate Success\nEnter Home scene now...";
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

            txtPrintInfo.text = "login for " + phone;
            StartCoroutine(AuthLoginCodeSend(phone, GameObject.Find("Canvas")));
        }

        private IEnumerator AuthLoginCodeSend(string phone, GameObject canvas)
        {
            CommonHelper.ShowLoading(canvas);
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
                    StartCoroutine(resp.Data.IsSignUp ? AuthSignup(phone, canvas) : AuthLoginCodeVerify(phone, canvas));
                }
                else
                {
                    ErrorOccur("IsSuccess is false");
                }
            }
            else
            {
                txtPrintInfo.text = "Result:" + unityWebRequest.result;
            }
        }

        private IEnumerator AuthSignup(string phone, GameObject canvas)
        {
            CommonHelper.ShowLoading(canvas);
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
                txtPrintInfo.text = unityWebRequest.downloadHandler.text;
            }
            else
            {
                txtPrintInfo.text = "Result:" + unityWebRequest.result;
            }
        }

        private IEnumerator AuthLoginCodeVerify(string phone, GameObject canvas)
        {
            CommonHelper.ShowLoading(canvas);
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
                txtPrintInfo.text = phone + " Login Success!\nToken: " + resp.Data.Token;

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
            txtPrintInfo.text = errorMessage;
            commonSoundEffect.PlayError();
        }
    }
}