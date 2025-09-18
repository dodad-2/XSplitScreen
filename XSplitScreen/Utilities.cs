using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dodad.XSplitscreen
{
	internal static class Utilities
	{
		/// <summary>
		/// Adds an Image component to the current GameObject and sets its sprite to the default Unity white texture.
		/// </summary>
		public static void AddDefaultImage(GameObject gameObject)
		{
			// Add an Image component if it doesn't already exist
			Image image = gameObject.GetComponent<Image>();

			if (image == null)
			{
				image = gameObject.AddComponent<Image>();
			}

			// Assign the default white sprite
			image.sprite = Sprite.Create(
				Texture2D.whiteTexture,
				new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
				new Vector2(0.5f, 0.5f)
			);

			// Optionally, set Image type to Simple and preserve aspect
			image.type = Image.Type.Simple;
			image.preserveAspect = false;
		}

		/// <summary>
		/// Aligns the RectTransform to the left or right edge of its parent, based on the input string.
		/// </summary>
		/// <param name="rectTransform">The RectTransform to align.</param>
		/// <param name="side">"left" or "right".</param>
		public static void AlignToEdge(RectTransform rectTransform, string side)
		{
			if (rectTransform == null || rectTransform.parent == null)
				return;

			// Anchor and pivot settings for left/right alignment
			if (side == "left")
			{
				rectTransform.anchorMin = new Vector2(0f, rectTransform.anchorMin.y);
				rectTransform.anchorMax = new Vector2(0f, rectTransform.anchorMax.y);
				rectTransform.pivot = new Vector2(0f, rectTransform.pivot.y);

				// Attach to leftmost edge
				rectTransform.anchoredPosition = new Vector2(0f, rectTransform.anchoredPosition.y);
			}
			else if (side == "right")
			{
				rectTransform.anchorMin = new Vector2(1f, rectTransform.anchorMin.y);
				rectTransform.anchorMax = new Vector2(1f, rectTransform.anchorMax.y);
				rectTransform.pivot = new Vector2(1f, rectTransform.pivot.y);

				// Attach to rightmost edge
				rectTransform.anchoredPosition = new Vector2(0f, rectTransform.anchoredPosition.y);
			}
			else
			{
				Log.Print("AlignToEdge: side must be \"left\" or \"right\".", Log.ELogChannel.Warning);
			}
		}

		/// <summary>
		/// Sets the child RectTransform to fill a percentage of the parent's area with a uniform margin.
		/// For example, margin = 0.9 means the child fills 90% of parent (10% margin).
		/// This preserves the aspect ratio correctly and centers the child in the parent.
		/// </summary>
		/// <param name="child">The child RectTransform to size</param>
		/// <param name="fillRatio">The ratio of parent space to fill (0-1)</param>
		public static void SetChildRectToPixelMargin(RectTransform child, float marginPx)
		{
			if (child == null) return;
			RectTransform parent = child.parent as RectTransform;
			if (parent == null) return;

			float parentWidth = parent.rect.width;
			float parentHeight = parent.rect.height;

			float childWidth = Mathf.Max(0, parentWidth - 2 * marginPx);
			float childHeight = Mathf.Max(0, parentHeight - 2 * marginPx);

			child.anchorMin = child.anchorMax = new Vector2(0.5f, 0.5f);
			child.pivot = new Vector2(0.5f, 0.5f);
			child.anchoredPosition = Vector2.zero;
			child.sizeDelta = new Vector2(childWidth, childHeight);
		}

		/// <summary>
		/// Utility class for finding the first Selectable UI element under a virtual cursor within a container.
		/// Highly optimized for frequent calls (10+ per frame).
		/// </summary>
		public static class Hovercast
		{
			/// <summary>
			/// Returns the first Selectable under the virtual cursor within the container hierarchy.
			/// If none is found, returns null.
			/// </summary>
			/// <param name="container">The RectTransform containing the UI elements.</param>
			/// <param name="virtualCursor">The RectTransform representing the cursor.</param>
			/// <returns>The first Selectable being hovered by the cursor, or null.</returns>
			public static Selectable GetSelectableUnderCursor(RectTransform container, RectTransform virtualCursor)
			{
				if (container == null || virtualCursor == null)
					return null;

				// Get cursor position in world space
				Vector3 cursorWorldPos = virtualCursor.position;

				// Recursively search for selectable under cursor
				return FindSelectableRecursive(container, cursorWorldPos);
			}

			private static Selectable FindSelectableRecursive(RectTransform parent, Vector3 cursorWorldPos)
			{
				// Traverse children in reverse order for topmost first
				for (int i = parent.childCount - 1; i >= 0; i--)
				{
					var child = parent.GetChild(i) as RectTransform;
					if (child == null || !child.gameObject.activeInHierarchy)
						continue;

					// Convert cursor position to child's local space
					Vector2 localPoint = child.InverseTransformPoint(cursorWorldPos);

					if (child.rect.Contains(localPoint))
					{
						// Check for Selectable
						var selectable = child.GetComponent<Selectable>();
						if (selectable != null && selectable.IsInteractable())
							return selectable;

						// Traverse nested children
						var nested = FindSelectableRecursive(child, cursorWorldPos);
						if (nested != null)
							return nested;
					}
				}
				return null;
			}
		}
	}
}
