using System;
using System.Collections.Generic;
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
    public class view_register : BaseView
    {
        private TextField  slackIdInputField = null;
        private TextField  passwordInputField = null;
        
        protected override void BuildUI()
        {
            var registerButton = RootViewInstance.Q<Button>("register-button");
              if (registerButton != null)
              {
                  registerButton.clicked += HandleRegisterClicked;
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
              
              var loginButton = RootViewInstance.Q<Button>("login-button");
              if (loginButton != null)
              {
                  loginButton.clicked += () =>
                  {
                      ViewManager.ShowView<view_login>();
                  };
              }
        }
        
        private void HandleRegisterClicked()
        {
            if (!ViewManager.CanRunCoroutine) return;
        
            string userid = slackIdInputField.value;
            string password = passwordInputField.value;

            if (string.IsNullOrEmpty(userid) || string.IsNullOrEmpty(password))
            {
                EditorUtility.DisplayDialog("Register error", "Please enter a valid username and password.", "OK");
                return;
            }

            RegisterData registerData = new RegisterData()
            {
                userid = userid,
                password = password,
            };
            bool registerSuccessful = false;
            ViewManager.TryRunCoroutine(MegalockAPIController.CallRegisterApi(registerData, (res,json) =>
            {
                registerSuccessful = res;
            }),(result) =>
            {
                if (result && registerSuccessful)
                {
                    ViewManager.ShowView<view_login>();
                }
                else
                {
                    //Debug.LogError($"Register failed: {result}. Login failed.");
                }
            });
        }

    }
}

