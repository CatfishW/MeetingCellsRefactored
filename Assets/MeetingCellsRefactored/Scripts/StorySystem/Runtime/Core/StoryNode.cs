using System;
using System.Collections.Generic;
using UnityEngine;

namespace StorySystem.Core
{
    /// <summary>
    /// Base class for all story nodes. Each node represents a single story element.
    /// </summary>
    [Serializable]
    public abstract class StoryNode : ScriptableObject
    {
        [SerializeField] protected string nodeId;
        [SerializeField] protected string nodeName;
        [SerializeField] protected string nodeDescription;
        [SerializeField] protected Vector2 position;
        [SerializeField] protected List<StoryPort> inputPorts = new List<StoryPort>();
        [SerializeField] protected List<StoryPort> outputPorts = new List<StoryPort>();
        [SerializeField] protected bool isBreakpoint;

        public string NodeId => nodeId;
        public string NodeName { get => nodeName; set => nodeName = value; }
        public string NodeDescription { get => nodeDescription; set => nodeDescription = value; }
        public Vector2 Position { get => position; set => position = value; }
        public IReadOnlyList<StoryPort> InputPorts => inputPorts;
        public IReadOnlyList<StoryPort> OutputPorts => outputPorts;
        public bool IsBreakpoint { get => isBreakpoint; set => isBreakpoint = value; }

        /// <summary>
        /// Display name for the node in the editor
        /// </summary>
        public abstract string DisplayName { get; }

        /// <summary>
        /// Category for node search/organization
        /// </summary>
        public abstract string Category { get; }

        /// <summary>
        /// Color for the node header in the editor
        /// </summary>
        public virtual Color NodeColor => new Color(0.3f, 0.3f, 0.3f);

        public virtual void Initialize(string id, Vector2 pos)
        {
            nodeId = id;
            position = pos;
            nodeName = DisplayName;
            SetupPorts();
        }

        /// <summary>
        /// Setup input and output ports. Override in derived classes.
        /// </summary>
        protected virtual void SetupPorts()
        {
            // Default: one input, one output
            AddInputPort("Input", "input");
            AddOutputPort("Output", "output");
        }

        protected StoryPort AddInputPort(string name, string id = null)
        {
            var port = new StoryPort(id ?? Guid.NewGuid().ToString(), name, PortDirection.Input);
            inputPorts.Add(port);
            return port;
        }

        protected StoryPort AddOutputPort(string name, string id = null)
        {
            var port = new StoryPort(id ?? Guid.NewGuid().ToString(), name, PortDirection.Output);
            outputPorts.Add(port);
            return port;
        }

        public StoryPort GetPort(string portId)
        {
            foreach (var port in inputPorts)
                if (port.PortId == portId) return port;
            foreach (var port in outputPorts)
                if (port.PortId == portId) return port;
            return null;
        }

        public StoryPort GetInputPort(int index = 0)
        {
            return index < inputPorts.Count ? inputPorts[index] : null;
        }

        public StoryPort GetOutputPort(int index = 0)
        {
            return index < outputPorts.Count ? outputPorts[index] : null;
        }

        /// <summary>
        /// Execute this node. Returns the output port ID to follow.
        /// </summary>
        public abstract StoryNodeResult Execute(StoryContext context);

        /// <summary>
        /// Called when entering this node
        /// </summary>
        public virtual void OnEnter(StoryContext context) { }

        /// <summary>
        /// Called when exiting this node
        /// </summary>
        public virtual void OnExit(StoryContext context) { }

        /// <summary>
        /// Validate node configuration. Returns list of error messages.
        /// </summary>
        public virtual List<string> Validate()
        {
            return new List<string>();
        }

        /// <summary>
        /// Clone this node with a new ID
        /// </summary>
        public virtual StoryNode Clone()
        {
            var clone = Instantiate(this);
            clone.nodeId = Guid.NewGuid().ToString();
            return clone;
        }

        /// <summary>
        /// Get node data for serialization
        /// </summary>
        public virtual Dictionary<string, object> GetSerializationData()
        {
            return new Dictionary<string, object>
            {
                { "nodeId", nodeId },
                { "nodeType", GetType().FullName },
                { "nodeName", nodeName },
                { "nodeDescription", nodeDescription },
                { "position", new { x = position.x, y = position.y } }
            };
        }

        /// <summary>
        /// Load node data from serialization
        /// </summary>
        public virtual void LoadSerializationData(Dictionary<string, object> data)
        {
            if (data.TryGetValue("nodeId", out var id)) nodeId = id.ToString();
            if (data.TryGetValue("nodeName", out var name)) nodeName = name.ToString();
            if (data.TryGetValue("nodeDescription", out var desc)) nodeDescription = desc.ToString();
            if (data.TryGetValue("position", out var pos) && pos is Dictionary<string, object> posDict)
            {
                float x = Convert.ToSingle(posDict["x"]);
                float y = Convert.ToSingle(posDict["y"]);
                position = new Vector2(x, y);
            }
        }
    }

    /// <summary>
    /// Result of node execution
    /// </summary>
    public class StoryNodeResult
    {
        public StoryNodeResultType Type { get; set; }
        public string NextPortId { get; set; }
        public float WaitTime { get; set; }
        public Func<bool> WaitCondition { get; set; }
        public object Data { get; set; }

        public static StoryNodeResult Continue(string portId = "output")
        {
            return new StoryNodeResult { Type = StoryNodeResultType.Continue, NextPortId = portId };
        }

        public static StoryNodeResult Wait(float time, string portId = "output")
        {
            return new StoryNodeResult { Type = StoryNodeResultType.Wait, WaitTime = time, NextPortId = portId };
        }

        public static StoryNodeResult WaitForCondition(Func<bool> condition, string portId = "output")
        {
            return new StoryNodeResult { Type = StoryNodeResultType.WaitForCondition, WaitCondition = condition, NextPortId = portId };
        }

        public static StoryNodeResult WaitForInput(string portId = "output")
        {
            return new StoryNodeResult { Type = StoryNodeResultType.WaitForInput, NextPortId = portId };
        }

        public static StoryNodeResult End()
        {
            return new StoryNodeResult { Type = StoryNodeResultType.End };
        }

        public static StoryNodeResult Branch(string portId)
        {
            return new StoryNodeResult { Type = StoryNodeResultType.Continue, NextPortId = portId };
        }
    }

    public enum StoryNodeResultType
    {
        Continue,
        Wait,
        WaitForCondition,
        WaitForInput,
        End
    }
}