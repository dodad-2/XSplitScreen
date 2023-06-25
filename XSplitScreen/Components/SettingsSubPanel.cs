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
        private const float sliderHeight = 12;
        private const float sliderWidth = 0.85f;
        private const float functionButtonSize = 56f;
        private static Sprite gearSprite;
        private static Sprite xSprite;

        public UserPanel panel;

        public bool open = false;
        
        private HGButton functionButton;
        private Image functionImage;

        private Slider colorSlider;
        private Slider aimAssistSlider;
        private Slider handicapSlider;
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
            verticalElements.childScaleHeight = false;
            verticalElements.childScaleWidth = false;
            verticalElements.childAlignment = TextAnchor.MiddleCenter;

            colorSlider = Instantiate(XLibrary.Resources.GetPrefabUI(XLibrary.Resources.UIPrefabIndex.Slider)).GetComponentInChildren<Slider>();
            Destroy(colorSlider.transform.parent.GetChild(1).gameObject);
            colorSlider.transform.parent.SetParent(transform);
            colorSlider.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelSizeDelta * sliderWidth);
            colorSlider.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sliderHeight);
            colorSlider.transform.parent.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelSizeDelta * sliderWidth);
            colorSlider.transform.parent.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sliderHeight);
            colorSlider.transform.localPosition = new Vector3(colorSlider.GetComponent<RectTransform>().sizeDelta.x / 2f, 0, 0);
            colorSlider.transform.parent.gameObject.SetActive(true);
            colorSlider.transform.parent.name = "(Slider) Color";
            colorSlider.onValueChanged.AddListener(OnUpdateColor);
            colorSlider.maxValue = 1;
            colorSlider.minValue = 0;
            //colorSlider.gameObject.AddComponent<MultiInputHelper>().slider = colorSlider;
        }
        #endregion

        #region Event Listeners
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
            ShowControllerSubIcon(!open);

            if (open)
            {
                functionImage.sprite = xSprite;
                UpdateColorSlider();
            }
            else
            {
                functionImage.sprite = gearSprite;
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
