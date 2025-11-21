using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Dodad.XSplitscreen.Components
{
	public class DisplayConfigurator : OptionConfigurator
	{
		private bool _isOpen;

		public void Awake()
		{
			EnableCancelButton = true;
			EnableConfirmButton = false;
		}

		public override void ForceClose()
		{

		}

		public override string GetName()
		{
			return "XSS_SELECT_MONITOR";
		}

		public override void OnCancel()
		{
			Cleanup();

			OnFinished();
		}

		public override void OnConfirm()
		{
		}

		public override void OnNavigate(int direction)
		{
			if (Options.Slot.IsKeyboardUser)
				return;

			if (direction > 0)
				NextDisplay();
			else
				PreviousDisplay();

			Options.OpenConfigurator();
		}

		public override void OnNavigateIndex(int index)
		{

		}

		public override bool CanOpen() => true;

		public override void Open()
		{
			_isOpen = true;

			UpdateMessage();
		}

		public void Cleanup()
		{
			_isOpen = false;
		}

		private void NextDisplay() => Options.Slot.MoveSlotByDirection(1);
		private void PreviousDisplay() => Options.Slot.MoveSlotByDirection(-1);

		private void UpdateMessage()
		{
			if (!_isOpen)
				Options.SetMessage(GetName());
			else
				Options.SetMessage(Options.Slot.Panel.Controller.monitorId.ToString(), Options.Slot.IsKeyboardUser ? Color.white : Color.yellow);
		}

		public override void ConfiguratorUpdate()
		{
			if (Options.Slot.IsKeyboardUser)
				return;

			if(Options.Slot.Input.South || Options.Slot.Input.East)
			{
				Cleanup();

				OnFinished();
			}
		}
	}
}
