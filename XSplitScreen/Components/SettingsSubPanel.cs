using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace DoDad.XSplitScreen.Components
{
    class SettingsSubPanel : MonoBehaviour
    {
        #region Variables
        private const float sliderHeight = 8;
        private const float sliderWidth = 0.85f;
        private const float functionButtonSize = 56f;
        private static Sprite gearSprite;
        private static Sprite xSprite;

        public UserPanel panel;

        public bool open = false;

        private HGButton functionButton;
        private Image functionImage;

        private Slider colorSlider;
        private Slider hudScaleSlider;
        private Slider handicapSlider;
        private LanguageTextMeshController hudScaleText;
        #endregion

        #region Unity Methods
        void OnEnable()
        {
            functionButton?.GetComponent<MPEventSystemLocator>().Awake();
        }
        #endregion

        #region Initialize
        public void Initialize(UserPanel panel, float panelSizeDelta)
        {
            this.panel = panel;

            //var panelSize = panel.GetComponent<RectTransform>().sizeDelta.x;

            if (gearSprite == null)
                gearSprite = Instantiate(XLibrary.Resources.GetSprite("gear"));

            if (xSprite == null)
                xSprite = Instantiate(XLibrary.Resources.GetSprite("xmark"));

            functionButton = GameObject.Instantiate(XLibrary.Resources.GetPrefabUI(XLibrary.Resources.UIPrefabIndex.SimpleButton)).GetComponent<HGButton>();
            functionButton.transform.SetParent(transform);
            functionButton.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            functionButton.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, functionButtonSize);
            functionButton.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, functionButtonSize);
            functionButton.gameObject.SetActive(true);
            functionButton.onClick.AddListener(OnClickFunction);
            functionButton.transform.localPosition = new Vector3(-(panelSizeDelta - (functionButtonSize / 2)), (panelSizeDelta - (functionButtonSize / 2)), 0);
            functionImage = functionButton.GetComponent<Image>();
            functionImage.sprite = gearSprite;

            var verticalElements = gameObject.AddComponent<VerticalLayoutGroup>();
            verticalElements.childForceExpandHeight = false;
            verticalElements.childForceExpandWidth = false;
            verticalElements.childControlHeight = true;
            verticalElements.childControlWidth = true;
            verticalElements.childScaleHeight = true;
            verticalElements.childScaleWidth = true;
            verticalElements.childAlignment = TextAnchor.MiddleCenter;
            verticalElements.spacing = 32f;

            var contentSizeFitter = gameObject.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            colorSlider = Instantiate(XLibrary.Resources.GetPrefabUI(XLibrary.Resources.UIPrefabIndex.Slider)).GetComponentInChildren<Slider>();
            colorSlider.gameObject.AddComponent<MultiInputHelper>().slider = colorSlider;

            Destroy(colorSlider.transform.parent.GetChild(1).gameObject);
            colorSlider.transform.parent.SetParent(transform);
            colorSlider.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelSizeDelta * SettingsSubPanel.sliderWidth);
            colorSlider.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sliderHeight);
            colorSlider.transform.parent.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelSizeDelta * SettingsSubPanel.sliderWidth);
            colorSlider.transform.parent.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sliderHeight);

            var sliderWidth = colorSlider.GetComponent<RectTransform>().sizeDelta.x;

            colorSlider.transform.localPosition = new Vector3(sliderWidth / 2f, 0, 0);
            colorSlider.transform.parent.gameObject.SetActive(true);
            colorSlider.transform.parent.name = "(Slider) Color";
            colorSlider.onValueChanged.AddListener(OnUpdateColor);
            colorSlider.maxValue = 1;
            colorSlider.minValue = 0;

            var colorSliderLayout = colorSlider.gameObject.AddComponent<LayoutElement>();
            colorSliderLayout.flexibleHeight = 1;
            colorSliderLayout.preferredHeight = 8;

            // HUD Scale slider text

            hudScaleText = Instantiate(XLibrary.Resources.GetPrefabUI(XLibrary.Resources.UIPrefabIndex.SimpleText)).GetComponent<LanguageTextMeshController>();
            var hudScaleTextLayout = hudScaleText.gameObject.AddComponent<LayoutElement>();
            hudScaleTextLayout.flexibleHeight = 1;
            hudScaleTextLayout.preferredHeight = 8;
            hudScaleTextLayout.preferredWidth = panelSizeDelta;

            var hudScaleTextMesh = hudScaleText.GetComponent<HGTextMeshProUGUI>();
            hudScaleTextMesh.raycastTarget = false;
            //hudScaleTextMesh.autoSizeTextContainer = true;
            hudScaleTextMesh.fontSize = 24;
            hudScaleText.transform.SetParent(transform);
            hudScaleText.transform.localPosition = new Vector3(sliderWidth / 2f, 0, 0);
            hudScaleText.token = "XSS_USEROPTION_HUDSCALE";
            hudScaleText.gameObject.SetActive(true);
            object[] args = new object[1];
            args[0] = "-";
            hudScaleText.formatArgs = args;

            // HUD Scale slider
            hudScaleSlider = Instantiate(XLibrary.Resources.GetPrefabUI(XLibrary.Resources.UIPrefabIndex.Slider)).GetComponentInChildren<Slider>();
            hudScaleSlider.gameObject.AddComponent<MultiInputHelper>().slider = hudScaleSlider;

            Destroy(hudScaleSlider.transform.parent.GetChild(1).gameObject);
            hudScaleSlider.transform.parent.SetParent(transform);
            hudScaleSlider.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelSizeDelta * SettingsSubPanel.sliderWidth);
            hudScaleSlider.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sliderHeight);
            hudScaleSlider.transform.parent.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelSizeDelta * SettingsSubPanel.sliderWidth);
            hudScaleSlider.transform.parent.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sliderHeight);

            hudScaleSlider.transform.localPosition = new Vector3(sliderWidth / 2f, 0, 0);
            hudScaleSlider.transform.parent.gameObject.SetActive(true);
            hudScaleSlider.transform.parent.name = "(Slider) Color";
            hudScaleSlider.onValueChanged.AddListener(OnUpdateHUDScale);
            hudScaleSlider.maxValue = 300;
            hudScaleSlider.minValue = 1;
            hudScaleSlider.wholeNumbers = true;

            var hudScaleSliderLayout = hudScaleSlider.gameObject.AddComponent<LayoutElement>();
            hudScaleSliderLayout.flexibleHeight = 1;
            hudScaleSliderLayout.preferredHeight = 8;

            /*
            //var colorLayout = colorSlider.transform.parent.gameObject.AddComponent<LayoutElement>();
            //colorLayout.flexibleHeight = 1;
            //colorLayout.preferredHeight = sliderHeight;

                        hudScaleSlider = Instantiate(XLibrary.Resources.GetPrefabUI(XLibrary.Resources.UIPrefabIndex.Slider)).GetComponentInChildren<Slider>();
                        Destroy(hudScaleSlider.transform.parent.GetChild(1).gameObject);
                        hudScaleSlider.transform.parent.SetParent(transform);
                        hudScaleSlider.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelSizeDelta * SettingsSubPanel.sliderWidth);
                        hudScaleSlider.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sliderHeight);
                        hudScaleSlider.transform.parent.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelSizeDelta * SettingsSubPanel.sliderWidth);
                        hudScaleSlider.transform.parent.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sliderHeight);
                        hudScaleSlider.transform.localPosition = new Vector3(sliderWidth / 2f, 0, 0);
                        hudScaleSlider.transform.parent.gameObject.SetActive(true);
                        hudScaleSlider.transform.parent.name = "(Slider) HUD Scale";
                        hudScaleSlider.onValueChanged.AddListener(OnUpdateHUDScale);
                        hudScaleSlider.maxValue = 200;
                        hudScaleSlider.minValue = 10f;
                        hudScaleSlider.wholeNumbers = true;
                        var hudScaleSliderLayout = hudScaleSlider.transform.parent.gameObject.AddComponent<LayoutElement>();
                        hudScaleSliderLayout.flexibleHeight = 1;
                        hudScaleSliderLayout.preferredHeight = sliderHeight;

                        hudScaleText = Instantiate(XLibrary.Resources.GetPrefabUI(XLibrary.Resources.UIPrefabIndex.SimpleText)).GetComponent<LanguageTextMeshController>();
                        var hudScaleTextLayout = hudScaleText.gameObject.AddComponent<LayoutElement>();
                        hudScaleTextLayout.flexibleHeight = 1;
                        hudScaleTextLayout.preferredHeight = sliderHeight;
                        var hudScaleTextMesh = hudScaleText.GetComponent<HGTextMeshProUGUI>();
                        hudScaleTextMesh.raycastTarget = false;
                        hudScaleTextMesh.autoSizeTextContainer = true;
                        hudScaleText.transform.SetParent(transform);
                        hudScaleText.transform.localPosition = hudScaleSlider.transform.localPosition;
                        hudScaleText.token = "XSS_USEROPTION_HUDSCALE";
                        hudScaleText.gameObject.SetActive(true);
            */
        }
        #endregion

        #region Event Listeners
        public void OnUpdateHUDScale(Single value)
        {
            if (panel.current == null)
                return;

            int rounded = Mathf.Clamp(((int)value / 10) * 10, 10, 300);
            object[] args = new object[1];
            args[0] = rounded;
            hudScaleText.formatArgs = args;
            panel.current.hudScale = (int)rounded;
            hudScaleSlider.SetValueWithoutNotify(rounded);
        }
        public void OnUpdateColor(Single value)
        {
            if (panel.current == null)
                return;

            Vector3 hsv = Vector3.zero;
            Color.RGBToHSV(panel.current.color, out hsv.x, out hsv.y, out hsv.z);

            var color = Color.HSVToRGB(value, hsv.y, hsv.z);

            panel.current.color = color;

            UpdateColorSliderHandle(color);
        }
        public void OnClickFunction()
        {
            panel.OnSubPanelFunction();
        }
        #endregion

        #region Helpers
        public void ShowSubPanel(bool status)
        {
            functionImage.enabled = status;

            SetOpenState(false);
        }
        public void SetOpenState(bool status)
        {
            open = status;

            colorSlider.transform.parent.gameObject.SetActive(open);
            hudScaleSlider.transform.parent.gameObject.SetActive(open);
            hudScaleText.gameObject.SetActive(open);

            ShowControllerSubIcon(!open);

            if (open)
            {
                functionImage.sprite = xSprite;
                UpdateColorSlider();
                UpdateHUDScaleSlider(true);
            }
            else
            {
                functionImage.sprite = gearSprite;
                Assignments.AssignmentManager.Save();
            }
        }
        private void UpdateColorSlider()
        {
            if (panel.current == null)
                return;

            Vector3 currentColorHSV = Vector3.zero;

            Color.RGBToHSV(panel.current.color, out currentColorHSV.x, out currentColorHSV.y, out currentColorHSV.z);

            colorSlider.SetValueWithoutNotify(currentColorHSV.x);

            UpdateColorSliderHandle(panel.current.color);
        }
        private void UpdateHUDScaleSlider(bool forceUpdateText = false)
        {
            if (panel.current == null)
                return;

            hudScaleSlider.value = panel.current.hudScale;

            if (forceUpdateText)
                OnUpdateHUDScale(hudScaleSlider.value);
        }
        private void ShowControllerSubIcon(bool status)
        {
            if (panel.current?.controller == null)
                return;

            AssignmentWindow.controllerPanel.GetIconByController(panel.current.controller).ShowSubIcon(status);
        }
        private void UpdateColorSliderHandle(Color color)
        {
            var colorBlock = colorSlider.colors;
            colorBlock.normalColor = panel.current.color;
            colorBlock.highlightedColor = panel.current.color;
            colorBlock.selectedColor = panel.current.color;
            colorBlock.pressedColor = panel.current.color;

            colorSlider.colors = colorBlock;
        }
        #endregion
    }
}
