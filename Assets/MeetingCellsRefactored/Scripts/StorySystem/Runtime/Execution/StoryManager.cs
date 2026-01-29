using System;
using System.Collections.Generic;
using UnityEngine;
using StorySystem.Core;
using StorySystem.Nodes;
using StorySystem.Serialization;

namespace StorySystem.Execution
{
    /// <summary>
    /// Central manager for the story system
    /// </summary>
    public class StoryManager : MonoBehaviour
    {
        private static StoryManager instance;
        public static StoryManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<StoryManager>();
                    if (instance == null)
                    {
                        var go = new GameObject("StoryManager");
                        instance = go.AddComponent<StoryManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        [SerializeField] private List<StoryGraph> preloadedGraphs = new List<StoryGraph>();
        [SerializeField] private StoryPlayer storyPlayerPrefab;
        
        private Dictionary<string, StoryGraph> graphCache = new Dictionary<string, StoryGraph>();
        private List<StoryPlayer> activePlayers = new List<StoryPlayer>();
        private StoryPlayer currentPlayer;
        private List<IStoryEventHandler> eventHandlers = new List<IStoryEventHandler>();

        public StoryPlayer CurrentPlayer => currentPlayer;
        public IReadOnlyList<StoryPlayer> ActivePlayers => activePlayers;

        public event Action<StoryEventData> OnStoryEvent;
        public event Action<StoryGraph> OnGraphLoaded;
        public event Action<StoryPlayer> OnPlayerCreated;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            // Cache preloaded graphs
            foreach (var graph in preloadedGraphs)
            {
                if (graph != null)
                {
                    graphCache[graph.GraphId] = graph;
                }
            }
        }

        #region Graph Management

        public StoryGraph LoadGraph(string graphId)
        {
            if (graphCache.TryGetValue(graphId, out var cached))
            {
                return cached;
            }

            // Try loading from Resources
            var graph = Resources.Load<StoryGraph>($"Stories/{graphId}");
            if (graph != null)
            {
                graphCache[graphId] = graph;
                OnGraphLoaded?.Invoke(graph);
                return graph;
            }

            Debug.LogError($"Could not load graph: {graphId}");
            return null;
        }

        public StoryGraph LoadGraphFromJson(string jsonPath)
        {
            var graph = StorySerializer.LoadFromJson(jsonPath);
            if (graph != null)
            {
                graphCache[graph.GraphId] = graph;
                OnGraphLoaded?.Invoke(graph);
            }
            return graph;
        }

        public void CacheGraph(StoryGraph graph)
        {
            if (graph != null)
            {
                graphCache[graph.GraphId] = graph;
            }
        }

        public void UnloadGraph(string graphId)
        {
            graphCache.Remove(graphId);
        }

        public void ClearCache()
        {
            graphCache.Clear();
        }

        #endregion

        #region Player Management

        public StoryPlayer PlayStory(StoryGraph graph)
        {
            var player = CreatePlayer();
            player.Play(graph);
            currentPlayer = player;
            return player;
        }

        public StoryPlayer PlayStory(StoryGraph graph, string startNodeId)
        {
            var player = CreatePlayer();
            if (string.IsNullOrEmpty(startNodeId))
            {
                player.Play(graph);
            }
            else
            {
                player.Play(graph, startNodeId);
            }
            currentPlayer = player;
            return player;
        }

        public StoryPlayer PlayStory(string graphId)
        {
            var graph = LoadGraph(graphId);
            if (graph != null)
            {
                return PlayStory(graph);
            }
            return null;
        }

        public StoryPlayer PlayStory(string graphId, string startNodeId)
        {
            var graph = LoadGraph(graphId);
            if (graph != null)
            {
                return PlayStory(graph, startNodeId);
            }
            return null;
        }

        public StoryPlayer PlayStoryFromJson(string jsonPath)
        {
            var graph = LoadGraphFromJson(jsonPath);
            if (graph != null)
            {
                return PlayStory(graph);
            }
            return null;
        }

        public StoryPlayer PlayStoryFromJson(string jsonPath, string startNodeId)
        {
            var graph = LoadGraphFromJson(jsonPath);
            if (graph != null)
            {
                return PlayStory(graph, startNodeId);
            }
            return null;
        }

        private StoryPlayer CreatePlayer()
        {
            StoryPlayer player;
            
            if (storyPlayerPrefab != null)
            {
                player = Instantiate(storyPlayerPrefab, transform);
            }
            else
            {
                var go = new GameObject("StoryPlayer");
                go.transform.SetParent(transform);
                player = go.AddComponent<StoryPlayer>();
            }

            player.OnStoryEnd += (graph, completed) => OnPlayerFinished(player);
            activePlayers.Add(player);
            OnPlayerCreated?.Invoke(player);
            
            return player;
        }

        private void OnPlayerFinished(StoryPlayer player)
        {
            activePlayers.Remove(player);
            if (currentPlayer == player)
            {
                currentPlayer = activePlayers.Count > 0 ? activePlayers[0] : null;
            }
        }

        public void StopAllStories()
        {
            foreach (var player in activePlayers.ToArray())
            {
                player.Stop();
            }
            activePlayers.Clear();
            currentPlayer = null;
        }

        #endregion

        #region Event System

        public void RegisterEventHandler(IStoryEventHandler handler)
        {
            if (!eventHandlers.Contains(handler))
            {
                eventHandlers.Add(handler);
            }
        }

        public void UnregisterEventHandler(IStoryEventHandler handler)
        {
            eventHandlers.Remove(handler);
        }

        public void TriggerEvent(StoryEventData eventData)
        {
            OnStoryEvent?.Invoke(eventData);

            foreach (var handler in eventHandlers)
            {
                if (handler.CanHandle(eventData))
                {
                    handler.HandleEvent(eventData);
                }
            }
        }

        #endregion

        #region Input Forwarding

        public void SendInput()
        {
            currentPlayer?.SendInput();
        }

        public void SelectChoice(int choiceIndex)
        {
            currentPlayer?.SelectChoice(choiceIndex);
        }

        #endregion

        #region Save/Load

        public Dictionary<string, object> SaveGameState()
        {
            var state = new Dictionary<string, object>();

            if (currentPlayer != null && currentPlayer.IsExecuting)
            {
                state["currentPlayer"] = currentPlayer.SaveState();
            }

            return state;
        }

        public void LoadGameState(Dictionary<string, object> state)
        {
            StopAllStories();

            if (state.TryGetValue("currentPlayer", out var playerState) && 
                playerState is Dictionary<string, object> playerDict)
            {
                if (playerDict.TryGetValue("graphId", out var graphId))
                {
                    var graph = LoadGraph(graphId.ToString());
                    if (graph != null)
                    {
                        var player = CreatePlayer();
                        player.LoadState(playerDict, graph);
                        currentPlayer = player;
                    }
                }
            }
        }

        #endregion
    }
}
