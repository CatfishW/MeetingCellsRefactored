#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MeetingCellsRefactored.UI.Editor
{
    public class UISetupHelper : MonoBehaviour
    {
        [ContextMenu("Fix UI Setup")]
        public void FixUISetup()
        {
            // Fix all RectTransforms to have scale 1
            RectTransform[] allRects = GetComponentsInChildren<RectTransform>(true);
            foreach (var rect in allRects)
            {
                rect.localScale = Vector3.one;
            }

            // Fix panel colors
            Image[] allImages = GetComponentsInChildren<Image>(true);
            foreach (var img in allImages)
            {
                // Check if this is a panel (large image)
                if (img.rectTransform.sizeDelta.x > 500 && img.rectTransform.sizeDelta.y > 400)
                {
                    // Semi-transparent dark panel
                    img.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
                }
                else if (img.GetComponent<Button>() != null)
                {
                    // Button background
                    img.color = new Color(0.2f, 0.6f, 0.9f, 1f);
                }
                else
                {
                    // Default UI element
                    img.color = new Color(1f, 1f, 1f, 0.9f);
                }
            }

            // Fix text colors
            TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in allTexts)
            {
                text.color = Color.white;
                text.alignment = TextAlignmentOptions.Center;
            }

            // Set SettingsPanel to be inactive by default
            Transform settingsPanel = transform.Find("SettingsPanel");
            if (settingsPanel != null)
                settingsPanel.gameObject.SetActive(false);

            // Set MobileControlsPanel based on platform
            Transform mobilePanel = transform.Find("MobileControlsPanel");
            if (mobilePanel != null)
            {
#if UNITY_ANDROID || UNITY_IOS
                mobilePanel.gameObject.SetActive(true);
#else
                mobilePanel.gameObject.SetActive(false);
#endif
            }

            Debug.Log("[UISetupHelper] UI setup fixed!");
        }

        [ContextMenu("Set Panel Sizes")]
        public void SetPanelSizes()
        {
            // Set HUDPanel to full screen
            Transform hudPanel = transform.Find("HUDPanel");
            if (hudPanel != null)
            {
                RectTransform rt = hudPanel.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
            }

            // Set SettingsPanel to centered
            Transform settingsPanel = transform.Find("SettingsPanel");
            if (settingsPanel != null)
            {
                RectTransform rt = settingsPanel.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(600, 500);
                rt.anchoredPosition = Vector2.zero;
            }

            // Set MobileControlsPanel to full screen
            Transform mobilePanel = transform.Find("MobileControlsPanel");
            if (mobilePanel != null)
            {
                RectTransform rt = mobilePanel.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
            }

            Debug.Log("[UISetupHelper] Panel sizes set!");
        }
    }
}
#endif
