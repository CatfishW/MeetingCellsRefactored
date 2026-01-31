using System;
using UnityEngine;
using StorySystem.Core;
using StorySystem.Execution;

namespace StorySystem.Triggers
{
    /// <summary>
    /// Base story trigger that can start a story graph.
    /// </summary>
    public class StoryTrigger : MonoBehaviour
    {
        public enum StorySource
        {
            GraphAsset,
            GraphId,
            JsonPath
        }

        [Header("Source")]
        [SerializeField] private StorySource source = StorySource.GraphAsset;
        [SerializeField] private StoryGraph graphAsset;
        [SerializeField] private string graphId;
        [SerializeField] private string jsonPath;
        [SerializeField] private string startNodeId;

        [Header("Behavior")]
        [SerializeField] private bool playOnStart = false;
        [SerializeField] private bool playOnce = true;
        [SerializeField] private bool disableAfterPlay = true;

        [Header("Gizmo")]
        [SerializeField] private bool showGizmo = true;
        [SerializeField] private Color gizmoColor = new Color(0.2f, 0.8f, 0.4f, 0.8f);
        [SerializeField] private float gizmoSize = 0.5f;

        private bool hasPlayed;

        public event Action<StoryPlayer> OnTriggered;

        public bool ShowGizmo => showGizmo;
        public Color GizmoColor => gizmoColor;
        public float GizmoSize => gizmoSize;
        public StoryGraph GraphAsset => graphAsset;
        public string GraphId => graphId;
        public StorySource Source => source;

        protected virtual void Start()
        {
            if (playOnStart)
            {
                Trigger();
            }
        }

        public virtual void Trigger()
        {
            if (playOnce && hasPlayed)
            {
                return;
            }

            var graph = ResolveGraph();
            if (graph == null)
            {
                Debug.LogWarning($"StoryTrigger on {name} could not resolve graph.");
                return;
            }

            StoryPlayer player = PlayGraph(graph);
            if (player != null)
            {
                hasPlayed = true;
                OnTriggered?.Invoke(player);

                if (disableAfterPlay)
                {
                    enabled = false;
                }
            }
        }

        protected StoryGraph ResolveGraph()
        {
            switch (source)
            {
                case StorySource.GraphAsset:
                    return graphAsset;
                case StorySource.GraphId:
                    return string.IsNullOrEmpty(graphId) ? null : StoryManager.Instance?.LoadGraph(graphId);
                case StorySource.JsonPath:
                    return string.IsNullOrEmpty(jsonPath) ? null : StoryManager.Instance?.LoadGraphFromJson(jsonPath);
                default:
                    return null;
            }
        }

        protected StoryPlayer PlayGraph(StoryGraph graph)
        {
            if (StoryManager.Instance == null)
            {
                Debug.LogWarning("StoryManager instance not found.");
                return null;
            }

            if (!string.IsNullOrEmpty(startNodeId))
            {
                return StoryManager.Instance.PlayStory(graph, startNodeId);
            }

            return StoryManager.Instance.PlayStory(graph);
        }

        protected virtual void OnDrawGizmos()
        {
            if (!showGizmo)
                return;

            DrawStoryGizmo();
        }

        protected virtual void OnDrawGizmosSelected()
        {
            DrawStoryGizmoSelected();
        }

        protected virtual void DrawStoryGizmo()
        {
            Gizmos.color = hasPlayed && playOnce ?
                new Color(gizmoColor.r * 0.5f, gizmoColor.g * 0.5f, gizmoColor.b * 0.5f, 0.5f) :
                gizmoColor;

            // Draw story book icon shape
            Vector3 pos = transform.position;
            float size = gizmoSize;

            // Book base
            Gizmos.DrawCube(pos + Vector3.up * size * 0.5f, new Vector3(size * 0.8f, size, size * 0.1f));

            // Book spine
            Gizmos.color = Color.Lerp(gizmoColor, Color.black, 0.3f);
            Gizmos.DrawCube(pos + Vector3.up * size * 0.5f, new Vector3(size * 0.1f, size, size * 0.12f));

            // Play indicator triangle
            Gizmos.color = hasPlayed && playOnce ? Color.gray : Color.white;
            Vector3 triangleCenter = pos + Vector3.up * size * 0.5f + Vector3.forward * size * 0.06f;
            float triSize = size * 0.25f;

            // Draw play triangle
            Gizmos.DrawLine(triangleCenter + Vector3.left * triSize * 0.5f + Vector3.down * triSize * 0.5f,
                           triangleCenter + Vector3.left * triSize * 0.5f + Vector3.up * triSize * 0.5f);
            Gizmos.DrawLine(triangleCenter + Vector3.left * triSize * 0.5f + Vector3.up * triSize * 0.5f,
                           triangleCenter + Vector3.right * triSize * 0.5f);
            Gizmos.DrawLine(triangleCenter + Vector3.right * triSize * 0.5f,
                           triangleCenter + Vector3.left * triSize * 0.5f + Vector3.down * triSize * 0.5f);

            // Source indicator
            Gizmos.color = GetSourceColor();
            Gizmos.DrawWireSphere(pos + Vector3.up * size * 1.1f, size * 0.15f);
        }

        protected virtual void DrawStoryGizmoSelected()
        {
            // Draw connection line to show trigger reach
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * gizmoSize * 2f);

            // Draw label with graph info
#if UNITY_EDITOR
            string label = GetGizmoLabel();
            if (!string.IsNullOrEmpty(label))
            {
                UnityEditor.Handles.color = gizmoColor;
                UnityEditor.Handles.Label(transform.position + Vector3.up * gizmoSize * 2.2f, label);
            }
#endif
        }

        protected virtual Color GetSourceColor()
        {
            switch (source)
            {
                case StorySource.GraphAsset: return new Color(0.4f, 0.8f, 1f, 1f);   // Cyan for asset
                case StorySource.GraphId: return new Color(1f, 0.8f, 0.2f, 1f);      // Yellow for ID
                case StorySource.JsonPath: return new Color(0.8f, 0.4f, 1f, 1f);     // Purple for JSON
                default: return Color.gray;
            }
        }

        protected virtual string GetGizmoLabel()
        {
            string label = $"📖 Story Trigger\n";

            switch (source)
            {
                case StorySource.GraphAsset:
                    label += graphAsset != null ? $"Asset: {graphAsset.GraphName}" : "Asset: [Missing]";
                    break;
                case StorySource.GraphId:
                    label += $"ID: {graphId}";
                    break;
                case StorySource.JsonPath:
                    label += $"JSON: {jsonPath}";
                    break;
            }

            if (!string.IsNullOrEmpty(startNodeId))
                label += $"\nStart: {startNodeId}";

            if (playOnce && hasPlayed)
                label += "\n[Already Played]";
            else if (playOnStart)
                label += "\n[Plays on Start]";

            return label;
        }
    }
}
