#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

namespace MeetingCellsRefactored.UI.Editor
{
    public class GamePlayerUISetupWindow : EditorWindow
    {
        private GameObject gamePlayerUI;
        private Color panelColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        private Color buttonColor = new Color(0.2f, 0.6f, 0.9f, 1f);
        private Color accentColor = new Color(0.95f, 0.3f, 0.3f, 1f);

        [MenuItem("Window/GamePlayer UI Setup")]
        public static void ShowWindow()
        {
            GetWindow<GamePlayerUISetupWindow>("GamePlayer UI Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("GamePlayer UI Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            gamePlayerUI = EditorGUILayout.ObjectField("GamePlayerUI Object", gamePlayerUI, typeof(GameObject), true) as GameObject;

            EditorGUILayout.Space();
            GUILayout.Label("Color Theme", EditorStyles.boldLabel);
            panelColor = EditorGUILayout.ColorField("Panel Color", panelColor);
            buttonColor = EditorGUILayout.ColorField("Button Color", buttonColor);
            accentColor = EditorGUILayout.ColorField("Accent Color", accentColor);

            EditorGUILayout.Space();

            if (GUILayout.Button("Complete UI Setup", GUILayout.Height(40)))
            {
                if (gamePlayerUI != null)
                {
                    SetupAllUI();
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Please assign the GamePlayerUI object first!", "OK");
                }
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Fix Layouts Only"))
            {
                if (gamePlayerUI != null) SetupLayouts();
            }

            if (GUILayout.Button("Fix Colors Only"))
            {
                if (gamePlayerUI != null) SetupColors();
            }

            if (GUILayout.Button("Reset Scales"))
            {
                if (gamePlayerUI != null) ResetScales();
            }
        }

        private void SetupAllUI()
        {
            Undo.RecordObject(gamePlayerUI, "Setup GamePlayer UI");

            ResetScales();
            SetupLayouts();
            SetupColors();
            SetupText();
            SetupHierarchy();

            EditorUtility.SetDirty(gamePlayerUI);
            PrefabUtility.RecordPrefabInstancePropertyModifications(gamePlayerUI);

            Debug.Log("[GamePlayerUISetup] UI setup complete!");
        }

        private void ResetScales()
        {
            RectTransform[] rects = gamePlayerUI.GetComponentsInChildren<RectTransform>(true);
            foreach (var rect in rects)
            {
                Undo.RecordObject(rect, "Reset Scale");
                rect.localScale = Vector3.one;
            }
        }

        private void SetupLayouts()
        {
            // Main Canvas
            RectTransform canvasRT = gamePlayerUI.GetComponent<RectTransform>();
            Undo.RecordObject(canvasRT, "Setup Canvas");
            canvasRT.anchorMin = Vector2.zero;
            canvasRT.anchorMax = Vector2.one;
            canvasRT.sizeDelta = Vector2.zero;

            // HUD Panel - Full screen, no visuals
            Transform hudPanel = gamePlayerUI.transform.Find("HUDPanel");
            if (hudPanel != null)
            {
                RectTransform rt = hudPanel.GetComponent<RectTransform>();
                Undo.RecordObject(rt, "Setup HUDPanel");
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;

                Image img = hudPanel.GetComponent<Image>();
                if (img != null) img.enabled = false;
            }

            // Settings Panel - Centered modal
            Transform settingsPanel = gamePlayerUI.transform.Find("SettingsPanel");
            if (settingsPanel != null)
            {
                RectTransform rt = settingsPanel.GetComponent<RectTransform>();
                Undo.RecordObject(rt, "Setup SettingsPanel");
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(600, 550);
                rt.anchoredPosition = Vector2.zero;

                // Add VerticalLayoutGroup
                VerticalLayoutGroup vlg = settingsPanel.GetComponent<VerticalLayoutGroup>();
                if (vlg == null) vlg = Undo.AddComponent<VerticalLayoutGroup>(settingsPanel.gameObject);

                vlg.padding = new RectOffset(30, 30, 30, 30);
                vlg.spacing = 15;
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childControlWidth = true;
                vlg.childControlHeight = false;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;

                // Add ContentSizeFitter
                ContentSizeFitter csf = settingsPanel.GetComponent<ContentSizeFitter>();
                if (csf == null) csf = Undo.AddComponent<ContentSizeFitter>(settingsPanel.gameObject);
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                settingsPanel.gameObject.SetActive(false);
            }

            // Mobile Controls Panel - Full screen overlay
            Transform mobilePanel = gamePlayerUI.transform.Find("MobileControlsPanel");
            if (mobilePanel != null)
            {
                RectTransform rt = mobilePanel.GetComponent<RectTransform>();
                Undo.RecordObject(rt, "Setup MobilePanel");
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;

                Image img = mobilePanel.GetComponent<Image>();
                if (img != null) img.enabled = false;

                // Left side layout
                GameObject leftSide = mobilePanel.Find("LeftSide")?.gameObject;
                if (leftSide == null)
                {
                    leftSide = new GameObject("LeftSide", typeof(RectTransform));
                    leftSide.transform.SetParent(mobilePanel, false);
                    Undo.RegisterCreatedObjectUndo(leftSide, "Create LeftSide");
                }

                RectTransform leftRT = leftSide.GetComponent<RectTransform>();
                leftRT.anchorMin = new Vector2(0, 0);
                leftRT.anchorMax = new Vector2(0.5f, 1);
                leftRT.sizeDelta = Vector2.zero;
                leftRT.anchoredPosition = Vector2.zero;

                // Right side layout
                GameObject rightSide = mobilePanel.Find("RightSide")?.gameObject;
                if (rightSide == null)
                {
                    rightSide = new GameObject("RightSide", typeof(RectTransform));
                    rightSide.transform.SetParent(mobilePanel, false);
                    Undo.RegisterCreatedObjectUndo(rightSide, "Create RightSide");
                }

                RectTransform rightRT = rightSide.GetComponent<RectTransform>();
                rightRT.anchorMin = new Vector2(0.5f, 0);
                rightRT.anchorMax = new Vector2(1, 1);
                rightRT.sizeDelta = Vector2.zero;
                rightRT.anchoredPosition = Vector2.zero;

                bool isMobile = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android ||
                               EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS;
                mobilePanel.gameObject.SetActive(isMobile);
            }

            // Position buttons in HUD
            SetupHUDButtons(hudPanel);

            // Position mobile controls
            SetupMobileControls(mobilePanel);
        }

        private void SetupHUDButtons(Transform hudPanel)
        {
            if (hudPanel == null) return;

            // Top-right corner container
            GameObject topRight = hudPanel.Find("TopRight")?.gameObject;
            if (topRight == null)
            {
                topRight = new GameObject("TopRight", typeof(RectTransform));
                topRight.transform.SetParent(hudPanel, false);
                Undo.RegisterCreatedObjectUndo(topRight, "Create TopRight");
            }

            RectTransform trRT = topRight.GetComponent<RectTransform>();
            trRT.anchorMin = new Vector2(1, 1);
            trRT.anchorMax = new Vector2(1, 1);
            trRT.pivot = new Vector2(1, 1);
            trRT.sizeDelta = new Vector2(200, 100);
            trRT.anchoredPosition = new Vector2(-20, -20);

            // Add HorizontalLayoutGroup
            HorizontalLayoutGroup hlg = topRight.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = Undo.AddComponent<HorizontalLayoutGroup>(topRight);
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleRight;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            // Move buttons to topRight
            Transform settingsBtn = hudPanel.Find("SettingsButton");
            if (settingsBtn != null) settingsBtn.SetParent(topRight.transform, false);

            Transform camBtn = hudPanel.Find("CameraModeButton");
            if (camBtn != null) camBtn.SetParent(topRight.transform, false);
        }

        private void SetupMobileControls(Transform mobilePanel)
        {
            if (mobilePanel == null) return;

            Transform leftSide = mobilePanel.Find("LeftSide");
            Transform rightSide = mobilePanel.Find("RightSide");

            // Position left joystick
            Transform leftJoystick = mobilePanel.Find("LeftJoystick");
            if (leftJoystick != null && leftSide != null)
            {
                leftJoystick.SetParent(leftSide, false);
                RectTransform rt = leftJoystick.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(0, 0);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(150, 150);
                rt.anchoredPosition = new Vector2(100, 100);
            }

            // Position right joystick
            Transform rightJoystick = mobilePanel.Find("RightJoystick");
            if (rightJoystick != null && rightSide != null)
            {
                rightJoystick.SetParent(rightSide, false);
                RectTransform rt = rightJoystick.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1, 0);
                rt.anchorMax = new Vector2(1, 0);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(150, 150);
                rt.anchoredPosition = new Vector2(-100, 100);
            }

            // Create buttons container on right side
            GameObject buttonsContainer = rightSide?.Find("ButtonsContainer")?.gameObject;
            if (buttonsContainer == null && rightSide != null)
            {
                buttonsContainer = new GameObject("ButtonsContainer", typeof(RectTransform));
                buttonsContainer.transform.SetParent(rightSide, false);
                Undo.RegisterCreatedObjectUndo(buttonsContainer, "Create ButtonsContainer");
            }

            if (buttonsContainer != null)
            {
                RectTransform bcRT = buttonsContainer.GetComponent<RectTransform>();
                bcRT.anchorMin = new Vector2(0.5f, 0);
                bcRT.anchorMax = new Vector2(1, 0.5f);
                bcRT.sizeDelta = Vector2.zero;
                bcRT.anchoredPosition = Vector2.zero;

                GridLayoutGroup glg = buttonsContainer.GetComponent<GridLayoutGroup>();
                if (glg == null) glg = Undo.AddComponent<GridLayoutGroup>(buttonsContainer);
                glg.cellSize = new Vector2(80, 80);
                glg.spacing = new Vector2(10, 10);
                glg.startCorner = GridLayoutGroup.Corner.UpperRight;
                glg.startAxis = GridLayoutGroup.Axis.Horizontal;
                glg.childAlignment = TextAnchor.MiddleCenter;

                // Move mobile buttons to container
                Transform jumpBtn = mobilePanel.Find("MobileJumpButton");
                if (jumpBtn != null) jumpBtn.SetParent(buttonsContainer.transform, false);

                Transform sprintBtn = mobilePanel.Find("MobileSprintButton");
                if (sprintBtn != null) sprintBtn.SetParent(buttonsContainer.transform, false);

                Transform interactBtn = mobilePanel.Find("MobileInteractButton");
                if (interactBtn != null) interactBtn.SetParent(buttonsContainer.transform, false);
            }
        }

        private void SetupColors()
        {
            Image[] images = gamePlayerUI.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                Undo.RecordObject(img, "Setup Color");

                if (img.transform.name.Contains("Panel"))
                {
                    img.color = panelColor;
                    img.enabled = true;
                }
                else if (img.GetComponent<Button>() != null)
                {
                    img.color = buttonColor;

                    // Setup button colors
                    Button btn = img.GetComponent<Button>();
                    ColorBlock cb = btn.colors;
                    cb.normalColor = buttonColor;
                    cb.highlightedColor = buttonColor * 1.2f;
                    cb.pressedColor = buttonColor * 0.8f;
                    cb.disabledColor = buttonColor * 0.5f;
                    btn.colors = cb;
                }
                else if (img.transform.name.Contains("Handle"))
                {
                    img.color = Color.white;
                }
                else
                {
                    img.color = Color.white;
                }
            }
        }

        private void SetupText()
        {
            TextMeshProUGUI[] texts = gamePlayerUI.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in texts)
            {
                Undo.RecordObject(text, "Setup Text");
                text.color = Color.white;
                text.alignment = TextAlignmentOptions.Center;
                text.fontSize = 24;

                if (text.transform.name.Contains("Title"))
                {
                    text.fontSize = 36;
                    text.fontStyle = FontStyles.Bold;
                }
                else if (text.transform.name.Contains("Label"))
                {
                    text.fontSize = 18;
                    text.alignment = TextAlignmentOptions.Left;
                }
            }
        }

        private void SetupHierarchy()
        {
            // Ensure SettingsPanel is last (on top)
            Transform settingsPanel = gamePlayerUI.transform.Find("SettingsPanel");
            if (settingsPanel != null)
            {
                settingsPanel.SetAsLastSibling();
            }

            // Ensure HUDPanel is first
            Transform hudPanel = gamePlayerUI.transform.Find("HUDPanel");
            if (hudPanel != null)
            {
                hudPanel.SetAsFirstSibling();
            }
        }
    }
}
#endif
