using UnityEngine;

namespace StorySystem.Triggers
{
    /// <summary>
    /// Trigger zone that starts a story when the player enters/exits.
    /// </summary>
    public class StoryTriggerZone : StoryTrigger
    {
        [Header("Trigger Settings")]
        [SerializeField] private bool triggerOnEnter = true;
        [SerializeField] private bool triggerOnExit = false;
        [SerializeField] private string requiredTag = "Player";
        [SerializeField] private bool use2D = false;

        public bool TriggerOnEnter { get => triggerOnEnter; set => triggerOnEnter = value; }
        public bool TriggerOnExit { get => triggerOnExit; set => triggerOnExit = value; }
        public string RequiredTag { get => requiredTag; set => requiredTag = value; }
        public bool Use2D { get => use2D; set => use2D = value; }

        [Header("Zone Visualization")]
        [SerializeField] private Color enterZoneColor = new Color(0.2f, 1f, 0.2f, 0.3f);
        [SerializeField] private Color exitZoneColor = new Color(1f, 0.2f, 0.2f, 0.3f);
        [SerializeField] private Color wireColor = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField] private bool showZoneFill = true;

        private void OnTriggerEnter(Collider other)
        {
            if (use2D || !triggerOnEnter)
            {
                return;
            }

            if (IsValidTarget(other.gameObject))
            {
                Trigger();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (use2D || !triggerOnExit)
            {
                return;
            }

            if (IsValidTarget(other.gameObject))
            {
                Trigger();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!use2D || !triggerOnEnter)
            {
                return;
            }

            if (IsValidTarget(other.gameObject))
            {
                Trigger();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!use2D || !triggerOnExit)
            {
                return;
            }

            if (IsValidTarget(other.gameObject))
            {
                Trigger();
            }
        }

        private bool IsValidTarget(GameObject target)
        {
            if (string.IsNullOrEmpty(requiredTag))
            {
                return true;
            }

            return target.CompareTag(requiredTag);
        }

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            DrawZoneGizmo();
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            DrawZoneGizmoSelected();
        }

        private void DrawZoneGizmo()
        {
            if (!ShowGizmo)
                return;

            // Get collider bounds for zone visualization
            Bounds bounds = GetZoneBounds();
            if (bounds.size == Vector3.zero)
            {
                // Fallback if no collider - draw default zone
                DrawDefaultZone();
                return;
            }

            // Draw zone based on trigger settings
            if (triggerOnEnter && triggerOnExit)
            {
                // Both - gradient color
                Gizmos.color = Color.Lerp(enterZoneColor, exitZoneColor, 0.5f);
            }
            else if (triggerOnEnter)
            {
                Gizmos.color = enterZoneColor;
            }
            else if (triggerOnExit)
            {
                Gizmos.color = exitZoneColor;
            }
            else
            {
                Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.2f);
            }

            if (showZoneFill)
            {
                Gizmos.DrawCube(bounds.center, bounds.size);
            }

            // Draw wireframe
            Gizmos.color = wireColor;
            Gizmos.DrawWireCube(bounds.center, bounds.size);

            // Draw trigger direction indicators
            DrawDirectionIndicators(bounds);
        }

        private void DrawZoneGizmoSelected()
        {
            Bounds bounds = GetZoneBounds();

            // Draw extended connection lines when selected
            float extend = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * 0.5f + 1f;

            if (triggerOnEnter)
            {
                Gizmos.color = enterZoneColor;
                DrawArrow(bounds.center + Vector3.up * bounds.extents.y,
                         bounds.center + Vector3.up * (bounds.extents.y + 0.5f), 0.2f);
            }

            if (triggerOnExit)
            {
                Gizmos.color = exitZoneColor;
                DrawArrow(bounds.center - Vector3.up * bounds.extents.y,
                         bounds.center - Vector3.up * (bounds.extents.y + 0.5f), 0.2f);
            }

            // Draw tag indicator
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(requiredTag))
            {
                UnityEditor.Handles.color = Color.white;
                UnityEditor.Handles.Label(bounds.center + Vector3.right * bounds.extents.x,
                    $"Tag: {requiredTag}");
            }
#endif
        }

        private void DrawDefaultZone()
        {
            Vector3 center = transform.position;
            Vector3 size = Vector3.one * 2f;

            if (use2D)
            {
                size.z = 0.1f;
                center.z = 0f;
            }

            if (triggerOnEnter)
                Gizmos.color = enterZoneColor;
            else if (triggerOnExit)
                Gizmos.color = exitZoneColor;
            else
                Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.2f);

            if (showZoneFill)
                Gizmos.DrawCube(center, size);

            Gizmos.color = wireColor;
            Gizmos.DrawWireCube(center, size);
        }

        private Bounds GetZoneBounds()
        {
            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            bool hasCollider = false;

            if (use2D)
            {
                Collider2D col2D = GetComponent<Collider2D>();
                if (col2D != null)
                {
                    bounds = col2D.bounds;
                    hasCollider = true;
                }
            }
            else
            {
                Collider col = GetComponent<Collider>();
                if (col != null)
                {
                    bounds = col.bounds;
                    hasCollider = true;
                }
            }

            if (!hasCollider)
            {
                // Check for trigger colliders specifically
                foreach (var col in GetComponents<Collider>())
                {
                    if (col.isTrigger)
                    {
                        bounds = col.bounds;
                        hasCollider = true;
                        break;
                    }
                }

                foreach (var col2D in GetComponents<Collider2D>())
                {
                    if (col2D.isTrigger)
                    {
                        bounds = col2D.bounds;
                        hasCollider = true;
                        break;
                    }
                }
            }

            return bounds;
        }

        private void DrawDirectionIndicators(Bounds bounds)
        {
            Vector3 center = bounds.center;
            float size = Mathf.Min(bounds.size.x, bounds.size.z) * 0.15f;

            if (triggerOnEnter)
            {
                Gizmos.color = new Color(enterZoneColor.r, enterZoneColor.g, enterZoneColor.b, 0.8f);
                // Draw arrows pointing inward (enter)
                DrawArrow(center + Vector3.forward * bounds.extents.z, center + Vector3.forward * bounds.extents.z * 0.7f, size);
                DrawArrow(center - Vector3.forward * bounds.extents.z, center - Vector3.forward * bounds.extents.z * 0.7f, size);
                DrawArrow(center + Vector3.right * bounds.extents.x, center + Vector3.right * bounds.extents.x * 0.7f, size);
                DrawArrow(center - Vector3.right * bounds.extents.x, center - Vector3.right * bounds.extents.x * 0.7f, size);
            }

            if (triggerOnExit)
            {
                Gizmos.color = new Color(exitZoneColor.r, exitZoneColor.g, exitZoneColor.b, 0.8f);
                // Draw arrows pointing outward (exit)
                DrawArrow(center + Vector3.forward * bounds.extents.z * 0.7f, center + Vector3.forward * bounds.extents.z, size);
                DrawArrow(center - Vector3.forward * bounds.extents.z * 0.7f, center - Vector3.forward * bounds.extents.z, size);
                DrawArrow(center + Vector3.right * bounds.extents.x * 0.7f, center + Vector3.right * bounds.extents.x, size);
                DrawArrow(center - Vector3.right * bounds.extents.x * 0.7f, center - Vector3.right * bounds.extents.x, size);
            }
        }

        private void DrawArrow(Vector3 from, Vector3 to, float size)
        {
            Gizmos.DrawLine(from, to);

            Vector3 direction = (to - from).normalized;
            Vector3 right = Vector3.Cross(direction, Vector3.up).normalized * size * 0.3f;
            Vector3 up = Vector3.Cross(direction, Vector3.right).normalized * size * 0.3f;

            Gizmos.DrawLine(to, to - direction * size + right);
            Gizmos.DrawLine(to, to - direction * size - right);
            Gizmos.DrawLine(to, to - direction * size + up);
            Gizmos.DrawLine(to, to - direction * size - up);
        }

        protected override string GetGizmoLabel()
        {
            string label = base.GetGizmoLabel();

            if (use2D)
                label += "\n[2D Zone]";
            else
                label += "\n[3D Zone]";

            if (triggerOnEnter)
                label += " [OnEnter]";
            if (triggerOnExit)
                label += " [OnExit]";

            return label;
        }
    }
}
