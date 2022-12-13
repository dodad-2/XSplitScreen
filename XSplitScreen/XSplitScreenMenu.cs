using RoR2;
using RoR2.UI;
using RoR2.UI.MainMenu;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DoDad.XSplitScreen.Components
{
    class XSplitScreenMenu : BaseMainMenuScreen
    {
        #region Variables
        public static BaseMainMenuScreen instance { get; private set; }

        public RectTransform buttonPanel { get; private set; }
        public RectTransform assignmentWindow { get; private set; }
        public UILayerKey layerKey { get; private set; }

        private bool convertButtons = true;
        private int delay = 5;
        private int frameCount = 0;
        #endregion

        #region Unity Methods
        public void OnEnable()
        {
            CursorOpener[] openers = FindObjectsOfType<CursorOpener>();

            foreach (CursorOpener opener in openers)
            {
                opener.forceCursorForGamePad = true;
            }

            foreach (MPEventSystem instance in MPEventSystem.instancesList)
            {
                instance.SetSelectedGameObject(null);
            }
        }
        public void Update()
        {
            base.Update();

            if(convertButtons)
            {
                foreach (HGButton button in assignmentWindow.GetComponentsInChildren<HGButton>(true))
                {
                    if (button.GetComponent<XButtonConverter>() == null && button.GetComponent<XButton>() == null)
                    {
                        XButtonConverter converter = button.gameObject.AddComponent<XButtonConverter>();
                        converter.migrateOnClick = true;
                        converter.Initialize(2);
                    }
                }

                convertButtons = false;
            }
        }
        public void OnDisable()
        {
            if (XSplitScreen.configuration.enabled)
                return;

            CursorOpener[] openers = FindObjectsOfType<CursorOpener>();

            foreach (CursorOpener opener in openers)
            {
                opener.forceCursorForGamePad = false;
            }
        }
        #endregion

        #region Initialization
        public XSplitScreenMenu()
        {
            instance = this;
        }
        public void Initialize()
        {
            InitializeWorldPosition();
            InitializeReferences();
            InitializeMenu();
        }
        private void InitializeWorldPosition()
        {
            Transform worldPosition = new GameObject("World Position").transform;
            worldPosition.SetParent(transform.parent);

            worldPosition.position = new Vector3(-10.8f, 601.2f, -424.2f);
            // 80.87, 1.36, 368.40
            Quaternion forward = Quaternion.identity;

            forward = Quaternion.AngleAxis(20f, Vector3.up);
            forward *= Quaternion.AngleAxis(-40f, Vector3.right);

            worldPosition.rotation = forward;

            desiredCameraTransform = worldPosition;
        }
        private void InitializeReferences()
        {
            onEnter = new UnityEvent();
            onExit = new UnityEvent();
        }
        private void InitializeMenu()
        {
            GameObject template = MainMenuController.instance.extraGameModeMenuScreen.gameObject;

            GameObject screen = gameObject;

            Canvas tCanvas = template.GetComponent<Canvas>();
            CanvasScaler tCanvasScaler = template.GetComponent<CanvasScaler>();
            GraphicRaycaster tRaycaster = template.GetComponent<GraphicRaycaster>();
            CanvasGroup tCanvasGroup = template.GetComponent<CanvasGroup>();
            UILayerKey tLayerKey = template.GetComponent<UILayerKey>();

            Canvas canvas = screen.AddComponent<Canvas>();
            CanvasScaler canvasScaler = screen.AddComponent<CanvasScaler>();
            GraphicRaycaster raycaster = screen.AddComponent<GraphicRaycaster>();
            CanvasGroup canvasGroup = screen.AddComponent<CanvasGroup>();

            // Disabled as this requires UI layer keys
            //InputSourceFilter inputSourceFilter = screen.AddComponent<InputSourceFilter>();
            //HGGamepadInputEvent gamepadInputEvent = screen.AddComponent<HGGamepadInputEvent>();

            layerKey = screen.AddComponent<UILayerKey>();

            CursorOpener cursorOpener = screen.AddComponent<CursorOpener>();

            canvas.additionalShaderChannels = tCanvas.additionalShaderChannels;
            canvas.renderMode = tCanvas.renderMode;
            canvasScaler.screenMatchMode = tCanvasScaler.screenMatchMode;
            canvasScaler.uiScaleMode = tCanvasScaler.uiScaleMode;
            canvasScaler.matchWidthOrHeight = tCanvasScaler.matchWidthOrHeight;
            canvasScaler.referenceResolution = tCanvasScaler.referenceResolution;
            canvasGroup.blocksRaycasts = tCanvasGroup.blocksRaycasts;
            canvas.scaleFactor = 1.3333f;

            layerKey.layer = ScriptableObject.CreateInstance<UILayer>();
            layerKey.layer.name = XSplitScreen.PluginName;
            layerKey.layer.priority = 10;
            layerKey.onBeginRepresentTopLayer = new UnityEvent();
            layerKey.onEndRepresentTopLayer = new UnityEvent();

            screen.AddComponent<MPEventSystemProvider>();

            GameObject duplicateMenu = Instantiate(template.transform.GetChild(0).gameObject);
            duplicateMenu.name = "Main Panel";
            duplicateMenu.transform.SetParent(screen.transform);

            OnEnableEvent onEnableGenericMenu = duplicateMenu.transform.GetChild(0).GetComponent<OnEnableEvent>();
            onEnableGenericMenu.action.AddListener(duplicateMenu.transform.GetChild(0).GetComponent<UIJuice>().TransitionPanFromLeft);
            onEnableGenericMenu.action.AddListener(duplicateMenu.transform.GetChild(0).GetComponent<UIJuice>().TransitionAlphaFadeIn);

            OnEnableEvent onEnableSubmenu = duplicateMenu.transform.GetChild(2).GetComponent<OnEnableEvent>();
            onEnableSubmenu.action.AddListener(duplicateMenu.transform.GetChild(2).GetComponent<UIJuice>().TransitionPanFromBottom);
            onEnableSubmenu.action.AddListener(duplicateMenu.transform.GetChild(2).GetComponent<UIJuice>().TransitionAlphaFadeIn);

            Destroy(screen.transform.GetChild(0).transform.GetChild(2).gameObject);

            // Disabled as this requires UI layer keys
            //inputSourceFilter.objectsToFilter = new GameObject[1];
            //inputSourceFilter.objectsToFilter[0] = screen.transform.GetChild(0).transform.GetChild(2).gameObject;
            //inputSourceFilter.requiredInputSource = MPEventSystem.InputSource.Gamepad;

            //gamepadInputEvent.actionName = "UICancel";
            //gamepadInputEvent.actionEvent = new UnityEvent();
            //gamepadInputEvent.enabledObjectsIfActive = new GameObject[0];

            foreach (HGButton button in duplicateMenu.GetComponentsInChildren<HGButton>())
            {
                button.requiredTopLayer = layerKey;

                XButtonConverter converter = button.gameObject.AddComponent<XButtonConverter>();

                converter.Initialize();

                if (button.name.ToLower().Contains("return"))
                {
                    converter.onClickMono.AddListener(OnClickRequestMenu);
                }
            }

            Destroy(onEnableGenericMenu.gameObject.transform.GetChild(0).GetChild(1).gameObject);
            Destroy(onEnableGenericMenu.gameObject.transform.GetChild(0).GetChild(2).gameObject);
            Destroy(onEnableGenericMenu.gameObject.transform.GetChild(0).GetChild(3).gameObject);

            ResetBindingControllers(screen);

            RectTransform menuRect = duplicateMenu.GetComponent<RectTransform>();

            menuRect.anchorMax = new Vector2(0.95f, 0.95f);
            menuRect.anchorMin = new Vector2(0.05f, 0.05f);
            menuRect.offsetMax = Vector2.zero;
            menuRect.offsetMin = Vector2.zero;
            menuRect.anchoredPosition = Vector2.zero;
            menuRect.localScale = Vector3.one;

            myMainMenuController = MainMenuController.instance;

            foreach (HGButton button in screen.GetComponentsInChildren<HGButton>())
                button.requiredTopLayer = layerKey;

            buttonPanel = transform.gameObject.GetComponentInChildren<VerticalLayoutGroup>().GetComponent<RectTransform>();

            XButtonConverter discordButton = CreateButton("Discord");
            discordButton.Initialize();

            discordButton.hoverToken = XSplitScreen.Language.MSG_DISCORD_LINK_HOVER_TOKEN;
            discordButton.token = XSplitScreen.Language.MSG_DISCORD_LINK_TOKEN;
            discordButton.onClickMono.AddListener(OnClickJoinDiscord);
            discordButton.defaultFallbackButton = false;
            // assignment

            XButtonConverter patreonButton = CreateButton("Patreon");
            patreonButton.Initialize();

            patreonButton.token = XSplitScreen.Language.MSG_PATREON_LINK_TOKEN;
            patreonButton.onClickMono.AddListener(OnClickVisitPatreon);
            patreonButton.defaultFallbackButton = false;

            template = MainMenuController.instance.profileMenuScreen.transform.GetChild(0).gameObject;

            assignmentWindow = Instantiate(template, transform).GetComponent<RectTransform>();
            assignmentWindow.name = "(Popup Panel) Assignment Window";

            Destroy(assignmentWindow.GetComponent<HGGamepadInputEvent>());
            Destroy(assignmentWindow.GetChild(0).gameObject);
            Destroy(assignmentWindow.GetChild(1).GetChild(0).gameObject); // CornerRect
            Destroy(assignmentWindow.GetChild(1).GetChild(2).GetChild(0).gameObject); // Header CornerRect
            Destroy(assignmentWindow.GetChild(1).GetComponent<OnEnableEvent>());

            assignmentWindow.GetChild(1).GetComponent<CanvasGroup>().alpha = 1;

            foreach (HGButton button in assignmentWindow.GetComponentsInChildren<HGButton>(true))
            {
                if (!button.gameObject.activeSelf)
                    continue;

                Destroy(button.gameObject);
            }

            foreach (UserProfileListElementController controller in assignmentWindow.GetComponentsInChildren<UserProfileListElementController>(true))
                Destroy(controller.gameObject);

            foreach (LanguageTextMeshController languageController in assignmentWindow.GetComponentsInChildren<LanguageTextMeshController>(true))
                languageController.token = XSplitScreen.Language.MSG_UNSET_TOKEN;

            RectTransform templateRect = template.GetComponent<RectTransform>();

            assignmentWindow.anchorMin = new Vector2(0.1f, 0.5f);
            assignmentWindow.anchorMax = new Vector2(0.9f, 0.8f);
            assignmentWindow.offsetMin = templateRect.offsetMin;
            assignmentWindow.offsetMax = templateRect.offsetMax;

            assignmentWindow.GetChild(0).GetComponent<RectTransform>().anchorMin = templateRect.GetChild(0).GetComponent<RectTransform>().anchorMin;
            assignmentWindow.GetChild(0).GetComponent<RectTransform>().anchorMax = templateRect.GetChild(0).GetComponent<RectTransform>().anchorMax;
            assignmentWindow.GetChild(0).GetComponent<RectTransform>().offsetMin = templateRect.GetChild(0).GetComponent<RectTransform>().offsetMin;
            assignmentWindow.GetChild(0).GetComponent<RectTransform>().offsetMax = templateRect.GetChild(0).GetComponent<RectTransform>().offsetMax;

            foreach (HGButton button in GetComponentsInChildren<HGButton>(true))
            {
                button.requiredTopLayer = layerKey;
            }

            ResetBindingControllers(assignmentWindow.gameObject);

            assignmentWindow.GetChild(1).GetChild(2).GetComponentInChildren<LanguageTextMeshController>().token = XSplitScreen.Language.MSG_SPLITSCREEN_CONFIG_HEADER_TOKEN;

            assignmentWindow.gameObject.AddComponent<ConfigurationManager>();
        }
        #endregion

        #region XButton Listeners
        public void OnClickRequestMenu(MonoBehaviour mono)
        {
            MainMenuController.instance.SetDesiredMenuScreen(MainMenuController.instance.titleMenuScreen);
        }
        public void OnClickRequestMenu()
        {
            OnClickRequestMenu(null);
        }
        private static void OnClickOpenDebugFolder(MonoBehaviour mono)
        {
            Application.OpenURL(Application.persistentDataPath);
        }
        private static void OnClickJoinDiscord(MonoBehaviour mono)
        {
            Application.OpenURL(XSplitScreen.Language.MSG_DISCORD_LINK_HREF);
        }
        private static void OnClickVisitPatreon(MonoBehaviour mono)
        {
            Application.OpenURL(XSplitScreen.Language.MSG_PATREON_LINK_HREF);
        }
        #endregion

        #region Helpers
        private static void ResetBindingControllers(GameObject subMenu)
        {
            foreach (InputBindingDisplayController child in subMenu.GetComponentsInChildren<InputBindingDisplayController>(true))
            {
                if (child)
                {
                    child.GetComponent<MPEventSystemLocator>().Awake();
                    child.Awake();
                }
            }
            foreach (HGButton child in subMenu.GetComponentsInChildren<HGButton>(true))
            {
                child.disablePointerClick = false;
                child.disableGamepadClick = false;
                child.GetComponent<MPEventSystemLocator>().Awake();
            }
        }
        private XButtonConverter CreateButton(string name = "XButton")
        {
            GameObject template = GameObject.Find("GenericMenuButton (Singleplayer)");

            GameObject newXButton = Instantiate(XSplitScreen.buttonTemplate);

            newXButton.name = $"XButton ({name})";

            newXButton.transform.SetParent(buttonPanel);
            newXButton.transform.SetSiblingIndex(1);
            newXButton.transform.localScale = Vector3.one;

            newXButton.GetComponent<MPButton>().requiredTopLayer = layerKey;

            return newXButton.AddComponent<XButtonConverter>();
        }
        #endregion
    }
}