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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Globalization;

namespace LeiaLoft
{

    public sealed class LeiaPostEffectsController : MonoBehaviour
    {
        private LeiaCamera _leiaCamera;
        private List<MonoBehaviour> _effects = new List<MonoBehaviour>();
        private int _lastViewCount = 0;
        private readonly string resetProjectionMatrixURL = "https://docs.leialoft.com/developer/unity-sdk/troubleshooting/render-pipeline-support/issue-leialoft-unity-ppsv2-camera-projection-reset";

        public void Update()
        {
            if (_leiaCamera == null)
            {
                _leiaCamera = GetComponent<LeiaCamera>();
                if (_leiaCamera == null)
                {
                    DestroyScript(this);
                    return;
                }
            }

            if (LeiaDisplay.InstanceIsNull)
            {
                return;
            }

            if (IsEffectsChanged())
            {
                CopyEffectsToLeiaViews();
                StartCoroutine(CheckProjectionMatrixReset());
            }
        }
        /// <summary>
        /// This check specifically deals with Post Processing Stack version 2 issue, where PostProcessLayer.cs 
        /// resets all camera projection matrices in OnPreCull(), breaking Leia interlacing. This is a known bug in Unity PPSv2.
        /// </summary>
        /// <returns></returns>
        IEnumerator CheckProjectionMatrixReset()
        {
            yield return new WaitForEndOfFrame();
            
            if (IsProjectionReset())
            {
                Debug.Break();
                Debug.LogError("Issue Recognized: PPSv2 Projection Matrix Reset with LeiaLoft: Opening Online Documentation..." + resetProjectionMatrixURL);
                if (resetProjectionMatrixURL.Length > 0)
                {
                    Application.OpenURL(resetProjectionMatrixURL);
                }
            }
        }
        bool IsProjectionReset()
        {
#if !UNITY_EDITOR
            return false;
#else
            if (_effects.Count < 1)
            {
                return false;
            }
            if (Mathf.Approximately(_leiaCamera.BaselineScaling, 0))
            {
                return false;
            }

            if (LeiaDisplay.Instance.ActualLightfieldMode == LeiaDisplay.LightfieldMode.Off)
            {
                return false;
            }

            for (int i = 0; i < _leiaCamera.GetViewCount(); i++)
            {
                //m02 only appears to be 0 when the projection matrix has been reset
                if (!Mathf.Approximately(_leiaCamera.GetView(i).Matrix.m02, 0))
                {
                    return false;
                }
            }

            return true;
#endif
        }
        public void ForceUpdate()
        {
            this.Debug("ForceUpdate");

            if (_leiaCamera == null)
            {
                _leiaCamera = GetComponent<LeiaCamera>();
            }

            if (LeiaDisplay.InstanceIsNull)
            {
                return;
            }

            IsEffectsChanged();
            CopyEffectsToLeiaViews();
        }

        private bool IsEffectsChanged()
        {
            bool isEffectsChanged = false;


            //Views count changed
            if (_lastViewCount != _leiaCamera.GetViewCount())
            {
                _lastViewCount = _leiaCamera.GetViewCount();
                isEffectsChanged = true;
            }

            //Effects removed
            for (int i = 0; i < _effects.Count; i++)
            {
                if (_effects[i] == null)
                {
                    _effects.RemoveAt(i);
                    i--;
                    isEffectsChanged = true;
                }
            }

            //Effects added or enabled
            foreach (var effect in GetEffectsBehaviors(gameObject))
            {
                if (effect.enabled)
                {
                    if (!_effects.Contains(effect))
                    {
                        _effects.Add(effect);
                        isEffectsChanged = true;
                    }
                }
            }

            //Effects disabled
            for (int i = 0; i < _effects.Count; i++)
            {
                if (!_effects[i].enabled)
                {
                    _effects.Remove(_effects[i]);
                    isEffectsChanged = true;
                    i--;
                }
            }

            return isEffectsChanged;
        }

        public void RestoreEffects()
        {
            this.Debug("RestoreEffects");
            _effects.Clear();

            if (_leiaCamera == null)
            {
                _leiaCamera = GetComponent<LeiaCamera>();
            }

            RemoveEffectsFromLeiaViews();
        }

        private void RemoveEffectsFromLeiaViews()
        {
            for (int i = 0; i < _leiaCamera.GetViewCount(); i++)
            {
                RemoveEffects(GetEffectsBehaviors(_leiaCamera.GetView(i).Object));
            }
        }

        private void CopyEffectsToLeiaViews()
        {
            for (int i = 0; i < _leiaCamera.GetViewCount(); i++)
            {
                CopyEffectsToView(_effects, _leiaCamera.GetView(i));
            }
        }

        private static void CopyEffectsToView(List<MonoBehaviour> effects, LeiaView view)
        {
            if (view == null || view.Object == null)
            {
                return;
            }

            var oldEffects = GetEffectsBehaviors(view.Object);

            foreach (var effect in effects)
            {
                if (effect == null)
                {
                    continue;
                }

                Component copy = null;

                for (int j = 0; j < oldEffects.Count; j++)
                {
                    if (effect.GetType() == oldEffects[j].GetType())
                    {
                        copy = oldEffects[j];
                        oldEffects.RemoveAt(j);
                        break;
                    }
                }

                if (copy == null)
                {
                    copy = view.Object.AddComponent(effect.GetType());
                }

                if (copy != null)
                {
                    var fields = effect.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    for (int j = 0; j < fields.Length; j++)
                    {
                        if (fields[j].FieldType == typeof(Camera) && (fields[j].GetValue(effect) as Camera) == effect.GetComponent<Camera>())
                        {
                            // LeiaView GameObjects have their own Camera that should replace references to the root object's Camera
                            fields[j].SetValue(copy, view.Camera);
                        }
                        else if (fields[j].FieldType == typeof(UnityEngine.Rendering.CommandBuffer))
                        {
                            // exclude case where property is a CommandBuffer; CB fields should be managed by the Component which has a ref to the CB
                            continue;
                        }
                        else if (fields[j].FieldType == typeof(LeiaView))
                        {
                            // when a MonoBehaviour which is being copied from root cam to LeiaView has a ref to a LeiaView, set the LeiaView ref
                            fields[j].SetValue(copy, view);
                        }
                        else
                        {
                            // most general case - copy component's property's value from LeiaCamera to LeiaView
                            fields[j].SetValue(copy, fields[j].GetValue(effect));
                        }
                    }

                    //Generate onEnable
                    var behavior = copy as MonoBehaviour;
                    behavior.enabled = false;
                    behavior.enabled = true;
                }
            }

            RemoveEffects(oldEffects);
        }

        private static void RemoveEffects(List<MonoBehaviour> effects)
        {
            foreach (var effect in effects)
            {
                DestroyScript(effect);
            }
        }

        private static List<MonoBehaviour> GetEffectsBehaviors(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return new List<MonoBehaviour>();
            }

            var methodFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            var effects = new List<MonoBehaviour>();
            foreach (var item in gameObject.GetComponents<MonoBehaviour>())
            {
                if (!item)
                {
                    continue;
                }
                if (item.GetType().GetMethod("OnRenderImage", methodFlags) != null ||
                    item.GetType().GetMethod("OnPostRender", methodFlags) != null ||
                    item.GetType().GetMethod("OnPreRender", methodFlags) != null)
                {
                    effects.Add(item);
                }
            }

            return effects;
        }

        private static void DestroyScript(Object o)
        {
            if (!Application.isPlaying)
            {
                DestroyImmediate(o);
            }
            else
            {
                Destroy(o);
            }
        }
    }
}