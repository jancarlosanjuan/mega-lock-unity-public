using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEngine;
using UnityEngine.UIElements;
using Text = UnityEngine.UIElements.TextElement;
using Button = UnityEngine.UIElements.Button;

namespace MegaLock
{
    public class view_loading : BaseView
    {
        private Image loadingIcon = null;
        private bool allowUpdate = false;
        protected override void BuildUI()
        {
            RootViewInstance.style.flexGrow = 1;
            RootViewInstance.style.width = new StyleLength(Length.Percent(100));
            RootViewInstance.style.height = new StyleLength(Length.Percent(100));
            RootViewInstance.style.position = new StyleEnum<Position>(Position.Absolute);
            RootViewInstance.style.justifyContent = Justify.Center;
            loadingIcon = RootViewInstance.Q<Image>("loading-icon");
         if (loadingIcon == null)
         {
             Debug.LogError("Loading icon not found in the visual tree.");
         }
            
        }

        public override void OnShow()
        {
            allowUpdate = true;
        }
        
        public override void OnHide()
        {
            allowUpdate = false;
        }

        private const float rotationSpeed = 180f; // degrees per second

        public override void OnEditorUpdate(float delta)
        {
            if (!allowUpdate || loadingIcon == null) return;
            float curr = loadingIcon.style.rotate.value.angle.value;
            float nextAngle = curr + (rotationSpeed * delta);
            if (nextAngle >= 360f) nextAngle -= 360f;
            loadingIcon.style.rotate = new Rotate(new Angle(nextAngle, AngleUnit.Degree));
        }
    }
}
