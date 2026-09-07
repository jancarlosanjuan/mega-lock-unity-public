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
        
        public ViewManager(VisualElement root, view_loading loadingInstance)
        {
            rootContainer = root;
            this.loadingInstance = loadingInstance?.Initialize(this) as view_loading;
            megalock_runner.viewManagerInstance = this;
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
        
        public void ShowLoading()
        {
            rootContainer.Add(loadingInstance.GetRootViewInstance());
            loadingInstance?.OnShow();
        }

        public void HideLoading()
        {
            rootContainer.Remove(loadingInstance.GetRootViewInstance());
            loadingInstance?.OnHide();
        }

        public void DeinitializeAllViews()
        {
            /*if (runningCoroutine != null)
                EditorCoroutineUtility.StopCoroutine(runningCoroutine);
            runningCoroutine = null;*/
            foreach (var entry in viewTable.Values)
            {
                entry.Deinitialize();
            }
            loadingInstance.Deinitialize();
            megalock_runner.viewManagerInstance = null;
        }
    }
}
#endif
