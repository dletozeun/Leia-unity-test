﻿using UnityEngine;
using UnityEditor;

namespace LeiaLoft
{
    /// <summary>
    /// Custom Editor/Inspector for user interactions with LeiaMediaRecorder
    /// </summary>
    [CustomEditor(typeof(LeiaLoft.LeiaMediaRecorder))]
    public class LeiaMediaRecorderEditor : UnityEditor.Editor
    {
        LeiaLoft.LeiaMediaRecorder lmr;

        SerializedProperty lmr_recordingCondition;
        SerializedProperty lmr_recordingFormat;

        SerializedProperty lmr_frameRate;

        void OnEnable()
        {
            lmr = (LeiaLoft.LeiaMediaRecorder)target;

            lmr_recordingCondition = serializedObject.FindProperty("_recordingCondition");
            lmr_recordingFormat = serializedObject.FindProperty("_recordingFormat");
            lmr_frameRate = serializedObject.FindProperty("_frameRate");
        }

        public override void OnInspectorGUI()
        {
            // User can begin "recording" in editor, but LateUpdate() will only be called during play
            EditorGUILayout.HelpBox("Recording will be saved at Assets/StreamingAssets after finishing capture and then unfocusing and refocusing Unity as an app", MessageType.Info);
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Recording is only recommended during editor play mode", MessageType.Info);
            }

#if !UNITY_2017_3_OR_NEWER
            EditorGUILayout.HelpBox("Use Unity 2017.3+ to record video", MessageType.Warning);
#endif
            EditorGUILayout.PropertyField(lmr_recordingCondition);
            EditorGUILayout.PropertyField(lmr_recordingFormat);
            EditorGUILayout.PropertyField(lmr_frameRate);

            // since we convert value to string, we do not have to worry about pre-2017.3 vs post-2017.3 issues
            string condition = ((LeiaLoft.LeiaMediaRecorder.RecordingCondition)lmr_recordingCondition.enumValueIndex).ToString();

            if (lmr != null && lmr_recordingFormat.enumValueIndex == -1)
            {
                Debug.LogWarningFormat("Project was rolled back to version predating MediaEncoder. Change {0}.LeiaMediaRecorder.recordingCondition to be png or jpg", lmr.transform.name);
            }
            if (lmr != null && condition.Equals("frame"))
            {
                UndoableInputFieldUtils.ImmediateFloatField(
                    () => { return lmr.startTime; },
                    (float f) => { lmr.startTime = f; },
                    "Start time (sec)"
                    );
                UndoableInputFieldUtils.ImmediateFloatField(
                    () => { return lmr.endTime; },
                    (float f) => { lmr.endTime = f; },
                    "End time (sec)"
                    );
            }

            // inactive: offer to start recording
            if (lmr != null && !lmr.GetActive() && condition.Equals("button_click") && GUILayout.Button("Start recording"))
            {
                lmr.BeginRecording();
            }
            // active: offer to stop recording
            if (lmr != null && lmr.GetActive() && condition.Equals("button_click") && GUILayout.Button("Stop recording"))
            {
                lmr.EndRecording();
            }

            serializedObject.ApplyModifiedProperties();
        }

    }
}