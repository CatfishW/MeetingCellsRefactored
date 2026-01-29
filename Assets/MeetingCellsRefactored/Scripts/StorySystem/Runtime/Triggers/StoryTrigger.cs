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

        private bool hasPlayed;

        public event Action<StoryPlayer> OnTriggered;

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
    }
}
