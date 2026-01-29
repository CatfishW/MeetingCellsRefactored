using UnityEditor.Experimental.GraphView;
using UnityEngine;
using StorySystem.Core;

namespace StorySystem.Editor
{
    public class StoryPortView : Port
    {
        public StoryPort PortData { get; private set; }
        public StoryNodeView NodeView { get; private set; }

        private StoryPortView(Orientation orientation, Direction direction, Capacity capacity, System.Type type)
            : base(orientation, direction, capacity, type)
        {
        }

        public static StoryPortView Create(StoryPort port, StoryNodeView nodeView)
        {
            var direction = port.Direction == PortDirection.Input ? Direction.Input : Direction.Output;
            var capacity = port.Capacity == PortCapacity.Single ? Capacity.Single : Capacity.Multi;

            var portView = new StoryPortView(Orientation.Horizontal, direction, capacity, typeof(bool))
            {
                PortData = port,
                NodeView = nodeView,
                portName = port.PortName
            };

            portView.portColor = port.PortColor;
            portView.userData = port;
            return portView;
        }
    }
}
