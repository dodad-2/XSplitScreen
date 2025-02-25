using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace dodad.XSplitscreen
{
	public class XUser : LocalUser
	{
		/// <summary>
		/// All configurators should use this dict to store persistent data
		/// </summary>
		internal MultiDictionary config;
	}
}
