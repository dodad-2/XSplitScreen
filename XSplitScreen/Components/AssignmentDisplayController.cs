using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace dodad.XSplitscreen.Components
{
	public class AssignmentDisplayController : MonoBehaviour
	{
		private Dictionary<int, AssignmentDisplay> assignments = new();
		private static GameObject assignmentPrefab;

		public void Initialize()
		{
			assignmentPrefab ??= Plugin.resources.LoadAsset<GameObject>("assignment.prefab");

			// Juice

			var canvas = gameObject.AddComponent<CanvasGroup>();
			var juice = gameObject.AddComponent<UIJuice>();

			juice.canvasGroup = canvas;
			juice.transitionDuration = 0.5f;
			juice.originalAlpha = 1f;

			SplitscreenMenuController.Singleton.onEnter.AddListener(() =>
			{
				juice.TransitionAlphaFadeIn();
			});
		}

		public void UpdateDisplay(Dictionary<int, Rect> display)
		{
			// Add or update assignments

			foreach(var item in display)
			{
				if(!assignments.ContainsKey(item.Key))
					AddAssignment(item.Key, item.Value);
				else
					UpdateAssignment(item.Key, item.Value);
			}

			// Remove extras

			var assignmentsCopy = assignments.ToList();

			foreach (var assignment in assignmentsCopy)
			{
				if (!display.ContainsKey(assignment.Key))
				{
					assignments.Remove(assignment.Key);
					GameObject.Destroy(assignment.Value.gameObject);
				}
			}
		}

		private void AddAssignment(int id, Rect rect)
		{
			var assignment = Instantiate(assignmentPrefab).AddComponent<AssignmentDisplay>();
			var rectTransform = assignment.GetComponent<RectTransform>();

			rectTransform.SetParent(transform);

			var juice = rectTransform.gameObject.AddComponent<UIJuice>();
			var canvasGroup = juice.gameObject.AddComponent<CanvasGroup>();

			juice.canvasGroup = canvasGroup;
			juice.transitionDuration = 0.5f;
			juice.originalAlpha = 1f;

			assignment.Initialize(id, rect);

			assignments.Add(id, assignment);
		}

		private void UpdateAssignment(int id, Rect rect)
		{
			assignments[id].UpdateAssignment(rect);
		}
	}
}
