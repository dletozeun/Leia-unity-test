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
    [System.Serializable]
    public struct LeiaCameraData
    {
        public float baseline { get; set; }
        public float screenHalfHeight { get; set; }
        public float screenHalfWidth { get; set; }
        public float baselineScaling { get; set; }
    }

    [System.Serializable]
    public struct LeiaBoundsData
    {
        public Vector3[] screen { get; set; }
        public Vector3[] north { get; set; }
        public Vector3[] south { get; set; }
        public Vector3[] top { get; set; }
        public Vector3[] bottom { get; set; }
        public Vector3[] east { get; set; }
        public Vector3[] west { get; set; }
    }

    public static class LeiaCameraUtils
    {
    public static LeiaCameraData ComputeLeiaCamera(Camera camera, float convergenceDistance, float baselineScaling, DisplayConfig displayConfig)
    {
		LeiaCameraData leiaCameraData = new LeiaCameraData();
		displayConfig.UserOrientationIsLandscape = camera.pixelWidth > camera.pixelHeight;
		
		float f = displayConfig.UserViewResolution.y / 2f / Mathf.Tan(camera.fieldOfView * Mathf.PI / 360f);
		leiaCameraData.baseline         = displayConfig.SystemDisparityPixels * baselineScaling * convergenceDistance / f ;
		leiaCameraData.screenHalfHeight = convergenceDistance * Mathf.Tan(camera.fieldOfView * Mathf.PI / 360.0f);
		leiaCameraData.screenHalfWidth	= camera.aspect * leiaCameraData.screenHalfHeight;
		leiaCameraData.baselineScaling  = baselineScaling;

		return leiaCameraData;
    }

        public static LeiaBoundsData ComputeLeiaBounds(Camera camera, LeiaCameraData leiaCamera, float convergenceDistance, Vector2 cameraShift, DisplayConfig displayConfig)
        {
      LeiaBoundsData leiaBounds = new LeiaBoundsData();
      var localToWorldMatrix = camera.transform.localToWorldMatrix;

      localToWorldMatrix.SetColumn(0, localToWorldMatrix.GetColumn(0).normalized);
      localToWorldMatrix.SetColumn(1, localToWorldMatrix.GetColumn(1).normalized);
      localToWorldMatrix.SetColumn(2, localToWorldMatrix.GetColumn(2).normalized);

      if (camera.orthographic)
      {

        // assumes baseline = (baseline scaling) * (width of view in world units) * (system disparity in pixels) * (convergence distance) / (view width in pixels)

        float halfSizeY = camera.orthographicSize;
        float halfSizeX = halfSizeY * camera.aspect;


        Vector3 screenTopLeft     = localToWorldMatrix.MultiplyPoint (new Vector3 (-halfSizeX,  halfSizeY, convergenceDistance));
        Vector3 screenTopRight    = localToWorldMatrix.MultiplyPoint (new Vector3 ( halfSizeX,  halfSizeY, convergenceDistance));
        Vector3 screenBottomLeft  = localToWorldMatrix.MultiplyPoint (new Vector3 (-halfSizeX, -halfSizeY, convergenceDistance));
        Vector3 screenBottomRight = localToWorldMatrix.MultiplyPoint (new Vector3 ( halfSizeX, -halfSizeY, convergenceDistance));


        float negativeSystemDisparityZ = convergenceDistance - 1.0f / leiaCamera.baselineScaling;

        Vector3 nearTopLeft     = localToWorldMatrix.MultiplyPoint (new Vector3 (-halfSizeX,  halfSizeY, negativeSystemDisparityZ));
        Vector3 nearTopRight    = localToWorldMatrix.MultiplyPoint (new Vector3 ( halfSizeX,  halfSizeY, negativeSystemDisparityZ));
        Vector3 nearBottomLeft  = localToWorldMatrix.MultiplyPoint (new Vector3 (-halfSizeX, -halfSizeY, negativeSystemDisparityZ));
        Vector3 nearBottomRight = localToWorldMatrix.MultiplyPoint (new Vector3 ( halfSizeX, -halfSizeY, negativeSystemDisparityZ));


        float positiveSystemDisparityZ = convergenceDistance + 1.0f / leiaCamera.baselineScaling;

        Vector3 farTopLeft     = localToWorldMatrix.MultiplyPoint (new Vector3 (-halfSizeX,  halfSizeY, positiveSystemDisparityZ));
        Vector3 farTopRight    = localToWorldMatrix.MultiplyPoint (new Vector3 ( halfSizeX,  halfSizeY, positiveSystemDisparityZ));
        Vector3 farBottomLeft  = localToWorldMatrix.MultiplyPoint (new Vector3 (-halfSizeX, -halfSizeY, positiveSystemDisparityZ));
        Vector3 farBottomRight = localToWorldMatrix.MultiplyPoint (new Vector3 ( halfSizeX, -halfSizeY, positiveSystemDisparityZ));


        leiaBounds.screen = new [] { screenTopLeft,  screenTopRight,  screenBottomRight, screenBottomLeft };
        leiaBounds.south  = new [] { nearTopLeft,    nearTopRight,    nearBottomRight,   nearBottomLeft   };
        leiaBounds.north  = new [] { farTopLeft,     farTopRight,     farBottomRight,    farBottomLeft    };
        leiaBounds.top    = new [] { nearTopLeft,    nearTopRight,    farTopRight,       farTopLeft       };
        leiaBounds.bottom = new [] { nearBottomLeft, nearBottomRight, farBottomRight,    farBottomLeft    };
        leiaBounds.east   = new [] { nearTopRight,   nearBottomRight, farBottomRight,    farTopRight      };
        leiaBounds.west   = new [] { nearTopLeft,    nearBottomLeft,  farBottomLeft,     farTopLeft       };

      }

      else
      {

        cameraShift = leiaCamera.baseline * cameraShift;

        Vector3 screenTopLeft     = localToWorldMatrix.MultiplyPoint(new Vector3(-leiaCamera.screenHalfWidth, leiaCamera.screenHalfHeight, convergenceDistance));
        Vector3 screenTopRight    = localToWorldMatrix.MultiplyPoint(new Vector3( leiaCamera.screenHalfWidth, leiaCamera.screenHalfHeight, convergenceDistance));
        Vector3 screenBottomLeft  = localToWorldMatrix.MultiplyPoint(new Vector3(-leiaCamera.screenHalfWidth,-leiaCamera.screenHalfHeight, convergenceDistance));
        Vector3 screenBottomRight = localToWorldMatrix.MultiplyPoint(new Vector3( leiaCamera.screenHalfWidth,-leiaCamera.screenHalfHeight, convergenceDistance));

        float nearPlaneZ = (leiaCamera.baselineScaling * convergenceDistance) / (leiaCamera.baselineScaling + 1f);
        float nearRatio  = nearPlaneZ / convergenceDistance;
        float nearShiftRatio = 1f - nearRatio;

        Bounds localNearPlaneBounds = new Bounds(
          new Vector3(nearShiftRatio * cameraShift.x, nearShiftRatio * cameraShift.y, nearPlaneZ),
          new Vector3(leiaCamera.screenHalfWidth * nearRatio * 2, leiaCamera.screenHalfHeight * nearRatio * 2, 0));

        Vector3 nearTopLeft     = localToWorldMatrix.MultiplyPoint(new Vector3(localNearPlaneBounds.min.x, localNearPlaneBounds.max.y, localNearPlaneBounds.center.z));
        Vector3 nearTopRight    = localToWorldMatrix.MultiplyPoint(new Vector3(localNearPlaneBounds.max.x, localNearPlaneBounds.max.y, localNearPlaneBounds.center.z));
        Vector3 nearBottomLeft  = localToWorldMatrix.MultiplyPoint(new Vector3(localNearPlaneBounds.min.x, localNearPlaneBounds.min.y, localNearPlaneBounds.center.z));
        Vector3 nearBottomRight = localToWorldMatrix.MultiplyPoint(new Vector3(localNearPlaneBounds.max.x, localNearPlaneBounds.min.y, localNearPlaneBounds.center.z));

        float farPlaneZ = (leiaCamera.baselineScaling * convergenceDistance) / (leiaCamera.baselineScaling - 1f);
        farPlaneZ = 1f / Mathf.Max(1f / farPlaneZ, 1e-5f);

        float farRatio  = farPlaneZ / convergenceDistance;
        float farShiftRatio = 1f - farRatio;

        Bounds localFarPlaneBounds = new Bounds(
          new Vector3(farShiftRatio * cameraShift.x, farShiftRatio * cameraShift.y, farPlaneZ),
          new Vector3(leiaCamera.screenHalfWidth * farRatio * 2, leiaCamera.screenHalfHeight  * farRatio * 2, 0));

        Vector3 farTopLeft      = localToWorldMatrix.MultiplyPoint(new Vector3(localFarPlaneBounds.min.x, localFarPlaneBounds.max.y, localFarPlaneBounds.center.z));
        Vector3 farTopRight     = localToWorldMatrix.MultiplyPoint(new Vector3(localFarPlaneBounds.max.x, localFarPlaneBounds.max.y, localFarPlaneBounds.center.z));
        Vector3 farBottomLeft   = localToWorldMatrix.MultiplyPoint(new Vector3(localFarPlaneBounds.min.x, localFarPlaneBounds.min.y, localFarPlaneBounds.center.z));
        Vector3 farBottomRight  = localToWorldMatrix.MultiplyPoint(new Vector3(localFarPlaneBounds.max.x, localFarPlaneBounds.min.y, localFarPlaneBounds.center.z));

				leiaBounds.screen = new [] { screenTopLeft,  screenTopRight,  screenBottomRight, screenBottomLeft };
				leiaBounds.south  = new [] { nearTopLeft,    nearTopRight,    nearBottomRight,   nearBottomLeft   };
				leiaBounds.north  = new [] { farTopLeft,     farTopRight,     farBottomRight,    farBottomLeft    };
				leiaBounds.top    = new [] { nearTopLeft,    nearTopRight,    farTopRight,       farTopLeft       };
				leiaBounds.bottom = new [] { nearBottomLeft, nearBottomRight, farBottomRight,    farBottomLeft    };
				leiaBounds.east   = new [] { nearTopRight,   nearBottomRight, farBottomRight,    farTopRight      };
				leiaBounds.west   = new [] { nearTopLeft,    nearBottomLeft,  farBottomLeft,     farTopLeft       };
      }

              return leiaBounds;
        }

        /// <summary>
        /// Performs a raycast from the given LeiaCamera
        /// </summary>
        /// <param name="leiaCam">A LeiaCamera with a Camera component and Transform</param>
        /// <param name="position">A screenPosition</param>
        /// <returns>A ray from the camera's world position, that passes through the screenPosition</returns>
        public static Ray ScreenPointToRay(LeiaCamera leiaCam, Vector3 screenPosition)
        {
            Camera cam = leiaCam.Camera;
            bool prev_state = cam.enabled;
            cam.enabled = true;
            Ray r = cam.ScreenPointToRay(screenPosition);
            cam.enabled = prev_state;
            return (r);
        }

        [System.Obsolete("Pass in a Vector3 position to LeiaCameraUtil.ScreenPointToRay")]
        public static Ray ScreenPointToRay(LeiaCamera leiaCam, Vector2 position)
        {
            return ScreenPointToRay(leiaCam, (Vector3) position);
        }
	    
    	/// <summary>
    	/// Uses first LeiaCamera instance to form a ray from screen position
    	/// </summary>
        [System.Obsolete("Pass in a LeiaCamera to LeiaCameraUtil.ScreenPointToRay")]
        public static Ray ScreenPointToRay(Vector2 position)
        {
            Camera c = LeiaCamera.Instance.Camera;
            return ScreenPointToRay(LeiaCamera.Instance, position);   
        }
    }
}
