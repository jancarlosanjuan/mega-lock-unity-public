using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

namespace MegaLock
{
    [Serializable]
    public abstract class BaseView : ScriptableObject
    {
        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset = default;
        protected VisualElement RootViewInstance { get; private set; } //This will be added to the root container if it is shown
        protected ViewManager ViewManager { get; private set; }

        public virtual BaseView Initialize(ViewManager viewManager)
        {
            ViewManager = viewManager;
            if (m_VisualTreeAsset == null)
            {
                Debug.LogError($"Visual tree asset on {this.GetType().ToString()} is null. Always start with a UXML and bind it on the script.");
                return this;
            }
            RootViewInstance = m_VisualTreeAsset.CloneTree();
            
            //We just wanna force the topmost root level INSTANCE to be the entire screen then we just adjust the children in the UIToolkit Editor
            RootViewInstance.style.flexGrow = 1;
            RootViewInstance.style.width = new StyleLength(Length.Percent(100));
            RootViewInstance.style.height = new StyleLength(Length.Percent(100));
            
            BuildUI(); //We can add more code stuff here in addition to the UXML file
            
            
            return this;
        }

        protected abstract void BuildUI();
        public virtual void OnShow() { } 
        public virtual void OnHide() { }

        public virtual void Deinitialize()
        {
            DestroyImmediate(this);   
        }
        
        public virtual void OnEditorUpdate(float delta) { }
        public VisualElement GetRootViewInstance() => RootViewInstance;
    }   
}