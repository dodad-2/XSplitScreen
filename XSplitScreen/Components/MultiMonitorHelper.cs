using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DoDad.XSplitScreen.Components
{
    class MultiMonitorHelper : MonoBehaviour
    {
        #region Variables
        public int targetDisplay = 0;
        #endregion

        #region Region
        void Update()
        {
            transform.GetComponent<Canvas>().targetDisplay = targetDisplay;
			//Log.LogOutput($"Setting target display to {targetDisplay}"); // 4.0.0 rewrite 9-12-24
			Destroy(this);
        }
        #endregion
    }
}
