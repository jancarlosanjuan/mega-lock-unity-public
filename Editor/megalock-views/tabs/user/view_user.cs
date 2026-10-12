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

namespace  MegaLock
{
    public class view_user : BaseView
    {
        private Button logoutButton = null;
        
        protected override void BuildUI()
        {
            RootViewInstance.style.flexGrow = 1;
            RootViewInstance.style.width = new StyleLength(Length.Percent(100));
            RootViewInstance.style.height = new StyleLength(Length.Percent(100));
            
            
            logoutButton = RootViewInstance.Q<Button>("logout-button");
            if (logoutButton != null)
            {
                logoutButton.clicked += HandleLogoutButton;
            }
        }

        private void HandleLogoutButton()
        {
            var result = EditorUtility.DisplayDialog("Warning!", "Logging out will delete credentials locally! Commiting at Git will automatically proceed without checks. \n" +
                                                    "Be sure to login again. ", "Logout and Close window" , "Cancel");
            if (result)
            {
                UserSessionController.DeleteUserSessionData();
                EditorWindow.GetWindow<megalock>("Mega Lock")?.Close();
            }
            
        }
        
    }
}

