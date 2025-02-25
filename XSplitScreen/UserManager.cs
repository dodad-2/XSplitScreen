using DoDad.XSplitScreen.Assignments;
using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using Rewired;
using System.Linq;
using UnityEngine;
using DoDad.XLibrary.Toolbox;
using System.Reflection;
using RoR2.UI;

namespace DoDad.XSplitScreen
{
    public static class UserManager
    {
        #region Variables
        public static Action onSplitscreenEnabled;
        public static Action onSplitscreenDisabled;
        /// <summary>
        /// A list of currently loaded local users. Do not manually modify this list.
        /// </summary>
        public static List<Assignment> localUsers { get; private set; } = new List<Assignment>();

        private static Rect[][] originalLayouts;
        #endregion

        #region Public Methods
        /// <summary>
        /// Loads the given assignments or if no users are provided disables splitscreen
        /// </summary>
        /// <param name="users">Leave null to disable splitscreen</param>
        /// <returns></returns>
        public static bool AssignSplitscreenUsers(List<Assignment> assignments, out List<Assignment> invalidAssignments, out ErrorType pluginError)
        {
            pluginError = ErrorType.NONE;
            invalidAssignments = assignments;

            if (!LocalUserManager.IsUserChangeSafe())
            {
				//Log.LogOutput($"Can't call AssignSplitscreenUsers: user login changes are not safe at this time"); // 4.0.0 rewrite 9-12-24
				return false;
            }

            if (localUsers.Count > 0 || LocalUserManager.localUsersList.Count() > 0)
                UnloadLocalUsers();

            if (assignments == null || assignments.Count < 2)
            {
                LoadDefaultUser();
                pluginError = ErrorType.MINIMUM;
				//Log.LogOutput($"Assignments were null or less than 2: no splitscreen users were created.", Log.LogLevel.Info); // 4.0.0 rewrite 9-12-24
				return false;
            }

            var invalidUsers = ValidateAssignments(assignments);

            if (invalidUsers.Count > 0)
            {
                LoadDefaultUser();
                return false;
            }

            localUsers.AddRange(assignments);

            try
            {
                int localId = 0;

                Debug.Log($"Attempting to activate '{localUsers.Count}' users");
				Debug.Log($"ReInput.players.playerCount = '{ReInput.players.playerCount}' users");

                if (ReInput.players.playerCount == 2)
                    return false;

                RoR2Application.SetIsInLocalMultiplayer(true);

				foreach (Assignment assignment in localUsers)
                {
                    var player = ReInput.players.GetPlayer(localId);

                    Debug.Log($"ReInput player id '{localId}' is '{GetProfile(assignment.profile).name}'");
                    player.controllers.ClearAllControllers();
                    player.controllers.AddController(assignment.controller, false);
                    
                    LocalUserManager.AddUser(player, GetProfile(assignment.profile));

                    var splitscreenUser = LocalUserManager.localUsersList[localId] as LocalSplitscreenUser;
                    splitscreenUser.assignment = assignment;

                    assignment.localId = localId;

                    localId++;
                }

                bool foundKeyboard = false;

                foreach (LocalUser user in LocalUserManager.localUsersList)
                {
                    if (user.inputPlayer.controllers.hasKeyboard)
                    {
                        AssignControllerToPlayer(ReInput.controllers.Mouse, user);
                        foundKeyboard = true;
                    }
                }

                if (!foundKeyboard)
                {
                    var user = LocalUserManager.GetFirstLocalUser();
                    AssignControllerToPlayer(ReInput.controllers.Mouse, user);
                    AssignControllerToPlayer(ReInput.controllers.Keyboard, user);
                }

                HookManager.UpdateHooks(HookType.Splitscreen, true);
				//Log.LogOutput($" -- Splitscreen hooks enabled"); // 4.0.0 rewrite 9-12-24

				foreach (MPEventSystem system in MPEventSystem.instancesList)
                {
                    if (system.isCombinedEventSystem)
                        continue;

                    system.cursorIndicatorController.mouseCursorSet.GetGameObject(CursorIndicatorController.CursorImage.Pointer).SetActive(false);

                    if (system.player.controllers.Controllers.Count() > 0)
                        system.OnLastActiveControllerChanged(system.player, system.player.controllers.Controllers.First());
                }

                MPEventSystem.RecenterCursors();
                //RoR2.Console.instance.RunClientCmd(LocalUserManager.GetFirstLocalUser().currentNetworkUser, "export_controller_maps", new string[0]);

                UpdateCameraRects(true);
                SetListeners(true);
                onSplitscreenEnabled?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                UnloadLocalUsers();
				//Log.LogOutput($"Unable to assign splitscreen users: {e}", Log.LogLevel.Error); // 4.0.0 rewrite 9-12-24
				return false;
            }
        }
        #endregion

        #region Splitscreen Methods
        public static void SetListeners(bool state)
        {
            if (state)
            {
                CameraRigController.onCameraEnableGlobal += CameraRigController_onCameraEnableGlobal;
            }
            else
            {
                CameraRigController.onCameraEnableGlobal -= CameraRigController_onCameraEnableGlobal;
            }
        }
        /// <summary>
        /// Why is this here and not in Hook Manager?
        /// </summary>
        /// <param name="obj"></param>
        private static void CameraRigController_onCameraEnableGlobal(CameraRigController obj)
        {
            if (obj.localUserViewer?.eventSystem?.localUser == null || Run.instance != null)
                return;

            LocalSplitscreenUser user = obj.localUserViewer.eventSystem.localUser as LocalSplitscreenUser;

            if (user != null && user.assignment.display > 0 && user.assignment.display < Display.displays.Count())
            {
                obj.sceneCam.targetDisplay = user.assignment.display;

                if (!Display.displays[user.assignment.display].active)
                    Display.displays[user.assignment.display].Activate();
            }
        }
        private static void Run_onRunStartGlobal(Run obj, PlayerCharacterMasterController player)
        {
			//Log.LogOutput($"Run started: {CameraRigController.instancesList.Count}"); // 4.0.0 rewrite 9-12-24
		}
		private static void UpdateCameraRects(bool state)
        {
            var layout = originalLayouts;

            if (state)
            {
                originalLayouts = RunCameraManager.ScreenLayouts;

                //int modifiedLocalCount = localUsers.Count + 1;
                int localCount = localUsers.Count;//modifiedLocalCount - 1;

                Rect[][] newLayout = RunCameraManager.ScreenLayouts;

                newLayout[localCount] = new Rect[localCount];

                IOrderedEnumerable<Assignment> ordered = localUsers.OrderBy(x => x.localId);

                var sortedUsers = ordered.ToArray();

                for (int e = 0; e < localCount; e++)
                {
                    newLayout[localCount][e] = new Rect();
                    newLayout[localCount][e] = sortedUsers[e].GetScreen();
                }

                layout = newLayout;
                //RunCameraManager.screenLayouts = newLayout;
            }
            else
            {
                //if (originalLayouts != null)
                //    RunCameraManager.screenLayouts = originalLayouts;
            }

            if(layout != null)
                typeof(RunCameraManager).GetField("ScreenLayouts", BindingFlags.Static | BindingFlags.Public).SetValue(null, layout);
        }
        private static void AssignControllerToPlayer(Controller controller, LocalUser user)
        {
            user.inputPlayer.controllers.AddController(controller, false);
            user.ApplyUserProfileBindingsToRewiredPlayer();

        }
        private static void LoadDefaultUser()
        {
            try
            {
                LocalUserManager.AddUser(LocalUserManager.GetRewiredMainPlayer(), PlatformSystems.saveSystem.loadedUserProfiles.First().Value);
                ReInput.controllers.AutoAssignJoysticks();
                onSplitscreenDisabled?.Invoke();
            }
            catch (Exception e)
            {
				//Log.LogOutput($"Unable to disable splitscreen: {e}", Log.LogLevel.Error); // 4.0.0 rewrite 9-12-24
			}
		}
        private static void UnloadLocalUsers()
        {
            if (localUsers.Count > 0)
                HookManager.UpdateHooks(HookType.Splitscreen, false);

            UpdateCameraRects(false);
            SetListeners(false);
            localUsers.Clear();
            LocalUserManager.ClearUsers();
        }
        private static bool SetLocalUsers(List<LocalUserManager.LocalUserInitializationInfo> users)
        {
            if (users == null || users.Count == 0)
                return false;

            try
            {
                LocalUserManager.SetLocalUsers(users.ToArray());
            }
            catch (Exception e)
            {
				//Log.LogOutput($"Unable to set local users: {e}", Log.LogLevel.Error); // 4.0.0 rewrite 9-12-24
				return false;
            }

            return true;
        }
        private static List<Assignment> ValidateAssignments(List<Assignment> assignments)
        {
            List<Assignment> invalidUsers = new List<Assignment>();

            foreach (Assignment assignment in assignments)
            {
                bool invalid = false;

                if (assignment == null)
                {
                    invalid = true;
                    assignment.error = ErrorType.OTHER;
                }

                if (!invalid && assignment.controller == null || assignments.Where(x => x.controller == assignment.controller).Count() > 1) // controller
                {
                    invalid = true;
                    assignment.error = ErrorType.CONTROLLER;
                }

                if (!invalid && assignment.display < 0 || assignment.display >= Display.displays.Length) // display
                {
                    invalid = true;
                    assignment.error = ErrorType.DISPLAY;
                }

                if (!invalid && assignment.profile == null || !PlatformSystems.saveSystem.GetAvailableProfileNames().Contains(assignment.profile)
                    || assignments.Where(x => x.profile.Equals(assignment.profile)).Count() > 1) // profile
                {
                    invalid = true;
                    assignment.error = ErrorType.PROFILE;
                }

                if (!invalid && !assignment.position.IsInRange(int2.zero, AssignmentGraph.graphDimensions) ||
                    assignments.Where(x => x.position.Equals(assignment.position) && x.display == assignment.display).Count() > 1) // position
                {
                    invalid = true;
                    assignment.error = ErrorType.POSITION;
                }

                if (invalid)
                    invalidUsers.Add(assignment);
                else
                    assignment.error = ErrorType.NONE;
            }

            return invalidUsers;
        }
        #endregion

        #region Event Handlers
        public static void Test()
        {

        }
        #endregion

        #region Helpers
        private static UserProfile GetProfile(string profile)
        {
            return PlatformSystems.saveSystem.GetProfile(profile);
        }
        #endregion
    }
}
