using DoDad.XSplitScreen.Assignments;
using RoR2;
using RoR2.UI;
using RoR2.UI.MainMenu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DoDad.XSplitScreen.Components
{
    class AssignmentScreen : BaseMainMenuScreen
    {
        public static UILayerKey layerKey { get; private set; }
        public static Transform overlayContainer { get; private set; }
        internal static LemonController lemonizer { get; private set; }
        internal static LanguageTextMeshController descriptionController { get; private set; }
        internal AssignmentWindow assignmentWindow { get; private set; }
        #region Unity Methods
        public void OnDisable()
        {
            /*ToggleCursorOpeners(Plugin.active, !Plugin.active); // 4.0.0 rewrite 9-12-24

            if (!Plugin.active)
                ToggleUIHooks(false);*/
        }
        #endregion

        #region Initialization
        public void Initialize()
        {
            onEnter = new UnityEvent();
            onExit = new UnityEvent();

            SetCameraTransform();
            CreateUI();
        }
        #endregion

        #region UI
        private void SetCameraTransform()
        {
            if (desiredCameraTransform == null)
                desiredCameraTransform = new GameObject("WorldPosition").transform;

            desiredCameraTransform.parent = transform.parent;
            desiredCameraTransform.position = new Vector3(-10.8f, 601.2f, -424.2f);

            Quaternion forward = Quaternion.identity;

            forward = Quaternion.AngleAxis(20f, Vector3.up);
            forward *= Quaternion.AngleAxis(-40f, Vector3.right);

            desiredCameraTransform.rotation = forward;
        }
        private void CreateUI()
        {
            transform.localScale = Vector3.one; //

            // Add options
            var juicePanel = transform.GetChild(0).GetChild(0);
            var backJuicePanel = transform.GetChild(1).GetChild(0);

            descriptionController = juicePanel.GetChild(0).GetChild(0).GetChild(1).GetComponent<LanguageTextMeshController>();

            if (!juicePanel || !descriptionController)
                return;

            lemonizer = gameObject.AddComponent<LemonController>();

            backJuicePanel.GetChild(0).GetComponent<HGButton>().onClick.m_PersistentCalls.Clear();
            backJuicePanel.GetChild(0).GetComponent<HGButton>().onClick.AddListener(RequestTitleMenu);

            layerKey = transform.parent.GetComponent<UILayerKey>();

            layerKey.layer = ScriptableObject.CreateInstance<UILayer>();
            layerKey.layer.name = "AssignmentScreen";
            layerKey.layer.priority = 10;
            layerKey.onBeginRepresentTopLayer = new UnityEvent();
            layerKey.onEndRepresentTopLayer = new UnityEvent();

            var optionDiscord = GameObject.Instantiate(XLibrary.Resources.GetPrefabUI("ScreenOption"));
            optionDiscord.name = "(Option) Discord";
            optionDiscord.transform.SetParent(juicePanel);
            optionDiscord.transform.localScale = Vector3.one;
            optionDiscord.GetComponent<LanguageTextMeshController>().token = "XSS_OPTION_DISCORD";

            var discordHGButton = optionDiscord.GetComponent<HGButton>();
            discordHGButton.hoverToken = "XSS_OPTION_DISCORD_HOVER";
            discordHGButton.requiredTopLayer = layerKey;
            discordHGButton.onClick.AddListener(OnClickDiscord);
            discordHGButton.hoverLanguageTextMeshController = descriptionController;
            discordHGButton.updateTextOnHover = true;

			LemonController.buttons.Add(discordHGButton);

			lemonizer.Lemonize(discordHGButton.image);

			optionDiscord.SetActive(true);
            
            overlayContainer = new GameObject("Overlay Container", typeof(RectTransform)).transform;
            overlayContainer.SetParent(transform);
            overlayContainer.transform.localScale = Vector3.one;

            assignmentWindow = new GameObject("Assignment Window", typeof(RectTransform), /*typeof(VerticalLayoutGroup), */typeof(AssignmentWindow)).GetComponent<AssignmentWindow>();
            assignmentWindow.transform.SetParent(transform);
            assignmentWindow.transform.localPosition = Vector3.zero;
            //assignmentWindow.transform.localScale = Vector3.one * 1.3f; // <-- last added

            overlayContainer.SetAsLastSibling();

        }
        #endregion

        #region Splitscreen
        private void ToggleUIHooks(bool status)
        {
            HookManager.UpdateHooks(HookType.Singleplayer, status);
        }
        #endregion

        #region Event Handlers
        public void RequestTitleMenu()
        {
            MainMenuController.instance.SetDesiredMenuScreen(MainMenuController.instance.titleMenuScreen);
        }
        public void OnClickDiscord()
        {
            Application.OpenURL("https://discord.gg/maHhJSv62G");
        }
        public void DebugOutput()
        {
			//Log.LogOutput($"Success!"); // 4.0.0 rewrite 9-12-24
		}
		public void OpenScreen()
        {
            ToggleCursorOpeners(true);
            ToggleUIHooks(true);

            foreach (MPEventSystem instance in MPEventSystem.instancesList)
            {
                instance.SetSelectedGameObject(null);
            }

            SetCameraTransform();

            MainMenuController.instance.SetDesiredMenuScreen(this);

            //assignmentWindow.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Display.main.renderingWidth);//1024);
            //assignmentWindow.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Display.main.renderingHeight);//1200);

        }
        #endregion

        #region Helpers
        private void ToggleCursorOpeners(bool status, bool onlyThis = true)
        {
            if (onlyThis)
            {
                transform.parent.GetComponent<CursorOpener>().forceCursorForGamePad = status;
                return;
            }

            CursorOpener[] openers = FindObjectsOfType<CursorOpener>();

            foreach (CursorOpener opener in openers)
            {
                opener.forceCursorForGamePad = status;
            }
        }
        #endregion
    }
}
