using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace dodad.XSplitscreen.Components
{
	public class AssignmentDisplay : MonoBehaviour
	{
		private RectTransform rectTransform;
		private UIJuice juice;

		public void Initialize(int id, Rect rect)
		{
			rectTransform = GetComponent<RectTransform>();
			juice = gameObject.GetComponent<UIJuice>();

			gameObject.SetActive(true);

			UpdateAssignment(rect);
		}

		public void UpdateAssignment(Rect rect)
		{
			juice.TransitionAlphaFadeIn();
		}
	}
}
