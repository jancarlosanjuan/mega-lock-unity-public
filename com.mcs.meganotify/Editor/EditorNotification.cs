using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MegaNotify
{
    public class EditorNotification : EditorWindow
    {
        public EditorNotificationContainer Container;
        public EditorNotificationInfo Info;
        
        [SerializeReference] public EditorNotificationOnClickAction OnClickAction;
        
        private Label headerLabel;
        private Label descriptionLabel;
        private Button closeButton;
        private ProgressBar lifetimeBar;
        
        private bool isHovered;
        private bool isHoveredOverCloseButton;
        private bool canTriggerClickCallbackAndClose;
        
        private void OnEnable()
        {
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        }

        private void OnDisable()
        {
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
        }

        private void OnAfterAssemblyReload()
        {
            if (Container != null)
                Container.ReaddToNotificationSystem();
            
            if (OnClickAction != null)
                OnClickAction.OnAfterAssemblyReload();
        }

        private void CreateGUI()
        {
            rootVisualElement.Add(MegaNotifySystem.CreateEditorNotificationInstance());
            rootVisualElement.RegisterCallback<PointerOverEvent>(OnPointerOver);
            rootVisualElement.RegisterCallback<PointerOutEvent>(OnPointerOut);
            rootVisualElement.RegisterCallback<PointerDownEvent>(OnPointerDown);
            rootVisualElement.RegisterCallback<PointerUpEvent>(OnPointerUp);

            headerLabel = rootVisualElement.Q<Label>("Header");
            if (headerLabel != null)
                headerLabel.text = Info.Header;

            descriptionLabel = rootVisualElement.Q<Label>("Description");
            if (descriptionLabel != null)
                descriptionLabel.text = Info.Description;

            closeButton = rootVisualElement.Q<Button>("CloseButton");
            if (closeButton != null)
            {
                closeButton.clicked += () => { MegaNotifySystem.CleanupNotification(this); };
                closeButton.RegisterCallback<PointerOverEvent>(OnCloseButtonPointerOver);
                closeButton.RegisterCallback<PointerOutEvent>(OnCloseButtonPointerOut);
            }

            lifetimeBar = rootVisualElement.Q<ProgressBar>("LifetimeBar");
            if (lifetimeBar != null)
                lifetimeBar.value = 0;
        }

        private void OnPointerOver(PointerOverEvent evt)
        {
            isHovered = true;
        }

        private void OnPointerOut(PointerOutEvent evt)
        {
            isHovered = false;
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (!isHovered || isHoveredOverCloseButton)
                return;

            canTriggerClickCallbackAndClose = true;
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!canTriggerClickCallbackAndClose)
                return;

            canTriggerClickCallbackAndClose = false;

            if (!isHovered || isHoveredOverCloseButton)
                return;

            if (!OnClickAction?.CanRunOnClick() ?? true)
                return;
            
            OnClickAction.OnClick();
            MegaNotifySystem.CleanupNotification(this);
        }

        private void OnCloseButtonPointerOver(PointerOverEvent evt)
        {
            isHoveredOverCloseButton = true;
        }

        private void OnCloseButtonPointerOut(PointerOutEvent evt)
        {
            isHoveredOverCloseButton = false;
        }

        public void BindToContainer(EditorNotificationContainer container, EditorNotificationInfo info)
        {
            Container = container;
            container.Notification = this;

            Info = info;
            OnClickAction = info?.OnClickAction;
        }

        public void UpdateLifetimeBar(float progress)
        {
            if (lifetimeBar != null)
                lifetimeBar.value = progress;
        }
    }
}