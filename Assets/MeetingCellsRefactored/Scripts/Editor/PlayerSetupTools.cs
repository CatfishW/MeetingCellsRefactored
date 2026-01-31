using UnityEngine;
using UnityEditor;
using MeetingCellsRefactored.Player;
using MeetingCellsRefactored.UI;

namespace MeetingCellsRefactored.Editor
{
    public class PlayerSetupTools : UnityEditor.EditorWindow
    {
        [MenuItem("MeetingCells/Setup Player Controller")]
        public static void SetupPlayer()
        {
            var player = GameObject.FindFirstObjectByType<CinemachinePlayerController>();
            if (player == null)
            {
                Debug.LogError("No CinemachinePlayerController found in scene!");
                return;
            }

            Debug.Log($"Found player: {player.name}");
            Undo.RecordObject(player, "Setup Player Controller");

            // 1. Find references
            var inputManager = GameObject.FindFirstObjectByType<InputManager>();
            var camLayer = GameObject.FindFirstObjectByType<CameraLayerController>();
            
            // 2. Setup InputManager if needed
            if (inputManager == null)
            {
                Debug.LogWarning("InputManager not found in scene. Creating one...");
                var imObj = new GameObject("InputManager");
                inputManager = imObj.AddComponent<InputManager>();
                Undo.RegisterCreatedObjectUndo(imObj, "Create InputManager");
            }
            
            // 3. Ensure player has CharacterController
            if (player.GetComponent<UnityEngine.CharacterController>() == null)
            {
                player.gameObject.AddComponent<UnityEngine.CharacterController>();
                Debug.Log("Added CharacterController to player");
            }

            // 4. Manual Assignment for CameraLayerController
            // Assuming CameraLayerController has a setter for PlayerModel
            if (camLayer != null)
            {
                Transform model = player.transform.Find("PlayerModel");
                // Fallback search
                if (model == null)
                {
                    foreach(Transform child in player.transform)
                    {
                         if (child.name.Contains("Model") || child.name.Contains("Body"))
                         {
                             model = child;
                             break;
                         }
                    }
                }

                if (model != null)
                {
                    camLayer.SetPlayerModel(model);
                    Debug.Log($"Assigned PlayerModel '{model.name}' to CameraLayerController");
                    
                    // Assign Animator if missing
                    var anim = model.GetComponent<Animator>();
                    if (anim != null)
                    {
                        SerializedObject so = new SerializedObject(player);
                        so.Update();
                        so.FindProperty("animator").objectReferenceValue = anim;
                        so.ApplyModifiedProperties();
                        Debug.Log("Assigned Animator from PlayerModel");
                    }
                }
                else
                {
                    Debug.LogWarning("Could not find a 'PlayerModel' child to assign to CameraLayerController.");
                }
            }

            // 5. Populate CinemachinePlayerController fields using SerializedObject
            SerializedObject playerSO = new SerializedObject(player);
            playerSO.Update();

            // Camera Target
            if (playerSO.FindProperty("cameraTarget").objectReferenceValue == null)
            {
                playerSO.FindProperty("cameraTarget").objectReferenceValue = player.transform;
                Debug.Log("Assigned CameraTarget to Player Transform");
            }

            // Ground Check
            if (playerSO.FindProperty("groundCheck").objectReferenceValue == null)
            {
                Transform gc = player.transform.Find("GroundCheck");
                if (gc == null)
                {
                    GameObject gcObj = new GameObject("GroundCheck");
                    gcObj.transform.SetParent(player.transform);
                    gcObj.transform.localPosition = new Vector3(0, -0.9f, 0);
                    gc = gcObj.transform;
                    Undo.RegisterCreatedObjectUndo(gcObj, "Create GroundCheck");
                }
                playerSO.FindProperty("groundCheck").objectReferenceValue = gc;
                Debug.Log("Assigned GroundCheck");
            }

            // Ground Mask (Default to 'Default' layer if Nothing)
            SerializedProperty groundMaskProp = playerSO.FindProperty("groundMask");
            if (groundMaskProp.intValue == 0)
            {
                groundMaskProp.intValue = 1; // Default layer
                Debug.Log("Set GroundMask to Default");
            }

            // FP Camera Root
            if (playerSO.FindProperty("fpCameraRoot").objectReferenceValue == null)
            {
                Transform fpRoot = player.transform.Find("FPCameraRoot");
                if (fpRoot == null)
                {
                    GameObject fpObj = new GameObject("FPCameraRoot");
                    fpObj.transform.SetParent(player.transform);
                    fpObj.transform.localPosition = new Vector3(0, 1.6f, 0);
                    fpRoot = fpObj.transform;
                    Undo.RegisterCreatedObjectUndo(fpObj, "Create FPCameraRoot");
                }
                playerSO.FindProperty("fpCameraRoot").objectReferenceValue = fpRoot;
                Debug.Log("Assigned FPCameraRoot");
            }

            // 6. Cameras Setup
            // First Person Camera
            if (playerSO.FindProperty("firstPersonCamera").objectReferenceValue == null)
            {
                // Try to find it first
                Transform fpCamTran = player.transform.Find("FPCameraRoot/FirstPersonCamera");
                if (fpCamTran == null)
                {
                    // Create it
                    GameObject fpCamObj = new GameObject("FirstPersonCamera");
                    Transform fpRoot = (Transform)playerSO.FindProperty("fpCameraRoot").objectReferenceValue;
                    if (fpRoot != null)
                    {
                        fpCamObj.transform.SetParent(fpRoot);
                    }
                    else
                    {
                         fpCamObj.transform.SetParent(player.transform);
                    }
                    fpCamObj.transform.localPosition = Vector3.zero;
                    fpCamObj.transform.localRotation = Quaternion.identity;
                    
                    var cam = fpCamObj.AddComponent<Unity.Cinemachine.CinemachineCamera>();
                    cam.Lens.FieldOfView = 60f;
                    cam.Priority = 10; // Default low priority

                    Undo.RegisterCreatedObjectUndo(fpCamObj, "Create FirstPersonCamera");
                    playerSO.FindProperty("firstPersonCamera").objectReferenceValue = cam;
                    Debug.Log("Created and Assigned FirstPersonCamera");
                }
                else
                {
                    var cam = fpCamTran.GetComponent<Unity.Cinemachine.CinemachineCamera>();
                    if (cam == null) cam = fpCamTran.gameObject.AddComponent<Unity.Cinemachine.CinemachineCamera>();
                    playerSO.FindProperty("firstPersonCamera").objectReferenceValue = cam;
                    Debug.Log("Assigned existing FirstPersonCamera");
                }
            }

            // Third Person Camera
            if (playerSO.FindProperty("thirdPersonCamera").objectReferenceValue == null)
            {
                // Try to find it
                Transform tpCamTran = player.transform.Find("ThirdPersonCamera");
                if (tpCamTran == null)
                {
                    GameObject tpCamObj = new GameObject("ThirdPersonCamera");
                    // Important: Parent it to player or keep it separate? Runtime script assumes it manages it?
                    // Typically Cinemachine brains live on MainCamera, Virtual Cams can be anywhere.
                    // Runtime script parents the FollowTarget to player, but the Camera object itself...
                    // "var camObj = new GameObject("ThirdPersonCamera");" -> no parent specified in runtime script initially?
                    // But usually we want it organized. Let's parent to Player for now to keep scene clean.
                    tpCamObj.transform.SetParent(player.transform); 
                    
                    var cam = tpCamObj.AddComponent<Unity.Cinemachine.CinemachineCamera>();
                    cam.Lens.FieldOfView = 60f;
                    cam.Priority = 10;
                    
                    // Create Follow Target
                    Transform followTarget = player.transform.Find("TPCameraFollow");
                    if (followTarget == null)
                    {
                        GameObject ftObj = new GameObject("TPCameraFollow");
                        ftObj.transform.SetParent(player.transform);
                        ftObj.transform.localPosition = new Vector3(0, 1.5f, 0);
                        followTarget = ftObj.transform;
                        Undo.RegisterCreatedObjectUndo(ftObj, "Create TPCameraFollow Target");
                    }
                    
                    cam.Target.TrackingTarget = followTarget;
                    // cam.Target.CustomLookAtTarget = followTarget; // Standard orbital doesn't strictly need LookAt if using OrbitalFollow often

                    // Add Orbit
                    var orbiter = tpCamObj.AddComponent<Unity.Cinemachine.CinemachineOrbitalFollow>();
                    orbiter.OrbitStyle = Unity.Cinemachine.CinemachineOrbitalFollow.OrbitStyles.ThreeRing;
                    
                    // GTA Style defaults
                    orbiter.TrackerSettings.PositionDamping = Vector3.zero;
                    orbiter.TrackerSettings.RotationDamping = Vector3.zero;
                    orbiter.HorizontalAxis.Recentering.Enabled = false;
                    orbiter.HorizontalAxis.Value = 0f;
                    orbiter.VerticalAxis.Value = 20f;
                    orbiter.RadialAxis.Value = 5f; // Default

                    Undo.RegisterCreatedObjectUndo(tpCamObj, "Create ThirdPersonCamera");
                    playerSO.FindProperty("thirdPersonCamera").objectReferenceValue = cam;
                    Debug.Log("Created and Assigned ThirdPersonCamera");
                }
                else
                {
                    var cam = tpCamTran.GetComponent<Unity.Cinemachine.CinemachineCamera>();
                    if (cam == null) cam = tpCamTran.gameObject.AddComponent<Unity.Cinemachine.CinemachineCamera>();
                    playerSO.FindProperty("thirdPersonCamera").objectReferenceValue = cam;
                    Debug.Log("Assigned existing ThirdPersonCamera");
                }
            }

            playerSO.ApplyModifiedProperties();

            Debug.Log("Player Controller Setup Actions Completed.");
        }
    }
}
