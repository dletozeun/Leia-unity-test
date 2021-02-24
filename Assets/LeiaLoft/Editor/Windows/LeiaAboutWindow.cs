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
using UnityEditor;
using UnityEngine;

namespace LeiaLoft.Editor
{

    [InitializeOnLoad]
    public class LeiaAboutWindow : UnityEditor.EditorWindow
    {

        const string BannerAssetFilename = "LeiaLoftSDK";
        const string editor_About_ForcePopUp = "LeiaLoft.About.ForcePopUp";

        static LeiaWelcomeWindow welcomeWindow;
        static LeiaReleaseNotesWindow releaseNotesWindow;
        static LeiaLogSettingsWindow logSettingsWindow;
        static LeiaAboutWindow window;
        private enum Page { Welcome, Release_Notes, Log_Settings };
        private static Page _page = Page.Welcome;

        static string[] pageNames;
        private static bool _isInitialized = false, forceShow = true;

        private static Texture2D _bannerImage;
        private static Vector2 scrollPosition;
        static GUIStyle centeredStyle;
        static LeiaAboutWindow()
        {
            EditorApplication.update += Update;
        }
        static void Update()
        {
            if (ShouldForceWindowPopUp())
            {
                Open();
            }
        }
        static bool ShouldForceWindowPopUp()
        {
            forceShow = EditorPrefs.GetBool(editor_About_ForcePopUp, false);
            if (!forceShow)
            {
                return false;
            }
            if (_isInitialized)
            {
                return false;
            }
            return true;
        }
        private void OnDestroy()
        {
            _page = Page.Welcome;
        }

        [MenuItem("LeiaLoft/About &l")]
        public static void Open()
        {
            InitilizeWindowTabs();
            _bannerImage = Resources.Load<Texture2D>(BannerAssetFilename);
            pageNames = Enum.GetNames(typeof(Page));
            for (int i = 0; i < pageNames.Length; i++)
            {
                pageNames[i] = pageNames[i].Replace('_', ' ');
            }
            window = GetWindow<LeiaAboutWindow>(true, "About LeiaLoft SDK");
            window.minSize = EditorWindowUtils.WindowMinSize;
            _isInitialized = true;
        }

        private static void InitilizeWindowTabs()
        {
            Type aboutWindowType = typeof(LeiaAboutWindow);
            releaseNotesWindow = GetWindow<LeiaReleaseNotesWindow>("LeiaLoft SDK Release Notes", false, aboutWindowType);
            releaseNotesWindow.Close();

            logSettingsWindow = GetWindow<LeiaLogSettingsWindow>("LeiaLoft SDK Log Settings", false, aboutWindowType);
            logSettingsWindow.Close();

            welcomeWindow = GetWindow<LeiaWelcomeWindow>("Welcome To LeiaLoft SDK", false, aboutWindowType);
            welcomeWindow.Close();
        }
        private void Title()
        {
            var versionAsset = Resources.Load<TextAsset>("VERSION");
            EditorWindowUtils.TitleTexture(_bannerImage);

            if (versionAsset == null)
            {
                return;
            }
            if (centeredStyle == null)
            {
                centeredStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16 };
            }
            EditorWindowUtils.Space(5);
            EditorWindowUtils.Label(String.Format("Version: {0}", versionAsset.text), centeredStyle);

            if (pageNames == null) //use case: About Window is open while entering Play
            {
                Open();
            }
            EditorWindowUtils.BeginHorizontalCenter();
            EditorWindowUtils.Button(() => { GetWindow<LeiaRecommendedSettings>(true); }, "Leia Recommended Settings");
            EditorWindowUtils.EndHorizontalCenter();
            EditorWindowUtils.Space(10);
            _page = (Page)GUILayout.Toolbar((int)_page, pageNames);
            EditorWindowUtils.HorizontalLine();
            EditorWindowUtils.Space(5);
        }

        private void OnGUI()
        {
            Title();
            EditorWindowUtils.BeginVertical();
            scrollPosition = EditorWindowUtils.BeginScrollView(scrollPosition);
            switch (_page)
            {
                case Page.Welcome:
                    welcomeWindow.OnGUI();
                    break;
                case Page.Release_Notes:
                    releaseNotesWindow.OnGUI();
                    break;
                case Page.Log_Settings:
                    logSettingsWindow.OnGUI();
                    break;
                default:
                    welcomeWindow.OnGUI();
                    break;
            }
            EditorWindowUtils.EndScrollView();
            EditorWindowUtils.EndVertical();
            EditorWindowUtils.Space(10);
            UndoableInputFieldUtils.BoolFieldWithTooltip(() => { forceShow = EditorPrefs.GetBool(editor_About_ForcePopUp, false); return forceShow; }, b => { forceShow = b; EditorPrefs.SetBool(editor_About_ForcePopUp, b); }, "  Automatically Pop-up", "Display this window when opening Unity. Alternatively, this widow can be opened from LeiaLoft-> About", window);
            EditorWindowUtils.Space(10);
        }
    }
}
