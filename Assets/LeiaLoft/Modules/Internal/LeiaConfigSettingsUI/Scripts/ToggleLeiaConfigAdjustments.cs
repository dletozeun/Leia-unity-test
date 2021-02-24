using UnityEngine;
using UnityEngine.EventSystems;

namespace LeiaLoft
{
    public class ToggleLeiaConfigAdjustments : MonoBehaviour
    {
#pragma warning disable 0649
        [SerializeField]
        private GameObject leiaConfigSettingsPanel;
        [SerializeField]
        private KeyCode key1Option1, key1Option2, key2;
        [SerializeField]
        private int mobileTapCount = 3;

        private void OnEnable()
        {
            EventSystem[] eventSystems = FindObjectsOfType<EventSystem>();
            if (eventSystems.Length == 0)
            {
                GameObject obj = new GameObject("EventSystem", typeof(EventSystem));
                obj.transform.parent = null;
                obj.AddComponent<StandaloneInputModule>();
            }
        }
        void Update()
        {
            if ((Input.GetKey(key1Option1) || Input.GetKey(key1Option2)) && Input.GetKeyDown(key2)//standalone
                || (Input.touchCount == mobileTapCount && Input.GetTouch(0).phase == TouchPhase.Began))//mobile
            {
                leiaConfigSettingsPanel.SetActive(!leiaConfigSettingsPanel.activeSelf);
            }
        }
    }
}
