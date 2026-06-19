using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using System.Collections;
using System;
using System.Text;
using Unity.EditorCoroutines.Editor;

using Text = UnityEngine.UIElements.TextElement;
using Button = UnityEngine.UIElements.Button;

namespace MegaLock
{
    public class megalock : EditorWindow
    {
        private ViewManager viewManager;
        public ViewManager GetViewManager() => viewManager;
        private VisualElement root = null; //Pass this to the view controller. It will handle view switching for us

        public static string SESSION_STATE_LOGIN_KEY = "loggedIn";

        [MenuItem("Window/Mega lock")]
        public static void ShowMegaLockWindow()
        {
            megalock wnd = GetWindow<megalock>();
            wnd.titleContent = new GUIContent("Mega Lock");
        }

        public void CreateGUI()
        {
            root = rootVisualElement;

            root.style.flexGrow = 1;
            root.style.width = new StyleLength(Length.Percent(100));
            root.style.height = new StyleLength(Length.Percent(100));

            viewManager = new ViewManager(root, CreateInstance<view_loading>() as view_loading);
            viewManager.RegisterView(CreateInstance<view_login>().Initialize(viewManager) as view_login);
            viewManager.RegisterView(CreateInstance<view_register>().Initialize(viewManager) as view_register);
            viewManager.RegisterView(CreateInstance<view_main>().Initialize(viewManager) as view_main);

            bool isLoggedIn = SessionState.GetBool(SESSION_STATE_LOGIN_KEY, false);
            if (isLoggedIn)
            {
                viewManager.ShowView<view_main>();
            }
            else
            {
                viewManager.ShowView<view_login>();
            }
        }

        private void Update()
        {
            viewManager?.OnEditorUpdate(Time.deltaTime);
        }

        private void OnDestroy()
        {
            viewManager?.DeinitializeAllViews();
        }
    }
}
