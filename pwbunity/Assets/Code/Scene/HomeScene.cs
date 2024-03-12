using System.Collections.Generic;
using Code.Base;
using Code.Helper;
using Code.Prefab.Home;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Code.Scene
{
    public class HomeScene : BaseScene
    {
        [SerializeField] private Button buttonTopLeft;
        [SerializeField] private Button buttonTopRight;
        [SerializeField] private Image spriteAvatar;
        [SerializeField] private TMP_Text textName;
        [SerializeField] private TMP_Text textID;
        [SerializeField] private TMP_Text textChip;
        [SerializeField] private Button btnTab1;
        [SerializeField] private Button btnTab2;
        [SerializeField] private Button btnTab3;
        [SerializeField] private Button btnTab4;
        [SerializeField] private Tab1 tab1;
        [SerializeField] private Tab2 tab2;
        [SerializeField] private Tab3 tab3;
        [SerializeField] private Tab4 tab4;

        private GameObject _canvas;

        private string _totalChip = "";

        protected override void Start()
        {
            base.Start();

            _canvas = GameObject.Find("Canvas");

            // UpdateTabs(1);

            buttonTopLeft.onClick.AddListener(Logout);
            buttonTopRight.onClick.AddListener(() =>
                Instantiate(Resources.Load<HomeSubMenu>("Prefabs/Home/HomeSubMenu"), _canvas.transform, false));

            // btnTab1.onClick.AddListener(() => UpdateTabs(1));
            // btnTab2.onClick.AddListener(() => UpdateTabs(2));
            // btnTab3.onClick.AddListener(() => UpdateTabs(3));
            // btnTab4.onClick.AddListener(() => UpdateTabs(4));

            tab1.SetCashOutCompleteCallback(SendGetPlayerLatestData);

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
                    tab1.SendListGames();
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

                    CommonHelper.HideLoading();
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
                        tab1.UpdateChip(_totalChip);

                        var displayName = resp.Result.DisplayName;

                        var targetIndex = new List<string>(Constant.LoginNames).FindIndex(it => string.Equals(it, displayName));
                        if (targetIndex != -1)
                        {
                            spriteAvatar.sprite = Resources.Load<Sprite>("Art/Image/Common/Avatar/" + Constant.LogonAvatars[targetIndex]); 
                        }
                        textName.text = displayName;
                        textID.text = "ID:" + resp.Result.ID;
                        textChip.text = "Chip:" + _totalChip;
                    }
                });
        }

        private static void Logout()
        {
            PlayerPrefs.SetString(Constant.PfKeyUserToken, "");
            SceneManager.LoadScene(nameof(LoginScene));
        }

        // private void UpdateTabs(int enableTab)
        // {
        //     btnTab1.interactable = enableTab != 1;
        //     tab1.gameObject.SetActive(enableTab == 1);
        //
        //     btnTab2.interactable = enableTab != 2;
        //     tab2.gameObject.SetActive(enableTab == 2);
        //
        //     btnTab3.interactable = enableTab != 3;
        //     tab3.gameObject.SetActive(enableTab == 3);
        //
        //     btnTab4.interactable = enableTab != 4;
        //     tab4.gameObject.SetActive(enableTab == 4);
        //
        //     if (enableTab == 1)
        //     {
        //         tab1.ShowGameList(false);
        //     }
        // }
    }
}