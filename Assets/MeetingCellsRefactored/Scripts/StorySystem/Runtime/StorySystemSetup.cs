using UnityEngine;
using StorySystem.Execution;
using StorySystem.UI;

namespace StorySystem.Runtime
{
    /// <summary>
    /// Helper component to quickly set up the story system in a scene.
    /// Add this to an empty GameObject to create all required story system components.
    /// </summary>
    public class StorySystemSetup : MonoBehaviour
    {
        [Header("UI Setup")]
        [SerializeField] private bool createUI = true;
        [SerializeField] private Canvas targetCanvas;

        [Header("Manager Setup")]
        [SerializeField] private bool createStoryManager = true;

        [Header("Player Setup")]
        [SerializeField] private bool autoFindPlayer = true;
        [SerializeField] private GameObject playerObject;

        private void Awake()
        {
            Setup();
        }

        public void Setup()
        {
            if (createStoryManager)
            {
                EnsureStoryManager();
            }

            if (createUI)
            {
                EnsureUI();
            }

            if (autoFindPlayer && playerObject == null)
            {
                FindPlayer();
            }

            Debug.Log("[StorySystem] Setup complete!");
        }

        private void EnsureStoryManager()
        {
            // StoryManager is a singleton that creates itself on first access,
            // but we can force it to exist by accessing the Instance
            var manager = StoryManager.Instance;
            if (manager != null)
            {
                Debug.Log("[StorySystem] StoryManager is ready.");
            }
        }

        private void EnsureUI()
        {
            // Find or create canvas
            if (targetCanvas == null)
            {
                targetCanvas = FindObjectOfType<Canvas>();
                if (targetCanvas == null)
                {
                    Debug.LogWarning("[StorySystem] No Canvas found. Please create a UI Canvas for the story system.");
                    return;
                }
            }

            // Check for existing story UI components
            var dialogueUI = FindObjectOfType<DialogueUI>();
            var choiceUI = FindObjectOfType<ChoiceUI>();
            var cutsceneUI = FindObjectOfType<CutsceneUI>();

            if (dialogueUI == null)
            {
                Debug.Log("[StorySystem] No DialogueUI found. Please add the DialogueUI prefab to your canvas.");
            }

            if (choiceUI == null)
            {
                Debug.Log("[StorySystem] No ChoiceUI found. Please add the ChoiceUI prefab to your canvas.");
            }

            if (cutsceneUI == null)
            {
                Debug.Log("[StorySystem] No CutsceneUI found. Please add the CutsceneUI prefab to your canvas.");
            }
        }

        private void FindPlayer()
        {
            // Try to find player by tag
            playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                Debug.Log($"[StorySystem] Found player: {playerObject.name}");
            }
            else
            {
                Debug.LogWarning("[StorySystem] No player found with 'Player' tag. Story triggers may not work correctly.");
            }
        }

        /// <summary>
        /// Call this to test the story system with a sample graph
        /// </summary>
        [ContextMenu("Test Story System")]
        public void TestStorySystem()
        {
            if (StoryManager.Instance != null)
            {
                Debug.Log("[StorySystem] StorySystem is configured and ready!");
                Debug.Log($"[StorySystem] StoryManager Instance: {StoryManager.Instance.name}");

                var dialogueUI = FindObjectOfType<DialogueUI>();
                var choiceUI = FindObjectOfType<ChoiceUI>();
                var cutsceneUI = FindObjectOfType<CutsceneUI>();

                Debug.Log($"[StorySystem] DialogueUI: {(dialogueUI != null ? "Found" : "Missing")}");
                Debug.Log($"[StorySystem] ChoiceUI: {(choiceUI != null ? "Found" : "Missing")}");
                Debug.Log($"[StorySystem] CutsceneUI: {(cutsceneUI != null ? "Found" : "Missing")}");
            }
            else
            {
                Debug.LogError("[StorySystem] StoryManager not found!");
            }
        }
    }
}
