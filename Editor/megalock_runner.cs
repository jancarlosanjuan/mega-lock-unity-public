#if UNITY_EDITOR
using System;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using System.Collections;
using MegaLock;
using UnityEditor;

public static class megalock_runner
{
    private static EditorCoroutine runningCoroutine = null;
    public static bool CanRunCoroutine => runningCoroutine == null;
    
    public static ViewManager viewManagerInstance; //Only show the loading indicator if the window is open.
    
    private static IEnumerator CoroutineWrapper(IEnumerator job, Action<bool> onComplete)//Just a wrapper so Action is imposed in all use cases and we dont forget to clear the running routine.
    {
        viewManagerInstance?.ShowLoading();
        bool success = true;
        while (true)
        {
            object current;
            try
            {
                if (!job.MoveNext()) break;
                current = job.Current;
            }
            catch (Exception e)
            {
                Debug.LogError($"Coroutine failed: {e.Message}");
                success = false;
                break;
            }
            yield return current;
        }

        runningCoroutine = null;
        viewManagerInstance?.HideLoading();
        onComplete?.Invoke(success); 
    }
    
    public static bool TryRunCoroutine(IEnumerator job, Action<bool> onComplete)
    {
        if (runningCoroutine != null)
        {
            Debug.LogWarning("Megalock Runner is busy, ignoring request.");
            return false;
        }

        runningCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless(
            CoroutineWrapper(job, onComplete)
        );
        return true;
    }
    
    
    [InitializeOnLoadMethod]
    public static void CancelCurrentRunningCoroutine()
    {
        if (runningCoroutine == null) return;
        EditorCoroutineUtility.StopCoroutine(runningCoroutine);
        runningCoroutine = null;
        viewManagerInstance?.HideLoading();
    }
    
}

#endif
