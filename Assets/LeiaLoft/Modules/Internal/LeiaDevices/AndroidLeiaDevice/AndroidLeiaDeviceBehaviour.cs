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
#if UNITY_ANDROID || UNITY_EDITOR
using UnityEngine;

namespace LeiaLoft
{
    [RequireComponent(typeof(LeiaDisplay))]
    public class AndroidLeiaDeviceBehaviour : MonoBehaviour
    {
        private bool _started = false;

        void OnEnable()
        {
            if (!_started) 
            {
                return;
            }

            RegisterDevice();
        }

        void Start()
        {
            RegisterDevice();
            _started = true;
        }

        private void RegisterDevice()
        {
#if !UNITY_EDITOR
            var device = new AndroidLeiaDevice(LeiaDisplay.Instance.ProfileStubName);

            if (LeiaDisplay.Instance.DeviceFactory.RegisterLeiaDevice(device))
            {
                LeiaDisplay.Instance.UpdateDevice();
            }
#endif
        }

    }
}
#endif
