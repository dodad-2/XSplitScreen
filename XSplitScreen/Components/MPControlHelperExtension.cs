using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DoDad.XSplitScreen.Components
{
    class MPControlHelperExtension : MonoBehaviour, ISubmitHandler
    {
        #region Variables
        public MPToggle toggle;

        public void OnSubmit(BaseEventData eventData)
        {
            toggle.mpControlHelper.OnSubmit(eventData, toggle.OnSubmit, true, ref toggle.inPointerUp);
        }
        #endregion
    }
}
