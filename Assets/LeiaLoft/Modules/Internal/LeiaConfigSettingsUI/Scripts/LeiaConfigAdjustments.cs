/****************************************************************
*
* Copyright 2020 © Leia Inc.  All rights reserved.
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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LeiaLoft;

namespace LeiaLoft
{
    /// <summary>
    /// A component for controling UI and modifying an existing scene's DisplayConfig params at runtime.
    ///
    /// In OnDestroy (gameObject destruction, scene deload, component destruction, editor stop play, etc.)
    /// writes your DisplayConfig params to a local file.
    /// </summary>
    public class LeiaConfigAdjustments : MonoBehaviour
    {
        [SerializeField] private JsonParamCollection sparseUpdates = new JsonParamCollection();

#pragma warning disable 0649

        [SerializeField]
        private Slider
            actaSlider, actbSlider, actcSlider, actdSlider,
            gammaSlider, disparitySlider, betaSlider,
            offsetSlider, resScaleSlider;
        [SerializeField]
        private Text actaLabel, actbLabel, actcLabel, actdLabel, gamaLabel, disparityLabel, offsetLabel, betaLabel, actcTitle, actdTitle, slantLabel, resScaleLabel;
        [SerializeField]
        private GameObject slantPage;

        private DisplayConfig config;
        bool isSlantedState;

        // Tuples not in all .net versions
        // private class to facilitate slider value changed -> text update
        private class SliderLabelPair
        {
            public SliderLabelPair(Slider _slider, Text _text)
            {
                Slider = _slider;
                Text = _text;
            }
            public Slider Slider { get; set; }
            public Text Text { get; set; }
        }

        private void OnDestroy()
        {
            // If we call StringAssetUtil.WriteJsonObject... in editor, it calls AssetDatabase.ImportAsset
            // Importing a Resource like a json can cause other Shader resources with float4x4 properties to discard their previously set properties.
            // So only write json when object is being destroyed
            if (sparseUpdates != null)
            {
                string stateUpdateFilename = string.Format("DisplayConfigUpdate{0}.json", isSlantedState ? "Slanted" : "Square");
                StringAssetUtil.WriteJsonObjectToDeviceAwareFilename(stateUpdateFilename, sparseUpdates, true);
            }
        }

        void DeconstructUI()
        {
            // json is written on Component destruction, not on UI deconstruction

            // clear all callbacks
            foreach (Slider slider in new[] { actaSlider, actbSlider, actcSlider, actdSlider, gammaSlider, disparitySlider, betaSlider, offsetSlider })
            {
                slider.onValueChanged.RemoveAllListeners();
            }
        }

        void ConstructUI()
        {
            config = LeiaDisplay.Instance.GetDisplayConfig();
            isSlantedState = config.isSlanted;
            // slant polarity
            slantLabel.text = config.Slant ? "Right" : "Left";

            AttachPrimitiveSliderCallbacks();
            AttachActSliderCallbacks();
            AttachFormattedTextCallbacks();

            // on opening config UI, retrieve values from DisplayConfig and assign them to sliders
            // callbacks in OnValueChanged are not triggered when callback attachment occurs in same code execution as callback trigger
            gammaSlider.value = config.Gamma;
            disparitySlider.value = config.SystemDisparityPixels;
            betaSlider.value = config.Beta;
            offsetSlider.value = config.AlignmentOffset.x;
            resScaleSlider.value = config.ResolutionScale;

            if (isSlantedState)
            {
                actcTitle.text = "ACT Y[2]";
                actdTitle.text = "ACT Y[3]";
                actaSlider.value = config.ActCoefficients.y[0];
                actbSlider.value = config.ActCoefficients.y[1];
                actcSlider.value = config.ActCoefficients.y[2];
                actdSlider.value = config.ActCoefficients.y[3];
            }
            else
            {
                actcTitle.text = "ACT X[0]";
                actdTitle.text = "ACT X[1]";
                actaSlider.value = config.ActCoefficients.y[0];
                actbSlider.value = config.ActCoefficients.y[1];
                actcSlider.value = config.ActCoefficients.x[0];
                actdSlider.value = config.ActCoefficients.x[1];
            }
            slantPage.SetActive(isSlantedState);
            betaSlider.gameObject.SetActive(isSlantedState);
            gammaSlider.GetComponent<RectTransform>().anchoredPosition = new Vector2(isSlantedState ? -200 : 0, gammaSlider.GetComponent<RectTransform>().anchoredPosition.y);
            string stateUpdateFilename = string.Format("DisplayConfigUpdate{0}.json", isSlantedState ? "Slanted" : "Square");
            bool loadedStateUpdateFile = StringAssetUtil.TryGetJsonObjectFromDeviceAwareFilename<JsonParamCollection>(stateUpdateFilename, out sparseUpdates);
            if (!loadedStateUpdateFile)
            {
                sparseUpdates = new JsonParamCollection();
            }

        }

        void AttachPrimitiveSliderCallbacks()
        {
            // 1-length-arrays
            gammaSlider.onValueChanged.AddListener((float v) =>
            {
                sparseUpdates.SetSingle("Gamma", v);
                config.Gamma = v;
            });

            betaSlider.onValueChanged.AddListener((float v) =>
            {
                sparseUpdates.SetSingle("Beta", v);
                config.Beta = v;
            });

            disparitySlider.onValueChanged.AddListener((float v) =>
            {
                sparseUpdates.SetSingle("SystemDisparityPixels", v);
                config.SystemDisparityPixels = v;
            });
            resScaleSlider.onValueChanged.AddListener((float v) =>
            {
                sparseUpdates.SetSingle("ResolutionScale", v);
                config.ResolutionScale = v;
                LeiaDisplay.Instance.UpdateDevice();
            });
            // sets AlignmentOffset to an array of len 2, with second value hardcoded to 0
            offsetSlider.onValueChanged.AddListener((float v) =>
            {
            // manual y = 0
            sparseUpdates["AlignmentOffset"] = new[] { v, 0.0f };
                config.AlignmentOffset = new XyPair<float>(v, 0.0f);
            });
        }

        void AttachActSliderCallbacks()
        {
            if (isSlantedState)
            {
                foreach (Slider slider in new[] { actaSlider, actbSlider, actcSlider, actdSlider })
                {
                    slider.onValueChanged.AddListener((float v) =>
                    {
                        sparseUpdates["ActCoefficientsY"] = new[] { actaSlider.value, actbSlider.value, actcSlider.value, actdSlider.value };
                    });
                }
                actaSlider.onValueChanged.AddListener((float v) => { config.ActCoefficients.y[0] = v; });
                actbSlider.onValueChanged.AddListener((float v) => { config.ActCoefficients.y[1] = v; });
                actcSlider.onValueChanged.AddListener((float v) => { config.ActCoefficients.y[2] = v; });
                actdSlider.onValueChanged.AddListener((float v) => { config.ActCoefficients.y[3] = v; });
            }
            else
            {
                // square callbacks; act x and y both need to be set
                // ACTA, ACTB are always ACT coefficients.y
                foreach (Slider slider in new[] { actaSlider, actbSlider })
                {
                    slider.onValueChanged.AddListener((float v) =>
                    {
                        sparseUpdates["ActCoefficientsY"] = new[] { actaSlider.value, actbSlider.value };
                    });
                }
                actaSlider.onValueChanged.AddListener((float v) => { config.ActCoefficients.y[0] = v; });
                actbSlider.onValueChanged.AddListener((float v) => { config.ActCoefficients.y[1] = v; });

                foreach (Slider slider in new[] { actcSlider, actdSlider })
                {
                    slider.onValueChanged.AddListener((float v) =>
                    {
                        sparseUpdates["ActCoefficientsX"] = new[] { actcSlider.value, actdSlider.value };
                    });
                }
                actcSlider.onValueChanged.AddListener((float v) => { config.ActCoefficients.x[0] = v; });
                actdSlider.onValueChanged.AddListener((float v) => { config.ActCoefficients.x[1] = v; });
            }
        }

        void AttachFormattedTextCallbacks()
        {
            // attaches callbacks to every single slider to control label with formatting, and trigger device updates
            foreach (SliderLabelPair sliderLabelPair in new[] {
            new SliderLabelPair(actaSlider, actaLabel), new SliderLabelPair(actbSlider, actbLabel), new SliderLabelPair(actcSlider, actcLabel),
            new SliderLabelPair(actdSlider, actdLabel), new SliderLabelPair(gammaSlider, gamaLabel), new SliderLabelPair(disparitySlider, disparityLabel),
            new SliderLabelPair(betaSlider, betaLabel), new SliderLabelPair(offsetSlider, offsetLabel), new SliderLabelPair(resScaleSlider, resScaleLabel)
            })
            {
                sliderLabelPair.Slider.onValueChanged.AddListener((float v) =>
                {
                    sliderLabelPair.Text.text = string.Format("{0:0.####}", v);
                    LeiaDisplay.Instance.UpdateLeiaState();
                });

                // manually set formatting on UI load
                sliderLabelPair.Text.text = string.Format("{0}", sliderLabelPair.Slider.value);
            }
        }

        void OnEnable()
        {
            ConstructUI();
        }

        private void OnDisable()
        {
            DeconstructUI();
        }

        public void ResetValues()
        {
            sparseUpdates.Clear();

            // trigger a write of a blank SparseUpdates file
            string stateUpdateFilename = string.Format("DisplayConfigUpdate{0}.json", isSlantedState ? "Slanted" : "Square");
            StringAssetUtil.WriteJsonObjectToDeviceAwareFilename(stateUpdateFilename, sparseUpdates, true);

            this.DeconstructUI();

            // reload DisplayConfig from firmware up
            LeiaDisplay.Instance.LeiaDevice.GetDisplayConfig(true);
            LeiaDisplay.Instance.UpdateDevice();

            // reload UI, rebind callbacks
            this.ConstructUI();
        }

        public void UpdateSlant()
        {
            // no slider for slant
            config.Slant = !config.Slant;
            sparseUpdates.SetSingle<bool>("Slant", config.Slant);
            slantLabel.text = config.Slant ? "Right" : "Left";
            LeiaDisplay.Instance.UpdateDevice();
        }
    }
}
