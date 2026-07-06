using UnityEngine;
using UnityEditor;
using System.Linq;

namespace MegaLock
{
    [InitializeOnLoad]
    public class MegalockPathHighlighter
    {
        static MegalockPathHighlighter()
        {
            EditorApplication.projectWindowItemOnGUI -= HandleProjectTabHighlightItem;
            EditorApplication.projectWindowItemOnGUI += HandleProjectTabHighlightItem;
        }
        private static void HandleProjectTabHighlightItem(string guid, Rect selectionrect)
        {
            var locks =
                MegalockPersistence.instance.locksGuidHash;
            if(locks == null) return;
            string currentUserID = MegalockPersistence.instance.currentUserSession.userId;
            if(!locks.TryGetValue(guid, out var lockData)) return;
            
            
            if (lockData.user_id == currentUserID)
            {
                EditorGUI.DrawRect(selectionrect, new Color(0, 1, 0, 0.08f));
                GUI.Label(
                    new Rect(selectionrect.xMax - 16, selectionrect.yMin, 18, 18),
                    EditorGUIUtility.IconContent("d_FilterSelectedOnly")
                );
            }
            else
            {
                EditorGUI.DrawRect(selectionrect, new Color(1, 0, 0, 0.08f));
                GUI.Label(
                    new Rect(selectionrect.xMax - 16, selectionrect.yMin, 18, 18),
                    EditorGUIUtility.IconContent("LockIcon-On"));
            }
        }
    }
}

