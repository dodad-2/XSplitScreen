using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace RoR2.UI.MainMenu
{
	public class BaseMainMenuScreen : MonoBehaviour
	{
		public Transform desiredCameraTransform;
		public FirstSelectedObjectProvider firstSelectedObjectProvider;
		public UnityEvent onEnter;
		public UnityEvent onExit;
	}
}