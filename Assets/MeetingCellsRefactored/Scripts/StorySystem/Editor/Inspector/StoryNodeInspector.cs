using System.Linq;
using UnityEditor;
using UnityEngine;
using StorySystem.Core;
using StorySystem.Nodes;
using System.Collections.Generic;

namespace StorySystem.Editor
{
    [CustomEditor(typeof(StoryNode), true)]
    public class StoryNodeInspector : UnityEditor.Editor
    {
        private bool showConnectedNodes = true;
        private bool showInputConnections = true;
        private bool showOutputConnections = true;
        private bool showDebugInfo = false;

        public override void OnInspectorGUI()
        {
            var node = (StoryNode)target;
            var graph = FindOwningGraph(node);

            // Header
            EditorGUILayout.Space(10);
            var headerStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 14
            };
            EditorGUILayout.LabelField($"{node.DisplayName} Node", headerStyle);
            EditorGUILayout.Space(5);

            // Basic Info Section
            EditorGUILayout.LabelField("Basic Info", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Node Id", node.NodeId);
            EditorGUILayout.LabelField("Type", node.GetType().FullName);
            EditorGUILayout.LabelField("Category", node.Category);

            EditorGUI.BeginChangeCheck();
            string newName = EditorGUILayout.TextField("Name", node.NodeName);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(node, "Change Node Name");
                node.NodeName = newName;
                EditorUtility.SetDirty(node);
                RefreshGraphForNode(node);
            }

            EditorGUI.BeginChangeCheck();
            string newDesc = EditorGUILayout.TextField("Description", node.NodeDescription);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(node, "Change Node Description");
                node.NodeDescription = newDesc;
                EditorUtility.SetDirty(node);
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(10);

            // Default Inspector for custom node properties
            DrawDefaultInspector();
            EditorGUILayout.Space(10);

            // Connected Nodes Section
            if (graph != null)
            {
                DrawConnectedNodesSection(node, graph);
            }
            else
            {
                EditorGUILayout.HelpBox("Node is not part of a StoryGraph asset. Cannot show connections.\n\nTo fix: Save this graph as an asset file.", MessageType.Info);
            }

            EditorGUILayout.Space(10);

            // Action Buttons
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            if (node is ChoiceNode choiceNode)
            {
                if (GUILayout.Button("Refresh Choice Ports"))
                {
                    Undo.RecordObject(choiceNode, "Refresh Choice Ports");
                    choiceNode.UpdateChoicePorts();
                    EditorUtility.SetDirty(choiceNode);
                    RefreshGraphForNode(choiceNode);
                }
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Select in Graph"))
            {
                if (graph != null)
                {
                    StoryGraphEditorWindow.Open(graph);
                    EditorGUIUtility.PingObject(node);
                }
            }

            if (GUILayout.Button("Open Graph Editor"))
            {
                if (graph != null)
                {
                    StoryGraphEditorWindow.Open(graph);
                }
            }

            EditorGUILayout.EndHorizontal();

            // Debug Info
            EditorGUILayout.Space(10);
            showDebugInfo = EditorGUILayout.Foldout(showDebugInfo, "Debug Info", true);
            if (showDebugInfo)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Input Ports", node.InputPorts.Count.ToString());
                EditorGUILayout.LabelField("Output Ports", node.OutputPorts.Count.ToString());

                foreach (var port in node.InputPorts)
                {
                    EditorGUILayout.LabelField($"  In: {port.PortName} (ID: {port.PortId})");
                }
                foreach (var port in node.OutputPorts)
                {
                    EditorGUILayout.LabelField($"  Out: {port.PortName} (ID: {port.PortId})");
                }

                if (graph != null)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Graph Info", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Graph Name", graph.GraphName);
                    EditorGUILayout.LabelField("Total Nodes", graph.Nodes.Count.ToString());
                    EditorGUILayout.LabelField("Total Connections", GetConnectionCount(graph).ToString());
                }
                EditorGUI.indentLevel--;
            }
        }

        private int GetConnectionCount(StoryGraph graph)
        {
            // Use reflection to access private connections list if needed
            return graph.Connections?.Count() ?? 0;
        }

        private void DrawConnectedNodesSection(StoryNode node, StoryGraph graph)
        {
            showConnectedNodes = EditorGUILayout.BeginFoldoutHeaderGroup(showConnectedNodes, "Connected Nodes", EditorStyles.foldoutHeader);

            if (showConnectedNodes)
            {
                // Get all connections involving this node
                var inputConnections = new List<StoryConnection>();
                var outputConnections = new List<StoryConnection>();

                foreach (var conn in graph.Connections)
                {
                    if (conn.InputNodeId == node.NodeId)
                        inputConnections.Add(conn);
                    if (conn.OutputNodeId == node.NodeId)
                        outputConnections.Add(conn);
                }

                // Input Connections (nodes that connect TO this node)
                showInputConnections = EditorGUILayout.Foldout(showInputConnections, $"Input Connections ({inputConnections.Count})", true);
                if (showInputConnections)
                {
                    if (inputConnections.Count == 0)
                    {
                        EditorGUILayout.HelpBox("No nodes connected to this node's inputs.", MessageType.Info);
                    }
                    else
                    {
                        EditorGUI.indentLevel++;
                        foreach (var conn in inputConnections)
                        {
                            var sourceNode = graph.GetNode(conn.OutputNodeId);
                            if (sourceNode != null)
                            {
                                DrawConnectionItem(sourceNode, conn.OutputPortId, node, conn.InputPortId, Direction.Input);
                            }
                            else
                            {
                                EditorGUILayout.LabelField($"Missing Node: {conn.OutputNodeId}", EditorStyles.miniLabel);
                            }
                        }
                        EditorGUI.indentLevel--;
                    }
                }

                EditorGUILayout.Space(5);

                // Output Connections (nodes this node connects TO)
                showOutputConnections = EditorGUILayout.Foldout(showOutputConnections, $"Output Connections ({outputConnections.Count})", true);
                if (showOutputConnections)
                {
                    if (outputConnections.Count == 0)
                    {
                        EditorGUILayout.HelpBox("No nodes connected from this node's outputs.", MessageType.Info);
                    }
                    else
                    {
                        EditorGUI.indentLevel++;
                        foreach (var conn in outputConnections)
                        {
                            var targetNode = graph.GetNode(conn.InputNodeId);
                            if (targetNode != null)
                            {
                                DrawConnectionItem(node, conn.OutputPortId, targetNode, conn.InputPortId, Direction.Output);
                            }
                            else
                            {
                                EditorGUILayout.LabelField($"Missing Node: {conn.InputNodeId}", EditorStyles.miniLabel);
                            }
                        }
                        EditorGUI.indentLevel--;
                    }
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private enum Direction { Input, Output }

        private void DrawConnectionItem(StoryNode sourceNode, string sourcePortId, StoryNode targetNode, string targetPortId, Direction direction)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            // Color indicator
            var colorRect = GUILayoutUtility.GetRect(4, 20, GUILayout.Width(4));
            EditorGUI.DrawRect(colorRect, direction == Direction.Input ? new Color(0.2f, 0.6f, 1f) : new Color(1f, 0.6f, 0.2f));

            EditorGUILayout.BeginVertical();

            // Source/Target node info
            var nodeStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11
            };

            if (direction == Direction.Input)
            {
                // Someone connects TO us
                EditorGUILayout.LabelField($"From: {sourceNode.DisplayName}", nodeStyle);
                EditorGUILayout.LabelField($"{sourceNode.NodeName}.{sourcePortId} → this.{targetPortId}", EditorStyles.miniLabel);
            }
            else
            {
                // We connect TO someone
                EditorGUILayout.LabelField($"To: {targetNode.DisplayName}", nodeStyle);
                EditorGUILayout.LabelField($"this.{sourcePortId} → {targetNode.NodeName}.{targetPortId}", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();

            // Select button
            if (GUILayout.Button("Select", GUILayout.Width(50)))
            {
                Selection.activeObject = direction == Direction.Input ? sourceNode : targetNode;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void RefreshGraphForNode(StoryNode node)
        {
            var graph = FindOwningGraph(node);
            if (graph != null)
            {
                StoryGraphEditorWindow.RequestRefresh(graph);
            }
        }

        private StoryGraph FindOwningGraph(StoryNode node)
        {
            if (node == null) return null;

            string path = AssetDatabase.GetAssetPath(node);

            // Method 1: Check if node is a sub-asset of a graph
            if (!string.IsNullOrEmpty(path))
            {
                var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var asset in assets)
                {
                    if (asset is StoryGraph graph)
                    {
                        // Verify this node is actually in the graph
                        if (graph.Nodes.Any(n => n.NodeId == node.NodeId))
                        {
                            return graph;
                        }
                    }
                }
            }

            // Method 2: Search all StoryGraph assets in the project
            // This is slower but works for nodes that aren't properly linked as sub-assets
            var graphGuids = AssetDatabase.FindAssets("t:StoryGraph");
            foreach (var guid in graphGuids)
            {
                var graphPath = AssetDatabase.GUIDToAssetPath(guid);
                var graph = AssetDatabase.LoadAssetAtPath<StoryGraph>(graphPath);
                if (graph != null)
                {
                    foreach (var n in graph.Nodes)
                    {
                        if (n.NodeId == node.NodeId)
                        {
                            return graph;
                        }
                    }
                }
            }

            // Method 3: Check currently open StoryGraphEditorWindow
            // This handles the case where JSON was loaded but not saved yet
            var windows = Resources.FindObjectsOfTypeAll<StoryGraphEditorWindow>();
            foreach (var window in windows)
            {
                // This won't work directly since we can't access private fields easily
                // But the graph should be findable through methods 1 or 2 after it's assigned
            }

            return null;
        }
    }
}
