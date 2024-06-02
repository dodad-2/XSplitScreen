using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DoDad.XSplitScreen.Components
{
    class MultiMonitorCamera : MonoBehaviour
    {
        #region Variables
        
        #endregion

        #region Unity Methods
        void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if(MultiMonitorCameraManager.renderTexture != null && MultiMonitorCameraManager.renderTexture.IsCreated())
                Graphics.Blit(MultiMonitorCameraManager.renderTexture, dest);
        }
        #endregion
    }
}
