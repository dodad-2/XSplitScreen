using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Dodad.XSplitscreen.Components
{
	public class CreditRoller : MonoBehaviour
	{
		public string Title;
		public string Content;
	
		public RectTransform Rect => _rect;

		private PhasingGraphicColor _colorPhaser;
		private TextMeshProUGUI _titleText;
		private TextMeshProUGUI _contentText;
		private RectTransform _rect;
		private List<Color> _colors;

		internal void Awake()
		{
			// Title Text
			_titleText = UIHelper.GetPrefab(UIHelper.EUIPrefabIndex.SimpleText).GetComponent<TextMeshProUGUI>();
			_titleText.alignment = TextAlignmentOptions.Right;
			_titleText.enableWordWrapping = false;
			Destroy(_titleText.GetComponent<RoR2.UI.LanguageTextMeshController>());
			_titleText.transform.SetParent(transform.Find("Left"), false);
			_titleText.transform.localScale = Vector3.one;
			_titleText.gameObject.SetActive(true);

			// Content Text
			_contentText = UIHelper.GetPrefab(UIHelper.EUIPrefabIndex.SimpleText).GetComponent<TextMeshProUGUI>();
			_contentText.alignment = TextAlignmentOptions.Left;
			_contentText.enableWordWrapping = false;
			Destroy(_contentText.GetComponent<RoR2.UI.LanguageTextMeshController>());
			_contentText.transform.SetParent(transform.Find("Right"), false);
			_contentText.transform.localScale = Vector3.one;
			_contentText.gameObject.SetActive(true);

			_rect = GetComponent<RectTransform>();

			_colorPhaser = gameObject.AddComponent<PhasingGraphicColor>();
			_colorPhaser.targetGraphic = _titleText;
		}

		internal void Start()
		{
			_titleText.text = Title;
			_contentText.text = Content;
			_colorPhaser.phaseColors = _colors;
		}

		public void SetColors(List<Color> colors) => _colors = colors;
	}
}
