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
    }
}
