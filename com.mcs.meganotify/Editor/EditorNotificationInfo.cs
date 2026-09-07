using System;
using UnityEngine;

namespace MegaNotify
{
    [Serializable]
    public class EditorNotificationInfo
    {
        public string Header;
        public string Description;
        
        [SerializeReference] public EditorNotificationOnClickAction OnClickAction;
    }
}