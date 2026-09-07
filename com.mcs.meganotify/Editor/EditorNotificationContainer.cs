using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MegaNotify
{
    [Serializable]
    public class EditorNotificationContainer
    {
        public EditorNotification Notification;
        public EditorNotificationInfo Info;
        
        public float StartTime;
        public float Lifetime;
        public int Order;
        
        public EditorNotificationContainer(EditorNotificationInfo info)
        {
            Notification = ScriptableObject.CreateInstance<EditorNotification>();
            Info = info;
            Notification.BindToContainer(this, info);

            var showWithMode =
                typeof(EditorWindow).GetMethod("ShowPopupWithMode", BindingFlags.Instance | BindingFlags.NonPublic);
            var popupMenu = 1;
            if (showWithMode != null)
                showWithMode.Invoke(Notification, new object[] { popupMenu, false });

            StartTime = Time.realtimeSinceStartup;
            
            if (info != null)
            {
                bool hasHeader = info.Header is { Length: > 0 };
                bool hasDescription = info.Description is { Length: > 0 };
                
                if (hasHeader && hasDescription)
                    Debug.Log($"[{Info.Header}] : {Info.Description}");
                else if (hasHeader)
                    Debug.Log($"[{Info.Header}]");
                else if (hasDescription)
                    Debug.Log($"[{Info.Description}]");
            }
        }

        public void OnUpdate(Rect rect)
        {
            float lifespan = MegaNotifySystem.NOTIFICATION_LIFESPAN;
            Lifetime = GetLifetime();
            if (Lifetime >= lifespan)
            {
                MegaNotifySystem.CleanupNotification(this);
                return;
            }

            Notification.position = rect;

            float progress = Lifetime / lifespan;
            Notification.UpdateLifetimeBar(progress);
        }

        public void ReaddToNotificationSystem()
        {
            // Refresh start time to account for domain reload hang
            StartTime = Time.realtimeSinceStartup - Lifetime;
            MegaNotifySystem.ReAddNotification(this);
        }

        public void Close()
        {
            Notification.Close();
        }

        public float GetLifetime()
        {
            return Time.realtimeSinceStartup - StartTime;
        }
    }
}