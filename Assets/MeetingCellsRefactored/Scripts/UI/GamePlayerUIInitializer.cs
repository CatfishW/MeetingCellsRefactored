using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

namespace MeetingCellsRefactored.UI
{
    /// <summary>
    /// Helper component to initialize and wire up the GamePlayerUI system
    /// Attach this to your main GameObject in the scene
    /// </summary>
    public class GamePlayerUIInitializer : MonoBehaviour
    {
        [Header("UI Prefab References")]
        [SerializeField] private GameObject gameUICanvasPrefab;
        [SerializeField] private GameObject settingsPanelPrefab;
        [SerializeField] private GameObject mobileControlsPrefab;

        [Header("Camera References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Transform firstPersonCameraTarget;
        [SerializeField] private Transform thirdPersonCameraTarget;

        [Header("Player References")]
        [SerializeField] private Transform playerTransform;

        [Header("Audio")]
        [SerializeField] private AudioMixer audioMixer;

        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;

        private void Awake()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            // Create UI Canvas if not exists
            var existingUI = FindObjectOfType<GamePlayerUI>();
            if (existingUI == null && gameUICanvasPrefab != null)
            {
                Instantiate(gameUICanvasPrefab);
            }

            // Setup Camera Controller
            var camController = FindObjectOfType<CameraModeController>();
            if (camController == null)
            {
                var camObj = new GameObject("CameraModeController");
                camController = camObj.AddComponent<CameraModeController>();
            }

            // Setup Input Manager
            var inputManager = FindObjectOfType<InputManager>();
            if (inputManager == null)
            {
                var inputObj = new GameObject("InputManager");
                inputManager = inputObj.AddComponent<InputManager>();
            }

            // Setup Settings Manager
            var settingsManager = FindObjectOfType<SettingsManager>();
            if (settingsManager == null)
            {
                var settingsObj = new GameObject("SettingsManager");
                settingsManager = settingsObj.AddComponent<SettingsManager>();
            }

            // Setup UI Manager
            var uiManager = FindObjectOfType<UIManager>();
            if (uiManager == null)
            {
                var uiObj = new GameObject("UIManager");
                uiManager = uiObj.AddComponent<UIManager>();
            }

            // Setup Sound Controller
            var soundController = FindObjectOfType<UISoundController>();
            if (soundController == null)
            {
                var soundObj = new GameObject("UISoundController");
                soundController = soundObj.AddComponent<UISoundController>();
            }

            Debug.Log("[GamePlayerUIInitializer] UI System initialized successfully");
        }

        private void Start()
        {
            // Verify all components are present
            VerifyComponents();
        }

        private void VerifyComponents()
        {
            bool allOk = true;

            if (FindObjectOfType<GamePlayerUI>() == null)
            {
                Debug.LogError("[GamePlayerUIInitializer] GamePlayerUI not found!");
                allOk = false;
            }
            if (FindObjectOfType<InputManager>() == null)
            {
                Debug.LogError("[GamePlayerUIInitializer] InputManager not found!");
                allOk = false;
            }
            if (FindObjectOfType<SettingsManager>() == null)
            {
                Debug.LogError("[GamePlayerUIInitializer] SettingsManager not found!");
                allOk = false;
            }
            if (FindObjectOfType<UIManager>() == null)
            {
                Debug.LogError("[GamePlayerUIInitializer] UIManager not found!");
                allOk = false;
            }

            if (allOk)
            {
                Debug.Log("[GamePlayerUIInitializer] All UI components verified successfully");
            }
        }
    }
}
