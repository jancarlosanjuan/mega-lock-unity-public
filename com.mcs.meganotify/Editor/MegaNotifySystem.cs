using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MegaNotify
{
    [InitializeOnLoad]
    public class MegaNotifySystem
    {
        private static VisualTreeAsset notificationVisualTree;
        public static VisualTreeAsset NotificationVisualTree
        {
            get
            {
                if (notificationVisualTree == null)
                    notificationVisualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Packages/com.mcs.megalock/com.mcs.meganotify/Editor/Views/EditorNotificationVisualTree.uxml");
                return notificationVisualTree;
            }
        }
        
        private static Vector2 WINDOW_SIZE = new Vector2(300, 100);
        private static Vector2 WINDOW_OFFSET = new Vector2(0, -10);

        public static float NOTIFICATION_LIFESPAN = 10.0f;
        
        private static List<EditorNotificationContainer> activeNotifications = new();
        
        public static float heightOffset = 0.0f;
        
        public static bool reorderNotifications = true;
        
        static MegaNotifySystem()
        {
            
        }

        public static void CreateNotification(string header, string description, Action onClickAction)
        {
            EditorNotificationOnClickAction action = null;
            if (onClickAction != null)
                action = new EditorNotificationOnClickAction_Transient(onClickAction);
            
            CreateNotification(header, description, action);
        }
        
        public static void CreateNotification(string header, string description, EditorNotificationOnClickAction onClickAction = null)
        {
            var info = new EditorNotificationInfo
            {
                Header = header,
                Description = description,
                OnClickAction = onClickAction,
            };
            
            CreateNotification(info);
        }

        public static void CreateNotification(EditorNotificationInfo info)
        {
            AddNotification(new EditorNotificationContainer(info));
        }

        public static void AddNotification(EditorNotificationContainer container)
        {
            container.Order = activeNotifications.Count;
            activeNotifications.Add(container);
            
            if (activeNotifications.Count == 1)
                EditorApplication.update += OnUpdate;
        }

        public static void ReAddNotification(EditorNotificationContainer container)
        {
            activeNotifications.Add(container);
            
            if (activeNotifications.Count == 1)
                EditorApplication.update += OnUpdate;
            
            reorderNotifications = true;
        }
        
        public static void CleanupNotification(EditorNotification notification)
        {
            CleanupNotification(notification.Container);
        }

        public static void CleanupNotification(EditorNotificationContainer notification)
        {
            if (activeNotifications.Contains(notification))
            {
                activeNotifications.Remove(notification);

                if (activeNotifications.Count == 0)
                    EditorApplication.update -= OnUpdate;
                else
                    for (int i = 0; i < activeNotifications.Count; i++)
                        activeNotifications[i].Order = i;
            }
            
            notification.Close();
        }
        
        private static void OnUpdate()
        {
            if (reorderNotifications)
            {
                ReorderNotifications();
                reorderNotifications = false;
            }
            
            for (int i = activeNotifications.Count - 1; i >= 0; i--)
            {
                var notif = activeNotifications[i];
                if (notif == null)
                    continue;
                
                notif.OnUpdate(GetRectOfActiveNotification(i));
            }
        }

        private static void ReorderNotifications()
        {
            activeNotifications = activeNotifications.OrderBy(n => n.Order).ToList();
        }
        
        static Rect GetRectOfActiveNotification(int index)
        {
            Rect mainWindowRect = EditorGUIUtility.GetMainWindowPosition();
            float targetWidth = mainWindowRect.width - WINDOW_SIZE.x + WINDOW_OFFSET.x;
            float targetHeight = mainWindowRect.height + (-WINDOW_SIZE.y + WINDOW_OFFSET.y) * (index + 1);
            Vector2 position = new Vector2(mainWindowRect.position.x + targetWidth, mainWindowRect.position.y + targetHeight);
            return new Rect(position.x, position.y, WINDOW_SIZE.x, WINDOW_SIZE.y);
        }

        public static TemplateContainer CreateEditorNotificationInstance()
        {
            if (NotificationVisualTree == null)
            {
                Debug.LogError("Could not load notification...");
                return null;
            }
            
            return NotificationVisualTree.Instantiate();
        }

        public static int GetNotificationsCount()
        {
            return activeNotifications?.Count ?? 0;
        }
    }
}