using System;
using UnityEditor;
using UnityEngine;

namespace MegaNotify
{
    [Serializable]
    public class EditorNotificationOnClickAction_SampleBehaviour : EditorNotificationOnClickAction
    {
        public override void OnClick()
        {
            Run();
        }
        
        /// <summary>
        /// Could simply define the behaviour in OnClick, but in case the user needs to run this block of logic independent
        /// of the action architecture, then this also works.
        /// </summary>
        public static void Run()
        {
            string header = $"Test Notification {MegaNotifySystem.GetNotificationsCount() + 1}";
            string description =
                $"This test notification was created from another notification's on click behavior! Though clicking on this notification will do nothing...";
            MegaNotifySystem.CreateNotification(header, description);

            EditorWindow gameView =
                EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
            gameView?.Focus();
        }
    }
}