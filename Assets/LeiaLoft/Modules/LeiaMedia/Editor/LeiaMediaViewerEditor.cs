﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace LeiaLoft.Editor
{
    [CustomEditor(typeof(LeiaLoft.LeiaMediaViewer))]
    public class LeiaMediaViewerEditor : UnityEditor.Editor
    {
        LeiaLoft.LeiaMediaViewer lmv;

        SerializedProperty leiaMediaVideoURL;
        SerializedProperty leiaMediaVideoClip;
        SerializedProperty leiaMediaTexture;
        SerializedProperty automaticAspectRatio;
        SerializedProperty maxScaleBeforeAspectRatio;

        private enum propertyStringIDs
        {
            LeiaMediaCol = 0,
            LeiaMediaRow = 1
        }
        // layout: ID = row. Row[0] = field name on LeiaMediaViewer. Row[1] = public-facing label for that property
        private readonly string[][] propertyStringResources = new string[][]
        {
            new [] {"property_col_count", "LeiaMedia Column count"},
            new [] {"property_row_count", "LeiaMedia Row count" }
        };

        void OnEnable()
        {
            lmv = (LeiaMediaViewer)target;

            leiaMediaVideoURL = serializedObject.FindProperty("leiaMediaVideoURL");
            leiaMediaVideoClip = serializedObject.FindProperty("leiaMediaVideoClip");
            leiaMediaTexture = serializedObject.FindProperty("leiaMediaTexture");
            automaticAspectRatio = serializedObject.FindProperty("automaticAspectRatio");
            maxScaleBeforeAspectRatio = serializedObject.FindProperty("maxScaleBeforeAspectRatio");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Set media here. Do NOT use VideoPlayer.");
            EditorGUILayout.HelpBox("Media filename should have format [name...]_[cols]x[rows].[fmt]", MessageType.Info);

            // display several properties using same style
            SerializedProperty[] leiaMediaProperties = new [] { leiaMediaVideoURL, leiaMediaVideoClip, leiaMediaTexture };
            bool[] leiaMediaPropertyUpdated = new bool[leiaMediaProperties.Length];
            for (int i = 0; i < leiaMediaProperties.Length; i++)
            {
                // show LeiaMedia properties, and record changes in their values
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(leiaMediaProperties[i]);
                leiaMediaPropertyUpdated[i] = EditorGUI.EndChangeCheck();
            }

            EditorGUILayout.PropertyField(automaticAspectRatio);

            if (automaticAspectRatio.boolValue)
            {
                EditorGUILayout.HelpBox("Max scale of gameObject before gameObject is shrunk to meet media's aspect ratio", MessageType.Info);
                EditorGUILayout.PropertyField(maxScaleBeforeAspectRatio);
            }

            if (GUILayout.Button("Move to Convergence Plane") && lmv != null)
            {
                lmv.ProjectOntoZDP();
            }

            // newer style for newer properties - UndoableInputFieldUtils
            // in addition to changing LeiaMedia URL/video/texture, allow users to change cols / rows
            UndoableInputFieldUtils.ImmediateIntField(
                // get
                () => lmv.ActiveLeiaMediaCols,
                // set
                (int val) => { lmv.ActiveLeiaMediaCols = val; },
                // label
                propertyStringResources[(int)propertyStringIDs.LeiaMediaCol][1],
                serializedObject.targetObject);

            UndoableInputFieldUtils.ImmediateIntField(
                // get
                () => lmv.ActiveLeiaMediaRows,
                // set
                (int val) => { lmv.ActiveLeiaMediaRows = val; },
                // label
                propertyStringResources[(int)propertyStringIDs.LeiaMediaRow][1],
                serializedObject.targetObject);

            serializedObject.ApplyModifiedProperties();

            // if we detected a change in LeiaMedia property, then after applying property update we should also update cols / rows
            for (int i = 0; i < leiaMediaProperties.Length; i++)
            {
                // if user updated a URL/texture/video property
                if (leiaMediaPropertyUpdated[i])
                {
                    string filename = lmv.ActiveLeiaMediaName;
                    int cols, rows = 0;
                    bool parsed = StringExtensions.TryParseColsRowsFromFilename(filename, out cols, out rows);

                    // if we could parse cols and rows from the LeiaMedia name
                    if (parsed)
                    {
                        // then set cols and rows on LeiaMediaViewer
                        lmv.ActiveLeiaMediaCols = cols;
                        lmv.ActiveLeiaMediaRows = rows;
                    }
                }
            }
        }
    }
}
