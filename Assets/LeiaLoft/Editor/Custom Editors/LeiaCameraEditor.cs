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
    /// <summary>
    /// Extends LeiaCamera inspector with additional controls for calibration, emulation.
    /// </summary>
    [UnityEditor.CustomEditor(typeof(LeiaCamera))]
    [CanEditMultipleObjects]
    public class LeiaCameraEditor : UnityEditor.Editor
    {
        private const string ConvergenceDistanceFieldLabel = "Convergence Distance";
        private const string BaselineScalingFieldLabel = "Baseline Scaling";
		private const string DrawCameraBoundsFieldLabel = "Always Show Camera Bounds in Scene Editor";
        private const string UpdateEffectsButtonLabel = "Update Effects";
        private const string UpdateEffectsHelpText = "Use to update all the effects manually (for example if you changed some parameter in an effect and want it to be applied to all the views). LeiaCamera code has relevant method: UpdateEffects.";
        private const string NoEffectsControllerHelpText = "If you want to use post effects with LeiaCamera, LeiaDisplay must have `Separate Tiles` On.";

        private LeiaCamera _controller;

        void Awake()
        {
            var displays = Resources.FindObjectsOfTypeAll<LeiaDisplay>();
            if (displays.Length == 0)
            {
                LogUtil.Trace("Creating new " + typeof(LeiaDisplay).Name + " gameObject");
                var go = new GameObject().AddComponent<LeiaDisplay>();
                if (go != null)
                {
                    go.name = go.ObjectName;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            if (_controller == null)
            {
                _controller = (LeiaCamera)target;
            }

            if (!_controller.enabled)
            {
                return;
            }

            EditorGUI.BeginChangeCheck();

            // allow multi-object editing
            // display object properties in sequence when multiple objects selected
            for (int i = 0; i < targets.Length; i++)
            {
                _controller = (LeiaCamera)targets[i];

                if (targets.Length > 1)
                {
                    EditorGUILayout.Separator();
                    EditorGUILayout.ObjectField(_controller, typeof(LeiaCamera), true);
                }

                UndoableInputFieldUtils.ImmediateFloatField(() => _controller.ConvergenceDistance, v => _controller.ConvergenceDistance = v, ConvergenceDistanceFieldLabel, _controller);
                UndoableInputFieldUtils.ImmediateFloatField(() => _controller.BaselineScaling, v => _controller.BaselineScaling = v, BaselineScalingFieldLabel, _controller);
			    UndoableInputFieldUtils.BoolField(() => _controller.DrawCameraBounds, v => _controller.DrawCameraBounds = v, DrawCameraBoundsFieldLabel, _controller);

                EditorGUILayout.Separator();

                if (EditorApplication.isPlaying)
                {
                    if (!LeiaDisplay.InstanceIsNull)
                    {
                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button(UpdateEffectsButtonLabel))
                        {
                            _controller.UpdateEffects();
                        }

                        EditorGUILayout.HelpBox(UpdateEffectsHelpText, MessageType.Info);
                        EditorGUILayout.EndHorizontal();
                    }
                    else
                    {
                        EditorGUILayout.HelpBox(NoEffectsControllerHelpText, MessageType.Info);
                    }
                }

                if (targets.Length > 1)
                {
                    EditorGUILayout.Separator();
                }
            }

            if(EditorGUI.EndChangeCheck())
            {
                foreach (Object obj in targets)
                {
                    EditorUtility.SetDirty(obj);
                }
            }
            
        }

#if UNITY_4_6 || UNITY_4_7 || UNITY_5_0_0 || UNITY_5_0_1
        [DrawGizmo(GizmoType.Selected | GizmoType.NotSelected)]
#elif UNITY_5_0_2 || UNITY_5_0_3 || UNITY_5_0_4
        [DrawGizmo(GizmoType.Selected | GizmoType.NotInSelectionHierarchy)]
#else
        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
#endif
        private static void OnDrawLeiaBounds(LeiaCamera controller, GizmoType gizmoType) 
        {
            LeiaCameraBounds.DrawCameraBounds(controller, gizmoType);
        }
    }
}