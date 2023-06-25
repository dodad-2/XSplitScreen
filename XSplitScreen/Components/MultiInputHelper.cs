using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DoDad.XSplitScreen.Components
{
    class MultiInputHelper : MonoBehaviour
    {
        public Toggle toggle;
        public MPButton mpButton;
        public MPDropdown mpDropdown;
        public MPScrollbar mpScrollbar;
        public Slider slider;
        public XButton xButton;

        public void OnClick(PointerEventData eventData, bool didClick, bool didDrag)
        {
            if (didClick)
            {
                mpButton?.InvokeClick();
                //((Toggle)mpToggle)?.InternalToggle();
                toggle?.InternalToggle();
                mpDropdown?.OnPointerClick(eventData);
                xButton?.OnSubmit(eventData);
            }

            if (didDrag)
            {
                slider?.OnPointerDown(eventData);
                mpScrollbar?.OnPointerDown(eventData);
            }
        }
    }
}
