using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static Dodad.XSplitscreen.Graph;

namespace Dodad.XSplitscreen.Components
{

	/// <summary>
	/// Controls the display and lifecycle of AssignmentDisplay objects for assignment rectangles.
	/// </summary>
	public class AssignmentDisplayController : MonoBehaviour
	{
		private static GameObject AssignmentPrefab;

		// Holds currently active assignment displays, keyed by assignment id
		private readonly Dictionary<int, AssignmentDisplay> _assignments = new();

		public AssignmentPanel Panel => _panel;
		private AssignmentPanel _panel;
		private Transform _assignmentArea;

		/// <summary>
		/// Initializes the assignment display controller and sets up UI juice transitions.
		/// </summary>
		public void Initialize(AssignmentPanel panel)
		{
			AssignmentPrefab ??= Plugin.Resources.LoadAsset<GameObject>("Assignment.prefab");

			var canvas = gameObject.AddComponent<CanvasGroup>();
			var juice = gameObject.AddComponent<UIJuice>();

			juice.canvasGroup = canvas;
			juice.transitionDuration = 0.5f;
			juice.originalAlpha = 1f;

			// Fade in on menu entry
			SplitscreenMenuController.Singleton.onEnter.AddListener(() =>
			{
				juice.TransitionAlphaFadeIn();
			});

			this._panel = panel;

			_assignmentArea = transform.Find("VerticalLayout/Middle/AssignmentArea");
		}

		/// <summary>
		/// Updates the assignment display to match the provided assignment rectangles.
		/// </summary>
		/// <param name="display">Dictionary of assignment IDs and their target rectangles.</param>
		public void UpdateDisplay(IReadOnlyDictionary<int, Face> display)
		{
			// Add or update assignments
			foreach (var item in display)
			{
				if (!_assignments.ContainsKey(item.Key))
					AddAssignment(item.Key, item.Value);
				else
					UpdateAssignment(item.Key, item.Value);
			}

			// Remove assignments that are not present in the display dictionary
			// Use ToList to avoid modification during iteration
			foreach (var assignment in _assignments.ToList())
			{
				if (!display.ContainsKey(assignment.Key))
				{
					GameObject.Destroy(assignment.Value.gameObject);
					_assignments.Remove(assignment.Key);
				}
			}
		}

		/// <summary>
		/// Instantiates and adds a new AssignmentDisplay.
		/// </summary>
		private void AddAssignment(int id, Face face)
		{
			var assignmentGO = Instantiate(AssignmentPrefab, _assignmentArea);
			var assignment = assignmentGO.AddComponent<AssignmentDisplay>();
			var rectTransform = assignment.GetComponent<RectTransform>();

			// Setup UIJuice for the assignment display
			var juice = assignmentGO.AddComponent<UIJuice>();
			var canvasGroup = assignmentGO.AddComponent<CanvasGroup>();
			juice.canvasGroup = canvasGroup;
			juice.transitionDuration = 0.5f;
			juice.originalAlpha = 1f;

			assignment.Initialize(id, face, this);

			_assignments.Add(id, assignment);
		}

		/// <summary>
		/// Updates an existing AssignmentDisplay.
		/// </summary>
		private void UpdateAssignment(int id, Face face)
		{
			if (_assignments.TryGetValue(id, out var assignment))
			{
				assignment.UpdateAssignment(face);
			}
		}

		public void Subdivide(Face face, bool vertical)
		{
			_panel.Subdivide(face, vertical);
		}

		public void Remove(Face face) => _panel.Remove(face);

		public void AssignClaimToKeyboard(DisplayClaim claim)
		{
			foreach(var panel in LocalUserPanel.Instances)
			{
				var slots = panel.UserSlots;

				foreach(var slot in slots)
				{
					if(slot.IsKeyboardUser)
					{
						var configurator = slot.GetComponentInChildren<AssignmentConfigurator>();

						if(configurator == null)
						{
							Log.Print("Configurator not found");

							return;
						}

						configurator.ReceiveClaim(claim);

						return;
					}
				}
			}
		}
	}
}
