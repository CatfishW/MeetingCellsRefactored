using UnityEngine;

namespace MeetingCellsRefactored.Player
{
    /// <summary>
    /// Sets up player components on startup
    /// Ensures proper layer assignment and camera configuration
    /// </summary>
    public class PlayerSetup : MonoBehaviour
    {
        [Header("Setup Settings")]
        [SerializeField] private bool setupOnStart = true;
        [SerializeField] private bool debugMode = true;

        [Header("Player Model")]
        [Tooltip("The transform containing the player visual model")]
        [SerializeField] private Transform playerModelRoot;

        [Header("Layer Settings")]
        [SerializeField] private string playerModelLayerName = "PlayerModel";

        private void Start()
        {
            if (setupOnStart)
            {
                SetupPlayer();
            }
        }

        public void SetupPlayer()
        {
            if (debugMode)
                Debug.Log("[PlayerSetup] Starting player setup...");

            // 1. Find player model if not assigned
            if (playerModelRoot == null)
            {
                FindPlayerModel();
            }

            // 2. Apply PlayerModel layer
            ApplyPlayerModelLayer();

            // 3. Setup camera culling
            SetupCameraCulling();

            if (debugMode)
                Debug.Log("[PlayerSetup] Player setup complete");
        }

        private void FindPlayerModel()
        {
            // Common names for player model containers
            string[] commonNames = { "PlayerModel", "Model", "Body", "Mesh", "Visual", "RBC", "Character", "Avatar" };

            foreach (string name in commonNames)
            {
                Transform found = transform.Find(name);
                if (found != null)
                {
                    playerModelRoot = found;
                    if (debugMode)
                        Debug.Log($"[PlayerSetup] Found player model: {name}");
                    return;
                }
            }

            // If still not found, look for SkinnedMeshRenderer in children
            SkinnedMeshRenderer smr = GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null)
            {
                playerModelRoot = smr.transform.parent; // Use parent as model root
                if (debugMode)
                    Debug.Log($"[PlayerSetup] Found player model via SkinnedMeshRenderer: {playerModelRoot.name}");
            }
        }

        private void ApplyPlayerModelLayer()
        {
            if (playerModelRoot == null)
            {
                Debug.LogError("[PlayerSetup] Cannot apply layer - player model not found");
                return;
            }

            int layer = LayerMask.NameToLayer(playerModelLayerName);
            if (layer == -1)
            {
                Debug.LogError($"[PlayerSetup] Layer '{playerModelLayerName}' not found in project!");
                return;
            }

            int count = SetLayerRecursively(playerModelRoot, layer);

            if (debugMode)
                Debug.Log($"[PlayerSetup] Applied layer '{playerModelLayerName}' to {count} objects");
        }

        private int SetLayerRecursively(Transform parent, int layer)
        {
            int count = 0;
            if (parent == null) return count;

            if (parent.gameObject.layer != layer)
            {
                parent.gameObject.layer = layer;
                count++;
            }

            foreach (Transform child in parent)
            {
                count += SetLayerRecursively(child, layer);
            }

            return count;
        }

        private void SetupCameraCulling()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("[PlayerSetup] No Main Camera found!");
                return;
            }

            // Check if CameraLayerController exists
            CameraLayerController layerController = mainCamera.GetComponent<CameraLayerController>();
            if (layerController == null)
            {
                // Add it
                layerController = mainCamera.gameObject.AddComponent<CameraLayerController>();
                if (debugMode)
                    Debug.Log("[PlayerSetup] Added CameraLayerController to Main Camera");
            }

            // Set the player model
            if (playerModelRoot != null)
            {
                layerController.SetPlayerModel(playerModelRoot);
            }

            // Force refresh
            layerController.RefreshCullingMask();

            if (debugMode)
                Debug.Log("[PlayerSetup] Camera culling configured");
        }

        /// <summary>
        /// Manually set the player model root
        /// </summary>
        public void SetPlayerModel(Transform modelRoot)
        {
            playerModelRoot = modelRoot;
            ApplyPlayerModelLayer();
            SetupCameraCulling();
        }

        private void OnDrawGizmosSelected()
        {
            if (playerModelRoot != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireCube(playerModelRoot.position, Vector3.one * 1.5f);
                Gizmos.DrawLine(transform.position, playerModelRoot.position);
            }
        }
    }
}
