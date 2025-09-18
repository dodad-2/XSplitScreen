using RoR2;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Dodad.XSplitscreen.Components
{
	/// <summary>
	/// Configurator for selecting user profiles.
	/// Handles profile selection, display, and assignment to user slots.
	/// </summary>
	public class ProfileConfigurator : OptionConfigurator
	{
		#region Static Fields

		/// <summary>
		/// Tracks currently active profiles to prevent duplicates.
		/// </summary>
		private static List<string> _activeProfiles;

		/// <summary>
		/// Event triggered when a profile is selected.
		/// </summary>
		private static Action _onProfileSelect;

		#endregion

		#region Private Fields

		private string[] _profileKeys;
		private int _profileIndex;
		//private HGTextMeshProUGUI _messageTextMesh;
		private bool _isOpen;

		#endregion

		#region OptionConfigurator Implementation

		/// <summary>
		/// Always display profile configurator first in the list.
		/// </summary>
		public override int GetPriority() => 0;

		/// <summary>
		/// Localization token for the profile configurator.
		/// </summary>
		public override string GetName() => Options.Slot.Profile == null ? "XSS_CONFIG_PROFILE" : Options.Slot.Profile.name;

		#endregion

		#region Unity Lifecycle

		/// <summary>
		/// Set up the UI components when the configurator is created.
		/// </summary>
		public void Awake()
		{
			EnableConfirmButton = true;
			//SetupMessageUI();
		}

		/// <summary>
		/// Clean up when the configurator is destroyed.
		/// </summary>
		public void OnDestroy()
		{
			ReleaseProfile();

			if (_isOpen)
			{
				_onProfileSelect -= OnProfileSelected;
			}
		}

		#endregion

		#region UI Setup

		/// <summary>
		/// Sets up the message UI for displaying the selected profile name.
		/// </summary>
		private void SetupMessageUI()
		{
			var messageText = UIHelper.GetPrefab(UIHelper.EUIPrefabIndex.SimpleText);
			var messageLayoutElement = messageText.AddComponent<LayoutElement>();
			messageLayoutElement.flexibleWidth = 1f;
			messageText.transform.SetParent(transform, false);
			Destroy(messageText.GetComponent<LanguageTextMeshController>());
			//_messageTextMesh = messageText.GetComponent<HGTextMeshProUGUI>();
			SetMessage(null, Color.white);
			messageText.gameObject.SetActive(false);
		}

		#endregion

		#region Profile Management

		/// <summary>
		/// Displays a message in the UI with the specified color.
		/// </summary>
		public void SetMessage(string text, Color color)
		{
			Options.SetMessage(text, color);
			/*_messageTextMesh.text = text;
			_messageTextMesh.color = color;
			_messageTextMesh.gameObject.SetActive(text != null);*/
		}

		/// <summary>
		/// Opens the profile selector interface.
		/// </summary>
		public override void Open()
		{
			Log.Print($"ProfileConfigurator.Open: '{transform.parent.parent.name}' subscribing");

			_activeProfiles ??= new List<string>();

			OnProfileSelected();
			_onProfileSelect += OnProfileSelected;

			_isOpen = true;
		}

		/// <summary>
		/// Forced close without selecting a profile.
		/// </summary>
		public override void ForceClose()
		{
			if (_isOpen)
			{
				_onProfileSelect -= OnProfileSelected;
				_isOpen = false;
				SetMessage(null, Color.white);
			}
		}

		/// <summary>
		/// Updates the list of available profiles when any profile is selected.
		/// </summary>
		private void OnProfileSelected()
		{
			_profileIndex = 0;

			string currentProfile = Options.Slot.Profile?.fileName;

			// Find valid profiles (current one and those not active)
			_profileKeys = PlatformSystems.saveSystem.loadedUserProfiles.Keys
				.Where(x =>
					x == currentProfile ||
					!_activeProfiles.Contains(x))
				.ToArray();

			// If the current profile exists in the pool, set it as the selected index
			if (currentProfile != null)
			{
				for (int i = 0; i < _profileKeys.Length; i++)
				{
					if (_profileKeys[i] == currentProfile)
					{
						_profileIndex = i;
						break;
					}
				}
			}

			if (_profileKeys.Length == 0)
				Log.Print("Need a guest profile!");

			UpdateProfileName();
			UpdateNavigatorCount();
		}

		private void UpdateNavigatorCount()
		{
			NavigatorCount = _profileKeys.Length;
		}

		private void UpdateNavigatorIndex()
		{
			NavigatorIndex = _profileIndex;
		}

		/// <summary>
		/// Selects a profile and assigns it to the slot.
		/// </summary>
		private void SelectProfile(string profileName)
		{
			if(_activeProfiles.Contains(profileName) && (Options.Slot.Profile == null ? true : Options.Slot.Profile.fileName != profileName))
			{
				OnProfileSelected();

				return;
			}

			ReleaseProfile();

			if (profileName != null)
			{
				_activeProfiles.Add(profileName);
				Options.Slot.Profile = PlatformSystems.saveSystem.GetProfile(profileName);
			}

			Log.Print($"ProfileConfigurator.SelectProfile: '{transform.parent.parent.name}' unsubscribing, profile = '{profileName}'");

			_onProfileSelect -= OnProfileSelected;

			_isOpen = false;

			OnFinished?.Invoke();
		}

		/// <summary>
		/// Releases the current profile from the active list.
		/// </summary>
		private void ReleaseProfile()
		{
			if (Options.Slot.Profile != null)
				_activeProfiles.Remove(Options.Slot.Profile.fileName);

			Options.Slot.Profile = null;
		}

		#endregion

		#region Input Handling

		public override void OnCancel()
		{

		}

		public override void OnConfirm()
		{
			Log.Print($" ------------ Button Press (south) (ConfiguratorUpdate '{transform.parent.parent.name}') ------------");

			// Safety check in case there are no profiles
			if (_profileKeys != null && _profileKeys.Length > 0)
			{
				SelectProfile(_profileKeys[_profileIndex]);
			}
			else
			{
				// Handle the no-profile case
				SelectProfile(null);
			}
		}

		/// <summary>
		/// Handles navigation input from the user interface.
		/// </summary>
		public override void OnNavigate(int direction)
		{
			if (direction == -1)
				SelectPreviousProfile();
			else
				SelectNextProfile();

			UpdateNavigatorIndex();
		}

		public override void OnNavigateIndex(int index)
		{
			_profileIndex = (int) Mathf.Clamp(index, 0, _profileKeys.Length - 1);
			UpdateProfileName();
			UpdateNavigatorIndex();
		}

		/// <summary>
		/// Updates the configurator based on input.
		/// </summary>
		public override void ConfiguratorUpdate()
		{
			if (Options.Slot.Input.Up)
			{
				OnNavigate(-1);
			}
			else if (Options.Slot.Input.Down)
			{
				OnNavigate(1);
			}
			else if (Options.Slot.Input.South)
			{
				OnConfirm();
			}
		}

		/// <summary>
		/// Selects the previous profile in the list.
		/// </summary>
		private void SelectPreviousProfile()
		{
			Log.Print($" ------------ Button Press (left) (ConfiguratorUpdate '{transform.parent.parent.name}') ------------");
			_profileIndex = (int) Mathf.Clamp(_profileIndex - 1, 0, _profileKeys.Length - 1);
			UpdateProfileName();
		}

		/// <summary>
		/// Selects the next profile in the list.
		/// </summary>
		private void SelectNextProfile()
		{
			Log.Print($" ------------ Button Press (right) (ConfiguratorUpdate '{transform.parent.parent.name}') ------------");
			_profileIndex = (int) Mathf.Clamp(_profileIndex + 1, 0, _profileKeys.Length - 1);
			UpdateProfileName();
		}

		#endregion

		#region UI Updates

		/// <summary>
		/// Updates the displayed profile name.
		/// </summary>
		private void UpdateProfileName()
		{
			if (_profileKeys == null || _profileKeys.Length == 0)
				Options.SetMessage("Guest", new Color(1f, 1f, 0f));
			else
				Options.SetMessage(PlatformSystems.saveSystem.GetProfile(_profileKeys[_profileIndex]).name, new Color(1f, 1f, 0f));
		}

		#endregion
	}
}