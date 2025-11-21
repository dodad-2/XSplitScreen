using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Dodad.XSplitscreen.Components
{
	public class ExecuteNextFrame : MonoBehaviour
	{
		public static void Invoke(Action action)
		{
			var go = new GameObject("RunNextFrame");
			var runner = go.AddComponent<ExecuteNextFrame>();
			runner._action = action;
		}

		private Action _action;
		
		private void Start()
		{
			_action?.Invoke();
			Destroy(gameObject);
		}
	}
}
