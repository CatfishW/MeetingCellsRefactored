#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using System.Collections.Generic;

namespace MeetingCellsRefactored.UI.Editor
{
    /// <summary>
    /// Sets up GamePlayerUI with existing texture assets
    /// </summary>
    public class GamePlayerUIAssetSetup : EditorWindow
    {
        private GameObject gamePlayerUI;
        private bool createMissingPanels = true;
        private bool assignSourceImages = true;
        private bool setupTextLabels = true;

        // Texture asset paths
        private const string PANEL_BG_PATH = "Assets/MeetingCellsRefactored/Textures/square128_round15px_fill.png";
        private const string BUTTON_BG_PATH = "Assets/MeetingCellsRefactored/Textures/Btn_Square01_White.png";
        private const string CLOSE_BUTTON_PATH = "Assets/MeetingCellsRefactored/Textures/btn_red_close.png";
        private const string SETTINGS_ICON_PATH = "Assets/MeetingCellsRefactored/Textures/icon_settings.png";
        private const string JOYSTICK_BG_PATH = "Assets/MeetingCellsRefactored/Textures/circle128_fill.png";
        private const string CAMERA_ICON_PATH = "Assets/MeetingCellsRefactored/Textures/3rdPOV.png";
        private const string MENU_BG_PATH = "Assets/MeetingCellsRefactored/Textures/menu_bg.png";

        [MenuItem("Window/GamePlayer UI Asset Setup")]
        public static void ShowWindow()
        {
            GetWindow<GamePlayerUIAssetSetup>("GamePlayer UI Assets");
        }

        private void OnGUI()
        {
            GUILayout.Label("GamePlayer UI Asset Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            gamePlayerUI = EditorGUILayout.ObjectField("GamePlayerUI Object", gamePlayerUI, typeof(GameObject), true) as GameObject;
            
            EditorGUILayout.Space();
            createMissingPanels = EditorGUILayout.Toggle("Create Missing Panels", createMissingPanels);
            assignSourceImages = EditorGUILayout.Toggle("Assign Source Images", assignSourceImages);
            setupTextLabels = EditorGUILayout.Toggle("Setup Text Labels", setupTextLabels);

            EditorGUILayout.Space();

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Setup UI Assets", GUILayout.Height(40)))
            {
                if (gamePlayerUI != null)
                {
                    SetupUI();
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Please assign the GamePlayerUI object first!", "OK");
                }
            }
            GUI.backgroundColor = Color.white;
        }

        private void SetupUI()
        {
            Undo.RecordObject(gamePlayerUI, "Setup GamePlayerUI Assets");

            if (createMissingPanels)
            {
                CreateMissingPanels();
            }

            if (assignSourceImages)
            {
                AssignSourceImages();
            }

            if (setupTextLabels)
            {
                SetupTextLabels();
            }

            EditorUtility.SetDirty(gamePlayerUI);
            Debug.Log("[GamePlayerUIAssetSetup] UI asset setup complete!");
        }

        private void CreateMissingPanels()
        {
            // Get or create HUDPanel
            Transform hudPanel = gamePlayerUI.transform.Find("HUDPanel");
            if (hudPanel == null)
            {
                hudPanel = CreatePanel("HUDPanel", gamePlayerUI.transform, Vector2.zero, Vector2.one);
                var image = hudPanel.GetComponent<Image>();
                image.enabled = false; // HUD is invisible, just a container
            }

            // Get or create SettingsPanel
            Transform settingsPanel = gamePlayerUI.transform.Find("SettingsPanel");
            if (settingsPanel == null)
            {
                settingsPanel = CreatePanel("SettingsPanel", gamePlayerUI.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                settingsPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 550);
                
                // Add vertical layout
                var vlg = settingsPanel.gameObject.AddComponent<VerticalLayoutGroup>();
                vlg.padding = new RectOffset(30, 30, 30, 30);
                vlg.spacing = 15;
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childControlWidth = true;
                vlg.childControlHeight = false;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;

                // Add content size fitter
                var csf = settingsPanel.gameObject.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                CreateSettingsContent(settingsPanel);
                settingsPanel.gameObject.SetActive(false);
            }

            // Get or create MobileControlsPanel
            Transform mobilePanel = gamePlayerUI.transform.Find("MobileControlsPanel");
            if (mobilePanel == null)
            {
                mobilePanel = CreatePanel("MobileControlsPanel", gamePlayerUI.transform, Vector2.zero, Vector2.one);
                var image = mobilePanel.GetComponent<Image>();
                image.enabled = false; // Invisible container
                
                CreateMobileControls(mobilePanel);
                
                // Only active on mobile
                #if UNITY_ANDROID || UNITY_IOS
                mobilePanel.gameObject.SetActive(true);
                #else
                mobilePanel.gameObject.SetActive(false);
                #endif
            }

            // Create TopRight container in HUD
            Transform topRight = hudPanel.Find("TopRight");
            if (topRight == null)
            {
                topRight = CreateUIObject("TopRight", hudPanel);
                var rt = topRight.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(1, 1);
                rt.sizeDelta = new Vector2(200, 100);
                rt.anchoredPosition = new Vector2(-20, -20);

                var hlg = topRight.gameObject.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 10;
                hlg.childAlignment = TextAnchor.MiddleRight;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;
            }

            // Ensure proper hierarchy order
            hudPanel.SetAsFirstSibling();
            settingsPanel.SetAsLastSibling();
        }

        private void CreateSettingsContent(Transform settingsPanel)
        {
            // Title
            var titleObj = CreateTextObject("SettingsTitle", settingsPanel, "SETTINGS", 36, FontStyles.Bold);
            
            // Master Volume
            CreateSliderWithLabel(settingsPanel, "MasterVolume", "Master Volume", 1f);
            
            // Music Volume
            CreateSliderWithLabel(settingsPanel, "MusicVolume", "Music Volume", 1f);
            
            // SFX Volume
            CreateSliderWithLabel(settingsPanel, "SFXVolume", "SFX Volume", 1f);
            
            // Quality Dropdown
            CreateDropdownWithLabel(settingsPanel, "Quality", "Graphics Quality");
            
            // Resolution Dropdown
            CreateDropdownWithLabel(settingsPanel, "Resolution", "Resolution");
            
            // Fullscreen Toggle
            CreateToggleWithLabel(settingsPanel, "Fullscreen", "Fullscreen");
            
            // Close Button
            CreateButton(settingsPanel, "CloseSettings", "Close");
        }

        private void CreateMobileControls(Transform mobilePanel)
        {
            // Left side for joystick
            var leftSide = CreateUIObject("LeftSide", mobilePanel);
            var leftRT = leftSide.GetComponent<RectTransform>();
            leftRT.anchorMin = new Vector2(0, 0);
            leftRT.anchorMax = new Vector2(0.5f, 1);
            leftRT.sizeDelta = Vector2.zero;

            // Right side for buttons
            var rightSide = CreateUIObject("RightSide", mobilePanel);
            var rightRT = rightSide.GetComponent<RectTransform>();
            rightRT.anchorMin = new Vector2(0.5f, 0);
            rightRT.anchorMax = new Vector2(1, 1);
            rightRT.sizeDelta = Vector2.zero;

            // Left Joystick
            var leftJoystick = CreateUIObject("LeftJoystick", leftSide);
            var joyRT = leftJoystick.GetComponent<RectTransform>();
            joyRT.anchorMin = new Vector2(0, 0);
            joyRT.anchorMax = new Vector2(0, 0);
            joyRT.pivot = new Vector2(0.5f, 0.5f);
            joyRT.sizeDelta = new Vector2(150, 150);
            joyRT.anchoredPosition = new Vector2(100, 100);
            
            var joyImg = leftJoystick.GetComponent<Image>();
            joyImg.sprite = LoadSprite(JOYSTICK_BG_PATH);
            joyImg.color = new Color(1, 1, 1, 0.5f);

            // Right Joystick
            var rightJoystick = CreateUIObject("RightJoystick", rightSide);
            var rJoyRT = rightJoystick.GetComponent<RectTransform>();
            rJoyRT.anchorMin = new Vector2(1, 0);
            rJoyRT.anchorMax = new Vector2(1, 0);
            rJoyRT.pivot = new Vector2(0.5f, 0.5f);
            rJoyRT.sizeDelta = new Vector2(150, 150);
            rJoyRT.anchoredPosition = new Vector2(-100, 100);
            
            var rJoyImg = rightJoystick.GetComponent<Image>();
            rJoyImg.sprite = LoadSprite(JOYSTICK_BG_PATH);
            rJoyImg.color = new Color(1, 1, 1, 0.5f);

            // Buttons container
            var buttonsContainer = CreateUIObject("ButtonsContainer", rightSide);
            var bcRT = buttonsContainer.GetComponent<RectTransform>();
            bcRT.anchorMin = new Vector2(0.5f, 0);
            bcRT.anchorMax = new Vector2(1, 0.5f);
            bcRT.sizeDelta = Vector2.zero;

            var glg = buttonsContainer.gameObject.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(80, 80);
            glg.spacing = new Vector2(10, 10);
            glg.startCorner = GridLayoutGroup.Corner.UpperRight;
            glg.startAxis = GridLayoutGroup.Axis.Horizontal;
            glg.childAlignment = TextAnchor.MiddleCenter;

            // Jump button
            CreateMobileButton(buttonsContainer, "MobileJump", "Jump");
            // Sprint button
            CreateMobileButton(buttonsContainer, "MobileSprint", "Sprint");
            // Interact button
            CreateMobileButton(buttonsContainer, "MobileInteract", "Interact");
        }

        private Transform CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var panel = CreateUIObject(name, parent);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            var image = panel.GetComponent<Image>();
            image.sprite = LoadSprite(PANEL_BG_PATH);
            image.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);
            image.type = Image.Type.Sliced;

            return panel;
        }

        private Transform CreateUIObject(string name, Transform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            obj.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
            return obj.transform;
        }

        private GameObject CreateTextObject(string name, Transform parent, string text, float fontSize, FontStyles style = FontStyles.Normal)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            
            var tmp = obj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            var rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400, 50);

            Undo.RegisterCreatedObjectUndo(obj, "Create Text " + name);
            return obj;
        }

        private void CreateSliderWithLabel(Transform parent, string name, string label, float defaultValue)
        {
            var container = CreateUIObject(name + "Container", parent);
            var rt = container.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(500, 60);

            var hlg = container.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            CreateTextObject(name + "Label", container, label, 18).GetComponent<RectTransform>().sizeDelta = new Vector2(150, 30);

            var sliderObj = new GameObject(name + "Slider", typeof(RectTransform), typeof(Slider));
            sliderObj.transform.SetParent(container, false);
            var sliderRT = sliderObj.GetComponent<RectTransform>();
            sliderRT.sizeDelta = new Vector2(300, 30);

            // Background
            var bg = CreateUIObject("Background", sliderObj.transform);
            var bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Fill Area
            var fillArea = CreateUIObject("Fill Area", sliderObj.transform);
            var faRT = fillArea.GetComponent<RectTransform>();
            faRT.anchorMin = Vector2.zero;
            faRT.anchorMax = Vector2.one;
            faRT.sizeDelta = new Vector2(-20, 0);

            // Fill
            var fill = CreateUIObject("Fill", fillArea);
            var fillRT = fill.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.sizeDelta = Vector2.zero;
            fill.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.9f, 1f);

            // Handle Slide Area
            var handleArea = CreateUIObject("Handle Slide Area", sliderObj.transform);
            var haRT = handleArea.GetComponent<RectTransform>();
            haRT.anchorMin = Vector2.zero;
            haRT.anchorMax = Vector2.one;
            haRT.sizeDelta = new Vector2(-20, 0);

            // Handle
            var handle = CreateUIObject("Handle", handleArea);
            var hRT = handle.GetComponent<RectTransform>();
            hRT.sizeDelta = new Vector2(20, 30);
            handle.GetComponent<Image>().color = Color.white;

            var slider = sliderObj.GetComponent<Slider>();
            slider.fillRect = fillRT;
            slider.handleRect = hRT;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = defaultValue;

            Undo.RegisterCreatedObjectUndo(sliderObj, "Create Slider " + name);
        }

        private void CreateDropdownWithLabel(Transform parent, string name, string label)
        {
            var container = CreateUIObject(name + "Container", parent);
            var hlg = container.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            CreateTextObject(name + "Label", container, label, 18).GetComponent<RectTransform>().sizeDelta = new Vector2(150, 30);

            var dropdownObj = new GameObject(name + "Dropdown", typeof(RectTransform), typeof(TMP_Dropdown));
            dropdownObj.transform.SetParent(container, false);
            var ddRT = dropdownObj.GetComponent<RectTransform>();
            ddRT.sizeDelta = new Vector2(300, 40);

            // Template setup would go here...
            // For now, just create the basic dropdown

            Undo.RegisterCreatedObjectUndo(dropdownObj, "Create Dropdown " + name);
        }

        private void CreateToggleWithLabel(Transform parent, string name, string label)
        {
            var container = CreateUIObject(name + "Container", parent);
            var hlg = container.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            var toggleObj = new GameObject(name + "Toggle", typeof(RectTransform), typeof(Toggle));
            toggleObj.transform.SetParent(container, false);
            var tRT = toggleObj.GetComponent<RectTransform>();
            tRT.sizeDelta = new Vector2(40, 40);

            var bg = CreateUIObject("Background", toggleObj.transform);
            var bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;
            bg.GetComponent<Image>().sprite = LoadSprite(BUTTON_BG_PATH);
            bg.GetComponent<Image>().color = Color.white;

            var checkmark = CreateUIObject("Checkmark", toggleObj.transform);
            var cmRT = checkmark.GetComponent<RectTransform>();
            cmRT.anchorMin = Vector2.zero;
            cmRT.anchorMax = Vector2.one;
            cmRT.sizeDelta = new Vector2(-10, -10);
            checkmark.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.9f, 1f);

            var toggle = toggleObj.GetComponent<Toggle>();
            toggle.targetGraphic = bg.GetComponent<Image>();
            toggle.graphic = checkmark.GetComponent<Image>();
            toggle.isOn = true;

            CreateTextObject(name + "Label", container, label, 18).GetComponent<RectTransform>().sizeDelta = new Vector2(150, 30);

            Undo.RegisterCreatedObjectUndo(toggleObj, "Create Toggle " + name);
        }

        private void CreateButton(Transform parent, string name, string label)
        {
            var btnObj = new GameObject(name + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            var rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 50);

            var img = btnObj.GetComponent<Image>();
            img.sprite = LoadSprite(BUTTON_BG_PATH);
            img.color = new Color(0.2f, 0.6f, 0.9f, 1f);
            img.type = Image.Type.Sliced;

            var btn = btnObj.GetComponent<Button>();
            btn.targetGraphic = img;

            var textObj = CreateTextObject("Text", btnObj.transform, label, 20, FontStyles.Bold);
            var tRT = textObj.GetComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero;
            tRT.anchorMax = Vector2.one;
            tRT.sizeDelta = Vector2.zero;

            Undo.RegisterCreatedObjectUndo(btnObj, "Create Button " + name);
        }

        private void CreateMobileButton(Transform parent, string name, string label)
        {
            var btnObj = new GameObject(name + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            var rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(80, 80);

            var img = btnObj.GetComponent<Image>();
            img.sprite = LoadSprite(CIRCLE_BG_PATH);
            img.color = new Color(1, 1, 1, 0.7f);
            img.type = Image.Type.Sliced;

            var btn = btnObj.GetComponent<Button>();
            btn.targetGraphic = img;

            var textObj = CreateTextObject("Text", btnObj.transform, label, 14);
            var tRT = textObj.GetComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero;
            tRT.anchorMax = Vector2.one;
            tRT.sizeDelta = Vector2.zero;

            Undo.RegisterCreatedObjectUndo(btnObj, "Create Mobile Button " + name);
        }

        private const string CIRCLE_BG_PATH = "Assets/MeetingCellsRefactored/Textures/circle128_fill.png";

        private void AssignSourceImages()
        {
            // Settings Panel background
            Transform settingsPanel = gamePlayerUI.transform.Find("SettingsPanel");
            if (settingsPanel != null)
            {
                var img = settingsPanel.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = LoadSprite(PANEL_BG_PATH);
                    img.color = new Color(0.08f, 0.08f, 0.1f, 0.96f);
                    img.type = Image.Type.Sliced;
                }
            }

            // Find and assign button images
            var buttons = gamePlayerUI.GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                var img = btn.GetComponent<Image>();
                if (img == null) continue;

                string btnName = btn.name.ToLower();
                
                if (btnName.Contains("close"))
                {
                    img.sprite = LoadSprite(CLOSE_BUTTON_PATH);
                    img.type = Image.Type.Simple;
                }
                else if (btnName.Contains("settings"))
                {
                    img.sprite = LoadSprite(SETTINGS_ICON_PATH);
                    img.type = Image.Type.Simple;
                }
                else
                {
                    img.sprite = LoadSprite(BUTTON_BG_PATH);
                    img.type = Image.Type.Sliced;
                    
                    // Set button colors
                    var cb = btn.colors;
                    cb.normalColor = new Color(0.2f, 0.6f, 0.9f, 1f);
                    cb.highlightedColor = new Color(0.3f, 0.7f, 1f, 1f);
                    cb.pressedColor = new Color(0.15f, 0.45f, 0.68f, 1f);
                    btn.colors = cb;
                }
            }

            // Joystick backgrounds
            var joysticks = gamePlayerUI.GetComponentsInChildren<Image>(true);
            foreach (var img in joysticks)
            {
                if (img.name.ToLower().Contains("joystick"))
                {
                    img.sprite = LoadSprite(JOYSTICK_BG_PATH);
                    img.type = Image.Type.Sliced;
                    img.color = new Color(1, 1, 1, 0.4f);
                }
            }
        }

        private void SetupTextLabels()
        {
            var texts = gamePlayerUI.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in texts)
            {
                Undo.RecordObject(text, "Setup Text");
                
                string name = text.name.ToLower();
                
                if (name.Contains("title"))
                {
                    text.fontSize = 36;
                    text.fontStyle = FontStyles.Bold;
                    text.alignment = TextAlignmentOptions.Center;
                    text.color = new Color(1f, 0.9f, 0.4f, 1f); // Gold color for titles
                }
                else if (name.Contains("label"))
                {
                    text.fontSize = 18;
                    text.fontStyle = FontStyles.Normal;
                    text.alignment = TextAlignmentOptions.Left;
                    text.color = new Color(0.9f, 0.9f, 0.9f, 1f);
                }
                else
                {
                    text.fontSize = 20;
                    text.alignment = TextAlignmentOptions.Center;
                    text.color = Color.white;
                }
            }
        }

        private Sprite LoadSprite(string path)
        {
            // Try to load as sprite first
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;

            // If that fails, try to load texture and create sprite
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
            {
                return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            }

            Debug.LogWarning($"[GamePlayerUIAssetSetup] Could not load sprite at path: {path}");
            return null;
        }
    }
}
#endif
