using RoR2.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Dodad.XSplitscreen.Components
{
	internal class ColorConfigurator : OptionConfigurator
	{
		private static Texture2D Hue;
		private static Texture2D SaturationGradient;
		private static Texture2D AlphaGradient;
		private static Color[] ColorSlice;

		private ColorSettingsModule _colorModule;
		private RectTransform _hueGradient;
		private RectTransform _alphaGradient;
		private RectTransform _indicator;
		private Image _indicatorImage;

		private float _indicatorPosition = 0.5f;

		private Coroutine _dragIndicatorRoutine;

		public override string GetName() => "XSS_CONFIG_COLOR";

		public void Awake()
		{
			EnableCancelButton = true;
			EnableConfirmButton = true;
			NavigatorCount = 0;

			CreateUI();
		}

		/// <summary>
		/// Updates the configurator based on input.
		/// </summary>
		public override void ConfiguratorUpdate()
		{
			if (Mathf.Abs(Options.Slot.Input.LeftRightDelta) > 0.1f)
			{
				UpdateSelection(Options.Slot.Input.LeftRightDelta);
				UpdateSelectedColor();
			}
			else
			{
				/*if (Options.Slot.Input.Up)
				{
					OnNavigate(-1);
				}
				else if (Options.Slot.Input.Down)
				{
					OnNavigate(1);
				}
				else */if (Options.Slot.Input.South)
				{
					OnConfirm();
				}
				else if(Options.Slot.Input.East)
				{
					OnCancel();
				}
			}

			UpdateIndicatorPosition();
		}

		public override bool CanOpen() => true;

		public override void Open()
		{
			Options.SetMessage(null);

			SetSelectionState(0);
		}

		public override void ForceClose()
		{
			StopDragRoutine();
			SetSelectionUIVisibility(-1);
		}

		public override void OnNavigate(int direction)
		{
			CycleSelectionState(direction);
		}

		public override void OnNavigateIndex(int direction)
		{
			SetSelectionState(direction);
		}

		public override void OnCancel()
		{
			SaveAndClose();
		}

		public override void OnConfirm()
		{
			SaveAndClose();
			//CycleSelectionState(1);
		}

		public override void OnLoadProfile()
		{
			LoadColor();
		}

		private void SaveAndClose()
		{
			SetSelectionUIVisibility(-1);

			StopDragRoutine();
			OnFinished();
		}

		private void LoadColor()
		{
			_colorModule = null;

			if (Options.Slot.Profile != null)
				_colorModule = SplitScreenSettings.GetOrCreateUserModule<ColorSettingsModule>(Options.Slot.Profile.fileName);

			var color = GetColor();

			SetColor(color, false);
			var index = GetIndicatorIndexFromColor(color);
			_indicatorPosition = index / 255f;
			UpdateIndicatorPosition(_indicatorPosition);
		}

		private void SaveColor()
		{
			if (Options.Slot.Profile == null)
				return;

			SplitScreenSettings.MarkUserDirty(Options.Slot.Profile.fileName);
		}

		public Color GetColor() => _colorModule == null ? Color.white : _colorModule.Color;

		public void SetColor(Color color, bool markDirty = true)
		{
			if (_colorModule != null)
			{
				_colorModule.MainR = color.r;
				_colorModule.MainG = color.g;
				_colorModule.MainB = color.b;
				_colorModule.MainA = color.a;
			}

			Options.Slot.MainColor = color;

			if (markDirty)
				SaveColor();
		}

		private void UpdateSelection(float direction)
		{
			float step = Mathf.Sign(direction) * 0.2f * Time.deltaTime;

			_indicatorPosition = Mathf.Clamp01(_indicatorPosition + step);
		}

		private void UpdateSelectedColor()
		{
			SetColor(ColorSlice[(int) (_indicatorPosition * 255)]);
		}

		private void UpdateIndicatorPosition(float? pos = null)
		{
			pos ??= Mathf.Lerp(_indicator.anchorMin.x, _indicatorPosition, 5f * Time.deltaTime);
			
			_indicator.anchorMin = new Vector2(pos.Value, 0.4f);
			_indicator.anchorMax = new Vector2(pos.Value, 0.6f);
		}

		/// <summary>
		/// Get the indicator position by comparing the provided color to the gradient
		/// </summary>
		/// <param name="color">The gradient index or 0 if not found</param>
		/// <returns></returns>
		private int GetIndicatorIndexFromColor(Color color)
		{
			for(int e = 0; e < 256; e++)
			{
				if (ColorSlice[e] == color)
				{
					return e;
				}
			}

			return 0;
		}

		private void CycleSelectionState(int state) => SetSelectionState(state + NavigatorIndex);

		/// <summary>
		/// Updates the selection state to the desired index.
		/// </summary>
		/// <param name="index">-1 to cycle back, 1 to cycle forward</param>
		private void SetSelectionState(int index)
		{
			NavigatorIndex = Mathf.Clamp(index, 0, NavigatorCount);

			SetSelectionUIVisibility(NavigatorIndex);
		}

		/// <summary>
		/// Set the various UI visibility states based on the index
		/// </summary>
		/// <param name="index">-1 = all off, > -1 = indicator on -- 0 = color</param>
		private void SetSelectionUIVisibility(int index)
		{
			_indicator.gameObject.SetActive(index > -1);
			_hueGradient.gameObject.SetActive(index == 0);
			//_alphaGradient.gameObject.SetActive(index == 1);
		}

		private void StartDragRoutine()
		{
			StopDragRoutine();
			_dragIndicatorRoutine = StartCoroutine(DragRoutine(() =>
			{
				UpdateIndicatorPosition(_indicatorPosition);
				UpdateSelectedColor();
			},
			() =>
			{
				MPEventSystem.current.SetSelectedGameObject(null);
			}));
		}

		private void StopDragRoutine()
		{
			if (_dragIndicatorRoutine != null)
				StopCoroutine(_dragIndicatorRoutine);
		}
		private IEnumerator DragRoutine(Action update, Action cleanup)
		{
			yield return new WaitForEndOfFrame();

			while(Options.Slot.Input.MouseLeft)
			{
				_indicatorPosition = GetCursorHorizontalNormalized(_hueGradient);

				update?.Invoke();

				yield return null;
			}

			cleanup?.Invoke();
		}

		private void CreateUI()
		{
			if (Hue == null)
			{
				Hue = GenerateColorSpectrumGradient(256, 1);
				AlphaGradient = GenerateBlackToWhiteGradient(256, 1);
				ColorSlice = Hue.GetPixels(0, 0, 256, 1);
			}

			_hueGradient = new GameObject("ColorGradient", typeof(RectTransform)).GetComponent<RectTransform>();
			_hueGradient.gameObject.AddComponent<Image>().sprite = Sprite.Create(Hue, new Rect(0, 0, 256, 1), new Vector2(0.5f, 0.5f));
			_hueGradient.SetParent(transform);
			_hueGradient.localPosition = Vector3.zero;
			_hueGradient.localScale = Vector3.one;
			_hueGradient.anchorMin = new Vector2(0, 0.45f);
			_hueGradient.anchorMax = new Vector2(1, 0.55f);
			_hueGradient.sizeDelta = Vector2.zero;
			var colorButton = _hueGradient.gameObject.AddComponent<MPButton>();
			colorButton.onSelect = new UnityEngine.Events.UnityEvent();
			colorButton.onSelect.AddListener(() => StartDragRoutine());
			colorButton.allowAllEventSystems = true;
			_hueGradient.gameObject.SetActive(false);

			_indicator = new GameObject("Indicator", typeof(RectTransform)).GetComponent<RectTransform>();
			_indicatorImage = _indicator.gameObject.AddComponent<Image>();
			_indicatorImage.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), new Vector2(0.5f, 0.5f));
			_indicatorImage.raycastTarget = false;
			_indicator.SetParent(transform);
			_indicator.localPosition = Vector3.zero;
			_indicator.localScale = Vector3.one;
			_indicator.anchorMin = new Vector2(0.5f, 0.4f);
			_indicator.anchorMax = new Vector2(0.5f, 0.6f);
			_indicator.sizeDelta = new Vector2(3f, 0f);
			_indicator.gameObject.SetActive(false);
		}

		/// <summary>
		/// Generates a horizontal texture with the full color spectrum.
		/// </summary>
		/// <param name="width">Width of the texture.</param>
		/// <param name="height">Height of the texture.</param>
		/// <returns>A Texture2D with the color spectrum gradient.</returns>
		public static Texture2D GenerateColorSpectrumGradient(int width, int height)
		{
			Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

			for (int x = 0; x < width; x++)
			{
				// Calculate hue based on x position (0 to 1)
				float hue = (float) x / width;
				Color color = Color.HSVToRGB(hue, 1f, 1f);

				// Set this color for the entire column
				for (int y = 0; y < height; y++)
				{
					texture.SetPixel(x, y, color);
				}
			}

			texture.Apply();
			return texture;
		}

		/// <summary>
		/// Generates a horizontal gradient from black to white.
		/// </summary>
		/// <param name="width">Width of the texture.</param>
		/// <param name="height">Height of the texture.</param>
		/// <returns>A Texture2D with the black to white gradient.</returns>
		public static Texture2D GenerateBlackToWhiteGradient(int width, int height)
		{
			Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

			for (int x = 0; x < width; x++)
			{
				// Calculate grayscale value based on x position (0 to 1)
				float value = (float) x / width;
				Color color = new Color(value, value, value);

				// Set this color for the entire column
				for (int y = 0; y < height; y++)
				{
					texture.SetPixel(x, y, color);
				}
			}

			texture.Apply();
			return texture;
		}

		/// <summary>
		/// Returns a float in the range [0,1] indicating the normalized horizontal position of the cursor over the RectTransform.
		/// 0 = left of rect (or outside to the left)
		/// 1 = right of rect (or outside to the right)
		/// 0.5 = center
		/// Vertical position is ignored.
		/// </summary>
		public static float GetCursorHorizontalNormalized(RectTransform rectTransform, Camera uiCamera = null)
		{
			// Get mouse position in screen space
			Vector2 mouseScreenPos = Input.mousePosition;

			// Convert mouse position to local position in the RectTransform's space
			Vector2 localPoint;
			if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, mouseScreenPos, uiCamera, out localPoint))
			{
				// rectTransform.rect contains the local-space rect (centered at pivot)
				float left = rectTransform.rect.xMin;
				float right = rectTransform.rect.xMax;

				// Clamp localPoint.x between left and right
				float clampedX = Mathf.Clamp(localPoint.x, left, right);

				// Normalize: 0 at left, 1 at right
				float normalized = (clampedX - left) / (right - left);

				// If cursor is outside, forcibly return 0 or 1
				if (localPoint.x <= left)
					return 0f;
				if (localPoint.x >= right)
					return 1f;

				return normalized;
			}
			// If cannot convert, return 0 (e.g. if rectTransform not on screen)
			return 0f;
		}
	}
}
