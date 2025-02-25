using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace dodad.XSplitscreen.Components
{
	public abstract class OptionConfigurator : MonoBehaviour
	{
		/// <summary>
		/// Lower priority items will be displayed further to the left in the options
		/// </summary>
		/// <returns></returns>
		public virtual int GetPriority() => 10;

		public abstract string GetName();

		public SlotOptions options;

		/// <summary>
		/// Invoked when the user is finished editing this option
		/// </summary>
		public Action OnFinished;

		/// <summary>
		/// Invoked when the user requests to edit this option
		/// </summary>
		public abstract void Open();

		/// <summary>
		/// TODO: Unused
		/// </summary>
		public abstract void ForceClose();

		/// <summary>
		/// Check input here
		/// </summary>
		public virtual void ConfiguratorUpdate() { }
	}
}
