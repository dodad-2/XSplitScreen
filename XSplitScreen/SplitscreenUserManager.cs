using Rewired;
using RoR2;
using System;
using System.Collections.Generic;
using System.Reflection;
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
		/// Initializes a new local user from assignment data
		/// </summary>
		/// <param name="data">User assignment data</param>
		public LocalUser(UserAssignmentData data)
		{
			Profile = data.Profile;
			UserIndex = data.UserIndex;
			InputPlayer = data.InputPlayer;
		}

		/// <summary>
		/// Gets a specific settings module for this user
		/// </summary>
		/// <typeparam name="T">Type of the settings module to retrieve</typeparam>
		/// <returns>Settings module instance</returns>
		public T GetSettingsModule<T>() where T : class, IUserSettingsModule, new()
		{
			return SplitScreenSettings.GetUserModule<T>(Profile.saveID);
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
		private static List<LocalUser> localUsers = new List<LocalUser>();

		/// <summary>
		/// Indicates if splitscreen is currently enabled
		/// </summary>
		private static bool isSplitscreenEnabled = false;

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
				Print($"Added local user with SaveID: {user.Profile.name}, UserIndex: {user.UserIndex}", ELogChannel.Debug);
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

			// Load local users

			List<LocalUserManager.LocalUserInitializationInfo> init = new List<LocalUserManager.LocalUserInitializationInfo>();

			foreach (var user in localUsers)
			{
				Print($"User '{user.Profile.name}' with InputPlayer '{user.InputPlayer.name}'");
				init.Add(new LocalUserManager.LocalUserInitializationInfo()
				{
					player = user.InputPlayer,
					profile = user.Profile
				});
			}

			var oldPlayer = RoR2.UI.MPEventSystem.FindByPlayer(localUsers[0].InputPlayer);

			Log.Print($"OLD PLAYER -> '{oldPlayer.name}'");
			LocalUserManager.ClearUsers();
			LocalUserManager.SetLocalUsers(init.ToArray());
			oldPlayer = RoR2.UI.MPEventSystem.FindByPlayer(localUsers[0].InputPlayer);
			Log.Print($"NEW PLAYER -> '{oldPlayer.name}'");

			isSplitscreenEnabled = true;

			//RoR2Application.SetIsInLocalMultiplayer(true);

			//RoR2.Console.instance.SubmitCmd(null, "transition_command \"gamemode ClassicRun; host 0;\"");
		}

		/// <summary>
		/// Disables splitscreen mode and removes hooks
		/// </summary>
		public static void DisableSplitscreen()
		{
			if (!isSplitscreenEnabled)
				return;

			Print("Disabling splitscreen mode", ELogChannel.Info);

			var profile = LocalUserManager.localUsersList[0].userProfile;

			var playerMain = ReInput.players.GetPlayer("PlayerMain");

			foreach(var player in ReInput.players.Players)
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

			isSplitscreenEnabled = false;
		}

		/// <summary>
		/// Gets the list of current local users
		/// </summary>
		/// <returns>A copy of the local users list</returns>
		public static List<LocalUser> GetLocalUsers()
		{
			return new List<LocalUser>(localUsers);
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

		/// <summary>
		/// Gets a local user by their save ID
		/// </summary>
		/// <param name="saveID">The user's save ID</param>
		/// <returns>The local user or null if not found</returns>
		public static LocalUser GetUserBySaveID(ulong saveID)
		{
			return localUsers.Find(user => user.Profile.saveID == saveID);
		}

		/// <summary>
		/// Checks if splitscreen mode is currently enabled
		/// </summary>
		/// <returns>True if splitscreen is enabled, false otherwise</returns>
		public static bool IsSplitscreenEnabled()
		{
			return isSplitscreenEnabled;
		}
	}
}