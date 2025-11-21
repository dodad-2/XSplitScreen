using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static ak.wwise.core;
using static Rewired.Demos.GamepadTemplateUI.GamepadTemplateUI;

namespace Dodad.XSplitscreen.Components
{
	public class TrailsConfigurator : OptionConfigurator
	{
		private static string[] _trailKeys;

		private TrailsSettingsModule _settings;

		public int TrailIndex
		{
			get => _trailIndex;
			set
			{
				value = Mathf.Clamp(value, 0, NavigatorCount - 1);
				_trailIndex = value;
				NavigatorIndex = value;
				SaveTrailKey();
			}
		}
		private int _trailIndex;

		public override string GetName() => "XSS_TRAILS";

		private void Awake()
		{
			_trailKeys ??= ["none", .. ParticleSystemFactory.GetParticleSystemKeys()];
			NavigatorCount = _trailKeys.Length;
			EnableConfirmButton = true;
		}

		public override void OnCancel()
		{
			OnFinished();
		}

		public override void ForceClose()
		{

		}

		public override void OnConfirm()
		{
			OnFinished();
		}

		public override void OnNavigate(int direction)
		{
			TrailIndex += direction;// = Mathf.Clamp(direction + _trailIndex, 0, NavigatorCount - 1);

			UpdateMessage();
		}

		public override void OnNavigateIndex(int index)
		{
			TrailIndex = index;
			UpdateMessage();
		}

		public override void Open()
		{
			UpdateMessage();
		}

		public override void OnLoadProfile()
		{
			LoadTrailKey();
		}

		private void LoadTrailKey()
		{
			_settings = null;

			if (Options.Slot.Profile != null)
			{
				_settings = SplitScreenSettings.GetOrCreateUserModule<TrailsSettingsModule>(Options.Slot.Profile.fileName);
				UpdateSavedIndex();
				Log.Print($"Loaded '{TrailIndex}' from '{_settings.TrailKey}'");
			}
		}

		public override bool CanOpen() => Options.Slot.Profile != null;

		private void UpdateSavedIndex() => TrailIndex = Array.IndexOf(_trailKeys, _settings.TrailKey);

		private void SaveTrailKey()
		{
			if (Options.Slot.Profile == null)
				return;

			_settings.TrailKey = _trailKeys[_trailIndex];
			SplitScreenSettings.MarkUserDirty(Options.Slot.Profile.fileName);
		}

		private void UpdateMessage()
		{
			Options.SetMessage(_trailKeys[_trailIndex]);
		}

		public override void ConfiguratorUpdate()
		{
			if (Options.Slot.Input.South || Options.Slot.Input.East)
				OnFinished();
		}
	}
}
