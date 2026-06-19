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
using Tab = UnityEngine.UIElements.Tab;
using MultiColumnList = UnityEngine.UIElements.MultiColumnListView;

namespace MegaLock
{
    public class view_locks : BaseView
    {
        private Button refresh_button = null;
        private MultiColumnListView multiColumnListView = null;
        [SerializeField] private VisualTreeAsset cellTemplate = null;
        protected override void BuildUI()
        {
            refresh_button = RootViewInstance.Q<Button>("refresh-button");
            if (refresh_button != null)
            {
                refresh_button.clicked += HandleRefreshClicked;
            }
            
            multiColumnListView = RootViewInstance.Q<MultiColumnListView>("locks-list");
            RefreshMultiColumnListView(MegalockPersistence.instance.locks);
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
            }),
            (r) =>
            {
                RefreshMultiColumnListView(MegalockPersistence.instance.locks);
            });
        }

        private void RefreshMultiColumnListView(List<LockData> locks)
        {
            if (multiColumnListView == null)
            {
                Debug.LogWarning("Multi-column list view is null");
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
        }
    }
}
