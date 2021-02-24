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

namespace LeiaLoft
{
    /// <summary>
    /// Returns proper ILeiaDevice implementation based on platform and connectivity
    /// </summary>
    public class LeiaDeviceFactory
    {
        private AbstractLeiaDevice _leiaDevice;

        public bool RegisterLeiaDevice(AbstractLeiaDevice device)
        {
            this.Debug("Registered leia device: " + device.GetType());
            _leiaDevice = device;
            return true;
        }

        public void UnregisterLeiaDevice()
        {
            this.Debug("Unregister LeiaDevice");
            _leiaDevice = null;
        }

        public ILeiaDevice GetDevice(string stubName)
        {
            this.Debug(string.Format("GetDevice( {0})", stubName));
            if (_leiaDevice != null)
            {
                _leiaDevice.SetProfileStubName(stubName);
                return _leiaDevice;
            }
            _leiaDevice = new OfflineEmulationLeiaDevice(stubName);
            return _leiaDevice;
        }
    }
}
