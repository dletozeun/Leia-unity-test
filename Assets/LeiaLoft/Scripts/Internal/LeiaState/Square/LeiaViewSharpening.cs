using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LeiaLoft
{
    [System.Obsolete("ViewSharpening.cs should not be added as a component. StateTemplate performs ViewSharpening now.")]
    public class LeiaViewSharpening : MonoBehaviour
    {
        public const string ShaderName = "LeiaLoft/ViewSharpening";
        public const string DisplayNotFound = "LeiaDisplay not found. ViewSharpening require LeiaDiplay on same GameObject.";

        private ScreenOrientation _lastOrientation;
        private LeiaDisplay _leiaDisplay;
        private Material _material;

        private float is_enabled = 1.0f;

        void Start()
        {
            _leiaDisplay = LeiaDisplay.Instance;

            if (_leiaDisplay == null)
            {
                this.Error(DisplayNotFound);
                return;
            }
        }

        /// <summary>
        /// Use to apply parameters
        /// </summary>
        public void UpdateParameters()
        {
            if (_material == null)
            {
                _material = new Material(Shader.Find(ShaderName));
            }

            int MAX_ACT_COEEFS = 4;
            int sizeX = Mathf.Min(_leiaDisplay.GetDisplayConfig().ActCoefficients.x.Count, MAX_ACT_COEEFS);
            int sizeY = Mathf.Min(_leiaDisplay.GetDisplayConfig().ActCoefficients.y.Count, MAX_ACT_COEEFS);

            Vector4 x = Vector4.zero;
            Vector4 y = Vector4.zero;
            for (int i = 0; i < sizeX; ++i)
            {
                x[i] = _leiaDisplay.GetDisplayConfig().ActCoefficients.x[i];
            }
            for (int i = 0; i < sizeY; ++i)
            {
                y[i] = _leiaDisplay.GetDisplayConfig().ActCoefficients.y[i];
            }

            _material.SetInt("sharpening_x_size", sizeX);
            _material.SetInt("sharpening_y_size", sizeY);
            _material.SetVector("sharpening_x", x);
            _material.SetVector("sharpening_y", y);

            _lastOrientation = Screen.orientation;
        }

        void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (_material == null || Screen.orientation != _lastOrientation)
            {
                UpdateParameters();
                Graphics.Blit(src, dest);
                return;
            }

            src.wrapMode = TextureWrapMode.Clamp;
            src.filterMode = FilterMode.Point;

            Graphics.Blit(src, dest, _material);
        }

        public void FlipACTEnabled()
        {
            if (is_enabled > 0.0)
            {
                is_enabled = 0.0f;
            }
            else
            {
                is_enabled = 1.0f;
            }
        }
    }
}