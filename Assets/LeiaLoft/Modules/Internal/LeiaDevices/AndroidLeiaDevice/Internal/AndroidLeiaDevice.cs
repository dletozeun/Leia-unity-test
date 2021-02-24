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
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace LeiaLoft
{
    public class AndroidLeiaDevice : AbstractLeiaDevice
    {
        private static AndroidJavaClass _leiaBacklightClass;
        private AndroidJavaObject _leiaBacklightInstance;

        public override int[] CalibrationOffset
        {
            get
            {
                InitializeBacklightModule();
                if (_leiaBacklightInstance == null)
                {
                    return base.CalibrationOffset;
                }

                int[] temp = _leiaBacklightInstance.Call<int[]> ("getXYCalibration");
                return new [] { temp[1], temp[2] };
            }
            set
            {
                this.Warning("Setting calibration from Unity Plugin is not supported anymore - use relevant app instead.");
            }
        }

        private void InitializeBacklightModule()
        {
            if (_leiaBacklightClass == null ||
                _leiaBacklightInstance == null)
            {
                try
                {
                    if (_leiaBacklightClass == null)
                    {
                        _leiaBacklightClass = new AndroidJavaClass("android.leia.LeiaBacklight");
                        _leiaBacklightClass.CallStatic("start", "LeiaDisplay", false);
                    }

                    _leiaBacklightInstance = _leiaBacklightClass.GetStatic<AndroidJavaObject>("instance");
                }
                catch (System.Exception e)
                {
                    this.Error("Unable to get response from backlight service. Using default profile stub:" + _profileStubName);
                    this.Error(e.ToString());

                }

            }
        }

        public AndroidLeiaDevice(string stubName)
        {
            this.Debug("ctor");
            string displayType = stubName;

            if (!string.IsNullOrEmpty(displayType))
            {
                _profileStubName = displayType;
                this.Trace("displayType " + displayType);
            }
            else
            {
                this.Debug("No displayType received, using stub: " + stubName);
            }
            InitializeBacklightModule();
        }

        public override void SetBacklightMode(int modeId)
        {
            InitializeBacklightModule();
            if (_leiaBacklightInstance != null)
            {
                this.Trace("SetBacklightMode" + modeId);
                _leiaBacklightInstance.Call ("setBacklightMode", modeId);
            }
        }

        /// <summary>
        /// Interpolates between display's 2D light and 3D backlight.
        /// </summary>
        /// <param name="alpha">Intensity of 3D backlight; 0 is very weak, 1 is fully active</param>
        public void SetDisplayLightBalance(float alpha)
        {
            const float ratio2d = 0.95f;
            float ratio3d = 1.0f - Mathf.Clamp01(alpha);

            // note: correct args for AndroidLeiaDevice :: setBacklightTransition are (3D, 2D).
            // whereas arg names in LeiaBacklight.java :: setBacklightTransition are (float ratio2d, float ratio3d)

            // note: in other firmware, span is from 0 - 16, but here it is from 0 - 1

            // note: on Hydrogen, SetBacklightMode(3) causes backlight intensity to be slightly higher than 1.0f
            // whereas on Lumepad, SetBacklightMode(3) gives equivalent backlight intensity as SetDisplayLightBalance(1.0f)
            _leiaBacklightInstance.Call("setBacklightTransition", ratio3d, ratio2d);
        }

        public override void SetBacklightMode(int modeId, int delay)
        {
            InitializeBacklightModule();
            if (_leiaBacklightInstance != null)
            {
                this.Trace("SetBacklightMode" + modeId);
                _leiaBacklightInstance.Call ("setBacklightMode", modeId, delay);
            }
        }

        public override void RequestBacklightMode(int modeId)
        {
        }

        public override void RequestBacklightMode(int modeId, int delay)
        {
        }
        public override int GetBacklightMode()
        {
            InitializeBacklightModule();
            if (_leiaBacklightInstance != null)
            {
                
                int mode =_leiaBacklightInstance.Call<int>("getBacklightMode");
                return mode;
            }
            
            return 2;
        }


        public override DisplayConfig GetDisplayConfig()
        {

            if (_displayConfig != null)
            {
                return _displayConfig;
            }

            _displayConfig = new DisplayConfig();
            try
            {
                AndroidJavaObject displayConfig = _leiaBacklightInstance.Call<AndroidJavaObject> ("getDisplayConfig");
                _displayConfig = new DisplayConfig();

                // TODO hardcoded for now; replace with firmware retrieval calls later
                _displayConfig.Gamma = 2.2f;
                _displayConfig.Beta = 1.4f;
                _displayConfig.isSquare = true;

                // The following params
                // DotPitchInMm, PanelResolution, NumViews, AlignmentOffset, DisplaySizeInMm, and ViewResolution
                // are reported as
                // (value for device's shorter dimension, value for device's longer dimension) rather than
                // (value for device's wide side, value for device's long side) as we would expect.
                // See DisplayConfig :: UserOrientationIsLandscape

                AndroidJavaObject dotPitchInMM = displayConfig.Call<AndroidJavaObject>("getDotPitchInMm");
                _displayConfig.DotPitchInMm = new XyPair<float>(dotPitchInMM.Get<AndroidJavaObject>("x").Call<float>("floatValue"),
                    dotPitchInMM.Get<AndroidJavaObject>("y").Call<float>("floatValue"));

                AndroidJavaObject panelResolution = displayConfig.Call<AndroidJavaObject>("getPanelResolution");
                _displayConfig.PanelResolution = new XyPair<int>( panelResolution.Get<AndroidJavaObject>("x").Call<int>("intValue"),
                    panelResolution.Get<AndroidJavaObject>("y").Call<int>("intValue"));

                AndroidJavaObject numViews = displayConfig.Call<AndroidJavaObject>("getNumViews");
                _displayConfig.NumViews = new XyPair<int>(numViews.Get<AndroidJavaObject>("x").Call<int>("intValue"),
                    numViews.Get<AndroidJavaObject>("y").Call<int>("intValue"));

                AndroidJavaObject alignmentOffset = displayConfig.Call<AndroidJavaObject>("getAlignmentOffset");
                _displayConfig.AlignmentOffset = new XyPair<float>(alignmentOffset.Get<AndroidJavaObject>("x").Call<float>("floatValue"),
                    alignmentOffset.Get<AndroidJavaObject>("y").Call<float>("floatValue"));

                AndroidJavaObject displaySizeInMm = displayConfig.Call<AndroidJavaObject>("getDisplaySizeInMm");
                _displayConfig.DisplaySizeInMm = new XyPair<int>( displaySizeInMm.Get<AndroidJavaObject>("x").Call<int>("intValue"),
                    displaySizeInMm.Get<AndroidJavaObject>("y").Call<int>("intValue"));

                AndroidJavaObject viewResolution = displayConfig.Call<AndroidJavaObject>("getViewResolution");
                _displayConfig.ViewResolution = new XyPair<int>(viewResolution.Get<AndroidJavaObject>("x").Call<int>("intValue"),
                    viewResolution.Get<AndroidJavaObject>("y").Call<int>("intValue"));

                AndroidJavaObject actCoefficients = displayConfig.Call<AndroidJavaObject>("getViewSharpeningCoefficients");

                float xA = actCoefficients.Get<AndroidJavaObject>("x").Call<AndroidJavaObject>("get",0).Call<float>("floatValue");
                float xB = actCoefficients.Get<AndroidJavaObject>("x").Call<AndroidJavaObject>("get",1).Call<float>("floatValue");
                float yA = actCoefficients.Get<AndroidJavaObject>("y").Call<AndroidJavaObject>("get",0).Call<float>("floatValue");
                float yB = actCoefficients.Get<AndroidJavaObject>("y").Call<AndroidJavaObject>("get",1).Call<float>("floatValue");
                this.Debug("coefs: " + xA + " " + xB + " " + yA + " " + yB);
                _displayConfig.ActCoefficients = new XyPair<List<float>>(new List<float>(), new List<float>());

                _displayConfig.ActCoefficients.x = new List<float>();
                _displayConfig.ActCoefficients.x.Add(xA);
                _displayConfig.ActCoefficients.x.Add(xB);

                _displayConfig.ActCoefficients.y = new List<float>();
                _displayConfig.ActCoefficients.y.Add(yA);
                _displayConfig.ActCoefficients.y.Add(yB);

                _displayConfig.SystemDisparityPercent = _leiaBacklightInstance.Call<float>("getSystemDisparityPercent");
                _displayConfig.SystemDisparityPixels = _leiaBacklightInstance.Call<float>("getSystemDisparityPixels");
            }
            catch (System.Exception e)
            {
                LogUtil.Log(LogLevel.Error, "While loading data from Android DisplayConfig from firmware, encountered error {0}", e);
            }

            // populate _displayConfig from FW with developer-tuned values
            base.ApplyDisplayConfigUpdate(DisplayConfigModifyPermission.Level.DeveloperTuned);

            return _displayConfig;
        }

        public override bool IsConnected()
        {
            InitializeBacklightModule();
            if (_leiaBacklightInstance != null)
            {
                return _leiaBacklightInstance.Call<bool>("isConnected");
            }
            return false;
        }

        public override RuntimePlatform GetRuntimePlatform()
        {
            return RuntimePlatform.Android;
        }

        /// <summary>
        /// Android devices may have screen or height greater at any moment.
        ///
        /// Due to an issue on Lumepad where Screen.Orientation can be Portrait or Landscape in wrong cases,
        /// have to use Screen.width/height to determine if device is landscape or not.
        /// </summary>
        /// <returns>True if device screen is wider than it is tall</returns>
        public override bool IsScreenOrientationLandscape()
        {
            return Screen.width > Screen.height;
        }
    }
}
