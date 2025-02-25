using Rewired;
using RoR2;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static RoR2.MasterSpawnSlotController;

namespace dodad.XSplitscreen.Components
{
	public class LocalUserSlot : MonoBehaviour
	{
		internal static List<LocalUserSlot> Instances { get; private set; }

		/// <summary>
		/// This Dictionary is created and cleared by LocalUserPanel
		/// </summary>
		internal static Dictionary<string, Sprite> DeviceIcons;

		private const float MIN_DEVICE_ALPHA = 0.4f;

		//-----------------------------------------------------------------------------------------------------------

		public Player LocalPlayer
		{
			get
			{
				return localPlayer;
			}
			internal set
			{
				//Log.Print($"LocalUserSlot: '{name}' set local player to '{(value == null ? "none" : value.name)}'");

				// First find a free input player
				
				EnsureEventSystemProviderExists();

				SetLocalPlayerListenerState(false);

				localPlayer = value;

				SetLocalPlayerListenerState(true);

				provider.eventSystem = value == null ? null : MPEventSystem.FindByPlayer(value);

				name = $"[{(value == null ? "Open" : value.name)}] Local User Slot {Instances.IndexOf(this)}";

				//Log.Print($"LocalUserSlot.Set_LocalPlayer: '{name}'");

				OnPlayerChanged?.Invoke(this);
			}
		}

		/// <summary>
		/// TODO: Profile name should be stored in the xUserConfigs - not here
		/// </summary>
		public UserProfile Profile
		{
			get
			{
				return profile;
			}
			set
			{
				profile = value;
			}
		}

		//-----------------------------------------------------------------------------------------------------------

		public static Action<LocalUserSlot> OnPlayerChanged;

		internal InputBank input = new InputBank();

		private UIJuice juice;
		private MPEventSystemProvider provider;
		private Player localPlayer;
		private UserProfile profile;

		// Children

		private Image deviceIcon;
		private GameObject message;
		private SlotOptions options;

		private LanguageTextMeshController messageController;

		// XUser config vars

		internal Dictionary<string, string> xUserConfigs = new Dictionary<string, string>();

		//-----------------------------------------------------------------------------------------------------------

		private void EnsureEventSystemProviderExists()
		{
			provider ??= gameObject.AddComponent<MPEventSystemProvider>();
			provider.fallBackToMainEventSystem = false;
		}

		//-----------------------------------------------------------------------------------------------------------

		public void Awake()
		{
			//Log.Print($"LocalUserSlot.Awake: '{name}'");

			// Set up references

			deviceIcon = transform.Find("Device Icon").GetComponent<Image>();

			message = transform.Find("Message").gameObject;

			EnsureEventSystemProviderExists();

			options = transform.Find("Options").gameObject.AddComponent<SlotOptions>();

			name = $"[Open] Local User Slot {Instances.IndexOf(this)}";

			// UI

			var messageText = UIHelper.GetPrefab(UIHelper.EUIPrefabIndex.SimpleText);

			var messageLayoutElement = messageText.AddComponent<LayoutElement>();
			messageLayoutElement.flexibleWidth = 1f;

			messageText.transform.SetParent(message.transform, false);

			messageController = messageText.GetComponentInChildren<LanguageTextMeshController>();
			
			var messageHgText = messageText.GetComponent<HGTextMeshProUGUI>();
			messageHgText.maxVisibleLines = 1;
			messageHgText.overflowMode = TextOverflowModes.Truncate;

			SetMessage("XSS_PRESS_START");

			messageText.gameObject.SetActive(true);

			// Juice

			var canvas = gameObject.AddComponent<CanvasGroup>();
			juice = gameObject.AddComponent<UIJuice>();

			juice.canvasGroup = canvas;
			juice.transitionDuration = 0.5f;
			juice.originalAlpha = 1f;

			ResolveSlotState(this);
		}

		//-----------------------------------------------------------------------------------------------------------

		public void Update()
		{
			if (!LocalUserPanel.AllowChanges)
				return;

			input.Update(LocalPlayer);

			// Device icon alpha

			var currentColor = deviceIcon.color;

			float alphaDirection = Time.deltaTime * 10f * (LocalPlayer != null && input.Any ? 1 : -1);

			deviceIcon.color = new Color(currentColor.r, currentColor.g, currentColor.b, Mathf.Clamp(currentColor.a + alphaDirection, MIN_DEVICE_ALPHA, 1f));

			// TODO this should be done from the assignment controller when editing the assignment display

			// Move slot to requested display on button press

			int displayDirection = 0;

			if (input.LB)
				displayDirection = -1;
			else if (input.RB)
				displayDirection = 1;

			if(displayDirection != 0)
			{
				bool foundDisplay = false;
				var panel = GetComponentInParent<LocalUserPanel>();

				int panelIndex = LocalUserPanel.Instances.IndexOf(panel);

				if (panelIndex == -1)
					return;

				while(!foundDisplay)
				{
					panelIndex += displayDirection;

					var panelCount = LocalUserPanel.Instances.Count;

					if (panelIndex < 0)
						panelIndex = panelCount - 1;
					else if (panelIndex == panelCount)
						panelIndex = 0;

					foundDisplay = LocalUserPanel.Instances[panelIndex].TryAddSlot(this);

					if(foundDisplay)
					{
						if (panel.transform.childCount != LocalUserPanel.MAX_USERS)
						{
							var freeSlot = panel.GetFreeSlot();

							if(freeSlot == null)
								panel.AddSlot();
						}
					}
				}
			}
		}

		//-----------------------------------------------------------------------------------------------------------

		public void OnDestroy()
		{
			Instances.Remove(this);

			if (Instances.Count == 0)
				OnPlayerChanged -= ResolveSlotState;

			SetLocalPlayerListenerState(false);
		}

		//-----------------------------------------------------------------------------------------------------------

		public void Initialize()
		{
			Instances ??= new List<LocalUserSlot>();

			Instances.Add(this);

			if (Instances.Count == 1)
				OnPlayerChanged += ResolveSlotState;
		}

		//-----------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Enable, disable or delete the slot 
		/// </summary>
		/// <param name="slot"></param>
		private static void ResolveSlotState(LocalUserSlot slot)
		{
			//Log.Print($"LocalUserSlot.ResolveSlotState: Evaluating slot '{slot.name}'");

			// The slot has a player and should display the first option

			if(slot.LocalPlayer != null)
			{
				//Log.Print($"LocalUserSlot.ResolveSlotState: Player '{slot.LocalPlayer.name}' found, displaying options");

				slot.options.gameObject.SetActive(true);

				slot.options.OpenProfileConfigurator();

				slot.ResolveDeviceIcon();

				slot.deviceIcon.gameObject.SetActive(true);

				slot.juice.TransitionAlphaFadeIn();
			}
			else
			{
				//Log.Print($"LocalUserSlot.ResolveSlotState: No player found - hiding or destroying");

				var siblings = Instances.Where(x => x.transform.parent == slot.transform.parent);
				int instanceCount = siblings.Count();

				// Empty slot already exists or too many players

				if (instanceCount > 1 && Instances.Count < RoR2Application.maxLocalPlayers && // Has more than 1 slot and not at max player count
					siblings.Where(x => 
					x.localPlayer == null && // Is a duplicate empty slot
					x != slot)
					.Any())
				{
					//Log.Print("LocalUserSlot.ResolveSlotState: --> destroy");

					Destroy(slot.gameObject);
				}
				else
				{
					//Log.Print("LocalUserSlot.ResolveSlotState: --> hide");

					slot.options.gameObject.SetActive(false);

					slot.deviceIcon.gameObject.SetActive(false);

					slot.SetMessage("XSS_PRESS_START");

					slot.transform.SetSiblingIndex(slot.transform.parent.childCount);

					slot.juice.TransitionAlphaFadeIn();
				}
			}
		}

		//-----------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Subscribe and unsubscribe from various events
		/// </summary>
		/// <param name="state"></param>
		private void SetLocalPlayerListenerState(bool state)
		{
			if (localPlayer != null)
			{
				if(state)
				{
					localPlayer.controllers.ControllerAddedEvent += OnControllerAdded;
					localPlayer.controllers.ControllerRemovedEvent += OnControllerRemoved;
				}
				else
				{
					localPlayer.controllers.ControllerAddedEvent -= OnControllerAdded;
					localPlayer.controllers.ControllerRemovedEvent -= OnControllerRemoved;
				}
			}
		}

		//-----------------------------------------------------------------------------------------------------------

		private void ResolveDeviceIcon()
		{
			var deviceKey = "x";
			
			if (LocalPlayer != null)
			{
				if (localPlayer.controllers.Controllers.Count() != 0)
				{ 
					var controller = localPlayer.controllers.Controllers.First();

					var controllerType = controller.inputSource.ToString().ToLower();

					if (controllerType == "ps4" || controllerType == "ps5")
						deviceKey = "ps";
					else if (controllerType == "unitykeyboardandmouse")
						deviceKey = "keyboard";
					else
						deviceKey = "xbox";
				}
			}

			//Log.Print($"LocalUserSlot.ResolveDeviceIcon: {name} resolved device icon to '{deviceKey}'");

			if (deviceIcon.sprite.name == deviceKey)
				return;

			Sprite sprite = null;

			DeviceIcons.TryGetValue(deviceKey, out sprite);

			deviceIcon.sprite = Instantiate(sprite);
		}

		//-----------------------------------------------------------------------------------------------------------

		public void OnControllerAdded(ControllerAssignmentChangedEventArgs args)
		{
			ResolveDeviceIcon();
		}

		//-----------------------------------------------------------------------------------------------------------

		public void OnControllerRemoved(ControllerAssignmentChangedEventArgs args)
		{
			ResolveDeviceIcon();
		}

		//-----------------------------------------------------------------------------------------------------------

		public void SetMessage(string token)
		{
			//Log.Print($"LocalUserSlot.SetMessage: Message = '{(token == null ? "null" : token)}'");

			if(token == null)
				message.gameObject.SetActive(false);
			else
			{
				messageController.token = token;
				message.gameObject.SetActive(true);
			}
		}

		//-----------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Provide input from an available input player
		/// </summary>
		public class InputBank
		{
			public bool Any
			{
				get
				{
					var val = any;

					//any = false;

					return val;
				}
				private set
				{
					any = value;
				}
			}

			public bool Left
			{
				get
				{
					var val = left;

					//left = false;

					return val;
				}
				private set
				{
					left = value;
				}
			}
			public bool Right
			{
				get
				{
					var val = right;

					//right = false;

					return val;
				}
				private set
				{
					right = value;
				}
			}
			public bool South
			{
				get
				{
					var val = south;

					//south = false;

					return val;
				}
				private set
				{
					south = value;
				}
			}
			public bool East
			{
				get
				{
					var val = east;

					//east = false;

					return val;
				}
				private set
				{
					east = value;
				}
			}
			public bool Up
			{
				get
				{
					var val = up;

					return val;
				}
				private set
				{
					up = value;
				}
			}
			public bool Down
			{
				get
				{
					var val = down;

					return val;
				}
				private set
				{
					down = value;
				}
			}
			public bool LB
			{
				get
				{
					var val = lb;

					return val;
				}
				private set
				{
					lb = value;
				}
			}
			public bool RB
			{
				get
				{
					var val = rb;

					return val;
				}
				private set
				{
					rb = value;
				}
			}

			//-----------------------------------------------------------------------------------------------------------

			private bool any, left, right, south, east, up, down, lb, rb;

			private float pressDelay = 0.3f;
			private float leftTimer, rightTimer, southTimer, eastTimer, upTimer, downTimer, lbTimer, rbTimer;

			//-----------------------------------------------------------------------------------------------------------

			public void Update(Player player)
			{
				any = left = right = south = east = up = down = lb = rb = false;

				leftTimer -= Time.deltaTime;
				rightTimer -= Time.deltaTime;
				southTimer -= Time.deltaTime;
				eastTimer -= Time.deltaTime;
				upTimer -= Time.deltaTime;
				downTimer -= Time.deltaTime;
				lbTimer -= Time.deltaTime;
				rbTimer -= Time.deltaTime;

				if (player == null)
					return;

				float lrValue = player.GetAxis(0);
				float udValue = player.GetAxis(1);
				bool southValue = player.GetButtonDown(4);
				bool eastValue = player.GetButtonDown(15);
				bool lbValue = player.GetButtonDown(9);
				bool rbValue = player.GetButtonDown(10);

				// Left stick

				if (leftTimer <= 0 && lrValue < -0.3f)
				{
					Left = true;

					leftTimer = pressDelay;
				}

				// Right stick

				if (rightTimer <= 0 && lrValue > 0.3f)
				{
					Right = true;

					rightTimer = pressDelay;
				}

				// Up stick

				if (upTimer <= 0 && udValue > 0.3f)
				{
					Up = true;

					upTimer = pressDelay;
				}

				// Down stick

				if (downTimer <= 0 && udValue < -0.3f)
				{
					Down = true;

					downTimer = pressDelay;
				}

				// South button

				if (southTimer <= 0 && southValue)
				{
					South = true;

					rightTimer = pressDelay;
				}

				// East button

				if (eastTimer <= 0 && eastValue)
				{
					East = true;

					eastTimer = pressDelay;
				}

				// Left bumper

				if (lbTimer <= 0 && lbValue)
				{
					LB = true;

					lbTimer = pressDelay;
				}

				// Right bumper

				if (rbTimer <= 0 && rbValue)
				{
					RB = true;

					rbTimer = pressDelay;
				}

				any = player.GetAnyButton() || new Vector2(lrValue, udValue).sqrMagnitude > 0.1f;
			}
		}
	}
}
