using UnityEngine;
using Unity.Cinemachine;

namespace MeetingCellsRefactored.Player
{
    [RequireComponent(typeof(CinemachineBrain))]
    [RequireComponent(typeof(Camera))]
    public class CameraLayerController : MonoBehaviour
    {
        [Header("Layer Settings")]
        [Tooltip("Layer for player model parts that should be hidden in first person")]
        [SerializeField] private LayerMask playerModelLayer = 256; // Layer 8 (1 << 8)
        [SerializeField] private LayerMask defaultViewLayers = ~0; // Everything by default

        [Header("Camera References")]
        [SerializeField] private CinemachineCamera firstPersonCamera;
        [SerializeField] private CinemachineCamera thirdPersonCamera;

        [Header("Player Model")]
        [Tooltip("Assign the player model transform here")]
        [SerializeField] private Transform playerModelRoot;

        [Header("Debug")]
        [SerializeField] private bool debugMode = true;

        private CinemachineBrain cinemachineBrain;
        private Camera mainCamera;
        private int playerModelLayerIndex = -1;
        private int fpCullingMask;
        private int tpCullingMask;
        private bool isInitialized = false;
        private CameraMode lastCameraMode = CameraMode.ThirdPerson;

        private void Awake()
        {
            cinemachineBrain = GetComponent<CinemachineBrain>();
            mainCamera = GetComponent<Camera>();

            InitializeLayerSettings();
        }

        private void Start()
        {
            // Try to find player model if not assigned
            if (playerModelRoot == null)
            {
                FindPlayerModel();
            }

            // Apply layer to player model
            if (playerModelRoot != null && playerModelLayerIndex != -1)
            {
                ApplyLayerToPlayerModel();
            }

            // Set initial culling mask
            UpdateCullingMask(true);
        }

        private void Update()
        {
            if (mainCamera == null || !isInitialized) return;

            // Check for camera mode change every frame
            UpdateCullingMask(false);
        }

        private void InitializeLayerSettings()
        {
            // Get layer index from the layer mask
            playerModelLayerIndex = GetLayerIndexFromMask(playerModelLayer);

            if (playerModelLayerIndex == -1)
            {
                // Try to find PlayerModel layer by name
                playerModelLayerIndex = LayerMask.NameToLayer("PlayerModel");
                if (playerModelLayerIndex != -1)
                {
                    playerModelLayer = 1 << playerModelLayerIndex;
                }
            }

            if (playerModelLayerIndex == -1)
            {
                Debug.LogError("[CameraLayerController] PlayerModel layer not found! Please create layer 'PlayerModel' (layer 8 recommended) in Edit > Project Settings > Tags and Layers");
                return;
            }

            // Calculate culling masks
            fpCullingMask = defaultViewLayers & ~(1 << playerModelLayerIndex); // Hide player model
            tpCullingMask = defaultViewLayers; // Show everything

            isInitialized = true;

            if (debugMode)
            {
                Debug.Log($"[CameraLayerController] Initialized - PlayerModel layer index: {playerModelLayerIndex}, FP Mask: {fpCullingMask}, TP Mask: {tpCullingMask}");
            }
        }

        private int GetLayerIndexFromMask(LayerMask mask)
        {
            int value = mask.value;
            for (int i = 0; i < 32; i++)
            {
                if ((value & (1 << i)) != 0)
                    return i;
            }
            return -1;
        }

        private void FindPlayerModel()
        {
            // Try to find via player controller
            var playerController = FindFirstObjectByType<CinemachinePlayerController>();
            if (playerController != null)
            {
                // Look for common model names
                string[] commonNames = { "Model", "Body", "Mesh", "Visual", "RBC", "Character", "Player" };
                foreach (Transform child in playerController.transform)
                {
                    foreach (string name in commonNames)
                    {
                        if (child.name.Contains(name))
                        {
                            playerModelRoot = child;
                            if (debugMode)
                                Debug.Log($"[CameraLayerController] Auto-found player model: {child.name}");
                            return;
                        }
                    }
                }
            }
        }

        private void ApplyLayerToPlayerModel()
        {
            if (playerModelRoot == null || playerModelLayerIndex == -1) return;

            int count = SetLayerRecursively(playerModelRoot, playerModelLayerIndex);

            if (debugMode)
                Debug.Log($"[CameraLayerController] Applied PlayerModel layer to {count} objects");
        }

        private int SetLayerRecursively(Transform parent, int layer)
        {
            int count = 0;
            if (parent == null) return count;

            if (parent.gameObject.layer != layer)
            {
                UndoLayerChange(parent.gameObject.layer);
                parent.gameObject.layer = layer;
                count++;
            }

            foreach (Transform child in parent)
            {
                count += SetLayerRecursively(child, layer);
            }

            return count;
        }

        private void UndoLayerChange(int previousLayer)
        {
            // This could be expanded to track changes for undo functionality
        }

        private void UpdateCullingMask(bool forceUpdate)
        {
            CameraMode currentMode = GetCurrentCameraMode();

            if (currentMode != lastCameraMode || forceUpdate)
            {
                lastCameraMode = currentMode;

                int targetMask = (currentMode == CameraMode.FirstPerson) ? fpCullingMask : tpCullingMask;
                mainCamera.cullingMask = targetMask;

                if (debugMode)
                {
                    Debug.Log($"[CameraLayerController] Camera mode: {currentMode}, CullingMask set to: {targetMask}");
                }
            }
        }

        private CameraMode GetCurrentCameraMode()
        {
            if (cinemachineBrain == null) return CameraMode.ThirdPerson;

            var activeCamera = cinemachineBrain.ActiveVirtualCamera;
            if (activeCamera == null) return CameraMode.ThirdPerson;

            // Check by reference
            if (firstPersonCamera != null && thirdPersonCamera != null)
            {
                // Use priority to determine active camera mode
                return firstPersonCamera.Priority > thirdPersonCamera.Priority
                    ? CameraMode.FirstPerson
                    : CameraMode.ThirdPerson;
            }

            // Fallback: check priority
            if (firstPersonCamera != null && thirdPersonCamera != null)
            {
                return firstPersonCamera.Priority > thirdPersonCamera.Priority
                    ? CameraMode.FirstPerson
                    : CameraMode.ThirdPerson;
            }

            return CameraMode.ThirdPerson;
        }

        /// <summary>
        /// Manually set the player model and apply layer
        /// </summary>
        public void SetPlayerModel(Transform modelRoot)
        {
            playerModelRoot = modelRoot;
            ApplyLayerToPlayerModel();
        }

        /// <summary>
        /// Force refresh of culling mask
        /// </summary>
        public void RefreshCullingMask()
        {
            UpdateCullingMask(true);
        }

        private void OnValidate()
        {
            if (playerModelLayer == 0)
            {
                int layerIndex = LayerMask.NameToLayer("PlayerModel");
                if (layerIndex != -1)
                    playerModelLayer = 1 << layerIndex;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (playerModelRoot != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(playerModelRoot.position, Vector3.one * 2f);
            }
        }
    }
}
