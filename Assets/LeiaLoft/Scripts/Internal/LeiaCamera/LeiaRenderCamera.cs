using UnityEngine;
using System.Collections.Generic;

#if UNITY_2019_1_OR_NEWER
using UnityEngine.Rendering;
#endif

namespace LeiaLoft
{
    public class LeiaRenderCamera : MonoBehaviour
    {
#pragma warning disable 414
        private Camera myCam;
        private readonly HashSet<Camera> frameCams = new HashSet<Camera>();
#pragma warning restore 414

        LeiaCamera _leiaCamera;

#if UNITY_2019_1_OR_NEWER

        string assetTypeURP = "UniversalRenderPipelineAsset";
        string assetTypeHDRP = "HDRenderPipelineAsset";
        string assetTypeLWRP = "LightweightRenderPipelineAsset";

        [System.Obsolete("Use IsUnityRenderPipeline")]
        bool IsHDRP_LWRP()
        {
            return IsUnityRenderPipeline();
        }

        /// <summary>
        /// Retreives the renderPipelineAsset which users can specify in Edit :: Project settings :: Graphics settings :: render asset.
        ///
        /// Checks it against known pipelines which the LeiaLoft Unity SDK provides preliminary support for.
        /// </summary>
        /// <returns>True if renderPipeline is non-null and tested with the LeiaLoft Unity SDK</returns>
        bool IsUnityRenderPipeline()
        {
            if (GraphicsSettings.renderPipelineAsset == null)
            {
                return false;
            }

            // currentRenderPipeline only retrievable in 2019.3+. instead have to do string checks
            string renderAssetName = GraphicsSettings.renderPipelineAsset.GetType().Name;
            return (renderAssetName.Contains(assetTypeURP) || renderAssetName.Contains(assetTypeLWRP) || renderAssetName.Contains(assetTypeHDRP));
        }

        void OnEnable()
        {
            if (IsUnityRenderPipeline())
            {
                RenderPipelineManager.endFrameRendering += EndFrameRenderHook;
            }
        }

        void OnDisable()
        {
            if (IsUnityRenderPipeline())
            {
                RenderPipelineManager.endFrameRendering -= EndFrameRenderHook;
            }
        }
        
        void EndFrameRenderHook(ScriptableRenderContext context, Camera[] cam)
        {
            // This method triggers each frame per each LeiaRenderCamera.
            // cam[] will sometimes contain n x m views where n = LeiaRenderCamera count and m = view count
            // cam[] will sometimes contain n LeiaRenderCamera Cameras

            if (IsUnityRenderPipeline() && cam != null && myCam != null)
            {
                // convert n LeiaView cameras and/or LeiaRenderCameras into a hashed collection for faster search
                // avoid using new obj() each frame; stash as a class var
                frameCams.Clear();
                frameCams.UnionWith(cam);

                // each LeiaRenderCamera has only one Camera: myCamera
                // only render out of set {myCamera} & set {Cam[]}
                // end result: each LeiaRenderCamera calls OnPostRender once with its cam
                if (myCam != null && myCam.enabled && frameCams.Contains(myCam))
                {
                    Camera.SetupCurrent(myCam);
                    OnPostRender();
                }
            }

        }

#endif

        public void setLeiaCamera(LeiaCamera leiaCamera)
        {
            // we may still end up populating a static collecton of Cameras here.
            // when users use ShareRenderTextures script, exactly one Camera (deepest Camera) needs to have clear flag = root cam's clear flag
            // other cameras need to have Clear flag = depth only
            myCam = leiaCamera.GetComponentInChildren<LeiaRenderCamera>().GetComponent<Camera>();
            _leiaCamera = leiaCamera;
        }


        void OnPostRender()
        {
            LeiaDisplay.Instance.RenderImage(_leiaCamera);
        }

    }
}
