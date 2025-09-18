using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Dodad.XSplitscreen.Components
{
	internal class ColorConfigurator : OptionConfigurator
	{
		public override string GetName() => "XSS_CONFIG_COLOR";

		public void Awake()
		{
			EnableCancelButton = true;
		}

		public override void Open()
		{
			Options.Slot.MainColor = new Color(UnityEngine.Random.Range(0.2f, 1f), UnityEngine.Random.Range(0.2f, 1f), UnityEngine.Random.Range(0.2f, 1f), 1);
		}

		public override void ForceClose()
		{

		}

		public override void ConfiguratorUpdate()
		{
			if (Options.Slot.Input.South || Options.Slot.Input.East)
				OnFinished?.Invoke();
		}

		public override void OnNavigate(int direction)
		{
			Debug.Log(direction);
		}

		public override void OnNavigateIndex(int direction)
		{
			Debug.Log(direction);
		}

		public override void OnCancel()
		{
			OnFinished?.Invoke();
		}

		public override void OnConfirm()
		{
			OnFinished?.Invoke();
		}
	}
}
