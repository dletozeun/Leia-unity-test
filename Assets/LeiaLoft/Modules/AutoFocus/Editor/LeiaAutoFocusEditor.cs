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
	[UnityEditor.CustomEditor(typeof(LeiaAutoFocus))]
	public class LeiaAutoFocusEditor : UnityEditor.Editor
	{
		private LeiaAutoFocus _controller;

		void OnEnable()
		{
			if (_controller == null)
			{
				_controller = (LeiaAutoFocus)target;
			}
		}

		public override void OnInspectorGUI()
		{
			if (!_controller.enabled)
			{
				return;
			}

			ShowFocusSmoothing();
			ShowFocalOffset();
			ShowTrackingTypes();
		}

		private void ShowLookAtTarget()
		{
			bool lookAt = _controller.LookAtTarget;

			UndoableInputFieldUtils.BoolField (() => lookAt,
				v => {
					_controller.LookAtTarget = v;
				}, "Look at Target");
		}

		private void ShowSphereRadius()
		{
			UndoableInputFieldUtils.ImmediateFloatField(() => _controller.SphereRadius, v => _controller.SphereRadius = v, "Sphere Radius", _controller);
		}

		private void ShowFocalOffset()
		{
			UndoableInputFieldUtils.ImmediateFloatField(() => _controller.FocusOffset, v => _controller.FocusOffset = v, "Focus Offset", _controller);
		}

		private void ShowFocusSmoothing()
		{
			UndoableInputFieldUtils.ImmediateFloatField(() => _controller.FocusSpeed, v => _controller.FocusSpeed = v, "Focus Speed", _controller);
		}

		private void ShowMaxFocusDistance()
		{
			UndoableInputFieldUtils.ImmediateFloatField(() => _controller.MaxFocusDistance, v => _controller.MaxFocusDistance = v, "Max Focus Distance", _controller);
		}

		private void ShowTrackingTypes()
		{
			UndoableInputFieldUtils.EnumField(() => _controller.TrackingMode, v => _controller.TrackingMode = (LeiaAutoFocus.TrackingModes) v, "Tracking Mode", _controller);

			if (_controller.TrackingMode == LeiaAutoFocus.TrackingModes.Target)
			{
				EditorGUI.indentLevel = 1;
				_controller.Target = (GameObject)EditorGUILayout.ObjectField("Target", _controller.Target,  typeof(GameObject), true);
				ShowLookAtTarget();
				EditorGUI.indentLevel = 0;
			} 
			else if(_controller.TrackingMode == LeiaAutoFocus.TrackingModes.Raycast)
			{
				EditorGUI.indentLevel = 1;
				ShowRaycastFields();
				ShowMaxFocusDistance();
				EditorGUI.indentLevel = 0;
			}
		}
		
		private void ShowRaycastFields()
		{
			UndoableInputFieldUtils.LayerField(() => _controller.TargetCollisionLayer, v => _controller.TargetCollisionLayer = v, "Raycast Layer", _controller);
			UndoableInputFieldUtils.EnumField(() => _controller.RaycastType, v => _controller.RaycastType = (LeiaAutoFocus.RaycastTypes) v, "Raycast Type", _controller) ;

			if(_controller.RaycastType == LeiaAutoFocus.RaycastTypes.Sphere)
			{
				EditorGUI.indentLevel = 2;
				ShowSphereRadius();
				EditorGUI.indentLevel = 1;
			}
		}
		
	}
}
