using DoDad.XSplitScreen.Assignments;
using Rewired;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DoDad.XSplitScreen.Components
{
    class ControllerIcon : MonoBehaviour
    {
        #region Variables
        private const float subIconScale = 1.3f;
        private const float lerpSpeed = 10f;
        private const float idleAlpha = 0.4f;
        private const float activityAlpha = 1f;
        private const float activityTimeout = 0.4f;
        private const float iconMovementSpeed = 0.4f;
        private const float iconDragSpeed = 0.05f;

        public Controller controller { get; private set; }
        public UserPanel panel { get; private set; }
        public Assignment current;

        public Action<ControllerIcon> onDropIcon;

        public Transform subIconTransform => subIcon.transform;

        private BaseInputModule interactor;

        private XButton iconButton;
        private XButton subIconButton;
        private Image icon;
        private Image subIcon;

        private Color assignedColor, iconTargetColor, subIconTargetColor, defaultColor = new Color(1, 1, 1, 0.4f);
        private Color invisibleColor = new Color(1, 1, 1, 0);

        private float activityTimer = 0f;

        private Vector3 iconVelocity, subVelocity;

        private bool didInteract = false;
        #endregion

        #region Unity Methods
        void Awake()
        {
            icon = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(XButton)).GetComponent<Image>();
            icon.transform.SetParent(AssignmentScreen.overlayContainer);
            icon.transform.position = transform.position;
            icon.transform.localScale = Vector3.one;
            icon.color = invisibleColor;

            iconButton = icon.GetComponent<XButton>();
            iconButton.onSubmit += OnControllerInteraction;
            iconButton.onPointerDown += OnBeginInteraction;
            iconButton.onPointerUp += OnEndInteraction;

            subIcon = new GameObject("Sub Icon", typeof(RectTransform), typeof(Image), typeof(XButton)).GetComponent<Image>();
            subIcon.transform.SetParent(AssignmentScreen.overlayContainer);
            subIcon.transform.position = transform.position;
            subIcon.transform.localScale = Vector3.one;
            subIcon.color = invisibleColor;
            subIcon.raycastTarget = false;

            subIconButton = subIcon.GetComponent<XButton>();
            subIconButton.onSubmit += OnControllerInteraction;
            subIconButton.onPointerDown += OnBeginInteraction;
            subIconButton.onPointerUp += OnEndInteraction;

            if (Plugin.active)
                OnSplitscreenEnabled();
            else
                OnSplitscreenDisabled();

            UserManager.onSplitscreenEnabled += OnSplitscreenEnabled;
            UserManager.onSplitscreenDisabled += OnSplitscreenDisabled;
            LemonController.onLemon += OnLemon;
        }
        void Update()
        {
            if (!Application.isFocused && !panel)
            {
                interactor = null;
                subIcon.raycastTarget = false;
            }

            if (controller != null)
            {
                if (controller.GetAnyButton())
                {
                    iconTargetColor.a = activityAlpha;
                    activityTimer = activityTimeout;
                }

                if (current != null)
                {
                    if (assignedColor != current.color)
                        UpdateColor(current.color);
                }

                if (activityTimer > 0)
                    activityTimer -= Time.unscaledDeltaTime;

                if (activityTimer <= 0)
                    iconTargetColor.a = idleAlpha;

                icon.color = Color.Lerp(icon.color, iconTargetColor, Time.unscaledDeltaTime * lerpSpeed);

                if (LemonController.isLemonized)
                    icon.color = new Color(icon.color.r, icon.color.g, icon.color.b, 1.2f);

                subIconTargetColor = icon.color;

                if (!subIcon.raycastTarget)
                    subIconTargetColor.a = 0f;

                subIcon.color = subIconTargetColor;
            }

            Vector3 targetPosition = transform.position;

            if (interactor)
                targetPosition = interactor.input.mousePosition;
            else if (panel)
                targetPosition = panel.iconTarget.position;

            icon.transform.position = Vector3.SmoothDamp(icon.transform.position, transform.position, ref iconVelocity, iconMovementSpeed);
            subIcon.transform.position = Vector3.SmoothDamp(subIcon.transform.position, targetPosition, ref subVelocity, iconDragSpeed);
        }
        void LateUpdate()
        {
            if (didInteract)
                didInteract = false;

            if (interactor && interactor.eventSystem && interactor.eventSystem.currentSelectedGameObject == null)
            {
                interactor.eventSystem.SetSelectedGameObject(subIcon.gameObject);
            }
        }
        void OnEnable()
        {
            icon.transform.position = transform.position;
        }
        void OnDestroy()
        {
            UserManager.onSplitscreenEnabled -= OnSplitscreenEnabled;
            UserManager.onSplitscreenDisabled -= OnSplitscreenDisabled;

            Destroy(subIcon.gameObject);
            Destroy(icon.gameObject);
        }
        #endregion

        #region Public Methods
        public void SetAssignment(Assignment assignment)
        {
            Log.LogOutput($"ControllerIcon.SetAssignment {controller.name} receiving assignment '{(assignment == null ? "none" : assignment.profile)}'");

            if (this.current != null)
                this.current.onUpdateController -= OnControllerUpdated;

            this.current = assignment;

            if (this.current != null)
            {
                this.current.onUpdateController += OnControllerUpdated;
                UpdateColor(this.current.color);
            }
            else
                UpdateColor(defaultColor);

            SetInteractionAbility(false);
        }
        public void SetPanel(UserPanel panel)
        {
            this.panel = panel;

            ShowSubIcon(this.panel == null ? false : true);

            SetInteractionAbility(true);
        }
        public void SetController(Controller controller)
        {
            this.controller = controller;

            icon.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ControllerPanel.iconSize.x);
            icon.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ControllerPanel.iconSize.y);
            subIcon.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ControllerPanel.iconSize.x * subIconScale);
            subIcon.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ControllerPanel.iconSize.y * subIconScale);

            if (controller.name.ToLower().Contains("sony") || controller.name.ToLower().Contains("hyper"))
            {
                icon.sprite = Instantiate(XLibrary.Resources.GetSprite("dinput"));
                subIcon.sprite = Instantiate(XLibrary.Resources.GetSprite("dinput"));
            }
            else if (controller.name.ToLower().Contains("key"))
            {
                icon.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ControllerPanel.iconSize.x + 50);
                icon.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ControllerPanel.iconSize.y + 50);
                subIcon.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, (ControllerPanel.iconSize.x + 25) * subIconScale);
                subIcon.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (ControllerPanel.iconSize.y + 25) * subIconScale);

                icon.sprite = Instantiate(XLibrary.Resources.GetSprite("keyboardmouse"));
                subIcon.sprite = Instantiate(XLibrary.Resources.GetSprite("keyboardmouse"));
            }
            else
            {
                icon.sprite = Instantiate(XLibrary.Resources.GetSprite("xinput"));
                subIcon.sprite = Instantiate(XLibrary.Resources.GetSprite("xinput"));
            }

            icon.transform.position = transform.position;
            subIcon.transform.position = transform.position;
        }
        #endregion

        #region Event Handlers
        public void OnLemon()
        {
            if (icon == null || subIcon == null)
                return;

            if (!LemonController.isLemonized)
            {
                var iconPosition = icon.transform.position;
                var subIconPosition = subIcon.transform.position;
                SetController(controller);
                icon.transform.position = iconPosition;
                subIcon.transform.position = subIconPosition;
            }
            else
            {
                icon.sprite = LemonController.lemonSprite;
                subIcon.sprite = LemonController.lemonSprite;
            }
        }
        public void OnSplitscreenEnabled()
        {
            iconButton.interactable = false;
            subIconButton.interactable = false;
        }
        public void OnSplitscreenDisabled()
        {
            iconButton.interactable = true;
            subIconButton.interactable = true;
        }
        public void OnControllerUpdated(Assignment assignment)
        {
            if (assignment.controller != controller)
                OnResetIcons();
        }
        public void OnResetIcons()
        {
            SetAssignment(null);
            SetPanel(null);
        }
        public void OnControllerInteraction(XButton button, BaseInputModule inputModule)
        {
            if (interactor && inputModule.Equals(interactor))
                OnEndInteraction(button, inputModule);
            else if (!interactor)
                OnBeginInteraction(button, inputModule);
        }
        public void OnBeginInteraction(XButton button, BaseInputModule inputModule)
        {
            if (didInteract)
                return;

            if (interactor)
                StopDrag(inputModule);

            StartDrag(inputModule);
        }
        public void OnEndInteraction(XButton button, BaseInputModule inputModule)
        {
            if (didInteract)
                return;

            if (interactor && interactor.Equals(inputModule))
            {
                StopDrag(interactor);
                OnDropIcon();
            }
        }
        #endregion

        #region Helpers
        private void SetInteractionAbility(bool state)
        {
            //iconButton.interactable = state;
            //subIconButton.interactable = state;
        }
        public void ShowSubIcon(bool status)
        {
            if (interactor)
                subIcon.transform.position = interactor.input.mousePosition;
            else if (panel && status)
                subIcon.transform.position = panel.iconTarget.position;

            subIcon.raycastTarget = status;
        }
        private void OnDropIcon()
        {
            onDropIcon?.Invoke(this);
        }
        private void StartDrag(BaseInputModule inputModule)
        {
            interactor = inputModule;
            ShowSubIcon(true);
            didInteract = true;
        }
        private void StopDrag(BaseInputModule inputModule)
        {
            interactor = null;
            ShowSubIcon(false); // likely don't need to call this here. it's called during SetPanel
            didInteract = true;
            inputModule.eventSystem.SetSelectedGameObject(null);
        }
        private Color LoadRGB(Color keepAlpha, Color rgb)
        {
            rgb.a = keepAlpha.a;
            return rgb;
        }
        private void UpdateColor(Color newColor)
        {
            assignedColor = newColor;
            iconTargetColor = LoadRGB(iconTargetColor, assignedColor);
        }
        #endregion
    }
}
