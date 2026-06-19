using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Text = UnityEngine.UIElements.TextElement;
using Button = UnityEngine.UIElements.Button;
namespace MegaLock
{
    public class view_admin : BaseView
    {
        private TextField textField;
        private List<LockData> selectedLocks = new List<LockData>();
        private MultiColumnListView multiColumnListView = null;
        [SerializeField] private VisualTreeAsset cellTemplate = null;
        protected override void BuildUI()
        {
            
            multiColumnListView = RootViewInstance.Q<MultiColumnListView>("locks-list-view");
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
            
            RefreshStagingList();
            RefreshProjectLockList(MegalockPersistence.instance.locks);
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

        public void RefreshProjectLockList(List<LockData> allLocks)
        {
            if (multiColumnListView == null)
            {
                Debug.LogWarning("Multi-column list view is null");
                return;
            }
            if (allLocks == null || allLocks.Count == 0)
            {
                multiColumnListView.columns.Clear();
                multiColumnListView.dataSource = MegalockPersistence.instance;
                multiColumnListView.itemsSource = null;
                multiColumnListView.Rebuild();
                return;
            }
            
            var callbackCache = new Dictionary<VisualElement, EventCallback<PointerDownEvent>>();
            
            multiColumnListView.showBoundCollectionSize = false;
            multiColumnListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            multiColumnListView.itemsSource = null; 
            multiColumnListView.columns.Clear();
            multiColumnListView.dataSource = MegalockPersistence.instance;
            multiColumnListView.itemsSource = allLocks.Count == 0 ? null : allLocks;
            multiColumnListView.selectionType = SelectionType.Multiple;
            
            CreateColumnAndCell("path",allLocks, "Path", (index) => MegalockUtilities.HighlightAssetNameFromPath(allLocks[index].path));
            CreateColumnAndCell("name",allLocks, "Owner", (index) => allLocks[index].name);
            CreateColumnAndCell("description",allLocks, "Description", (index) => string.IsNullOrEmpty(allLocks[index].description) ? "-------" : allLocks[index].description);
            CreateColumnAndCell("lock_duration",allLocks, "Locked Since", (index) => allLocks[index].lock_duration);
            
            multiColumnListView.Rebuild();
        }
        
        private void CreateColumnAndCell(string columnName,List<LockData> allLocks, string columnLabel, Func<int,string> GetStringFromIndex) //"id", "ID", dataList[rowIndex].id.ToString()
        {
            multiColumnListView.columns.Add(new Column()
            {
                name = columnName,
                title = columnLabel,
                stretchable = true,
            });
            
            var col = multiColumnListView.columns[columnName];
            col.makeCell = () => cellTemplate.CloneTree();
            col.bindCell = (VisualElement cell, int rowIndex) =>
            {
                var label = cell.Q<Text>("cell-text");
                if (label != null)
                {
                    label.text = GetStringFromIndex?.Invoke(rowIndex);
                    label.pickingMode = PickingMode.Ignore;
                }
                
                EventCallback<PointerDownEvent> callback = evt =>
                {
                    if (evt.button == 1)
                    {
                        evt.StopPropagation();
                        ShowLockListContextMenu(evt.position, rowIndex, allLocks, multiColumnListView);
                    }
                };
                
                cell.RegisterCallback<PointerDownEvent>(callback);
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

            menu.DropDown(new Rect(position, Vector2.zero), listView, 
#if UNITY_6000_4_OR_NEWER
                DropdownMenuSizeMode.Auto
#else
                false
#endif
            );
        }
        
        private void ShowLockListContextMenu(Vector2 position, int index, List<LockData> objects, MultiColumnListView listView)
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
                    RefreshProjectLockList(MegalockPersistence.instance.locks);
                    //Debug.Log("Finished fetching locks");
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
            
            ViewManager.TryRunCoroutine(MegalockAPIController.CallAdminDeleteLocksApi(
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
