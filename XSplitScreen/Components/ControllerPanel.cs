using DoDad.XSplitScreen.Assignments;
using Rewired;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace DoDad.XSplitScreen.Components
{
    class ControllerPanel : MonoBehaviour
    {
        #region Variables
        public static readonly Vector2 iconSize = new Vector2(64, 64);

        private Action onResetIcons;

        private List<ControllerIcon> icons;
        private Transform container;
        #endregion

        #region Unity Methods
        void Awake()
        {
            container = new GameObject("Controller Icon Container", typeof(RectTransform), typeof(LayoutElement), typeof(HorizontalLayoutGroup)).transform;

            var containerElement = container.GetComponent<LayoutElement>();
            //containerElement.minWidth = DisplayAssignmentsPanel.displaySize.x;
            //containerElement.preferredWidth = DisplayAssignmentsPanel.displaySize.x;
            containerElement.preferredHeight = iconSize.y * 1.5f;//256;//128;
            containerElement.flexibleHeight = 1;
            //container.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, containerElement.minWidth);

            container.SetParent(transform);

            container.transform.localPosition = new Vector3(0, 320, 0);
            container.transform.localScale = Vector3.one;

            var layoutGroup = container.GetComponent<HorizontalLayoutGroup>();
            layoutGroup.childControlHeight = false;
            layoutGroup.childControlWidth = false;
            layoutGroup.childScaleHeight = false;
            layoutGroup.childScaleWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.spacing = iconSize.x * 0.4f;

            RefreshAvailableIcons();

            Rewired.ReInput.ControllerConnectedEvent += ReInput_ControllerConnectedEvent;
            Rewired.ReInput.ControllerDisconnectedEvent += ReInput_ControllerDisconnectedEvent;
        }
        #endregion

        #region Icons
        public void AssignControllerToPanel(ControllerIcon icon, UserPanel panel)
        {
            if (panel)
                panel.current.controller = icon.controller;

            icon.SetPanel(panel);
        }
        public void UpdateIcons()
        {
            onResetIcons?.Invoke();

            foreach (var assignment in AssignmentManager.assignments)
            {
                if (assignment.controller == null)
                    continue;

                var icon = GetIconByController(assignment.controller);

                if (icon == null)
                    continue;

                icon.SetAssignment(assignment);

                var panel = AssignmentWindow.displayPanel.GetPanelByAssignment(assignment);

                if (panel == null)
                    continue;

                icon.SetPanel(panel);
            }
        }
        public ControllerIcon GetIconByController(Controller controller)
        {
            foreach (ControllerIcon icon in icons)
            {
                if (controller == icon.controller)
                    return icon;
            }

            return null;
        }
        private void RefreshAvailableIcons()
        {
            if (icons == null)
                icons = new List<ControllerIcon>();

            foreach (Controller controller in ReInput.controllers.Controllers)
            {
                if (controller.type == ControllerType.Mouse)
                    continue;

                if (icons.Where(x => x.controller.Equals(controller)).Count() == 0)
                    AddIcon(controller);
            }
        }
        private void AddIcon(Controller controller)
        {
            var controllerIcon = new GameObject($"Controller Icon for '{controller.name}'", typeof(RectTransform), typeof(LayoutElement), typeof(ControllerIcon)).GetComponent<ControllerIcon>();

            var element = controllerIcon.GetComponent<LayoutElement>();
            element.minHeight = iconSize.y;
            element.minWidth = iconSize.x;

            //element.preferredHeight = iconSize.y;
            //element.preferredWidth = iconSize.x;

            controllerIcon.transform.SetParent(container);
            controllerIcon.transform.localPosition = Vector3.zero;
            controllerIcon.transform.localScale = Vector3.one;
            controllerIcon.onDropIcon += OnDropIcon;
            controllerIcon.SetController(controller);

            onResetIcons += controllerIcon.OnResetIcons;

            icons.Add(controllerIcon);
        }
        private void ClearDisconnectedIcons()
        {
            icons.Where(x => !x.controller.isConnected).ToList().ForEach(x => Destroy(x.gameObject));
            icons.RemoveAll(x => x == null);
        }
        #endregion

        #region Event Handlers
        private void ReInput_ControllerDisconnectedEvent(Rewired.ControllerStatusChangedEventArgs obj)
        {
            if (obj.controllerType == ControllerType.Mouse)
                return;

            ClearDisconnectedIcons();
            UpdateIcons();
        }
        private void ReInput_ControllerConnectedEvent(Rewired.ControllerStatusChangedEventArgs obj)
        {
            if (obj.controllerType == ControllerType.Mouse)
                return;

            AddIcon(obj.controller);
            UpdateIcons();
        }
        private void OnDropIcon(ControllerIcon icon)
        {
            if (icon.current != null)
                icon.current.controller = null;

            var panel = AssignmentWindow.displayPanel.GetNearestPanel(icon.subIconTransform.position);

            icon.OnResetIcons();

            if (panel)
            {
                panel.AssignController(icon.controller);
                icon.SetAssignment(panel.current);
                icon.SetPanel(panel);
            }
        }
        #endregion
    }
}
