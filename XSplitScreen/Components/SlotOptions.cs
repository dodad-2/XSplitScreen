using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace dodad.XSplitscreen.Components
{
	public class SlotOptions : MonoBehaviour
	{
		internal LocalUserSlot slot { get; private set; }

		private List<OptionConfigurator> configurators = new List<OptionConfigurator>();

		private int configuratorIndex;

		private float changeOptionTimer;

		public bool IsEditing
		{
			get
			{
				return isEditing;
			}
		}
		private bool isEditing;

		//-----------------------------------------------------------------------------------------------------------

		public void Awake()
		{
			slot ??= GetComponentInParent<LocalUserSlot>();

			if (slot.Profile == null)
				CreateOptions();
		}

		//-----------------------------------------------------------------------------------------------------------

		/*public void OnEnable()
		{
			if (!isEditing)
			{
				Log.Print($"SlotOptions.OnEnable");
				OpenConfigurator();
			}
		}*/

		//-----------------------------------------------------------------------------------------------------------

		public void OnDisable()
		{
			//Log.Print("SlotOptions: Disabled");

			if (isEditing)
				configurators[configuratorIndex].ForceClose();
		}

		//-----------------------------------------------------------------------------------------------------------

		public void Update()
		{
			if (!LocalUserPanel.AllowChanges)
				return;

			// Check input to change configurator

			if (!isEditing)
			{
				if(slot.input.Left)
				{
					PreviousConfigurator();
					DisplayOptionName();

					Log.Print(" ------------ Button Press (left) (SlotOptions) ------------");
					//Log.Print($"SlotOptions: Selected '{configurators[configuratorIndex].GetName()}'");
				}
				else if(slot.input.Right)
				{
					NextConfigurator();
					DisplayOptionName();

					Log.Print(" ------------ Button Press (right) (SlotOptions) ------------");
					//Log.Print($"SlotOptions: Selected '{configurators[configuratorIndex].GetName()}'");
				}

				// Open configurator on press A

				if (slot.input.South)
				{
					Log.Print(" ------------ Button Press (south) (SlotOptions) ------------");
					OpenConfigurator();
				}
			}
			else
				configurators[configuratorIndex].ConfiguratorUpdate();
		}

		//-----------------------------------------------------------------------------------------------------------

		private void CreateOptions()
		{
			var configPrefab = Plugin.Resources.LoadAsset<GameObject>("Option Configurator.prefab");

			var optionConfiguratorType = typeof(OptionConfigurator);

			foreach (var type in Assembly.GetExecutingAssembly().GetTypes().Where( x =>
					optionConfiguratorType.IsAssignableFrom(x) && !x.IsAbstract))
			{
				OptionConfigurator configurator = (OptionConfigurator)(Instantiate(configPrefab).AddComponent(type));

				configurators.Add(configurator);

				configurator.OnFinished += OnFinished;
				configurator.options = this;
				configurator.transform.SetParent(transform);
				configurator.transform.localPosition = Vector3.zero;
				configurator.gameObject.SetActive(true);
				configurator.name = type.Name;
			}

			configurators = configurators.OrderBy(x => x.GetPriority()).ToList();

			//Log.Print($"SlotOptions.CreateOptions: '{transform.parent.name}'");
			//OpenProfileConfigurator();
		}

		//-----------------------------------------------------------------------------------------------------------

		internal void OpenProfileConfigurator()
		{
			// Open profile configurator by default

			var profileConfiguratorType = typeof(ProfileConfigurator);

			int configCount = configurators.Count;

			for (int e = 0; e < configCount; e++)
			{
				if (profileConfiguratorType.IsAssignableFrom(configurators[e].GetType()))
				{
					configuratorIndex = e;

					break;
				}
			}

			//Log.Print($"SlotOptions.OpenProfileConfigurator");
			OpenConfigurator();
		}

		//-----------------------------------------------------------------------------------------------------------

		private void NextConfigurator() => configuratorIndex = (int) Mathf.Clamp(configuratorIndex + 1, 0, configurators.Count - 1);

		//-----------------------------------------------------------------------------------------------------------

		private void PreviousConfigurator() => configuratorIndex = (int) Mathf.Clamp(configuratorIndex - 1, 0, configurators.Count - 1);

		//-----------------------------------------------------------------------------------------------------------

		internal void OpenConfigurator()
		{
			if (isEditing)
				return;

			Log.Print($"SlotOptions.OpenConfigurator: '{transform.parent.name}'");

			slot.SetMessage(null);

			isEditing = true;

			configurators[configuratorIndex].Open();
		}

		//-----------------------------------------------------------------------------------------------------------

		internal void CloseConfigurator()
		{
			if (!isEditing)
				return;

			configurators[configuratorIndex].ForceClose();

			DisplayOptionName();

			isEditing = false;
		}

		//-----------------------------------------------------------------------------------------------------------

		internal void OnFinished()
		{
			//Log.Print("SlotOptions.OnFinished");
			DisplayOptionName();

			isEditing = false;
		}

		//-----------------------------------------------------------------------------------------------------------

		private void DisplayOptionName()
		{
			//Log.Print("SlotOptions.DisplayOptionName");
			slot.SetMessage(configurators[configuratorIndex].GetName());
		}
	}
}
