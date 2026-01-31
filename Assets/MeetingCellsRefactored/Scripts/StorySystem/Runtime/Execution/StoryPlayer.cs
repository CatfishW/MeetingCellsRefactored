using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StorySystem.Core;
using StorySystem.Nodes;

namespace StorySystem.Execution
{
    /// <summary>
    /// Executes story graphs node by node
    /// </summary>
    public class StoryPlayer : MonoBehaviour
    {
        [SerializeField] private StoryGraph currentGraph;
        [SerializeField] private bool autoStart = false;
        [SerializeField] private bool debugMode = false;

        private StoryContext context;
        private Coroutine executionCoroutine;
        private bool isExecuting;
        private bool isPaused;
        private bool inputReceived;
        private string selectedPortId;

        public StoryGraph CurrentGraph => currentGraph;
        public StoryContext Context => context;
        public StoryNode CurrentNode => context?.CurrentNode;
        public bool IsExecuting => isExecuting;
        public bool IsPaused => isPaused;

        public event Action<StoryNode> OnNodeEnter;
        public event Action<StoryNode> OnNodeExit;
        public event Action<StoryGraph> OnStoryStart;
        public event Action<StoryGraph, bool> OnStoryEnd; // bool = completed successfully
        public event Action<string> OnError;

        private void Start()
        {
            if (autoStart && currentGraph != null)
            {
                Play(currentGraph);
            }
        }

        public void Play(StoryGraph graph)
        {
            if (isExecuting)
            {
                Stop();
            }

            currentGraph = graph;
            context = new StoryContext(graph);
            
            var startNode = graph.GetStartNode();
            if (startNode == null)
            {
                OnError?.Invoke("No start node found in graph");
                return;
            }

            OnStoryStart?.Invoke(graph);
            executionCoroutine = StartCoroutine(ExecuteGraph(startNode));
        }

        public void Play(StoryGraph graph, string startNodeId)
        {
            if (isExecuting)
            {
                Stop();
            }

            currentGraph = graph;
            context = new StoryContext(graph);
            
            var startNode = graph.GetNode(startNodeId);
            if (startNode == null)
            {
                OnError?.Invoke($"Node with ID '{startNodeId}' not found");
                return;
            }

            OnStoryStart?.Invoke(graph);
            executionCoroutine = StartCoroutine(ExecuteGraph(startNode));
        }

        public void Stop()
        {
            if (executionCoroutine != null)
            {
                StopCoroutine(executionCoroutine);
                executionCoroutine = null;
            }

            if (CurrentNode != null)
            {
                CurrentNode.OnExit(context);
            }

            isExecuting = false;
            OnStoryEnd?.Invoke(currentGraph, context?.IsComplete ?? false);
        }

        public void Pause()
        {
            isPaused = true;
            context.IsPaused = true;
        }

        public void Resume()
        {
            isPaused = false;
            context.IsPaused = false;
        }

        public void SendInput()
        {
            inputReceived = true;
        }

        public void SelectChoice(int choiceIndex)
        {
            if (CurrentNode is ChoiceNode choiceNode)
            {
                selectedPortId = choiceNode.SelectChoice(choiceIndex, context);
                inputReceived = true;
            }
        }

        public void SelectPort(string portId)
        {
            selectedPortId = portId;
            inputReceived = true;
        }

        private IEnumerator ExecuteGraph(StoryNode startNode)
        {
            isExecuting = true;
            StoryNode currentNode = startNode;

            while (currentNode != null && isExecuting)
            {
                // Check for pause
                while (isPaused)
                {
                    yield return null;
                }

                // Enter node
                if (debugMode)
                {
                    Debug.Log($"[StoryPlayer] Entering node: {currentNode.DisplayName} ({currentNode.NodeId})");
                }

                context.SetCurrentNode(currentNode);
                currentNode.OnEnter(context);
                OnNodeEnter?.Invoke(currentNode);

                // Check for breakpoint in debug mode
                if (debugMode && currentNode.IsBreakpoint)
                {
                    Debug.Log($"[StoryPlayer] Breakpoint hit at: {currentNode.DisplayName}");
                    isPaused = true;
                    while (isPaused)
                    {
                        yield return null;
                    }
                }

                // Execute node
                StoryNodeResult result = currentNode.Execute(context);

                // Handle result
                string nextPortId = result.NextPortId;

                switch (result.Type)
                {
                    case StoryNodeResultType.Continue:
                        // Immediate continue
                        break;

                    case StoryNodeResultType.Wait:
                        yield return new WaitForSeconds(result.WaitTime);
                        break;

                    case StoryNodeResultType.WaitForCondition:
                        while (result.WaitCondition != null && !result.WaitCondition())
                        {
                            yield return null;
                        }
                        break;

                    case StoryNodeResultType.WaitForInput:
                        inputReceived = false;
                        selectedPortId = null;
                        
                        while (!inputReceived)
                        {
                            yield return null;
                        }
                        
                        if (!string.IsNullOrEmpty(selectedPortId))
                        {
                            nextPortId = selectedPortId;
                        }
                        break;

                    case StoryNodeResultType.End:
                        currentNode.OnExit(context);
                        OnNodeExit?.Invoke(currentNode);
                        isExecuting = false;
                        OnStoryEnd?.Invoke(currentGraph, true);
                        yield break;
                }

                // Exit current node
                currentNode.OnExit(context);
                OnNodeExit?.Invoke(currentNode);

                // Find next node
                currentNode = currentGraph.GetConnectedNode(currentNode.NodeId, nextPortId);

                if (currentNode == null && debugMode)
                {
                    Debug.Log($"[StoryPlayer] No connected node found for port: {nextPortId}");
                }
            }

            isExecuting = false;
            OnStoryEnd?.Invoke(currentGraph, context.IsComplete);
        }

        public void JumpToNode(string nodeId)
        {
            var node = currentGraph.GetNode(nodeId);
            if (node == null)
            {
                OnError?.Invoke($"Cannot jump to node: {nodeId} not found");
                return;
            }

            Stop();
            executionCoroutine = StartCoroutine(ExecuteGraph(node));
        }

        public Dictionary<string, object> SaveState()
        {
            return new Dictionary<string, object>
            {
                { "graphId", currentGraph.GraphId },
                { "currentNodeId", CurrentNode?.NodeId },
                { "variables", context.SaveState() }
            };
        }

        public void LoadState(Dictionary<string, object> state, StoryGraph graph)
        {
            currentGraph = graph;
            context = new StoryContext(graph);
            
            if (state.TryGetValue("variables", out var vars) && vars is Dictionary<string, object> variables)
            {
                context.LoadState(variables);
            }

            if (state.TryGetValue("currentNodeId", out var nodeId) && nodeId != null)
            {
                var node = graph.GetNode(nodeId.ToString());
                if (node != null)
                {
                    executionCoroutine = StartCoroutine(ExecuteGraph(node));
                }
            }
        }
    }
}