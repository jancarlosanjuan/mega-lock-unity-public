using System;
using UnityEditor;

namespace MegaNotify
{
    public static class EditorNotificationSample
    {
        [MenuItem("Examples/Create Sample Editor Notification")]
        public static void CreateSampleEditorNotification()
        {
            string header = $"Test Notification {MegaNotifySystem.GetNotificationsCount() + 1}";
            //string description = $"This is a test notification which was spawned in as notification number {EditorNotificationSystem.GetNotificationsCount() + 1}!";
            string description = $"Lorem ipsum dolor sit amet. Est quod voluptate ut placeat voluptatibus aut culpa omnis in quis impedit nam ratione fugit. Nam similique consequatur quo omnis quae qui veniam rerum. Et facere necessitatibus est nesciunt unde et assumenda omnis et dolorum neque est earum soluta ea omnis officiis quo minus voluptas.";

            var onClick = new EditorNotificationOnClickAction_SampleBehaviour(); // Uncomment for persistent version
            //Action onClick = EditorNotificationOnClickAction_SampleBehaviour.Run; // Uncomment for transient version
            
            MegaNotifySystem.CreateNotification(header, description, onClick);
        }
    }
}
