using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace DoDad.XSplitScreen.Components
{
    class ColorShifter : MonoBehaviour
    {
        #region Variables
        public List<Color[]> colorCycles = new List<Color[]>();

        public bool cycle = true;

        public int cycleIndex = -1;

        public float cycleSpeed = 1f;

        public Image target;

        private int colorIndex = 0;

        private float timer = 0;
        #endregion

        #region Unity Methods
        void LateUpdate()
        {
            if (!cycle)
                return;

            timer += Time.unscaledDeltaTime;

            if (timer > cycleSpeed)
                timer = 0;

            if (cycleIndex > -1 && cycleIndex < colorCycles.Count && colorCycles[cycleIndex].Length > 1 && target != null)
            {
                var currentCycle = colorCycles[cycleIndex];

                if (timer == 0)
                    colorIndex++;

                if (colorIndex == currentCycle.Length || colorIndex >= colorCycles[cycleIndex].Length || colorIndex < 0)
                    colorIndex = 0;

                target.color = Color.Lerp(target.color, currentCycle[colorIndex], timer / cycleSpeed);
            }
        }
        #endregion

        #region Static Methods

        #endregion

        #region Helpers

        #endregion
    }
}
