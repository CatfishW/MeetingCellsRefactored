using UnityEditor;
using UnityEngine;
using StorySystem.Triggers;

namespace StorySystem.Editor
{
    /// <summary>
    /// Menu items and utilities for creating story triggers
    /// </summary>
    public static class StoryTriggerMenus
    {
        [MenuItem("GameObject/Story System/Story Trigger", false, 10)]
        public static void CreateStoryTrigger()
        {
            GameObject go = new GameObject("Story Trigger", typeof(StoryTrigger));
            go.transform.position = GetSpawnPosition();
            Selection.activeGameObject = go;
            Undo.RegisterCreatedObjectUndo(go, "Create Story Trigger");
        }

        [MenuItem("GameObject/Story System/Story Trigger Zone (3D)", false, 11)]
        public static void CreateStoryTriggerZone3D()
        {
            GameObject go = new GameObject("Story Trigger Zone", typeof(StoryTriggerZone), typeof(BoxCollider));
            go.transform.position = GetSpawnPosition();

            var collider = go.GetComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(4f, 2f, 4f);

            var trigger = go.GetComponent<StoryTriggerZone>();

            Selection.activeGameObject = go;
            Undo.RegisterCreatedObjectUndo(go, "Create Story Trigger Zone 3D");
        }

        [MenuItem("GameObject/Story System/Story Trigger Zone (2D)", false, 12)]
        public static void CreateStoryTriggerZone2D()
        {
            GameObject go = new GameObject("Story Trigger Zone 2D", typeof(StoryTriggerZone), typeof(BoxCollider2D));
            go.transform.position = GetSpawnPosition();

            var collider = go.GetComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(4f, 2f);

            var trigger = go.GetComponent<StoryTriggerZone>();
            trigger.Use2D = true;

            Selection.activeGameObject = go;
            Undo.RegisterCreatedObjectUndo(go, "Create Story Trigger Zone 2D");
        }

        [MenuItem("Tools/Story System/Select All Triggers", false, 100)]
        public static void SelectAllTriggers()
        {
            var triggers = Object.FindObjectsOfType<StoryTrigger>();
            if (triggers.Length > 0)
            {
                var selection = new GameObject[triggers.Length];
                for (int i = 0; i < triggers.Length; i++)
                {
                    selection[i] = triggers[i].gameObject;
                }
                Selection.objects = selection;
            }
            else
            {
                EditorUtility.DisplayDialog("Story System", "No story triggers found in the scene.", "OK");
            }
        }

        [MenuItem("Tools/Story System/Toggle All Gizmos", false, 101)]
        public static void ToggleAllGizmos()
        {
            var triggers = Object.FindObjectsOfType<StoryTrigger>();
            bool anyVisible = false;

            foreach (var trigger in triggers)
            {
                if (trigger.ShowGizmo)
                {
                    anyVisible = true;
                    break;
                }
            }

            Undo.RecordObjects(triggers, "Toggle All Trigger Gizmos");

            foreach (var trigger in triggers)
            {
                // Toggle visibility
                var so = new SerializedObject(trigger);
                var prop = so.FindProperty("showGizmo");
                if (prop != null)
                {
                    prop.boolValue = !anyVisible;
                    so.ApplyModifiedProperties();
                }
            }

            EditorUtility.DisplayDialog("Story System",
                anyVisible ? "All trigger gizmos hidden." : "All trigger gizmos shown.", "OK");
        }

        private static Vector3 GetSpawnPosition()
        {
            // Try to spawn in front of scene view camera
            var view = SceneView.lastActiveSceneView;
            if (view != null && view.camera != null)
            {
                Camera cam = view.camera;
                return cam.transform.position + cam.transform.forward * 5f;
            }

            // Fallback to world origin
            return Vector3.zero;
        }
    }
}
