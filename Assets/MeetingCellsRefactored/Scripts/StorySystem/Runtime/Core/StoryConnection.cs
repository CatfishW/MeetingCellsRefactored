using System;
using UnityEngine;

namespace StorySystem.Core
{
    /// <summary>
    /// Represents a connection between two nodes
    /// </summary>
    [Serializable]
    public class StoryConnection
    {
        [SerializeField] private string connectionId;
        [SerializeField] private string outputNodeId;
        [SerializeField] private string outputPortId;
        [SerializeField] private string inputNodeId;
        [SerializeField] private string inputPortId;

        public string ConnectionId => connectionId;
        public string OutputNodeId => outputNodeId;
        public string OutputPortId => outputPortId;
        public string InputNodeId => inputNodeId;
        public string InputPortId => inputPortId;

        public StoryConnection(string id, string outNodeId, string outPortId, 
            string inNodeId, string inPortId)
        {
            connectionId = id;
            outputNodeId = outNodeId;
            outputPortId = outPortId;
            inputNodeId = inNodeId;
            inputPortId = inPortId;
        }

        public override string ToString()
        {
            return $"Connection[{connectionId}]: {outputNodeId}.{outputPortId} -> {inputNodeId}.{inputPortId}";
        }
    }
}