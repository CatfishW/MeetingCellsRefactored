#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using MeetingCellsRefactored.Player;

namespace MeetingCellsRefactored.Editor
{
    /// <summary>
    /// Editor utility to help set up player model layers for first-person camera culling
    /// </summary>
    public class PlayerLayerSetupEditor : EditorWindow
    {
        private GameObject playerPrefab;
        private bool showHelp = true;

        [MenuItem("Tools/Meeting Cells/Setup Player Layers")]
        public static void ShowWindow()
        {
            GetWindow<PlayerLayerSetupEditor>("Player Layer Setup");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Player Model Layer Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (showHelp)
            {
                EditorGUILayout.HelpBox(
                    "This tool helps set up the PlayerModel layer for first-person camera culling. " +
                    "When in first-person mode, the player model should be hidden to prevent seeing " +
                    "eyebrows, hair, or other parts of your own character.",
                    MessageType.Info);
                EditorGUILayout.Space(10);
            }

            playerPrefab = EditorGUILayout.ObjectField(
                "Player Prefab",
                playerPrefab,
                typeof(GameObject),
                false) as GameObject;

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Apply PlayerModel Layer to Prefab"))
            {
                if (playerPrefab != null)
                {
                    ApplyLayerToPrefab();
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Please assign a Player Prefab first.", "OK");
                }
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Find and Setup Player in Scene"))
            {
                SetupPlayerInScene();
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Verify Layer Configuration"))
            {
                VerifyLayerSetup();
            }
        }

        private void ApplyLayerToPrefab()
        {
            int playerModelLayer = LayerMask.NameToLayer("PlayerModel");
            if (playerModelLayer == -1)
            {
                EditorUtility.DisplayDialog(
                    "Layer Not Found",
                    "The 'PlayerModel' layer does not exist. Please add it in Edit > Project Settings > Tags and Layers (layer 8 is recommended).",
                    "OK");
                return;
            }

            // Apply layer to all children of the prefab
            Undo.RecordObject(playerPrefab, "Apply PlayerModel Layer");
            int changedCount = SetLayerRecursively(playerPrefab.transform, playerModelLayer);

            EditorUtility.SetDirty(playerPrefab);
            PrefabUtility.RecordPrefabInstancePropertyModifications(playerPrefab);

            EditorUtility.DisplayDialog(
                "Success",
                $"Applied 'PlayerModel' layer to {changedCount} objects in the prefab.\n\n" +
                $"Note: Make sure your Main Camera has the CameraLayerController component attached.",
                "OK");
        }

        private void SetupPlayerInScene()
        {
            var playerController = FindFirstObjectByType<CinemachinePlayerController>();
            if (playerController == null)
            {
                EditorUtility.DisplayDialog(
                    "Player Not Found",
                    "No CinemachinePlayerController found in the current scene.",
                    "OK");
                return;
            }

            int playerModelLayer = LayerMask.NameToLayer("PlayerModel");
            if (playerModelLayer == -1)
            {
                EditorUtility.DisplayDialog(
                    "Layer Not Found",
                    "The 'PlayerModel' layer does not exist. Please add it in Edit > Project Settings > Tags and Layers.",
                    "OK");
                return;
            }

            // Find and setup the model
            Transform playerModel = null;
            foreach (Transform child in playerController.transform)
            {
                if (child.name.Contains("Model") || child.name.Contains("Body") ||
                    child.name.Contains("Mesh") || child.name.Contains("Visual") ||
                    child.name.Contains("RBC") || child.name.Contains("Character"))
                {
                    playerModel = child;
                    break;
                }
            }

            if (playerModel != null)
            {
                Undo.RecordObject(playerModel.gameObject, "Setup Player Model Layer");
                int changedCount = SetLayerRecursively(playerModel, playerModelLayer);

                EditorUtility.DisplayDialog(
                    "Success",
                    $"Found and configured player model: {playerModel.name}\n" +
                    $"Applied 'PlayerModel' layer to {changedCount} objects.\n\n" +
                    $"Total children processed.",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Model Not Found",
                    "Could not find a player model. Looking for names containing: Model, Body, Mesh, Visual, RBC, or Character.",
                    "OK");
            }
        }

        private void VerifyLayerSetup()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                EditorUtility.DisplayDialog("Error", "No Main Camera found in scene.", "OK");
                return;
            }

            var layerController = camera.GetComponent<CameraLayerController>();

            if (layerController == null)
            {
                EditorUtility.DisplayDialog(
                    "CameraLayerController Missing",
                    "The Main Camera does not have a CameraLayerController component.\n\n" +
                    "Please add the CameraLayerController component to your Main Camera.",
                    "OK");
                return;
            }

            int playerModelLayer = LayerMask.NameToLayer("PlayerModel");
            if (playerModelLayer == -1)
            {
                EditorUtility.DisplayDialog(
                    "Layer Not Found",
                    "The 'PlayerModel' layer does not exist in project settings.",
                    "OK");
                return;
            }

            // Check for objects on PlayerModel layer
            var playerObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int playerModelObjectCount = 0;
            foreach (var obj in playerObjects)
            {
                if (obj.layer == playerModelLayer)
                {
                    playerModelObjectCount++;
                }
            }

            EditorUtility.DisplayDialog(
                "Verification Results",
                $"PlayerModel layer: EXIST (index {playerModelLayer})\n" +
                $"CameraLayerController: ATTACHED\n" +
                $"Objects on PlayerModel layer: {playerModelObjectCount}\n\n" +
                $"Setup appears correct!",
                "OK");
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
    }
}
#endif // UNITY_EDITOR
