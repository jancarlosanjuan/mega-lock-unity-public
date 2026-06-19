using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Text = UnityEngine.UIElements.TextElement;
using Button = UnityEngine.UIElements.Button;
using Tab = UnityEngine.UIElements.Tab;
using MultiColumnList = UnityEngine.UIElements.MultiColumnListView;
using Object = UnityEngine.Object;

namespace MegaLock
{
    public class view_remove_locks : BaseView
    {
        private TextField textField;
        private List<LockData> selectedLocks = new List<LockData>();
        
        protected override void BuildUI()
        {
            RefreshStagingList();
            RefreshCurrentUserLocksList(MegalockPersistence.instance.user_locks);
            RootViewInstance.style.flexGrow = 1;
            RootViewInstance.style.width = new StyleLength(Length.Percent(100));
            RootViewInstance.style.height = new StyleLength(Length.Percent(100));
            
            var refreshButton = RootViewInstance.Q<Button>("refresh-button");
            if (refreshButton != null)
            {
                refreshButton.clicked += HandleRefreshClicked;
            }
            
            var unlockButton = RootViewInstance.Q<Button>("unlock-button");
            if (unlockButton != null)
            {
                unlockButton.clicked += HandleUnlockButtonClicked;
            }
        }

        public void RefreshStagingList()
        {
            var items = selectedLocks;
            
            Func<VisualElement> makeItem = () => new Label();
            
            
            var listView = RootViewInstance.Q<ListView>("staging-list-view");
            
            var callbackCache = new Dictionary<VisualElement, EventCallback<PointerDownEvent>>();
            
            Action<VisualElement, int> bindItem = (e, i) =>
            {
                Label label = (Label)e;
                label.text = MegalockUtilities.HighlightAssetNameFromPath(selectedLocks[i].path);
                
                if (callbackCache.TryGetValue(label, out var old))
                    label.UnregisterCallback<PointerDownEvent>(old);

                EventCallback<PointerDownEvent> callback = evt =>
                {
                    if (evt.button == 1)
                    {
                        evt.StopPropagation();
                        ShowStagingContextMenu(evt.position, i, items, listView);
                    }
                };

                callbackCache[label] = callback;
                label.RegisterCallback<PointerDownEvent>(callback);
            };

            listView.makeItem = makeItem;
            listView.bindItem = bindItem;
            listView.itemsSource = items;
            listView.selectionType = SelectionType.Multiple;
            
            //handle cleaning up because it keeps piling up when UIToolkit reuses the pool element
            listView.unbindItem = (e, i) =>
            {
                Label label = (Label)e;
                if (callbackCache.TryGetValue(label, out var old))
                {
                    label.UnregisterCallback<PointerDownEvent>(old);
                    callbackCache.Remove(label);
                }
            };
        }

        public void RefreshCurrentUserLocksList(List<LockData> projectLocks)
        {
            var listView = RootViewInstance.Q<ListView>("locks-list-view");
            if (listView == null) return;
            
            var callbackCache = new Dictionary<VisualElement, EventCallback<PointerDownEvent>>();
            
            Func<VisualElement> makeItem = () => new Label();
            Action<VisualElement, int> bindItem = (e, i) =>
            {
                Label label = (Label)e;
                if (projectLocks == null || projectLocks.Count == 0 || i >= projectLocks.Count) 
                    return; 
                label.text = MegalockUtilities.HighlightAssetNameFromPath(projectLocks[i].path);
                
                if (callbackCache.TryGetValue(label, out var old))
                    label.UnregisterCallback<PointerDownEvent>(old);
                
                EventCallback<PointerDownEvent> callback = evt =>
                {
                    if (evt.button == 1)
                    {
                        evt.StopPropagation();
                        ShowLockListContextMenu(evt.position, i, projectLocks, listView);
                    }
                };

                callbackCache[label] = callback;
                label.RegisterCallback<PointerDownEvent>(callback);
            };
            
            listView.makeItem = makeItem;
            listView.bindItem = bindItem;
            listView.itemsSource = projectLocks;
            listView.selectionType = SelectionType.Multiple;
            listView.dataSource = MegalockPersistence.instance;
            listView.unbindItem = (e, i) =>
            {
                Label label = (Label)e;
                if (callbackCache.TryGetValue(label, out var old))
                {
                    label.UnregisterCallback<PointerDownEvent>(old);
                    callbackCache.Remove(label);
                }
            };

        }
        
        private void ShowStagingContextMenu(Vector2 position, int index, List<LockData> objects, ListView listView)
        {
            var menu = new GenericDropdownMenu();

            menu.AddItem("Delete", false, () =>
            {
                var selections = listView.selectedIndices.OrderByDescending(i => i);
                foreach (var i in selections)
                {
                    objects.RemoveAt(i);
                }
                listView.Rebuild();
                listView.selectedIndex = -1;
            });

            menu.DropDown(new Rect(position, Vector2.zero), listView, DropdownMenuSizeMode.Auto);
        }
        
        private void ShowLockListContextMenu(Vector2 position, int index, List<LockData> objects, ListView listView)
        {
            var menu = new GenericDropdownMenu();

            menu.AddItem("Add to Staging", false, () =>
            {
                var selections = listView.selectedIndices.OrderByDescending(i => i);
                foreach (var i in selections)
                {
                    if (!selectedLocks.Any(e => e.guid == objects[i].guid))
                    {
                        selectedLocks.Add(objects[i]);
                        RefreshStagingList();
                    }
                    
                    objects.RemoveAt(i);
                }
                listView.Rebuild();
                listView.selectedIndex = -1;
            });

            menu.DropDown(new Rect(position, Vector2.zero), listView, DropdownMenuSizeMode.Auto);
        }
        
        private void HandleRefreshClicked()
        {
            ViewManager.TryRunCoroutine(MegalockAPIController.CallFetchUserLocksApi(MegalockPersistence.instance.currentUserSession, 
                    (res, json) =>
                {
                    if (res)
                    {
                        var rows = JsonConvert.DeserializeObject<List<LockData>>(json);
                        MegalockPersistence.instance.SaveUserLocks(rows);
                        return;
                    }
                }),
                (r) =>
                {
                    RefreshCurrentUserLocksList(MegalockPersistence.instance.user_locks);
                });
        }

        private void HandleUnlockButtonClicked()
        {
            if (selectedLocks.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Unlock Error",                                 
                    "Unlock list is empty.", 
                    "OK"                                              
                );
                return;
            }
            
            DeleteLocksDataList deleteLockDataList = new DeleteLocksDataList(){guids = new List<string>()};
            foreach (var entry in selectedLocks)
            {
                if (string.IsNullOrEmpty(entry.guid))
                {
                    Debug.LogError("Could not find GUID of selected asset");
                    continue;
                }
                
                deleteLockDataList.guids.Add(entry.guid);
            }

            ViewManager.TryRunCoroutine(MegalockAPIController.CallDeleteLocksApi(
                    deleteLockDataList,
                    MegalockPersistence.instance.currentUserSession
                    ,(res, json) =>
                {
                    if (res)
                    {
                       selectedLocks.Clear();
                    }
                    
                }),
                (r) =>
                {
                    RefreshStagingList();
                    HandleRefreshClicked();
                });
        }
        
        public override void OnShow()
        {
            base.OnShow();
            MegalockPersistence.instance.currentTabView = this;
        }
    }
}
