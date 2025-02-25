using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace dodad.XSplitscreen.Components
{
	internal class AssignmentPanel : MonoBehaviour
	{
		private static List<AssignmentPanel> Instances;

		private Graph graph;
		private AssignmentDisplayController display;

		private int targetDisplay;

		//-----------------------------------------------------------------------------------------------------------

		public void Initialize()
		{
			Instances ??= new();
			Instances.Add(this);

			graph = new Graph();

			display = transform.Find("Display").gameObject.AddComponent<AssignmentDisplayController>();

			display.Initialize();

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

			OnGraphUpdated();

			// TODO: Load from JSON

			/*// Graph tests

			assignmentGraph = new Graph();

			var panels = assignmentGraph.GetAllPanelRects();

			foreach (var panel in panels)
				Log.Print($"Panel A: '{panel.Key}' = '{panel.Value}'");

			assignmentGraph.nodes = new int[6][];

			for(int x = 0; x < 6; x++)
			{
				assignmentGraph.nodes[x] = new int[3];

				for (int y = 0; y < 3; y++)
				{
					if(x < 3)
						assignmentGraph.nodes[x][y] = 0;
					else
						assignmentGraph.nodes[x][y] = 1;
				}
			}

			panels = assignmentGraph.GetAllPanelRects();

			Log.Print(" ------------- ");

			foreach (var panel in panels)
				Log.Print($"Panel B: '{panel.Key}' = '{panel.Value}'");*/

			//
		}

		//-----------------------------------------------------------------------------------------------------------

		public void OnDestroy()
		{
			Instances.Remove(this);
		}

		//-----------------------------------------------------------------------------------------------------------

		private void OnGraphUpdated()
		{
			display.UpdateDisplay(graph.GetAllPanelRects());
		}

		//-----------------------------------------------------------------------------------------------------------

		internal class Graph
		{
			public Action OnGraphUpdated;

			internal int[][] nodes;

			public Graph()
			{
				nodes = new int[1][];
				nodes[0] = new int[1];
				nodes[0][0] = -1;
			}

			//-----------------------------------------------------------------------------------------------------------

			/// <summary>
			/// Calculate rects for each panel and return them
			/// </summary>
			/// <returns></returns>
			public Dictionary<int, Rect> GetAllPanelRects()
			{
				Dictionary<int, Rect> panels = new Dictionary<int, Rect>();

				// Loop through all nodes and find the maximums of each group

				int xMax = nodes.Length;
				int yMax = nodes[0].Length;

				for (int x = 0; x < xMax; x++)
				{
					for(int y = 0;  y < yMax; y++)
					{
						int id = nodes[x][y];

						if (!panels.ContainsKey(id))
							panels.Add(id, GetPanelRect(id));
					}
				}

				return panels;
			}

			//-----------------------------------------------------------------------------------------------------------

			/// <summary>
			/// Add a new panel to the graph
			/// </summary>
			/// <param name="id"></param>
			/// <param name="width"></param>
			/// <param name="height"></param>
			/// <param name="split">If true, the nearest panel will be split in half. If false the entire graph will be shifted</param>
			public void AddPanel(int x, int y, bool split = true)
			{
				int graphWidth = nodes.Length;
				int graphHeight = nodes[0].Length;

				if (graphWidth == 1 && graphHeight == 1)
					split = false;

				if(split)
				{

				}
				else
				{

				}

				OnGraphUpdated?.Invoke();
			}

			//-----------------------------------------------------------------------------------------------------------

			/// <summary>
			/// Get a Rect describing the screen coverage of a particular panel
			/// </summary>
			/// <param name="id"></param>
			/// <returns></returns>
			public Rect GetPanelRect(int id)
			{
				float xBounds = nodes.Length;
				float yBounds = nodes[0].Length;

				GetPanelCoordinates(id, xBounds, yBounds, out float maxX, out float minX, out float maxY, out float minY);

				// Calculate rect by dimensions

				maxX++;
				maxY++;

				//Log.Print($"GetCoverage: id = '{id}', xBounds = '{xBounds}', yBounds = '{yBounds}', minX = '{minX}', maxX = '{maxX}', minY = '{minY}', maxY = '{maxY}'");

				Rect rect = new Rect(
					minX / xBounds, 
					minY / yBounds,
					(maxX - minX) / xBounds,
					(maxY - minY) / yBounds);

				return rect;
			}

			//-----------------------------------------------------------------------------------------------------------
			
			/// <summary>
			/// Get the upper and lower panel coordinates
			/// </summary>
			/// <param name="panelId"></param>
			public void GetPanelCoordinates(int id, float xBounds, float yBounds,
				out float maxX, out float minX, out float maxY, out float minY)
			{
				maxX = int.MinValue;
				minX = int.MaxValue;

				maxY = int.MinValue;
				minY = int.MaxValue;

				for (int x = 0; x < xBounds; x++)
				{
					for (int y = 0; y < yBounds; y++)
					{
						if (nodes[x][y] == id)
						{
							if (x < minX)
								minX = x;
							if (x > maxX)
								maxX = x;

							if (y < minY)
								minY = y;
							if (y > maxY)
								maxY = y;
						}
					}
				}
			}
		}
	}
}
