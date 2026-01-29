using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using StorySystem.Core;

namespace StorySystem.Editor
{
    public class StoryGraphView : GraphView
    {
        private StoryGraphEditorWindow editorWindow;
        private StoryNodeSearchWindow searchWindow;
        private StoryGraph graph;
        private bool isPopulating;

        public StoryGraphView(StoryGraphEditorWindow window)
        {
            editorWindow = window;

            styleSheets.Add(StoryEditorStyles.LoadGraphStyle());

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            graphViewChanged = OnGraphViewChanged;
            viewTransformChanged += OnViewTransformChanged;

            AddSearchWindow();
        }

        public void PopulateView(StoryGraph storyGraph)
        {
            graph = storyGraph;
            isPopulating = true;

            DeleteElements(graphElements.ToList());

            if (graph == null)
            {
                isPopulating = false;
                return;
            }

            // Create node views
            foreach (var node in graph.Nodes)
            {
                CreateNodeView(node);
            }

            // Create edges
            foreach (var connection in graph.Connections)
            {
                var outputNodeView = GetNodeById(connection.OutputNodeId);
                var inputNodeView = GetNodeById(connection.InputNodeId);
                if (outputNodeView == null || inputNodeView == null)
                {
                    continue;
                }

                var outputPort = outputNodeView.GetPort(connection.OutputPortId);
                var inputPort = inputNodeView.GetPort(connection.InputPortId);
                if (outputPort == null || inputPort == null)
                {
                    continue;
                }

                var edge = new StoryEdgeView
                {
                    output = outputPort,
                    input = inputPort,
                    ConnectionData = connection
                };
                edge.output.Connect(edge);
                edge.input.Connect(edge);
                AddElement(edge);
            }

            if (graph.ViewScale > 0f)
            {
                viewTransform.position = graph.ViewOffset;
                viewTransform.scale = Vector3.one * graph.ViewScale;
            }

            isPopulating = false;
        }

        public void CreateNode(Type nodeType, Vector2 position)
        {
            if (graph == null)
            {
                return;
            }

            var node = graph.CreateNode(nodeType, position);
            CreateNodeView(node);
        }

        private void CreateNodeView(StoryNode node)
        {
            var nodeView = new StoryNodeView(node);
            nodeView.OnNodeSelected = OnNodeSelected;
            AddElement(nodeView);
        }

        private void AddSearchWindow()
        {
            searchWindow = ScriptableObject.CreateInstance<StoryNodeSearchWindow>();
            searchWindow.Initialize(this);
            nodeCreationRequest = context =>
                SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), searchWindow);
        }

        private void OnNodeSelected(StoryNode node)
        {
            Selection.activeObject = node;
        }

        private StoryNodeView GetNodeById(string nodeId)
        {
            return nodes.ToList().OfType<StoryNodeView>().FirstOrDefault(n => n.Node.NodeId == nodeId);
        }

        public StoryPortView GetPortView(string nodeId, string portId)
        {
            var nodeView = GetNodeById(nodeId);
            return nodeView != null ? nodeView.GetPort(portId) : null;
        }

        public Vector2 GetLocalMousePosition(Vector2 screenMousePosition)
        {
            Vector2 windowMousePosition = screenMousePosition - editorWindow.position.position;
            return contentViewContainer.WorldToLocal(windowMousePosition);
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (graph == null || isPopulating)
            {
                return change;
            }

            if (change.elementsToRemove != null)
            {
                foreach (var element in change.elementsToRemove)
                {
                    if (element is StoryNodeView nodeView)
                    {
                        graph.RemoveNode(nodeView.Node);
                    }
                    else if (element is Edge edge)
                    {
                        RemoveConnection(edge);
                    }
                }
            }

            if (change.edgesToCreate != null)
            {
                foreach (var edge in change.edgesToCreate)
                {
                    CreateConnection(edge);
                }
            }

            if (change.movedElements != null)
            {
                foreach (var element in change.movedElements)
                {
                    if (element is StoryNodeView nodeView)
                    {
                        nodeView.UpdatePosition();
                    }
                }
            }

            return change;
        }

        private void CreateConnection(Edge edge)
        {
            if (edge.output is StoryPortView outputPort && edge.input is StoryPortView inputPort)
            {
                var outputNode = outputPort.NodeView.Node;
                var inputNode = inputPort.NodeView.Node;
                var connection = graph.CreateConnection(outputNode.NodeId, outputPort.PortData.PortId,
                    inputNode.NodeId, inputPort.PortData.PortId);

                if (connection != null)
                {
                    edge.userData = connection;
                    if (edge is StoryEdgeView storyEdge)
                    {
                        storyEdge.ConnectionData = connection;
                    }
                }
            }
        }

        private void RemoveConnection(Edge edge)
        {
            if (edge.userData is StoryConnection storedConnection)
            {
                graph.RemoveConnection(storedConnection);
                return;
            }

            if (edge is StoryEdgeView storyEdge && storyEdge.ConnectionData != null)
            {
                graph.RemoveConnection(storyEdge.ConnectionData);
                return;
            }

            if (edge.output is StoryPortView outputPort && edge.input is StoryPortView inputPort)
            {
                var connection = graph.Connections.FirstOrDefault(c =>
                    c.OutputNodeId == outputPort.NodeView.Node.NodeId &&
                    c.OutputPortId == outputPort.PortData.PortId &&
                    c.InputNodeId == inputPort.NodeView.Node.NodeId &&
                    c.InputPortId == inputPort.PortData.PortId);

                if (connection != null)
                {
                    graph.RemoveConnection(connection);
                }
            }
        }

        private void OnViewTransformChanged(GraphView graphView)
        {
            if (graph == null)
            {
                return;
            }

            graph.ViewOffset = viewTransform.position;
            graph.ViewScale = viewTransform.scale.x;

            EditorUtility.SetDirty(graph);
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = ports.ToList();
            compatiblePorts.RemoveAll(port =>
                port == startPort ||
                port.node == startPort.node ||
                port.direction == startPort.direction);
            return compatiblePorts;
        }
    }
}
