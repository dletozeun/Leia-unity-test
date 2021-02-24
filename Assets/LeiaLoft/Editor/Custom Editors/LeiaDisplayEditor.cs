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
using System.Linq;
using System.Collections.Generic;

namespace LeiaLoft
{
    [UnityEditor.CustomEditor(typeof(LeiaDisplay))]
    public class LeiaDisplayEditor : UnityEditor.Editor
    {
        private const string ProfileSelectionLabel = "Profile to use if no device is connected:";
        private const string RenderModeLabel = "Render Mode (Legacy)";
        private const string RenderTechniqueLabel = "Render Technique";
        private const string LightfieldModeLabel = "Lightfield Mode";
        private const string AntiAliasingLabel = "Anti Aliasing";
        private const string DeferredNotSupportedLabel = "(not supported in deferred rendering mode)";
        private const string EnabledParallaxWarning = "Parallax Auto Rotation checkbox will be ignored if AutoRotation is enabled in PlayerSettings";
        private const string RenderModeFieldLabel = "LeiaLoft_LeiaDisplayEditor_RenderMode";
        private const string AntiAliasingFieldLabel = "LeiaLoft_LeiaDisplayEditor_AntiAliasing";
        private const string CalibrationSquaresFieldLabel = "Show Calibration Squares";
        private const string ParallaxFieldLabel = "Parallax Auto Rotation";
        private const string AlphaBlendingLabel = "Enable Alpha Blending";
        private const string CalibrationXFieldLabel = "Calibration X";
        private const string CalibrationYFieldLabel = "Calibration Y";
        private const string ShowTilesFieldLabel = "Show Tiles";
        private const string AdaptFovXFieldLabel = "Adapt. FOV X";
        private const string AdaptFovYFieldLabel = "Adapt. FOV Y";
        private const string SwitchModeOnSceneChangeLabel = "Switch to 2D mode on scene change";
        private const string alphaBlendingTooltip = "Multiple cameras with different depths can be blended together. Set one camera to have a clear flag Solid Color with low alpha, and the other camera will render its content over that weak alpha background";

        private LeiaDisplay _controller;

        void OnEnable()
        {
            if (_controller == null)
            {
                _controller = (LeiaDisplay)target;
            }
        }

        private void ShowLightfieldModeControl()
        {
            LeiaDisplay.LightfieldMode[] modes = new[] { LeiaDisplay.LightfieldMode.Off, LeiaDisplay.LightfieldMode.On };
            string[] options = new[] { modes[0].ToString(), modes[1].ToString() };

            int previousIndex = _controller.DesiredLightfieldValue;

            UndoableInputFieldUtils.PopupLabeled(index =>
            {
                _controller.DesiredLightfieldMode = modes[index];
            }
            , LightfieldModeLabel, previousIndex, options, _controller);

        }

        private void ShowRenderTechniqueControl()
        {
            if (_controller.DesiredLightfieldMode != LeiaDisplay.LightfieldMode.On)
            {
                return;
            }
            LeiaDisplay.RenderTechnique[] renderTechniques = new[] { LeiaDisplay.RenderTechnique.Default, LeiaDisplay.RenderTechnique.Stereo };
            string[] options = new[] { string.Format("{0}", renderTechniques[0]), string.Format("{0}", renderTechniques[1]) };

            int previousIndex = (int)_controller.DesiredRenderTechnique;

            UndoableInputFieldUtils.PopupLabeled(index =>
            {
                _controller.DesiredRenderTechnique = renderTechniques[index];
            }, RenderTechniqueLabel, previousIndex, options, _controller.Settings);
        }

#pragma warning disable CS0618 // Type or member is obsolete
        private void ShowRenderModeControl()
        {
            List<string> leiaModes = _controller.GetDisplayConfig().RenderModes;
            var list = leiaModes.ToList().Beautify();
            var previousIndex = list.IndexOf(_controller.DesiredLeiaStateID, ignoreCase: true);

            if (previousIndex < 0)
            {
                LogUtil.Log(LogLevel.Error, "Did not recognize renderMode {0}", _controller.DesiredLeiaStateID);
                list.Add(_controller.DesiredLeiaStateID);
                previousIndex = list.Count - 1;
            }

            EditorGUI.BeginDisabledGroup(true);
            UndoableInputFieldUtils.PopupLabeled(index =>
            {
                if (list[index] == LeiaDisplay.TWO_D)
                {
                    _controller.DesiredLightfieldMode = LeiaDisplay.LightfieldMode.Off;
                }
                else if (list[index] == LeiaDisplay.THREE_D || list[index] == LeiaDisplay.HPO)
                {
                    _controller.DesiredLightfieldMode = LeiaDisplay.LightfieldMode.On;
                }
                else
                {
                    LogUtil.Log(LogLevel.Error, "Could not match RenderMode {0} at index {1} to LightfieldMode", list[index], index);
                }
            }
            , RenderModeLabel, previousIndex, list.ToArray(), _controller.Settings);
            EditorGUI.EndDisabledGroup();

            if (_controller.DesiredLeiaStateID == LeiaDisplay.TWO_D && _controller.IsLightfieldModeDesiredOn() ||
                _controller.DesiredLeiaStateID == LeiaDisplay.HPO && !_controller.IsLightfieldModeDesiredOn())
            {
                Debug.LogErrorFormat("On GameObject {0}: state mismatch between legacy RenderMode DesiredLeiaStateID {1} and LightfieldMode {2}\n" +
                    "Please update the {0}'s LightfieldMode",
                    _controller.gameObject.name, _controller.DesiredLeiaStateID, _controller.DesiredLightfieldMode);
            }
        }
#pragma warning restore CS0618 // Type or member is obsolete

        private void ShowAntialiasingDropdown()
        {
            bool deferred = false;

#if !UNITY_5_5_OR_NEWER
            deferred = PlayerSettings.renderingPath == RenderingPath.DeferredLighting;
#else
            var settings = UnityEditor.Rendering.EditorGraphicsSettings.
                GetTierSettings(EditorUserBuildSettings.selectedBuildTargetGroup, Graphics.activeTier);
            deferred = settings.renderingPath == RenderingPath.DeferredLighting ||
                settings.renderingPath == RenderingPath.DeferredShading;
#endif

            if (deferred)
            {
                GUILayout.Label(DeferredNotSupportedLabel);

                if (_controller.AntiAliasing != 1)
                {
                    _controller.AntiAliasing = 1;
                }

                return;
            }

            var values = AntiAliasingHelper.Values;
            var previousIndex = values.ToList().IndexOf(_controller.AntiAliasing);
            var stringOptions = AntiAliasingHelper.NamedValues;

            UndoableInputFieldUtils.PopupLabeled(index => _controller.AntiAliasing = values[index], AntiAliasingLabel, previousIndex, stringOptions, _controller.Settings);
        }


        private void ShowDecoratorsControls()
        {
            var decorators = _controller.Decorators;

            if (PlayerSettings.defaultInterfaceOrientation == UIOrientation.AutoRotation)
            {
                EditorGUI.BeginDisabledGroup(true);
            }

            UndoableInputFieldUtils.BoolFieldWithTooltip(() => decorators.ParallaxAutoRotation, v =>
                {
                    decorators.ParallaxAutoRotation = v;

#if UNITY_EDITOR && UNITY_ANDROID
                    if (PlayerSettings.defaultInterfaceOrientation == UIOrientation.AutoRotation && v)
                    {
                        this.Warning(EnabledParallaxWarning);
                    }
#endif
                }

                , ParallaxFieldLabel, "Only accessible when Player Settings -> Android -> Resolution and presentation -> Default orientation is not AutoRotation", _controller.Settings);

            if (PlayerSettings.defaultInterfaceOrientation == UIOrientation.AutoRotation)
            {
                EditorGUI.EndDisabledGroup();
            }

            if (Application.isPlaying)
            {
                EditorGUI.BeginDisabledGroup(true);
            }

            UndoableInputFieldUtils.BoolFieldWithTooltip(() => decorators.AlphaBlending, v => decorators.AlphaBlending = v, AlphaBlendingLabel,
            alphaBlendingTooltip,
            _controller.Settings);
            
            if (Application.isPlaying)
            {
                EditorGUI.EndDisabledGroup();
            }

            // Edit > Project Settings > Player settings > Other > Scripting define symbols
#if LEIA_ADVANCED_USER
            // for ShowTiles param on LeiaDisplay decorator
            // setting does not persist between runs
            if (Application.isEditor && Application.isPlaying)
            {
                UndoableInputFieldUtils.BoolField(() => _controller.Decorators.ShowTiles, (bool b) =>
                {
                    decorators.ShowTiles = b;
                    _controller.IsDirty = true;
                }, ShowTilesFieldLabel);
            }
#endif

            _controller.Decorators = decorators;
        }

        /// <summary>
        /// User needs to be able to set DisplayConfig once and have setting persist through play/edit process.
        ///
        /// Dev needs to be able to retrieve DisplayConfig data without chaining through LeiaDisplay -> DeviceFactory -> OfflineEmulationLeiaDevice.
        /// These objects may be reconstructed and drop pointers on play, or some code which we want to be editor-only would have to be included in builds.
        /// </summary>
        void ShowDisplayConfigDropdown()
        {
            // build a path to subfolder where display config files are found
            string searchPath = Application.dataPath;
            foreach (string subfolder in new[] { "LeiaLoft", "Resources" })
            {
                searchPath = System.IO.Path.Combine(searchPath, subfolder);
            }

            string fileSearchString = "DisplayConfiguration_";
            string fileTerminalString = ".json";
            // convert file paths into short names which can be displayed to user
            string[] displayConfigPathMatches = System.IO.Directory.GetFiles(searchPath, fileSearchString + "*.json");
            List<string> displayConfigFilenames = new List<string>();
            for (int i = 0; i < displayConfigPathMatches.Length; i++)
            {
                displayConfigFilenames.Add(System.IO.Path.GetFileName(displayConfigPathMatches[i]));
            }

            // write user-selection into editor prefs
            int ind = Mathf.Max(0, displayConfigFilenames.IndexOf(OfflineEmulationLeiaDevice.EmulatedDisplayConfigFilename));

            if (ind >= displayConfigFilenames.Count)
            {
                LogUtil.Log(LogLevel.Error, "No DisplayConfiguration files found in Assets/LeiaLoft/Resources! Please reinstall your LeiaLoft Unity SDK");
                return;
            }

            string[] trimmedDisplayConfigFilenameArray = displayConfigFilenames.Select(x => x.Replace(fileSearchString, "").Replace(fileTerminalString, "")).ToArray();

            // suppress DisplayConfig dropdown selection when build player window is open. This avoids a bug where selecting a new build target,
            // not switching platform, and then changing emulated device profile would cause Unity to throw a GUI error
            bool isBuildPlayerWindowOpen = IsWindowOpen<BuildPlayerWindow>();

            EditorGUI.BeginDisabledGroup(Application.isPlaying || isBuildPlayerWindowOpen);
            UndoableInputFieldUtils.PopupLabeled(
                (int i) =>
                {
                    OfflineEmulationLeiaDevice.EmulatedDisplayConfigFilename = displayConfigFilenames[i];
                }, "Editor Emulated Device", ind, trimmedDisplayConfigFilenameArray);
            if (isBuildPlayerWindowOpen)
            {
                EditorGUILayout.LabelField("Close build player window before changing emulated device profile");
            }

            UndoableInputFieldUtils.BoolFieldWithTooltip(
                () =>
                {
                    return LeiaPreferenceUtil.GetUserPreferenceBool(true, OfflineEmulationLeiaDevice.updateGameViewResOnDisplayProfileChange, Application.dataPath);
                },
                (bool b) =>
                {
                    LeiaPreferenceUtil.SetUserPreferenceBool(OfflineEmulationLeiaDevice.updateGameViewResOnDisplayProfileChange, b, Application.dataPath);
                },
                "Set game view resolution when Editor Emulated Device changes", "", null);
            EditorGUILayout.LabelField("");

            EditorGUI.EndDisabledGroup();
        }

        /// <summary>
        /// Searches through all UnityEditor objects for an EditorWindow
        /// </summary>
        /// <typeparam name="WindowType">A specific type of EditorWindow to search for</typeparam>
        /// <returns>True if any window of this type is open</returns>
        private static bool IsWindowOpen<WindowType>() where WindowType : EditorWindow
        {
            WindowType[] openWindows = Resources.FindObjectsOfTypeAll<WindowType>();
            return openWindows != null && openWindows.Length > 0;
        }

        public override void OnInspectorGUI()
        {
            if (!_controller.enabled)
            {
                return;
            }

            ShowDisplayConfigDropdown();
            ShowLightfieldModeControl();
            ShowRenderTechniqueControl();
            ShowAntialiasingDropdown();
            ShowDecoratorsControls();
        }
    }
}