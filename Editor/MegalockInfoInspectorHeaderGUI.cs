using System;
using MegaLock;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class MegalockInfoInspectorHeaderGUI
{
    private static object lastSelectedObject = null;
    private static LockData lockData;
    
    static MegalockInfoInspectorHeaderGUI()
    {
        Editor.finishedDefaultHeaderGUI += DisplaySearchBar;
    }

    static void DisplaySearchBar(Editor editor)
    {
        if (!CanRenderInfo())
            return;

        if (CanUpdateLockData())
            UpdateLockData();
        
        RenderInfo();
    }

    static bool CanRenderInfo()
    {
        bool isLoggedIn = SessionState.GetBool(megalock.SESSION_STATE_LOGIN_KEY, false);
        if (!isLoggedIn)
            return false;
        
        var objects = Selection.objects;
        if (objects == null || objects.Length == 0)
            return false;

        if (objects.Length > 1)
            return false;
        
        var currentSelectedObject = Selection.activeObject;
        if (!AssetDatabase.Contains(currentSelectedObject))
            return false;

        var path = AssetDatabase.GetAssetPath(currentSelectedObject.GetEntityId());
        if (path == String.Empty)
            return false;
        
        return !AssetDatabase.IsValidFolder(path);
    }

    static void RenderInfo()
    {
        EditorGUIUtility.LookLikeInspector();
        
        GUIStyle textStyle = GUI.skin.label;
        bool previousWordWrap = textStyle.wordWrap;
        bool previousRichText = textStyle.richText;
        textStyle.wordWrap = true;
        textStyle.richText = true;
        
        bool isLocked = !lockData.path.Equals(String.Empty);
        
        GUILayout.Label($"Megalock File Status: {(isLocked ? "<color=#FF0000>Locked</color>" : "<color=#00FF00>Free</color>")}", textStyle);
        if (isLocked)
        {
            GUILayout.Label($"Locked By: <color=#FFFF00>{lockData.name}</color>", textStyle);
            GUILayout.Label($"Locked Since: <color=#FFFF00>{lockData.lock_duration}</color>", textStyle);
            GUILayout.Label($"Lock Description:\n<color=#FFFF00>{lockData.description}</color>", textStyle);
        }
        
        textStyle.wordWrap = previousWordWrap;
        textStyle.richText = previousRichText;
    }

    static bool CanUpdateLockData()
    {
        var currentSelectedObject = Selection.activeObject;
        if (currentSelectedObject == null)
            return lastSelectedObject != null;
        
        return true;
    }

    static void UpdateLockData()
    {
        var currentSelectedObject = Selection.activeObject;
        if (currentSelectedObject == null)
        {
            lastSelectedObject = null;
            lockData = new LockData { path = string.Empty };
            return;
        }
        
        lastSelectedObject = currentSelectedObject;
        
        var locks = MegalockPersistence.instance.locksGuidHash;
        if (locks == null || locks.Count == 0)
            return;
        
        var guid = MegalockUtilities.SanitizeObjectToUnityGuid(currentSelectedObject);
        if (guid.Equals(String.Empty))
            return;
        
        foreach (var guidToCheck in locks.Keys)
        {
            if (guidToCheck.Equals(String.Empty))
                continue;
            
            if (!guidToCheck.Equals(guid))
                continue;
            
            lockData = locks[guidToCheck];
            return;
        }
        
        lockData = new LockData { path = string.Empty };
    }
}
