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
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LeiaLoft
{
    /// <summary>
    /// ILeiaState implementation of common methods (independent of display type)
    /// </summary>
    public abstract class AbstractLeiaStateTemplate : ILeiaState
    {
        private const string SeparateTilesNotSupported = "RenderToSeparateTiles is not supporting more than 16 LeiaViews.";

        protected DisplayConfig _displayConfig;
        protected int _viewsWide;
        protected int _viewsHigh;
        protected float _deltaView;
        protected int _backlightMode;
        protected Material _material;
        protected string _shaderName;
        protected string _transparentShaderName;
        private Vector2[] _emissionPattern;
        protected RenderTexture template_renderTexture;
        private string _viewBinPattern;
        public string GetViewBinPattern()
        {
            return _viewBinPattern;
        }

        public AbstractLeiaStateTemplate(DisplayConfig displayConfig)
        {
            _displayConfig = displayConfig;
        }

        public virtual void SetViewCount(int viewsWide, int viewsHigh)
        {
            this.Debug(string.Format("SetViewCount( {0}, {1})", viewsWide, viewsHigh));
            _viewsWide = viewsWide;
            _viewsHigh = viewsHigh;
        }

        public void SetBacklightMode(int modeId)
        {
            _backlightMode = modeId;
        }

        public void SetShaderName(string shaderName, string transparentShaderName)
        {
            this.Debug(string.Format("SetShaderName( {0}, {1})", shaderName, transparentShaderName));
            _shaderName = shaderName;
            _transparentShaderName = transparentShaderName;
        }

        public abstract void GetFrameBufferSize(out int width, out int height);

        public abstract void GetTileSize(out int width, out int height);

        protected virtual Material CreateMaterial(bool alphaBlending)
        {
            var shaderName = alphaBlending ? _transparentShaderName : _shaderName;
            return new Material(Shader.Find(shaderName));
        }

        public virtual void DrawImage(LeiaCamera camera, LeiaStateDecorators decorators)
        {
            if (_material == null)
            {
                this.Trace("Creating material");
                _material = CreateMaterial(decorators.AlphaBlending);
            }
            if(template_renderTexture == null)
            {
                template_renderTexture = new RenderTexture(camera.Camera.pixelWidth, camera.Camera.pixelHeight, 0) { name = "interlaced" };
            }

            _material.SetFloat("_viewRectX",camera.Camera.rect.x);
            _material.SetFloat("_viewRectY",camera.Camera.rect.y);
            _material.SetFloat("_viewRectW",camera.Camera.rect.width);
            _material.SetFloat("_viewRectH",camera.Camera.rect.height);

            if (_viewsHigh * _viewsWide > 16)
            {
                throw new NotSupportedException(SeparateTilesNotSupported);
            }

            for(int i = 0; i < _displayConfig.NumViews.x; i++)
            {
                int  viewIndex = (int)(i * (1f/_displayConfig.NumViews.x) * _displayConfig.UserNumViews.x);
                _material.SetTexture("_texture_" + i, camera.GetView(viewIndex).TargetTexture);
            }

            // all templates run this line
            // Square and Slanted use it to interlace using an interlacing _material.
            // Abstract uses it to copy data from _texture_0 to template_renderTexture because _material is TWO_DIM shader, i.e. a simple pixel-copy shader
            Graphics.Blit(Texture2D.whiteTexture, template_renderTexture, _material);
            // Square and Slanted perform additional blits to screen in their override DrawImage classes

            // Square and Slanted are excluded from this line because their _material.name is not OpaqueShaderName or TransparentShaderName
            if (_material != null && !string.IsNullOrEmpty(_material.name) && (_material.name.Equals(TwoDimLeiaStateTemplate.OpaqueShaderName) || _material.name.Equals(TwoDimLeiaStateTemplate.TransparentShaderName)))
            {
                // AbstractLeiaStateTemplate uses this line to copy 2D view data from template_renderTexture to screen
                Graphics.Blit(template_renderTexture, Camera.current.activeTexture);
            }
        }

        //potentially ready for removal
        private void DrawQuad(LeiaStateDecorators decorators)
        {
            GL.PushMatrix();
            GL.LoadOrtho();
            _material.SetPass(0);
            GL.Begin(GL.QUADS);

            int o = 1;
            int z = 0;

            if (decorators.ParallaxOrientation.IsInv())
            {
                o = 0;
                z = 1;
            }

            GL.TexCoord2(z, z); GL.Vertex3(0, 0, 0);
            GL.TexCoord2(z, o); GL.Vertex3(0, 1, 0);
            GL.TexCoord2(o, o); GL.Vertex3(1, 1, 0);
            GL.TexCoord2(o, z); GL.Vertex3(1, 0, 0);

            GL.End();
            GL.PopMatrix();
        }
        protected void CheckRenderTechnique(LeiaStateDecorators decorators)
        {
            switch (decorators.RenderTechnique)
            {
                case LeiaDisplay.RenderTechnique.Default:
                    _displayConfig.UserNumViews = _displayConfig.NumViews;
                    break;
                case LeiaDisplay.RenderTechnique.Stereo:
                    _displayConfig.UserNumViews = new XyPair<int>(2, 1);
                    break;
                default:
                    this.Error(string.Format("Invalid RenderTechinque : {0}", decorators.RenderTechnique));
                    break;
            }
        }
        protected virtual int YOffsetWhenInverted()
        {
            return 0;
        }

        protected virtual int XOffsetWhenInverted()
        {
            return 0;
        }

        protected void RespectOrientation(LeiaStateDecorators decorators)
        {
            if (_viewsWide == _viewsHigh)
            {
                return;
            }

            var wide = _viewsWide > _viewsHigh;

            if (decorators.ParallaxOrientation.IsLandscape() != wide)
            {
                var tmp = _viewsWide;
                _viewsWide = _viewsHigh;
                _viewsHigh = tmp;
            }
        }

#region view_peeling_code
        private float getNxf(LeiaStateDecorators decorators, int nx)
        {
            int xPeel = (int)decorators.AdaptFOV.x;
            int initialNxf = nx;
            int terminalNxf = nx;
            if (xPeel > 0.0f)
            {
                if (initialNxf < xPeel)
                {
                    int offset = (xPeel - initialNxf + _viewsWide) / _viewsWide * _viewsWide;
                    terminalNxf = initialNxf + offset;
                }
            }
            else if (xPeel < 0.0f)
            {
                if (initialNxf >= _viewsWide + xPeel)
                {
                    int offset = (-xPeel + initialNxf) / _viewsWide * _viewsWide;
                    terminalNxf = initialNxf - offset;
                }
            }
            return terminalNxf;
        }

        // viewConfig defines the rate at which parallax views are shifted. further from 4 -> faster parallax shift for same adaptFOV.x
        private readonly int[] viewConfig = new [] { 2, 2, 2, 2, 6, 6, 6, 6 };
        private float getNxfForStereo(LeiaStateDecorators decorators, int nx)
        {
            return viewConfig[getShiftPosition(decorators, nx)];
        }

        public int getShiftPosition(LeiaStateDecorators decorators, int nx)
        {
            int xPeel = (int)decorators.AdaptFOV.x;
            int xShift = nx + Mathf.Abs(xPeel);
            int position = (xShift) % viewConfig.Length;
            return position;
        }
#endregion 

        protected void UpdateEmissionPattern(LeiaStateDecorators decorators)
        {
            _emissionPattern = new Vector2[_viewsWide * _viewsHigh];
            float offsetX = -0.5f * (_viewsWide - 1.0f);
            float offsetY = -0.5f * (_viewsHigh - 1.0f);
            float[] nxfs = new float[_viewsWide];
            System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();

            for (int ny = 0; ny < _viewsHigh; ny++)
            {
                for (int nx = 0; nx < _viewsWide; nx++)
                {
                    float nxf;
                    if (decorators.ViewPeelEnabled)
                    {
                        // parallax shift cycles cameras
                        nxf = getNxf(decorators, nx);
                    }
                    else
                    {
                        // parallax shift is a function of FOV and camera x position in array
                        nxf = decorators.AdaptFOV.x + nx;
                    }

                    nxfs[nx] = nxf;
                    float nyf = ny;

                    _emissionPattern[nx + ny * _viewsWide] = new Vector2(offsetX + nxf, offsetY + nyf);
                }
            }

            for (int i = 0; i < nxfs.Length; i++)
            {
                stringBuilder.AppendFormat("{0:0.}|", nxfs[i]);
            }
            _viewBinPattern = stringBuilder.ToString();
        }

		public virtual void UpdateState(LeiaStateDecorators decorators, ILeiaDevice device)
        {
            this.Debug("UpdateState");
            if (_material == null)
            {
                _material = CreateMaterial(decorators.AlphaBlending);
            }
            // by default UserNumViews will be same as NumViews
            _displayConfig.UserNumViews = _displayConfig.NumViews;
            // but in CheckRenderTechnique, override UserNumViews to have more accurate values
            CheckRenderTechnique(decorators);
            // once _displayConfig.UserNumViews is definitely containing appropriate viewCount x, call SetViewCount to cache viewCount x in _viewsWide.
            // Later, AbstractLeiaStateTemplate :: UpdateViews will retrieve _viewsWide and use it to call LeiaCamera :: SetViewCount
            SetViewCount(_displayConfig.UserNumViews.x, 1);

            RespectOrientation(decorators);
            UpdateEmissionPattern(decorators);
            var shaderParams = new ShaderFloatParams();

            shaderParams._width = _displayConfig.UserPanelResolution.x ;
            shaderParams._height = _displayConfig.UserPanelResolution.y ;
            shaderParams._viewResX = _displayConfig.UserViewResolution.x / _displayConfig.ResolutionScale;
            shaderParams._viewResY = _displayConfig.UserViewResolution.y / _displayConfig.ResolutionScale;

            var offset = new [] {(int)_displayConfig.AlignmentOffset.x, (int)_displayConfig.AlignmentOffset.y };
            shaderParams._offsetX = offset[0] + (decorators.ParallaxOrientation.IsInv() ? XOffsetWhenInverted() : 0);
            shaderParams._offsetY = offset[1] + (decorators.ParallaxOrientation.IsInv() ? YOffsetWhenInverted() : 0);

            shaderParams._viewsX = _displayConfig.NumViews.x;
            shaderParams._viewsY = _viewsHigh;

            shaderParams._orientation = decorators.ParallaxOrientation.IsLandscape() ? 1 : 0;
            shaderParams._adaptFOVx = decorators.AdaptFOV.x;
            shaderParams._adaptFOVy = decorators.AdaptFOV.y;
            shaderParams._enableSwizzledRendering = 1;
            shaderParams._enableHoloRendering = 1;
            shaderParams._enableSuperSampling = 0;
            shaderParams._separateTiles = 1;


            var is2d = shaderParams._viewsY == 1 && shaderParams._viewsX == 1;

            if (decorators.ShowTiles || is2d)
            {
                shaderParams._enableSwizzledRendering = 0;
                shaderParams._enableHoloRendering = 0;
            }

            if (decorators.ShowTiles)
            {
                _material.EnableKeyword("ShowTiles");
            }
            else
            {
                _material.DisableKeyword("ShowTiles");
            }

            shaderParams._showCalibrationSquares = decorators.ShowCalibration ? 1 : 0;
            shaderParams.ApplyTo(_material);
        }

        public virtual void UpdateViews(LeiaCamera leiaCamera)
        {
            this.Debug("UpdateViews");
            if (_viewsWide != _displayConfig.UserNumViews.x)
            {
                SetViewCount(_displayConfig.UserNumViews.x, _viewsHigh);
                UpdateEmissionPattern(LeiaDisplay.Instance.Decorators);
            }
            leiaCamera.SetViewCount(_viewsWide * _viewsHigh);

            int width, height;
            GetTileSize(out width, out height);

            int id = 0;
            for (int ny = 0; ny < _viewsHigh; ny++)
            {
                for (int nx = 0; nx < _viewsWide; nx++)
                {
                    int viewId = ny * _viewsWide + nx;
					var view = leiaCamera.GetView(viewId);

                    if (view.IsCameraNull)
                    {
                        continue;
                    }
                    string viewIdStr = string.Format("view_{0}_{1}", nx, ny);
                    view.SetTextureParams(width, height, viewIdStr);
                    view.ViewIndexX = nx;
                    view.ViewIndexY = ny;
                    view.AttachLeiaMediaCommandBuffersForIndex(id);
                    view.ViewIndex = id++;
                }
            }
        }

        public virtual int GetViewsCount()
        {
            return _viewsWide * _viewsHigh;
        }

        public int GetBacklightMode()
        {
            return _backlightMode;
        }

        protected float GetEmissionX(int nx, int ny)
        {
            return _emissionPattern[nx + ny * _viewsWide].x;
        }

        protected float GetEmissionY(int nx, int ny)
        {
            return _emissionPattern[nx + ny * _viewsWide].y;
        }

        public virtual void Release()
        {
            this.Debug("Release()");

            // release interlacing _material
            if (_material != null)
            {
                if (Application.isPlaying)
                {
                    GameObject.Destroy(_material);
                }
                else
                {
                    GameObject.DestroyImmediate(_material);
                }
            }

            // release _templateRenderTexture
            if (template_renderTexture != null)
            {
                if (Application.isPlaying)
                {
                    template_renderTexture.Release();
                    GameObject.Destroy(template_renderTexture);
                }
                else
                {
                    template_renderTexture.Release();
                    GameObject.DestroyImmediate(template_renderTexture);
                }

                template_renderTexture = null;
            }
        }

    }
}
