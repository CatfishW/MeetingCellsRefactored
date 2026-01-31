using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using StorySystem.Core;

namespace StorySystem.Optimization
{
    /// <summary>
    /// High-performance cache for story graph lookups
    /// Pre-builds hash maps for O(1) node and connection access
    /// </summary>
    public class StoryGraphCache
    {
        private StoryGraph _graph;

        // Native hash maps for fast lookups
        private NativeHashMap<int, int> _nodeIdToIndex;
        private NativeHashMap<int, int> _connectionIdToIndex;
        private NativeParallelMultiHashMap<int, int> _nodeToOutputConnections;
        private NativeParallelMultiHashMap<int, int> _nodeToInputConnections;
        private NativeHashMap<long, int> _portToConnection; // Combined hash of node+port

        // Flat arrays for cache-friendly iteration
        private StoryNode[] _nodes;
        private StoryConnection[] _connections;

        private bool _isInitialized;
        private bool _isDisposed;

        public bool IsInitialized => _isInitialized;
        public StoryGraph Graph => _graph;

        public StoryGraphCache(StoryGraph graph)
        {
            _graph = graph;
        }

        public void Initialize()
        {
            if (_isInitialized) return;

            var nodes = _graph.Nodes;
            var connections = _graph.Connections;

            // Allocate native collections
            _nodeIdToIndex = new NativeHashMap<int, int>(nodes.Count, Allocator.Persistent);
            _connectionIdToIndex = new NativeHashMap<int, int>(connections.Count, Allocator.Persistent);
            _nodeToOutputConnections = new NativeParallelMultiHashMap<int, int>(connections.Count, Allocator.Persistent);
            _nodeToInputConnections = new NativeParallelMultiHashMap<int, int>(connections.Count, Allocator.Persistent);
            _portToConnection = new NativeHashMap<long, int>(connections.Count, Allocator.Persistent);

            // Copy nodes to array for fast access
            _nodes = new StoryNode[nodes.Count];
            for (int i = 0; i < nodes.Count; i++)
            {
                _nodes[i] = nodes[i];
                _nodeIdToIndex.TryAdd(nodes[i].NodeId.GetHashCode(), i);
            }

            // Copy connections to array and build lookup maps
            _connections = new StoryConnection[connections.Count];
            for (int i = 0; i < connections.Count; i++)
            {
                var conn = connections[i];
                _connections[i] = conn;

                int connHash = conn.ConnectionId.GetHashCode();
                _connectionIdToIndex.TryAdd(connHash, i);

                int outputNodeHash = conn.OutputNodeId.GetHashCode();
                int inputNodeHash = conn.InputNodeId.GetHashCode();

                _nodeToOutputConnections.Add(outputNodeHash, i);
                _nodeToInputConnections.Add(inputNodeHash, i);

                // Create unique key from node+port for direct lookup
                long portKey = GetPortKey(outputNodeHash, conn.OutputPortId.GetHashCode());
                _portToConnection.TryAdd(portKey, i);
            }

            _isInitialized = true;
        }

        /// <summary>
        /// Get node by ID - O(1) lookup
        /// </summary>
        public StoryNode GetNode(string nodeId)
        {
            if (!_isInitialized) Initialize();

            int hash = nodeId.GetHashCode();
            if (_nodeIdToIndex.TryGetValue(hash, out int index))
            {
                return _nodes[index];
            }
            return null;
        }

        /// <summary>
        /// Get connection by ID - O(1) lookup
        /// </summary>
        public StoryConnection GetConnection(string connectionId)
        {
            if (!_isInitialized) Initialize();

            int hash = connectionId.GetHashCode();
            if (_connectionIdToIndex.TryGetValue(hash, out int index))
            {
                return _connections[index];
            }
            return null;
        }

        /// <summary>
        /// Get connection from a specific port - O(1) lookup
        /// </summary>
        public StoryConnection GetConnectionFromPort(string nodeId, string portId)
        {
            if (!_isInitialized) Initialize();

            long key = GetPortKey(nodeId.GetHashCode(), portId.GetHashCode());
            if (_portToConnection.TryGetValue(key, out int index))
            {
                return _connections[index];
            }
            return null;
        }

        /// <summary>
        /// Get connected node from a specific port - O(1) lookup
        /// </summary>
        public StoryNode GetConnectedNode(string nodeId, string outputPortId)
        {
            var connection = GetConnectionFromPort(nodeId, outputPortId);
            if (connection != null)
            {
                return GetNode(connection.InputNodeId);
            }
            return null;
        }

        /// <summary>
        /// Get all connections from a node - O(1) to get iterator
        /// </summary>
        public IEnumerable<StoryConnection> GetConnectionsFromNode(string nodeId)
        {
            if (!_isInitialized) Initialize();

            int hash = nodeId.GetHashCode();
            if (_nodeToOutputConnections.TryGetFirstValue(hash, out int index, out var iterator))
            {
                do
                {
                    yield return _connections[index];
                }
                while (_nodeToOutputConnections.TryGetNextValue(out index, ref iterator));
            }
        }

        /// <summary>
        /// Get all connections to a node - O(1) to get iterator
        /// </summary>
        public IEnumerable<StoryConnection> GetConnectionsToNode(string nodeId)
        {
            if (!_isInitialized) Initialize();

            int hash = nodeId.GetHashCode();
            if (_nodeToInputConnections.TryGetFirstValue(hash, out int index, out var iterator))
            {
                do
                {
                    yield return _connections[index];
                }
                while (_nodeToInputConnections.TryGetNextValue(out index, ref iterator));
            }
        }

        /// <summary>
        /// Get start node - cached for fast access
        /// </summary>
        public StoryNode GetStartNode()
        {
            if (!_isInitialized) Initialize();

            for (int i = 0; i < _nodes.Length; i++)
            {
                if (_nodes[i] is StorySystem.Nodes.StartNode)
                {
                    return _nodes[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Batch get nodes by type - cache-friendly iteration
        /// </summary>
        public void GetNodesOfType<T>(List<T> results) where T : StoryNode
        {
            if (!_isInitialized) Initialize();

            results.Clear();
            for (int i = 0; i < _nodes.Length; i++)
            {
                if (_nodes[i] is T typed)
                {
                    results.Add(typed);
                }
            }
        }

        /// <summary>
        /// Rebuild cache when graph changes
        /// </summary>
        public void Rebuild()
        {
            Dispose();
            _isInitialized = false;
            _isDisposed = false;
            Initialize();
        }

        private long GetPortKey(int nodeHash, int portHash)
        {
            // Combine two 32-bit hashes into one 64-bit key
            return ((long)nodeHash << 32) | (uint)portHash;
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            if (_nodeIdToIndex.IsCreated) _nodeIdToIndex.Dispose();
            if (_connectionIdToIndex.IsCreated) _connectionIdToIndex.Dispose();
            if (_nodeToOutputConnections.IsCreated) _nodeToOutputConnections.Dispose();
            if (_nodeToInputConnections.IsCreated) _nodeToInputConnections.Dispose();
            if (_portToConnection.IsCreated) _portToConnection.Dispose();
            _nodes = null;
            _connections = null;

            _isDisposed = true;
        }
    }
}
