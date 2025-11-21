using MonoMod.Cil;
using Rewired;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using static Dodad.XSplitscreen.Log;
using UnityEngine;
using RoR2;
using Mono.Cecil.Cil;
using R2API.Utils;
using RoR2.UI;
using System.Linq;

namespace Dodad.XSplitscreen
{
	public static class HookManager
	{
		public static void OnStateChange(bool state)
		{
			Log.Print($"HookManager::OnStateChange -> '{state}'");

			ResizePlayerCount(state);
			UpdateCameraRects(state);
			UpdateMultiplayerColors(state);
			UpdateDisplayHook(state);
			UpdateTrailHooks(state);
			UpdateMPEventHooks(state);
			UpdateChatHooks(state);
			UpdateSubscriptions(state);
			UpdateRunCameraManager(state);
		}

		private static void ResizePlayerCount(bool state)
		{
			int playerCount = 4;

			if (state)
				playerCount = Math.Max(4, SplitscreenUserManager.LocalUsers.Count);

			RoR2.LobbyManager.cvSteamLobbyMaxMembers.defaultValue = playerCount.ToString();
			Reflection.SetPropertyValue<int>((object) RoR2.LobbyManager.cvSteamLobbyMaxMembers, "value", playerCount);
			RoR2.Networking.NetworkManagerSystem.SvMaxPlayersConVar.instance.SetString(playerCount.ToString());
		}

		private static MethodInfo RunCameraManager_Udpate_Orig;
		private static MethodInfo RunCameraManager_Udpate_Patch;
		private static void UpdateRunCameraManager(bool isSplitscreenEnabled)
		{
			if (isSplitscreenEnabled)
			{
				IL.RoR2.RunCameraManager.Update += RunCameraManager_Update_IL;
			}
			else
			{
				IL.RoR2.RunCameraManager.Update -= RunCameraManager_Update_IL;
			}
		}

		public static void LogInputPlayerName(string name)
		{
			/*if (name == "PlayerMain" ||
				name == "Player2" ||
				name == "Player3" ||
				name == "Player4")
				return;*/

			Log.Print($"HookManager::LogInputPlayerName: '{name}'");
		}

		private static void RunCameraManager_Update_IL(ILContext il)
		{
			ILCursor c = new ILCursor(il);

			if (c.TryGotoNext(
					x => x.MatchLdloc(16),
					x => x.MatchCallvirt<RoR2.LocalUser>("get_inputPlayer"),
					x => x.MatchCallvirt<Rewired.Player>("get_name"),
					x => x.MatchLdstr("PlayerMain"),
					x => x.MatchCall<string>("op_Equality")
				))
			{
				// Store name in local 18 for fallback logic
				c.Index += 3;
				c.Emit(OpCodes.Dup);
				c.Emit(OpCodes.Stloc_S, (byte) 18); // local 18 = playerName

				// Check against PlayerMain, Player2, Player3, Player4
				string[] names = { "PlayerMain", "Player2", "Player3", "Player4" };
				var stringEquality = typeof(string).GetMethod("op_Equality", new[] { typeof(string), typeof(string) });

				ILLabel continueLabel = c.DefineLabel();

				foreach (var name in names)
				{
					c.Emit(OpCodes.Dup);               // [name, name]
					c.Emit(OpCodes.Ldstr, name);       // [name, name, "PlayerN"]
					c.Emit(OpCodes.Call, stringEquality); // [name, bool]
					c.Emit(OpCodes.Brtrue_S, continueLabel); // If true, jump to original logic
				}

				// If none matched, pop the name and instantiate the fallback prefab
				c.Emit(OpCodes.Pop);
				c.Emit(OpCodes.Ldarg_0); // this
				c.Emit(OpCodes.Ldfld, typeof(RoR2.RunCameraManager).GetField("CharacterSelectUILocal"));
				c.Emit(OpCodes.Call, typeof(UnityEngine.Object).GetMethod("Instantiate", new[] { typeof(UnityEngine.GameObject) }));
				c.Emit(OpCodes.Stloc_S, (byte) 17);

				// Set MPEventSystemProvider.eventSystem on the new GameObject
				// Load gameObject (local 17)
				c.Emit(OpCodes.Ldloc_S, (byte) 17);
				// GetComponent<MPEventSystemProvider>()
				var getComponentGeneric = typeof(UnityEngine.GameObject)
					.GetMethods()
					.First(m => m.Name == "GetComponent" && m.IsGenericMethod && m.GetGenericArguments().Length == 1 && m.GetParameters().Length == 0)
					.MakeGenericMethod(typeof(MPEventSystemProvider));
				c.Emit(OpCodes.Callvirt, getComponentGeneric);

				// Load playerName (local 18)
				c.Emit(OpCodes.Ldloc_S, (byte) 18);

				// LocalUserManager.FindLocalUserByPlayerName(playerName)
				var findLocalUserByPlayerName = typeof(LocalUserManager).GetMethod("FindLocalUserByPlayerName", new[] { typeof(string) });
				c.Emit(OpCodes.Call, findLocalUserByPlayerName);

				// Null check for LocalUser
				c.Emit(OpCodes.Dup);
				ILLabel notNull = c.DefineLabel();
				c.Emit(OpCodes.Brtrue_S, notNull);
				c.Emit(OpCodes.Pop); // remove null
				c.Emit(OpCodes.Ldnull);
				ILLabel afterInputPlayer = c.DefineLabel();
				c.Emit(OpCodes.Br_S, afterInputPlayer);

				// If not null, get inputPlayer property
				c.MarkLabel(notNull);
				var inputPlayerGetter = typeof(RoR2.LocalUser).GetProperty("inputPlayer").GetGetMethod();
				c.Emit(OpCodes.Callvirt, inputPlayerGetter);
				c.MarkLabel(afterInputPlayer);

				// MPEventSystem.FindByPlayer(inputPlayer)
				var findByPlayer = typeof(MPEventSystem).GetMethod("FindByPlayer", new[] { typeof(Rewired.Player) });
				c.Emit(OpCodes.Call, findByPlayer);

				// Set eventSystem property
				var eventSystemSetter = typeof(MPEventSystemProvider).GetProperty("eventSystem").GetSetMethod();
				c.Emit(OpCodes.Callvirt, eventSystemSetter);

				// Jump past the original if/else
				ILLabel afterInstantiate = c.DefineLabel();
				c.Emit(OpCodes.Br_S, afterInstantiate);

				// Continue original logic
				c.MarkLabel(continueLabel);

				// (The original code for PlayerMain/Player2/3/4 continues here)

				c.MarkLabel(afterInstantiate);
				// (After the fallback logic, execution continues here)
			}
			else
			{
				Log.Print($"Could not hook '{il.Method.Name}'", ELogChannel.Error);
			}
		}

		/*		private static void RunCameraManager_Update_IL(ILContext il)
				{
					ILCursor c = new ILCursor(il);

					if (c.TryGotoNext(
							x => x.MatchLdloc(16),
							x => x.MatchCallvirt<RoR2.LocalUser>("get_inputPlayer"),
							x => x.MatchCallvirt<Rewired.Player>("get_name"),
							x => x.MatchLdstr("PlayerMain"),
							x => x.MatchCall<string>("op_Equality")
						))
					{
						c.Index += 3;

						MethodInfo logMethod = typeof(HookManager).GetMethod(nameof(LogInputPlayerName), BindingFlags.Public | BindingFlags.Static);

						if (logMethod == null)
						{
							Log.Print("RunCameraManager_Update_IL: failed to find LogInputPlayerName method", ELogChannel.Error);

							return;
						}

						c.Emit(OpCodes.Dup);
						c.Emit(OpCodes.Call, logMethod);
					}
					else
					{
						Log.Print($"Could not hook '{il.Method.Name}'", ELogChannel.Error);
					}
				}*/

		private static RoR2.UI.ChatBox ChatInstance;
		private static MethodInfo ChatBox_Update_Orig;
		private static MethodInfo ChatBox_Update_Patch;
		private static void UpdateChatHooks(bool isSplitscreenEnabled)
		{
			ChatBox_Update_Orig ??= typeof(RoR2.UI.ChatBox).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
			ChatBox_Update_Patch ??= typeof(HookManager).GetMethod("ChatBox_Update", BindingFlags.Static | BindingFlags.NonPublic);

			if (isSplitscreenEnabled)
			{
				Plugin.Patcher.Patch(ChatBox_Update_Orig, prefix: new HarmonyLib.HarmonyMethod(ChatBox_Update_Patch));
			}
			else
			{
				Plugin.Patcher.Unpatch(ChatBox_Update_Orig, ChatBox_Update_Patch);
			}
		}

		private static bool ChatBox_Update(RoR2.UI.ChatBox __instance)
		{
			if (ChatInstance == null)
				ChatInstance = __instance;

			if (ChatInstance != __instance)
				return false;

			return true;
		}

		private static MethodInfo PlayerCharacterMasterController_OnBodyStart_Orig;
		private static MethodInfo PlayerCharacterMasterController_OnBodyStart_Patch;
		private static void UpdateTrailHooks(bool isSplitscreenEnabled)
		{
			PlayerCharacterMasterController_OnBodyStart_Orig ??= typeof(RoR2.PlayerCharacterMasterController).GetMethod("SetBody", BindingFlags.Instance | BindingFlags.NonPublic);
			PlayerCharacterMasterController_OnBodyStart_Patch ??= typeof(HookManager).GetMethod("PlayerCharacterMasterController_SetBody", BindingFlags.Static | BindingFlags.NonPublic);

			if (isSplitscreenEnabled)
			{
				Plugin.Patcher.Patch(PlayerCharacterMasterController_OnBodyStart_Orig, postfix: new HarmonyLib.HarmonyMethod(PlayerCharacterMasterController_OnBodyStart_Patch));
			}
			else
			{
				Plugin.Patcher.Unpatch(PlayerCharacterMasterController_OnBodyStart_Orig, PlayerCharacterMasterController_OnBodyStart_Patch);
			}
		}

		private static void PlayerCharacterMasterController_SetBody(RoR2.PlayerCharacterMasterController __instance, GameObject __0)
		{
			if (__0 == null)
				return;

			if (__instance.networkUser != null && __instance.networkUser.isLocalPlayer && __instance.networkUser.localUser != null)
			{
				var user = SplitscreenUserManager.GetUserByInputName(__instance.networkUser.localUser.inputPlayer.name);

				if (user.ParticleSystem != null)
					return;

				if (__instance.networkUser.localUser.userProfile.fileName == null)
					return;

				var trailKey = SplitScreenSettings.GetOrCreateUserModule<TrailsSettingsModule>(__instance.networkUser.localUser.userProfile.fileName).TrailKey;

				if (trailKey == "none")
				{
					user.ParticleSystem = new GameObject("Dummy").transform;

					return;
				}

				var particleSystem = ParticleSystemFactory.GetParticleSystem(trailKey);
				particleSystem.transform.SetParent(__instance.body.transform);
				particleSystem.transform.localPosition = Vector3.zero;
				particleSystem.gameObject.SetActive(true);
				user.ParticleSystem = particleSystem.transform;
			}
		}

		private static void UpdateMultiplayerColors(bool isSplitscreenEnabled)
		{
			var field = typeof(ColorCatalog).GetField("multiplayerColors", BindingFlags.Static | BindingFlags.NonPublic);

			var multiplayerColors = new Color[4]
			{
					new Color32(252, 62, 62, byte.MaxValue),
					new Color32(62, 109, 252, byte.MaxValue),
					new Color32(129, 252, 62, byte.MaxValue),
					new Color32(252, 241, 62, byte.MaxValue)
			};

			if (isSplitscreenEnabled)
			{
				var localUsers = SplitscreenUserManager.LocalUsers;

				multiplayerColors = new Color[localUsers.Count];

				for (int e = 0; e < localUsers.Count; e++)
					multiplayerColors[e] = SplitScreenSettings.GetUserModule<ColorSettingsModule>(localUsers[e].Profile?.fileName ?? string.Empty)?.Color ?? Color.white;
			}

			field.SetValue(null, multiplayerColors);
		}

		private static MethodInfo CameraRigController_Start_Orig;
		private static MethodInfo CameraRigController_Start_Patch;
		private static void UpdateDisplayHook(bool isSplitscreenEnabled)
		{
			CameraRigController_Start_Orig ??= typeof(RoR2.CameraRigController).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
			CameraRigController_Start_Patch ??= typeof(HookManager).GetMethod("CameraRigController_Start", BindingFlags.Static | BindingFlags.NonPublic);

			if (isSplitscreenEnabled)
			{
				Plugin.Patcher.Patch(CameraRigController_Start_Orig, postfix: new HarmonyLib.HarmonyMethod(CameraRigController_Start_Patch));
			}
			else
			{
				Plugin.Patcher.Unpatch(CameraRigController_Start_Orig, CameraRigController_Start_Patch);
			}
		}

		private static void CameraRigController_Start(RoR2.CameraRigController __instance)
		{
			Components.ExecuteNextFrame.Invoke(() =>
			{
				if (__instance == null || RunCameraManager.instance == null)
					return;

				var field = typeof(RunCameraManager).GetField("cameras", BindingFlags.Instance | BindingFlags.NonPublic);
				var cameraDict = (Dictionary<string, CameraRigController>) field.GetValue(RunCameraManager.instance);
				string userName = null;

				foreach (var kvp in cameraDict)
				{
					Log.Print($"CameraRigController_Start: kvp -> '{kvp.Key}'");
					if (kvp.Value == __instance)
					{
						Log.Print($"CameraRigController_Start: '{kvp.Key}' accepted");
						userName = kvp.Key;
						break;
					}
				}

				if (userName == null)
					return;

				int targetDisplay = SplitscreenUserManager.GetUserByInputName(userName).Display;
				Log.Print($"CameraRigController_Start: User '{userName}' on display '{targetDisplay}'");
				Log.Print($"Null -> sceneCam = '{__instance.sceneCam == null}', uiCam = '{__instance.uiCam == null}', skyboxCam = '{__instance.skyboxCam == null}'");
				if (__instance.sceneCam != null)
					__instance.sceneCam.targetDisplay = targetDisplay;
				if (__instance.uiCam != null)
					__instance.uiCam.targetDisplay = targetDisplay;
				if (__instance.skyboxCam != null)
					__instance.skyboxCam.targetDisplay = targetDisplay;
			});
		}

		private static MethodInfo MPEventSystem_OnLastActiveControllerChanged_Orig;
		private static MethodInfo MPEventSystem_OnLastActiveControllerChanged_Patch;
		private static void UpdateMPEventHooks(bool isSplitscreenEnabled)
		{
			MPEventSystem_OnLastActiveControllerChanged_Orig ??= typeof(RoR2.UI.MPEventSystem).GetMethod("OnLastActiveControllerChanged", BindingFlags.Instance | BindingFlags.NonPublic);
			MPEventSystem_OnLastActiveControllerChanged_Patch ??= typeof(HookManager).GetMethod("MPEventSystem_OnLastActiveControllerChanged", BindingFlags.Static | BindingFlags.NonPublic);

			if (isSplitscreenEnabled)
			{
				Plugin.Patcher.Patch(MPEventSystem_OnLastActiveControllerChanged_Orig, postfix: new HarmonyLib.HarmonyMethod(MPEventSystem_OnLastActiveControllerChanged_Patch));
			}
			else
			{
				Plugin.Patcher.Unpatch(MPEventSystem_OnLastActiveControllerChanged_Orig, MPEventSystem_OnLastActiveControllerChanged_Patch);
			}
		}

		private static void MPEventSystem_OnLastActiveControllerChanged(RoR2.UI.MPEventSystem __instance, Player __0, Controller __1)
		{
			if (__1 == null)
				__instance.currentInputSource = RoR2.UI.MPEventSystem.InputSource.Gamepad;
		}

		private static void UpdateSubscriptions(bool isSplitscreenEnabled)
		{
			if (isSplitscreenEnabled)
				ReInput.ControllerConnectedEvent += OnControllerConnected;
			else
				ReInput.ControllerConnectedEvent -= OnControllerConnected;
		}

		private static void OnControllerConnected(ControllerStatusChangedEventArgs args)
		{
			var localUsers = SplitscreenUserManager.LocalUsers;

			foreach (var user in localUsers)
			{
				if (user.InputPlayer.controllers.joystickCount == 0)
					user.InputPlayer.controllers.AddController(args.controller, true);
			}
		}

		private static void UpdateCameraRects(bool isSplitscreenEnabled)
		{
			var field = typeof(RunCameraManager).GetField("ScreenLayouts", BindingFlags.Static | BindingFlags.Public);

			Rect[][] rects = new Rect[5][]
			{
				new Rect[0],
				new Rect[1]
				{
					new Rect(0f, 0f, 1f, 1f)
				},
				new Rect[2]
				{
					new Rect(0f, 0.5f, 1f, 0.5f),
					new Rect(0f, 0f, 1f, 0.5f)
				},
				new Rect[3]
				{
					new Rect(0f, 0.5f, 1f, 0.5f),
					new Rect(0f, 0f, 0.5f, 0.5f),
					new Rect(0.5f, 0f, 0.5f, 0.5f)
				},
				new Rect[4]
				{
					new Rect(0f, 0.5f, 0.5f, 0.5f),
					new Rect(0.5f, 0.5f, 0.5f, 0.5f),
					new Rect(0f, 0f, 0.5f, 0.5f),
					new Rect(0.5f, 0f, 0.5f, 0.5f)
				}
			};

			if (isSplitscreenEnabled)
			{
				var localUsers = SplitscreenUserManager.LocalUsers;

				rects = new UnityEngine.Rect[localUsers.Count + 1][];
				rects[0] = new UnityEngine.Rect[0];
				rects[1] = new Rect[1]
				{
				new Rect(0f, 0f, 1f, 1f)
				};

				var uRects = new UnityEngine.Rect[localUsers.Count];

				for (int e = 0; e < uRects.Length; e++)
				{
					uRects[e] = localUsers[e].CameraRect;
					Log.Print($"Adding '{localUsers[e].UserIndex}' with rect '{localUsers[e].CameraRect}'");
				}

				rects[localUsers.Count] = uRects;
			}

			field.SetValue(null, rects);

			for (int e = 0; e < rects.Length; e++)
			{
				if (rects[e] == null)
					continue;

				for (int r = 0; r < rects[e].Length; r++)
				{
					Log.Print($"Rect[{e}][{r}] -> {rects[e][r]}");
				}
			}
		}
	}
}
