using UnityEngine;
using Unity.Cinemachine;

namespace MeetingCellsRefactored.Player
{
    /// <summary>
    /// Validates player setup and reports any issues
    /// Attach this to the player prefab for debugging
    /// </summary>
    public class PlayerSetupValidator : MonoBehaviour
    {
        [Header("Validation Settings")]
        [SerializeField] private bool validateOnStart = true;
        [SerializeField] private bool showDebugInfo = true;

        [Header("Required Components")]
        [SerializeField] private Transform playerModel;
        [SerializeField] private CinemachineCamera firstPersonCamera;
        [SerializeField] private CinemachineCamera thirdPersonCamera;

        private void Start()
        {
            if (validateOnStart)
            {
                ValidateSetup();
            }
        }

        [ContextMenu("Validate Setup")]
        public void ValidateSetup()
        {
            bool allValid = true;

            Debug.Log("========== Player Setup Validation ==========");

            // 1. Check Player Model
            if (playerModel == null)
            {
                playerModel = FindPlayerModel();
            }

            if (playerModel != null)
            {
                int layer = playerModel.gameObject.layer;
                string layerName = LayerMask.LayerToName(layer);

                if (layerName == "PlayerModel")
                {
                    Debug.Log("✓ Player Model: Found on correct layer (PlayerModel)");
                }
                else
                {
                    Debug.LogError($"✗ Player Model: On wrong layer '{layerName}' (should be 'PlayerModel')");
                    allValid = false;
                }
            }
            else
            {
                Debug.LogError("✗ Player Model: Not found");
                allValid = false;
            }

            // 2. Check Cameras
            if (firstPersonCamera == null)
                firstPersonCamera = GameObject.Find("FirstPersonCamera")?.GetComponent<CinemachineCamera>();
            if (thirdPersonCamera == null)
                thirdPersonCamera = GameObject.Find("ThirdPersonCamera")?.GetComponent<CinemachineCamera>();

            if (firstPersonCamera != null)
                Debug.Log("✓ First Person Camera: Found");
            else
            {
                Debug.LogError("✗ First Person Camera: Not found");
                allValid = false;
            }

            if (thirdPersonCamera != null)
                Debug.Log("✓ Third Person Camera: Found");
            else
            {
                Debug.LogError("✗ Third Person Camera: Not found");
                allValid = false;
            }

            // 3. Check Main Camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Debug.Log("✓ Main Camera: Found");

                // Check CameraLayerController
                CameraLayerController layerController = mainCam.GetComponent<CameraLayerController>();
                if (layerController != null)
                {
                    Debug.Log("✓ CameraLayerController: Attached to Main Camera");
                }
                else
                {
                    Debug.LogError("✗ CameraLayerController: Not found on Main Camera");
                    allValid = false;
                }

                // Check CinemachineBrain
                CinemachineBrain brain = mainCam.GetComponent<CinemachineBrain>();
                if (brain != null)
                {
                    Debug.Log("✓ CinemachineBrain: Attached to Main Camera");
                }
                else
                {
                    Debug.LogError("✗ CinemachineBrain: Not found on Main Camera");
                    allValid = false;
                }
            }
            else
            {
                Debug.LogError("✗ Main Camera: Not found");
                allValid = false;
            }

            // 4. Check UI
            UI.GamePlayerUI gameUI = UI.GamePlayerUI.Instance;
            if (gameUI != null)
            {
                Debug.Log("✓ GamePlayerUI: Instance available");
            }
            else
            {
                Debug.LogWarning("⚠ GamePlayerUI: Instance not found (may be created later)");
            }

            // 5. Check Input
            UI.InputManager input = UI.InputManager.Instance;
            if (input != null)
            {
                Debug.Log($"✓ InputManager: Instance available (Mobile: {input.IsMobile})");
            }
            else
            {
                Debug.LogWarning("⚠ InputManager: Instance not found (may be created later)");
            }

            Debug.Log($"========== Validation {(allValid ? "PASSED" : "FAILED")} ==========");
        }

        private Transform FindPlayerModel()
        {
            string[] names = { "PlayerModel", "Model", "Body", "Mesh", "Visual", "RBC", "Character", "Avatar" };
            foreach (string name in names)
            {
                Transform t = transform.Find(name);
                if (t != null) return t;
            }
            return null;
        }

        [ContextMenu("Test Camera Mode Switch")]
        public void TestCameraModeSwitch()
        {
            CinemachinePlayerController controller = GetComponent<CinemachinePlayerController>();
            if (controller != null)
            {
                Debug.Log("Testing camera mode switch...");
                if (controller.CurrentMode == CameraMode.FirstPerson)
                    controller.SwitchMode(CameraMode.ThirdPerson);
                else
                    controller.SwitchMode(CameraMode.FirstPerson);
            }
            else
            {
                Debug.LogError("CinemachinePlayerController not found");
            }
        }

        [ContextMenu("Log Current State")]
        public void LogCurrentState()
        {
            Debug.Log("========== Current Player State ==========");

            CinemachinePlayerController controller = GetComponent<CinemachinePlayerController>();
            if (controller != null)
            {
                Debug.Log($"Camera Mode: {controller.CurrentMode}");
                Debug.Log($"Input Enabled: {controller.InputEnabled}");
            }

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Debug.Log($"Camera Culling Mask: {mainCam.cullingMask}");

                CinemachineBrain brain = mainCam.GetComponent<CinemachineBrain>();
                if (brain != null && brain.ActiveVirtualCamera != null)
                {
                    Debug.Log($"Active Virtual Camera: {brain.ActiveVirtualCamera.Name}");
                }
            }

            if (playerModel != null)
            {
                Debug.Log($"Player Model Layer: {LayerMask.LayerToName(playerModel.gameObject.layer)} ({playerModel.gameObject.layer})");
            }

            UI.InputManager input = UI.InputManager.Instance;
            if (input != null)
            {
                Debug.Log($"Move Input: {input.MoveInput}");
                Debug.Log($"Look Input: {input.LookInput}");
            }

            Debug.Log("==========================================");
        }
    }
}
