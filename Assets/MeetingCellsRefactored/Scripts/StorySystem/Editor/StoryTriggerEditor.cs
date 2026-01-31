using UnityEditor;
using UnityEngine;
using StorySystem.Triggers;

namespace StorySystem.Editor
{
    [CustomEditor(typeof(StoryTrigger), true)]
    public class StoryTriggerEditor : UnityEditor.Editor
    {
        private static readonly Color HandleColor = new Color(0.2f, 0.8f, 0.4f, 1f);
        private static readonly Color SelectedHandleColor = new Color(1f, 0.8f, 0.2f, 1f);
        private static readonly Color DisabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        private StoryTrigger trigger;
        private bool isEditing;

        private void OnEnable()
        {
            trigger = (StoryTrigger)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Gizmo Controls", EditorStyles.boldLabel);

            if (GUILayout.Button("Toggle Gizmo Visibility"))
            {
                Undo.RecordObject(trigger, "Toggle Gizmo");
                var showField = serializedObject.FindProperty("showGizmo");
                if (showField != null)
                {
                    showField.boolValue = !showField.boolValue;
                    serializedObject.ApplyModifiedProperties();
                }
            }

            EditorGUILayout.HelpBox(
                "Gizmo Colors:\n" +
                "🟢 Cyan = Graph Asset source\n" +
                "🟡 Yellow = Graph ID source\n" +
                "🟣 Purple = JSON Path source\n" +
                "⚫ Gray = Already played (Play Once)",
                MessageType.Info);
        }

        private void OnSceneGUI()
        {
            if (!trigger.ShowGizmo)
                return;

            DrawTriggerHandles();
            DrawInteractionPreview();
        }

        private void DrawTriggerHandles()
        {
            Handles.color = Selection.activeGameObject == trigger.gameObject ?
                SelectedHandleColor : HandleColor;

            Vector3 pos = trigger.transform.position;
            float size = HandleUtility.GetHandleSize(pos) * 0.3f;

            // Draw a circular handle around the trigger
            Handles.DrawWireDisc(pos, Vector3.up, size);

            // Draw direction indicator
            Vector3 forward = trigger.transform.forward * size;
            Handles.DrawLine(pos, pos + forward);
            Handles.ConeHandleCap(0, pos + forward, Quaternion.LookRotation(forward), size * 0.2f, EventType.Repaint);

            // Draw size handle
            EditorGUI.BeginChangeCheck();
            float newSize = Handles.ScaleSlider(trigger.GizmoSize, pos, Vector3.up, Quaternion.identity, size * 2f, 0.1f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(trigger, "Change Gizmo Size");
                var sizeProp = serializedObject.FindProperty("gizmoSize");
                if (sizeProp != null)
                {
                    sizeProp.floatValue = Mathf.Max(0.1f, newSize);
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private void DrawInteractionPreview()
        {
            if (trigger is StoryTriggerZone zone)
            {
                DrawZonePreview(zone);
            }
            else
            {
                DrawPointTriggerPreview();
            }
        }

        private void DrawZonePreview(StoryTriggerZone zone)
        {
            // Draw wire sphere showing interaction range
            Handles.color = new Color(1f, 1f, 1f, 0.1f);
            Handles.DrawWireDisc(zone.transform.position, Vector3.up, 2f);

            // Draw player position indicator when in play mode
            if (Application.isPlaying)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    float distance = Vector3.Distance(player.transform.position, zone.transform.position);
                    bool inRange = distance < 2f;

                    Handles.color = inRange ? Color.green : Color.red;
                    Handles.DrawLine(zone.transform.position, player.transform.position);

                    Vector3 midPoint = Vector3.Lerp(zone.transform.position, player.transform.position, 0.5f);
                    GUIStyle style = new GUIStyle();
                    style.normal.textColor = inRange ? Color.green : Color.red;
                    Handles.Label(midPoint + Vector3.up * 0.5f, $"{distance:F1}m", style);
                }
            }
        }

        private void DrawPointTriggerPreview()
        {
            // Draw activation radius for point triggers
            Handles.color = new Color(trigger.GizmoColor.r, trigger.GizmoColor.g, trigger.GizmoColor.b, 0.1f);

            float radius = 1f;
            Handles.DrawWireDisc(trigger.transform.position, Vector3.up, radius);

            // Draw height indicator
            Vector3 top = trigger.transform.position + Vector3.up * trigger.GizmoSize;
            Vector3 bottom = trigger.transform.position;
            Handles.DrawDottedLine(bottom, top, 5f);
        }
    }

    [CustomEditor(typeof(StoryTriggerZone))]
    public class StoryTriggerZoneEditor : StoryTriggerEditor
    {
        private StoryTriggerZone zoneTrigger;

        private void OnEnable()
        {
            zoneTrigger = (StoryTriggerZone)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Zone Visualization", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Zone Colors:\n" +
                "🟢 Green fill = Trigger On Enter\n" +
                "🔴 Red fill = Trigger On Exit\n" +
                "🟠 Orange fill = Both Enter & Exit\n" +
                "➡️ Arrows show trigger direction",
                MessageType.Info);

            if (GUILayout.Button("Auto-size to Collider"))
            {
                AutoSizeToCollider();
            }
        }

        private void AutoSizeToCollider()
        {
            Undo.RecordObject(zoneTrigger, "Auto-size Gizmo");

            // Find collider and adjust gizmo
            Collider col = zoneTrigger.GetComponent<Collider>();
            Collider2D col2D = zoneTrigger.GetComponent<Collider2D>();

            if (col != null)
            {
                var sizeProp = serializedObject.FindProperty("gizmoSize");
                if (sizeProp != null)
                {
                    sizeProp.floatValue = Mathf.Max(col.bounds.size.x, col.bounds.size.y, col.bounds.size.z) * 0.5f;
                    serializedObject.ApplyModifiedProperties();
                }
            }
            else if (col2D != null)
            {
                var sizeProp = serializedObject.FindProperty("gizmoSize");
                if (sizeProp != null)
                {
                    sizeProp.floatValue = Mathf.Max(col2D.bounds.size.x, col2D.bounds.size.y) * 0.5f;
                    serializedObject.ApplyModifiedProperties();
                }
            }
            else
            {
                EditorUtility.DisplayDialog("No Collider Found",
                    "Please add a Collider or Collider2D component to this GameObject for auto-sizing.", "OK");
            }
        }
    }
}
