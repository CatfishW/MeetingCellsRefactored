using UnityEngine;
using Unity.Cinemachine;
using UnityEditor;
using MeetingCellsRefactored.Player;
using MeetingCellsRefactored.UI;
using UnityEditor.SceneManagement;

namespace MeetingCellsRefactored.Editor
{
    public static class SetupPlayerCameraReferences
    {
        [MenuItem("Meeting Cells/Setup Player Camera References")]
        public static void SetupReferences()
        {
            var player = GameObject.Find("Player");
            var firstPersonCamera = GameObject.Find("FirstPersonCamera");
            var thirdPersonCamera = GameObject.Find("ThirdPersonCamera");
            var mainCamera = Camera.main;
            var cameraModeController = GameObject.Find("CameraModeController");

            if (player == null)
            {
                Debug.LogError("Player GameObject not found!");
                return;
            }

            if (firstPersonCamera == null)
            {
                Debug.LogError("FirstPersonCamera not found!");
                return;
            }

            if (thirdPersonCamera == null)
            {
                Debug.LogError("ThirdPersonCamera not found!");
                return;
            }

            var fpCam = firstPersonCamera.GetComponent<CinemachineCamera>();
            var tpCam = thirdPersonCamera.GetComponent<CinemachineCamera>();

            // Setup ThirdPersonCamera Tracking
            if (tpCam != null)
            {
                Undo.RecordObject(tpCam, "Setup TP Camera Tracking");
                tpCam.Follow = player.transform;
                tpCam.Target.TrackingTarget = player.transform;
                EditorUtility.SetDirty(tpCam);
                Debug.Log("✓ ThirdPersonCamera tracking target set to Player");
            }

            // Setup FirstPersonCamera
            if (fpCam != null)
            {
                Undo.RecordObject(fpCam, "Setup FP Camera");
                fpCam.Lens.FieldOfView = 70f;
                fpCam.Lens.NearClipPlane = 0.01f;
                EditorUtility.SetDirty(fpCam);
                Debug.Log("✓ FirstPersonCamera configured");
            }

            // Setup Player controller
            var playerController = player.GetComponent<CinemachinePlayerController>();
            if (playerController != null)
            {
                Undo.RecordObject(playerController, "Setup Player Camera References");
                
                var fpField = typeof(CinemachinePlayerController).GetField("firstPersonCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var tpField = typeof(CinemachinePlayerController).GetField("thirdPersonCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var targetField = typeof(CinemachinePlayerController).GetField("cameraTarget", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var fpRootField = typeof(CinemachinePlayerController).GetField("fpCameraRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (fpField != null) fpField.SetValue(playerController, fpCam);
                if (tpField != null) tpField.SetValue(playerController, tpCam);
                if (targetField != null) targetField.SetValue(playerController, player.transform);
                if (fpRootField != null) fpRootField.SetValue(playerController, firstPersonCamera.transform);
                
                EditorUtility.SetDirty(playerController);
                Debug.Log("✓ Player camera references assigned");
            }

            // Setup CameraModeController
            if (cameraModeController != null)
            {
                var camModeCtrl = cameraModeController.GetComponent<CameraModeController>();
                if (camModeCtrl != null)
                {
                    Undo.RecordObject(camModeCtrl, "Setup Camera Mode Controller");
                    
                    var fpField = typeof(CameraModeController).GetField("firstPersonCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var tpField = typeof(CameraModeController).GetField("thirdPersonCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var playerField = typeof(CameraModeController).GetField("playerTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (fpField != null) fpField.SetValue(camModeCtrl, fpCam);
                    if (tpField != null) tpField.SetValue(camModeCtrl, tpCam);
                    if (playerField != null) playerField.SetValue(camModeCtrl, player.transform);
                    
                    EditorUtility.SetDirty(camModeCtrl);
                    Debug.Log("✓ CameraModeController references assigned");
                }
            }

            // Setup CameraLayerController
            if (mainCamera != null)
            {
                var layerController = mainCamera.GetComponent<CameraLayerController>();
                if (layerController != null)
                {
                    Undo.RecordObject(layerController, "Setup Camera Layer Controller");
                    
                    var fpField = typeof(CameraLayerController).GetField("firstPersonCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var tpField = typeof(CameraLayerController).GetField("thirdPersonCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (fpField != null) fpField.SetValue(layerController, fpCam);
                    if (tpField != null) tpField.SetValue(layerController, tpCam);
                    
                    EditorUtility.SetDirty(layerController);
                    Debug.Log("✓ CameraLayerController references assigned");
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("✅ Player camera setup complete!");
        }
    }
}
