using Rewired;
using RoR2;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dodad.XSplitscreen.Components
{
	/// <summary>
	/// Manages a local user slot for multiplayer input and user assignment.
	/// Handles controller/keyboard assignment, UI updates, and slot management.
	/// </summary>
	public class LocalUserSlot : MonoBehaviour
	{
		#region Static Properties and Fields

		/// <summary>
		/// Collection of all active user slots.
		/// </summary>
		internal static List<LocalUserSlot> Instances { get; private set; }

		/// <summary>
		/// Dictionary mapping device types to UI icons.
		/// </summary>
		internal static Dictionary<string, Sprite> DeviceIcons;

		/// <summary>
		/// Minimum alpha value for device icons when not active.
		/// </summary>
		private const float MIN_DEVICE_ALPHA = 0.4f;

		/// <summary>
		/// Event triggered when a player is assigned or removed from a slot.
		/// </summary>
		public static Action<LocalUserSlot> OnPlayerChanged;

		public static Action OnRemovedKeyboardUser;
		#endregion

		#region Public Properties

		public UnityEngine.Rect ScreenRect => _options.GetConfigurator<AssignmentConfigurator>()?.Rect ?? new(0, 0, 1, 1);

		/// <summary>
		/// The Rewired Player assigned to this slot. Setting this updates associated systems.
		/// </summary>
		public Player LocalPlayer
		{
			get => _localPlayer;
			internal set
			{
				EnsureEventSystemProviderExists();
				SetLocalPlayerListenerState(false);
				_localPlayer = value;
				SetLocalPlayerListenerState(true);
				_provider.eventSystem = value == null ? null : MPEventSystem.FindByPlayer(value);

				if(_provider.eventSystem != null)
					_provider.
				name = $"[{(value == null ? "Open" : value.name)}] Local User Slot {Instances.IndexOf(this)}";
				OnPlayerChanged?.Invoke(this);
			}
		}

		/// <summary>
		/// The user profile associated with this slot.
		/// </summary>
		public UserProfile Profile
		{
			get => _profile;
			set
			{
				if (_profile != null)
				{
					OnUnloadProfile?.Invoke();
				}

				_profile = value;

				OnLoadProfile?.Invoke();
			}
		}

		public Color MainColor = Color.white;

		/// <summary>
		/// Determines if this slot is using a keyboard/mouse as input.
		/// </summary>
		public bool IsKeyboardUser => LocalPlayer?.controllers.hasKeyboard ?? false;

		public Action<int> OnNavigateIndex;
		public Action OnCancel;
		public Action OnConfirm;
		public Action OnUnloadProfile;
		public Action OnLoadProfile;
		
		public int NavigatorCount
		{
			set => _navigationController.SetDotCount(value);
		}

		public int NavigatorIndex
		{
			set => _navigationController.SetDotIndex(value);
		}

		public bool EnableConfirmButton
		{
			get => _confirmButton.interactable;
			set => _confirmButton.interactable = value;
		}

		public bool EnableCancelButton
		{
			get => _cancelButton.interactable;
			set => _cancelButton.interactable = value;
		}

		#endregion

		#region Internal Fields

		/// <summary>
		/// Input bank to process and debounce player input.
		/// </summary>
		internal InputBank Input = new InputBank();
		internal LocalUserPanel Panel => _panel;
		#endregion

		#region Private Fields

		private LocalUserPanel _panel;
		private Player _localPlayer;
		private UserProfile _profile;
		private MPEventSystemProvider _provider;
		private UIJuice _juice;

		// UI Elements
		private Image _deviceIcon;
		private Transform _titleContainer;
		private Transform _configuratorContainer;
		private LanguageTextMeshController _titleController;
		private MPButton _cancelButton;
		private MPButton _confirmButton;
		private Image _cancelImage;
		private Image _confirmImage;
		private SlotOptions _options;
		private NavigatorDotController _navigationController;
		#endregion

		#region Unity Lifecycle

		/// <summary>
		/// Initializes the slot when added to the scene.
		/// </summary>
		public void Awake()
		{
			if (Instances == null)
			{
				Instances = new List<LocalUserSlot>();
				OnPlayerChanged += ResolveSlotState;
			}

			Instances.Add(this);

			_panel = GetComponentInParent<LocalUserPanel>();

			InitializeUIComponents();
			ResolveSlotState(this);
		}

		/// <summary>
		/// Updates input processing and UI elements.
		/// </summary>
		public void Update()
		{
			if (!LocalUserPanel.AllowChanges) return;

			UpdateInput();
			UpdateDeviceIconAlpha();
			HandleDisplaySlotMovement();
		}

		/// <summary>
		/// Cleanup when the slot is destroyed.
		/// </summary>
		public void OnDestroy()
		{
			Instances.Remove(this);
			if (Instances.Count == 0)
			{
				Instances = null;
				OnPlayerChanged -= ResolveSlotState;
			}
			SetLocalPlayerListenerState(false);
		}

		#endregion

		#region Initialization Methods

		/// <summary>
		/// Sets up all UI components for the slot.
		/// </summary>
		private void InitializeUIComponents()
		{
			name = $"[Open] User Slot {Instances.IndexOf(this)}";

			EnsureEventSystemProviderExists();
			SetupTitleUI();
			SetupNavigatorUI();
			SetupOptionsUI();
			SetupJuiceEffects();
		}

		private void SetupOptionsUI()
		{
			_configuratorContainer = transform.Find("ConfiguratorContainer/MainContainer");
			_options = _configuratorContainer.gameObject.AddComponent<SlotOptions>();
		}

		/// <summary>
		/// Ensures an event system provider exists for this slot.
		/// </summary>
		private void EnsureEventSystemProviderExists()
		{
			_provider ??= gameObject.AddComponent<MPEventSystemProvider>();
			_provider.fallBackToMainEventSystem = true;
		}

		/// <summary>
		/// Sets up the title UI components.
		/// </summary>
		private void SetupTitleUI()
		{
			_deviceIcon = transform.Find("DeviceIcon").GetComponent<Image>();
			_deviceIcon.enabled = false;
			_titleContainer = transform.Find("ConfiguratorContainer/TitleContainer");

			var messageText = UIHelper.GetPrefab(UIHelper.EUIPrefabIndex.SimpleText);
			messageText.transform.SetParent(_titleContainer, false);
			var messageRect = messageText.GetComponent<RectTransform>();
			messageRect.anchorMin = Vector2.zero;
			messageRect.anchorMax = Vector2.one;
			_titleController = messageText.GetComponentInChildren<LanguageTextMeshController>();
			var messageHgText = messageText.GetComponent<HGTextMeshProUGUI>();
			messageHgText.maxVisibleLines = 1;
			messageHgText.overflowMode = TextOverflowModes.Overflow;
			messageHgText.alignment = TextAlignmentOptions.Center;
			messageHgText.horizontalAlignment = HorizontalAlignmentOptions.Center;
			messageText.AddComponent<MPButton>();
			SetMessage("XSS_PRESS_START_KBM");
			messageText.gameObject.SetActive(true);
		}

		/// <summary>
		/// Sets up the navigation button UI components.
		/// </summary>
		private void SetupNavigatorUI()
		{
			_navigationController = transform.Find("ConfiguratorContainer/Navigation").gameObject.AddComponent<NavigatorDotController>();
			_navigationController.OnNavigate += (x) => OnNavigateIndex?.Invoke(x);

			NavigatorCount = 0;

			_cancelButton = transform.Find("ConfiguratorContainer/CancelButton").gameObject.AddComponent<MPButton>();
			_confirmButton = transform.Find("ConfiguratorContainer/ConfirmButton").gameObject.AddComponent<MPButton>();

			_cancelButton.interactable = false;
			_cancelButton.allowAllEventSystems = true;
			_confirmButton.interactable = false;
			_confirmButton.allowAllEventSystems = true;

			_cancelImage = _cancelButton.GetComponent<Image>();
			_confirmImage = _confirmButton.GetComponent<Image>();

			_cancelImage.enabled = false;
			_confirmImage.enabled = false;

			_cancelButton.onClick.AddListener(() => TryRemoveSlot());
			_confirmButton.onClick.AddListener(() => OnConfirm?.Invoke());
		}

		/// <summary>
		/// Sets up transition effects for the UI.
		/// </summary>
		private void SetupJuiceEffects()
		{
			var canvas = gameObject.AddComponent<CanvasGroup>();
			_juice = gameObject.AddComponent<UIJuice>();
			_juice.canvasGroup = canvas;
			_juice.transitionDuration = 0.5f;
			_juice.originalAlpha = 1f;
		}

		#endregion

		#region Update Methods

		/// <summary>
		/// Updates input state from the assigned player.
		/// </summary>
		private void UpdateInput()
		{
			Input.Update(LocalPlayer);
		}

		/// <summary>
		/// Updates the alpha of the device icon based on input activity.
		/// </summary>
		private void UpdateDeviceIconAlpha()
		{
			var currentColor = new Color(MainColor.r, MainColor.g, MainColor.b, _deviceIcon.color.a);
			float alphaDirection = Time.deltaTime * 10f * (LocalPlayer != null && (Input.MouseActive || Input.Any) ? 1 : -1);
			var newAlpha = Mathf.Clamp(currentColor.a + alphaDirection, MIN_DEVICE_ALPHA, 1f);
			_deviceIcon.color = new Color(currentColor.r, currentColor.g, currentColor.b, newAlpha);
		}

		/// <summary>
		/// Handles movement of this slot between display panels.
		/// </summary>
		private void HandleDisplaySlotMovement()
		{
			if (IsKeyboardUser)
				return;

			int displayDirection = GetDisplayDirection();
			if (displayDirection == 0) return;

			MoveSlotByDirection(displayDirection);
		}

		/// <summary>
		/// Gets the direction for display movement based on shoulder button input.
		/// </summary>
		private int GetDisplayDirection()
		{
			if (Input.LB) return -1;
			if (Input.RB) return 1;
			return 0;
		}

		/// <summary>
		/// Moves the slot to another display panel.
		/// </summary>
		internal void MoveSlotByDirection(int displayDirection)
		{
			var currentPanel = _panel;
			int panelIndex = LocalUserPanel.Instances.IndexOf(_panel);
			if (panelIndex == -1) return;

			bool foundDisplay = false;
			int panelCount = LocalUserPanel.Instances.Count;

			while (!foundDisplay)
			{
				// Calculate next panel index with wraparound
				panelIndex = (panelIndex + displayDirection + panelCount) % panelCount;

				var targetPanel = LocalUserPanel.Instances[panelIndex];
				foundDisplay = targetPanel.TryAddSlot(this);

				// If found and the panel isn't full, add a free slot if possible
				if (foundDisplay)
				{
					_panel = targetPanel;
					_options.ForceClose();
					TryAddFreeSlotToPanel(currentPanel);
				}
			}
		}

		/// <summary>
		/// Attempts to add a free slot to the panel if needed.
		/// </summary>
		private void TryAddFreeSlotToPanel(LocalUserPanel panel)
		{
			if (panel.transform.childCount != LocalUserPanel.MAX_USERS)
			{
				var freeSlot = panel.GetFreeSlot();
				if (freeSlot == null)
				{
					panel.AddSlot();
				}
			}
		}

		#endregion

		#region Slot State Management

		/// <summary>
		/// Try to remove this player or invoke OnCancel to notify the options
		/// </summary>
		public void TryRemoveSlot()
		{
			if (!LocalUserPanel.AllowChanges)
				return;

			if(!_options.IsEditing)
			{
				bool isKeyboard = IsKeyboardUser;

				_panel.TryRemovePlayerFromSlot(_localPlayer, this);

				if (isKeyboard)
				{
					OnRemovedKeyboardUser?.Invoke();
				}
			}
			else
			{
				OnCancel?.Invoke();
			}
		}

		/// <summary>
		/// Enable, disable or delete the slot based on state.
		/// </summary>
		private static void ResolveSlotState(LocalUserSlot slot)
		{
			if (slot.LocalPlayer != null)
			{
				slot.ActivateOccupiedSlot();
			}
			else
			{
				slot.HandleEmptySlot();
			}
		}

		/// <summary>
		/// Configure the slot for an assigned player.
		/// </summary>
		private void ActivateOccupiedSlot()
		{
			_configuratorContainer.gameObject.SetActive(true);
			ResolveDeviceIcon();
			SetSlotUIState(true);
			_options.OpenProfileConfigurator();
			_juice.TransitionAlphaFadeIn();
		}

		/// <summary>
		/// Handle the slot when it's empty (available for a new player).
		/// </summary>
		private void HandleEmptySlot()
		{
			var siblings = Instances.Where(x => x.transform.parent == transform.parent);
			int instanceCount = siblings.Count();

			bool hasOtherEmptySlot = siblings.Any(x => !(x == null) && x._localPlayer == null && x != this);
			bool canRemove = instanceCount > 1 && Instances.Count < RoR2Application.maxLocalPlayers;

			if (canRemove && hasOtherEmptySlot)
			{
				gameObject.SetActive(false);
				gameObject.transform.SetParent(null);
				Destroy(gameObject);
			}
			else
			{
				SetSlotUIState(false);

				bool hasKeyboardUser = Instances.Any(x => x.IsKeyboardUser);

				_titleController.GetComponent<MPButton>().onClick.RemoveAllListeners();

				if (_panel.MonitorId != 0 || hasKeyboardUser)
				{
					SetMessage("XSS_PRESS_START");
				}
				else
				{
					_titleController.GetComponent<MPButton>().onClick.AddListener(() =>
					{
						var controllers = new Controller[2];
						controllers[0] = ReInput.controllers.Keyboard as Controller;
						controllers[1] = ReInput.controllers.Mouse as Controller;

						GetComponentInParent<LocalUserPanel>().TryAddControllersToSlot(controllers);
					});

					SetMessage("XSS_PRESS_START_KBM");
				}

				transform.SetAsLastSibling();
				_juice.TransitionAlphaFadeIn();
			}
		}

		/// <summary>
		/// Set the UI state based on whether the slot is filled or empty.
		/// </summary>
		private void SetSlotUIState(bool isOccupied)
		{
			_configuratorContainer.gameObject.SetActive(isOccupied);
			_deviceIcon.enabled = isOccupied;
			//_rightArrowImage.enabled = isOccupied;
			//_leftArrowImage.enabled = isOccupied;
			_titleContainer.gameObject.SetActive(!isOccupied);

			bool isKeyboard = IsKeyboardUser;

			_cancelButton.gameObject.SetActive(isKeyboard && isOccupied);
			_confirmButton.gameObject.SetActive(isKeyboard && isOccupied);
			_cancelImage.enabled = isOccupied;
			_confirmImage.enabled = isOccupied;

			if(!isOccupied)
			{
				OnRemovedKeyboardUser += RefreshIfEmpty;
			}
			else
			{
				OnRemovedKeyboardUser -= RefreshIfEmpty;
			}

		}

		private void RefreshIfEmpty()
		{
			OnRemovedKeyboardUser -= RefreshIfEmpty;

			HandleEmptySlot();
		}

		#endregion

		#region Player Management

		/// <summary>
		/// Subscribe or unsubscribe from player events.
		/// </summary>
		private void SetLocalPlayerListenerState(bool subscribe)
		{
			if (_localPlayer != null)
			{
				if (subscribe)
				{
					_localPlayer.controllers.ControllerAddedEvent += OnControllerAdded;
					_localPlayer.controllers.ControllerRemovedEvent += OnControllerRemoved;
				}
				else
				{
					_localPlayer.controllers.ControllerAddedEvent -= OnControllerAdded;
					_localPlayer.controllers.ControllerRemovedEvent -= OnControllerRemoved;
				}
			}
		}

		/// <summary>
		/// Updates the device icon based on the current controller.
		/// </summary>
		private void ResolveDeviceIcon()
		{
			string deviceKey = GetDeviceKeyForCurrentController(_localPlayer);

			if (_deviceIcon.sprite != null && _deviceIcon.sprite.name == deviceKey)
				return;

			if (DeviceIcons.TryGetValue(deviceKey, out Sprite sprite) && sprite != null)
				_deviceIcon.sprite = Instantiate(sprite);
		}

		/// <summary>
		/// Gets the device key for the controller assigned to a player.
		/// </summary>
		public static string GetDeviceKeyForCurrentController(Player localPlayer)
		{
			if (localPlayer == null || !localPlayer.controllers.Controllers.Any())
				return "x";

			return GetDeviceKeyFromController(localPlayer.controllers.Controllers.First());
		}

		/// <summary>
		/// Gets the device key from a controller type.
		/// </summary>
		public static string GetDeviceKeyFromController(Controller controller)
		{
			if (controller == null)
				return "x";

			string controllerType = controller.name.ToString().ToLower();

			if (controllerType.Contains("sony"))
				return "ps";
			else if (controller is Keyboard || controller is Mouse)
				return "keyboard";
			else
				return "xbox";
		}

		/// <summary>
		/// Handler for when a controller is added to the player.
		/// </summary>
		public void OnControllerAdded(ControllerAssignmentChangedEventArgs args)
		{
			ResolveDeviceIcon();
		}

		/// <summary>
		/// Handler for when a controller is removed from the player.
		/// </summary>
		public void OnControllerRemoved(ControllerAssignmentChangedEventArgs args)
		{
			ResolveDeviceIcon();
		}

		#endregion

		#region UI Methods

		/// <summary>
		/// Sets the message text in the title area.
		/// </summary>
		public void SetMessage(string token)
		{
			if (token == null)
				_titleContainer.gameObject.SetActive(false);
			else
			{
				_titleController.token = token;
				_titleContainer.gameObject.SetActive(true);
			}
		}

		#endregion

		#region Input Bank Class

		/// <summary>
		/// Provides input from an available input player, debounced with configurable press delay.
		/// </summary>
		public class InputBank
		{
			public bool Any { get; private set; }
			public bool Left { get; private set; }
			public bool Right { get; private set; }
			public bool South { get; private set; }
			public bool East { get; private set; }
			public bool Up { get; private set; }
			public bool Down { get; private set; }
			public bool LB { get; private set; }
			public bool RB { get; private set; }
			public float LeftRightDelta { get; private set; }
			public float UpDownDelta { get; private set; }
			public bool MouseLeft { get; private set; }
			public bool MouseActive { get; private set; }

			private float pressDelay = 0.3f;
			private float leftTimer, rightTimer, southTimer, eastTimer, upTimer, downTimer, lbTimer, rbTimer;

			/// <summary>
			/// Updates the input bank, debouncing input events.
			/// </summary>
			public void Update(Player player)
			{
				Any = Left = Right = South = East = Up = Down = LB = RB = MouseLeft = MouseActive = false;
				LeftRightDelta = UpDownDelta = 0f;

				leftTimer -= Time.deltaTime;
				rightTimer -= Time.deltaTime;
				southTimer -= Time.deltaTime;
				eastTimer -= Time.deltaTime;
				upTimer -= Time.deltaTime;
				downTimer -= Time.deltaTime;
				lbTimer -= Time.deltaTime;
				rbTimer -= Time.deltaTime;

				if (player == null) return;

				LeftRightDelta = player.GetAxis(0);
				UpDownDelta = player.GetAxis(1);

				bool southValue = player.GetButtonDown(14);
				bool eastValue = player.GetButtonDown(15);
				bool lbValue = player.GetButtonDown(9);
				bool rbValue = player.GetButtonDown(10);
;
				if (player.controllers.hasKeyboard)
				{
					/*LeftRightDelta +=
						(player.controllers.Keyboard.GetKey(KeyCode.LeftArrow) ? -1 : 0)
						+
						(player.controllers.Keyboard.GetKey(KeyCode.RightArrow) ? 1 : 0);*/

					UpDownDelta +=
						(player.controllers.Keyboard.GetKey(KeyCode.DownArrow) ? -1 : 0)
						+
						(player.controllers.Keyboard.GetKey(KeyCode.UpArrow) ? 1 : 0);

					var scrollDelta = player.controllers.Mouse.GetAxis(2);
					UpDownDelta += scrollDelta;

					if (scrollDelta != 0)
						upTimer = downTimer = 0;

					/*southValue |= player.controllers.Keyboard.GetKey(KeyCode.Space) | player.controllers.Keyboard.GetKey(KeyCode.KeypadEnter);
					eastValue |= player.controllers.Keyboard.GetKey(KeyCode.Escape) | player.controllers.Keyboard.GetKey(KeyCode.Backspace);*/
					southValue |= player.controllers.Keyboard.GetKey(KeyCode.RightArrow);
					eastValue |= player.controllers.Keyboard.GetKey(KeyCode.LeftArrow);

					MouseLeft = player.controllers.Mouse.GetButton(0);
					MouseActive = new Vector2(LeftRightDelta, UpDownDelta).sqrMagnitude > 0.1f;

					// Keyboard user on alternate monitor not supported for now
					/*lbValue |= player.controllers.Keyboard.GetKey(KeyCode.Q);
					rbValue |= player.controllers.Keyboard.GetKey(KeyCode.E);*/
				}

				if (leftTimer <= 0 && LeftRightDelta < -0.3f)
				{
					Left = true;
					leftTimer = pressDelay;
				}
				if (rightTimer <= 0 && LeftRightDelta > 0.3f)
				{
					Right = true;
					rightTimer = pressDelay;
				}
				if (upTimer <= 0 && UpDownDelta > 0.3f)
				{
					Up = true;
					upTimer = pressDelay;
				}
				if (downTimer <= 0 && UpDownDelta < -0.3f)
				{
					Down = true;
					downTimer = pressDelay;
				}
				if (southTimer <= 0 && southValue)
				{
					South = true;
					southTimer = pressDelay;
				}
				if (eastTimer <= 0 && eastValue)
				{
					East = true;
					eastTimer = pressDelay;
				}
				if (lbTimer <= 0 && lbValue)
				{
					LB = true;
					lbTimer = pressDelay;
				}
				if (rbTimer <= 0 && rbValue)
				{
					RB = true;
					rbTimer = pressDelay;
				}

				Any = player.GetAnyButton();
			}
		}

		#endregion
	}
}