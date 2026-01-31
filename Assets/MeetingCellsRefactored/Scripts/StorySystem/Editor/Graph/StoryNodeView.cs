using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using StorySystem.Core;

namespace StorySystem.Editor
{
    public class StoryNodeView : Node
    {
        public StoryNode Node { get; private set; }
        public Action<StoryNode> OnNodeSelected;

        private readonly Dictionary<string, StoryPortView> portLookup = new Dictionary<string, StoryPortView>();

        public StoryNodeView(StoryNode node)
        {
            Node = node;
            title = string.IsNullOrEmpty(node.NodeName) ? node.DisplayName : node.NodeName;
            viewDataKey = node.NodeId;

            style.left = node.Position.x;
            style.top = node.Position.y;
            style.minWidth = 140;
            style.width = StyleKeyword.Auto;
            style.maxWidth = StyleKeyword.None;

            // Configure input container (left side)
            inputContainer.style.flexDirection = FlexDirection.Column;
            inputContainer.style.alignItems = Align.FlexStart;
            inputContainer.style.paddingLeft = 0;
            inputContainer.style.marginLeft = 0;

            // Configure output container (right side)
            outputContainer.style.flexDirection = FlexDirection.Column;
            outputContainer.style.alignItems = Align.FlexEnd;
            outputContainer.style.paddingRight = 0;
            outputContainer.style.marginRight = 0;

            CreatePorts();
            RefreshExpandedState();
            RefreshPorts();
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            Node.Position = new Vector2(newPos.xMin, newPos.yMin);
            EditorUtility.SetDirty(Node);
        }

        public void UpdatePosition()
        {
            Node.Position = new Vector2(resolvedStyle.left, resolvedStyle.top);
            EditorUtility.SetDirty(Node);
        }

        public StoryPortView GetPort(string portId)
        {
            return portLookup.TryGetValue(portId, out var port) ? port : null;
        }

        private void CreatePorts()
        {
            portLookup.Clear();

            foreach (var port in Node.InputPorts)
            {
                var portView = StoryPortView.Create(port, this);
                inputContainer.Add(portView);
                portLookup[port.PortId] = portView;
            }

            foreach (var port in Node.OutputPorts)
            {
                var portView = StoryPortView.Create(port, this);
                outputContainer.Add(portView);
                portLookup[port.PortId] = portView;
            }
        }

        public void RebuildPorts()
        {
            inputContainer.Clear();
            outputContainer.Clear();
            CreatePorts();
            RefreshPorts();
            RefreshExpandedState();
        }

        public override void OnSelected()
        {
            base.OnSelected();
            OnNodeSelected?.Invoke(Node);
        }
    }
}
