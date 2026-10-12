using System;
using System.Collections.Generic;
using System.IO;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.UIElements;
using Text = UnityEngine.UIElements.TextElement;
using Button = UnityEngine.UIElements.Button;

namespace MegaLock
{
    public class view_login : BaseView
    {
        private TextField  slackIdInputField = null;
        private TextField  passwordInputField = null;
        
        protected override void BuildUI()
        {
            var loginButton = RootViewInstance.Q<Button>("login");
              if (loginButton != null)
              {
                  //int i = loginButton.parent.IndexOf(loginButton);
                  //loginButton.parent.Insert(i, logText);
                  loginButton.clicked += HandleLoginClicked;
              }

              var slackField = RootViewInstance.Q<TextField>("slackid");
              if (slackField != null)
              {
                  slackIdInputField = slackField;
              }

              var passwordField = RootViewInstance.Q<TextField>("password");
              if (slackField != null)
              {
                  passwordInputField = passwordField;
              }
              
              var registerButton = RootViewInstance.Q<Button>("register-button");
              if (registerButton != null)
              {
                  registerButton.clicked += () =>
                  {
                      ViewManager.ShowView<view_register>();
                  };
              }

        }
        
        //Use any existing credentials (if any) in the local directory. We also validate if the JWT is valid here so the user does not have to login everytime the open Unity
        //This one was the most requested since every user has to grab their slack ID every time. Now, the JWT is valid for 30 days. Cats who have left the studio will also have only 30 days to access the locks
        [InitializeOnLoadMethod]
        private static void DoAutoLogin()
        {
            if (SessionState.GetBool(megalock.SESSION_STATE_LOGIN_KEY, false)) return;
            
            if (!UserSessionController.TryParseLocalCredentials(out UserSession session))
                return;
            
            bool loginSuccessful = false;
            string jsonBody = string.Empty;
            megalock_runner.TryRunCoroutine(MegalockAPIController.CallValidateSessionToken(session, (res,json) =>
            {
                loginSuccessful = res;
                jsonBody = json;
            }),(result) =>
            {
                if (result && loginSuccessful)
                {
                    var obj = JObject.Parse(jsonBody);
                    session.isAdmin =  (bool)obj["isAdmin"];//Update admin info just in case it changed
                    UserSessionController.CreateOrUpdateUserSessionData(session);
                    SessionState.SetBool(megalock.SESSION_STATE_LOGIN_KEY, true);
                    if (EditorWindow.HasOpenInstances<megalock>())
                    {
                        var window = EditorWindow.GetWindow<megalock>();
                        var mainView = window?.GetViewManager()?.GetView<view_main>() as view_main;
                        if(mainView != null)
                            mainView.RefreshAdminTabView();
                        window?.GetViewManager()?.ShowView<view_main>();
                    }
                }
                else
                {
                    Debug.LogError($"Failed to login: {result}. Try Manually logging in");
                }
            });
        }
        
        private void HandleLoginClicked()
        {
            if (!megalock_runner.CanRunCoroutine) return;

            if (!DoPreSignInValidation()) return;
        
            DoManualLogin();
        }
        
        private void DoManualLogin()
        {
            string userid = slackIdInputField.value;
            string password = passwordInputField.value;

            if (string.IsNullOrEmpty(userid) || string.IsNullOrEmpty(password))
            {
                EditorUtility.DisplayDialog("Sign in error", "Please enter a valid username and password.", "OK");
                return;
            }

            LoginData loginData = new LoginData()
            {
                userid = userid,
                password = password,
                projectname = Application.productName,
            };
            bool loginSuccessful = false;
            string jsonBody = string.Empty;
            megalock_runner.TryRunCoroutine(MegalockAPIController.CallLoginApi(loginData, (res,json) =>
            {
                loginSuccessful = res;
                jsonBody = json;
            }),(result) =>
            {
                //Debug.Log($"Job successful? {result} ");
                if (result && loginSuccessful)
                {
                    var obj = JObject.Parse(jsonBody);
                    string accessToken = (string)obj["accessToken"];
                    string projectId = (string)obj["projectid"];
                    bool isAdmin = (bool)obj["isAdmin"];
                    UserSessionController.CreateOrUpdateUserSessionData(new UserSession()
                    {
                        userId = loginData.userid,
                        accessToken = accessToken,
                        projectid = projectId,
                        isAdmin = isAdmin
                    });
                    
                    SessionState.SetBool(megalock.SESSION_STATE_LOGIN_KEY, true);
                    ViewManager.ShowView<view_main>();

                    var v = ViewManager.GetView<view_main>() as view_main;
                    if (v) v.RefreshAdminTabView();
                }
                else
                {
                    //Debug.LogError($"Job failed: {result}. Login failed.");
                }
            });
        }
        
        private bool DoPreSignInValidation()
        {
            string projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../"));
            string pre_commit_hook_path = Path.Combine(projectPath, ".git","hooks", "pre-commit");
            string pre_commit_exe_path = Path.Combine(projectPath, ".git","hooks", "pre-commit-client.exe");
            if (!File.Exists(pre_commit_hook_path))
            {
                EditorUtility.DisplayDialog("Validation Error",
                    "Pre-commit hook file is missing. Please make sure to read installation requirements first. ",
                    "OK");
                return false;
            }
            if (!File.Exists(pre_commit_exe_path))
            {
                EditorUtility.DisplayDialog("Validation Error",
                    "Pre-commit client file is missing. Please make sure to read installation requirements first. ",
                    "OK");
                return false;
            }
            return true;
        }

    }
}

