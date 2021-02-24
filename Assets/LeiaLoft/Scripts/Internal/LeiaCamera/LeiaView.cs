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
using UnityEngine.Rendering;

namespace LeiaLoft
{
    /// <summary>
    /// Wrapper around child LeiaCamera view camera.
    /// </summary>
    public class LeiaView
    {
        private static readonly CameraEvent[] leiaMediaEventTimes = new[] { CameraEvent.BeforeGBuffer, CameraEvent.BeforeForwardOpaque };
        private readonly CommandBuffer[] leiaMediaCommandBuffers = new CommandBuffer[2];

        public static string ENABLED_NAME { get { return "LeiaView"; } }
        public static string DISABLED_NAME { get { return "Disabled_LeiaView"; } }

        private int _viewIndexX = -1;
        private int _viewIndexY = -1;
        private int _viewIndex = -1;

        /// <summary>
        /// Absolute index of this view. The ith LeiaView will have ViewIndex i regardless of its position in a camera grid.
        /// </summary>
        public int ViewIndex
        {
            get
            {
                return (IsCameraNull || !Enabled) ? -1 : _viewIndex;
            }
            set
            {
                _viewIndex = value;
            }
        }

        /// <summary>
        /// First dimension of position in a n x m grid of cameras
        /// </summary>
        public int ViewIndexX
        {
            get
            {
                return (IsCameraNull || !Enabled) ? -1 : _viewIndexX;
            }
            set
            {
                _viewIndexX = value;
            }
        }

        /// <summary>
        /// Second dimension of position in a n x m grid of cameras
        /// </summary>
        public int ViewIndexY
        {
            get
            {
                return (IsCameraNull || !Enabled) ? -1 : _viewIndexY;
            }
            set
            {
                _viewIndexY = value;
            }
        }

        private Camera _camera;

        // maintain same style as LeiaCamera :: Camera
        public Camera Camera
        {
            get
            {
                return _camera;
            }
        }

        public bool IsCameraNull
        {
            get { return _camera ? false : true; }
        }

        public GameObject Object
        {
            get { return _camera ? _camera.gameObject : default(GameObject); }
        }

        public Vector3 Position
        {
            get { return _camera.transform.localPosition; }
            set { _camera.transform.localPosition = value; }
        }

        public Matrix4x4 Matrix
        {
            get { return _camera.projectionMatrix; }
            set { _camera.projectionMatrix = value; }
        }

        public float FarClipPlane
        {
            get { return _camera.farClipPlane; }
            set { _camera.farClipPlane = value; }
        }

        public float NearClipPlane
        {
            get { return _camera.nearClipPlane; }
            set { _camera.nearClipPlane = value; }
        }

        public Rect ViewRect
        {
            get { return _camera.rect; }
            set { _camera.rect = value; }
        }

        public RenderTexture TargetTexture
        {
            get { return !_camera ? null : _camera.targetTexture; }
            set { if (_camera) { _camera.targetTexture = value; } }
        }

        public bool Enabled
        {
            get { return !_camera ? false : _camera.enabled; }
            set { if (_camera) { _camera.enabled = value; } }
        }

        /// <summary>
        /// Creates a renderTexture with a specific width and height, but no name.
        /// 
        /// Use cases - user has old code, wants to continue compiling with old code.
        /// Or user intentionally wants to not specify a name for RenderTexture.
        /// </summary>
        /// <param name="width">Width of renderTexture</param>
        /// <param name="height">Height of renderTexture</param>
        public void SetTextureParams(int width, int height)
        {
            SetTextureParams(width, height, "");
        }

        /// <summary>
        /// Creates a renderTexture.
        /// </summary>
        /// <param name="width">Width of renderTexture in pixels</param>
        /// <param name="height">Height of renderTexture in pixels</param>
        /// <param name="viewName">Name of renderTexture</param>
        public void SetTextureParams(int width, int height, string viewName)
        {
            if (IsCameraNull)
            {
                return;
            }

            int antiAliasing = LeiaDisplay.Instance.AntiAliasing;

            if (_camera.targetTexture == null)
            {
                TargetTexture = CreateRenderTexture(width, height, antiAliasing, viewName);
            }
            else
            {
                if (TargetTexture.width != width ||
                    TargetTexture.height != height ||
                    TargetTexture.antiAliasing != antiAliasing)
                {
                    Release();
                    TargetTexture = CreateRenderTexture(width, height, antiAliasing, viewName);
                }
            }
        }

        private RenderTexture CreateRenderTexture(int width, int height, int antiAliasing, string rtName)
        {
            this.Debug("Creating RenderTexture");
            this.Trace(width + "x" + height + ", antiAliasing: " + antiAliasing);
            var newTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { name = rtName };
            newTexture.anisoLevel = 1;
            newTexture.filterMode = FilterMode.Bilinear;
            newTexture.antiAliasing = antiAliasing;
            newTexture.Create();

            return newTexture;
        }

        /// <summary>
        /// Gets parameters from root camera
        /// </summary>
        public void RefreshParameters(UnityCameraParams cameraParams)
        {
            if (IsCameraNull)
            {
                return;
            }

            _camera.clearFlags = cameraParams.ClearFlags;
            _camera.cullingMask = cameraParams.CullingMask;
            _camera.depth = cameraParams.Depth;
            _camera.backgroundColor = cameraParams.BackgroundColor;
            _camera.orthographic = cameraParams.Orthographic;
            _camera.orthographicSize = cameraParams.OrthographicSize;
            _camera.fieldOfView = cameraParams.FieldOfView;
            ViewRect = cameraParams.ViewportRect;
#if UNITY_5_6_OR_NEWER
            _camera.allowHDR = cameraParams.AllowHDR;
#else
			_camera.hdr = cameraParams.AllowHDR;
#endif
            _camera.renderingPath = cameraParams.RenderingPath;
        }

        [System.Obsolete("Deprecated. Users should not be manually rendering from a LeiaView. Allow enabled Camera component to render.")]
        public void Render()
        {
            if (IsCameraNull)
            {
                return;
            }

            _camera.Render();
        }

        /// <summary>
        /// A method for attaching a CommandBuffer to a LeiaView at the BeforeImageEffectsOpaque step
        /// Dispose of CommandBuffer in calling funciton.
        /// </summary>
        /// <param name="cb">A CommandBuffer to attach.</param>
        [System.Obsolete("Deprecated. Users should now also specify CameraEvent time in this call.")]
        public void AttachCommandBufferToView(CommandBuffer cb)
        {
            AttachCommandBufferToView(cb, CameraEvent.BeforeImageEffectsOpaque);
        }

        /// <summary>
        /// A method for attaching a CommandBuffer to a LeiaView.
        /// Dispose of CommandBuffer in calling function.
        /// </summary>
        /// <param name="cb">The CommandBuffer to attach</param>
        /// <param name="eventTime">The CameraEvent which should trigger the CommandBuffer</param>
        public void AttachCommandBufferToView(CommandBuffer cb, CameraEvent eventTime)
        {
            if (_camera != null)
            {
                _camera.AddCommandBuffer(eventTime, cb);
            }
        }

        public void AttachLeiaMediaCommandBuffersForIndex(int index)
        {
            for (int i = 0; i < leiaMediaCommandBuffers.Length; i++)
            {
                if (leiaMediaCommandBuffers[i] == null)
                {
                    // attach a CommandBuffer which sets _LeiaViewID early in deferred and forward rendering paths
                    leiaMediaCommandBuffers[i] = new CommandBuffer { name = "_LeiaViewID = " + index.ToString() };
                    leiaMediaCommandBuffers[i].SetGlobalFloat("_LeiaViewID", index);
                    // deferred: beforegbuffer, forward: beforeforwardopaque
                    AttachCommandBufferToView(leiaMediaCommandBuffers[i], leiaMediaEventTimes[i]);
                }
            }
        }

        public LeiaView(GameObject root, UnityCameraParams cameraParams)
        {
            this.Debug("ctor()");
            var rootCamera = root.GetComponent<Camera>();

            for (int i = 0; i < rootCamera.transform.childCount; i++)
            {
                var child = rootCamera.transform.GetChild(i);

                if (child.name == DISABLED_NAME)
                {
                    child.name = ENABLED_NAME;
                    child.hideFlags = HideFlags.None;
                    _camera = child.GetComponent<Camera>();
                    _camera.enabled = true;

#if UNITY_5_6_OR_NEWER
                    _camera.allowHDR = cameraParams.AllowHDR;
#else
					_camera.hdr = cameraParams.AllowHDR;
#endif
                    break;
                }
            }

            if (_camera == null)
            {
                _camera = new GameObject(ENABLED_NAME).AddComponent<Camera>();
            }

            _camera.transform.parent = root.transform;
            _camera.transform.localPosition = Vector3.zero;
            _camera.transform.localRotation = Quaternion.identity;
            _camera.clearFlags = cameraParams.ClearFlags;
            _camera.cullingMask = cameraParams.CullingMask;
            _camera.depth = cameraParams.Depth;
            _camera.backgroundColor = cameraParams.BackgroundColor;
            _camera.fieldOfView = cameraParams.FieldOfView;
            _camera.depthTextureMode = DepthTextureMode.None;
            _camera.hideFlags = HideFlags.None;
            _camera.orthographic = cameraParams.Orthographic;
            _camera.orthographicSize = cameraParams.OrthographicSize;
            ViewRect = rootCamera.rect;
#if UNITY_5_6_OR_NEWER
            _camera.allowHDR = cameraParams.AllowHDR;
#else
			_camera.hdr = cameraParams.AllowHDR;
#endif

        }

        /// <summary>
        /// Allows LeiaCamera to clear all CommandBuffers on this view.
        /// Called from LeiaCamera.SetViewCount -> LeiaCamera.ClearViews().
        /// </summary>
        [System.Obsolete("CommandBuffer disposal should be managed from outside the LeiaLoft Unity SDK")]
        public void ClearCommandBuffers()
        {
            if (_camera != null)
            {
                _camera.RemoveAllCommandBuffers();
            }
        }

        [System.Obsolete("Use Release()")]
        public void ReleaseTexture()
        {
            Release();
        }

        public void Release()
        {
            // targetTexture can be null at this point in execution
            if (TargetTexture != null)
            {
                if (Application.isPlaying)
                {
                    TargetTexture.Release();
                    GameObject.Destroy(TargetTexture);
                }
                else
                {
                    TargetTexture.Release();
                    GameObject.DestroyImmediate(TargetTexture);
                }

                TargetTexture = null;
            }

            // internal LeiaMedia CommandBuffers are released at same time as all other Disposable / Releasable resources
            for (int i = 0; i < leiaMediaCommandBuffers.Length; i++)
            {
                if (leiaMediaCommandBuffers[i] != null)
                {
                    if (_camera != null)
                    {
                        _camera.RemoveCommandBuffer(leiaMediaEventTimes[i], leiaMediaCommandBuffers[i]);
                    }
                    leiaMediaCommandBuffers[i].Dispose();
                    leiaMediaCommandBuffers[i] = null;
                }
            }
        }
    }
}
