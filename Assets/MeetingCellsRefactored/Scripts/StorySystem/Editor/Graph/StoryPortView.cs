using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using StorySystem.Core;

namespace StorySystem.Editor
{
    public class StoryPortView : Port
    {
        public StoryPort PortData { get; private set; }
        public StoryNodeView NodeView { get; private set; }

        // Default constructor required by Unity's UI system
        public StoryPortView()
            : base(Orientation.Horizontal, Direction.Input, Capacity.Single, typeof(bool))
        {
        }

        private StoryPortView(Orientation orientation, Direction direction, Capacity capacity, System.Type type)
            : base(orientation, direction, capacity, type)
        {
        }

        public static StoryPortView Create(StoryPort port, StoryNodeView nodeView)
        {
            var direction = port.Direction == PortDirection.Input ? Direction.Input : Direction.Output;
            var capacity = port.Capacity == PortCapacity.Single ? Capacity.Single : Capacity.Multi;

            // Create using standard constructor
            // The port.node property will be set by Unity when this port is added to the node's container
            var portView = new StoryPortView(Orientation.Horizontal, direction, capacity, typeof(bool));

            // Set properties
            portView.PortData = port;
            portView.NodeView = nodeView;
            portView.portName = port.PortName;
            // Check if port color is valid (alpha > 0 means it's been set)
            Color effectiveColor = port.PortColor;
            portView.portColor = effectiveColor.a > 0.01f ? effectiveColor : GetDefaultColor(direction);
            portView.userData = port;

            // Add custom classes
            portView.AddToClassList("story-port");
            portView.AddToClassList(direction == Direction.Input ? "port-input" : "port-output");

            // Style the connector after UI is built
            portView.RegisterCallback<GeometryChangedEvent>(evt => portView.StyleConnector(direction));

            // Defer adding EdgeConnector until the port is attached to the hierarchy
            // This ensures port.node is properly set
            portView.RegisterCallback<AttachToPanelEvent>(evt =>
            {
                if (portView.node != null && !portView.HasEdgeConnector())
                {
                    var edgeConnector = new EdgeConnector<StoryEdgeView>(new StoryEdgeConnectorListener());
                    portView.AddManipulator(edgeConnector);
                }
            });

            return portView;
        }

        private bool HasEdgeConnector()
        {
            // Check if we already have an edge connector by looking for EdgeConnector type in manipulators
            // This is a workaround since manipulators is protected
            try
            {
                var manipulatorsField = typeof(Port).GetField("m_Manipulators", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (manipulatorsField != null)
                {
                    var manipulators = manipulatorsField.GetValue(this) as System.Collections.IList;
                    if (manipulators != null)
                    {
                        foreach (var m in manipulators)
                        {
                            if (m is EdgeConnector<StoryEdgeView>)
                                return true;
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        private void StyleConnector(Direction direction)
        {
            // Set up port container
            style.minWidth = 24;
            style.minHeight = 24;
            style.alignItems = Align.Center;

            // Set flex direction based on port type
            // Input: connector left, label right
            // Output: label left, connector right
            style.flexDirection = direction == Direction.Input ? FlexDirection.Row : FlexDirection.RowReverse;

            // Extend outside node for easier targeting
            if (direction == Direction.Input)
            {
                style.marginLeft = -6;
                style.paddingLeft = 2;
            }
            else
            {
                style.marginRight = -6;
                style.paddingRight = 2;
            }

            // Find and style the connector element (this is the clickable part)
            var connector = this.Q("connector");
            if (connector != null)
            {
                connector.style.width = 12;
                connector.style.height = 12;
                connector.style.minWidth = 12;
                connector.style.minHeight = 12;
                connector.style.borderTopLeftRadius = 6;
                connector.style.borderTopRightRadius = 6;
                connector.style.borderBottomLeftRadius = 6;
                connector.style.borderBottomRightRadius = 6;

                // Set color based on direction
                connector.style.backgroundColor = direction == Direction.Input
                    ? new Color(0.29f, 0.62f, 1f)    // Blue for input
                    : new Color(1f, 0.62f, 0.29f);   // Orange for output

                connector.style.borderLeftWidth = 2;
                connector.style.borderRightWidth = 2;
                connector.style.borderTopWidth = 2;
                connector.style.borderBottomWidth = 2;
                connector.style.borderLeftColor = direction == Direction.Input
                    ? new Color(0.16f, 0.43f, 0.8f)
                    : new Color(0.8f, 0.43f, 0.16f);
                connector.style.borderRightColor = connector.style.borderLeftColor;
                connector.style.borderTopColor = connector.style.borderLeftColor;
                connector.style.borderBottomColor = connector.style.borderLeftColor;

                // Ensure connector is visible and clickable
                connector.style.position = Position.Relative;
                connector.style.overflow = Overflow.Visible;
            }

            // Style the label
            var label = this.Q<Label>();
            if (label != null)
            {
                label.style.fontSize = 11;
                label.style.color = new Color(0.8f, 0.8f, 0.8f);
                label.style.marginLeft = direction == Direction.Input ? 4 : 0;
                label.style.marginRight = direction == Direction.Output ? 4 : 0;
                label.style.whiteSpace = WhiteSpace.NoWrap;
            }
        }

        private static Color GetDefaultColor(Direction direction)
        {
            return direction == Direction.Input
                ? new Color(0.29f, 0.62f, 1f)    // Blue for input
                : new Color(1f, 0.62f, 0.29f);   // Orange for output
        }
    }

    /// <summary>
    /// Listener for edge connection events
    /// </summary>
    public class StoryEdgeConnectorListener : IEdgeConnectorListener
    {
        public void OnDropOutsidePort(Edge edge, Vector2 position)
        {
            // Edge was dropped outside any port - remove it
            if (edge?.parent is GraphView graphView)
            {
                graphView.RemoveElement(edge);
            }
        }

        public void OnDrop(GraphView graphView, Edge edge)
        {
            // Edge was dropped on a valid port - GraphView will handle this via graphViewChanged
            // We need to manually add the edge to the graph view to trigger the creation event
            graphView.AddElement(edge);
        }
    }
}
