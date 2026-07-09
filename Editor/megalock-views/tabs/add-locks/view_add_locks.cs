using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Object = UnityEngine.Object;

namespace MegaLock
{
    public class view_add_locks : BaseView
    {
        private TextField textField;
        private Button refresh_button = null;
        private Button search_button = null;
        private Button clear_button = null;
        private ToolbarSearchField pathInputField = null;
        protected override void BuildUI()
        {
            RootViewInstance.style.flexGrow = 1;
            RootViewInstance.style.width = new StyleLength(Length.Percent(100));
            RootViewInstance.style.height = new StyleLength(Length.Percent(100));
            
            refresh_button = RootViewInstance.Q<Button>("refresh-button");
            if (refresh_button != null)
            {
                refresh_button.clicked += HandleRefreshClicked;
            }
            
            search_button = RootViewInstance.Q<Button>("search-button");
            if (search_button != null)
            {
                search_button.clicked += HandleSearchClicked;
            }
            
            clear_button = RootViewInstance.Q<Button>("clear-button");
            if (clear_button != null)
            {
                clear_button.clicked += HandleClearButtonClicked;
            }
            
            pathInputField = RootViewInstance.Q<ToolbarSearchField>("searchbar-path");
            if (pathInputField == null)
            {
                Debug.LogWarning("No path field selected");
                return;
            }
            pathInputField.value = MegalockPersistence.instance.userLocksSearchField;
            
            
            var lockButton = RootViewInstance.Q<Button>("lock-button");
            if (lockButton != null)
            {
                lockButton.clicked += HandleLockButtonClicked;
            }
            
            textField = RootViewInstance.Q<TextField>("lock-description-field");
            
            RefreshStagingList();
            RefreshCurrentUserLocksList(MegalockPersistence.instance.user_locks);
        }

        private void HandleClearButtonClicked()
        {
            pathInputField.value = String.Empty;
            HandleSearchClicked();
        }

        private void HandleSearchClicked()
        {
            MegalockPersistence.instance.UpdateUserLockSearchStates(pathInputField.value);
            RefreshCurrentUserLocksList(MegalockPersistence.instance.user_locks);
        }

        public void RefreshStagingList()
        {
            var items = MegalockPersistence.instance.selectedObjects;
            
            Func<VisualElement> makeItem = () => new Label();
            
            
            var listView = RootViewInstance.Q<ListView>("staging-list-view");
            
            var callbackCache = new Dictionary<VisualElement, EventCallback<PointerDownEvent>>();
            
            Action<VisualElement, int> bindItem = (e, i) =>
            {
                Label label = (Label)e;
                label.text = MegalockUtilities.HighlightAssetNameFromPath(AssetDatabase.GetAssetPath(items[i]));
                
                //Unregister previous callback to prevent piling up the callback
                if (callbackCache.TryGetValue(label, out var old))
                    label.UnregisterCallback<PointerDownEvent>(old);

                EventCallback<PointerDownEvent> callback = evt =>
                {
                    if (evt.button == 1)
                    {
                        evt.StopPropagation();
                        ShowContextMenu(evt.position, i, items, listView);
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
            
            //Copy pasta-ed from UIToolkit samples lol
            /*listView.itemsChosen += (selectedItems) =>
            {
                Debug.Log("Items chosen:   " + string.Join(", ", selectedItems));
            };

            listView.selectedIndicesChanged += (selectedIndices) =>
            {
                Debug.Log("Index selected: " + string.Join(", ", selectedIndices));

                // Note: selectedIndices can also be used to get the selected items from the itemsSource directly or
                // by using listView.viewController.GetItemForIndex(index).
            };*/
            
            //handleRefreshClicked();
        }

        public void RefreshCurrentUserLocksList(List<LockData> userLocks)
        {
            var listView = RootViewInstance.Q<ListView>("locks-list-view");
            if (listView == null) return;
            
            Func<VisualElement> makeItem = () => new Label();
            Action<VisualElement, int> bindItem = (e, i) =>
            {
                Label label = (Label)e;
                if (userLocks == null || userLocks.Count == 0 || i >= userLocks.Count) 
                    return; 
                label.text = MegalockUtilities.HighlightAssetNameFromPath(userLocks[i].path);
            };
            
            listView.makeItem = makeItem;
            listView.bindItem = bindItem;
            listView.itemsSource = userLocks;
            listView.selectionType = SelectionType.None;
            listView.dataSource = MegalockPersistence.instance;
            
            listView.Rebuild();
            /*listView.itemsChosen += (selectedItems) =>
            {
                Debug.Log("Items chosen:   " + string.Join(", ", selectedItems));
            };

            listView.selectedIndicesChanged += (selectedIndices) =>
            {
                Debug.Log("Index selected: " + string.Join(", ", selectedIndices));
            };*/

        }
        
        private void ShowContextMenu(Vector2 position, int index, List<Object> objects, ListView listView)
        {
            var menu = new GenericDropdownMenu();

            menu.AddItem("Delete", false, () =>
            {
                var selections = listView.selectedIndices.OrderByDescending(i => i);
                foreach (var i in selections)
                    objects.RemoveAt(i);
                listView.Rebuild();
                listView.selectedIndex = -1;
            });

            menu.DropDown(new Rect(position, Vector2.zero), listView, 
#if UNITY_6000_4_OR_NEWER
                DropdownMenuSizeMode.Auto
#else
                false
#endif
                );
            
        }
        
        private void HandleRefreshClicked()
        {
            ViewManager.TryRunCoroutine(MegalockAPIController.CallFetchLocksApi(MegalockPersistence.instance.currentUserSession,
                    (res, json) =>
                {
                    if (res)
                    {
                        var rows = JsonConvert.DeserializeObject<List<LockData>>(json);
                        MegalockPersistence.instance.SaveAll(rows);
                        return;
                    }
                    //Debug.LogError("Failed to fetch locks");
                }),
                (r) =>
                {
                    RefreshCurrentUserLocksList(MegalockPersistence.instance.user_locks.OrderByDescending(entry => entry.path).ToList());
                    //Debug.Log("Finished fetching locks");
                });
        }

        private void HandleLockButtonClicked()
        {
            if (textField == null) return;
            string description = textField.text;
            
            if (MegalockPersistence.instance.selectedObjects.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Add Lock Error",                                 
                    "Lock list is empty.", 
                    "OK"                                              
                );
                return;
            }
            
            if (string.IsNullOrEmpty(description))
            {
                EditorUtility.DisplayDialog(
                    "Add Lock Error",                                 
                    "Description cannot be empty.", 
                    "OK"                                              
                );
                return;
            }
            
            AddLockDataList addLockDataList = new AddLockDataList(){locks = new List<AddLockData>()};
            foreach (var entry in MegalockPersistence.instance.selectedObjects)
            {
                string guidCleaned = MegalockUtilities.GetCleanGuidOfObject(entry);
                if (string.IsNullOrEmpty(guidCleaned))
                {
                    Debug.LogError("Could not find GUID of selected asset");
                    continue;
                }
                var data = new AddLockData
                {
                    path = AssetDatabase.GetAssetPath(entry),
                    description = description,
                    guid = guidCleaned,
                };
                
                addLockDataList.locks.Add(data);
            }

            ViewManager.TryRunCoroutine(MegalockAPIController.CallAddLocksApi(
                    addLockDataList,
                    MegalockPersistence.instance.currentUserSession
                    ,(res, json) =>
                {
                    if (res)
                    {
                        var rows = JsonConvert.DeserializeObject<List<AddLockData>>(json);
                        //Check for diffs in the current selection. Those that were returned were successful so we remove it from the selections.
                        for (int i = MegalockPersistence.instance.selectedObjects.Count - 1; i >= 0; i--)
                        {
                            var selectionGuid =
                                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                                    MegalockPersistence.instance.selectedObjects[i], out var guid, out _);
                            if (!selectionGuid) continue;

                            if (rows.Exists(x => x.guid.Replace("-","") == guid))
                            {
                                MegalockPersistence.instance.selectedObjects.RemoveAt(i);
                            }
                        }
                        
                        textField.value = "";
                    }
                    //Debug.LogError("Failed to fetch locks");
                }),
                (r) =>
                {
                    RefreshStagingList();
                    HandleRefreshClicked();
                    //RefreshCurrentUserLocksList(MegalockPersistence.instance.locks);
                    //Debug.Log("Finished inserting locks");
                });
        }
        
        public override void OnShow()
        {
            base.OnShow();
            //HandleRefreshClicked();
            MegalockPersistence.instance.currentTabView = this;
            pathInputField.value = MegalockPersistence.instance.userLocksSearchField;
            HandleSearchClicked();
        }
    }
}
