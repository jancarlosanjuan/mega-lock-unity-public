using System;

namespace MegaNotify
{
    /// <summary>
    /// If the user doesn't mind having any on click behaviors get lost on after assembly reload, then using this class
    /// instance is fine. Otherwise, make another child class of EditorNotificationOnClickAction.
    /// </summary>
    [Serializable]
    public class EditorNotificationOnClickAction_Transient : EditorNotificationOnClickAction
    {
        private Action onClick;
        private bool hasTransientAction;

        public EditorNotificationOnClickAction_Transient(Action transientOnClick)
        {
            if (transientOnClick == null)
            {
                hasTransientAction = false;
                return;
            }
            
            onClick = transientOnClick;
            hasTransientAction = true;
        }

        public override void OnClick()
        {
            onClick?.Invoke();
        }

        public override bool CanRunOnClick()
        {
            return hasTransientAction;
        }

        public override void OnAfterAssemblyReload()
        {
            hasTransientAction = false;
        }
    }
}