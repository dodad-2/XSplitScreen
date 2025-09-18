using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static RoR2.MasterSpawnSlotController;

namespace Dodad.XSplitscreen.Components
{
	/// <summary>
	/// Manages configuration options for a user slot.
	/// Handles navigation between different configuration types and their UI representation.
	/// </summary>
	public class SlotOptions : MonoBehaviour
	{
		#region Properties and Fields

		/// <summary>
		/// Reference to the parent LocalUserSlot.
		/// </summary>
		internal LocalUserSlot Slot { get; private set; }

		/// <summary>
		/// Available configuration options.
		/// </summary>
		private List<OptionConfigurator> _configurators = new List<OptionConfigurator>();

		/// <summary>
		/// Index of the currently selected configurator.
		/// </summary>
		private int _configuratorIndex;

		/// <summary>
		/// Whether the user is currently editing an option.
		/// </summary>
		public bool IsEditing { get; private set; }

		/// <summary>
		/// UI navigation elements.
		/// </summary>
		private GameObject _titleText;
		private LanguageTextMeshController _titleController;
		#endregion

		#region Unity Lifecycle

		/// <summary>
		/// Initialize the options UI when the component awakens.
		/// </summary>
		public void Awake()
		{
			Slot = GetComponentInParent<LocalUserSlot>();

			CreateOptions();

			SetupMessageUI();
			SubscribeToNavigation();
			//SetupMouseUI();
			//SetMouseUIActive(false);
		}

		/// <summary>
		/// Clean up when the options are disabled.
		/// </summary>
		public void OnDisable()
		{
			ForceClose();
		}

		/// <summary>
		/// Handle input and updates for the options UI.
		/// </summary>
		public void Update()
		{
			if (!LocalUserPanel.AllowChanges)
				return;

			// Check input to change configurator
			if (!IsEditing)
			{
				if (Slot.Input.Up)
				{
					OnNavigate(-1);
				}
				else if (Slot.Input.Down)
				{
					OnNavigate(1);
				}

				// Open configurator on press A
				if (Slot.Input.South)
				{
					HandleUserConfirm();
				}

				if (Slot.LocalPlayer != null && Slot.LocalPlayer.GetButton(15))
				{
					Slot.LocalPlayer.SetVibration(0, Slot.LocalPlayer.GetVibration(0) + (Time.deltaTime * 10f), true);
					if (Slot.LocalPlayer.GetButtonTimedPressDown(15, 0.5f))
						Slot.TryRemoveSlot();// TryRemovePlayerFromSlot(currentPlayer, slot);
				}
			}
			else
			{
				if (_configurators.Count > 0)
				{
					_configurators[_configuratorIndex].ConfiguratorUpdate();

					if(IsEditing)
					{
						SetNavigationIndex(_configurators[_configuratorIndex].NavigatorIndex);
						SetMouseButtonsState(_configurators[_configuratorIndex].EnableConfirmButton, _configurators[_configuratorIndex].EnableCancelButton);
					}
				}
			}
		}

		#endregion

		#region Configurator Methods

		public void ForceClose()
		{
			if (IsEditing && _configurators.Count > 0)
				_configurators[_configuratorIndex].ForceClose();

			CleanupConfigurator();
		}

		#endregion

		#region UI Setup

		/// <summary>
		/// Sets up the message display UI.
		/// </summary>
		private void SetupMessageUI()
		{
			_titleText = UIHelper.GetPrefab(UIHelper.EUIPrefabIndex.SimpleText);
			_titleText.transform.SetParent(transform, false);
			var messageRect = _titleText.GetComponent<RectTransform>();
			messageRect.anchorMin = Vector2.zero;
			messageRect.anchorMax = Vector2.one;
			_titleController = _titleText.GetComponentInChildren<LanguageTextMeshController>();
			var messageHgText = _titleText.GetComponent<HGTextMeshProUGUI>();
			messageHgText.maxVisibleLines = 1;
			messageHgText.overflowMode = TextOverflowModes.Overflow;
			messageHgText.alignment = TextAlignmentOptions.Center;
			messageHgText.horizontalAlignment = HorizontalAlignmentOptions.Center;
			messageHgText.raycastTarget = false;
			_titleText.gameObject.SetActive(true);
		}

		/// <summary>
		/// Creates all available option configurators.
		/// </summary>
		private void CreateOptions()
		{
			var optionConfiguratorType = typeof(OptionConfigurator);

			// Find all non-abstract classes that inherit from OptionConfigurator
			foreach (var type in Assembly.GetExecutingAssembly().GetTypes().Where(x =>
					optionConfiguratorType.IsAssignableFrom(x) && !x.IsAbstract))
			{
				OptionConfigurator configurator = (OptionConfigurator) new GameObject(type.Name).AddComponent(type);

				_configurators.Add(configurator);

				configurator.OnFinished += OnFinished;
				configurator.Options = this;
				configurator.transform.SetParent(transform);
				configurator.transform.localPosition = Vector3.zero;
				configurator.gameObject.SetActive(true);
				configurator.name = type.Name;
			}

			// Sort configurators by priority
			_configurators = _configurators.OrderBy(x => x.GetPriority()).ToList();
		}

		/// <summary>
		/// Set the navigation dot index.
		/// </summary>
		private void SetNavigationIndex(int index)
		{
			Slot.NavigatorIndex = index;
		}

		private void SetNavigatorCount(int count)
		{
			Slot.NavigatorCount = count;
		}

		private void SetMouseButtonsState(bool confirm, bool cancel)
		{
			Slot.EnableConfirmButton = confirm;
			Slot.EnableCancelButton = cancel;
		}

		#endregion

		#region Navigation Methods

		private void HandleUserConfirm()
		{
			if (IsEditing)
				_configurators[_configuratorIndex].OnConfirm();
			else
				OpenConfigurator();
		}

		private void SubscribeToNavigation()
		{
			Slot.OnNavigateIndex += OnNavigateIndex;
			Slot.OnCancel += OnCancel;
			Slot.OnConfirm += OnConfirm;
		}

		/// <summary>
		/// Opens the profile configurator by default.
		/// </summary>
		internal void OpenProfileConfigurator()
		{
			if (_configurators.Count == 0) return;

			var profileConfiguratorType = typeof(ProfileConfigurator);

			// Find and select the profile configurator
			for (int i = 0; i < _configurators.Count; i++)
			{
				if (profileConfiguratorType.IsAssignableFrom(_configurators[i].GetType()))
				{
					_configuratorIndex = i;
					break;
				}
			}

			OpenConfigurator();
		}

		/// <summary>
		/// Moves to the next configurator in the list.
		/// </summary>
		private void NextConfigurator() =>
			_configuratorIndex = Mathf.Clamp(_configuratorIndex + 1, 0, _configurators.Count - 1);

		/// <summary>
		/// Moves to the previous configurator in the list.
		/// </summary>
		private void PreviousConfigurator() =>
			_configuratorIndex = Mathf.Clamp(_configuratorIndex - 1, 0, _configurators.Count - 1);

		/// <summary>
		/// Handles cancellation input from UI button.
		/// </summary>
		private void OnCancel()
		{
			Log.Print("SlotOptions.OnCancel");
			if (IsEditing)
				_configurators[_configuratorIndex].OnCancel();
		}

		/// <summary>
		/// Handles confirmation input from UI button.
		/// </summary>
		private void OnConfirm() => HandleUserConfirm();

		private void OnNavigateIndex(int index)
		{
			Log.Print($"SlotOptions::OnNavigateIndex: '{index}', IsEditing = '{IsEditing}'");

			if (!IsEditing)
			{
				_configuratorIndex = index;

				DisplayOptionName();
				SetNavigationIndex(index);
			}
			else
			{
				_configurators[_configuratorIndex].OnNavigateIndex(index);
			}
		}

		/// <summary>
		/// Handles navigation input from UI buttons.
		/// </summary>
		private void OnNavigate(int direction)
		{
			if (!IsEditing)
			{
				if (direction == -1)
					PreviousConfigurator();
				else
					NextConfigurator();

				DisplayOptionName();
				SetNavigationIndex(_configuratorIndex);
			}
			else if (_configurators.Count > 0)
			{
				_configurators[_configuratorIndex].OnNavigate(direction);
				SetNavigationIndex(_configurators[_configuratorIndex].NavigatorIndex);
			}
		}

		/// <summary>
		/// Opens the currently selected configurator.
		/// </summary>
		internal void OpenConfigurator()
		{
			if (IsEditing || _configurators.Count == 0)
				return;

			Slot.SetMessage(null);
			IsEditing = true;
			_configurators[_configuratorIndex].Open();
			Slot.NavigatorCount = _configurators[_configuratorIndex].NavigatorCount;
		}

		/// <summary>
		/// Closes the currently active configurator.
		/// </summary>
		internal void CloseConfigurator()
		{
			if (!IsEditing || _configurators.Count == 0)
				return;

			_configurators[_configuratorIndex].ForceClose();

			CleanupConfigurator();
		}

		/// <summary>
		/// Called when a configurator finishes its operation.
		/// </summary>
		internal void OnFinished()
		{
			CleanupConfigurator();
		}

		private void CleanupConfigurator()
		{
			IsEditing = false;

			DisplayOptionName();
			SetNavigatorCount(_configurators.Count);
			SetNavigationIndex(_configuratorIndex);
			SetMouseButtonsState(Slot.IsKeyboardUser, true);
		}
		#endregion

		#region UI Methods

		/// <summary>
		/// Displays the name of the currently selected option.
		/// </summary>
		private void DisplayOptionName()
		{
			if (_configurators.Count == 0) return;
			SetMessage(_configurators[_configuratorIndex].GetName());
		}

		public void SetMessage(string token) => SetMessage(token, Color.white);

		/// <summary>
		/// Sets the message text for the options display.
		/// </summary>
		public void SetMessage(string token, Color color)
		{
			if (token == null)
			{
				_titleText.gameObject.SetActive(false);
				_titleText.GetComponent<TextMeshProUGUI>().color = Color.white;
			}
			else
			{
				_titleController.token = token;
				_titleText.gameObject.SetActive(true);
				_titleText.GetComponent<TextMeshProUGUI>().color = color;
			}
		}

		#endregion
	}
}