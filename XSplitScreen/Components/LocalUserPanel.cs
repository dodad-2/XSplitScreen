using DoDad.XSplitScreen;
using Rewired;
using RoR2;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace dodad.XSplitscreen.Components
{
	internal class LocalUserPanel : MonoBehaviour
	{
		internal static List<LocalUserPanel> Instances { get; private set; }

		/// <summary>
		/// Should players be allowed to make changes to any slots or slot options?
		/// </summary>
		internal static bool AllowChanges { get; private set; }

		private static bool subscribed;

		private static int localPlayerCount;

		internal const int MAX_USERS = 8;

		private GameObject localUserPrefab;

		internal int id { get; private set; }

		//-----------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Handle adding and removing users to slots when pressing start or back
		/// </summary>
		public void Update()
		{
			if (!AllowChanges)
				return;

			if (id != 0)
				return;

			var playerList = ReInput.players.Players;
			
			for (int e = 0; e < localPlayerCount; e++)
			{
				var currentPlayer = playerList[e];

				var slot = FindSlotByPlayer(currentPlayer);

				// Pull any controller not yet assigned into a new slot when pressing start

				if (currentPlayer.name == "PlayerMain")
				{
					if (slot == null &&
						currentPlayer.GetButtonDown(11) &&
						LocalUserSlot.Instances.Count < RoR2Application.maxLocalPlayers
						)
					{
						foreach (var panel in Instances)
						{
							int panelChildCount = panel.transform.childCount;

							if (panelChildCount <= MAX_USERS)
							{
								var freeSlot = panel.GetFreeSlot();
								var freePlayer = panel.GetFreePlayer();

								if (freeSlot == null || freePlayer == null)
									continue;

								Log.Print(" ------------ Button Press (start) (LocalUserPanel) ------------");
								Log.Print($"LocalUserPanel.Update: '{panel.name}' adding new player '{freePlayer.name}', slot '{freeSlot.name}' (existing slot is null)");

								freePlayer.controllers.AddController(currentPlayer.controllers.GetLastActiveController(), true);

								freeSlot.LocalPlayer = freePlayer;

								if(panelChildCount != MAX_USERS)
									panel.AddSlot();

								// Set current selected object to null on add player

								EventSystem.current.SetSelectedGameObject(null);

								break;
							}
						}
					}
				}
				else
				{
					// Remove a player that held cancel

					if (slot != null && 
						currentPlayer.GetButton(15))
					{
						currentPlayer.SetVibration(0, currentPlayer.GetVibration(0) + (Time.deltaTime * 10f), true);

						if (currentPlayer.GetButtonTimedPressDown(15, 0.5f))
						{
							Log.Print(" ------------ Button Press (b) (LocalUserPanel) ------------");
							Log.Print($"LocalUserPanel.Update: Removing '{slot.LocalPlayer.name}'");

							var controllers = slot.LocalPlayer.controllers.Controllers;

							var main = LocalUserManager.GetRewiredMainPlayer();

							foreach (var controller in controllers)
								main.controllers.AddController(controller, true);

							slot.LocalPlayer = null;

							currentPlayer.SetVibration(0, 0, true);
						}
					}
					
					/*// Switch displays

					if(slot != null &&
						slot.LocalPlayer != null)
					{
						int displayDirection = 0;

						if (slot.input.LB)
							displayDirection = -1;
						else if (slot.input.RB)
							displayDirection = 1;

						if (displayDirection != 0)
						{
							bool foundDisplay = false;
							var panel = GetComponentInParent<LocalUserPanel>();

							int panelIndex = LocalUserPanel.Instances.IndexOf(panel);

							if (panelIndex == -1)
								return;

							while (!foundDisplay)
							{
								panelIndex += displayDirection;

								var panelCount = LocalUserPanel.Instances.Count;

								if (panelIndex < 0)
									panelIndex = panelCount - 1;
								else if (panelIndex == panelCount)
									panelIndex = 0;

								foundDisplay = LocalUserPanel.Instances[panelIndex].TryAddSlot(this);

								if (foundDisplay)
								{
									if (panel.transform.childCount != LocalUserPanel.MAX_USERS)
										panel.AddSlot();
								}
							}
						}
					}*/
				}
			}
		}
		//-----------------------------------------------------------------------------------------------------------

		public void OnDestroy()
		{
			Instances.Remove(this);
		}

		//-----------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Attempt to transfer a slot to another display
		/// </summary>
		/// <param name="slot"></param>
		/// <returns></returns>
		internal bool TryAddSlot(LocalUserSlot slot)
		{
			var freeSlot = GetFreeSlot();

			if (freeSlot == null)
				return false;

			slot.transform.SetParent(transform);

			if (transform.childCount == MAX_USERS)
				Destroy(freeSlot.gameObject);
			else
				freeSlot.transform.SetSiblingIndex(slot.transform.GetSiblingIndex());

			return true;
		}

		//-----------------------------------------------------------------------------------------------------------

		internal void Initialize(int id)
		{
			try
			{
				Log.Print($"[{this.GetType().Name}.{MethodBase.GetCurrentMethod().Name}] : id -> '{id}'");

				// Vars

				this.id = id;
				localPlayerCount = ReInput.players.playerCount;
				
				Instances ??= new();
				Instances.Add(this);

				// Juice

				var juice = gameObject.AddComponent<UIJuice>();

				juice.canvasGroup = gameObject.GetComponent<CanvasGroup>();
				juice.panningRect = gameObject.GetComponent<RectTransform>();
				juice.panningMagnitude = 30;
				juice.transitionDuration = 0.5f;
				juice.transitionStartPosition = new Vector2(-30, 0);
				juice.originalAlpha = 1f;

				SplitscreenMenuController.Singleton.onEnter.AddListener(() =>
				{
					juice.TransitionAlphaFadeIn();
					juice.TransitionPanFromLeft();
				});

				localUserPrefab ??= Plugin.Resources.LoadAsset<GameObject>("Local User.prefab");

				if (LocalUserSlot.Instances == null || LocalUserSlot.Instances.Count < RoR2Application.maxLocalPlayers)
					AddSlot();

				SplitscreenMenuController.Singleton.onEnter.AddListener(OnEnter);
				SplitscreenMenuController.Singleton.onExit.AddListener(OnExit);
			}
			catch(Exception e)
			{
				Log.Print(e, Log.ELogChannel.Fatal);

				return;
			}
		}

		//-----------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Add a new slot tracker
		/// </summary>
		/// <returns></returns>
		internal LocalUserSlot AddSlot()
		{
			//Log.Print($"[{this.GetType().Name}.{MethodBase.GetCurrentMethod().Name}]");

			var newSlot = GameObject.Instantiate(localUserPrefab);

			newSlot.transform.SetParent(transform);

			var slot = newSlot.gameObject.AddComponent<LocalUserSlot>();

			slot.Initialize();

			newSlot.gameObject.SetActive(true);

			return slot;
		}

		//-----------------------------------------------------------------------------------------------------------

		internal LocalUserSlot GetFreeSlot()
		{
			foreach (var instance in LocalUserSlot.Instances)
			{
				if (instance.transform.parent != transform)
					continue;

				if (instance.LocalPlayer == null)
					return instance;
			}

			return null;
		}

		//-----------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Get the first input player without an assigned controller
		/// </summary>
		private Player GetFreePlayer()
		{
			var players = ReInput.players.Players;
			int playerCount = players.Count;

			for(int e = 2; e < playerCount; e++)
			{
				if (players[e].controllers.joystickCount == 0)
					return players[e];
			}

			return null;
		}

		//-----------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Find the local slot for the provided player
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		private LocalUserSlot FindSlotByPlayer(Player player)
		{
			foreach (var instance in LocalUserSlot.Instances)
			{
				if (instance.LocalPlayer == null || instance.LocalPlayer.controllers.joystickCount == 0)
					continue;

				if (instance.LocalPlayer.name == player.name)
					return instance;
			}

			return null;
		}

		//-----------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Temporarily assign all controllers to individual input users
		/// </summary>
		private void OnEnter()
		{
			if (!subscribed)
			{
				ReInput.ControllerConnectedEvent += OnControllerAddedEvent;
				subscribed = true;
			}

			// Load device icon resources for LocalUserSlot

			if (LocalUserSlot.DeviceIcons == null)
			{
				var availableIcons = Plugin.Resources.GetAllAssetNames()
					.Where(x => x.ToLower().Contains("device_")).ToList();

				LocalUserSlot.DeviceIcons = new Dictionary<string, Sprite>();

				foreach (var icon in availableIcons)
					LocalUserSlot.DeviceIcons.Add(icon.Split("/")
						.Reverse().First().Replace("device_", "").Replace(".png", ""), Plugin.Resources.LoadAsset<Sprite>(icon));
			}

			AllowChanges = true;
		}

		//-----------------------------------------------------------------------------------------------------------

		/// <summary>
		/// DEBUG: Resets controller changes
		/// </summary>
		private void OnExit()
		{
			if(subscribed)
			{
				ReInput.ControllerConnectedEvent -= OnControllerAddedEvent;
				subscribed = false;
			}

			AllowChanges = false;
		}

		//-----------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Spread controllers out among input users
		/// </summary>
		/// <param name="args"></param>
		public void OnControllerAddedEvent(ControllerStatusChangedEventArgs args)
		{
			foreach(var slot in LocalUserSlot.Instances)
			{
				if (slot.LocalPlayer != null &&
					slot.LocalPlayer.controllers.Controllers.Count() == 0)
				{
					slot.LocalPlayer.controllers.AddController(args.controller, false);

					return;
				}
			}

			var players = ReInput.players.Players;
			int playerCount = players.Count;
			int playerId = 2;

			for (int e = playerId; e < playerCount; e++)
			{
				if (players[e].controllers.joystickCount == 0)
				{
					players[e].controllers.AddController(args.controller, false);

					return;
				}
			}
		}
	}
}
