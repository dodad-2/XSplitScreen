using DoDad.XLibrary.Components;
using DoDad.XLibrary.Toolbox;
using RoR2.UI;
using RoR2.UI.MainMenu;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DoDad.XSplitScreen.Components
{
    class OptionsPanel : MonoBehaviour
    {
        #region Variables
        private static readonly int2 monitorSize = new int2(64, 64);
        private static readonly int2 resetSize = new int2(52, 52);

        private const string enableToken = "XSS_ENABLE";
        private const string disableToken = "XSS_DISABLE";

        private const int splitscreenToggleSize = 48;

        private Image monitorWidget;

        private HGTextMeshProUGUI monitorText;

        private HGButton arrowLeft;
        private HGButton arrowRight;
        private HGButton reset;

        private LanguageTextMeshController splitscreenText;

        internal MPToggle splitscreenToggle;

        private GameObject display;
        private GameObject notificationBar;
        #endregion

        #region Unity Methods
        void Awake()
        {
        }

        void OnEnable()
        {
            arrowRight?.GetComponent<MPEventSystemLocator>().Awake();

            arrowLeft?.GetComponent<MPEventSystemLocator>().Awake();

            reset?.GetComponent<MPEventSystemLocator>().Awake();

            splitscreenToggle?.GetComponent<MPEventSystemLocator>().Awake();

            splitscreenToggle?.SetIsOnWithoutNotify(Plugin.active);

            UpdateSplitscreenToggleText();
        }
        #endregion

        #region Initialization
        public void Initialize()
        {
            var element = GetComponent<LayoutElement>();
            //element.minWidth = panelSize.x;
            //element.minHeight = 128;
            element.preferredHeight = 128;
            element.flexibleHeight = 1.5f;

            monitorWidget = Instantiate(XLibrary.Resources.GetPrefabUI("SimpleImage")).GetComponent<Image>();
            monitorWidget.name = "Monitor Widget";
            monitorWidget.transform.SetParent(transform);
            monitorWidget.transform.localPosition = Vector3.zero;
            monitorWidget.sprite = Instantiate(XLibrary.Resources.GetSprite("monitor"));
            monitorWidget.SetNativeSize();

            monitorWidget.gameObject.SetActive(true);

            monitorWidget.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, monitorSize.x);
            monitorWidget.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, monitorSize.y);

            monitorText = Instantiate(XLibrary.Resources.GetPrefabUI("SimpleText")).GetComponent<HGTextMeshProUGUI>();
            monitorText.name = "Display Text";
            monitorText.transform.SetParent(monitorWidget.transform);
            monitorText.transform.localPosition = new Vector3(-1.5f, -30.5f, 0);
            monitorText.fontSize = 24;
            monitorText.fontSizeMax = 24;
            monitorText.fontSizeMin = 24;
            monitorText.horizontalAlignment = HorizontalAlignmentOptions.Center;

            Destroy(monitorText.GetComponent<LanguageTextMeshController>());

            monitorText.gameObject.SetActive(true);

            arrowRight = Instantiate(XLibrary.Resources.GetPrefabUI("RightArrow")).GetComponent<HGButton>();
            arrowRight.name = "(Button) Arrow Right";
            arrowRight.transform.SetParent(monitorWidget.transform);
            arrowRight.transform.localScale = Vector3.one;
            arrowRight.transform.localPosition = new Vector3(100, 0, 0);

            var arrowRightButton = arrowRight.GetComponent<HGButton>();
            arrowRightButton.onClick.AddListener(OnArrowRight);
            arrowRightButton.requiredTopLayer = AssignmentScreen.layerKey;

            arrowRight.gameObject.SetActive(true);

            arrowLeft = Instantiate(XLibrary.Resources.GetPrefabUI("RightArrow")).GetComponent<HGButton>();
            arrowLeft.name = "(Button) Arrow Left";
            arrowLeft.transform.SetParent(monitorWidget.transform);
            arrowLeft.transform.localRotation = Quaternion.AngleAxis(180, Vector3.forward);
            arrowLeft.transform.localPosition = new Vector3(-100, 0, 0);
            arrowLeft.transform.localScale = Vector3.one;

            var arrowLeftButton = arrowLeft.GetComponent<HGButton>();
            arrowLeftButton.onClick.AddListener(OnArrowLeft);
            arrowLeftButton.requiredTopLayer = AssignmentScreen.layerKey;

            arrowLeft.gameObject.SetActive(true);

            reset = Instantiate(XLibrary.Resources.GetPrefabUI("SimpleButton")).GetComponent<HGButton>();
            reset.name = "(Button) Reset";
            reset.transform.SetParent(transform);
            reset.transform.localPosition = new Vector3(-200, 0, 0);
            reset.hoverToken = "XSS_OPTION_RESET_ASSIGNMENTS_HOVER";
            reset.hoverLanguageTextMeshController = AssignmentScreen.descriptionController;
            reset.updateTextOnHover = true;

            reset.GetComponent<Image>().sprite = Instantiate(XLibrary.Resources.GetSprite("reset"));
            reset.GetComponent<Image>().SetNativeSize();

            reset.onClick.AddListener(OnReset);

            reset.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, resetSize.x);
            reset.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, resetSize.y);

            reset.gameObject.SetActive(!Plugin.active);

            splitscreenToggle = Instantiate(XLibrary.Resources.GetPrefabUI(XLibrary.Resources.UIPrefabIndex.Toggle)).GetComponent<MPToggle>();

            var toggleContainer = new GameObject("Toggle Container");
            toggleContainer.transform.SetParent(transform);
            toggleContainer.transform.position = Vector3.zero;

            splitscreenToggle.transform.SetParent(transform);
            splitscreenToggle.transform.localPosition = new Vector3(-15, -70, 0);
            splitscreenToggle.transform.localScale = Vector3.one;
            splitscreenToggle.gameObject.SetActive(true);
            splitscreenToggle.onValueChanged.AddListener(OnEnableSplitscreenToggle);

            splitscreenText = Instantiate(XLibrary.Resources.GetPrefabUI(XLibrary.Resources.UIPrefabIndex.SimpleText)).GetComponent<LanguageTextMeshController>();

            var textRect = splitscreenText.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0.5f);
            textRect.anchorMax = textRect.anchorMin;
            textRect.pivot = textRect.anchorMin;
            textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200);

            splitscreenText.transform.SetParent(transform);
            splitscreenText.transform.localPosition = new Vector3(15, -70, 0);
            splitscreenText.gameObject.SetActive(true);
            splitscreenText.textMeshPro.fontSizeMin = 24;
            splitscreenText.textMeshPro.alignment = TextAlignmentOptions.Left;

            var rect = splitscreenToggle.GetComponent<RectTransform>();
            rect.anchorMax = Vector3.one;
            rect.anchorMin = Vector3.zero;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, splitscreenToggleSize);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, splitscreenToggleSize);
        }
        #endregion

        #region Event Handlers
        public void OnEnableSplitscreenToggle(bool value)
        {
            AssignmentWindow.SetSplitscreenEnabled(value);
            splitscreenToggle.SetIsOnWithoutNotify(UserManager.localUsers.Count > 0);
            UpdateSplitscreenToggleText();
            UpdateResetButton();
        }
        public void OnAssignmentsUpdated()
        {
            // reset monitor widget, etc
            UpdateMonitorWidget(AssignmentWindow.currentDisplay);
        }
        private void OnGraphReloaded()
        {
            UpdateMonitorWidget(AssignmentWindow.currentDisplay);
        }
        public void OnArrowRight()
        {
            RequestDisplay(AssignmentWindow.currentDisplay + 1);
        }
        public void OnArrowLeft()
        {
            RequestDisplay(AssignmentWindow.currentDisplay - 1);
        }
        public void OnReset()
        {
            AssignmentWindow.ResetAllAssignments();
        }
        #endregion

        #region Helpers
        private void UpdateResetButton()
        {
            reset.interactable = !Plugin.active;
        }
        private void UpdateSplitscreenToggleText()
        {
            if (splitscreenText == null)
                return;

            splitscreenText.token = Plugin.active ? disableToken : enableToken;
        }
        private void RequestDisplay(int display)
        {
            display = Mathf.Clamp(display, 0, Display.displays.Length - 1);

            AssignmentWindow.RequestDisplay(display);
        }
        private void UpdateMonitorWidget(int display)
        {
            monitorText.text = (display + 1).ToString();

            arrowLeft.interactable = true;
            arrowRight.interactable = true;

            if (display == 0)
                arrowLeft.interactable = false;

            if (display == Display.displays.Length - 1)
                arrowRight.interactable = false;
        }
        #endregion
    }
}
