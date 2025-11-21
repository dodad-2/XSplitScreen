using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Dodad.XSplitscreen.Components
{
	public class AssignmentDisplay : MonoBehaviour
	{
		private AssignmentDisplayController controller;
		private Graph.Face face;
		private RectTransform rectTransform;
		private RectTransform[] splitButtonTransforms;
		private RectTransform[] utilButtonTransforms;
		private RectTransform bgFillerTransform;
		private DisplayClaim claim;
		private UISquare uiSquare;
		private UIJuice juice;
		public bool updateAssignment;
		public bool updateTransforms;
		public int frameCount;

		public void Awake()
		{
			// UI Square

			var uiSquareObj = new GameObject("UISquare", typeof(UISquare));
			uiSquareObj.transform.SetParent(transform);
			uiSquareObj.transform.localScale = Vector3.one;
			uiSquare = uiSquareObj.GetComponent<UISquare>();
			uiSquare.raycastTarget = false;

			// Background image -> bgFillerTransform

			bgFillerTransform = new GameObject("BG", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>();
			bgFillerTransform.SetParent(transform);
			bgFillerTransform.localScale = Vector3.one;
			bgFillerTransform.gameObject.AddComponent<MPButton>().onClick.AddListener(() => this.controller.Panel.Display.AssignClaimToKeyboard(claim));

			var bgFillerImage = bgFillerTransform.GetComponent<UnityEngine.UI.Image>();

			bgFillerImage.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new(0.5f, 0.5f));
			bgFillerImage.color = new Color(.5f, .5f, .5f, 0.1f);

			claim = bgFillerTransform.gameObject.AddComponent<DisplayClaim>();

			// Split buttons

			splitButtonTransforms = new RectTransform[2];
			splitButtonTransforms[0] = transform.GetChild(0).GetComponent<RectTransform>();
			splitButtonTransforms[0].transform.SetParent(transform);
			splitButtonTransforms[1] = Instantiate(splitButtonTransforms[0].gameObject, transform).GetComponent<RectTransform>();
			splitButtonTransforms[1].rotation = Quaternion.Euler(0, 0, 90);
			splitButtonTransforms[0].SetAsLastSibling();

			var button1 = splitButtonTransforms[0].gameObject.AddComponent<MPButton>();
			button1.onClick.AddListener(() => controller.Subdivide(face, true));
			var button2 = splitButtonTransforms[1].gameObject.AddComponent<MPButton>();
			button2.onClick.AddListener(() => controller.Subdivide(face, false));

			// Create X button

			utilButtonTransforms = new RectTransform[1];
			utilButtonTransforms[0] = Instantiate(splitButtonTransforms[0].gameObject, transform).GetComponent<RectTransform>();

			var button3 = utilButtonTransforms[0].GetComponent<MPButton>();
			button3.onClick.RemoveAllListeners();
			button3.onClick.AddListener(() => controller.Remove(face));

			var image1 = utilButtonTransforms[0].GetComponent<UnityEngine.UI.Image>();
			var xTex = Plugin.Resources.LoadAsset<Texture2D>("device_x.png");
			image1.sprite = Sprite.Create(xTex, new Rect(0, 0, xTex.width, xTex.height), new Vector2(0.5f, 0.5f));

			// 

			gameObject.GetComponent<UnityEngine.UI.Image>().enabled = false;// color = new Color(UnityEngine.Random.Range(0.2f, 1f), UnityEngine.Random.Range(0.2f, 1f), UnityEngine.Random.Range(0.2f, 1f), 0.5f);

			rectTransform = GetComponent<RectTransform>();
			juice = gameObject.GetComponent<UIJuice>();
		}

		public void LateUpdate()
		{
			if (updateAssignment && frameCount > 0)
				UpdateAssignment(face);

			if (updateTransforms && frameCount > 0)
				UpdateTransforms();

			frameCount++;
		}

		public void Initialize(int id, Graph.Face face, AssignmentDisplayController controller)
		{
			this.face = face;
			this.controller = controller;

			updateAssignment = true;
			frameCount = 0;
			gameObject.SetActive(true);

			UpdateRect();
		}

		public void UpdateTransforms()
		{
			SetBGFillerActive(true);
			SetUISquareActive(true);
			SetButtonsActive(true);

			UpdateBGFiller();
			UpdateUISquare();
			UpdateButtons();

			juice.TransitionAlphaFadeIn();

			updateTransforms = false;
		}

		public void UpdateAssignment(Graph.Face face)
		{
			SetBGFillerActive(false);
			SetUISquareActive(false);
			SetButtonsActive(false);

			SetFaceRect(face, 0.95f);

			juice.TransitionAlphaFadeOut();

			updateAssignment = false;
			updateTransforms = true;
			frameCount = 0;

			this.face = face;

			UpdateRect();
		}

		public void SetBGFillerActive(bool state)
		{
			bgFillerTransform.gameObject.SetActive(state);
		}

		public void SetUISquareActive(bool state)
		{
			uiSquare.gameObject.SetActive(state);
		}

		public void SetButtonsActive(bool state)
		{
			foreach (var split in splitButtonTransforms)
				split.gameObject.SetActive(state);
			foreach (var util in utilButtonTransforms)
				util.gameObject.SetActive(state);
		}

		public void UpdateButtons()
		{
			ScaleAndCenterRectsSideBySide(splitButtonTransforms, 1f);
			ScaleAndAnchorRectsTopRightRightToLeft(utilButtonTransforms, 1f);
		}

		public void UpdateUISquare()
		{
			if (uiSquare == null)
				return;

			Utilities.SetChildRectToPixelMargin(uiSquare.GetComponent<RectTransform>(), 2);
		}

		public void UpdateBGFiller()
		{
			if (bgFillerTransform == null)
				return;

			Utilities.SetChildRectToPixelMargin(bgFillerTransform, 5);
		}

		/// <summary>
		/// Sets this child to fill its assigned face inside a grid,
		/// with the entire grid area centered and scaled to 80% of the parent's minimum dimension (square).
		/// Assumes all faces are generated with normalized coordinates in [0,1] (the full grid fills [0,0,1,1]).
		/// </summary>
		/// <param name="face">The Graph.Face for this child</param>
		/// <param name="percent">Rect will fill this percentage of the parent's minimum dimension</param>
		public void SetFaceRect(Graph.Face face, float percent)
		{
			var parentRT = rectTransform?.parent as RectTransform;
			if (rectTransform == null || parentRT == null || face == null)
			{
				Log.Print("SetFaceRect: Missing RectTransform, parent, or face", Log.ELogChannel.Error);
				return;
			}

			// Parent dimensions
			float parentWidth = parentRT.rect.width;
			float parentHeight = parentRT.rect.height;

			// The grid area is always [0,0,1,1] in normalized coordinates
			float gridNormWidth = 1f;
			float gridNormHeight = 1f;

			// Compute scale (fit grid into 80% of parent min dimension, maintaining 1:1 aspect)
			float parentMin = Mathf.Min(parentWidth, parentHeight);
			float gridScale = parentMin * percent;//0.8f;
			float scaleX = gridScale / gridNormWidth;
			float scaleY = gridScale / gridNormHeight;

			// Center grid area in parent
			float gridPixelWidth = gridNormWidth * scaleX;
			float gridPixelHeight = gridNormHeight * scaleY;
			float gridOffsetX = (parentWidth - gridPixelWidth) * 0.5f;
			float gridOffsetY = (parentHeight - gridPixelHeight) * 0.5f;

			// Face in grid space
			float facePixelX = gridOffsetX + face.Rect.x * scaleX;
			float facePixelY = gridOffsetY + face.Rect.y * scaleY;
			float facePixelWidth = face.Rect.width * scaleX;
			float facePixelHeight = face.Rect.height * scaleY;

			rectTransform.anchorMin = Vector2.zero;
			rectTransform.anchorMax = Vector2.zero;
			rectTransform.pivot = Vector2.zero;
			rectTransform.anchoredPosition = new Vector2(facePixelX, facePixelY);
			rectTransform.sizeDelta = new Vector2(facePixelWidth, facePixelHeight);
		}

		/// <summary>
		/// Scales and arranges given rects side-by-side, centered in the container, with optional spacing and margins.
		/// Every button keeps its aspect and never exceeds its original size.
		/// </summary>
		public void ScaleAndCenterRectsSideBySide(RectTransform[] rects, float spacing = 0f, float leftMargin = 0f, float rightMargin = 0f)
		{
			RectTransform container = GetComponent<RectTransform>();
			float containerWidth = container.rect.width - leftMargin - rightMargin;
			float containerHeight = container.rect.height;

			// Step 1: Get original sizes
			float[] originalWidths = new float[rects.Length];
			float[] originalHeights = new float[rects.Length];
			float totalOriginalWidth = Mathf.Max(0, (rects.Length - 1) * spacing); // spacing between buttons

			for (int i = 0; i < rects.Length; i++)
			{
				originalWidths[i] = rects[i].rect.width;
				originalHeights[i] = rects[i].rect.height;
				totalOriginalWidth += originalWidths[i];
			}

			// Step 2: Calculate max possible scale (never > 1)
			float scaleByHeight = containerHeight / GetMax(originalHeights); // so no button is taller than container
			float scaleByWidth = containerWidth / totalOriginalWidth; // so buttons fit side-by-side
			float scale = Mathf.Min(scaleByHeight, scaleByWidth, 1f); // never upscale

			// Step 3: Layout
			float totalScaledWidth = leftMargin + rightMargin + Mathf.Max(0, (rects.Length - 1) * spacing);
			for (int i = 0; i < rects.Length; i++)
			{
				totalScaledWidth += originalWidths[i] * scale;
			}

			float x = -totalScaledWidth / 2f + leftMargin;
			for (int i = 0; i < rects.Length; i++)
			{
				RectTransform rt = rects[i];
				float scaledWidth = originalWidths[i] * scale;
				float scaledHeight = originalHeights[i] * scale;
				rt.sizeDelta = new Vector2(scaledWidth, scaledHeight);
				rt.anchoredPosition = new Vector2(x + scaledWidth / 2f, 0f); // y=0 for vertical center
				x += scaledWidth + spacing;
			}
		}

		/// <summary>
		/// Scales and arranges given rects side-by-side, anchored to the top right corner and laid out from right to left.
		/// Every button keeps its aspect ratio and never exceeds its original size.
		/// </summary>
		public void ScaleAndAnchorRectsTopRightRightToLeft(
			RectTransform[] rects,
			float spacing = 0f,
			float topMargin = 10f,
			float rightMargin = 10f)
		{
			if (rects == null || rects.Length == 0)
				return;

			RectTransform container = GetComponent<RectTransform>();
			float containerWidth = container.rect.width;
			float containerHeight = container.rect.height;

			// Step 1: Get original sizes
			float[] originalWidths = new float[rects.Length];
			float[] originalHeights = new float[rects.Length];
			float totalOriginalWidth = 0;

			for (int i = 0; i < rects.Length; i++)
			{
				originalWidths[i] = rects[i].rect.width;
				originalHeights[i] = rects[i].rect.height;
				totalOriginalWidth += originalWidths[i];
			}

			// Add spacing between elements
			if (rects.Length > 1)
				totalOriginalWidth += (rects.Length - 1) * spacing;

			// Step 2: Calculate max possible scale (never > 1)
			float scaleByHeight = (containerHeight - topMargin) / GetMax(originalHeights);
			float scaleByWidth = (containerWidth - rightMargin) / totalOriginalWidth;
			float scale = Mathf.Min(scaleByHeight, scaleByWidth, 1f); // never upscale

			// Step 3: Layout from right to left, starting at top-right corner
			float x = containerWidth - rightMargin;

			for (int i = 0; i < rects.Length; i++)
			{
				RectTransform rt = rects[i];
				float scaledWidth = originalWidths[i] * scale;
				float scaledHeight = originalHeights[i] * scale;

				// Set up anchors and pivot for top-right positioning
				rt.anchorMin = new Vector2(1f, 1f);
				rt.anchorMax = new Vector2(1f, 1f);
				rt.pivot = new Vector2(1f, 1f);

				// Position from the right edge, moving left
				rt.sizeDelta = new Vector2(scaledWidth, scaledHeight);
				rt.anchoredPosition = new Vector2(-containerWidth + x, -topMargin);

				// Move left for the next element
				x -= (scaledWidth + spacing);
			}
		}

		// Helper
		private float GetMax(float[] vals)
		{
			float max = float.MinValue;
			foreach (var v in vals) if (v > max) max = v;
			return max;
		}

		private void UpdateRect()
		{
			claim.SetRect(this.face.Rect);
		}
	}
}
