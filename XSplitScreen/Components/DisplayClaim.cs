using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Dodad.XSplitscreen.Components
{
	public class DisplayClaim : MonoBehaviour
	{
		public bool IsClaimed => _configurator != null;

		public Rect ScreenRect;

		private AssignmentConfigurator _configurator;
		private Image _bgImage;
		private Color _originalColor;

		private void Awake()
		{
			_bgImage = GetComponent<Image>();
			_originalColor = _bgImage.color;
		}

		private void OnDestroy()
		{
			if(_configurator != null)
				_configurator.ResetClaim();
		}

		internal void SetRect(Rect rect) => ScreenRect = rect;

		public void Claim(AssignmentConfigurator slot)
		{
			if (slot == null)
				ResetClaim();
			else
				AssignClaim(slot);
		}

		private void AssignClaim(AssignmentConfigurator slot)
		{
			_configurator = slot;
		}

		private void ResetClaim()
		{
			_configurator = null;
		}

		private void Update()
		{
			if (_configurator == null)
				_bgImage.color = Color.Lerp(_bgImage.color, _originalColor, 20f * Time.unscaledDeltaTime);
			else
				_bgImage.color = Color.Lerp(_bgImage.color, _configurator.Options.Slot.MainColor, 20f * Time.unscaledDeltaTime);
		}
	}
}
