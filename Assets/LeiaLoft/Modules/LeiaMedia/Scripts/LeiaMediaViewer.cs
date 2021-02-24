/****************************************************************
*
* Copyright 2019 Â© Leia Inc.  All rights reserved.
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
using UnityEngine.Video;

namespace LeiaLoft
{
    /// <summary>
    /// Class for spawning an object with a LeiaMaterial in front of the LeiaCamera.
    /// The LeiaMaterial gives different views to different LeiaCameras.
    /// Media types which can be rendered using a LeiaMaterial include images (textures) and video.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]

    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(VideoPlayer))]
    [RequireComponent(typeof(LeiaMediaVideoPlayer))]
    public class LeiaMediaViewer : UnityEngine.MonoBehaviour, ILeiaMediaMaterialHandler
    {
        public delegate void OnMediaChanged();
        public event OnMediaChanged VideoChangedResponses;

        // variables which should always be present and used
        private MeshRenderer mr;
        private MeshFilter mf;
        private MaterialPropertyBlock mpb;

        // variables which may be hidden in some cases
        private VideoPlayer vp;
        private AudioSource aud_source;
        private LeiaMediaVideoPlayer lmvp;

        public bool automaticAspectRatio = true;

        [SerializeField] private string leiaMediaVideoURL;
        [SerializeField] private VideoClip leiaMediaVideoClip;
        [SerializeField] private Texture2D leiaMediaTexture;

        private static Material LeiaMaterial;
        private static readonly string LeiaMaterial_id = "Materials/LeiaMediaMaterial";

        [SerializeField] private int property_col_count = 0;
        [SerializeField] private int property_row_count = 0;

        private static readonly string id_main_tex = "_MainTex";
        private static readonly string id_col_count = "_ColCount";
        private static readonly string id_row_count = "_RowCount";

        public Vector3 maxScaleBeforeAspectRatio = Vector3.one;

        // Start is called before the first frame update
        void Start()
        {
            Rebuild();
        }

        /// <summary>
        /// OnValidate is called after a variable is edited. E.g. tex, vc, or maxScaleBeforeAspectRatio
        /// </summary>
        void OnValidate()
        {
            Rebuild();
        }

        /// <summary>
        /// Gathers identities of components, prepares content for export using MaterialPropertyBlock
        /// </summary>
        void Rebuild()
        {
            // script requires these components - always exist
            if (mr == null) { mr = transform.GetComponent<MeshRenderer>(); }
            if (mf == null) { mf = transform.GetComponent<MeshFilter>(); }
            if (mpb == null) { mpb = new MaterialPropertyBlock(); }
            if (aud_source == null) { aud_source = transform.GetComponent<AudioSource>(); }
            if (vp == null) { vp = transform.GetComponent<VideoPlayer>(); }
            if (lmvp == null) { lmvp = transform.GetComponent<LeiaMediaVideoPlayer>(); }

            // highest priority - url stream
            if (!string.IsNullOrEmpty(leiaMediaVideoURL))
            {
                if (vp != null)
                {
                    vp.enabled = true;
                    vp.url = leiaMediaVideoURL;
                    vp.source = VideoSource.Url;
                }
                RevealVideoComponents();
            }
            // high priority - video
            else if (leiaMediaVideoClip != null)
            {
                if (vp != null)
                {
                    vp.enabled = true;
                    vp.clip = leiaMediaVideoClip;
                    vp.source = VideoSource.VideoClip;
                }
                RevealVideoComponents();
            }
            // lower priority - texture
            else if (leiaMediaTexture != null)
            {
                vp.enabled = false;
                HideVideoComponents();
            }

            // tertiary priority - use black texture to clear media
            else
            {
                vp.enabled = false;
                mpb.SetTexture(id_main_tex, Texture2D.blackTexture);
                HideVideoComponents();
            }

            if (LeiaMaterial == null)
            {
                LoadMat();
            }

            // mpb is attached to renderer here
            ExportRenderingParams();
        }

        /// <summary>
        /// Forces component HideFlags to be hidden in inspector
        /// </summary>
        void HideVideoComponents()
        {
            if (lmvp != null)
            {
                lmvp.hideFlags = HideFlags.HideInInspector;
            }

            if (vp != null)
            {
                vp.hideFlags = HideFlags.HideInInspector;
                // address issue where video player was being hidden, but not detaching clip

                int old_id = (vp.clip == null ? 0 : vp.clip.GetInstanceID());
                int new_id = (leiaMediaVideoClip == null ? 0 : leiaMediaVideoClip.GetInstanceID());

                vp.clip = leiaMediaVideoClip;

                if (VideoChangedResponses != null && old_id != new_id)
                {
                    VideoChangedResponses();
                }
            }

            if (aud_source != null)
            {
                aud_source.hideFlags = HideFlags.HideInInspector;
            }
        }

        /// <summary>
        /// Forces component HideFlags to be None
        /// </summary>
        void RevealVideoComponents()
        {
            if (lmvp != null)
            {
                lmvp.hideFlags = HideFlags.None;
            }
            if (vp != null)
            {
                vp.hideFlags = HideFlags.None;
            }
            if (aud_source != null)
            {
                aud_source.hideFlags = HideFlags.None;
            }
        }

        public int ActiveLeiaMediaRows
        {
            get
            {
                return property_row_count;
            }
            set
            {
                property_row_count = value;
                ExportRenderingParams();
            }
        }

        public int ActiveLeiaMediaCols
        {
            get
            {
                return property_col_count;
            }
            set
            {
                property_col_count = value;
                ExportRenderingParams();
            }
        }

        [System.Obsolete("Superseded by LeiaLoft.StringExtensions.TryParseColsRowsFromFilename")]
        void ParseNameAs(string local_filename)
        {
            int c = 0;
            int r = 0;

            if (string.IsNullOrEmpty(local_filename))
            {
#if UNITY_EDITOR
                Debug.LogWarningFormat("Set media on {0} but media did not have a filename", gameObject.name);
#else
                LogUtil.Debug("Set media on " + gameObject.name + "  but media did not have a filename");
#endif
            }
            else if (local_filename.LastIndexOf('_') < 0)
            {
#if UNITY_EDITOR
                Debug.LogWarningFormat("Failed to parse filename {0} as *_cxr.fmt. Filename lacks a _ character", local_filename);
#else
                LogUtil.Debug("Failed to parse filename " + local_filename + " as *_cxr.fmt. Filename lacks a _ character");
#endif
            }
            else
            {
                // case: local filename is non-null and has a _ character
                int cresult = 1;
                int rresult = 1;

                try
                {
                    // strip format, strip path, strip whitespace
                    local_filename = System.IO.Path.GetFileNameWithoutExtension(local_filename).Trim();
                    string[] data = local_filename.Substring(local_filename.LastIndexOf('_') + 1).Split('x');
                    int.TryParse(data[0], out cresult);
                    c = cresult;
                    int.TryParse(data[1], out rresult);
                    r = rresult;
                }
                catch (System.Exception e)
                {
#if UNITY_EDITOR
                    Debug.LogWarningFormat("Failed to parse filename {0} as *_cxr.fmt. Got error {1}", local_filename, e);
#else
                LogUtil.Debug("Failed to parse filename " + local_filename + " as *_cxr.fmt. Got error " + e);
#endif
                    c = 0;
                    r = 0;
                }

                property_col_count = c;
                property_row_count = r;
            }
        }

        /// <summary>
        /// Static function - attempts to load the LeiaPrerendered8Material if possible
        /// </summary>
        static void LoadMat()
        {
            LeiaMaterial = Resources.Load(LeiaMaterial_id) as Material;
        }

        /// <summary>
        /// Sets properties of MPB. Sends MPB from memory to material queue
        /// </summary>
        void ExportRenderingParams()
        {
            mpb.SetFloat(id_col_count, property_col_count);
            mpb.SetFloat(id_row_count, property_row_count);
#if UNITY_EDITOR
            if (mr.sharedMaterial == null && LeiaMaterial != null)
            {
                mr.sharedMaterial = LeiaMaterial;
            }
#else
            if (mr.material == null && LeiaMaterial != null)
            {
                mr.material = LeiaMaterial;
            }
#endif

            if (!string.IsNullOrEmpty(leiaMediaVideoURL) && property_col_count > 0 && property_row_count > 0)
            {
                vp.enabled = false;

                // no aspect ratio in URL-loaded files
                vp.url = leiaMediaVideoURL.Trim();
                vp.source = VideoSource.Url;

                vp.enabled = true;
            }
            else if (leiaMediaVideoClip != null && property_col_count > 0 && property_row_count > 0)
            {
                vp.enabled = false;

                FixRatio(new Vector2(leiaMediaVideoClip.width / property_col_count, leiaMediaVideoClip.height / property_row_count));

                int old_id = (vp.clip == null ? 0 : vp.clip.GetInstanceID());
                int new_id = (leiaMediaVideoClip == null ? 0 : leiaMediaVideoClip.GetInstanceID());
                vp.clip = leiaMediaVideoClip;

                // after we update the VideoPlayer.video_clip, provoke responses from listeners
                if (VideoChangedResponses != null && old_id != new_id)
                {
                    VideoChangedResponses();
                }

                vp.enabled = true;
            }

            else if (leiaMediaTexture != null && property_col_count > 0 && property_row_count > 0)
            {
                mpb.SetTexture(id_main_tex, leiaMediaTexture);
                FixRatio(new Vector2(leiaMediaTexture.width / property_col_count, leiaMediaTexture.height / property_row_count));
            }

            mr.SetPropertyBlock(mpb);
        }

        /// <summary>
        /// Sets the local xy scale of the Leia Media
        /// </summary>
        /// <param name="r">Vector2(width, height)</param>
        private void FixRatio(Vector2 r)
        {
            // some variables may not be loaded at editor start time
            if ((int)r.x == 0 || (int)r.y == 0)
                return;
            if (!automaticAspectRatio)
                return;

            Vector2 ratios = new Vector2(maxScaleBeforeAspectRatio.x / r.x, maxScaleBeforeAspectRatio.y / r.y);
            float ratio = Mathf.Min(ratios.x, ratios.y);
            transform.localScale = new Vector3(r.x * ratio, r.y * ratio, transform.localScale.z);
            return;
        }

        /// <summary>
        /// Gets the video URL of this LeiaMediaViewer
        /// </summary>
        /// <returns></returns>
        public string GetLeiaMediaVideoURL()
        {
            return leiaMediaVideoURL;
        }

        /// <summary>
        /// Obsolete way of setting URL. Use SetVideoURL instead of SetLeiaMediaVideoURL.
        /// </summary>
        /// <param name="absolute_path"></param>
        [System.Obsolete("This method is obsolete. Use SetVideoURL instead")]
        public void SetLeiaMediaVideoURL(string absolute_path)
        {
            SetVideoURL(absolute_path);
        }

        /// <summary>
        /// Sets the video URL of this LeiaMediaViewer
        /// </summary>
        /// <param name="absolute_path">Absolute path to a video clip outside the Unity build</param>
        public void SetVideoURL(string absolute_path)
        {
            leiaMediaVideoURL = absolute_path;
            Rebuild();
        }
        /// <summary>
        /// Sets the video URL of this LeiaMediaViewer
        /// </summary>
        /// <param name="absolute_path">Absolute path to a video clip outside the Unity build</param>
        /// <param name="rows">Leia Media rows</param>
        /// <param name="columns">Leia Media columns</param>
        public void SetVideoURL(string absolute_path, int rows, int columns)
        {
            ActiveLeiaMediaRows = rows;
            ActiveLeiaMediaCols = columns;
            SetVideoURL(absolute_path);
        }

        /// <summary>
        /// Gets state of Renderer component on this object
        /// </summary>
        /// <returns>true if Renderer attached and enabled, false otherwise</returns>
        public bool GetRendererActive()
        {
            return (mr.enabled);
        }

        /// <summary>
        /// Sets renderer enabled state
        /// </summary>
        /// <param name="status">true if enabled, false otherwise</param>
        public void SetRendererActive(bool status)
        {
            if (mr == null)
            {
                mr = GetComponent<MeshRenderer>();
            }
            mr.enabled = status;
        }

        /// <summary>
        /// Toggle MeshRenderer on/off. Most useful for toggling one Leia Media off while toggling another on,
        /// so that movie can seamlessly move from one renderer to another.
        /// </summary>
        public void ToggleRenderer()
        {
            mr.enabled = !mr.enabled;
        }

        /// <summary>
        /// Sets video clip on Leia Media
        /// </summary>
        /// <param name="video_clip">A video clip which is routed through MaterialPropertyBlock</param>
        public void SetVideoClip(VideoClip video_clip)
        {
            leiaMediaVideoClip = video_clip;
            Rebuild();
        }

        /// <summary>
        ///  Sets video clip on Leia Media 
        /// <param name="video_clip">A video clip which is routed through MaterialPropertyBlock</param>
        /// <param name="rows">Leia Media rows</param>
        /// <param name="columns">Leia Media columns</param>
        public void SetVideoClip(VideoClip video_clip, int rows, int columns)
        {
            ActiveLeiaMediaRows = rows;
            ActiveLeiaMediaCols = columns;
            SetVideoClip(video_clip);
        }


        /// <summary>
        /// Sets texture on Leia Media
        /// </summary>
        /// <param name="texture"></param>
        public void SetTexture(Texture2D texture)
        {
            if (leiaMediaTexture != texture)
            {
                leiaMediaTexture = texture;
                Rebuild();
            }
        }
        /// <summary>
        /// Sets texture on Leia Media
        /// </summary>
        /// <param name="texture">testure to apply to Leia Media</param>
        /// <param name="rows">Leia Media rows</param>
        /// <param name="columns">Leia Media columns</param>
        public void SetTexture(Texture2D texture, int rows, int columns)
        {
            ActiveLeiaMediaRows = rows;
            ActiveLeiaMediaCols = columns;
            SetTexture(texture);
        }

        /// <summary>
        /// Switches aspect ratio regulation. By default, aspect ratio is regulated by dimensions of Leia Media
        /// </summary>
        public void ToggleAspectRatioRegulation()
        {
            automaticAspectRatio = !automaticAspectRatio;
        }

        /// <summary>
        /// Sets state of aspect ratio regulation.
        /// </summary>
        /// <param name="status">true: Leia Media's local xy scale are changed to fit the media playing on it. false: Leia Media's local xy scale are not changed</param>
        public void SetAspectRatioRegulation(bool status)
        {
            automaticAspectRatio = status;
        }

        /// <summary>
        /// Retrieves aspect ratio regulation state
        /// </summary>
        /// <returns>true if aspect ratio is corrected by Leia Media, false if aspect ratio is not corrected by Leia Media</returns>
        public bool GetAspectRatioRegulation()
        {
            return (automaticAspectRatio);
        }

        /// <summary>
        /// Forces dimensions/localScale of Leia Media to be forcedx, forcedy, same z
        /// </summary>
        /// <param name="forced_aspect_ratio">(width, height)</param>
        public void ForceAspectRatio(Vector2 forced_aspect_ratio)
        {
            FixRatio(forced_aspect_ratio);
        }

        public void ProjectOntoZDP()
        {
            Transform t = null;
            LeiaCamera ideal_lc = null;

            if (Camera.main != null && Camera.main.GetComponent<LeiaCamera>() != null)
            {
                t = Camera.main.transform;
                ideal_lc = Camera.main.GetComponent<LeiaCamera>();
            }
            else if (FindObjectOfType<LeiaCamera>() != null)
            {
                t = FindObjectOfType<LeiaCamera>().transform;
                ideal_lc = t.GetComponent<LeiaCamera>();
            }
            else if (FindObjectOfType<Camera>() != null)
            {
                t = FindObjectOfType<Camera>().transform;
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning("No LeiaCamera or Camera in scene");
#else
                LogUtil.Debug("No LeiaCamera or Camera in scene.");
#endif

            }

            if (ideal_lc != null)
            {
                Vector3 error = ideal_lc.transform.position + ideal_lc.ConvergenceDistance * ideal_lc.transform.forward - transform.position;
                Vector3 BF = Vector3.Project(error, ideal_lc.transform.forward);
                transform.position = transform.position + BF;
                transform.rotation = ideal_lc.transform.rotation;
            }
            else if (t != null)
            {
                Vector3 error = t.position + 10.0f * t.forward - transform.position;
                Vector3 BF = Vector3.Project(error, t.forward);
                transform.position = t.position + BF;
                transform.rotation = t.rotation;
            }

        }

        /// <summary>
        /// Property that will always retrieve the string-ish equivalent of our active LeiaMedia
        /// </summary>
        public string ActiveLeiaMediaName
        {
            get
            {
                if (!string.IsNullOrEmpty(leiaMediaVideoURL))
                {
                    return leiaMediaVideoURL;
                }
                else if (leiaMediaVideoClip != null)
                {
                    return leiaMediaVideoClip.name;
                }
                else if (leiaMediaTexture != null)
                {
                    return leiaMediaTexture.name;
                }
                return "";
            }
        }
    }

}
