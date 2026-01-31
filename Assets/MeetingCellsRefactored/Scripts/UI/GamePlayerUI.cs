using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using System;

namespace MeetingCellsRefactored.UI
{
    /// <summary>
    /// Main GamePlayer UI controller - manages all HUD elements
    /// Cross-platform: PC/WebGL/Android/iOS compatible
    /// </summary>
    public class GamePlayerUI : MonoBehaviour
    {
        public static GamePlayerUI Instance { get; private set; }

        [Header("Canvas References")]
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private CanvasScaler canvasScaler;
        [SerializeField] private GraphicRaycaster graphicRaycaster;

        [Header("UI Panels")]
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject mobileControlsPanel;

        [Header("Control Buttons")]
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button cameraModeButton;
        [SerializeField] private TextMeshProUGUI cameraModeText;

        [Header("Mobile Controls")]
        [SerializeField] private GameObject leftJoystick;
        [SerializeField] private GameObject touchLookArea; // Right screen touch area for look
        [SerializeField] private Button mobileJumpButton;
        [SerializeField] private Button mobileSprintButton;
        [SerializeField] private Button mobileInteractButton;
        [SerializeField] private Button mobileCameraModeButton;

        [Header("Top Action Bar")]
        [SerializeField] private GameObject topActionBar;
        [SerializeField] private Button settingsButtonTop;
        [SerializeField] private Button cameraToggleButtonTop;
        [SerializeField] private Button interactButtonTop;

        [Header("Settings UI")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private Button closeSettingsButton;

        [Header("Cross-Platform Settings")]
        [SerializeField] private bool autoDetectPlatform = true;
        [SerializeField] private bool forceMobileControls = false;
        [SerializeField] private bool forcePCControls = false;

        [Header("Mobile Layout")]
        [SerializeField] private Vector2 mobileReferenceResolution = new Vector2(1080, 1920);
        [SerializeField] private Vector2 pcReferenceResolution = new Vector2(1920, 1080);
        [SerializeField] private float mobileJoystickScale = 1.5f;
        [SerializeField] private float mobileButtonScale = 1.2f;

        // Events
        public event Action<bool> OnSettingsPanelToggled;
        public event Action<CameraMode> OnCameraModeChanged;
        public event Action<float> OnMasterVolumeChanged;
        public event Action<float> OnMusicVolumeChanged;
        public event Action<float> OnSfxVolumeChanged;

        private bool isSettingsOpen = false;
        private CameraMode currentCameraMode = CameraMode.FirstPerson;
        private PlatformType currentPlatform;

        public enum CameraMode
        {
            FirstPerson,
            ThirdPerson
        }

        public enum PlatformType
        {
            PC,
            WebGL,
            Android,
            iOS
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Only call DontDestroyOnLoad if this is a root GameObject
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Start()
        {
            EnsureEventSystemExists();
            DetectPlatform();
            SetupCanvas();
            SetupEventListeners();
            InitializeSettings();
            UpdateUIForPlatform();
        }

        private void OnDestroy()
        {
            RemoveEventListeners();
        }

        private void EnsureEventSystemExists()
        {
            var eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                var inputModule = eventSystemObj.AddComponent<InputSystemUIInputModule>();

                Debug.Log("[GamePlayerUI] Created EventSystem with InputSystemUIInputModule");
            }
        }

        private void DetectPlatform()
        {
            if (forceMobileControls)
            {
                currentPlatform = PlatformType.Android;
                Debug.Log("[GamePlayerUI] Mobile controls FORCED via inspector");
                return;
            }
            if (forcePCControls)
            {
                currentPlatform = PlatformType.PC;
                Debug.Log("[GamePlayerUI] PC controls FORCED via inspector");
                return;
            }

#if UNITY_ANDROID
            currentPlatform = PlatformType.Android;
#elif UNITY_IOS
            currentPlatform = PlatformType.iOS;
#elif UNITY_WEBGL
            // WebGL could be on mobile or PC - use runtime detection
            currentPlatform = Application.isMobilePlatform ? PlatformType.Android : PlatformType.WebGL;
#else
            currentPlatform = PlatformType.PC;
#endif

            // Additional runtime check for mobile
            if (Application.isMobilePlatform &&
                currentPlatform != PlatformType.Android &&
                currentPlatform != PlatformType.iOS)
            {
                currentPlatform = PlatformType.Android;
            }

            Debug.Log($"[GamePlayerUI] Platform detected: {currentPlatform}, isMobilePlatform: {Application.isMobilePlatform}");
        }

        private void SetupCanvas()
        {
            if (mainCanvas == null)
                mainCanvas = GetComponent<Canvas>();
            if (canvasScaler == null)
                canvasScaler = GetComponent<CanvasScaler>();

            // Always use Screen Space Overlay for proper mobile rendering
            if (mainCanvas != null)
            {
                mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                mainCanvas.sortingOrder = 100;
            }

            // Configure scaler based on platform
            if (canvasScaler != null)
            {
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

                bool isMobile = currentPlatform == PlatformType.Android || currentPlatform == PlatformType.iOS;

                if (isMobile)
                {
                    // Mobile: Portrait orientation
                    canvasScaler.referenceResolution = mobileReferenceResolution;
                    canvasScaler.matchWidthOrHeight = 0f; // Match width
                }
                else
                {
                    // PC: Landscape orientation
                    canvasScaler.referenceResolution = pcReferenceResolution;
                    canvasScaler.matchWidthOrHeight = 0.5f; // Balance width/height
                }
            }
        }

        private void SetupEventListeners()
        {
            // Settings buttons
            if (settingsButton != null)
                settingsButton.onClick.AddListener(ToggleSettings);
            if (settingsButtonTop != null)
                settingsButtonTop.onClick.AddListener(ToggleSettings);

            // Camera mode buttons
            if (cameraModeButton != null)
                cameraModeButton.onClick.AddListener(ToggleCameraMode);
            if (cameraToggleButtonTop != null)
                cameraToggleButtonTop.onClick.AddListener(ToggleCameraMode);

            if (closeSettingsButton != null)
                closeSettingsButton.onClick.AddListener(CloseSettings);

            if (interactButtonTop != null)
                interactButtonTop.onClick.AddListener(OnMobileInteract);

            // Settings sliders
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeSliderChanged);
            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeSliderChanged);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeSliderChanged);

            // Settings toggles/dropdowns
            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggleChanged);
            if (qualityDropdown != null)
                qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            if (resolutionDropdown != null)
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

            // Mobile buttons
            if (mobileJumpButton != null)
                mobileJumpButton.onClick.AddListener(OnMobileJump);
            if (mobileSprintButton != null)
                mobileSprintButton.onClick.AddListener(OnMobileSprint);
            if (mobileInteractButton != null)
                mobileInteractButton.onClick.AddListener(OnMobileInteract);
            if (mobileCameraModeButton != null)
                mobileCameraModeButton.onClick.AddListener(ToggleCameraMode);
        }

        private void RemoveEventListeners()
        {
            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(ToggleSettings);
            if (settingsButtonTop != null)
                settingsButtonTop.onClick.RemoveListener(ToggleSettings);
            if (cameraModeButton != null)
                cameraModeButton.onClick.RemoveListener(ToggleCameraMode);
            if (cameraToggleButtonTop != null)
                cameraToggleButtonTop.onClick.RemoveListener(ToggleCameraMode);
            if (closeSettingsButton != null)
                closeSettingsButton.onClick.RemoveListener(CloseSettings);

            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeSliderChanged);
            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeSliderChanged);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeSliderChanged);

            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenToggleChanged);
            if (qualityDropdown != null)
                qualityDropdown.onValueChanged.RemoveListener(OnQualityChanged);
            if (resolutionDropdown != null)
                resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);

            if (mobileJumpButton != null)
                mobileJumpButton.onClick.RemoveListener(OnMobileJump);
            if (mobileSprintButton != null)
                mobileSprintButton.onClick.RemoveListener(OnMobileSprint);
            if (mobileInteractButton != null)
                mobileInteractButton.onClick.RemoveListener(OnMobileInteract);
            if (mobileCameraModeButton != null)
                mobileCameraModeButton.onClick.RemoveListener(ToggleCameraMode);
        }

        private void InitializeSettings()
        {
            // Load saved settings
            if (masterVolumeSlider != null)
                masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
            if (musicVolumeSlider != null)
                musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);

            // Initialize quality dropdown
            if (qualityDropdown != null)
            {
                qualityDropdown.ClearOptions();
                var qualityNames = new System.Collections.Generic.List<string>(QualitySettings.names);
                qualityDropdown.AddOptions(qualityNames);
                qualityDropdown.value = QualitySettings.GetQualityLevel();
            }

            // Initialize resolution dropdown
            if (resolutionDropdown != null)
            {
                SetupResolutionDropdown();
            }

            if (fullscreenToggle != null)
                fullscreenToggle.isOn = Screen.fullScreen;
        }

        private void SetupResolutionDropdown()
        {
            resolutionDropdown.ClearOptions();
            var resolutions = Screen.resolutions;
            var options = new System.Collections.Generic.List<string>();
            int currentResolutionIndex = 0;

            for (int i = 0; i < resolutions.Length; i++)
            {
                string option = $"{resolutions[i].width} x {resolutions[i].height} @ {resolutions[i].refreshRateRatio}Hz";
                options.Add(option);

                if (resolutions[i].width == Screen.currentResolution.width &&
                    resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = i;
                }
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentResolutionIndex;
        }

        private void UpdateUIForPlatform()
        {
            bool isMobile = currentPlatform == PlatformType.Android || currentPlatform == PlatformType.iOS;

            // Show/hide mobile controls panel
            if (mobileControlsPanel != null)
                mobileControlsPanel.SetActive(isMobile);

            // Configure for platform
            if (isMobile)
            {
                ConfigureMobileUI();
            }
            else
            {
                ConfigurePCUI();
            }

            // Setup joysticks
            if (isMobile)
            {
                SetupMobileJoysticks();
            }

            // Notify InputManager
            if (InputManager.Instance != null)
            {
                InputManager.Instance.SetMobileMode(isMobile);
            }

            Debug.Log($"[GamePlayerUI] Platform: {currentPlatform}, Mobile: {isMobile}");
        }

        private void ConfigureMobileUI()
        {
            if (canvasScaler != null)
            {
                canvasScaler.referenceResolution = mobileReferenceResolution;
                canvasScaler.matchWidthOrHeight = 0f;
            }

            // Reposition joysticks for mobile
            PositionMobileControls();
        }

        private void ConfigurePCUI()
        {
            if (canvasScaler != null)
            {
                canvasScaler.referenceResolution = pcReferenceResolution;
                canvasScaler.matchWidthOrHeight = 0.5f;
            }
        }

        private void PositionMobileControls()
        {
            // Setup top action bar with horizontal layout group
            SetupTopActionBar();

            // Position left joystick at bottom-left (only movement control)
            if (leftJoystick != null)
            {
                var rect = leftJoystick.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0, 0);
                    rect.anchorMax = new Vector2(0, 0);
                    rect.pivot = new Vector2(0, 0);
                    rect.anchoredPosition = new Vector2(120, 120);
                    rect.sizeDelta = new Vector2(200, 200);
                    rect.localScale = Vector3.one * mobileJoystickScale;
                }
            }

            // Position action buttons at bottom-right (stacked vertically)
            PositionActionButtons();
        }

        private void SetupTopActionBar()
        {
            if (topActionBar == null)
            {
                // Create top action bar
                topActionBar = new GameObject("TopActionBar");
                topActionBar.transform.SetParent(mainCanvas.transform, false);

                var rectTransform = topActionBar.AddComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0, 1);
                rectTransform.anchorMax = new Vector2(1, 1);
                rectTransform.pivot = new Vector2(0.5f, 1);
                rectTransform.anchoredPosition = new Vector2(0, -20);
                rectTransform.sizeDelta = new Vector2(-40, 100);

                // Add horizontal layout group
                var horizontalLayout = topActionBar.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                horizontalLayout.childAlignment = TextAnchor.UpperRight;
                horizontalLayout.childControlWidth = false;
                horizontalLayout.childControlHeight = false;
                horizontalLayout.childForceExpandWidth = false;
                horizontalLayout.childForceExpandHeight = false;
                horizontalLayout.spacing = 20;
                horizontalLayout.padding = new RectOffset(20, 20, 10, 10);
            }

            // Move existing buttons to top bar
            MoveButtonToTopBar(settingsButton, "SettingsButton");
            MoveButtonToTopBar(cameraModeButton, "CameraButton");

            // Move functional/action buttons to top bar as well
            MoveButtonToTopBar(mobileSprintButton, "SprintButton");
            MoveButtonToTopBar(mobileInteractButton, "InteractButton");
        }

        private void MoveButtonToTopBar(Button button, string name)
        {
            if (button == null) return;

            button.transform.SetParent(topActionBar.transform, false);
            var rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(80, 80);
                rect.localScale = Vector3.one;
            }
        }

        private void PositionActionButtons()
        {
            // Only position the jump button at bottom-right
            // Sprint and Interact buttons are moved to top action bar
            if (mobileJumpButton != null)
            {
                var rect = mobileJumpButton.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(1, 0);
                    rect.anchorMax = new Vector2(1, 0);
                    rect.pivot = new Vector2(1, 0);
                    rect.anchoredPosition = new Vector2(-100, 120);
                    rect.sizeDelta = new Vector2(100, 100);
                    rect.localScale = Vector3.one * mobileButtonScale;
                }
            }
        }

        private void MoveButtonToContainer(Button button, Transform container, Vector2 size)
        {
            if (button == null) return;

            button.transform.SetParent(container, false);
            var rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = size;
                rect.localScale = Vector3.one;
                // Reset anchors for layout group
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
            }
        }

        private void PositionButton(Button button, Vector2 anchorPosition)
        {
            if (button == null) return;

            var rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = anchorPosition;
                rect.anchorMax = anchorPosition;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.localScale = Vector3.one * mobileButtonScale;
            }
        }

        private void SetupMobileJoysticks()
        {
            // Left joystick for movement only
            if (leftJoystick != null)
            {
                var joystick = leftJoystick.GetComponent<FloatingJoystick>();
                if (joystick == null)
                {
                    joystick = leftJoystick.AddComponent<FloatingJoystick>();
                }
                joystick.SetHandleRange(1f);
                joystick.SetDeadZone(0.1f);
                joystick.SetSnapToFinger(true);
                joystick.SetFadeWhenNotUsed(true);
            }

            // Setup right screen touch look area (replaces right joystick)
            SetupTouchLookArea();

            // Pass to InputManager (right joystick is null, we'll use touch look)
            if (InputManager.Instance != null)
            {
                InputManager.Instance.SetJoysticks(
                    leftJoystick?.GetComponent<FloatingJoystick>(),
                    null // No right joystick - using touch area instead
                );

                // Set touch look area if available
                if (touchLookArea != null)
                {
                    var touchLook = touchLookArea.GetComponent<ScreenTouchLook>();
                    if (touchLook != null)
                    {
                        InputManager.Instance.SetTouchLookArea(touchLook);
                    }
                }
            }
        }

        private void SetupTouchLookArea()
        {
            if (touchLookArea == null)
            {
                // Create touch look area if not assigned
                touchLookArea = new GameObject("TouchLookArea");
                touchLookArea.transform.SetParent(mainCanvas.transform, false);

                // Add required components
                var rectTransform = touchLookArea.AddComponent<RectTransform>();
                var image = touchLookArea.AddComponent<Image>();
                image.color = new Color(1, 1, 1, 0); // Invisible but raycastable
                image.raycastTarget = true;

                // Cover right half of screen
                rectTransform.anchorMin = new Vector2(0.5f, 0);
                rectTransform.anchorMax = new Vector2(1, 1);
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;

                // Add the touch look component
                var touchLook = touchLookArea.AddComponent<ScreenTouchLook>();
                touchLook.SetSensitivity(3f);
            }
            else
            {
                // Ensure it has the component
                var touchLook = touchLookArea.GetComponent<ScreenTouchLook>();
                if (touchLook == null)
                {
                    touchLook = touchLookArea.AddComponent<ScreenTouchLook>();
                    touchLook.SetSensitivity(3f);
                }

                // Ensure it covers right half
                var rectTransform = touchLookArea.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchorMin = new Vector2(0.5f, 0);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                }

                // Ensure it has an image for raycasting
                var image = touchLookArea.GetComponent<Image>();
                if (image == null)
                {
                    image = touchLookArea.AddComponent<Image>();
                    image.color = new Color(1, 1, 1, 0);
                }
                image.raycastTarget = true;
            }

            // Hide right joystick if it exists (legacy)
            var rightJoystick = GameObject.Find("RightJoystick");
            if (rightJoystick != null)
            {
                rightJoystick.SetActive(false);
            }
        }

        #region Public Methods

        public void ToggleSettings()
        {
            isSettingsOpen = !isSettingsOpen;
            settingsPanel?.SetActive(isSettingsOpen);
            hudPanel?.SetActive(!isSettingsOpen);

            // Pause game when settings are open
            Time.timeScale = isSettingsOpen ? 0f : 1f;

            OnSettingsPanelToggled?.Invoke(isSettingsOpen);
        }

        public void CloseSettings()
        {
            if (isSettingsOpen)
                ToggleSettings();
        }

        public void ToggleCameraMode()
        {
            currentCameraMode = currentCameraMode == CameraMode.FirstPerson
                ? CameraMode.ThirdPerson
                : CameraMode.FirstPerson;

            UpdateCameraModeUI();
            OnCameraModeChanged?.Invoke(currentCameraMode);
        }

        public void SetCameraMode(CameraMode mode)
        {
            currentCameraMode = mode;
            UpdateCameraModeUI();
            OnCameraModeChanged?.Invoke(currentCameraMode);
        }

        private void UpdateCameraModeUI()
        {
            if (cameraModeText != null)
            {
                cameraModeText.text = currentCameraMode == CameraMode.FirstPerson ? "1P" : "3P";
            }
        }

        public void ShowHUD()
        {
            hudPanel?.SetActive(true);
        }

        public void HideHUD()
        {
            hudPanel?.SetActive(false);
        }

        public void ShowMobileControls(bool show)
        {
            if (mobileControlsPanel != null)
                mobileControlsPanel.SetActive(show);
        }

        #endregion

        #region Settings Callbacks

        private void OnMasterVolumeSliderChanged(float value)
        {
            PlayerPrefs.SetFloat("MasterVolume", value);
            OnMasterVolumeChanged?.Invoke(value);
        }

        private void OnMusicVolumeSliderChanged(float value)
        {
            PlayerPrefs.SetFloat("MusicVolume", value);
            OnMusicVolumeChanged?.Invoke(value);
        }

        private void OnSfxVolumeSliderChanged(float value)
        {
            PlayerPrefs.SetFloat("SFXVolume", value);
            OnSfxVolumeChanged?.Invoke(value);
        }

        private void OnFullscreenToggleChanged(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
            PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        }

        private void OnQualityChanged(int qualityIndex)
        {
            QualitySettings.SetQualityLevel(qualityIndex);
            PlayerPrefs.SetInt("QualityLevel", qualityIndex);
        }

        private void OnResolutionChanged(int resolutionIndex)
        {
            var resolutions = Screen.resolutions;
            if (resolutionIndex >= 0 && resolutionIndex < resolutions.Length)
            {
                var res = resolutions[resolutionIndex];
                Screen.SetResolution(res.width, res.height, Screen.fullScreen);
            }
        }

        #endregion

        #region Mobile Input Callbacks

        private void OnMobileJump()
        {
            InputManager.Instance?.OnMobileJump();
        }

        private void OnMobileSprint()
        {
            InputManager.Instance?.OnMobileSprint();
        }

        private void OnMobileInteract()
        {
            InputManager.Instance?.OnMobileInteract();
        }

        #endregion

        #region Properties

        public bool IsSettingsOpen => isSettingsOpen;
        public CameraMode CurrentCameraMode => currentCameraMode;
        public PlatformType CurrentPlatformType => currentPlatform;
        public bool IsMobile => currentPlatform == PlatformType.Android || currentPlatform == PlatformType.iOS;

        #endregion
    }
}
