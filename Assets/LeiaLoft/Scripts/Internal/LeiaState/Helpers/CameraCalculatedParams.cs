/****************************************************************
*
* Copyright 2019 © Leia Inc.  All rights reserved.
*
* NOTICE:  All information contained herein is, and remains
* the property of Leia Inc. and its suppliers, if any.  The
* intellectual and technical concepts contained herein are
* proprietary to Leia Inc. and its suppliers and may be covered
* by U.S. and Foreign Patents, patents in process, and are
* protected by trade secret or copyright law.  Dissemination of
* this information or reproduction of this materials strictly
* forbidden unless prior written permission is obtained from
* Leia Inc.
*
****************************************************************
*/
using UnityEngine;

namespace LeiaLoft
{
    /// <summary>
    /// Wrapper for repeating routine of getting parameters from Settings and doing calculations
    /// </summary>
    class CameraCalculatedParams
    {
        public float ScreenHalfHeight { get; private set; }
        public float ScreenHalfWidth { get; private set; }
        public float EmissionRescalingFactor { get; private set; }

        public CameraCalculatedParams(LeiaCamera properties, DisplayConfig displayConfig)
		{
			ScreenHalfHeight = properties.ConvergenceDistance * Mathf.Tan(properties.FieldOfView * Mathf.PI / 360.0f);
			ScreenHalfWidth = displayConfig.UserAspectRatio * ScreenHalfHeight;
			float f = (displayConfig.ViewResolution.y / displayConfig.ResolutionScale) / 2f / Mathf.Tan(properties.FieldOfView * Mathf.PI / 360f);
            EmissionRescalingFactor = displayConfig.SystemDisparityPixels * properties.BaselineScaling * properties.ConvergenceDistance / f;

			/*Debug.Log ("CameraCalculatedParams"
				+ " UserPanel.x: " + displayConfig.UserPanelResolution.x
				+ " UserPanel.y: " + displayConfig.UserPanelResolution.y
				+ " UserView.x: " + displayConfig.UserViewResolution.x 
				+ " UserView.y: " + displayConfig.UserViewResolution.y
				+ " Screen.width: " + Screen.width
				+ " Screen.height: " + Screen.height
				+ " properties.FieldOfView: " + properties.FieldOfView
				+ " ScreenHalfWidth: " + ScreenHalfWidth
				+ " ScreenHalfHeight: " + ScreenHalfHeight
				+ " baseline: " + EmissionRescalingFactor);*/
        }
    }
}
