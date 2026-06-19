using System;
using System.Collections.Generic;
using System.Linq;
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
    public class view_main : BaseView
    {
        private TabView mainTabView;
        //Tab mapping
        public Dictionary<Tab, BaseView> tabMapping = new Dictionary<Tab, BaseView>();
        //Tab Instances
        public Dictionary<Type, BaseView> tabViews = new Dictionary<Type, BaseView>();
        protected override void BuildUI()
        {
            mainTabView = RootViewInstance.Q<TabView>("main-tab-view");
            mainTabView.style.flexGrow = 1;
            mainTabView.style.width = new StyleLength(Length.Percent(100));
            mainTabView.style.height = new StyleLength(Length.Percent(100));
            
            var tab_locks_all = RootViewInstance.Q<Tab>("tab-locks-all");
            if (tab_locks_all == null)
            {
                Debug.LogError("Tab not found in the visual tree.");
                return;
            }
            var view_locks_instance = CreateInstance<view_locks>().Initialize(ViewManager) as view_locks;
            if (view_locks_instance)
            {
                tab_locks_all.Add(view_locks_instance.GetRootViewInstance());
                tabViews.Add(typeof(view_locks), view_locks_instance);
                tabMapping.Add(tab_locks_all, view_locks_instance);
            }
            
            var tab_add_lock = RootViewInstance.Q<Tab>("tab-add-lock");
            var view_add_locks_instance = CreateInstance<view_add_locks>().Initialize(ViewManager) as view_add_locks;
            if (view_add_locks_instance)
            {
                tab_add_lock.Add(view_add_locks_instance.GetRootViewInstance());
                tabViews.Add(typeof(view_add_locks), view_add_locks_instance);
                tabMapping.Add(tab_add_lock, view_add_locks_instance);
            }
            
            var tab_remove_lock = RootViewInstance.Q<Tab>("tab-remove-lock");
            var view_remove_locks_instance = CreateInstance<view_remove_locks>().Initialize(ViewManager) as view_remove_locks;
            if (view_remove_locks_instance)
            {
                tab_remove_lock.Add(view_remove_locks_instance.GetRootViewInstance());
                tabViews.Add(typeof(view_remove_locks), view_remove_locks_instance);
                tabMapping.Add(tab_remove_lock, view_remove_locks_instance);
            }
            
            if (mainTabView == null)
            {
                Debug.LogError("Main view not found in the visual tree.");
                return;
            }
            
            var tab_admin = RootViewInstance.Q<Tab>("admin");
            var tab_admin_instance = CreateInstance<view_admin>().Initialize(ViewManager) as view_admin;
            if (tab_admin_instance)
            {
                tab_admin.Add(tab_admin_instance.GetRootViewInstance());
                tabViews.Add(typeof(view_admin), tab_admin_instance);
                tabMapping.Add(tab_admin, tab_admin_instance);
            }
            
            RefreshAdminTabView();
            
            if (MegalockPersistence.instance.currentTabView != null)
            {
                var currentTabView = MegalockPersistence.instance.currentTabView;
                int index = tabViews.Keys.ToList().IndexOf(currentTabView.GetType()); //looks weird but this takes into account tab reorganization
                mainTabView.selectedTabIndex = index;
            }

            if (mainTabView == null) return;
            mainTabView.activeTabChanged += (tab, tab1) =>
            {
                tabMapping[tab1]?.OnShow();
            };
            
        }

        public override void Deinitialize()
        {
            foreach (var tabs in tabMapping.Values)
            {
                tabs.Deinitialize();
            }
            base.Deinitialize();
        }

        public void RefreshAdminTabView()
        {
            var tab_admin = RootViewInstance.Q<Tab>("admin");
            tab_admin.SetEnabled(MegalockPersistence.instance.currentUserSession.isAdmin);
        }
        
    }
}
