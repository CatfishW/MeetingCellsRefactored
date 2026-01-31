using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace MeetingCellsRefactored.UI
{
    /// <summary>
    /// Central UI manager that coordinates all UI systems
    /// Handles panel stacking, navigation, and state management
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("UI Controllers")]
        [SerializeField] private GamePlayerUI gamePlayerUI;
        [SerializeField] private InputManager inputManager;
        [SerializeField] private SettingsManager settingsManager;
        [SerializeField] private CameraModeController cameraController;
        [SerializeField] private UISoundController soundController;

        [Header("UI Panels")]
        [SerializeField] private List<UIPanel> uiPanels = new List<UIPanel>();

        [Header("Navigation")]
        [SerializeField] private bool enableBackNavigation = true;
        [SerializeField] private KeyCode backKey = KeyCode.Escape;

        // Panel stack for navigation
        private Stack<UIPanel> panelStack = new Stack<UIPanel>();
        private UIPanel currentPanel;

        // Events
        public event Action<UIPanel> OnPanelOpened;
        public event Action<UIPanel> OnPanelClosed;
        public event Action OnBackNavigation;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitializeControllers();
            InitializePanels();
        }

        private void Update()
        {
            HandleBackNavigation();
        }

        private void InitializeControllers()
        {
            // Auto-find controllers if not assigned
            if (gamePlayerUI == null)
                gamePlayerUI = FindObjectOfType<GamePlayerUI>();
            if (inputManager == null)
                inputManager = FindObjectOfType<InputManager>();
            if (settingsManager == null)
                settingsManager = FindObjectOfType<SettingsManager>();
            if (cameraController == null)
                cameraController = FindObjectOfType<CameraModeController>();
            if (soundController == null)
                soundController = FindObjectOfType<UISoundController>();

            // Subscribe to GamePlayerUI events
            if (gamePlayerUI != null)
            {
                gamePlayerUI.OnSettingsPanelToggled += OnSettingsToggled;
            }
        }

        private void InitializePanels()
        {
            foreach (var panel in uiPanels)
            {
                if (panel != null)
                {
                    panel.Initialize(this);
                    panel.gameObject.SetActive(false);
                }
            }
        }

        private void OnDestroy()
        {
            if (gamePlayerUI != null)
            {
                gamePlayerUI.OnSettingsPanelToggled -= OnSettingsToggled;
            }
        }

        #region Panel Management

        public void OpenPanel(string panelName)
        {
            var panel = uiPanels.Find(p => p != null && p.PanelName == panelName);
            if (panel != null)
            {
                OpenPanel(panel);
            }
            else
            {
                Debug.LogWarning($"[UIManager] Panel '{panelName}' not found!");
            }
        }

        public void OpenPanel(UIPanel panel)
        {
            if (panel == null) return;

            // Close current panel if it's not the same
            if (currentPanel != null && currentPanel != panel)
            {
                panelStack.Push(currentPanel);
                currentPanel.Hide();
            }

            currentPanel = panel;
            panel.Show();

            // Switch to UI input mode
            inputManager?.EnableUIInput();

            OnPanelOpened?.Invoke(panel);
            soundController?.PlayPanelOpen();
        }

        public void CloseCurrentPanel()
        {
            if (currentPanel != null)
            {
                currentPanel.Hide();
                OnPanelClosed?.Invoke(currentPanel);

                // Pop from stack
                if (panelStack.Count > 0)
                {
                    currentPanel = panelStack.Pop();
                    currentPanel.Show();
                }
                else
                {
                    currentPanel = null;
                    // Return to game input
                    inputManager?.EnablePlayerInput();
                }

                soundController?.PlayPanelClose();
            }
            else
            {
                // No panel open, toggle settings or pause
                gamePlayerUI?.ToggleSettings();
            }
        }

        public void CloseAllPanels()
        {
            while (panelStack.Count > 0)
            {
                var panel = panelStack.Pop();
                panel?.Hide();
            }

            if (currentPanel != null)
            {
                currentPanel.Hide();
                currentPanel = null;
            }

            inputManager?.EnablePlayerInput();
        }

        public void RegisterPanel(UIPanel panel)
        {
            if (!uiPanels.Contains(panel))
            {
                uiPanels.Add(panel);
                panel.Initialize(this);
            }
        }

        public void UnregisterPanel(UIPanel panel)
        {
            uiPanels.Remove(panel);
        }

        #endregion

        #region Navigation

        private void HandleBackNavigation()
        {
            if (!enableBackNavigation) return;

            if (Keyboard.current != null && IsKeyPressedThisFrame(backKey))
            {
                OnBackNavigation?.Invoke();
                CloseCurrentPanel();
            }
        }

        private bool IsKeyPressedThisFrame(KeyCode keyCode)
        {
            var key = GetKeyFromKeyCode(keyCode);
            return key != null && key.wasPressedThisFrame;
        }

        private UnityEngine.InputSystem.Controls.KeyControl GetKeyFromKeyCode(KeyCode keyCode)
        {
            return keyCode switch
            {
                KeyCode.V => Keyboard.current.vKey,
                KeyCode.Space => Keyboard.current.spaceKey,
                KeyCode.Return => Keyboard.current.enterKey,
                KeyCode.Escape => Keyboard.current.escapeKey,
                KeyCode.Tab => Keyboard.current.tabKey,
                KeyCode.LeftShift => Keyboard.current.leftShiftKey,
                KeyCode.RightShift => Keyboard.current.rightShiftKey,
                KeyCode.LeftControl => Keyboard.current.leftCtrlKey,
                KeyCode.RightControl => Keyboard.current.rightCtrlKey,
                KeyCode.LeftAlt => Keyboard.current.leftAltKey,
                KeyCode.RightAlt => Keyboard.current.rightAltKey,
                KeyCode.UpArrow => Keyboard.current.upArrowKey,
                KeyCode.DownArrow => Keyboard.current.downArrowKey,
                KeyCode.LeftArrow => Keyboard.current.leftArrowKey,
                KeyCode.RightArrow => Keyboard.current.rightArrowKey,
                KeyCode.A => Keyboard.current.aKey,
                KeyCode.B => Keyboard.current.bKey,
                KeyCode.C => Keyboard.current.cKey,
                KeyCode.D => Keyboard.current.dKey,
                KeyCode.E => Keyboard.current.eKey,
                KeyCode.F => Keyboard.current.fKey,
                KeyCode.G => Keyboard.current.gKey,
                KeyCode.H => Keyboard.current.hKey,
                KeyCode.I => Keyboard.current.iKey,
                KeyCode.J => Keyboard.current.jKey,
                KeyCode.K => Keyboard.current.kKey,
                KeyCode.L => Keyboard.current.lKey,
                KeyCode.M => Keyboard.current.mKey,
                KeyCode.N => Keyboard.current.nKey,
                KeyCode.O => Keyboard.current.oKey,
                KeyCode.P => Keyboard.current.pKey,
                KeyCode.Q => Keyboard.current.qKey,
                KeyCode.R => Keyboard.current.rKey,
                KeyCode.S => Keyboard.current.sKey,
                KeyCode.T => Keyboard.current.tKey,
                KeyCode.U => Keyboard.current.uKey,
                KeyCode.W => Keyboard.current.wKey,
                KeyCode.X => Keyboard.current.xKey,
                KeyCode.Y => Keyboard.current.yKey,
                KeyCode.Z => Keyboard.current.zKey,
                KeyCode.Alpha0 => Keyboard.current.digit0Key,
                KeyCode.Alpha1 => Keyboard.current.digit1Key,
                KeyCode.Alpha2 => Keyboard.current.digit2Key,
                KeyCode.Alpha3 => Keyboard.current.digit3Key,
                KeyCode.Alpha4 => Keyboard.current.digit4Key,
                KeyCode.Alpha5 => Keyboard.current.digit5Key,
                KeyCode.Alpha6 => Keyboard.current.digit6Key,
                KeyCode.Alpha7 => Keyboard.current.digit7Key,
                KeyCode.Alpha8 => Keyboard.current.digit8Key,
                KeyCode.Alpha9 => Keyboard.current.digit9Key,
                KeyCode.F1 => Keyboard.current.f1Key,
                KeyCode.F2 => Keyboard.current.f2Key,
                KeyCode.F3 => Keyboard.current.f3Key,
                KeyCode.F4 => Keyboard.current.f4Key,
                KeyCode.F5 => Keyboard.current.f5Key,
                KeyCode.F6 => Keyboard.current.f6Key,
                KeyCode.F7 => Keyboard.current.f7Key,
                KeyCode.F8 => Keyboard.current.f8Key,
                KeyCode.F9 => Keyboard.current.f9Key,
                KeyCode.F10 => Keyboard.current.f10Key,
                KeyCode.F11 => Keyboard.current.f11Key,
                KeyCode.F12 => Keyboard.current.f12Key,
                _ => null
            };
        }

        private void OnSettingsToggled(bool isOpen)
        {
            if (isOpen)
            {
                inputManager?.EnableUIInput();
            }
            else
            {
                inputManager?.EnablePlayerInput();
            }
        }

        #endregion

        #region Cross-Platform Helpers

        public void ShowMobileControls(bool show)
        {
            gamePlayerUI?.ShowMobileControls(show);
        }

        public void SetCameraMode(GamePlayerUI.CameraMode mode)
        {
            gamePlayerUI?.SetCameraMode(mode);
        }

        public void ToggleCameraMode()
        {
            gamePlayerUI?.ToggleCameraMode();
        }

        #endregion

        #region Properties

        public GamePlayerUI GamePlayerUI => gamePlayerUI;
        public InputManager InputManager => inputManager;
        public SettingsManager SettingsManager => settingsManager;
        public CameraModeController CameraController => cameraController;
        public UISoundController SoundController => soundController;
        public UIPanel CurrentPanel => currentPanel;
        public bool IsPanelOpen => currentPanel != null;
        public int PanelStackCount => panelStack.Count;

        #endregion
    }
}
