using Rewired;
using RoR2;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine.EventSystems;
using Rewired.UI;
using Rewired.Integration.UnityUI;
using UnityEngine;
using UnityEngine.UI;
using DoDad.XSplitScreen.Components;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace DoDad.XSplitScreen
{
    internal enum HookType
    {
        /// <summary>
        /// These hooks can be enabled without splitscreen users and are mainly for the assignment screen
        /// </summary>
        Singleplayer,
        /// <summary>
        /// These hooks generally require splitscreen users
        /// </summary>
        Splitscreen,
        General
    }
    internal static class HookManager
    {
        public static List<Action> onRunStartExecute { get; private set; } = new List<Action>();
        public static List<Action> onRunDestroyExecute { get; private set; } = new List<Action>();

        public static List<Hook> hooks { get; private set; } = new List<Hook>();

        public static void InitializeCustomHookList()
        {
            hooks.Clear();

            //hooks.Add(
            //    new Hook(typeof(RoR2.UI.TooltipController).GetMethod("FindUICamera", BindingFlags.Static | BindingFlags.NonPublic), TooltipController_FindUICamera)
            //);
        }

        public static void UpdateHooks(HookType hookType, bool enable)
        {
            switch (hookType)
            {
                case HookType.General:
                    if (enable)
                    {
                        On.RoR2.LocalUserManager.AddUser += LocalUserManager_AddUser;
                        On.RoR2.UI.SurvivorIconController.Update += SurvivorIconController_Update;
                        On.RoR2.ViewablesCatalog.AddNodeToRoot += ViewablesCatalog_AddNodeToRoot;
                        On.RoR2.UI.MPButton.Awake += MPButton_Awake;
                        On.RoR2.UI.MPToggle.Awake += MPToggle_Awake;
                        On.RoR2.UI.MPDropdown.Awake += MPDropdown_Awake;
                        On.RoR2.UI.MainMenu.SubmenuMainMenuScreen.OnExit += SubmenuMainMenuScreen_OnExit;
                        On.RoR2.UI.SettingsPanelController.Start += SettingsPanelController_Start;
                    }
                    else
                    {
                        On.RoR2.LocalUserManager.AddUser -= LocalUserManager_AddUser;
                        On.RoR2.UI.SurvivorIconController.Update -= SurvivorIconController_Update;
                        On.RoR2.ViewablesCatalog.AddNodeToRoot -= ViewablesCatalog_AddNodeToRoot;
                        On.RoR2.UI.MPButton.Awake -= MPButton_Awake;
                        On.RoR2.UI.MPToggle.Awake -= MPToggle_Awake;
                        On.RoR2.UI.MPDropdown.Awake -= MPDropdown_Awake;
                        On.RoR2.UI.MainMenu.SubmenuMainMenuScreen.OnExit -= SubmenuMainMenuScreen_OnExit;
                        On.RoR2.UI.SettingsPanelController.Start -= SettingsPanelController_Start;
                    }

                    SetRunListeners(enable);
                    break;
                case HookType.Singleplayer:
                    if (enable)
                    {
                        On.RoR2.UI.InputSourceFilter.Refresh += InputSourceFilter_Refresh;
                        On.RoR2.UI.MPEventSystem.ValidateCurrentSelectedGameobject += MPEventSystem_ValidateCurrentSelectedGameobject;
                        On.RoR2.UI.MPInputModule.GetMousePointerEventData += MPInputModule_GetMousePointerEventData;
                    }
                    else
                    {
                        On.RoR2.UI.InputSourceFilter.Refresh -= InputSourceFilter_Refresh;
                        On.RoR2.UI.MPEventSystem.ValidateCurrentSelectedGameobject -= MPEventSystem_ValidateCurrentSelectedGameobject;
                        On.RoR2.UI.MPInputModule.GetMousePointerEventData -= MPInputModule_GetMousePointerEventData;
                    }
                    break;
                case HookType.Splitscreen:
                    if (enable)
                    {
                        On.RoR2.UI.CursorOpener.OnEnable += CursorOpener_OnEnable;
                        On.RoR2.UI.SurvivorIconController.UpdateAvailability += SurvivorIconController_UpdateAvailability;
                        On.RoR2.CharacterSelectBarController.PickIcon += CharacterSelectBarController_PickIcon;
                        On.RoR2.CharacterSelectBarController.ShouldDisplaySurvivor += CharacterSelectBarController_ShouldDisplaySurvivor;
                        On.RoR2.UI.CharacterSelectController.Update += CharacterSelectController_Update;
                        On.RoR2.UI.MPInput.Update += MPInput_Update;
                        On.RoR2.UI.LoadoutPanelController.UpdateDisplayData += LoadoutPanelController_UpdateDisplayData;
                        On.RoR2.UI.MPButton.Update += MPButton_Update;
                        On.RoR2.CameraRigController.Start += CameraRigController_Start;
                        On.RoR2.UI.HUD.OnEnable += HUD_OnEnable;
                        On.RoR2.PauseManager.CCTogglePause += PauseManager_CCTogglePause;
                        //On.RoR2.UI.MPEventSystem.Update += MPEventSystem_Update;
                        On.RoR2.UI.SimpleDialogBox.Create += SimpleDialogBox_Create;
                        On.RoR2.LocalCameraEffect.OnUICameraPreCull += LocalCameraEffect_OnUICameraPreCull;
                        On.RoR2.UI.CombatHealthBarViewer.SetLayoutHorizontal += CombatHealthBarViewer_SetLayoutHorizontal;
                        On.RoR2.NetworkUser.UpdateUserName += NetworkUser_UpdateUserName;
                        On.RoR2.ColorCatalog.GetMultiplayerColor += ColorCatalog_GetMultiplayerColor;
                        On.RoR2.UI.Nameplate.SetBody += Nameplate_SetBody;
                        On.RoR2.UI.BaseSettingsControl.GetCurrentUserProfile += BaseSettingsControl_GetCurrentUserProfile;
                        On.RoR2.UI.InputBindingControl.StartListening += InputBindingControl_StartListening;
                        On.RoR2.UI.MPInput.CenterCursor += MPInput_CenterCursor;
                        On.RoR2.UI.VoteInfoPanelController.Awake += VoteInfoPanelController_Awake;
                        On.RoR2.UI.CharacterSelectController.ClientSetReady += CharacterSelectController_ClientSetReady;
                        On.RoR2.UI.CharacterSelectController.ClientSetUnready += CharacterSelectController_ClientSetUnready;
                        On.RoR2.UI.MPEventSystem.RecenterCursors += MPEventSystem_RecenterCursors;
                        On.RoR2.UI.ProfileNameLabel.LateUpdate += ProfileNameLabel_LateUpdate;
                        On.RoR2.UI.ScoreboardController.Awake += ScoreboardController_Awake;
                        On.RoR2.UI.RuleCategoryController.SetRandomVotes += RuleCategoryController_SetRandomVotes;
                        On.RoR2.UI.TooltipController.SetTooltipProvider += TooltipController_SetTooltipProvider;
                        On.RoR2.InputBindingDisplayController.Refresh += InputBindingDisplayController_Refresh;
                        On.RoR2.InputBindingDisplayController.OnEnable += InputBindingDisplayController_OnEnable;
                        On.RoR2.UI.CustomScrollbar.Awake += CustomScrollbar_Awake;
                        On.RoR2.UI.HUDScaleController.SetScale += HUDScaleController_SetScale;

                        if (Plugin.developerMode)
                        {
                            On.RoR2.HealthComponent.TakeDamage += HealthComponent_TakeDamage;
                        }
                    }
                    else
                    {
                        On.RoR2.UI.CursorOpener.OnEnable -= CursorOpener_OnEnable;
                        On.RoR2.UI.SurvivorIconController.UpdateAvailability -= SurvivorIconController_UpdateAvailability;
                        On.RoR2.CharacterSelectBarController.PickIcon -= CharacterSelectBarController_PickIcon;
                        On.RoR2.CharacterSelectBarController.ShouldDisplaySurvivor -= CharacterSelectBarController_ShouldDisplaySurvivor;
                        On.RoR2.UI.CharacterSelectController.Update -= CharacterSelectController_Update;
                        On.RoR2.UI.MPInput.Update -= MPInput_Update;
                        On.RoR2.UI.LoadoutPanelController.UpdateDisplayData -= LoadoutPanelController_UpdateDisplayData;
                        On.RoR2.UI.MPButton.Update -= MPButton_Update;
                        On.RoR2.CameraRigController.Start -= CameraRigController_Start;
                        On.RoR2.UI.HUD.OnEnable -= HUD_OnEnable;
                        On.RoR2.PauseManager.CCTogglePause -= PauseManager_CCTogglePause;
                        //On.RoR2.UI.MPEventSystem.Update -= MPEventSystem_Update;
                        On.RoR2.UI.SimpleDialogBox.Create -= SimpleDialogBox_Create;
                        On.RoR2.LocalCameraEffect.OnUICameraPreCull -= LocalCameraEffect_OnUICameraPreCull;
                        On.RoR2.UI.CombatHealthBarViewer.SetLayoutHorizontal -= CombatHealthBarViewer_SetLayoutHorizontal;
                        On.RoR2.NetworkUser.UpdateUserName -= NetworkUser_UpdateUserName;
                        On.RoR2.ColorCatalog.GetMultiplayerColor -= ColorCatalog_GetMultiplayerColor;
                        On.RoR2.UI.Nameplate.SetBody -= Nameplate_SetBody;
                        On.RoR2.UI.BaseSettingsControl.GetCurrentUserProfile -= BaseSettingsControl_GetCurrentUserProfile;
                        On.RoR2.UI.InputBindingControl.StartListening -= InputBindingControl_StartListening;
                        On.RoR2.UI.MPInput.CenterCursor -= MPInput_CenterCursor;
                        On.RoR2.UI.VoteInfoPanelController.Awake -= VoteInfoPanelController_Awake;
                        On.RoR2.UI.CharacterSelectController.ClientSetReady -= CharacterSelectController_ClientSetReady;
                        On.RoR2.UI.CharacterSelectController.ClientSetUnready -= CharacterSelectController_ClientSetUnready;
                        On.RoR2.UI.MPEventSystem.RecenterCursors -= MPEventSystem_RecenterCursors;
                        On.RoR2.UI.ProfileNameLabel.LateUpdate -= ProfileNameLabel_LateUpdate;
                        On.RoR2.UI.ScoreboardController.Awake -= ScoreboardController_Awake;
                        On.RoR2.UI.RuleCategoryController.SetRandomVotes -= RuleCategoryController_SetRandomVotes;
                        On.RoR2.InputBindingDisplayController.Refresh -= InputBindingDisplayController_Refresh;
                        On.RoR2.InputBindingDisplayController.OnEnable -= InputBindingDisplayController_OnEnable;
                        On.RoR2.UI.CustomScrollbar.Awake -= CustomScrollbar_Awake;
                        On.RoR2.UI.HUDScaleController.SetScale -= HUDScaleController_SetScale;

                        if (Plugin.developerMode)
                        {
                            On.RoR2.HealthComponent.TakeDamage -= HealthComponent_TakeDamage;
                        }
                    }

                    SetGeneralRunDelegates(enable);
                    SetGeneralHooks(enable);
                    break;
            }
        }
        #region Hooks (General)
        /// <summary>
        /// Add MultiInputHelper to custom sliders
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private static void CustomScrollbar_Awake(On.RoR2.UI.CustomScrollbar.orig_Awake orig, CustomScrollbar self)
        {
            orig(self);

            if (self.gameObject.GetComponent<MultiInputHelper>() == null)
                self.gameObject.AddComponent<MultiInputHelper>().mpScrollbar = self;
        }
        /// <summary>
        /// Add MultiInputHelper to Settings Sliders
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private static void SettingsPanelController_Start(On.RoR2.UI.SettingsPanelController.orig_Start orig, SettingsPanelController self)
        {
            orig(self);

            foreach (var slider in self.GetComponentsInChildren<Slider>())
            {
                if (slider.gameObject.GetComponent<MultiInputHelper>() == null)
                    slider.gameObject.AddComponent<MultiInputHelper>().slider = slider;
            }
        }
        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <param name="mainMenuController"></param>
        private static void SubmenuMainMenuScreen_OnExit(On.RoR2.UI.MainMenu.SubmenuMainMenuScreen.orig_OnExit orig, RoR2.UI.MainMenu.SubmenuMainMenuScreen self, RoR2.UI.MainMenu.MainMenuController mainMenuController)
        {
            orig(self, mainMenuController);

            Plugin.RecreateAssignmentScreen();
        }
        /// <summary>
        /// Add MultiInputHelper
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private static void MPButton_Awake(On.RoR2.UI.MPButton.orig_Awake orig, MPButton self)
        {
            orig(self);

            if (self.gameObject.GetComponent<MultiInputHelper>() == null)
                self.gameObject.AddComponent<MultiInputHelper>().mpButton = self;
        }
        /// <summary>
        /// Add MultiInputHelper
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private static void MPToggle_Awake(On.RoR2.UI.MPToggle.orig_Awake orig, MPToggle self)
        {
            orig(self);

            if (self.gameObject.GetComponent<MultiInputHelper>() == null)
                self.gameObject.AddComponent<MultiInputHelper>().toggle = self;
        }
        /// <summary>
        /// Add MultiInputHelper
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private static void MPDropdown_Awake(On.RoR2.UI.MPDropdown.orig_Awake orig, MPDropdown self)
        {
            orig(self);

            if (self.gameObject.GetComponent<MultiInputHelper>() == null)
                self.gameObject.AddComponent<MultiInputHelper>().mpDropdown = self;
        }
        /// <summary>
        /// Silence log spam
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="node"></param>
        private static void ViewablesCatalog_AddNodeToRoot(On.RoR2.ViewablesCatalog.orig_AddNodeToRoot orig, ViewablesCatalog.Node node)
        {
            node.SetParent(ViewablesCatalog.rootNode);

            foreach (ViewablesCatalog.Node descendant in node.Descendants())
                if (!ViewablesCatalog.fullNameToNodeMap.ContainsKey(descendant.fullName))
                    ViewablesCatalog.fullNameToNodeMap.Add(descendant.fullName, descendant);
        }
        /// <summary>
        /// LocalUserManager now adds LocalSplitscreenUser
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="inputPlayer"></param>
        /// <param name="userProfile"></param>
        private static void LocalUserManager_AddUser(On.RoR2.LocalUserManager.orig_AddUser orig, Player inputPlayer, UserProfile userProfile)
        {
            if (LocalUserManager.UserExists(inputPlayer))
                return;

            int firstAvailableId = LocalUserManager.GetFirstAvailableId();

            LocalSplitscreenUser localUser = new LocalSplitscreenUser()
            {
                inputPlayer = inputPlayer,
                id = firstAvailableId,
                userProfile = userProfile
            };

            LocalUserManager.localUsersList.Add(localUser);

            userProfile.OnLogin();

            MPEventSystem.FindByPlayer(inputPlayer).localUser = localUser;

            var onUserSignInField = (Action<LocalUser>)typeof(LocalUserManager).GetField("onUserSignIn", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

            if (onUserSignInField != null)
                onUserSignInField(localUser);

            var onLocalUsersUpdatedField = (Action)typeof(LocalUserManager).GetField("onLocalUsersUpdated", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

            if (onLocalUsersUpdatedField != null)
                onLocalUsersUpdatedField();
        }
        /// <summary>
        /// Allow UnityExplorer to be used in the character select screen
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private static void SurvivorIconController_Update(On.RoR2.UI.SurvivorIconController.orig_Update orig, SurvivorIconController self)
        {
            // Fix debug spam

            if (EventSystem.current == null)
                return;

            MPEventSystem system = EventSystem.current as MPEventSystem;

            if (system == null)
                return;

            orig(self);
        }
        #endregion

        #region Hooks (Singleplayer - required for assignment screen)
        /// <summary>
        /// Forward input on players with joysticks to MultiInputHelper component
        /// Call Input.UpdateEventSystem
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <param name="playerId"></param>
        /// <param name="mouseIndex"></param>
        /// <returns></returns>
        private static object MPInputModule_GetMousePointerEventData(On.RoR2.UI.MPInputModule.orig_GetMousePointerEventData orig, MPInputModule self, int playerId, int mouseIndex)
        {
            IMouseInputSource mouseInputSource = self.GetMouseInputSource(playerId, mouseIndex);

            if (mouseInputSource == null)
                return null;

            // Something about this hook causes the mouse to stop dragging sliders so exit here
            var currentPlayer = ((MPEventSystem)self.eventSystem)?.player;
            var lastActiveController = currentPlayer.controllers.GetLastActiveController();

            if (currentPlayer == null || lastActiveController == null || lastActiveController.type == ControllerType.Mouse)
            {
                return orig(self, playerId, mouseIndex);
            }

            //

            PlayerPointerEventData dataLeft;

            bool flag = self.GetPointerData(playerId, mouseIndex, -1, out dataLeft, true, PointerEventType.Mouse);

            dataLeft.Reset();

            Vector2 mousePosition = self.input.mousePosition;

            if (flag)
                dataLeft.position = mousePosition;

            if (mouseInputSource.locked || !mouseInputSource.enabled)
            {
                dataLeft.position = new Vector2(-1, -1);
                dataLeft.delta = Vector2.zero;
            }
            else
            {
                dataLeft.position = mousePosition;
                dataLeft.delta = mousePosition - dataLeft.position;
            }

            dataLeft.scrollDelta = mouseInputSource.wheelDelta;
            dataLeft.button = PointerEventData.InputButton.Left;

            var previousPosition = dataLeft.position;

            //var splitscreenUser = LocalUserManager.localUsersList[(self.eventSystem as MPEventSystem).localUser.id] as LocalSplitscreenUser;
            var splitscreenUser = (self.eventSystem as MPEventSystem).localUser as LocalSplitscreenUser;

            if (Run.instance && splitscreenUser != null && splitscreenUser.eventSystem.cursorIndicatorController?.currentChildIndicator != null)
            {
                var position = dataLeft.position;//new Vector2(splitscreenUser.eventSystem.cursorIndicatorController.currentChildIndicator.transform.position.x,
                                                 //splitscreenUser.eventSystem.cursorIndicatorController.currentChildIndicator.transform.position.y);

                if (splitscreenUser.assignment.display != 0 && Display.displays[splitscreenUser.assignment.display].active)
                {
                    for (int i = 0; i < splitscreenUser.assignment.display; i++)
                    {
                        if (Display.displays[i].active)
                        {
                            position.x += Display.displays[i].systemWidth;
                        }
                    }

                    position.y += Display.displays[0].systemHeight - Display.displays[splitscreenUser.assignment.display].systemHeight;
                }

                dataLeft.position = position;
            }

            self.eventSystem.RaycastAll(dataLeft, self.m_RaycastResultCache);
            /*
            if (Run.instance && splitscreenUser != null && splitscreenUser.assignment.display == 1 && splitscreenUser.cameraRigController?.uiCam != null)
            {
                var rect = splitscreenUser.eventSystem?.cursorIndicatorController?.currentChildIndicator?.GetComponent<RectTransform>();

                if (rect != null && splitscreenUser.cameraRigController.hud != null)
                {
                    var point = splitscreenUser.eventSystem.cursorIndicatorController.currentChildIndicator.transform.position;//TransformPoint(Vector3.zero);
                    point.x += Display.displays[0].systemWidth;
                    Log.LogOutput($"Point = {point}");

                    //point = splitscreenUser.cameraRigController.hud.transform.TransformPoint(point);
                    //point.z = splitscreenUser.cameraRigController.uiCam.nearClipPlane;

                    dataLeft.position = point;//splitscreenUser.cameraRigController.uiCam.ScreenToWorldPoint(point);//rect.position);
                    
                    Log.LogOutput($"dataLeft.position = {dataLeft.position}");
                    //splitscreenUser.cameraRigController.hud.GetComponent<GraphicRaycaster>().ignoreReversedGraphics = false;

                    splitscreenUser.cameraRigController.hud.GetComponent<GraphicRaycaster>().Raycast(dataLeft, self.m_RaycastResultCache);

                    foreach (var result in self.m_RaycastResultCache)
                    {
                        Log.LogOutput($"splitscreenUser.cameraRigController.hud.GetComponent<GraphicRaycaster>() {result.gameObject.name}");
                    }

                    foreach (var raycaster in splitscreenUser.cameraRigController.hud.GetComponentsInChildren<GraphicRaycaster>())
                    {
                        //raycaster.ignoreReversedGraphics = false;
                        raycaster.Raycast(dataLeft, self.m_RaycastResultCache);

                        foreach (var result in self.m_RaycastResultCache)
                        {
                            Log.LogOutput($"raycaster.Raycast - result.worldPosition {result.worldPosition}");
                            Log.LogOutput($"raycaster.Raycast - result.screenPosition {result.screenPosition}");
                            Log.LogOutput($"raycaster.Raycast - result.module.gameObject.name {result.module.gameObject.name}");
                            Log.LogOutput($"raycaster.Raycast - result.depth {result.depth}");
                            Log.LogOutput($"raycaster.Raycast - result.displayIndex {result.displayIndex}");
                            Log.LogOutput($"raycaster.Raycast - result.distance {result.distance}");
                            Log.LogOutput($"raycaster.Raycast - result.index {result.index}");
                        }
                    }

                    //.ignoreReversedGraphics = false;
                    self.eventSystem.RaycastAll(dataLeft, self.m_RaycastResultCache);

                    foreach (var result in self.m_RaycastResultCache)
                    {
                        Log.LogOutput($"eventSystem.RaycastAll {result.gameObject.name}");
                    }

                    
                    var ray = splitscreenUser.cameraRigController.uiCam.ScreenPointToRay(rect.position);

                    var hits = Physics.RaycastAll(ray, 10, LayerIndex.ui.mask);

                    foreach (var hit in hits)
                    {
                        if (hit.collider.GetComponent<MultiInputHelper>() != null)
                        {
                            Log.LogOutput($"MultiInputHelper {hit.collider.name}");
                            self.m_RaycastResultCache.Clear();
                            self.m_RaycastResultCache.Add(new RaycastResult()
                            {
                                depth = (int)hit.distance,
                                m_GameObject = hit.collider.gameObject,
                            });
                        }
                    }

                    foreach (var result in self.m_RaycastResultCache)
                    {
                        Log.LogOutput($"Raycast hit {result.gameObject.name}");
                    }
                }
            }
            else
            {
                self.eventSystem.RaycastAll(dataLeft, self.m_RaycastResultCache);
            }
            */
            dataLeft.position = previousPosition;
            // Push gamepad input to MultiInputHelper and toggle navigation events
            // Input.UpdateEventSystem
            var eventSystem = (MPEventSystem)self.eventSystem;

            if (eventSystem.isCursorVisible)
            {
                if (currentPlayer.GetAnyButton())
                    Input.UpdateEventSystem(eventSystem);

                if (currentPlayer.id > 0 && currentPlayer.controllers.joystickCount > 0 && currentPlayer.controllers.hasKeyboard)
                {
                    if (lastActiveController.type == ControllerType.Joystick)
                    {
                        if (!self.eventSystem.sendNavigationEvents)
                            self.eventSystem.sendNavigationEvents = true;
                    }
                    else
                    {
                        if (self.eventSystem.sendNavigationEvents)
                            self.eventSystem.sendNavigationEvents = false;
                    }
                }

                if (self.useCursor && currentPlayer.controllers.joystickCount > 0)
                {
                    MultiInputHelper multiInputHelper = null;

                    foreach (var raycast in self.m_RaycastResultCache)
                    {
                        multiInputHelper = raycast.gameObject?.GetComponentInParent<MultiInputHelper>();

                        if (multiInputHelper != null && !raycast.gameObject.name.ToLower().Contains("command"))
                            break;

                        multiInputHelper = null;
                    }

                    if (multiInputHelper != null)
                    {
                        bool didClick = currentPlayer.GetButtonDown(4) || currentPlayer.GetButtonDown(14);
                        bool didDrag = currentPlayer.GetButton(4) || currentPlayer.GetButton(14);

                        if (didClick || didDrag)
                            multiInputHelper.OnClick(dataLeft, didClick, didDrag);
                        else
                            self.eventSystem.SetSelectedGameObject(null);
                    }
                    else
                        self.eventSystem.SetSelectedGameObject(null);
                }
            }

            //

            RaycastResult firstRaycast = BaseInputModule.FindFirstRaycast(self.m_RaycastResultCache);

            dataLeft.pointerCurrentRaycast = firstRaycast;

            self.UpdateHover(self.m_RaycastResultCache);
            self.m_RaycastResultCache.Clear();

            PlayerPointerEventData dataRight;

            self.GetPointerData(playerId, mouseIndex, -2, out dataRight, true, PointerEventType.Mouse);
            self.CopyFromTo(dataLeft, dataRight);

            dataRight.button = PointerEventData.InputButton.Right;

            PlayerPointerEventData dataMiddle;

            self.GetPointerData(playerId, mouseIndex, -3, out dataMiddle, true, PointerEventType.Mouse);
            self.CopyFromTo(dataLeft, dataMiddle);

            dataMiddle.button = PointerEventData.InputButton.Middle;

            for (int index = 3; index < mouseInputSource.buttonCount; index++)
            {
                PlayerPointerEventData dataCustom;
                self.GetPointerData(playerId, mouseIndex, index - 2147483520, out dataCustom, true, PointerEventType.Mouse);
                self.CopyFromTo(dataLeft, dataCustom);
                dataCustom.button = ~PointerEventData.InputButton.Left;
            }

            self.m_MouseState.SetButtonState(0, self.StateForMouseButton(playerId, mouseIndex, 0), dataLeft);
            self.m_MouseState.SetButtonState(1, self.StateForMouseButton(playerId, mouseIndex, 1), dataRight);
            self.m_MouseState.SetButtonState(2, self.StateForMouseButton(playerId, mouseIndex, 2), dataMiddle);

            for (int index = 3; index < mouseInputSource.buttonCount; index++)
            {
                PlayerPointerEventData dataCustom;
                self.GetPointerData(playerId, mouseIndex, index - 2147483520, out dataCustom, false, PointerEventType.Mouse);
                self.m_MouseState.SetButtonState(index, self.StateForMouseButton(playerId, mouseIndex, index), dataCustom);
            }


            return self.m_MouseState;
        }
        /// <summary>
        /// Properly clear the selected GameObject for gamepads
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private static void MPEventSystem_ValidateCurrentSelectedGameobject(On.RoR2.UI.MPEventSystem.orig_ValidateCurrentSelectedGameobject orig, MPEventSystem self)
        {
            if (!self.currentSelectedGameObject)
                return;

            var selectable = self.currentSelectedGameObject.GetComponent<Selectable>();
            var mpButton = selectable as MPButton;

            bool hasSelection = selectable == null ? false : selectable.hasSelection;

            if (mpButton != null && !mpButton.CanBeSelected())
                hasSelection = false;

            if (hasSelection)
                return;

            self.SetSelectedGameObject(null);
        }
        /// <summary>
        /// Disable keyboard and mouse elements from being hidden and gamepad elements from being shown
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <param name="forceRefresh"></param>
        private static void InputSourceFilter_Refresh(On.RoR2.UI.InputSourceFilter.orig_Refresh orig, InputSourceFilter self, bool forceRefresh)
        {
            MPEventSystem.InputSource? currentInputSource = MPEventSystem.InputSource.MouseAndKeyboard;//Input.lastEventSystem?.currentInputSource;

            bool flag = (currentInputSource.GetValueOrDefault() == self.requiredInputSource & currentInputSource.HasValue) || self.requiredInputSource == MPEventSystem.InputSource.MouseAndKeyboard;

            if (flag != self.wasOn | forceRefresh)
            {
                for (int index = 0; index < self.objectsToFilter.Length; ++index)
                    self.objectsToFilter[index].SetActive(flag);
            }

            self.wasOn = flag;
        }
        #endregion

        #region Hooks (Splitscreen - including additional UI hooks)
        /// <summary>
        /// Make HUD scale per player
        /// </summary>
        private static void HUDScaleController_SetScale(On.RoR2.UI.HUDScaleController.orig_SetScale orig, HUDScaleController self)
        {
            var user = (self.GetComponent<HUD>().localUserViewer.currentNetworkUser.localUser as LocalSplitscreenUser);

            if (user == null)
            {
                orig(self);
                return;
            }

            var scale = new Vector3(user.assignment.hudScale / 100f, user.assignment.hudScale / 100f, user.assignment.hudScale / 100f);

            foreach (var rect in self.rectTransforms)
                rect.localScale = scale;
        }
        /// <summary>
        /// Refresh binding text 
        /// </summary>
        private static void InputBindingDisplayController_OnEnable(On.RoR2.InputBindingDisplayController.orig_OnEnable orig, InputBindingDisplayController self)
        {
            orig(self);

            self.Refresh(true);
        }
        /// <summary>
        /// Update binding text to current user
        /// </summary>
        private static void InputBindingDisplayController_Refresh(On.RoR2.InputBindingDisplayController.orig_Refresh orig, InputBindingDisplayController self, bool forceRefresh)
        {
            var oldSystem = self.eventSystemLocator.eventSystem;

            if (self.name.Equals("ButtonText"))
                self.eventSystemLocator.eventSystem = Input.lastEventSystem;

            orig(self, forceRefresh);

            self.eventSystemLocator.eventSystem = oldSystem;
        }
        private static void TooltipController_SetTooltipProvider(On.RoR2.UI.TooltipController.orig_SetTooltipProvider orig, TooltipController self, TooltipProvider provider)
        {
            orig(self, provider);

            if (self.uiCamera?.camera != null)
                self.GetComponent<Canvas>().targetDisplay = self.uiCamera.camera.targetDisplay;
        }
        /// <summary>
        /// Fix random votes
        /// </summary>
        private static void RuleCategoryController_SetRandomVotes(On.RoR2.UI.RuleCategoryController.orig_SetRandomVotes orig, RuleCategoryController self)
        {
            var oldSystem = self.eventSystemLocator.eventSystem;

            self.eventSystemLocator.eventSystem = Input.lastEventSystem;

            orig(self);

            self.eventSystemLocator.eventSystem = oldSystem;
        }
        /// <summary>
        /// Remove scoreboard postprocessing
        /// </summary>
        private static void ScoreboardController_Awake(On.RoR2.UI.ScoreboardController.orig_Awake orig, ScoreboardController self)
        {
            orig(self);
            self.transform.GetComponentInChildren<PostProcessDuration>().gameObject.SetActive(false);
        }
        /// <summary>
        /// Update Settings profile name to currently active input system
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private static void ProfileNameLabel_LateUpdate(On.RoR2.UI.ProfileNameLabel.orig_LateUpdate orig, ProfileNameLabel self)
        {
            if (Input.lastEventSystem.localUser.userProfile.name == self.currentUserName)
                return;

            self.currentUserName = Input.lastEventSystem.localUser.userProfile.name;
            self.label.text = RoR2.Language.GetStringFormatted(self.token, self.currentUserName);
        }
        /// <summary>
        /// Center all cursors
        /// </summary>
        /// <param name="orig"></param>
        private static void MPEventSystem_RecenterCursors(On.RoR2.UI.MPEventSystem.orig_RecenterCursors orig)
        {
            Log.LogOutput($"MPEventSystem_RecenterCursors");
            foreach (var instance in MPEventSystem.instancesList)
                if (instance.currentInputModule)
                    ((MPInput)instance.currentInputModule.input).CenterCursor();
        }
        /// <summary>
        /// Fix pregame voting for local users
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private static void CharacterSelectController_ClientSetUnready(On.RoR2.UI.CharacterSelectController.orig_ClientSetUnready orig, CharacterSelectController self)
        {
            Input.lastEventSystem.localUser?.currentNetworkUser?.CallCmdSubmitVote(PreGameController.instance.gameObject, -1);
        }
        /// <summary>
        /// Fix pregame voting for local users
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private static void CharacterSelectController_ClientSetReady(On.RoR2.UI.CharacterSelectController.orig_ClientSetReady orig, CharacterSelectController self)
        {
            Input.lastEventSystem.localUser?.currentNetworkUser?.CallCmdSubmitVote(PreGameController.instance.gameObject, 0);
        }
        /// <summary>
        /// Enable local user ready voting
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private static void VoteInfoPanelController_Awake(On.RoR2.UI.VoteInfoPanelController.orig_Awake orig, VoteInfoPanelController self)
        {
            if (RoR2Application.isInMultiPlayer || Plugin.active)
            {
                self.voteController.canChangeVote = true;

                return;
            }

            self.gameObject.SetActive(false);
        }
        /// <summary>
        /// Center cursors by display
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private static void MPInput_CenterCursor(On.RoR2.UI.MPInput.orig_CenterCursor orig, MPInput self)
        {
            Log.LogOutput($"MPInput_CenterCursor");
            var user = self.eventSystem.localUser as LocalSplitscreenUser;

            if (user != null && user.assignment.display != -1 && user.assignment.display < Display.displays.Length)
            {
                Vector2 center = new Vector2(Display.displays[user.assignment.display].systemWidth, Display.displays[user.assignment.display].systemHeight) * 0.5f;

                Vector2 halfDimensions = center / 2f;

                if (user.assignment.position.y > 1)
                    center.y += halfDimensions.y;
                else if (user.assignment.position.y < 1)
                    center.y -= halfDimensions.y;

                if (user.assignment.position.x > 1)
                    center.x += halfDimensions.x;
                else if (user.assignment.position.x < 1)
                    center.x -= halfDimensions.x;

                self.internalMousePosition = center;
            }
            else
            {
                orig(self);
            }
        }
        /// <summary>
        /// Allow local users to assign new keybinds.
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private static void InputBindingControl_StartListening(On.RoR2.UI.InputBindingControl.orig_StartListening orig, InputBindingControl self)
        {
            var oldSystem = self.eventSystemLocator.eventSystem;

            self.eventSystemLocator.eventSystem = Input.lastEventSystem;

            orig(self);

            self.eventSystemLocator.eventSystem = oldSystem;
        }
        /// <summary>
        /// Allow settings to be saved to the correct profile.
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <returns></returns>
        private static UserProfile BaseSettingsControl_GetCurrentUserProfile(On.RoR2.UI.BaseSettingsControl.orig_GetCurrentUserProfile orig, BaseSettingsControl self)
        {
            return Input.lastEventSystem?.localUser?.userProfile;
        }
        /// <summary>
        /// Feed custom colors to ColorCatalog
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="playerSlot"></param>
        /// <returns></returns>
        private static Color ColorCatalog_GetMultiplayerColor(On.RoR2.ColorCatalog.orig_GetMultiplayerColor orig, int playerSlot)
        {
            if (MPEventSystem.activeCount <= 1)
                return Color.white;

            return ((LocalSplitscreenUser)LocalUserManager.localUsersList[playerSlot]).assignment.color;
        }
        /// <summary>
        /// Update nameplate colors
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <param name="body"></param>
        private static void Nameplate_SetBody(On.RoR2.UI.Nameplate.orig_SetBody orig, Nameplate self, CharacterBody body)
        {
            self.body = body;

            if (self.body?.master?.playerCharacterMasterController?.networkUser?.localUser != null)
                self.baseColor = ColorCatalog.GetMultiplayerColor(self.body.master.playerCharacterMasterController.networkUser.localUser.id);
        }
        /// <summary>
        /// Fix local player names
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private static void NetworkUser_UpdateUserName(On.RoR2.NetworkUser.orig_UpdateUserName orig, NetworkUser self)
        {
            LocalSplitscreenUser user = self.localUser as LocalSplitscreenUser;

            if (self.localUser == null)
            {
                Log.LogOutput($"Local user is null");
                self.userName = self.GetNetworkPlayerName().GetResolvedName();
            }
            else
            {
                Log.LogOutput($"Local user is NOT null");
                self.userName = Plugin.developerMode ? $"Player {user.id + 1}" : user.userProfile.name;
            }
        }
        /// <summary>
        /// Health bar fix by iDeathHD
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private static void CombatHealthBarViewer_SetLayoutHorizontal(On.RoR2.UI.CombatHealthBarViewer.orig_SetLayoutHorizontal orig, CombatHealthBarViewer self)
        {
            if (!self.uiCamera)
                return;

            self.UpdateAllHealthbarPositions(self.uiCamera.cameraRigController.sceneCam, self.uiCamera.camera);
        }
        /// <summary>
        /// Clear the death effect for alive players
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="uiCamera"></param>
        private static void LocalCameraEffect_OnUICameraPreCull(On.RoR2.LocalCameraEffect.orig_OnUICameraPreCull orig, UICamera uiCamera)
        {
            GameObject target = uiCamera.cameraRigController.target;

            for (int index = 0; index < LocalCameraEffect.instancesList.Count; index++)
            {
                LocalCameraEffect instance = LocalCameraEffect.instancesList[index];
                HealthComponent component = uiCamera?.cameraRigController?.localUserViewer?.cachedBody?.healthComponent;

                if (!component)
                    continue;

                if (instance.targetCharacter == target && component.alive)
                    instance.effectRoot.SetActive(true);
                else
                    instance.effectRoot.SetActive(false);
            }

        }
        /// <summary>
        /// Parent unowned dialogue boxes to the last player who provided input
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        private static SimpleDialogBox SimpleDialogBox_Create(On.RoR2.UI.SimpleDialogBox.orig_Create orig, MPEventSystem owner)
        {
            var box = orig(owner);

            if (Run.instance && Input.lastEventSystem != null)
                box.transform.parent.gameObject.AddComponent<MultiMonitorHelper>().targetDisplay = ((LocalSplitscreenUser)Input.lastEventSystem.localUser).assignment.display;

            return box;
        }
        /// <summary>
        /// Attach local user to pause command
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private static void MPEventSystem_Update(On.RoR2.UI.MPEventSystem.orig_Update orig, MPEventSystem self)
        {
            var current = EventSystem.current;

            EventSystem.current = self;

            var pointer = typeof(EventSystem).GetMethod(nameof(EventSystem.Update), (BindingFlags)(-1)).MethodHandle.GetFunctionPointer();
            var baseUpdate = (Action)Activator.CreateInstance(typeof(Action), self, pointer);

            baseUpdate();

            EventSystem.current = current;

            self.ValidateCurrentSelectedGameobject();

            if (!self.player.GetButtonDown(25) || PauseScreenController.instancesList.Count != 0 && SimpleDialogBox.instancesList.Count != 0)
                return;

            RoR2.Console.instance.SubmitCmd(self.localUser?.currentNetworkUser, "pause", false);
        }
        /// <summary>
        /// Set pause screen target display
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="args"></param>
        private static void PauseManager_CCTogglePause(On.RoR2.PauseManager.orig_CCTogglePause orig, ConCommandArgs args)
        {
            if (args.localUserSender != null)
            {
                var splitscreenUser = args.localUserSender as LocalSplitscreenUser;

                if (PauseManager.pauseScreenInstance)
                {
                    GameObject.Destroy(splitscreenUser.pauseScreenInstance);

                    foreach (LocalSplitscreenUser user in LocalUserManager.localUsersList)
                    {
                        if (user.pauseScreenInstance != null)
                            PauseManager.pauseScreenInstance = user.pauseScreenInstance;
                    }
                }
                else
                {
                    if (!UnityEngine.Networking.NetworkManager.singleton.isNetworkActive)
                        return;

                    PauseManager.pauseScreenInstance = GameObject.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/PauseScreen"), RoR2Application.instance.transform);
                    splitscreenUser.pauseScreenInstance = PauseManager.pauseScreenInstance;
                    splitscreenUser.pauseScreenInstance.GetComponent<Canvas>().targetDisplay = splitscreenUser.assignment.display;
                }
            }
            else
            {
                orig(args);
            }

        }
        /// <summary>
        /// Set canvas target display to current user's current display
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private static void HUD_OnEnable(On.RoR2.UI.HUD.orig_OnEnable orig, HUD self)
        {
            orig(self);

            if (self.localUserViewer?.currentNetworkUser?.localUser == null)
                return;

            self.canvas.targetDisplay = (self.localUserViewer.currentNetworkUser.localUser as LocalSplitscreenUser).assignment.display;
            self.canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        /// <summary>
        /// Assign cameras and UI to the correct display
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private static void CameraRigController_Start(On.RoR2.CameraRigController.orig_Start orig, CameraRigController self)
        {
            orig(self);

            if (Run.instance && self.viewer?.localUser != null)
            {
                var splitscreenUser = self.viewer.localUser as LocalSplitscreenUser;//LocalUserManager.localUsersList[self.viewer.localUser.id] as LocalSplitscreenUser;

                var display = splitscreenUser.assignment.display;

                if (splitscreenUser != null && display != 0)
                {
                    if (!Display.displays[display].active)
                        Display.displays[display].Activate();

                    self.sceneCam.targetDisplay = display;
                    self.uiCam.targetDisplay = display;

                    var canvas = splitscreenUser.eventSystem.cursorIndicatorController.GetComponent<Canvas>();

                    canvas.targetDisplay = display;
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                }
            }

        }
        /// <summary>
        /// Open cursors when splitscreen is enabled
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private static void CursorOpener_OnEnable(On.RoR2.UI.CursorOpener.orig_OnEnable orig, CursorOpener self)
        {
            orig(self);

            if (self.forceCursorForGamePad != Plugin.active)
                self.forceCursorForGamePad = Plugin.active;
        }
        /// <summary>
        /// Update event system from cursor movement
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private static void MPInput_Update(On.RoR2.UI.MPInput.orig_Update orig, MPInput self)
        {
            var previousPosiiton = self.mousePosition;

            if (Run.instance)
            {
                if (self.eventSystem.isCursorVisible)
                {
                    var splitscreenUser = LocalUserManager.localUsersList[self.eventSystem.localUser.id] as LocalSplitscreenUser;

                    if (splitscreenUser != null)
                    {
                        var width = Display.displays[splitscreenUser.assignment.display].renderingWidth;
                        var height = Display.displays[splitscreenUser.assignment.display].renderingHeight;

                        float num = Mathf.Min(width / 1920f, height / 1080f);

                        self.internalScreenPositionDelta = Vector2.zero;

                        if (self.eventSystem.currentInputSource == MPEventSystem.InputSource.MouseAndKeyboard)
                        {
                            if (Application.isFocused)
                                self.internalMousePosition = UnityEngine.Input.mousePosition;
                        }
                        else
                        {
                            var vector2 = new Vector2(self.player.GetAxis(23), self.player.GetAxis(24));

                            float magnitude = vector2.magnitude;

                            self.stickMagnitude = Mathf.Min(Mathf.MoveTowards(self.stickMagnitude, magnitude, self.cursorAcceleration * Time.unscaledDeltaTime), magnitude);

                            float stickMagnitude = self.stickMagnitude;

                            if (self.eventSystem.isHovering)
                                stickMagnitude *= self.cursorStickyModifier;

                            self.internalScreenPositionDelta = ((double)magnitude == 0.0 ? Vector2.zero : vector2 * (stickMagnitude / magnitude)) * Time.unscaledDeltaTime * (1920f * self.cursorScreenSpeed * num);
                            self.internalMousePosition += self.internalScreenPositionDelta;
                        }

                        self.internalMousePosition.x = Mathf.Clamp(self.internalMousePosition.x, 0.0f, width);
                        self.internalMousePosition.y = Mathf.Clamp(self.internalMousePosition.y, 0.0f, height);
                    }
                }
            }
            else
            {
                orig(self);
            }

            if (Vector3.SqrMagnitude(previousPosiiton - self.mousePosition) > 0.1f)
                Input.UpdateEventSystem(self.eventSystem);
        }
        /// <summary>
        /// Update local user from the last event system
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private static void CharacterSelectController_Update(On.RoR2.UI.CharacterSelectController.orig_Update orig, CharacterSelectController self)
        {
            self.localUser = Input.lastEventSystem.localUser;

            orig(self);
        }
        /// <summary>
        /// Combine all entitlements and make them available for all users
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private static void SurvivorIconController_UpdateAvailability(On.RoR2.UI.SurvivorIconController.orig_UpdateAvailability orig, SurvivorIconController self)
        {
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
        /// <summary>
        /// Update current user from Input.lastEventSystem
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <param name="survivorDef"></param>
        /// <returns></returns>
        private static bool CharacterSelectBarController_ShouldDisplaySurvivor(On.RoR2.CharacterSelectBarController.orig_ShouldDisplaySurvivor orig, CharacterSelectBarController self, SurvivorDef survivorDef)
        {
            self.currentLocalUser = Input.lastEventSystem.localUser;

            return orig(self, survivorDef);
        }
        /// <summary>
        /// Update current user from Input.lastEventSystem
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <param name="newPickedIcon"></param>
        private static void CharacterSelectBarController_PickIcon(On.RoR2.CharacterSelectBarController.orig_PickIcon orig, CharacterSelectBarController self, SurvivorIconController newPickedIcon)
        {
            self.currentLocalUser = Input.lastEventSystem.localUser;

            orig(self, newPickedIcon);
        }
        /// <summary>
        /// Update the loadout panel to the last event system
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private static void LoadoutPanelController_UpdateDisplayData(On.RoR2.UI.LoadoutPanelController.orig_UpdateDisplayData orig, LoadoutPanelController self)
        {
            UserProfile profile = Input.lastEventSystem?.localUser?.userProfile;
            NetworkUser network = Input.lastEventSystem?.localUser?.currentNetworkUser;
            BodyIndex bodyIndex = network ? network.bodyIndexPreference : BodyIndex.None;

            self.SetDisplayData(new LoadoutPanelController.DisplayData()
            {
                userProfile = profile,
                bodyIndex = bodyIndex
            });
        }
        /// <summary>
        /// Disable update logic
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private static void MPButton_Update(On.RoR2.UI.MPButton.orig_Update orig, MPButton self)
        {
            return;
        }
        private static void MPEventSystem_Update_IL(bool state)
        {
            if (state)
            {
                Log.LogOutput($" -------------- ENABLING ---------------");
                IL.RoR2.UI.MPEventSystem.Update += (il) =>
                {
                    ILCursor c = new ILCursor(il);

                    c.GotoNext(
                        x => x.MatchCall<RoR2.Console>("get_instance"),
                        x => x.MatchLdnull(),
                        x => x.MatchLdstr("pause"),
                        x => x.MatchLdcI4(0)
                        );

                    c.Index += 1;
                    c.Next.Operand = Input.lastEventSystem;

                    Log.LogOutput($"OpCode.Op1: {c.Next.OpCode.Op1}");
                    Log.LogOutput($"OpCode.Op2: {c.Next.OpCode.Op2}");
                    Log.LogOutput($"OpCode.FlowControl: {c.Next.OpCode.FlowControl}");
                    Log.LogOutput($"Operand: {c.Next.Operand}");
                };
            }
            else
            {
                IL.RoR2.UI.MPEventSystem.Update -= (il) =>
                {
                    ILCursor c = new ILCursor(il);

                    c.GotoNext(
                        x => x.MatchCall<RoR2.Console>("get_instance"),
                        x => x.MatchLdnull(),
                        x => x.MatchLdstr("pause"),
                        x => x.MatchLdcI4(0)
                        );

                    c.Index += 1;
                    c.Next.Operand = Input.lastEventSystem;

                    Log.LogOutput($"OpCode.Op1: {c.Next.OpCode.Op1}");
                    Log.LogOutput($"OpCode.Op2: {c.Next.OpCode.Op2}");
                    Log.LogOutput($"OpCode.FlowControl: {c.Next.OpCode.FlowControl}");
                    Log.LogOutput($"Operand: {c.Next.Operand}");
                };
            }
        }
        #endregion

        #region Delegates & Events
        private static void SetGeneralHooks(bool enable)
        {
            foreach (var hook in hooks)
            {
                if (enable)
                    hook.Apply();
                else
                    hook.Undo();
            }
        }
        private static void SetRunListeners(bool enable)
        {
            if (enable)
            {
                Run.onRunStartGlobal += OnRunStartGlobal;
                Run.onRunDestroyGlobal += OnRunDestroyGlobal;
            }
            else
            {
                Run.onRunStartGlobal -= OnRunStartGlobal;
                Run.onRunDestroyGlobal -= OnRunDestroyGlobal;
            }
        }
        /// <summary>
        /// Add or remove general delegates to call list
        /// </summary>
        /// <param name="enable"></param>
        private static void SetGeneralRunDelegates(bool enable)
        {
            var onRunDestroyActions = new Action[]
            {
                delegate
                {
                    foreach (var eventSystem in MPEventSystem.instancesList)
                        eventSystem.cursorIndicatorController.GetComponent<Canvas>().targetDisplay = 0;
                },
            };

            var onRunStartActions = new Action[]
            {
                delegate
                {
                    foreach(var user in LocalUserManager.localUsersList)
                        user.currentNetworkUser.UpdateUserName();
                },
            };

            if (enable)
            {
                foreach (var action in onRunDestroyActions)
                    onRunDestroyExecute.Add(action);

                foreach (var action in onRunStartActions)
                    onRunStartExecute.Add(action);
            }
            else
            {
                foreach (var action in onRunDestroyActions)
                    onRunDestroyExecute.Remove(action);

                foreach (var action in onRunStartActions)
                    onRunStartExecute.Remove(action);
            }
        }
        private static void OnRunStartGlobal(Run run)
        {
            foreach (var action in onRunStartExecute)
                action();
        }
        private static void OnRunDestroyGlobal(Run run)
        {
            foreach (var action in onRunDestroyExecute)
                action();
        }
        #endregion

        #region Dev
        /// <summary>
        /// Dev mode invulnerability
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <param name="damageInfo"></param>
        private static void HealthComponent_TakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {

            if (self.body.teamComponent.teamIndex == TeamIndex.Player)
            {
                damageInfo.damage = 0;
                damageInfo.force = Vector3.zero;
            }
            else
            {
                damageInfo.damage *= 100;
            }

            orig(self, damageInfo);
        }
        #endregion

    }
}
