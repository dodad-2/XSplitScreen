using BepInEx;
using BepInEx.Configuration;
using MonoMod.Cil;
using R2API;
using R2API.Utils;
using Rewired;
using RoR2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using DoDad.Library.Math;
using RoR2.UI.MainMenu;
using System.Collections;
using UnityEngine.SceneManagement;
using RoR2.UI;
using UnityEngine.EventSystems;
using Rewired.Integration.UnityUI;
using Rewired.UI;
using Mono.Cecil.Cil;
using UnityEngine.UI;
using DoDad.XSplitScreen.Components;

namespace DoDad.XSplitScreen
{
    [BepInDependency(R2API.R2API.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(Library.Library.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [R2APISubmoduleDependency(new string[] { "CommandHelper", "LanguageAPI" })]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    public class XSplitScreen : BaseUnityPlugin
    {
        #region Variables
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "com.DoDad";
        public const string PluginName = "XSplitScreen";
        public const string PluginVersion = "2.0.6";

        public static Configuration configuration { get; private set; }
        public static XSplitScreen instance { get; private set; }
        public static AssetBundle assets { get; private set; }
        public static Input input { get; private set; }
        internal static GameObject buttonTemplate { get; private set; }

        private static readonly Log.LogLevel logLevel = Log.LogLevel.All;

        private static readonly bool developerMode = false;

        private static Coroutine WaitForMenuRoutine;
        private static Coroutine WaitForRewiredRoutine;

        private RectTransform titleButton;
        private RectTransform menuContainer;

        private bool readyToInitializePlugin;
        private bool readyToCreateUI;

        private int createUIFrameBuffer = 5;
        #endregion

        #region Unity Methods
        public void Awake()
        {
            if (instance)
                Destroy(this);

            Log.logLevel = logLevel;

            WaitForRewiredRoutine = StartCoroutine(WaitForRewired());
        }
        public void OnDestroy()
        {
            CleanupReferences();

            int c;

            SetEnabled(false, out c);
        }
        public void LateUpdate()
        {
            
            if (readyToCreateUI)
            {
                if (MainMenuController.instance.currentMenuScreen.Equals(MainMenuController.instance.titleMenuScreen))
                {
                    createUIFrameBuffer--;

                    if (createUIFrameBuffer < 1)
                    {
                        if (CreateUI())
                        {
                            readyToCreateUI = false;
                            createUIFrameBuffer = 5;

                            configuration.TryAutoEnable();
                        }
                    }
                }
            }

            if (readyToInitializePlugin)
                Initialize();
        }
        #endregion

        #region Initialization & Exit
        private void Initialize()
        {
            instance = this;
            readyToInitializePlugin = false;

            Log.Init(Logger);
            CommandHelper.AddToConsoleWhenReady();

            InitializeLanguage();
            InitializeReferences();

            TogglePersistentListeners(true);

            if (developerMode)
            {
                if (WaitForMenuRoutine != null)
                    StopCoroutine(WaitForMenuRoutine);

                WaitForMenuRoutine = StartCoroutine(WaitForMenu());
            }

        }
        private void InitializeLanguage()
        {
            LanguageAPI.Add(Language.MSG_HOVER_TOKEN, Language.MSG_HOVER_STRING);
            LanguageAPI.Add(Language.MSG_TITLE_BUTTON_TOKEN, Language.MSG_TITLE_BUTTON_STRING);
            LanguageAPI.Add(Language.MSG_SPLITSCREEN_ENABLE_TOKEN, Language.MSG_SPLITSCREEN_ENABLE_STRING);
            LanguageAPI.Add(Language.MSG_SPLITSCREEN_DISABLE_TOKEN, Language.MSG_SPLITSCREEN_DISABLE_STRING);
            LanguageAPI.Add(Language.MSG_SPLITSCREEN_CONFIG_HEADER_TOKEN, Language.MSG_SPLITSCREEN_CONFIG_HEADER_STRING);
            LanguageAPI.Add(Language.MSG_DISCORD_LINK_TOKEN, Language.MSG_DISCORD_LINK_STRING);
            LanguageAPI.Add(Language.MSG_PATREON_LINK_TOKEN, Language.MSG_PATREON_LINK_STRING);
            LanguageAPI.Add(Language.MSG_DISCORD_LINK_HOVER_TOKEN, Language.MSG_DISCORD_LINK_HOVER_STRING);
            LanguageAPI.Add(Language.MSG_VERIFY_CONTROLLER_TOKEN, Language.MSG_VERIFY_CONTROLLER_STRING);
            LanguageAPI.Add(Language.MSG_VERIFY_PROFILE_TOKEN, Language.MSG_VERIFY_PROFILE_STRING);
            LanguageAPI.Add(Language.MSG_VERIFY_GENERIC_TOKEN, Language.MSG_VERIFY_GENERIC_STRING);
            LanguageAPI.Add(Language.MSG_UNSET_TOKEN, Language.MSG_UNSET_STRING);
        }
        private void InitializeReferences()
        {
            if (assets is null)
            {
                try
                {
                    using (Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("XSplitScreen.xsplitscreenbundle"))
                        assets = AssetBundle.LoadFromStream(manifestResourceStream);
                }
                catch (Exception e)
                {
                    Log.LogError(e);
                }
            }

            if (configuration is null)
                configuration = new Configuration(Config);

            if (input is null)
                input = new Input();
        }
        private void CleanupReferences()
        {
            configuration?.Destroy();
            configuration = null;

            assets?.Unload(true);
            assets = null;

            instance = null;

            TogglePersistentListeners(false);
            ToggleConditionalHooks(false);

            if (titleButton != null)
                Destroy(titleButton?.gameObject);

            if (menuContainer != null)
                Destroy(menuContainer?.gameObject);

            if (buttonTemplate != null)
                Destroy(buttonTemplate);
        }
        #endregion

        #region Hooks & Event Handlers

        #region Persistent
        private void TogglePersistentListeners(bool status)
        {
            if (status)
            {
                SceneManager.activeSceneChanged += ActiveSceneChanged;
            }
            else
            {
                SceneManager.activeSceneChanged -= ActiveSceneChanged;
            }
        }
        private void ActiveSceneChanged(Scene previous, Scene current)
        {
            if (string.Compare(current.name, "title") == 0)
            {
                if (WaitForMenuRoutine != null)
                    StopCoroutine(WaitForMenuRoutine);

                WaitForMenuRoutine = StartCoroutine(WaitForMenu());
            }
            else
            {
                foreach (LocalUser user in LocalUserManager.readOnlyLocalUsersList)
                {
                    user.ApplyUserProfileBindingsToRewiredPlayer();
                }
            }
        }
        private void ScreenOnEnter()
        {
            ToggleConditionalHooks();
        }
        private void ScreenOnExit()
        {
            ToggleConditionalHooks();
        }
        #endregion

        #region Splitscreen
        private void ToggleSplitScreenHooks(bool status)
        {
            input.UpdateCurrentEventSystem(LocalUserManager.GetFirstLocalUser().eventSystem);
            input.UpdateCurrentEventSystem(LocalUserManager.GetFirstLocalUser().eventSystem, true);

            // TODO organize

            if (status)
            {
                On.RoR2.UI.CursorOpener.Awake += CursorOpener_Awake;

                On.RoR2.UI.MPButton.Update += MPButton_Update;
                On.RoR2.UI.MPButton.OnPointerClick += MPButton_OnPointerClick;
                On.RoR2.UI.MPButton.InputModuleIsAllowed += MPButton_InputModuleIsAllowed;
                On.RoR2.UI.MPButton.Awake += MPButton_Awake;
                On.RoR2.UI.MPButton.CanBeSelected += MPButton_CanBeSelected;

                On.RoR2.UI.MPInput.CenterCursor += MPInput_CenterCursor;
                On.RoR2.UI.MPInput.Update += MPInput_Update;

                On.RoR2.UI.CharacterSelectController.Update += CharacterSelectController_Update;

                On.RoR2.CharacterSelectBarController.PickIcon += CharacterSelectBarController_PickIcon;

                On.RoR2.UI.SurvivorIconController.Update += SurvivorIconController_Update;
                On.RoR2.UI.SurvivorIconController.UpdateAvailability += SurvivorIconController_UpdateAvailability;

                On.RoR2.UI.RuleChoiceController.FindNetworkUser += RuleChoiceController_FindNetworkUser;

                On.RoR2.UI.LoadoutPanelController.UpdateDisplayData += LoadoutPanelController_UpdateDisplayData;

                On.RoR2.RunCameraManager.Update += RunCameraManager_Update;

                On.RoR2.CameraRigController.Start += CameraRigController_Start;

                On.RoR2.LocalCameraEffect.OnUICameraPreCull += LocalCameraEffect_OnUICameraPreCull;

                On.RoR2.UI.CombatHealthBarViewer.SetLayoutHorizontal += CombatHealthBarViewer_SetLayoutHorizontal;

                On.RoR2.NetworkUser.UpdateUserName += NetworkUser_UpdateUserName;
                On.RoR2.NetworkUser.GetNetworkPlayerName += NetworkUser_GetNetworkPlayerName;

                On.RoR2.PlayerCharacterMasterController.GetDisplayName += PlayerCharacterMasterController_GetDisplayName;

                On.RoR2.UI.Nameplate.LateUpdate += Nameplate_LateUpdate;

                On.RoR2.InputBindingDisplayController.Refresh += InputBindingDisplayController_Refresh;

                On.RoR2.ColorCatalog.GetMultiplayerColor += ColorCatalog_GetMultiplayerColor;

                On.RoR2.UI.BaseSettingsControl.GetCurrentUserProfile += BaseSettingsControl_GetCurrentUserProfile;

                On.RoR2.UI.ProfileNameLabel.LateUpdate += ProfileNameLabel_LateUpdate;

                On.RoR2.SubjectChatMessage.GetSubjectName += SubjectChatMessage_GetSubjectName;

                On.RoR2.Util.GetBestMasterName += Util_GetBestMasterName;

                On.RoR2.UI.InputBindingControl.StartListening += InputBindingControl_StartListening; // IL Test

                On.RoR2.UI.ScoreboardController.Awake += ScoreboardController_Awake;

                On.RoR2.UI.RuleCategoryController.SetRandomVotes += RuleCategoryController_SetRandomVotes;
                /* // Controller navigation requires layer keys

                On.RoR2.UI.InputSourceFilter.Refresh += InputSourceFilter_Refresh;

                On.RoR2.UI.HGGamepadInputEvent.Update += HGGamepadInputEvent_Update;
                */

                // UILayerKey.topLayerRepresentations and queries should probably be handled by this plugin. 
                // MPButton_CanBeSelected is a quick hack to get things working but it makes layer keys useless
                //On.RoR2.UI.UILayerKey.RefreshTopLayerForEventSystem += UILayerKey_RefreshTopLayerForEventSystem; 
            }
            else
            {
                On.RoR2.UI.CursorOpener.Awake -= CursorOpener_Awake;

                On.RoR2.UI.MPButton.Update -= MPButton_Update;
                On.RoR2.UI.MPButton.OnPointerClick -= MPButton_OnPointerClick;
                On.RoR2.UI.MPButton.InputModuleIsAllowed -= MPButton_InputModuleIsAllowed;
                On.RoR2.UI.MPButton.Awake -= MPButton_Awake;
                On.RoR2.UI.MPButton.CanBeSelected -= MPButton_CanBeSelected;

                On.RoR2.UI.MPInput.CenterCursor -= MPInput_CenterCursor;
                On.RoR2.UI.MPInput.Update -= MPInput_Update;

                On.RoR2.UI.CharacterSelectController.Update -= CharacterSelectController_Update;

                On.RoR2.CharacterSelectBarController.PickIcon -= CharacterSelectBarController_PickIcon;

                On.RoR2.UI.SurvivorIconController.Update -= SurvivorIconController_Update;
                On.RoR2.UI.SurvivorIconController.UpdateAvailability -= SurvivorIconController_UpdateAvailability;

                On.RoR2.UI.RuleChoiceController.FindNetworkUser -= RuleChoiceController_FindNetworkUser;

                On.RoR2.UI.LoadoutPanelController.UpdateDisplayData -= LoadoutPanelController_UpdateDisplayData;

                On.RoR2.RunCameraManager.Update -= RunCameraManager_Update;

                On.RoR2.CameraRigController.Start -= CameraRigController_Start;

                On.RoR2.LocalCameraEffect.OnUICameraPreCull -= LocalCameraEffect_OnUICameraPreCull;

                On.RoR2.UI.CombatHealthBarViewer.SetLayoutHorizontal -= CombatHealthBarViewer_SetLayoutHorizontal;

                On.RoR2.NetworkUser.UpdateUserName -= NetworkUser_UpdateUserName;
                On.RoR2.NetworkUser.GetNetworkPlayerName -= NetworkUser_GetNetworkPlayerName;

                On.RoR2.PlayerCharacterMasterController.GetDisplayName -= PlayerCharacterMasterController_GetDisplayName;

                On.RoR2.UI.Nameplate.LateUpdate -= Nameplate_LateUpdate;

                On.RoR2.ColorCatalog.GetMultiplayerColor -= ColorCatalog_GetMultiplayerColor;
                
                On.RoR2.InputBindingDisplayController.Refresh -= InputBindingDisplayController_Refresh;

                On.RoR2.UI.BaseSettingsControl.GetCurrentUserProfile -= BaseSettingsControl_GetCurrentUserProfile;

                On.RoR2.UI.ProfileNameLabel.LateUpdate -= ProfileNameLabel_LateUpdate;

                On.RoR2.SubjectChatMessage.GetSubjectName -= SubjectChatMessage_GetSubjectName;

                On.RoR2.Util.GetBestMasterName -= Util_GetBestMasterName;

                On.RoR2.UI.InputBindingControl.StartListening -= InputBindingControl_StartListening; // IL Test

                On.RoR2.UI.ScoreboardController.Awake -= ScoreboardController_Awake;

                On.RoR2.UI.RuleCategoryController.SetRandomVotes -= RuleCategoryController_SetRandomVotes;

                /*
                On.RoR2.UI.HGGamepadInputEvent.Update -= HGGamepadInputEvent_Update;
                */

                //On.RoR2.UI.GameEndReportPanelController.SetPlayerInfo -= 
                //On.RoR2.UI.UILayerKey.RefreshTopLayerForEventSystem -= UILayerKey_RefreshTopLayerForEventSystem;
            }

            //InputMapperHelper_StartListening_IL(status);

            //InputBindingControl_Update_IL(status);

            ToggleConditionalHooks();
        }
        private void ToggleConditionalHooks(bool exit = false)
        {
            bool status = false;

            if(configuration != null && instance != null)
                status = configuration.enabled || MainMenuController.instance.desiredMenuScreen == XSplitScreenMenu.instance;

            if (configuration != null)
                if (configuration.enabled && MainMenuController.instance.desiredMenuScreen != XSplitScreenMenu.instance)
                    return;

            if (exit)
                status = false;


            if (status)
            {
                On.RoR2.UI.MPInputModule.GetMousePointerEventData += MPInputModule_GetMousePointerEventData;

                On.RoR2.UI.MPControlHelper.InputModuleIsAllowed += MPControlHelper_InputModuleIsAllowed;
                On.RoR2.UI.MPControlHelper.OnPointerClick += MPControlHelper_OnPointerClick;

                On.RoR2.UI.MPEventSystem.ValidateCurrentSelectedGameobject += MPEventSystem_ValidateCurrentSelectedGameobject;
            }
            else
            {
                On.RoR2.UI.MPInputModule.GetMousePointerEventData -= MPInputModule_GetMousePointerEventData;

                On.RoR2.UI.MPControlHelper.InputModuleIsAllowed -= MPControlHelper_InputModuleIsAllowed;
                On.RoR2.UI.MPControlHelper.OnPointerClick -= MPControlHelper_OnPointerClick;

                On.RoR2.UI.MPEventSystem.ValidateCurrentSelectedGameobject -= MPEventSystem_ValidateCurrentSelectedGameobject;
            }
        }

        #region UI Hooks
        // TODO Replace hooks with IL hooks where appropriate
        private void CursorOpener_Awake(On.RoR2.UI.CursorOpener.orig_Awake orig, CursorOpener self)
        {
            // Force the use of cursors for all gamepads

            orig(self);
            self._forceCursorForGamepad = true;
        }
        private bool MPControlHelper_InputModuleIsAllowed(On.RoR2.UI.MPControlHelper.orig_InputModuleIsAllowed orig, ref MPControlHelper self, BaseInputModule inputModule)
        {
            return true;
        }
        private void MPControlHelper_OnPointerClick(On.RoR2.UI.MPControlHelper.orig_OnPointerClick orig, ref MPControlHelper self, PointerEventData eventData, Action<PointerEventData> baseMethod)
        {
            // On click
            input?.UpdateCurrentEventSystem(eventData.currentInputModule.eventSystem);

            orig(ref self, eventData, baseMethod);
        }
        private void MPButton_OnSubmit()
        {
            // TODO - Unused -
            Log.LogDebug($"MPButton_OnSubmit");
        }
        private void MPButton_Awake(On.RoR2.UI.MPButton.orig_Awake orig, MPButton self)
        {
            self.disableGamepadClick = false;
            self.disablePointerClick = false;
            orig(self);
        }
        private void MPButton_Update(On.RoR2.UI.MPButton.orig_Update orig, RoR2.UI.MPButton self)
        {
            // Remove the check for 'disableGamepadClick' - is this necessary?
            // Remove fallback button setting

            if (!self.eventSystem || self.eventSystem.player == null)
                return;

            for (int e = 1; e < MPEventSystem.readOnlyInstancesList.Count; e++)
            {
                MPEventSystem eventSystem = MPEventSystem.readOnlyInstancesList[e];

                if (!eventSystem)
                    continue;

                if (eventSystem.currentSelectedGameObject == self.gameObject &&
                    (eventSystem.player.GetButtonDown(4) || eventSystem.player.GetButtonDown(14)))
                {
                    input?.UpdateCurrentEventSystem(eventSystem);
                    self.InvokeClick();
                }
            }
        }
        private void MPButton_OnPointerClick(On.RoR2.UI.MPButton.orig_OnPointerClick orig, RoR2.UI.MPButton self, PointerEventData eventData)
        {
            // On click

            input?.UpdateCurrentEventSystem(eventData.currentInputModule.eventSystem);

            orig(self, eventData);
        }
        private bool MPButton_InputModuleIsAllowed(On.RoR2.UI.MPButton.orig_InputModuleIsAllowed orig, RoR2.UI.MPButton self, BaseInputModule inputModule)
        {
            // Allow any input module
            return true;
        }
        private bool MPButton_CanBeSelected(On.RoR2.UI.MPButton.orig_CanBeSelected orig, MPButton self)
        {
            // Remove top layer requirement

            if (!self.gameObject.activeInHierarchy)
                return false;

            return true;
        }
        private void MPInput_CenterCursor(On.RoR2.UI.MPInput.orig_CenterCursor orig, MPInput self)
        {
            // Center each cursor on the assigned screen

            Assignment? assignment = configuration.GetAssignmentByPlayerId(self.playerId - 1);

            if (assignment.HasValue)
            {
                Vector2 center = new Vector2(Screen.width, Screen.height) * 0.5f;

                float halfWidth = center.x * 0.5f;
                float halfHeight = center.y * 0.5f;

                if (assignment.Value.position.x > 1)
                    center.y -= halfHeight;
                else if (assignment.Value.position.x < 1)
                    center.y += halfHeight;

                if (assignment.Value.position.y > 1)
                    center.x += halfWidth;
                else if (assignment.Value.position.y < 1)
                    center.x -= halfWidth;

                self.internalMousePosition = center;
            }
            else
            {
                Log.LogOutput($"MPInput_CenterCursor: '{self.playerId}' has no assignment", Log.LogLevel.Warning);
            }
        }
        private void MPInput_Update(On.RoR2.UI.MPInput.orig_Update orig, MPInput self)
        {
            // Update current mouse event system

            if (!self.eventSystem.isCursorVisible)
                return;

            float width = Screen.width;
            float height = Screen.height;

            self.internalScreenPositionDelta = Vector2.zero;

            if (self.eventSystem.currentInputSource == MPEventSystem.InputSource.MouseAndKeyboard)
            {
                if (Application.isFocused)
                {
                    if (Vector3.SqrMagnitude(UnityEngine.Input.mousePosition - (Vector3)self.internalMousePosition) > 0.1f)
                    {
                        input?.UpdateCurrentEventSystem(self.eventSystem, true);
                    }

                    self.internalMousePosition = UnityEngine.Input.mousePosition;
                }
            }
            else
            {
                float num = Mathf.Min(width / 1920f, height / 1080f);

                Vector2 vector2 = new Vector2(self.player.GetAxis(23), self.player.GetAxis(24));

                float magnitude = vector2.magnitude;

                self.stickMagnitude = Mathf.Min(Mathf.MoveTowards(self.stickMagnitude, magnitude, self.cursorAcceleration * Time.unscaledDeltaTime), magnitude);

                float stickMagnitude = self.stickMagnitude;

                if (self.eventSystem.isHovering)
                    stickMagnitude *= self.cursorStickyModifier;

                self.internalScreenPositionDelta = (magnitude == 0.0 ? Vector2.zero : vector2 * (stickMagnitude / magnitude)) * Time.unscaledDeltaTime * (1920f * self.cursorScreenSpeed * num);
                
                Vector3 delta = self.internalMousePosition + self.internalScreenPositionDelta;

                if (Vector3.SqrMagnitude(delta - (Vector3)self.internalMousePosition) > 0.1f)
                {
                    input?.UpdateCurrentEventSystem(self.eventSystem, true);
                }

                self.internalMousePosition = delta;
            }

            self.internalMousePosition.x = Mathf.Clamp(self.internalMousePosition.x, 0.0f, width);
            self.internalMousePosition.y = Mathf.Clamp(self.internalMousePosition.y, 0.0f, height);
            self._scrollDelta = new Vector2(0.0f, self.player.GetAxis(26));
        }
        private object MPInputModule_GetMousePointerEventData(On.RoR2.UI.MPInputModule.orig_GetMousePointerEventData orig, RoR2.UI.MPInputModule self, int playerId, int mouseIndex)
        {
            // Cycle through raycasts to allow input field to be selected
            // Enable MPToggle click

            IMouseInputSource mouseInputSource = self.GetMouseInputSource(playerId, mouseIndex);

            if (mouseInputSource == null)
            {
                return null;
            }

            PlayerPointerEventData data1;

            int num = self.GetPointerData(playerId, mouseIndex, -1, out data1, true, PointerEventType.Mouse) ? 1 : 0;

            data1.Reset();

            if (num != 0)
                data1.position = self.input.mousePosition;

            Vector2 mousePosition = self.input.mousePosition;

            if (mouseInputSource.locked || !mouseInputSource.enabled)
            {
                data1.position = new Vector2(-1f, -1f);
                data1.delta = Vector2.zero;
            }
            else
            {
                data1.delta = mousePosition - data1.position;
                data1.position = mousePosition;
            }

            data1.scrollDelta = mouseInputSource.wheelDelta;
            data1.button = PointerEventData.InputButton.Left;

            self.eventSystem.RaycastAll(data1, self.m_RaycastResultCache);
            RaycastResult firstRaycast = BaseInputModule.FindFirstRaycast(self.m_RaycastResultCache);

            GameObject focusObject = null;
            
            int priority = 0;

            foreach (RaycastResult raycast in self.m_RaycastResultCache)
            {
                if (self.useCursor)
                {
                    if (raycast.gameObject != null)
                    {
                        TMPro.TMP_InputField input = raycast.gameObject.GetComponent<TMP_InputField>();
                        MPButton mpButton = raycast.gameObject.GetComponent<MPButton>();
                        HGButton hgButton = raycast.gameObject.transform?.parent.gameObject.GetComponent<HGButton>();
                        MPToggle mpToggle = raycast.gameObject.transform?.parent.gameObject.GetComponent<MPToggle>();
                        Slider selectableSlider = raycast.gameObject.transform.parent?.parent?.GetComponentInChildren<Slider>();

                        if (input != null && priority < 3)
                        {

                            focusObject = raycast.gameObject;
                            priority = 3;
                        }
                        if (hgButton != null)
                        {
                            if (priority < 2)
                            {
                                focusObject = raycast.gameObject.transform.parent.gameObject;
                                priority = 2;
                            }
                        }
                        if (mpButton != null)
                        {
                            if (priority < 1)
                            {
                                focusObject = raycast.gameObject;
                                priority = 1;
                            }
                        }
                        if (mpToggle != null)
                        {
                            if (priority < 1)
                            {
                                focusObject = raycast.gameObject.transform.parent.gameObject;
                                priority = 1;
                            }
                        }
                        if (selectableSlider != null)
                        {
                            if (priority < 1)
                            {
                                focusObject = selectableSlider.gameObject;

                                priority = 1;
                            }
                        }
                    }
                }
            }

            if (self.eventSystem.currentSelectedGameObject != null && focusObject == null)
                if (self.eventSystem.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() != null)
                    focusObject = self.eventSystem.currentSelectedGameObject;

            MPToggle toggle = focusObject?.GetComponent<MPToggle>();

            if (toggle)
            {
                MPEventSystemLocator locator = toggle.GetComponent<MPEventSystemLocator>();

                if (locator?.eventSystem)
                {
                    if (locator.eventSystem.player.GetButtonDown(4) || locator.eventSystem.player.GetButtonDown(14))
                    {
                        input.UpdateCurrentEventSystem(locator.eventSystem);
                        toggle.Set(!toggle.isOn);
                    }
                }
            }

            self.eventSystem.SetSelectedGameObject(focusObject);

            data1.pointerCurrentRaycast = firstRaycast;
            self.UpdateHover(self.m_RaycastResultCache);
            self.m_RaycastResultCache.Clear();

            PlayerPointerEventData data2;
            self.GetPointerData(playerId, mouseIndex, -2, out data2, true, PointerEventType.Mouse);
            self.CopyFromTo(data1, data2);

            data2.button = PointerEventData.InputButton.Right;

            PlayerPointerEventData data3;
            self.GetPointerData(playerId, mouseIndex, -3, out data3, true, PointerEventType.Mouse);
            self.CopyFromTo(data1, data3);
            data3.button = PointerEventData.InputButton.Middle;

            for (int index = 3; index < mouseInputSource.buttonCount; index++)
            {
                PlayerPointerEventData data4;
                self.GetPointerData(playerId, mouseIndex, index - 2147483520, out data4, true, PointerEventType.Mouse);
                self.CopyFromTo(data1, data4);
                data4.button = ~PointerEventData.InputButton.Left;

            }

            self.m_MouseState.SetButtonState(0, self.StateForMouseButton(playerId, mouseIndex, 0), data1);
            self.m_MouseState.SetButtonState(1, self.StateForMouseButton(playerId, mouseIndex, 1), data2);
            self.m_MouseState.SetButtonState(2, self.StateForMouseButton(playerId, mouseIndex, 2), data3);

            for (int index = 3; index < mouseInputSource.buttonCount; index++)
            {
                PlayerPointerEventData data4;
                self.GetPointerData(playerId, mouseIndex, index - 2147483520, out data4, false, PointerEventType.Mouse);
                self.m_MouseState.SetButtonState(index, self.StateForMouseButton(playerId, mouseIndex, index), data4);
            }

            return self.m_MouseState;
        }
        private void SurvivorIconController_Update(On.RoR2.UI.SurvivorIconController.orig_Update orig, SurvivorIconController self)
        {
            // Fix debug spam

            if (EventSystem.current == null)
                return;

            MPEventSystem system = EventSystem.current as MPEventSystem;

            if (system == null)
                return;

            orig(self);
        }
        private void SurvivorIconController_UpdateAvailability(On.RoR2.UI.SurvivorIconController.orig_UpdateAvailability orig, SurvivorIconController self)
        {
            // Combine and enable entitlements for each profile

            self.SetBoolAndMarkDirtyIfChanged(ref self.survivorIsUnlocked, SurvivorCatalog.SurvivorIsUnlockedOnThisClient(self.survivorIndex));
            self.SetBoolAndMarkDirtyIfChanged(ref self.survivorRequiredExpansionEnabled, self.survivorDef.CheckRequiredExpansionEnabled((NetworkUser)null));

            bool hasEntitlement = false;

            foreach (LocalUser user in LocalUserManager.readOnlyLocalUsersList)
            {
                hasEntitlement |= self.survivorDef.CheckUserHasRequiredEntitlement(user);
            }

            self.SetBoolAndMarkDirtyIfChanged(ref self.survivorRequiredEntitlementAvailable, hasEntitlement);
            self.survivorIsAvailable = self.survivorIsUnlocked && self.survivorRequiredExpansionEnabled && self.survivorRequiredEntitlementAvailable;
        }
        private void MPEventSystem_ValidateCurrentSelectedGameobject(On.RoR2.UI.MPEventSystem.orig_ValidateCurrentSelectedGameobject orig, RoR2.UI.MPEventSystem self)
        {
            // Disabled
            return;

            // Remove input source check
            // Remove navigation mode check

            if (!self.currentSelectedGameObject)
                return;

            MPButton component = self.currentSelectedGameObject.GetComponent<MPButton>();

            if (!component || component.CanBeSelected())
                return;

            self.SetSelectedGameObject(null);
        }
        private void CharacterSelectController_Update(On.RoR2.UI.CharacterSelectController.orig_Update orig, CharacterSelectController self)
        {
            // Update the local user to the player who last interacted with the UI

            if (input?.currentButtonEventSystem)
                self.localUser = input.currentMouseEventSystem.localUser;
                
            orig(self);
        }
        private void CharacterSelectBarController_PickIcon(On.RoR2.CharacterSelectBarController.orig_PickIcon orig, CharacterSelectBarController self, RoR2.UI.SurvivorIconController newPickedIcon)
        {
            // Update the local user to the player who last interacted with the UI

            if (self.pickedIcon == newPickedIcon)
                return;

            self.pickedIcon = newPickedIcon;

            CharacterSelectBarController.SurvivorPickInfoUnityEvent onSurvivorPicked = self.onSurvivorPicked;

            if (onSurvivorPicked == null)
                return;

            LocalUser user = input?.currentMouseEventSystem?.localUser;

            if (user is null)
                return;

            onSurvivorPicked.Invoke(new CharacterSelectBarController.SurvivorPickInfo()
            {
                localUser = user,
                pickedSurvivor = newPickedIcon.survivorDef
            });
        }
        private void ViewablesCatalog_AddNodeToRoot(On.RoR2.ViewablesCatalog.orig_AddNodeToRoot orig, ViewablesCatalog.Node node)
        {
            // Stop console spam

            node.SetParent(ViewablesCatalog.rootNode);

            foreach (ViewablesCatalog.Node descendant in node.Descendants())
                if (!ViewablesCatalog.fullNameToNodeMap.ContainsKey(descendant.fullName))
                    ViewablesCatalog.fullNameToNodeMap.Add(descendant.fullName, descendant);
        }
        private void UILayerKey_RefreshTopLayerForEventSystem(On.RoR2.UI.UILayerKey.orig_RefreshTopLayerForEventSystem orig, MPEventSystem eventSystem)
        {
            int num = int.MinValue;

            UILayerKey uiLayerKey1 = null;
            UILayerKey layerRepresentation = UILayerKey.topLayerRepresentations[eventSystem];

            List<UILayerKey> instancesList = InstanceTracker.GetInstancesList<UILayerKey>();

            //bool debug = eventSystem.player.id == 2;

            for (int index = 0; index < instancesList.Count; ++index)
            {
                UILayerKey uiLayerKey2 = instancesList[index];

                //if (debug)
                //    Log.LogDebug($"UILayerKey_RefreshTopLayerForEventSystem: Checking '{uiLayerKey2}'");

                if (!(uiLayerKey2.eventSystemLocator.eventSystem != eventSystem) && uiLayerKey2.layer.priority > num)
                {

                    uiLayerKey1 = uiLayerKey2;
                    num = uiLayerKey2.layer.priority;
                }
            }

            if (uiLayerKey1 == layerRepresentation)
                return;

            if (layerRepresentation)
            {
                layerRepresentation.onEndRepresentTopLayer.Invoke();
                layerRepresentation.representsTopLayer = false;
                //Log.LogDebug($"UILayerKey_RefreshTopLayerForEventSystem: '{layerRepresentation}' representation ended for '{eventSystem?.name}'");
            }

            UILayerKey uiLayerKey3 = UILayerKey.topLayerRepresentations[eventSystem] = uiLayerKey1;

            if (!uiLayerKey3)
                return;

            uiLayerKey3.representsTopLayer = true;
            uiLayerKey3.onBeginRepresentTopLayer.Invoke();
            //Log.LogDebug($"UILayerKey_RefreshTopLayerForEventSystem: '{layerRepresentation}' representation began for '{eventSystem?.name}'");
        }
        private NetworkUser RuleChoiceController_FindNetworkUser(On.RoR2.UI.RuleChoiceController.orig_FindNetworkUser orig, RuleChoiceController self)
        {
            // Use input.currentEventSystem


            return input?.currentButtonEventSystem?.localUser.currentNetworkUser;
        }
        private void LoadoutPanelController_UpdateDisplayData(On.RoR2.UI.LoadoutPanelController.orig_UpdateDisplayData orig, RoR2.UI.LoadoutPanelController self)
        {
            // Use input.currentEventSystem

            UserProfile userProfile = input?.currentMouseEventSystem?.localUser?.userProfile;
            NetworkUser currentNetworkUser = input?.currentMouseEventSystem?.localUser?.currentNetworkUser;

            BodyIndex bodyIndex = (currentNetworkUser) ? currentNetworkUser.bodyIndexPreference : BodyIndex.None;

            self.SetDisplayData(new RoR2.UI.LoadoutPanelController.DisplayData()
            {
                userProfile = userProfile,
                bodyIndex = bodyIndex
            });
        }
        private void RunCameraManager_Update(On.RoR2.RunCameraManager.orig_Update orig, RunCameraManager self)
        {
            // Set screens to desired areas

            bool instance = Stage.instance;

            if (instance)
            {
                int index = 0;

                for (int count = CameraRigController.readOnlyInstancesList.Count; index < count; ++index)
                    if (CameraRigController.readOnlyInstancesList[index].suppressPlayerCameras)
                        return;
            }

            if (instance)
            {
                int index1 = 0;

                System.Collections.ObjectModel.ReadOnlyCollection<NetworkUser> localPlayersList = NetworkUser.readOnlyLocalPlayersList;

                for (int index2 = 0; index2 < localPlayersList.Count; ++index2)
                {
                    NetworkUser networkUser = localPlayersList[index2];
                    CameraRigController cameraRigController = self.cameras[index1];

                    if (!cameraRigController)
                    {
                        cameraRigController = Instantiate<GameObject>(LegacyResourcesAPI.Load<GameObject>("Prefabs/Main Camera")).GetComponent<CameraRigController>();
                        cameraRigController.viewport = configuration.GetScreenRectByLocalId(index2);

                        if (Display.displays.Length > 1)
                        {
                            cameraRigController.sceneCam.targetDisplay = configuration.GetDisplayIdByLocalId(index2);
                            cameraRigController.uiCam.targetDisplay = configuration.GetDisplayIdByLocalId(index2);
                        }

                        self.cameras[index1] = cameraRigController;
                    }

                    cameraRigController.viewer = networkUser;

                    networkUser.cameraRigController = cameraRigController;

                    GameObject networkUserBodyObject = RunCameraManager.GetNetworkUserBodyObject(networkUser);

                    ForceSpectate forceSpectate = InstanceTracker.FirstOrNull<ForceSpectate>();

                    if (forceSpectate)
                    {
                        cameraRigController.nextTarget = forceSpectate.target;
                        cameraRigController.cameraMode = RoR2.CameraModes.CameraModePlayerBasic.spectator;
                    }
                    else if (networkUserBodyObject)
                    {
                        cameraRigController.nextTarget = networkUserBodyObject;
                        cameraRigController.cameraMode = RoR2.CameraModes.CameraModePlayerBasic.playerBasic;
                    }
                    else if (!cameraRigController.disableSpectating)
                    {
                        cameraRigController.cameraMode = RoR2.CameraModes.CameraModePlayerBasic.spectator;
                        if (!cameraRigController.target)
                            cameraRigController.nextTarget = CameraRigControllerSpectateControls.GetNextSpectateGameObject(networkUser, null);
                    }
                    else
                        cameraRigController.cameraMode = RoR2.CameraModes.CameraModeNone.instance;

                    ++index1;
                }

                int index3 = index1;

                for (int index2 = index1; index2 < self.cameras.Length; ++index2)
                {
                    ref CameraRigController local = ref self.cameras[index1];

                    if (local != null)
                    {
                        if (local)
                            Destroy(self.cameras[index1].gameObject);
                        local = null;
                    }
                }

                for (int index2 = 0; index2 < index3; ++index2)
                {
                    //self.cameras[index2].viewport = configuration.GetScreenRectByLocalId(index2);
                }
            }
            else
            {
                for (int index = 0; index < self.cameras.Length; ++index)
                {
                    if (self.cameras[index])
                        Destroy(self.cameras[index].gameObject);
                }
            }
        }
        private void CameraRigController_Start(On.RoR2.CameraRigController.orig_Start orig, CameraRigController self)
        {
            orig(self);

            if (configuration is null)
                return;

            Assignment[] assignments = configuration.assignments.Where(a => a.displayId != 0).ToArray();

            foreach (Assignment assignment in assignments)
            {
                if (assignment.displayId <= 0 || assignment.displayId >= Display.displays.Length)
                    continue;

                Display currentDisplay = Display.displays[assignment.displayId];

                if (!currentDisplay.active)
                    currentDisplay.Activate();
            }
        }
        private void LocalCameraEffect_OnUICameraPreCull(On.RoR2.LocalCameraEffect.orig_OnUICameraPreCull orig, UICamera uiCamera)
        {
            // Disable death effect for players still alive
            // TODO this is broken



            for (int index = 0; index < LocalCameraEffect.instancesList.Count; index++)
            {
                GameObject target = uiCamera?.cameraRigController?.target;
                LocalCameraEffect instance = LocalCameraEffect.instancesList[index];
                HealthComponent component = uiCamera?.cameraRigController?.localUserViewer?.cachedBody?.healthComponent;

                if (!target || !component || !instance.targetCharacter)
                    continue;

                if (instance.targetCharacter == target && component.alive)
                    instance.effectRoot.SetActive(true);
                else
                    instance.effectRoot.SetActive(false);
            }
        }
        private void CombatHealthBarViewer_SetLayoutHorizontal(On.RoR2.UI.CombatHealthBarViewer.orig_SetLayoutHorizontal orig, RoR2.UI.CombatHealthBarViewer self)
        {
            // Another iDeathHD fix

            UICamera uiCamera = self.uiCamera;

            if (!uiCamera)
                return;

            self.UpdateAllHealthbarPositions(uiCamera.cameraRigController.sceneCam, uiCamera.camera);
        }
        private void NetworkUser_UpdateUserName(On.RoR2.NetworkUser.orig_UpdateUserName orig, RoR2.NetworkUser self)
        {
            if (self.localUser == null)
            {
                self.userName = self.GetNetworkPlayerName().GetResolvedName();
            }
            else
            {
                self.userName = self.localUser.userProfile.name;
            }
        }
        private NetworkPlayerName NetworkUser_GetNetworkPlayerName(On.RoR2.NetworkUser.orig_GetNetworkPlayerName orig, RoR2.NetworkUser self)
        {
            // TODO is this necessary?
            NetworkPlayerName name = new NetworkPlayerName()
            {
                nameOverride = self.id.strValue != null ? self.id.strValue : (string)null,
                steamId = !string.IsNullOrEmpty(self.id.strValue) ? new CSteamID() : new CSteamID(self.id.value)
            }; 

            if (self.localUser != null)
            {
            //    name.nameOverride = self.localUser?.userProfile.name;
            }

            return name;
        }
        private string PlayerCharacterMasterController_GetDisplayName(On.RoR2.PlayerCharacterMasterController.orig_GetDisplayName orig, RoR2.PlayerCharacterMasterController self)
        {
            string name = "";

            if (self.networkUserObject)
            {
                NetworkUser networkUser = self.networkUserObject.GetComponent<NetworkUser>();

                if (networkUser)
                {
                    if (networkUser.localUser == null)
                    {
                        name = networkUser.userName;
                    }
                    else
                    {
                        name = networkUser.localUser.userProfile.name;
                    }
                }
            }

            return name;
        }
        private void Nameplate_LateUpdate(On.RoR2.UI.Nameplate.orig_LateUpdate orig, RoR2.UI.Nameplate self)
        {
            string str = "";

            Color baseColor = self.baseColor;

            bool flag1 = true;
            bool flag2 = false;
            bool flag3 = false;

            int localUserIndex = -1;

            if (self.body)
            {
                str = self.body.GetDisplayName();

                flag1 = self.body.healthComponent.alive;
                flag2 = !self.body.outOfCombat || !self.body.outOfDanger;
                flag3 = self.body.healthComponent.isHealthLow;

                CharacterMaster master = self.body.master;

                if (master)
                {
                    PlayerCharacterMasterController component1 = master.GetComponent<PlayerCharacterMasterController>();

                    if (component1)
                    {
                        GameObject networkUserObject = component1.networkUserObject;

                        if (networkUserObject)
                        {
                            NetworkUser component2 = networkUserObject.GetComponent<NetworkUser>();

                            if (component2)
                            {
                                str = component2.userName;

                                if (component2.localUser != null)
                                {
                                    str = component2.localUser.userProfile.name;
                                    localUserIndex = component2.localUser.id;
                                }
                            }
                        }
                    }
                    else
                        str = RoR2.Language.GetString(self.body.baseNameToken);
                }
            }

            Color color = flag2 ? self.combatColor : localUserIndex > -1 ? ColorCatalog.GetMultiplayerColor(localUserIndex) : self.baseColor;

            self.aliveObject.SetActive(flag1);
            self.deadObject.SetActive(!flag1);

            if (self.criticallyHurtSpriteRenderer)
            {
                self.criticallyHurtSpriteRenderer.enabled = flag3 & flag1;
                self.criticallyHurtSpriteRenderer.color = HealthBar.GetCriticallyHurtColor();
            }

            if (self.label)
            {
                self.label.text = str;
                self.label.color = color;
            }

            foreach (SpriteRenderer coloredSprite in self.coloredSprites)
                coloredSprite.color = color;
        }
        private string SubjectChatMessage_GetSubjectName(On.RoR2.SubjectChatMessage.orig_GetSubjectName orig, SubjectChatMessage self)
        {
            if (self.subjectAsNetworkUser)
            {
                if (self.subjectAsNetworkUser.localUser != null)
                    return Util.EscapeRichTextForTextMeshPro(self.subjectAsNetworkUser.localUser.userProfile.name);

                return Util.EscapeRichTextForTextMeshPro(self.subjectAsNetworkUser.userName);
            }

            if (self.subjectAsCharacterBody)
                return self.subjectAsCharacterBody.GetDisplayName();

            return "???";
        }
        private void InputBindingDisplayController_Refresh(On.RoR2.InputBindingDisplayController.orig_Refresh orig, InputBindingDisplayController self, bool forceRefresh)
        {
            // TODO use IL hook

            if (input is null)
            {
                orig(self, forceRefresh);
                return;
            }

            MPEventSystem eventSystem = input.currentMouseEventSystem;

            if (!eventSystem || Run.instance)
            {
                MPEventSystem eventSystemOverride = self.eventSystemLocator?.eventSystem;

                if (eventSystemOverride)
                {
                    eventSystem = eventSystemOverride;
                }

                if (!eventSystem)
                {
                    Log.LogOutput("InputBindingDisplayController_Refresh: MPEventSystem is invalid.", Log.LogLevel.Warning);
                    return;
                }
            }

            if (!forceRefresh && eventSystem == self.lastEventSystem && eventSystem.currentInputSource == self.lastInputSource)
                return;

            //if (eventSystem.currentInputSource == MPEventSystem.InputSource.MouseAndKeyboard) // Removed for settings screen
            //    return;

            if (self.useExplicitInputSource)
            {
                InputBindingDisplayController.sharedStringBuilder.Clear();
                InputBindingDisplayController.sharedStringBuilder.Append(Glyphs.GetGlyphString(eventSystem, self.actionName, self.axisRange, self.explicitInputSource));
            }
            else
            {
                InputBindingDisplayController.sharedStringBuilder.Clear();
                InputBindingDisplayController.sharedStringBuilder.Append(Glyphs.GetGlyphString(eventSystem, self.actionName, AxisRange.Full));
            }

            if (self.guiLabel)
                self.guiLabel.SetText(InputBindingDisplayController.sharedStringBuilder);

            else if (self.label)
                self.label.SetText(InputBindingDisplayController.sharedStringBuilder);

            self.lastEventSystem = eventSystem;
            self.lastInputSource = eventSystem.currentInputSource;
        }
        private void InputSourceFilter_Refresh(On.RoR2.UI.InputSourceFilter.orig_Refresh orig, InputSourceFilter self, bool forceRefresh)
        {
            if (self.eventSystem?.currentInputSource != MPEventSystem.InputSource.Gamepad || Run.instance)
                orig(self, forceRefresh);

            return;

            //
            MPEventSystem.InputSource? currentInputSource = input.currentMouseEventSystem?.currentInputSource;

            if (Run.instance)
                currentInputSource = self.eventSystem?.currentInputSource;

            MPEventSystem.InputSource requiredInputSource = self.requiredInputSource;

            bool flag = currentInputSource.GetValueOrDefault() == requiredInputSource & currentInputSource.HasValue;

            if (flag != self.wasOn | forceRefresh)
            {
                for (int index = 0; index < self.objectsToFilter.Length; ++index)
                    self.objectsToFilter[index].SetActive(flag);
            }

            self.wasOn = flag;
        }
        private void HGGamepadInputEvent_Update(On.RoR2.UI.HGGamepadInputEvent.orig_Update orig, HGGamepadInputEvent self)
        {
            bool flag = self.CanAcceptInput();

            if (self.couldAcceptInput != flag)
            {
                foreach (GameObject gameObject in self.enabledObjectsIfActive)
                    gameObject.SetActive(flag);
            }

            if (self.CanAcceptInput() && self.eventSystem.player.GetButtonDown(self.actionName))
                self.actionEvent.Invoke();

            self.couldAcceptInput = flag;
        }
        private Color ColorCatalog_GetMultiplayerColor(On.RoR2.ColorCatalog.orig_GetMultiplayerColor orig, int playerSlot)
        {
            if (configuration is null)
                return orig(playerSlot);

            Assignment? assignment = configuration.GetAssignmentByLocalId(playerSlot);

            if (assignment.HasValue)
                return assignment.Value.color;

            return orig(playerSlot);
        }
        private UserProfile BaseSettingsControl_GetCurrentUserProfile(On.RoR2.UI.BaseSettingsControl.orig_GetCurrentUserProfile orig, BaseSettingsControl self)
        {
            if (input.currentMouseEventSystem is null)
                return orig(self);

            return input.currentMouseEventSystem.localUser.userProfile;
        }
        private void ProfileNameLabel_LateUpdate(On.RoR2.UI.ProfileNameLabel.orig_LateUpdate orig, ProfileNameLabel self)
        {
            if (input is null)
            {
                orig(self);
                return;
            }

            string str = input.currentMouseEventSystem?.localUser?.userProfile.name ?? string.Empty;

            if (str == self.currentUserName)
                return;

            self.currentUserName = str;
            self.label.text = RoR2.Language.GetStringFormatted(self.token, self.currentUserName);
        }
        private string Util_GetBestMasterName(On.RoR2.Util.orig_GetBestMasterName orig, CharacterMaster characterMaster)
        {
            if (!characterMaster)
                return "Null Master";

            string userName = null;

            if (characterMaster.playerCharacterMasterController?.networkUser)
            {
                if (characterMaster.playerCharacterMasterController.networkUser.isLocalPlayer)
                    userName = characterMaster.playerCharacterMasterController?.networkUser.localUser.userProfile.name;
                else
                    userName = characterMaster.playerCharacterMasterController.networkUser.userName;
            }

            if(userName is null)
            {
                string baseNameToken = characterMaster.bodyPrefab?.GetComponent<CharacterBody>().baseNameToken;

                if (baseNameToken != null)
                    userName = RoR2.Language.GetString(baseNameToken);
            }

            return userName;
        }
        private void InputBindingControl_StartListening(On.RoR2.UI.InputBindingControl.orig_StartListening orig, InputBindingControl self)
        {
            // TODO is this used?

            if (!self.button.IsInteractable())
                return;
            self.inputMapperHelper.Stop();
            self.currentPlayer = input.currentMouseEventSystem.player;
            if (self.currentPlayer == null)
                return;
            IList<Controller> controllers = (IList<Controller>)null;
            switch (self.inputSource)
            {
                case MPEventSystem.InputSource.MouseAndKeyboard:
                    controllers = (IList<Controller>)new Controller[2]
                    {
            (Controller) self.currentPlayer.controllers.Keyboard,
            (Controller) self.currentPlayer.controllers.Mouse
                    };
                    break;
                case MPEventSystem.InputSource.Gamepad:
                    controllers = (IList<Controller>)self.currentPlayer.controllers.Joysticks.ToArray<Joystick>();
                    break;
            }
            self.inputMapperHelper.Start(self.currentPlayer, controllers, self.action, self.axisRange);
            if (!(bool)(self.button))
                return;
            self.button.interactable = false;
        }
        private void ScoreboardController_Awake(On.RoR2.UI.ScoreboardController.orig_Awake orig, ScoreboardController self)
        {
            orig(self);
            self.transform.GetComponentInChildren<PostProcessDuration>().gameObject.SetActive(false);
        }
        private void RuleCategoryController_SetRandomVotes(On.RoR2.UI.RuleCategoryController.orig_SetRandomVotes orig, RuleCategoryController self)
        {
            if (input is null)
            {
                orig(self);
                return;
            }

            PreGameRuleVoteController forUser = PreGameRuleVoteController.FindForUser(input.currentMouseEventSystem?.localUser?.currentNetworkUser);

            if (!forUser)
                return;

            List<RuleChoiceDef> ruleChoiceDefList = new List<RuleChoiceDef>();

            foreach (RuleDef child in self.currentCategory.children)
            {
                ruleChoiceDefList.Clear();

                foreach (RuleChoiceDef choice in child.choices)
                {
                    if (self.cachedAvailability[choice.globalIndex])
                        ruleChoiceDefList.Add(choice);
                }

                int choiceValue = -1;

                if (ruleChoiceDefList.Count > 0 && UnityEngine.Random.value > 0.5)
                    choiceValue = ruleChoiceDefList[UnityEngine.Random.Range(0, ruleChoiceDefList.Count)].localIndex;

                forUser.SetVote(child.globalIndex, choiceValue);
            }
        }

        private void InputBindingControl_Update_IL(bool status)
        {
            if (status)
            {
                IL.RoR2.UI.InputBindingControl.StartListening += (il) =>
                {
                    ILCursor c = new ILCursor(il);
                    c.GotoNext(
                            x => x.MatchLdarg(0),
                            x => x.MatchLdarg(0),
                            x => x.MatchLdfld<InputBindingControl>("eventSystemLocator"),
                            x => x.MatchCallvirt<MPEventSystemLocator>("get_eventSystem"),
                            x => x.MatchDup()
                        );
                    c.Index += 4;
                    Log.LogOutput($"Instruction: '{c}'");
                    c.Next.Operand = input.currentMouseEventSystem;
                };
                Log.LogOutput($"Enabled hook");
            }
            else
            {
                IL.RoR2.UI.InputBindingControl.StopListening -= (il) =>
                {
                    ILCursor c = new ILCursor(il);
                    c.GotoNext(
                            x => x.MatchLdarg(0),
                            x => x.MatchLdarg(0),
                            x => x.MatchLdfld<InputBindingControl>("eventSystemLocator"),
                            x => x.MatchCallvirt<MPEventSystemLocator>("get_eventSystem"),
                            x => x.MatchDup()
                        );
                    c.Index += 4;
                    c.Next.Operand = input.currentMouseEventSystem;
                };
                Log.LogOutput($"Disabled hook");
            }
        }
        private void InputMapperHelper_StartListening_IL(bool status)
        {
            // This doesn't work
            if (status)
            {
                IL.RoR2.UI.InputBindingControl.StartListening += (il) =>
                {
                    ILCursor c = new ILCursor(il);
                    c.GotoNext(
                            x => x.MatchLdarg(0),
                            x => x.MatchLdarg(0),
                            x => x.MatchLdfld<InputBindingControl>("eventSystemLocator"),
                            x => x.MatchCallvirt<MPEventSystemLocator>("get_eventSystem"),
                            x => x.MatchDup()
                        );
                    c.Index += 4;

                    c.Remove();
                    c.Emit(OpCodes.Ldloc_1);
                    c.EmitDelegate<Func<InputBindingControl, EventSystem>>((ib) =>
                    {
                        return input.currentMouseEventSystem;
                    });
                };
            }
            else
            {
                IL.RoR2.UI.InputBindingControl.StartListening -= (il) =>
                {
                    ILCursor c = new ILCursor(il);
                    c.GotoNext(
                            x => x.MatchLdarg(0),
                            x => x.MatchLdarg(0),
                            x => x.MatchLdfld<InputBindingControl>("eventSystemLocator"),
                            x => x.MatchCallvirt<MPEventSystemLocator>("get_eventSystem"),
                            x => x.MatchDup()
                        );
                    c.Index += 4;

                    c.Remove();
                    c.Emit(OpCodes.Ldloc_1);
                    c.EmitDelegate<Func<InputBindingControl, EventSystem>>((ib) =>
                    {
                        return input.currentMouseEventSystem;
                    });
                };
            }

            return;
            if (status)
            {
                IL.RoR2.UI.InputBindingControl.StartListening += (il) =>
                {
                    ILCursor c = new ILCursor(il);
                    c.GotoNext(
                            x => x.MatchLdarg(0),
                            x => x.MatchLdarg(0),
                            x => x.MatchLdfld<InputBindingControl>("eventSystemLocator"),
                            x => x.MatchCallvirt<MPEventSystemLocator>("get_eventSystem"),
                            x => x.MatchDup()
                        );
                    c.Index += 4;
                    Log.LogOutput($"Start Instruction: '{c}'");
                    c.Next.Operand = input.currentMouseEventSystem;
                    Log.LogOutput($"Next operand = '{c.Next.Operand}'");
                };
            }
            else
            {
                IL.RoR2.UI.InputBindingControl.StartListening -= (il) =>
                {
                    ILCursor c = new ILCursor(il);
                    c.GotoNext(
                            x => x.MatchLdarg(0),
                            x => x.MatchLdarg(0),
                            x => x.MatchLdfld<InputBindingControl>("eventSystemLocator"),
                            x => x.MatchCallvirt<MPEventSystemLocator>("get_eventSystem"),
                            x => x.MatchDup()
                        );
                    c.Index += 4;
                    Log.LogOutput($"Stop Instruction: '{c}'");
                    c.Next.Operand = input.currentMouseEventSystem;
                };
            }
        }
        #endregion

        #endregion

        #endregion

        #region UI
        private bool CreateUI()
        {
            Log.LogOutput($"XSplitScreen.CreateUI: Attempting to create UI", Log.LogLevel.Info);

            if (CreateMainMenuButton())
            {
                CreateMenu();
                return true;
            }

            return false;
        }
        private bool CreateMainMenuButton()
        {
            if (titleButton != null)
                return false;

            GameObject template = GameObject.Find("GenericMenuButton (Singleplayer)");

            if (template is null)
                return false;

            GameObject newXButton = Instantiate(template);

            newXButton.name = "(XButton) XSplitScreen";

            newXButton.transform.SetParent(template.transform.parent);
            newXButton.transform.SetSiblingIndex(1);
            newXButton.transform.localScale = Vector3.one;

            XButtonConverter converter = newXButton.AddComponent<XButtonConverter>();
            converter.Initialize();

            converter.onClickMono.AddListener(OpenMenu);
            converter.hoverToken = Language.MSG_HOVER_TOKEN;
            converter.token = Language.MSG_TITLE_BUTTON_TOKEN;

            titleButton = newXButton.GetComponent<RectTransform>();

            return true;
        }
        private void CreateMenu()
        {
            if (menuContainer != null)
                return;

            menuContainer = new GameObject("MENU: XSplitScreen", typeof(RectTransform)).GetComponent<RectTransform>();
            menuContainer.SetParent(MainMenuController.instance.transform);
            
            GameObject menu = new GameObject("XSplitScreenMenu", typeof(RectTransform));
            menu.transform.SetParent(menuContainer);

            var screen = menu.AddComponent<XSplitScreenMenu>();
            screen.Initialize();
            screen.onEnter.AddListener(ScreenOnEnter);
            screen.onExit.AddListener(ScreenOnExit);

            menu.gameObject.SetActive(false);
        }
        private void OpenMenu(MonoBehaviour mono)
        {
            RoR2.UI.MainMenu.MainMenuController.instance.SetDesiredMenuScreen(XSplitScreenMenu.instance);
        }
        #endregion

        #region Splitscreen Logic
        public void UpdateCursorStatus(bool status)
        {
            CursorOpener[] openers = FindObjectsOfType<CursorOpener>();

            foreach (CursorOpener opener in openers)
            {
                if(!opener.name.Contains("XSplit"))
                    opener.forceCursorForGamePad = status;
            }

            if (!status)
                foreach (MPEventSystem instance in MPEventSystem.instancesList)
                    instance.SetSelectedGameObject(null);
        }
        private VerifyStatus SetEnabled(bool requestedStatus, out int localPlayerCount)
        {
            if (configuration != null)
                localPlayerCount = configuration.currentLocalPlayerCount;
            else
                localPlayerCount = 0;

            if (configuration is null)
                requestedStatus = false;

            UserProfile[] profiles = new UserProfile[PlatformSystems.saveSystem.loadedUserProfiles.Values.Count];
            PlatformSystems.saveSystem.loadedUserProfiles.Values.CopyTo(profiles, 0);

            VerifyStatus verifyStatus = VerifyStatus.Fail;

            if (profiles.Length == 0)
            {
                Log.LogOutput($"XSplitScreen.SetEnabled: No profiles found. Unable to log in local users.", Log.LogLevel.Warning);
                return verifyStatus;
            }

            if (!LogInUsers(profiles, requestedStatus, out localPlayerCount, out verifyStatus))
                return verifyStatus;

            AssignControllers(requestedStatus);

            ToggleSplitScreenHooks(requestedStatus);
            
            return verifyStatus;
        }
        private bool LogInUsers(UserProfile[] profiles, bool requestedStatus, out int localPlayerCount, out VerifyStatus verifyStatus)
        {
            List<LocalUserManager.LocalUserInitializationInfo> localUsers = new List<LocalUserManager.LocalUserInitializationInfo>();

            localPlayerCount = 1;

            if (!requestedStatus)
            {
                localUsers.Add(new LocalUserManager.LocalUserInitializationInfo()
                {
                    player = ReInput.players.GetPlayer(0),
                    profile = PlatformSystems.saveSystem.loadedUserProfiles.First().Value,
                });
            }
            else
            {
                int localId = 1;

                Assignment[] assignments = new Assignment[configuration.assignments.Count];
                configuration.assignments.CopyTo(assignments);
                
                foreach (Assignment assignment in assignments)
                {
                    if (assignment.isAssigned)
                    {
                        localUsers.Add(new LocalUserManager.LocalUserInitializationInfo()
                        {
                            player = ReInput.players.GetPlayer(localId),
                            profile = profiles[assignment.profileId],
                        });

                        configuration.SetLocalId(assignment.playerId, localId - 1);
                        Log.LogOutput($"LogInUsers: Logged in user for '{assignment}'");
                        localId++;
                    }
                }
            }

            for (int indexA = 0; indexA < localUsers.Count; indexA++)
            {
                for (int indexB = indexA + 1; indexB < localUsers.Count; indexB++)
                {
                    if (localUsers[indexA].profile is null || localUsers[indexB].profile is null)
                        continue;

                    if (string.Compare(localUsers[indexA].profile.fileName, localUsers[indexB].profile.fileName) == 0)
                    {
                        Log.LogOutput($"LogInUsers: Unable to assign profile '{localUsers[indexA].profile.name}' to multiple local users", Log.LogLevel.Message);
                        verifyStatus = VerifyStatus.InvalidProfile;
                        return false;
                    }
                }
            }

            try
            {
                // Silence log spam
                On.RoR2.ViewablesCatalog.AddNodeToRoot += ViewablesCatalog_AddNodeToRoot;

                LocalUserManager.ClearUsers();
                LocalUserManager.SetLocalUsers(localUsers.ToArray());

                On.RoR2.ViewablesCatalog.AddNodeToRoot -= ViewablesCatalog_AddNodeToRoot;

                localPlayerCount = localUsers.Count;
                verifyStatus = VerifyStatus.Success;
                return true;
            }
            catch (Exception e)
            {
                Log.LogOutput(e, Log.LogLevel.Error);

                verifyStatus = VerifyStatus.Fail;

                if(localUsers.Count > 1)
                    LogInUsers(null, false, out localPlayerCount, out verifyStatus);

                verifyStatus = VerifyStatus.Fail;

                return false;
            }
        }
        private void AssignControllers(bool status)
        {
            if (!status || configuration is null)
            {
                Log.LogOutput($"AssignControllers: Auto assigned", Log.LogLevel.Info);
                ReInput.controllers.AutoAssignJoysticks();
                PrintControllers();
                return;
            }

            // PrintControllers();

            bool keyboardAssigned = false;

            foreach (Assignment assignment in configuration.assignments)
            {
                if (!assignment.isAssigned || assignment.localId < 0)
                    continue;

                int playerIndex = assignment.localId;

                LocalUserManager.readOnlyLocalUsersList[playerIndex].inputPlayer.controllers.ClearAllControllers();

                if (assignment.controller.type == ControllerType.Keyboard)
                {
                    keyboardAssigned = true;

                    foreach (Controller controller in ReInput.controllers.Controllers)
                    {
                        if (controller.type == ControllerType.Mouse)
                        {
                            LocalUserManager.readOnlyLocalUsersList[playerIndex].inputPlayer.controllers.AddController(controller, false);
                            break;
                        }
                    }
                }

                LocalUserManager.readOnlyLocalUsersList[playerIndex].inputPlayer.controllers.AddController(assignment.controller, false);
                LocalUserManager.readOnlyLocalUsersList[playerIndex].ApplyUserProfileBindingsToRewiredPlayer();

                Log.LogOutput($"AssignControllers: Assigned controller '{assignment.controller.name}' to localId '{playerIndex}'");
            }

            if (!keyboardAssigned)
            {
                foreach (Controller controller in ReInput.controllers.Controllers)
                {
                    if (controller.type == ControllerType.Mouse || controller.type == ControllerType.Keyboard)
                    {
                        Log.LogOutput($"Keyboard not assigned - adding to first local player");
                        LocalUserManager.GetFirstLocalUser().inputPlayer.controllers.AddController(controller, false);
                        LocalUserManager.GetFirstLocalUser().ApplyUserProfileBindingsToRewiredPlayer();
                    }
                }
            }

           // PrintControllers();
        }
        private void PrintControllers()
        {
            Log.LogOutput($"PrintControllers: readOnlyLocalUsersList");
            for (int e = 0; e < LocalUserManager.readOnlyLocalUsersList.Count; e++)
            {
                foreach (Controller controller in LocalUserManager.readOnlyLocalUsersList[e].inputPlayer.controllers.Controllers)
                {
                    Log.LogOutput($"PrintControllers: Player '{LocalUserManager.readOnlyLocalUsersList[e].inputPlayer.name}' has controller '{controller}'");
                }
            }

            Log.LogOutput($"PrintControllers: ReInput players");
            foreach (Player player in ReInput.players.AllPlayers)
            {
                
                foreach (Controller controller in player.controllers.Controllers)
                {
                    Log.LogOutput($"PrintControllers: '{player.name}' <- '{controller.name}'");
                }
            }

            Log.LogOutput($"PrintControllers: MPInputModules");

            foreach (MPEventSystem eventSystem in MPEventSystem.readOnlyInstancesList)
            {
                Log.LogOutput($"PrintControllers: '{eventSystem.name}' currentInputSource = '{eventSystem.currentInputSource}'");
            }
            
        }
        #endregion

        #region Coroutines
        IEnumerator WaitForRewired()
        {
            while (!ReInput.initialized)
                yield return null;

            readyToInitializePlugin = true;

            yield return null;
        }
        IEnumerator WaitForMenu()
        {
            while (MainMenuController.instance == null)
                yield return null;

            GameObject singleplayerButton = null;

            while(singleplayerButton is null)
            {
                singleplayerButton = GameObject.Find("GenericMenuButton (Singleplayer)");
                yield return null;
            }

            buttonTemplate = Instantiate(singleplayerButton);
            buttonTemplate.SetActive(false);

            Log.LogOutput($"XSplitScreen.WaitForMenu: Ready to create UI");
            readyToCreateUI = true;

            yield return null;
        }
        #endregion

        #region Definitions
        public class AssignmentEvent : UnityEvent<Controller, Assignment> { }

        /// <summary>
        /// This API is unreliable and subject to change! 
        /// </summary>
        [System.Serializable]
        public class Configuration
        {
            #region Variables
            public UnityEvent onConfigurationUpdated { get; private set; }
            public UnityEvent onSplitScreenEnabled { get; private set; }
            public UnityEvent onSplitScreenDisabled { get; private set; }

            public Action<ControllerStatusChangedEventArgs> onControllerConnected;
            public Action<ControllerStatusChangedEventArgs> onControllerDisconnected;

            public List<Assignment> assignments { get; private set; }
            public List<Controller> controllers { get; private set; }

            internal int2 graphDimensions { get; private set; }

            public bool enabled { get; private set; }

            public int assignedPlayerCount
            {
                get
                {
                    int value = 0;

                    foreach (Assignment assignment in assignments)
                    {
                        if (assignment.isAssigned)
                            value++;
                    }

                    return value;
                }
            }
            public int currentLocalPlayerCount { get; private set; }

            public readonly int maxLocalPlayers = 4;

            private bool developerMode = false;

            private List<Preference> preferences;

            private ConfigFile config;

            private ConfigEntry<string> preferencesConfig;
            private ConfigEntry<bool> enabledConfig;
            #endregion

            #region Unity Methods
            public Configuration(ConfigFile configFile)
            {
                InitializeReferences();
                LoadConfigFile(configFile);
                InitializeAssignments();
                Save();
            }
            public void Destroy()
            {
                ToggleListeners(false);

                if(developerMode)
                    ClearSave();

                Save();
            }
            #endregion

            #region Initialization & Exit
            private void ClearSave()
            {
                preferences.Clear();
                assignments.Clear();
                InitializeAssignments();
                Log.LogOutput($"Configuration.ClearSave");
            }
            private void InitializeReferences()
            {
                assignments = new List<Assignment>();
                controllers = new List<Controller>();

                preferences = new List<Preference>();

                onConfigurationUpdated = new UnityEvent();
                onSplitScreenEnabled = new UnityEvent();
                onSplitScreenDisabled = new UnityEvent();

                graphDimensions = new int2(3, 3);

                ToggleListeners(true);
                UpdateActiveControllers();
            }
            private void LoadConfigFile(ConfigFile configFile)
            {
                this.config = configFile;

                try
                {
                    enabledConfig = configFile.Bind<bool>("General", "Enabled", false, "Should splitscreen automatically enable using last saved configuration if controllers are available?");
                    preferencesConfig = configFile.Bind<string>("Preferences", "Assignments", "", "Changes may break the mod! Color values range from 0.0 to 1.0");
                }
                catch(Exception e)
                {
                    Log.LogOutput(e, Log.LogLevel.Error);
                }

                if(preferencesConfig.Value.Length > 0)
                {
                    try
                    {
                        Wrapper wrapper = JsonUtility.FromJson<Wrapper>(preferencesConfig.Value);

                        for(int e = 0; e < wrapper.displayId.Length; e++)
                        {
                            preferences.Add(new Preference()
                            {
                                position = new int2(wrapper.positionX[e], wrapper.positionY[e]),
                                displayId = wrapper.displayId[e],
                                playerId = wrapper.playerId[e],
                                profileId = wrapper.profileId[e],
                                color = new Color(wrapper.colorR[e], wrapper.colorG[e], wrapper.colorB[e], wrapper.colorA[e]),
                            });
                        }

                        Log.LogOutput($"Loaded '{wrapper.displayId.Length}' saved player preferences.", Log.LogLevel.Message);
                    }
                    catch(Exception e)
                    {
                        Log.LogOutput(e, Log.LogLevel.Error);
                    }
                }
                else
                {
                    Log.LogOutput($"No player preferences found.", Log.LogLevel.Message);
                }
            }
            private void InitializeAssignments()
            {
                Controller[] availableControllers = controllers.ToArray();

                if(preferences.Count == 0)
                {
                    Log.LogOutput($"Creating default preferences.", Log.LogLevel.Message);

                    for(int e = 0; e < maxLocalPlayers; e++)
                    {
                        if(e == 0)
                        {
                            preferences.Add(new Preference()
                            {
                                position = int2.one,
                                displayId = 0,
                                playerId = 0,
                                profileId = 0,
                                color = ColorCatalog.GetMultiplayerColor(e)
                            });
                        }
                        else
                        {
                            preferences.Add(new Preference()
                            {
                                position = int2.negative,
                                displayId = -1,
                                playerId = e,
                                profileId = -1,
                                color = ColorCatalog.GetMultiplayerColor(e)
                            });
                        }
                    }
                }

                for (int preferenceId = 0; preferenceId < preferences.Count; preferenceId++)
                {
                    if(preferenceId < availableControllers.Length) // If controllers are available
                        LoadAssignment(preferenceId, availableControllers[preferenceId]);
                    else
                        LoadAssignment(preferenceId, null);
                }
            }
            private void ToggleListeners(bool status)
            {
                if (status)
                {
                    ReInput.ControllerConnectedEvent += OnControllerConnected;
                    ReInput.ControllerDisconnectedEvent += OnControllerDisconnected;
                    onSplitScreenEnabled.AddListener(OnSplitScreenEnabled);
                    onSplitScreenDisabled.AddListener(OnSplitScreenDisabled);
                }
                else
                {
                    ReInput.ControllerConnectedEvent -= OnControllerConnected;
                    ReInput.ControllerDisconnectedEvent -= OnControllerDisconnected;
                    onSplitScreenEnabled.RemoveListener(OnSplitScreenEnabled);
                    onSplitScreenDisabled.RemoveListener(OnSplitScreenDisabled);
                }
            }
            #endregion

            #region Events
            internal void OnControllerConnected(Rewired.ControllerStatusChangedEventArgs args)
            {
                if (args.controllerType == ControllerType.Mouse)
                    return;

                UpdateActiveControllers();

                if (onControllerConnected != null)
                    onControllerConnected.Invoke(args);

                foreach(Assignment assignment in assignments)
                {
                    if (assignment.controller is null)
                    {
                        assignment.Load(args.controller);
                        return;
                    }
                }
            }
            internal void OnControllerDisconnected(Rewired.ControllerStatusChangedEventArgs args)
            {
                if (args.controllerType == ControllerType.Mouse)
                    return;

                UpdateActiveControllers();

                if(onControllerDisconnected != null)
                    onControllerDisconnected.Invoke(args);
            }
            #endregion

            #region Controllers
            public Assignment? GetAssignment(Controller controller)
            {
                if (controller is null)
                    return null;

                foreach(Assignment assignment in assignments)
                {
                    if (assignment.HasController(controller))
                        return assignment;
                }

                return null;
            }
            public bool IsAssigned(Controller controller)
            {
                if (controller is null)
                    return false;

                foreach (Assignment assignment in assignments)
                {
                    if (assignment.HasController(controller))
                    {
                        return true;
                    }
                }

                return false;
            }
            private void UpdateActiveControllers()
            {
                controllers = ReInput.controllers.Controllers.Where(c => c.type != ControllerType.Mouse).ToList();
            }
            #endregion

            #region Assignments
            internal void ResetAllPositions()
            {
                List<Assignment> changes = new List<Assignment>();

                foreach (Assignment assignment in assignments)
                {
                    var newAssignment = assignment;

                    newAssignment.ClearScreen();

                    if (newAssignment.playerId == 0)
                    {
                        newAssignment.position = int2.one;
                        newAssignment.displayId = 0;
                    }

                    changes.Add(newAssignment);
                }

                assignments = changes;
                Save();
            }
            internal void SetLocalId(int playerId, int localId)
            {
                Assignment? assignment = GetAssignmentByPlayerId(playerId);

                if (assignment.HasValue)
                {
                    Assignment newAssignment = assignment.Value;
                    newAssignment.localId = localId;

                    //SetAssignment(newAssignment);
                    assignments[newAssignment.playerId] = newAssignment;
                }
            }
            public Rect GetScreenRectByLocalId(int localId)
            {
                Assignment? assignment = GetAssignmentByLocalId(localId);

                Rect screenRect = new Rect(0, 0, 1, 1);

                if (!assignment.HasValue)
                    return screenRect;

                screenRect.y = assignment.Value.position.x > 0 ? 0 : 0.5f;
                screenRect.x = assignment.Value.position.y < 2 ? 0 : 0.5f;

                screenRect.width = assignment.Value.position.y == 1 ? 1 : 0.5f;
                screenRect.height = assignment.Value.position.x == 1 ? 1 : 0.5f;

                return screenRect;
            }
            public int GetDisplayIdByLocalId(int localId)
            {
                Assignment? assignment = GetAssignmentByLocalId(localId);

                int displayId = 0;

                if (!assignment.HasValue)
                    return displayId;

                return assignment.Value.displayId;
            }
            public Assignment? GetAssignmentByLocalId(int localId)
            {
                foreach (Assignment assignment in assignments)
                {
                    if (assignment.localId == localId)
                        return assignment;
                }

                return null;
            }
            public Assignment? GetAssignmentByPlayerId(int playerId)
            {
                foreach (Assignment assignment in assignments)
                {
                    if (assignment.playerId == playerId)
                        return assignment;
                }

                return null;
            }
            internal void PushChanges(List<Assignment> changes)
            {
                if (changes is null)
                    return;
                
                foreach (Assignment change in changes)
                {
                    SetAssignment(change);
                }
            }
            internal void SetAssignment(Assignment assignment)
            {
                if (!assignment.position.IsPositive())
                    assignment.controller = null;

                List<Assignment> readonlyAssignments = assignments.AsReadOnly().ToList();

                foreach (Assignment other in readonlyAssignments)
                {
                    if(other.position.Equals(assignment.position) && other.displayId == assignment.displayId)
                    {
                        Assignment unassigned = other;
                        unassigned.position = int2.negative;
                        assignments[unassigned.playerId] = unassigned;
                    }
                }

                assignments[assignment.playerId] = assignment;
            }
            internal bool Save()
            {
                try
                {
                    UpdatePreferences();
                    Wrapper wrapper = new Wrapper(preferences);
                    preferencesConfig.Value = JsonUtility.ToJson(wrapper);
                    config.Save();
                    Log.LogOutput("Configuration.Save: Success.", Log.LogLevel.Message);
                    return true;
                }
                catch(Exception e)
                {
                    Log.LogOutput(e, Log.LogLevel.Error);
                    return false;
                }

            }
            private void LoadAssignment(int preferenceId, Controller controller)
            {
                if (preferenceId < 0 && preferenceId >= preferences.Count)
                    return;

                Assignment newAssignment = new Assignment(controller);

                newAssignment.Load(preferences[preferenceId]);

                assignments.Add(newAssignment);
            }
            private void UpdatePreferences()
            {
                currentLocalPlayerCount = 0;

                foreach(Assignment assignment in assignments)
                {
                    for(int e = 0; e < preferences.Count; e++)
                    {
                        if(assignment.MatchesPlayer(preferences[e]))
                        {
                            var preference = preferences[e];
                            preference.Update(assignment);
                            preferences[e] = preference;
                            break;
                        }
                    }

                    if(assignment.isAssigned)
                        currentLocalPlayerCount++;
                }

                onConfigurationUpdated.Invoke();
            }
            private VerifyStatus VerifyConfiguration()
            {
                if (PlatformSystems.saveSystem.loadedUserProfiles.Values.Count == 0 || assignedPlayerCount < 2)
                    return VerifyStatus.Fail;

                foreach (Assignment assignment in configuration.assignments)
                {
                    if (assignment.isAssigned)
                    {
                        if (assignment.profileId == -1 || assignment.profileId >= PlatformSystems.saveSystem.loadedUserProfiles.Values.Count)
                            return VerifyStatus.InvalidProfile;

                        if (assignment.controller is null)
                            return VerifyStatus.InvalidController;

                        if (assignment.displayId < 0 || assignment.displayId >= Display.displays.Length)
                            return VerifyStatus.InvalidDisplay;

                        foreach (Assignment other in configuration.assignments)
                        {
                            if (!other.isAssigned)
                                continue;

                            if (other.playerId == assignment.playerId)
                            {
                                if (!other.MatchesAssignment(assignment))
                                    return VerifyStatus.Fail;
                                else
                                    continue;
                            }

                            if (other.position.Equals(assignment.position) && other.displayId == assignment.displayId)
                                return VerifyStatus.InvalidPosition;

                            if (other.profileId == assignment.profileId)
                                return VerifyStatus.InvalidProfile;
                        }
                    }
                }

                return VerifyStatus.Success;
            }
            #endregion

            #region Splitscreen
            internal void TryAutoEnable()
            {
                if (enabledConfig.Value)
                    SetEnabled(true);
            }
            public VerifyStatus SetEnabled(bool requestedStatus)
            {
                VerifyStatus verifyStatus;

                if (requestedStatus)
                {
                    verifyStatus = VerifyConfiguration();

                    if (verifyStatus == VerifyStatus.Success)
                    {
                        int playerCount;

                        verifyStatus = instance.SetEnabled(requestedStatus, out playerCount);

                        if (verifyStatus == VerifyStatus.Success)
                        {
                            currentLocalPlayerCount = playerCount;
                            enabled = currentLocalPlayerCount > 1 ? true : false;

                            if (enabled)
                                onSplitScreenEnabled.Invoke();
                            else
                                onSplitScreenDisabled.Invoke();

                            return VerifyStatus.Success;
                        }
                    }
                    else
                    {
                        Log.LogOutput($"Configuration.SetEnabled: Failed to verify configuration with status '{verifyStatus}'");

                        foreach (Assignment assignment in assignments)
                        {
                            Log.LogOutput($"Configuration.SetEnabled: '{assignment}'");
                        }
                    }
                }
                else
                {
                    int c;

                    verifyStatus = instance.SetEnabled(false, out c);
                    currentLocalPlayerCount = 1;
                    enabled = false;
                    onSplitScreenDisabled.Invoke();
                }

                return verifyStatus;
            }
            private void OnSplitScreenEnabled()
            {
                instance.UpdateCursorStatus(true);
            }
            private void OnSplitScreenDisabled()
            {
                instance.UpdateCursorStatus(false);
            }
            
            #endregion

            #region Definitions
            [System.Serializable]
            private class Wrapper
            {
                public int[] positionX;
                public int[] positionY;
                public int[] displayId;
                public int[] playerId;
                public int[] profileId;
                public float[] colorR;
                public float[] colorG;
                public float[] colorB;
                public float[] colorA;

                public Wrapper(List<Preference> preferences)
                {
                    positionX = new int[preferences.Count];
                    positionY = new int[preferences.Count];
                    displayId = new int[preferences.Count];
                    playerId = new int[preferences.Count];
                    profileId = new int[preferences.Count];
                    colorR = new float[preferences.Count];
                    colorG = new float[preferences.Count];
                    colorB = new float[preferences.Count];
                    colorA = new float[preferences.Count];

                    for (int e = 0; e < preferences.Count; e++)
                    {
                        positionX[e] = preferences[e].position.x;
                        positionY[e] = preferences[e].position.y;
                        displayId[e] = preferences[e].displayId;
                        playerId[e] = preferences[e].playerId;
                        profileId[e] = preferences[e].profileId;
                        colorR[e] = preferences[e].color.r;
                        colorG[e] = preferences[e].color.g;
                        colorB[e] = preferences[e].color.b;
                        colorA[e] = preferences[e].color.a;
                    }
                }
            }
            #endregion
        }
        public class Input
        {
            public MPEventSystem currentButtonEventSystem { get; private set; }
            public MPEventSystem currentMouseEventSystem { get; private set; }

            public bool clickedThisFrame;

            public void UpdateCurrentEventSystem(EventSystem eventSystem, bool mouse = false)
            {
                if (eventSystem)
                    UpdateCurrentEventSystem(eventSystem as MPEventSystem, mouse);
                else
                {
                    if (mouse)
                        currentMouseEventSystem = null;
                    else
                        currentButtonEventSystem = null;
                }
            }
            public void UpdateCurrentEventSystem(MPEventSystem eventSystem, bool mouse = false)
            {
                if (mouse)
                {
                    if (currentMouseEventSystem)
                        currentMouseEventSystem.SetSelectedGameObject(null);

                    currentMouseEventSystem = eventSystem;
                }
                else
                {
                    if (currentButtonEventSystem)
                        currentButtonEventSystem.SetSelectedGameObject(null);

                    currentButtonEventSystem = eventSystem;
                }
            }
        }

        public enum VerifyStatus
        {
            Success,
            InvalidProfile,
            InvalidPosition,
            InvalidDisplay,
            InvalidController,
            Fail,
        }
        public struct Language
        {
            // TODO clean up & use language folder
            public static readonly string MSG_DISCORD_LINK_HREF = "https://discord.gg/maHhJSv62G";
            public static readonly string MSG_DISCORD_LINK_STRING = "Discord";
            public static readonly string MSG_DISCORD_LINK_TOKEN = "XSPLITSCREEN_DISCORD";
            public static readonly string MSG_DISCORD_LINK_HOVER_STRING = "Join the Discord for support";
            public static readonly string MSG_DISCORD_LINK_HOVER_TOKEN = "XSPLITSCREEN_DISCORD_HOVER";
            public static readonly string MSG_PATREON_LINK_HREF = "https://www.patreon.com/user?u=84145799";
            public static readonly string MSG_PATREON_LINK_TOKEN = "XSPLITSCREEN_PATREON";
            public static readonly string MSG_PATREON_LINK_STRING = "Patreon";
            public static readonly string MSG_SPLITSCREEN_CONFIG_HEADER_TOKEN = "XSPLITSCREEN_CONFIG_HEADER";
            public static readonly string MSG_SPLITSCREEN_CONFIG_HEADER_STRING = "Assignment";
            public static readonly string MSG_SPLITSCREEN_ENABLE_TOKEN = "XSPLITSCREEN_ENABLE";
            public static readonly string MSG_SPLITSCREEN_ENABLE_STRING = "Enable";
            public static readonly string MSG_SPLITSCREEN_DISABLE_TOKEN = "XSPLITSCREEN_DISABLE";
            public static readonly string MSG_SPLITSCREEN_DISABLE_STRING = "Disable";
            public static readonly string MSG_TITLE_BUTTON_TOKEN = "TITLE_XSPLITSCREEN";
            public static readonly string MSG_TITLE_BUTTON_STRING = "Splitscreen";
            public static readonly string MSG_HOVER_TOKEN = "TITLE_XSPLITSCREEN_DESC";
            public static readonly string MSG_HOVER_STRING = "Modify splitscreen settings.";
            public static readonly string MSG_UNSET_TOKEN = "XSPLITSCREEN_UNSET";
            public static readonly string MSG_UNSET_STRING = "(not set)";

            public static readonly string MSG_VERIFY_PROFILE_TOKEN = "XSPLITSCREEN_VERIFY_PROFILE";
            public static readonly string MSG_VERIFY_PROFILE_STRING = "Invalid profile assignments";
            public static readonly string MSG_VERIFY_GENERIC_TOKEN = "XSPLITSCREEN_VERIFY_GENERIC";
            public static readonly string MSG_VERIFY_GENERIC_STRING = "Invalid assignments";
            public static readonly string MSG_VERIFY_CONTROLLER_TOKEN = "XSPLITSCREEN_CONTROLLER_PROFILE";
            public static readonly string MSG_VERIFY_CONTROLLER_STRING = "Invalid controller assignments";
        }
        #endregion
    }
}