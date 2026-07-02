using System;
using System.Collections.Generic;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;
using Text = UnityEngine.UIElements.TextElement;
using Button = UnityEngine.UIElements.Button;
using InputField = UnityEngine.UIElements.TextField;
using Tab = UnityEngine.UIElements.Tab;
using MultiColumnList = UnityEngine.UIElements.MultiColumnListView;

namespace MegaLock
{
    public class view_locks : BaseView
    {
        private Button refresh_button = null;
        private Button search_button = null;
        private Button clear_button = null;
        
        private ToolbarSearchField pathInputField = null;
        private ToolbarSearchField ownerInputField = null;
        private ToolbarSearchField descriptionInputField = null;
        
        private MultiColumnListView multiColumnListView = null;
        [SerializeField] private VisualTreeAsset cellTemplate = null;
        protected override void BuildUI()
        {
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
            pathInputField.value = MegalockPersistence.instance.searchFields.assetPath;
            
            ownerInputField = RootViewInstance.Q<ToolbarSearchField>("searchbar-owner");
            if (ownerInputField == null)
            {
                Debug.LogWarning("No owner field selected");
                return;
            }
            ownerInputField.value = MegalockPersistence.instance.searchFields.ownerName;
            
            descriptionInputField = RootViewInstance.Q<ToolbarSearchField>("searchbar-description");
            if (descriptionInputField == null)
            {
                Debug.LogWarning("No description field selected");
                return;
            }
            descriptionInputField.value = MegalockPersistence.instance.searchFields.description;
            
            multiColumnListView = RootViewInstance.Q<MultiColumnListView>("locks-list");
            RefreshMultiColumnListView(MegalockPersistence.instance.project_locks);
        }

        private void HandleClearButtonClicked()
        {
            pathInputField.value = String.Empty;
            descriptionInputField.value = String.Empty;
            ownerInputField.value = String.Empty;
            
            HandleSearchClicked();
        }

        private void HandleSearchClicked()
        {
            MegalockPersistence.instance.UpdateProjectLockSearchStates(new SearchFields()
            {
                assetPath = pathInputField.value,
                description = descriptionInputField.value,
                ownerName = ownerInputField.value
            });
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
                    HandleSearchClicked();
                    return;
                }
            }),
            (r) =>
            {
                RefreshMultiColumnListView(MegalockPersistence.instance.project_locks);
            });
        }

        private void RefreshMultiColumnListView(List<LockData> locks)
        {
            if (multiColumnListView == null)
            {
                Debug.LogWarning("Multi-column list view is null ");
                return;
            }
            if (locks == null || locks.Count == 0)
            {
                multiColumnListView.columns.Clear();
                multiColumnListView.dataSource = MegalockPersistence.instance;
                multiColumnListView.itemsSource = null;
                multiColumnListView.Rebuild();
                return;
            }
            multiColumnListView.showBoundCollectionSize = false;
            multiColumnListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            multiColumnListView.itemsSource = null; 
            multiColumnListView.columns.Clear();
            multiColumnListView.dataSource = MegalockPersistence.instance;
            multiColumnListView.itemsSource = locks.Count == 0 ? null : locks;
            
            var dataList = locks;
            
            CreateColumnAndCell("path", "Path", (index) => MegalockUtilities.HighlightAssetNameFromPath(dataList[index].path));
            CreateColumnAndCell("name", "Owner", (index) => dataList[index].name);
            CreateColumnAndCell("description", "Description", (index) => string.IsNullOrEmpty(dataList[index].description) ? "-------" : dataList[index].description);
            CreateColumnAndCell("lock_duration", "Locked Since", (index) => dataList[index].lock_duration);
            
            /*serializedObject ??= new SerializedObject(MegalockPersistence.instance);
            multiColumnListView.Bind(serializedObject);*/
            multiColumnListView.Rebuild();
        }

        

        private void CreateColumnAndCell(string columnName, string columnLabel, Func<int,string> GetStringFromIndex) //"id", "ID", dataList[rowIndex].id.ToString()
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
                }
            };
        }
        
        public override void OnShow()
        {
            base.OnShow();
            MegalockPersistence.instance.currentTabView = this;
            pathInputField.value = MegalockPersistence.instance.searchFields.assetPath;
            descriptionInputField.value = MegalockPersistence.instance.searchFields.description;
            ownerInputField.value = MegalockPersistence.instance.searchFields.ownerName;
        }
    }
}
