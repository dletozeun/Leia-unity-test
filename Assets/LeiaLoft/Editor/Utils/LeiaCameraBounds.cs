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
using UnityEditor;

namespace LeiaLoft
{
    public static class LeiaCameraBounds
    {
        private static readonly Color _leiaScreenPlaneColor = new Color(20 / 255.0f, 100 / 255.0f, 160 / 255.0f, 0.2f);
        private static readonly Color _leiaScreenWireColor = new Color(35 / 255.0f, 200 / 255.0f, 1.0f, 0.6f);
        private static readonly Color _leiaBoundsPlaneColor = new Color(1.0f, 1.0f, 1.0f, 0.1f);
        private static readonly Color _leiaBoundsWireColor = new Color(1.0f, 1.0f, 1.0f, 0.2f);

        public static void DrawCameraBounds(LeiaCamera controller, GizmoType gizmoType)
        {

#if UNITY_4_6 || UNITY_4_7 || UNITY_5_0_0 || UNITY_5_0_1
            GizmoType notSelected = GizmoType.NotSelected;
#elif UNITY_5_0_2 || UNITY_5_0_3 || UNITY_5_0_4
            GizmoType notSelected = GizmoType.NotInSelectionHierarchy;
#else
            GizmoType notSelected = GizmoType.NonSelected;
#endif
            if ((gizmoType & notSelected) != 0 && controller.DrawCameraBounds == false)
            {
                return;
            }

            var camera = controller.GetComponent<Camera>();

            if (camera == null)
            {
                return;
            }

			DisplayConfig displayConfig;

            if (Application.isPlaying)
            {
                displayConfig = LeiaDisplay.Instance.GetDisplayConfig();
            }
            else
            {
                displayConfig = Object.FindObjectOfType<LeiaDisplay>().GetDisplayConfig();
            }

            LeiaCameraData leiaCameraData = LeiaCameraUtils.ComputeLeiaCamera(
                camera,
                controller.ConvergenceDistance,
                controller.BaselineScaling,
				displayConfig);

            LeiaBoundsData leiaBoundsData = LeiaCameraUtils.ComputeLeiaBounds(camera, leiaCameraData, controller.ConvergenceDistance, controller.CameraShift, displayConfig);

            if (((gizmoType & notSelected) != 0 && controller.DrawCameraBounds) || (gizmoType & GizmoType.Selected) != 0)
            {
                // draw convergence plane in editor play mode
                Handles.DrawSolidRectangleWithOutline(leiaBoundsData.screen, _leiaScreenPlaneColor, _leiaScreenWireColor);
            }

            if (((gizmoType & notSelected) != 0 && controller.DrawCameraBounds) || (gizmoType & GizmoType.Selected) != 0)
            {
                // draw frustum outline in white
                Handles.DrawSolidRectangleWithOutline(leiaBoundsData.north, _leiaBoundsPlaneColor, _leiaBoundsWireColor);
                Handles.DrawSolidRectangleWithOutline(leiaBoundsData.south, _leiaBoundsPlaneColor, _leiaBoundsWireColor);
                Handles.DrawSolidRectangleWithOutline(leiaBoundsData.east, _leiaBoundsPlaneColor, _leiaBoundsWireColor);
                Handles.DrawSolidRectangleWithOutline(leiaBoundsData.west, _leiaBoundsPlaneColor, _leiaBoundsWireColor);
                Handles.DrawSolidRectangleWithOutline(leiaBoundsData.top, _leiaBoundsPlaneColor, _leiaBoundsWireColor);
                Handles.DrawSolidRectangleWithOutline(leiaBoundsData.bottom, _leiaBoundsPlaneColor, _leiaBoundsWireColor);
            }
        }

    }
}