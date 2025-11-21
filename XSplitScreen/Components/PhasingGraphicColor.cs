using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Dodad.XSplitscreen.Components
{
	public class PhasingGraphicColor : MonoBehaviour
	{
		public Graphic targetGraphic;
		public List<Color> phaseColors = new List<Color> 
		{
			Color.yellow, Color.red,
		};

		public float phaseSpeed = 5f;
		public float phaseOffset = 0f;

		private float phasePosition = 0f;
		public int direction = 1;

		void Start()
		{
			if(targetGraphic == null)
				targetGraphic = GetComponent<Graphic>();

			if (targetGraphic == null)
			{
				Log.Print($"PhasingImageColor: Missing graphic");
				Destroy(this);
			}

			phasePosition = phaseOffset * (phaseColors.Count - 1);
		}

		void Update()
		{
			if (targetGraphic == null || phaseColors == null || phaseColors.Count < 2) return;

			// Advance phasePosition based on speed and direction
			phasePosition += direction * phaseSpeed * Time.deltaTime;

			// Ping-pong phasePosition between 0 and phaseColors.Count - 1
			if (phasePosition >= phaseColors.Count - 1)
			{
				phasePosition = phaseColors.Count - 1;
				direction = -1;
			}
			else if (phasePosition <= 0f)
			{
				phasePosition = 0f;
				direction = 1;
			}

			int idxA = Mathf.FloorToInt(phasePosition);
			int idxB = Mathf.Min(idxA + 1, phaseColors.Count - 1);
			float lerpT = phasePosition - idxA;

			targetGraphic.color = Color.Lerp(phaseColors[idxA], phaseColors[idxB], lerpT);
		}
	}
}