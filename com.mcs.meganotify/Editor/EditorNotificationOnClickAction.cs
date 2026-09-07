using System;
using UnityEngine;

namespace MegaNotify
{
    /// <summary>
    /// A wrapper to hold behavior on clicking editor notifications. This is needed due to the editor removing any actions
    /// or delegates stored in memory on after assembly reload. This is solved by caching variables and explicitly defining
    /// behaviour inside a serializable class instance.
    /// </summary>
    [Serializable]
    public class EditorNotificationOnClickAction
    {
        public virtual void OnClick() { }

        public virtual bool CanRunOnClick() => true;

        public virtual void OnAfterAssemblyReload() { }
    }
}

