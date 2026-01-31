using UnityEngine;
using StorySystem.Execution;
using StorySystem.Core;

namespace StorySystem.Runtime.Examples
{
    /// <summary>
    /// Demo script showing how to use the story system programmatically
    /// </summary>
    public class StorySystemDemo : MonoBehaviour
    {
        [Header("Demo Story")]
        [SerializeField] private StoryGraph demoStory;

        [Header("UI")]
        [SerializeField] private bool showDebugUI = true;

        private void OnGUI()
        {
            if (!showDebugUI) return;

            GUILayout.BeginArea(new Rect(10, 10, 250, 300));
            GUILayout.BeginVertical("box");

            GUILayout.Label("Story System Demo", GUILayout.Height(30));
            GUILayout.Space(10);

            if (StoryManager.Instance == null)
            {
                GUILayout.Label("StoryManager: Not Ready");
            }
            else
            {
                GUILayout.Label($"StoryManager: Ready");

                var currentPlayer = StoryManager.Instance.CurrentPlayer;
                if (currentPlayer != null)
                {
                    GUILayout.Label($"Current Node: {currentPlayer.CurrentNode?.DisplayName ?? "None"}");
                    GUILayout.Label($"Is Executing: {currentPlayer.IsExecuting}");
                }
                else
                {
                    GUILayout.Label("No active story");
                }
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Test Story Manager"))
            {
                TestStoryManager();
            }

            if (demoStory != null && GUILayout.Button("Play Demo Story"))
            {
                PlayDemoStory();
            }

            if (GUILayout.Button("Stop All Stories"))
            {
                StopAllStories();
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void TestStoryManager()
        {
            if (StoryManager.Instance != null)
            {
                Debug.Log("[StorySystemDemo] StoryManager is working!");
                Debug.Log($"- Instance: {StoryManager.Instance.name}");
                Debug.Log($"- Active Players: {StoryManager.Instance.ActivePlayers.Count}");
            }
            else
            {
                Debug.LogError("[StorySystemDemo] StoryManager not found!");
            }
        }

        private void PlayDemoStory()
        {
            if (demoStory == null)
            {
                Debug.LogWarning("[StorySystemDemo] No demo story assigned!");
                return;
            }

            if (StoryManager.Instance != null)
            {
                var player = StoryManager.Instance.PlayStory(demoStory);
                if (player != null)
                {
                    Debug.Log($"[StorySystemDemo] Started story: {demoStory.GraphName}");
                }
            }
        }

        private void StopAllStories()
        {
            StoryManager.Instance?.StopAllStories();
            Debug.Log("[StorySystemDemo] Stopped all stories");
        }
    }
}
