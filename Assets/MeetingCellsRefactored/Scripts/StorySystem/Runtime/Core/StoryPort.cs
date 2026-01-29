using System;
using UnityEngine;

namespace StorySystem.Core
{
    /// <summary>
    /// Represents an input or output port on a node
    /// </summary>
    [Serializable]
    public class StoryPort
    {
        [SerializeField] private string portId;
        [SerializeField] private string portName;
        [SerializeField] private PortDirection direction;
        [SerializeField] private PortCapacity capacity;
        [SerializeField] private Color portColor;

        public string PortId => portId;
        public string PortName { get => portName; set => portName = value; }
        public PortDirection Direction => direction;
        public PortCapacity Capacity { get => capacity; set => capacity = value; }
        public Color PortColor { get => portColor; set => portColor = value; }

        public StoryPort(string id, string name, PortDirection dir, PortCapacity cap = PortCapacity.Single)
        {
            portId = id;
            portName = name;
            direction = dir;
            capacity = cap;
            portColor = dir == PortDirection.Input ? 
                new Color(0.2f, 0.8f, 0.2f) : new Color(0.8f, 0.2f, 0.2f);
        }

        public StoryPort Clone()
        {
            return new StoryPort(Guid.NewGuid().ToString(), portName, direction, capacity)
            {
                portColor = portColor
            };
        }
    }

    public enum PortDirection
    {
        Input,
        Output
    }

    public enum PortCapacity
    {
        Single,
        Multi
    }
}