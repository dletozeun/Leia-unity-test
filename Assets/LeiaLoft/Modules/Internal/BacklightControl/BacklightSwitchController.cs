using UnityEngine;

namespace LeiaLoft
{
    /// <summary>
    /// Leia backlight switch MonoBehaviour. Performs no action if Unity Editor version earlier than 2018.1.
    /// Preferentially uses tasks in 2019.1+.
    /// Also supports opening execution log through keypress on Windows. Backlight switching and need for
    /// error logs tend to go hand-in-hand.
    /// </summary>
    public class BacklightSwitchController : UnityEngine.MonoBehaviour
    {
#pragma warning disable 0649
        [SerializeField] private KeyCode Key2D = KeyCode.F2;
        [SerializeField] private KeyCode Key3D = KeyCode.F3;
#pragma warning restore 0649

        void Update()
        {
            if (Input.GetKeyDown(Key2D))
            {
                if (!LeiaDisplay.InstanceIsNull)
                {
                    LeiaDisplay.Instance.DesiredLightfieldMode = LeiaDisplay.LightfieldMode.Off;
                }
            }
            if (Input.GetKeyDown(Key3D))
            {
                if (!LeiaDisplay.InstanceIsNull)
                {
                    LeiaDisplay.Instance.DesiredLightfieldMode = LeiaDisplay.LightfieldMode.On;
                }
            }
        }

        /// <summary>
        /// Application developer access to backlight API.
        /// </summary>
        /// <param name="mode">2D: backlight off, 3D: backlight on</param>
        public static void ApplicationRequestBacklight(string mode)
        {
            if (string.IsNullOrEmpty(mode))
            {
                Debug.LogWarningFormat("ApplicationRequestBacklight has empty param");
                return;
            }

            switch (mode.ToLower())
            {
                case "2d":
                    if (!LeiaDisplay.InstanceIsNull)
                    {
                        LeiaDisplay.Instance.DesiredLightfieldMode = LeiaDisplay.LightfieldMode.Off;
                    }
                    break;
                case "3d":
                    if (!LeiaDisplay.InstanceIsNull)
                    {
                        LeiaDisplay.Instance.DesiredLightfieldMode = LeiaDisplay.LightfieldMode.On;
                    }
                    break;
                default:
                    LogUtil.Log(LogLevel.Warning, "ApplicationRequestBacklight mode not recognized: {0}", mode);
                    break;
            }
        }

    }
}
