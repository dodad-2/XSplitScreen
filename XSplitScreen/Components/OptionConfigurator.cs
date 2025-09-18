using System;
using UnityEngine;

namespace Dodad.XSplitscreen.Components
{
	/// <summary>
	/// Abstract base class for configuration options in the splitscreen setup.
	/// Defines the common interface and functionality all configurators must implement.
	/// </summary>
	public abstract class OptionConfigurator : MonoBehaviour
	{
		/// <summary>
		/// Determines the order of configurators in the options menu.
		/// Lower priority items will be displayed further to the left.
		/// </summary>
		/// <returns>Priority value for sorting configurators</returns>
		public virtual int GetPriority() => 10;

		/// <summary>
		/// Gets the display name for this configurator (usually a language token).
		/// </summary>
		/// <returns>The name to display in the UI</returns>
		public abstract string GetName();

		/// <summary>
		/// Indicates whether the cancel button should be enabled for this configurator.
		/// </summary>
		public bool EnableCancelButton;

		/// <summary>
		/// Indicates whether the confirmation button should be enabled for this configurator.
		/// </summary>
		public bool EnableConfirmButton;

		/// <summary>
		/// Number of navigator dots to display.
		/// </summary>
		public int NavigatorCount;

		/// <summary>
		/// Current navigator dot to highlight
		/// </summary>
		public int NavigatorIndex;

		/// <summary>
		/// Reference to the parent options container.
		/// </summary>
		public SlotOptions Options;

		/// <summary>
		/// Invoked when the user is finished editing this option.
		/// </summary>
		public Action OnFinished;

		/// <summary>
		/// Handles navigation input from the D-pad.
		/// </summary>
		/// <param name="direction">Direction (-1 for left, 1 for right)</param>
		public abstract void OnNavigate(int direction);

		/// <summary>
		/// Handles navigation input from mouse click
		/// </summary>
		/// <param name="direction"></param>
		public abstract void OnNavigateIndex(int index);

		/// <summary>
		/// Handles navigation input from mouse.
		/// </summary>
		public abstract void OnCancel();

		/// <summary>
		/// Handles navigation input from mouse.
		/// </summary>
		public abstract void OnConfirm();

		/// <summary>
		/// Invoked when the user requests to edit this option.
		/// </summary>
		public abstract void Open();

		/// <summary>
		/// Forces the configurator to close without saving.
		/// </summary>
		public abstract void ForceClose();

		/// <summary>
		/// Called every frame when the configurator is active.
		/// Override to implement input handling and updates.
		/// </summary>
		public virtual void ConfiguratorUpdate() { }
	}
}