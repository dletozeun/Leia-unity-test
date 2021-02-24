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
    /// ILeiaState implementation for 2D (1 view) mode
    /// </summary>
    public class TwoDimLeiaStateTemplate : AbstractLeiaStateTemplate
    {
        public static string OpaqueShaderName { get { return "LeiaLoft/TwoDimensional"; } } 
        public static string TransparentShaderName { get { return "LeiaLoft/TwoDimensional_Blending"; } }

		public TwoDimLeiaStateTemplate(DisplayConfig displayConfig) : base(displayConfig)
        {
            // this method was left blank intentionally
        }

        public override void GetFrameBufferSize(out int width, out int height)
        {
			width = _displayConfig.UserPanelResolution.x;
			height = _displayConfig.UserPanelResolution.y;
        }

        public override void GetTileSize(out int tileWidth, out int tileHeight)
        {
            GetFrameBufferSize(out tileWidth, out tileHeight);
        }

		public override void UpdateState(LeiaStateDecorators decorators, ILeiaDevice device)
        {
            // this method was left blank intentionally
        }

        public override void UpdateViews(LeiaCamera leiaCamera)
        {
            _displayConfig.UserNumViews = new XyPair<int>(1, 1);
			base.UpdateViews(leiaCamera);

			float near = Mathf.Max(1.0e-5f, leiaCamera.NearClipPlane);
			float far = Mathf.Max(near, leiaCamera.FarClipPlane);
            
			var cam = leiaCamera.GetView(0);

            if (cam.IsCameraNull)
            {
                return;
            }

            cam.Camera.ResetProjectionMatrix();

            cam.Position = new Vector3(0.0f, 0.0f, 0.0f);
            cam.NearClipPlane = near;
            cam.FarClipPlane = far;
        }
    }
}