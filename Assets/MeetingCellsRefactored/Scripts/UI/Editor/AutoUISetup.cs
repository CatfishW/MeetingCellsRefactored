#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

namespace MeetingCellsRefactored.UI.Editor
{
    public static class AutoUISetup
    {
        [MenuItem("Tools/Fix GamePlayer UI Automatically")]
        public static void FixUIAutomatically()
        {
            GameObject gamePlayerUI = GameObject.Find("GamePlayerUI");
            if (gamePlayerUI == null)
            {
                Debug.LogError("[AutoUISetup] GamePlayerUI not found in scene!");
                return;
            }

            Undo.RecordObject(gamePlayerUI, "Auto Fix GamePlayer UI");

            // Reset all scales
            RectTransform[] rects = gamePlayerUI.GetComponentsInChildren<RectTransform>(true);
            foreach (var rect in rects)
            {
                Undo.RecordObject(rect, "Reset Scale");
                rect.localScale = Vector3.one;
            }

            // Colors
            Color panelColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            Color buttonColor = new Color(0.2f, 0.6f, 0.9f, 1f);

            // Fix all Images
            Image[] images = gamePlayerUI.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                Undo.RecordObject(img, "Fix Image");
                
                if (img.transform.name.Contains("Panel"))
                {
                    img.color = panelColor;
                    img.enabled = true;
                }
                else if (img.GetComponent<Button>() != null)
                {
                    img.color = buttonColor;
                    Button btn = img.GetComponent<Button>();
                    ColorBlock cb = btn.colors;
                    cb.normalColor = buttonColor;
                    cb.highlightedColor = buttonColor * 1.2f;
                    cb.pressedColor = buttonColor * 0.8f;
                    btn.colors = cb;
                }
                else if (img.transform.name.Contains("Handle"))
                {
                    img.color = new Color(1, 1, 1, 0.9f);
                }
            }

            // Fix all Texts
            TextMeshProUGUI[] texts = gamePlayerUI.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in texts)
            {
                Undo.RecordObject(text, "Fix Text");
                text.color = Color.white;
                text.alignment = TextAlignmentOptions.Center;
                if (text.transform.name.Contains("Title"))
                {
                    text.fontSize = 32;
                    text.fontStyle = FontStyles.Bold;
                }
                else if (text.transform.name.Contains("Label"))
                {
                    text.fontSize = 18;
                    text.alignment = TextAlignmentOptions.Right;
                }
                else
                {
                    text.fontSize = 20;
                }
            }

            // Fix Panels Layout
            Transform hudPanel = gamePlayerUI.transform.Find("HUDPanel");
            if (hudPanel != null)
            {
                RectTransform rt = hudPanel.GetComponent<RectTransform>();
                Undo.RecordObject(rt, "Fix HUDPanel");
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
                
                Image img = hudPanel.GetComponent<Image>();
                if (img != null) img.enabled = false;
            }

            Transform settingsPanel = gamePlayerUI.transform.Find("SettingsPanel");
            if (settingsPanel != null)
            {
                RectTransform rt = settingsPanel.GetComponent<RectTransform>();
                Undo.RecordObject(rt, "Fix SettingsPanel");
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(600, 500);
                rt.anchoredPosition = Vector2.zero;

                // Add layout group if not exists
                VerticalLayoutGroup vlg = settingsPanel.GetComponent<VerticalLayoutGroup>();
                if (vlg == null) vlg = Undo.AddComponent<VerticalLayoutGroup>(settingsPanel.gameObject);
                vlg.padding = new RectOffset(40, 40, 40, 40);
                vlg.spacing = 20;
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childControlWidth = true;
                vlg.childControlHeight = false;

                settingsPanel.gameObject.SetActive(false);
            }

            Transform mobilePanel = gamePlayerUI.transform.Find("MobileControlsPanel");
            if (mobilePanel != null)
            {
                RectTransform rt = mobilePanel.GetComponent<RectTransform>();
                Undo.RecordObject(rt, "Fix MobilePanel");
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                
                Image img = mobilePanel.GetComponent<Image>();
                if (img != null) img.enabled = false;

                // Check platform
                bool isMobile = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android ||
                               EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS;
                mobilePanel.gameObject.SetActive(isMobile);
            }

            // Fix buttons layout
            if (hudPanel != null)
            {
                Transform settingsBtn = hudPanel.Find("SettingsButton");
                if (settingsBtn != null)
                {
                    RectTransform rt = settingsBtn.GetComponent<RectTransform>();
                    Undo.RecordObject(rt, "Fix SettingsButton");
                    rt.anchorMin = new Vector2(1, 1);
                    rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(1, 1);
                    rt.sizeDelta = new Vector2(80, 80);
                    rt.anchoredPosition = new Vector2(-20, -20);
                }

                Transform camBtn = hudPanel.Find("CameraModeButton");
                if (camBtn != null)
                {
                    RectTransform rt = camBtn.GetComponent<RectTransform>();
                    Undo.RecordObject(rt, "Fix CameraButton");
                    rt.anchorMin = new Vector2(1, 1);
                    rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(1, 1);
                    rt.sizeDelta = new Vector2(80, 80);
                    rt.anchoredPosition = new Vector2(-110, -20);

                    Transform camText = camBtn.Find("CameraModeText");
                    if (camText != null)
                    {
                        RectTransform textRT = camText.GetComponent<RectTransform>();
                        Undo.RecordObject(textRT, "Fix CamText");
                        textRT.anchorMin = Vector2.zero;
                        textRT.anchorMax = Vector2.one;
                        textRT.sizeDelta = Vector2.zero;
                        textRT.anchoredPosition = Vector2.zero;
                    }
                }
            }

            // Fix Joysticks
            if (mobilePanel != null)
            {
                Transform leftJoy = mobilePanel.Find("LeftJoystick");
                if (leftJoy != null)
                {
                    RectTransform rt = leftJoy.GetComponent<RectTransform>();
                    Undo.RecordObject(rt, "Fix LeftJoystick");
                    rt.anchorMin = new Vector2(0, 0);
                    rt.anchorMax = new Vector2(0, 0);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(150, 150);
                    rt.anchoredPosition = new Vector2(120, 120);
                }

                Transform rightJoy = mobilePanel.Find("RightJoystick");
                if (rightJoy != null)
                {
                    RectTransform rt = rightJoy.GetComponent<RectTransform>();
                    Undo.RecordObject(rt, "Fix RightJoystick");
                    rt.anchorMin = new Vector2(1, 0);
                    rt.anchorMax = new Vector2(1, 0);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(150, 150);
                    rt.anchoredPosition = new Vector2(-120, 120);
                }
            }

            EditorUtility.SetDirty(gamePlayerUI);
            PrefabUtility.RecordPrefabInstancePropertyModifications(gamePlayerUI);

            Debug.Log("[AutoUISetup] UI Fixed Successfully!");
        }
    }
}
#endif
