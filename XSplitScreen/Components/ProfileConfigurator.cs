using RoR2;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace dodad.XSplitscreen.Components
{
	public class ProfileConfigurator : OptionConfigurator
	{
		public override int GetPriority() => 0;

		public override string GetName() => "XSS_CONFIG_PROFILE";

		private static List<string> ActiveProfiles;
		private static Action OnProfileSelect;

		private string[] profileKeys;

		private int profileIndex;

		private HGTextMeshProUGUI messageTextMesh;

		private string debugName => (transform?.parent?.parent?.name) ?? "null";

		private bool isOpen;

		//-----------------------------------------------------------------------------------------------------------

		public void Awake()
		{
			var messageText = UIHelper.GetPrefab(UIHelper.EUIPrefabIndex.SimpleText);

			var messageLayoutElement = messageText.AddComponent<LayoutElement>();

			messageLayoutElement.flexibleWidth = 1f;

			messageText.transform.SetParent(transform, false);

			Destroy(messageText.GetComponent<LanguageTextMeshController>());

			messageTextMesh = messageText.GetComponent<HGTextMeshProUGUI>();

			//Log.Print($"ProfileConfigurator.Awake: '{transform.parent.parent.name}'");
			SetMessage(null, Color.white);

			messageText.gameObject.SetActive(false);
		}

		//-----------------------------------------------------------------------------------------------------------

		public void OnDestroy()
		{
			ReleaseProfile();

			if (isOpen)
			{
				//Log.Print($"ProfileConfigurator.OnDestroy: '{transform.parent.parent.name}'");
				OnProfileSelect -= OnProfileSelected;
			}
		}

		//-----------------------------------------------------------------------------------------------------------

		public void SetMessage(string text, Color color)
		{
			//Log.Print($"ProfileConfigurator.SetMessage: '{transform.parent.parent.name}', text = '{(text == null ? "null" : text)}'");

			messageTextMesh.text = text;
			messageTextMesh.color = color;
			messageTextMesh.gameObject.SetActive(text != null);
		}

		//-----------------------------------------------------------------------------------------------------------

		public override void Open()
		{
			Log.Print($"ProfileConfigurator.Open: '{transform.parent.parent.name}' subscribing");

			ActiveProfiles ??= new();

			OnProfileSelected();
			OnProfileSelect += OnProfileSelected;

			isOpen = true;
		}

		//-----------------------------------------------------------------------------------------------------------

		public override void ForceClose()
		{

		}

		//-----------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Reset selection options as another user has selected a profile
		/// </summary>
		private void OnProfileSelected()
		{
			Log.Print($"ProfileConfigurator.OnProfileSelected: '{transform.parent.parent.name}' invoked");

			profileIndex = 0;

			string currentProfile = options.slot.Profile?.fileName;

			// Find valid profiles (current one and those not active)

			profileKeys = PlatformSystems.saveSystem.loadedUserProfiles.Keys
				.Where(x => 
					x == currentProfile || 
					!ActiveProfiles.Contains(x))
				.ToArray();

			// If the current profile exists in the pool, get the new index

			if (currentProfile != null)
			{
				int profileLength = profileKeys.Length;

				for (int e = 0; e < profileLength; e++)
				{
					if (profileKeys[e] == currentProfile)
					{
						profileIndex = e;

						break;
					}
				}
			}

			if (profileKeys.Length == 0)
				Log.Print($"Need a guest profile!");

			UpdateProfileName();
		}

		//-----------------------------------------------------------------------------------------------------------

		private void SelectProfile(string profile)
		{
			ReleaseProfile();

			if (profile != null)
			{
				ActiveProfiles.Add(profile);

				options.slot.Profile = PlatformSystems.saveSystem.GetProfile(profile);
			}

			Log.Print($"ProfileConfigurator.SelectProfile: '{transform.parent.parent.name}' unsubscribing, profile = '{profile}'");

			OnProfileSelect -= OnProfileSelected;
			OnProfileSelect?.Invoke();
			OnFinished.Invoke();

			SetMessage(null, Color.white);

			isOpen = false;
		}

		//-----------------------------------------------------------------------------------------------------------

		private void ReleaseProfile()
		{
			//Log.Print($"ProfileConfigurator.ReleaseProfile: '{transform.parent.parent.name}'");

			if (options.slot.Profile != null)
				ActiveProfiles.Remove(options.slot.Profile.fileName);

			options.slot.Profile = null;
		}

		//-----------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Check input and update accordingly
		/// </summary>
		public override void ConfiguratorUpdate()
		{
			if (options.slot.input.Left)
			{
				Log.Print($" ------------ Button Press (left) (ConfiguratorUpdate '{transform.parent.parent.name}') ------------");
				PreviousProfile();
				UpdateProfileName();
			}
			else if(options.slot.input.Right)
			{
				Log.Print($" ------------ Button Press (right) (ConfiguratorUpdate '{transform.parent.parent.name}') ------------");
				NextProfile();
				UpdateProfileName();
			}
			else if (options.slot.input.South)
			{
				Log.Print($" ------------ Button Press (south) (ConfiguratorUpdate '{transform.parent.parent.name}') ------------");
				SelectProfile(profileKeys[profileIndex]);
			}
		}

		//-----------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Update the label
		/// </summary>
		private void UpdateProfileName()
		{
			//Log.Print($"ProfileConfigurator.UpdateProfileName: '{transform.parent.parent.name}'");

			if (profileKeys.Length == 0)
				SetMessage("Guest", new Color(1f, 1f, 0f));
			else
				SetMessage(PlatformSystems.saveSystem.GetProfile(profileKeys[profileIndex]).name, new Color(1f, 1f, 0f));
		}

		//-----------------------------------------------------------------------------------------------------------

		private void NextProfile() => profileIndex = (int) Mathf.Clamp(profileIndex + 1, 0, profileKeys.Length - 1);

		//-----------------------------------------------------------------------------------------------------------

		private void PreviousProfile() => profileIndex = (int) Mathf.Clamp(profileIndex - 1, 0, profileKeys.Length - 1);
	}
}
