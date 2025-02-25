using System;
using System.Collections.Generic;
using System.Text;

namespace dodad.XSplitscreen.Components
{
	internal class ColorConfigurator : OptionConfigurator
	{
		public override string GetName() => "XSS_CONFIG_COLOR";

		public override void Open()
		{
			Log.Print($"Opened '{GetName()}'");
		}

		public override void ForceClose()
		{
			Log.Print($"Opened '{GetName()}'");
		}

		public override void ConfiguratorUpdate()
		{
			if (options.slot.input.South)
				OnFinished.Invoke();
		}
	}
}
