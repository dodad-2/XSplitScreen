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

namespace Dodad.XSplitscreen.Components
{
	internal class LocalUserPanel : MonoBehaviour
	{
		internal static List<LocalUserPanel> Instances { get; private set; }

		/// <summary>
		/// Should players be allowed to make changes to any slots or slot options?
		/// </summary>
		internal static bool AllowChanges { get; private set; }

		private static bool subscribed;

		internal const int MAX_USERS = 8;

		public SplitscreenMenuController Controller => _controller;
		internal LocalUserSlot[] UserSlots => userContainer.GetComponentsInChildren<LocalUserSlot>();

		internal int FilledSlots => userContainer.childCount;

		private GameObject userPrefab;
		private Transform userContainer;

		internal int MonitorId { get; private set; }

		private SplitscreenMenuController _controller;

		//-----------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Handle adding and removing users to slots when pressing start or back
		/// </summary>
		public void Update()
		{
			if (!AllowChanges || MonitorId != 0)
				return;

			var playerList = ReInput.players.Players;

			for (int e = 0; e < playerList.Count; e++)
			{
				var currentPlayer = playerList[e];
				var slot = FindSlotByPlayer(currentPlayer);

				var controller = currentPlayer.controllers.GetLastActiveController();

				if (LocalUserSlot.GetDeviceKeyFromController(controller) == "keyboard")
					continue;

				if (currentPlayer.name == "PlayerMain")
				{
					if (slot != null)
						continue;

					if (currentPlayer.GetButtonDown(11))
						TryAddPlayerToSlot(currentPlayer);
				}
				/*else
				{
					if (currentPlayer.GetButton(15))
					{
						currentPlayer.SetVibration(0, currentPlayer.GetVibration(0) + (Time.deltaTime * 10f), true);
						if(currentPlayer.GetButtonTimedPressDown(15, 0.5f))
							TryRemovePlayerFromSlot(currentPlayer, slot);
					}
				}*/
			}
		}

		internal void TryAddPlayerToSlot(Player currentPlayer)
		{
			TryAddControllersToSlot(new Controller[1] { currentPlayer.controllers.GetLastActiveController() });
		}

		internal void TryAddControllersToSlot(Controller[] controllers)
		{
			if (LocalUserSlot.Instances.Count >= RoR2Application.maxLocalPlayers)
				return;

			foreach (var panel in Instances)
			{
				if (panel.FilledSlots >= MAX_USERS)
					continue;

				var freeSlot = panel.GetFreeSlot();
				var freePlayer = panel.GetFreePlayer();

				if (freeSlot == null || freePlayer == null)
					continue;

				Log.Print($"LocalUserPanel.TryAddControllersToSlot: '{panel.name}' adding new player '{freePlayer.name}', slot '{freeSlot.name}' (existing slot is null)");

				foreach(var controller in controllers)
					freePlayer.controllers.AddController(controller, !(controller is Keyboard || controller is Mouse));

				freeSlot.LocalPlayer = freePlayer;

				if (panel.FilledSlots != MAX_USERS)
					panel.AddSlot();

				EventSystem.current.SetSelectedGameObject(null);
				break;
			}
		}

		internal void TryRemovePlayerFromSlot(Player currentPlayer, LocalUserSlot slot)
		{
			if (slot == null)
				return;

			Log.Print($"LocalUserPanel.TryRemovePlayerFromSlot: Removing '{slot.LocalPlayer.name}'");

			var controllers = slot.LocalPlayer.controllers.Controllers;
			var main = LocalUserManager.GetRewiredMainPlayer();

			foreach (var controller in controllers)
				main.controllers.AddController(controller, true);

			slot.LocalPlayer = null;
			currentPlayer.SetVibration(0, 0, true);
		}
		//-----------------------------------------------------------------------------------------------------------

		public void OnDestroy()
		{
			Instances.Remove(this);
		}

		//-----------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Attempt to transfer a slot to this panel
		/// </summary>
		/// <param name="slot"></param>
		/// <returns></returns>
		internal bool TryAddSlot(LocalUserSlot slot)
		{
			var freeSlot = GetFreeSlot();

			if (freeSlot == null)
				return false;

			slot.transform.SetParent(userContainer);
			slot.transform.localScale = Vector3.one;

			if (transform.childCount == MAX_USERS)
				Destroy(freeSlot.gameObject);
			else
				freeSlot.transform.SetAsLastSibling();

			return true;
		}

		//-----------------------------------------------------------------------------------------------------------

		internal void Initialize(SplitscreenMenuController controller, int id)
		{
			try
			{
				Log.Print($"[{this.GetType().Name}.{MethodBase.GetCurrentMethod().Name}] : id -> '{id}'");

				// Vars

				this.MonitorId = id;
				
				Instances ??= new();
				Instances.Add(this);

				_controller = controller;

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

				userContainer = transform.Find("Slots");

				userPrefab ??= Plugin.Resources.LoadAsset<GameObject>("UserSlot.prefab");

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
		internal void AddSlot()
		{
			//Log.Print($"[{this.GetType().Name}.{MethodBase.GetCurrentMethod().Name}]");

			var newSlot = GameObject.Instantiate(userPrefab, userContainer);

			newSlot.gameObject.AddComponent<LocalUserSlot>();

			newSlot.gameObject.SetActive(true);
		}

		//-----------------------------------------------------------------------------------------------------------

		internal LocalUserSlot GetFreeSlot()
		{
			foreach (var instance in LocalUserSlot.Instances)
			{
				if (instance.transform.parent != userContainer)
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

			for(int e = 1; e < playerCount; e++)
			{
				if (players[e].controllers.joystickCount == 0 && !players[e].controllers.hasKeyboard)
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
				if (instance.LocalPlayer == null || (instance.LocalPlayer.controllers.joystickCount == 0 && !instance.LocalPlayer.controllers.hasKeyboard))
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
