using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StorySystem.Core
{
    /// <summary>
    /// Represents a complete story graph containing all nodes and connections.
    /// This is the main data container for a story.
    /// </summary>
    [Serializable]
    public class StoryGraph : ScriptableObject
    {
        [SerializeField] private string graphId;
        [SerializeField] private string graphName;
        [SerializeField] private string description;
        [SerializeField] private List<StoryNode> nodes = new List<StoryNode>();
        [SerializeField] private List<StoryConnection> connections = new List<StoryConnection>();
        [SerializeField] private List<StoryVariable> variables = new List<StoryVariable>();
        [SerializeField] private Vector2 viewOffset;
        [SerializeField] private float viewScale = 1f;

        public string GraphId => graphId;
        public string GraphName => graphName;
        public string Description => description;
        public IReadOnlyList<StoryNode> Nodes => nodes;
        public IReadOnlyList<StoryConnection> Connections => connections;
        public IReadOnlyList<StoryVariable> Variables => variables;
        public Vector2 ViewOffset { get => viewOffset; set => viewOffset = value; }
        public float ViewScale { get => viewScale; set => viewScale = value; }

        public void Initialize(string id, string name)
        {
            graphId = id;
            graphName = name;
        }

        public void SetName(string name) => graphName = name;
        public void SetDescription(string desc) => description = desc;

        #region Node Management

        public T CreateNode<T>(Vector2 position) where T : StoryNode, new()
        {
            var node = CreateInstance<T>();
            node.Initialize(Guid.NewGuid().ToString(), position);
            node.name = typeof(T).Name;
            AddNode(node);
            return node;
        }

        public StoryNode CreateNode(Type nodeType, Vector2 position)
        {
            if (!typeof(StoryNode).IsAssignableFrom(nodeType))
                throw new ArgumentException($"Type {nodeType} is not a StoryNode");

            var node = (StoryNode)CreateInstance(nodeType);
            node.Initialize(Guid.NewGuid().ToString(), position);
            node.name = nodeType.Name;
            AddNode(node);
            return node;
        }

        public void AddNode(StoryNode node)
        {
            if (node == null || nodes.Contains(node)) return;
            
            nodes.Add(node);
            
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.AddObjectToAsset(node, this);
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public void RemoveNode(StoryNode node)
        {
            if (node == null) return;

            // Remove all connections involving this node
            var connectionsToRemove = connections
                .Where(c => c.OutputNodeId == node.NodeId || c.InputNodeId == node.NodeId)
                .ToList();

            foreach (var connection in connectionsToRemove)
            {
                RemoveConnection(connection);
            }

            nodes.Remove(node);

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.RemoveObjectFromAsset(node);
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public StoryNode GetNode(string nodeId)
        {
            return nodes.FirstOrDefault(n => n.NodeId == nodeId);
        }

        public T GetNode<T>(string nodeId) where T : StoryNode
        {
            return GetNode(nodeId) as T;
        }

        public StoryNode GetStartNode()
        {
            return nodes.FirstOrDefault(n => n is Nodes.StartNode);
        }

        public IEnumerable<T> GetNodesOfType<T>() where T : StoryNode
        {
            return nodes.OfType<T>();
        }

        #endregion

        #region Connection Management

        public StoryConnection CreateConnection(string outputNodeId, string outputPortId, 
            string inputNodeId, string inputPortId)
        {
            // Validate nodes exist
            var outputNode = GetNode(outputNodeId);
            var inputNode = GetNode(inputNodeId);
            
            if (outputNode == null || inputNode == null)
            {
                Debug.LogError("Cannot create connection: node not found");
                return null;
            }

            // Check for duplicate connections
            if (connections.Any(c => 
                c.OutputNodeId == outputNodeId && c.OutputPortId == outputPortId &&
                c.InputNodeId == inputNodeId && c.InputPortId == inputPortId))
            {
                return null;
            }

            var connection = new StoryConnection(
                Guid.NewGuid().ToString(),
                outputNodeId, outputPortId,
                inputNodeId, inputPortId
            );

            connections.Add(connection);
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
            
            return connection;
        }

        public void RemoveConnection(StoryConnection connection)
        {
            connections.Remove(connection);
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public void RemoveConnection(string connectionId)
        {
            var connection = connections.FirstOrDefault(c => c.ConnectionId == connectionId);
            if (connection != null)
            {
                RemoveConnection(connection);
            }
        }

        public IEnumerable<StoryConnection> GetConnectionsFromNode(string nodeId)
        {
            return connections.Where(c => c.OutputNodeId == nodeId);
        }

        public IEnumerable<StoryConnection> GetConnectionsToNode(string nodeId)
        {
            return connections.Where(c => c.InputNodeId == nodeId);
        }

        public StoryConnection GetConnectionFromPort(string nodeId, string portId)
        {
            return connections.FirstOrDefault(c => 
                c.OutputNodeId == nodeId && c.OutputPortId == portId);
        }

        public StoryNode GetConnectedNode(string nodeId, string outputPortId)
        {
            var connection = GetConnectionFromPort(nodeId, outputPortId);
            return connection != null ? GetNode(connection.InputNodeId) : null;
        }

        #endregion

        #region Variable Management

        public void AddVariable(StoryVariable variable)
        {
            if (!variables.Any(v => v.Name == variable.Name))
            {
                variables.Add(variable);
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        public void RemoveVariable(string variableName)
        {
            variables.RemoveAll(v => v.Name == variableName);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public StoryVariable GetVariable(string variableName)
        {
            return variables.FirstOrDefault(v => v.Name == variableName);
        }

        #endregion

        #region Validation

        public List<string> Validate()
        {
            var errors = new List<string>();

            // Check for start node
            if (!nodes.Any(n => n is Nodes.StartNode))
            {
                errors.Add("Graph must have at least one Start node");
            }

            // Check for orphaned connections
            foreach (var connection in connections)
            {
                if (GetNode(connection.OutputNodeId) == null)
                    errors.Add($"Connection {connection.ConnectionId} has invalid output node");
                if (GetNode(connection.InputNodeId) == null)
                    errors.Add($"Connection {connection.ConnectionId} has invalid input node");
            }

            // Validate each node
            foreach (var node in nodes)
            {
                errors.AddRange(node.Validate());
            }

            return errors;
        }

        #endregion

        public void Clear()
        {
            foreach (var node in nodes.ToList())
            {
                RemoveNode(node);
            }
            connections.Clear();
            variables.Clear();
        }
    }
}