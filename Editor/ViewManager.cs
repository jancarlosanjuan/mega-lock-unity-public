#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using UnityEngine.UIElements;
using UnityEngine;
using Text = UnityEngine.UIElements.TextElement;

namespace MegaLock
{
    public class ViewManager
    {
        private view_loading loadingInstance;
        private readonly VisualElement rootContainer;
        private readonly Dictionary<Type, BaseView> viewTable = new();
        private BaseView currentView;
        private EditorCoroutine runningCoroutine = null;
        public bool CanRunCoroutine => runningCoroutine == null;
        
        public ViewManager(VisualElement root, view_loading loadingInstance)
        {
            rootContainer = root;
            this.loadingInstance = loadingInstance?.Initialize(this) as view_loading;
        }

        public void OnEditorUpdate(float delta)
        {
            foreach (var element in viewTable)
            {
                element.Value.OnEditorUpdate(delta);
            }
            loadingInstance?.OnEditorUpdate(delta);
        }

        public void RegisterView<T>(T view) where T : BaseView
        {
            viewTable[typeof(T)] = view;
        }

        public void ShowView<T>() where T : BaseView
        {
            if (!viewTable.TryGetValue(typeof(T), out var view))
            {
                Debug.LogError($"View {typeof(T).Name} not registered.");
                return;
            }
            currentView?.OnHide();
            rootContainer.Clear();
            currentView = view;
            rootContainer.Add(view.GetRootViewInstance());
            currentView?.OnShow();
        }

        public void HideView<T>() where T : BaseView
        {
            if (!viewTable.TryGetValue(typeof(T), out var view))
            {
                Debug.LogError($"View {typeof(T).Name} not registered.");
                return;
            }
            currentView?.OnHide();
            rootContainer.Clear();
            currentView = null;
        }

        public T GetView<T>() where T : BaseView =>
            viewTable.TryGetValue(typeof(T), out var view) ? (T)view : null;
        
        private void ShowLoading()
        {
            rootContainer.Add(loadingInstance.GetRootViewInstance());
            loadingInstance?.OnShow();
        }

        private void HideLoading()
        {
            rootContainer.Remove(loadingInstance.GetRootViewInstance());
            loadingInstance?.OnHide();
        }

        public void DeinitializeAllViews()
        {
            if (runningCoroutine != null)
                EditorCoroutineUtility.StopCoroutine(runningCoroutine);
            runningCoroutine = null;
            foreach (var entry in viewTable.Values)
            {
                entry.Deinitialize();
            }
            loadingInstance.Deinitialize();
        }
        
        private IEnumerator CoroutineWrapper(IEnumerator job, Action<bool> onComplete)//Just a wrapper so Action is imposed in all use cases and we dont forget to clear the running routine.
        {
            ShowLoading();
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
            HideLoading();
            onComplete?.Invoke(success); 
        }
        public bool TryRunCoroutine(IEnumerator job, Action<bool> onComplete)
        {
            if (runningCoroutine != null)
            {
                Debug.LogWarning("ViewManager is busy, ignoring request.");
                return false;
            }

            runningCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless(
                CoroutineWrapper(job, onComplete)
            );
            return true;
        }
        public void CancelCurrentRunningCoroutine()
        {
            if (runningCoroutine == null) return;
            EditorCoroutineUtility.StopCoroutine(runningCoroutine);
            runningCoroutine = null;
            HideLoading();
        }
    }
}
#endif
