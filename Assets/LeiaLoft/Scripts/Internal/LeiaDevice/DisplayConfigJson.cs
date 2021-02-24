using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;

namespace LeiaLoft
{
    public class DisplayConfigJson
    {
#if UNITY_ANDROID
        private static readonly string _filename = "DisplayConfiguration_Hydrogen.json";
#else
        private static readonly string _filename = "DisplayConfiguration_A0.json";
#endif

        public static string JsonPath { get; private set; }
        [Serializable]
        public class JsonConfig
        {
            public string ComName;
            public bool isSquare;
            public bool isSlanted;
            public static readonly string DefaultRenderMode = "HPO";
            public float Gamma; 
            public bool Slant;
            public float Beta;
            public float[] InterlacingVector;
            public float[] InterlacingMatrix;
            public float[] DotPitchInMm;
            public int[] PanelResolution;
            public int[] NumViews;
            public float[] AlignmentOffset;
            public float[] ActCoefficientsX;
            public float[] ActCoefficientsY;
            public float SystemDisparityPercent;
            public float SystemDisparityPixels;
            public int[] DisplaySizeInMm;
            public int[] ViewResolution;
            public string[] RenderModes;
            public float[] GetActKernel;
            public float[] UserActCoefficients;
            public float[] UserDotPitchInMM;
            public int[] UserPanelResolution;
            public int[] UserNumViews;
            public float[] UserAlignmentOffset;
            public int[] UserViewResolution;
            public int[] UserDisplaySizeInMm;
            public float UserAspectRatio;
            public bool UserOrientationIsLandscape;
            public float ResolutionScale;

            [NonSerialized] private HashSet<string> updateTags = new HashSet<string>();

            /// <summary>
            /// Constructs a JsonConfig from serialized data
            /// </summary>
            /// <param name="fullFilePath">Path to a serialized text asset</param>
            public JsonConfig(string fullFilePath)
            {
                string data = File.ReadAllText(fullFilePath);
                Overwrite(data);
            }

            /// <summary>
            /// Constructs a JsonConfig from a DisplayConfig
            /// </summary>
            /// <param name="config">A DisplayConfig whose members will be cloned</param>
            public JsonConfig(DisplayConfig config)
            {
                ResolutionScale = config.ResolutionScale;
                ActCoefficientsX = config.ActCoefficients.x.ToArray();
                ActCoefficientsY = config.ActCoefficients.y.ToArray();
                DotPitchInMm = new[] { config.DotPitchInMm.x, config.DotPitchInMm.y };
                PanelResolution = new[] { config.PanelResolution.x, config.PanelResolution.y };
                NumViews = new[] { config.NumViews.x, config.NumViews.y };
                AlignmentOffset = new[] { config.AlignmentOffset.x, config.AlignmentOffset.y };
                ViewResolution = new[] { config.ViewResolution.x, config.ViewResolution.y };
                DisplaySizeInMm = new[] { 0, 0 };
                SystemDisparityPercent = config.SystemDisparityPercent;
                SystemDisparityPixels = config.SystemDisparityPixels;
                UserOrientationIsLandscape = config.UserOrientationIsLandscape;
                UserDotPitchInMM = config.UserDotPitchInMM == null ? new[] { 0f, 0f } : new[] { config.UserDotPitchInMM.x, config.UserDotPitchInMM.y };
                UserPanelResolution = new[] { config.UserPanelResolution.x, config.UserPanelResolution.y };
                UserNumViews = new[] { config.UserNumViews.x, config.UserNumViews.y };
                UserAlignmentOffset = config.UserAlignmentOffset == null ? new[] { 0f, 0f } : new[] { config.UserAlignmentOffset.x, config.UserAlignmentOffset.y };
                UserActCoefficients = new float[config.UserActCoefficients.x.Count + config.UserActCoefficients.y.Count];
                UserViewResolution = new[] { config.UserViewResolution.x, config.UserViewResolution.y };
                UserDisplaySizeInMm = config.UserDisplaySizeInMm == null ? new[] { 0, 0 } : new[] { config.UserDisplaySizeInMm.x, config.UserDisplaySizeInMm.y };
                UserAspectRatio = config.UserAspectRatio;
                RenderModes = config.RenderModes.ToArray();
                InterlacingMatrix = config.InterlacingMatrix == null ? new[] { 0f, 0f } : config.InterlacingMatrix;
                InterlacingVector = config.InterlacingVector == null ? new[] { 0f, 0f } : config.InterlacingVector;
                Gamma = config.Gamma;
                Slant = config.Slant;
                Beta = config.Beta;
                isSquare = config.isSquare;
                isSlanted = config.isSlanted;
            }

            /// <summary>
            /// Updates members of this class with serialized data
            /// </summary>
            /// <param name="serializedData">String of serialized data from a text asset</param>
            public void Overwrite(string serializedData)
            {
                // maintain list of attributes which were updated by this json
                // when actually updating a DisplayConfig with this object, we will 

                // finds pattern    "name"<optionalwhitespace>:
                // this is a variable name in json
                Regex patternJsonVarname = new Regex("\"\\w*\"\\s*:");
                
                foreach (Match match in patternJsonVarname.Matches(serializedData))
                {
                    // remove remove all non-chars (whitespace [ ], [:], ["]), leaving only param name
                    string regexReplaced = Regex.Replace(match.Value, @"[^\w]", string.Empty);
                    updateTags.Add(regexReplaced);
                }

                // populate all fields on this json
                JsonUtility.FromJsonOverwrite(serializedData, this);

                // could do error-checking here
            }

            /// <summary>
            /// Overwrite accessible properties on a DisplayConfig.
            /// </summary>
            /// <param name="dc">A DisplayConfig to overwrite some properties on</param>
            /// <param name="permissions">Update permissions to adhere to when converting JsonConfig params to DisplayConfig params</param>
            public void UpdatePermittedAttributesOn(DisplayConfig dc, params JsonConfigParameterUpdatePermission[] permissions)
            {
                HashSet<JsonConfigParameterUpdatePermission> hashPermissions = new HashSet<JsonConfigParameterUpdatePermission>(permissions);

                // Permission level determines which params will be checked in sparse JsonConfig definition
                // Note: permissions levels are unordered and one permission level is not necessarily a subset of another permission level

                // updateParamProtocol defines what Action (defined by delegate     () => {}     ) to perform when a variable is set
                Dictionary<string, Action> updateParamProtocol = new Dictionary<string, Action>();
                updateParamProtocol["ResolutionScale"] = () => { dc.ResolutionScale = ResolutionScale; };
                updateParamProtocol["Gamma"] = () => { dc.Gamma = Gamma; };
                updateParamProtocol["Beta"] = () => { dc.Beta = Beta; };
                updateParamProtocol["Slant"] = () => { dc.Slant = Slant; };
                updateParamProtocol["isSquare"] = () => { dc.isSquare = isSquare; };
                updateParamProtocol["isSlanted"] = () => { dc.isSlanted = isSlanted; };
                updateParamProtocol["AlignmentOffset"] = () => { dc.AlignmentOffset = new XyPair<float>(AlignmentOffset[0], AlignmentOffset[1]); };
                updateParamProtocol["SystemDisparityPercent"] = () => { dc.SystemDisparityPercent = SystemDisparityPercent; };
                updateParamProtocol["SystemDisparityPixels"] = () => { dc.SystemDisparityPixels = SystemDisparityPixels; };
                updateParamProtocol["ActCoefficientsX"] = () =>
                {
                    if (dc.ActCoefficients == null) { dc.ActCoefficients = new XyPair<List<float>>(null, null); }
                    dc.ActCoefficients.x = new List<float>(ActCoefficientsX);
                };
                updateParamProtocol["ActCoefficientsY"] = () =>
                {
                    if (dc.ActCoefficients == null) { dc.ActCoefficients = new XyPair<List<float>>(null, null); }
                    dc.ActCoefficients.y = new List<float>(ActCoefficientsY);
                };
                updateParamProtocol["InterlacingVector"] = () => { dc.InterlacingVector = InterlacingVector; };
                updateParamProtocol["InterlacingMatrix"] = () => { dc.InterlacingMatrix = InterlacingMatrix; };
                updateParamProtocol["DotPitchInMm"] = () => { dc.DotPitchInMm = new XyPair<float>(DotPitchInMm[0], DotPitchInMm[1]); };
                updateParamProtocol["PanelResolution"] = () => { dc.PanelResolution = new XyPair<int>(PanelResolution[0], PanelResolution[1]); };
                updateParamProtocol["NumViews"] = () => { dc.NumViews = new XyPair<int>(NumViews[0], NumViews[1]); };
                updateParamProtocol["DisplaySizeInMm"] = () => { dc.DisplaySizeInMm = new XyPair<int>(DisplaySizeInMm[0], DisplaySizeInMm[1]); };
                updateParamProtocol["ViewResolution"] = () => { dc.ViewResolution = new XyPair<int>(ViewResolution[0], ViewResolution[1]); };

                // UpdatePermissions define what keys to search for, which define which protocols get run

                // Run update protocol for PerApplicationAct permission level
                if (hashPermissions.Contains(JsonConfigParameterUpdatePermission.PerApplicationACT))
                {
                    foreach (string permittedTag in new[] { "Gamma", "Beta", "Slant", "AlignmentOffset", "SystemDisparityPercent", "SystemDisparityPixels", "ActCoefficientsX", "ActCoefficientsY", "ResolutionScale" })
                    {
                        if (updateTags.Contains(permittedTag) && updateParamProtocol.ContainsKey(permittedTag))
                        {
                            updateParamProtocol[permittedTag]();
                            updateParamProtocol.Remove(permittedTag);
                        }
                    }
                }

                // Run update protocol for Unrestricted permission level
                if (hashPermissions.Contains(JsonConfigParameterUpdatePermission.Unrestricted))
                {
                    foreach (string permittedTag in new[] { "Gamma", "Beta", "Slant", "isSquare", "isSlanted", "AlignmentOffset", "SystemDisparityPercent", "SystemDisparityPixels", "ActCoefficientsX", "ActCoefficientsY",
                                                            "InterlacingVector", "InterlacingMatrix", "DotPitchInMm", "PanelResolution", "NumViews", "AlignmentOffset", "DisplaySizeInMm",
                                                            "ViewResolution", "ResolutionScale"})
                    {
                        if (updateTags.Contains(permittedTag) && updateParamProtocol.ContainsKey(permittedTag))
                        {
                            updateParamProtocol[permittedTag]();
                            updateParamProtocol.Remove(permittedTag);
                        }
                    }
                }
            }

            public override string ToString()
            {
                return JsonUtility.ToJson(this, true);
            }
        }

        public enum JsonConfigParameterUpdatePermission
        {
            Unrestricted,
            PerApplicationACT
        };

        public static void WriteJson(DisplayConfig config)
        {
            JsonConfig updated = new JsonConfig(config);
            if (!JsonFileExists())
            {
                File.Create(JsonPath).Dispose();
            }
            File.WriteAllText(JsonPath, updated.ToString());
        }
        [System.Obsolete("Use AbstractLeiaDevice :: ApplyDisplayConfigUpdate")]
        public static DisplayConfig ReadConfigFromJson()
        {
            try
            {
                if (JsonFileExists())
                {
                    JsonConfig jsonConfig = new JsonConfig(JsonPath);
                    return Convert(jsonConfig);
                }
            }
            catch(Exception e)
            {
                LogUtil.Error(e.Message);
            }
            return null;
        }
        public static bool JsonFileExists()
        {
#if UNITY_EDITOR
            JsonPath = Path.Combine(Path.Combine("Assets", "LeiaLoft"), Path.Combine("Resources", _filename)); //Assets/LeiaLot/Resources/DisplayConfig*.json
#elif UNITY_STANDALONE
            JsonPath = Path.Combine(Application.dataPath, _filename); //<AppLocation>/<AppName>_Data/DisplayConfig*.json
#elif UNITY_ANDROID
            JsonPath = Path.Combine(Application.persistentDataPath, _filename); //storage/emulated/0/Android/data/<packagename>/files/DispalyConfig*.json
#endif
            return File.Exists(JsonPath);
        }

        static DisplayConfig Convert(JsonConfig jsonConfig)
        {
            List<float> actX = new List<float>();
            List<float> actY = new List<float>();
            for (int i = 0; i < jsonConfig.ActCoefficientsX.Length; ++i)
            {
                actX.Add(jsonConfig.ActCoefficientsX[i]);
            }
            for (int i = 0; i < jsonConfig.ActCoefficientsY.Length; ++i)
            {
                actY.Add(jsonConfig.ActCoefficientsY[i]);
            }
            return new DisplayConfig
            {
                ResolutionScale = jsonConfig.ResolutionScale,
                DotPitchInMm = new XyPair<float>(jsonConfig.DotPitchInMm[0], jsonConfig.DotPitchInMm[1]),
                PanelResolution = new XyPair<int>(jsonConfig.PanelResolution[0], jsonConfig.PanelResolution[1]),
                NumViews = new XyPair<int>(jsonConfig.NumViews[0], jsonConfig.NumViews[1]),
                AlignmentOffset = new XyPair<float>(jsonConfig.AlignmentOffset[0], jsonConfig.AlignmentOffset[1]),
                ActCoefficients = new XyPair<List<float>>(actX, actY),
                ViewResolution = new XyPair<int>(jsonConfig.ViewResolution[0], jsonConfig.ViewResolution[1]),
                DisplaySizeInMm = new XyPair<int>(jsonConfig.DisplaySizeInMm[0], jsonConfig.DisplaySizeInMm[1]),
                SystemDisparityPercent = jsonConfig.SystemDisparityPercent,
                SystemDisparityPixels = jsonConfig.SystemDisparityPixels,
                UserOrientationIsLandscape = jsonConfig.UserOrientationIsLandscape,
                UserDotPitchInMM = new XyPair<float>(jsonConfig.UserDotPitchInMM[0], jsonConfig.UserDotPitchInMM[1]),
                UserPanelResolution = new XyPair<int>(jsonConfig.UserPanelResolution[0], jsonConfig.UserPanelResolution[1]),
                UserNumViews = new XyPair<int>(jsonConfig.UserNumViews[0], jsonConfig.UserNumViews[1]),
                UserAlignmentOffset = new XyPair<float>(jsonConfig.UserAlignmentOffset[0], jsonConfig.UserAlignmentOffset[1]),
                UserActCoefficients = new XyPair<List<float>>(actX, actY),
                UserViewResolution = new XyPair<int>(jsonConfig.UserViewResolution[0], jsonConfig.UserViewResolution[1]),
                UserDisplaySizeInMm = new XyPair<int>(jsonConfig.UserDisplaySizeInMm[0], jsonConfig.UserDisplaySizeInMm[1]),
                UserAspectRatio = jsonConfig.UserAspectRatio,
                InterlacingMatrix = jsonConfig.InterlacingMatrix,
                InterlacingVector = jsonConfig.InterlacingVector,
                Gamma = jsonConfig.Gamma,
                Slant = jsonConfig.Slant,
                Beta = jsonConfig.Beta
            };
        }
    }
}

