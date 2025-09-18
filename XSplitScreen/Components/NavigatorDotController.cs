using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Dodad.XSplitscreen.Components
{
	public class NavigatorDotController : MonoBehaviour
	{
		public Action<int> OnNavigate;

		private List<Image> _dots = new();
		private GameObject _dotPrefab;
		private int _index;
		private Color _colorTarget;
		private Vector3 _scaleTarget;
		private Color _originalColor;
		private Vector3 _originalScale;

		private void Awake()
		{
			_dotPrefab = transform.Find("Dot").gameObject;
			_dotPrefab.SetActive(false);
			_originalColor = _dotPrefab.GetComponent<Image>().color;
			_originalScale = _dotPrefab.transform.localScale;
			_colorTarget = Color.white;
			_scaleTarget = new Vector3(1.8f, 1.8f, 1.8f);
			OnNavigate += SetDotIndex;
		}

		private void Update()
		{
			for(int e = 0; e < _dots.Count; e++)
			{
				if (e == _index)
				{
					_dots[e].transform.localScale = Vector3.Lerp(_dots[e].transform.localScale, _scaleTarget, Time.unscaledDeltaTime * 20f);
					_dots[e].color = Color.Lerp(_dots[e].color, _colorTarget, Time.unscaledDeltaTime * 10f);
				}
				else
				{
					_dots[e].transform.localScale = Vector3.Lerp(_dots[e].transform.localScale, _originalScale, Time.unscaledDeltaTime * 20f);
					_dots[e].color = Color.Lerp(_dots[e].color, _originalColor, Time.unscaledDeltaTime * 10f);
				}
			}
		}

		public void SetDotCount(int count)
		{
			foreach (var dot in _dots)
				Destroy(dot.gameObject);

			_dots.Clear();

			for(int e = 0; e < count; e++)
			{
				_dots.Add(Instantiate(_dotPrefab, transform).GetComponent<Image>());

				int index = e;

				_dots[e].gameObject.AddComponent<MPButton>().onClick.AddListener(() =>
					{
						OnNavigate?.Invoke(index);
					});

				_dots[e].gameObject.GetComponent<MPButton>().allowAllEventSystems = true;
				_dots[e].gameObject.SetActive(true);
			}

			SetDotIndex(0);
		}

		public void SetDotIndex(int index)
		{
			_index = index;
		}
	}
}
