using Rewired;
using RoR2;
using RoR2.UI.MainMenu;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Dodad.XSplitscreen.Log;

namespace Dodad.XSplitscreen
{
	/// <summary>
	/// Data structure representing user assignment info from the assignment screen
	/// </summary>
	public class UserAssignmentData
	{
		/// <summary>
		/// User Profile
		/// </summary>
		public UserProfile Profile { get; set; }

		/// <summary>
		/// Local index for the player (0 for first player, 1 for second, etc.)
		/// </summary>
		public int UserIndex { get; set; }

		public Player InputPlayer { get; set; }

		public UnityEngine.Rect CameraRect { get; set; }

		public int Display { get; set; }
	}

	/// <summary>
	/// Represents a local user in the splitscreen session
	/// </summary>
	public class LocalUser
	{
		/// <summary>
		/// User Profile
		/// </summary>
		public UserProfile Profile { get; set; }

		/// <summary>
		/// Local index for the player (0 for first player, 1 for second, etc.)
		/// </summary>
		public int UserIndex { get; private set; }

		/// <summary>
		/// Local Rewired input player
		/// </summary>
		public Player InputPlayer { get; private set; }

		/// <summary>
		/// Screen area for the player
		/// </summary>
		public UnityEngine.Rect CameraRect { get; private set; }

		public int Display { get; private set; }

		public Transform ParticleSystem;

		/// <summary>
		/// Initializes a new local user from assignment data
		/// </summary>
		/// <param name="data">User assignment data</param>
		public LocalUser(UserAssignmentData data)
		{
			Profile = data.Profile;
			UserIndex = data.UserIndex;
			InputPlayer = data.InputPlayer;
			CameraRect = data.CameraRect;
			Display = data.Display;
		}

		/// <summary>
		/// Gets a specific settings module for this user
		/// </summary>
		/// <typeparam name="T">Type of the settings module to retrieve</typeparam>
		/// <returns>Settings module instance</returns>
		public T GetSettingsModule<T>() where T : class, IUserSettingsModule, new()
		{
			return SplitScreenSettings.GetUserModule<T>(Profile.fileName);
		}
	}

	/// <summary>
	/// Static manager for splitscreen users and hooks
	/// </summary>
	public static class SplitscreenUserManager
	{
		/// <summary>
		/// List of local users for splitscreen
		/// </summary>
		public static IReadOnlyList<LocalUser> LocalUsers => localUsers.AsReadOnly();

		private static readonly List<LocalUser> localUsers = new List<LocalUser>();

		/// <summary>
		/// Invoked when splitscreen is enabled or disabled.
		/// </summary>
		public static Action<bool> OnStateChange;

		public static bool IsSplitscreenEnabled => isSplitscreenEnabled;

		/// <summary>
		/// Indicates if splitscreen is currently enabled
		/// </summary>
		private static bool isSplitscreenEnabled = false;

		/// <summary>
		/// Set to true before enabling splitscreen for online play.
		/// </summary>
		public static bool OnlineMode;

		/// <summary>
		/// Receives user assignment data and creates local users
		/// </summary>
		/// <param name="userAssignments">List of user assignment info (saveID, userIndex)</param>
		public static void InitializeUsers(List<UserAssignmentData> userAssignments)
		{
			localUsers.Clear();

			foreach (var assignment in userAssignments)
			{
				LocalUser user = new LocalUser(assignment);
				localUsers.Add(user);
				Print($"Added local user: '{user.Profile.name}', UserIndex: '{user.UserIndex}', Display: '{user.Display}',", ELogChannel.Debug);
			}
		}

		/// <summary>
		/// Enables splitscreen mode by applying runtime hooks
		/// </summary>
		public static void EnableSplitscreen()
		{
			if (isSplitscreenEnabled || localUsers.Count <= 1)
			{
				if (localUsers.Count <= 1)
				{
					Print("Cannot enable splitscreen with fewer than 2 users", ELogChannel.Warning);
				}
				return;
			}

			Print($"Enabling splitscreen mode for {localUsers.Count} users", ELogChannel.Info);

			isSplitscreenEnabled = true;

			RoR2Application.SetIsInLocalMultiplayer(true);
			LoadLocalUsers();
			OnStateChange?.Invoke(true);
			TransitionToLobby();
		}

		/// <summary>
		/// Disables splitscreen mode and removes hooks
		/// </summary>
		public static void DisableSplitscreen()
		{
			if (!isSplitscreenEnabled)
				return;

			Print("Disabling splitscreen mode", ELogChannel.Info);

			isSplitscreenEnabled = false;

			ClearLocalUsers();
			OnStateChange?.Invoke(false);
		}

		private static void TransitionToLobby()
		{
			//RoR2.Console.instance.SubmitCmd(null, "transition_command \"gamemode ClassicRun; host 0;\"");
			if (OnlineMode)
				MainMenuController.instance.SetDesiredMenuScreen(MainMenuController.instance.multiplayerMenuScreen);
			//	PlatformSystems.lobbyManager.EnterGameButtonPressed();
			else
				RoR2.Console.instance.SubmitCmd(null, "transition_command \"gamemode ClassicRun; host 0;\"");
		}

		private static void LoadLocalUsers()
		{
			// Load local users

			List<LocalUserManager.LocalUserInitializationInfo> init = new();

			foreach (var user in localUsers)
			{
				init.Add(new LocalUserManager.LocalUserInitializationInfo()
				{
					player = user.InputPlayer,
					profile = user.Profile
				});

				user.InputPlayer.isPlaying = true;
			}

			LocalUserManager.ClearUsers();
			LocalUserManager.SetLocalUsers([.. init]);
		}

		private static void ClearLocalUsers()
		{
			localUsers.Clear();

			var profile = LocalUserManager.localUsersList[0].userProfile;

			var playerMain = ReInput.players.GetPlayer("PlayerMain");

			foreach (var player in ReInput.players.Players)
			{
				if (player.name == "PlayerMain" || player.name == "System")
					continue;

				foreach (var controller in player.controllers.Controllers)
					playerMain.controllers.AddController(controller, false);

				player.controllers.ClearAllControllers();
			}

			LocalUserManager.ClearUsers();
			LocalUserManager.SetLocalUsers(new LocalUserManager.LocalUserInitializationInfo[1]
			{
				new LocalUserManager.LocalUserInitializationInfo()
				{
					profile = profile
				}
			});
		}

		/// <summary>
		/// Gets the list of current local users
		/// </summary>
		/// <returns>A copy of the local users list</returns>
		public static List<LocalUser> GetLocalUsers()
		{
			return new List<LocalUser>(localUsers);
		}

		public static LocalUser GetUserByInputName(string name)
		{
			return localUsers.Find(user => user.InputPlayer.name == name);
		}

		/// <summary>
		/// Gets a local user by their user index
		/// </summary>
		/// <param name="userIndex">The user's index</param>
		/// <returns>The local user or null if not found</returns>
		public static LocalUser GetUserByIndex(int userIndex)
		{
			return localUsers.Find(user => user.UserIndex == userIndex);
		}
	}
}