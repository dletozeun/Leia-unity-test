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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.IO;

namespace LeiaLoft
{
    /// <summary>
    /// Enables Leia devices to work with Unity Remote 5 for previewing content.
    /// </summary>
    public class LeiaRemote : MonoBehaviour
    {

#if UNITY_EDITOR && UNITY_ANDROID
        string adbPath;
        string sdkPath;
#endif

        void Start()
        {
#if UNITY_EDITOR && UNITY_ANDROID
            // Switch Backlight to 4V mode
            StartCoroutine(startSwitch());
#endif
        }

    private void Awake()
        {
#if UNITY_EDITOR && UNITY_ANDROID
            sdkPath = UnityEditor.EditorPrefs.GetString("AndroidSdkRoot");
            if (!string.IsNullOrEmpty(sdkPath))
            {
                adbPath = Path.GetFullPath(sdkPath) + Path.DirectorySeparatorChar + "platform-tools" + Path.DirectorySeparatorChar + "adb";

                if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                    adbPath = Path.ChangeExtension(adbPath, "exe");
                }
            }

            startLeiaRemote();

            // Reset Backlight to 2D
            switch2D();

#endif
        }

        private void OnDestroy()
        {
            switch2D();
        }

        private void switch4V()
        {
            executeADB("shell curl -X POST http://127.0.0.1:8005?4v=1");
        }

        private IEnumerator startSwitch()
        {
            yield return new WaitForSeconds(1f);
            switch4V();
        }

        private void startLeiaRemote()
        {
            executeADB("shell am start -n com.leia.remote/com.leia.remote.MainActivity");
            executeADB("shell am start -n com.unity3d.genericremote/com.unity3d.player.UnityPlayerActivity");
        }

        private void switch2D()
        {
            executeADB("shell curl -X POST http://127.0.0.1:8005?2d=1");
        }

        private void executeADB(string command)
        {
#if UNITY_EDITOR && UNITY_ANDROID
            var process = new System.Diagnostics.Process();

            var startInfo = new System.Diagnostics.ProcessStartInfo(adbPath, command)
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            process.StartInfo = startInfo;
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            process.Close();
            process.Dispose();
#endif
        }
    }
}