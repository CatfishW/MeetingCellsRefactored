using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using StorySystem.Core;
using StorySystem.Serialization;

namespace StorySystem.Editor
{
    public class StoryGraphEditorWindow : EditorWindow
    {
        private static readonly List<StoryGraphEditorWindow> OpenWindows = new List<StoryGraphEditorWindow>();

        private StoryGraphView graphView;
        private StoryGraph currentGraph;
        private Label graphLabel;

        [MenuItem("Window/Story System/Story Graph Editor")]
        public static void OpenWindow()
        {
            GetWindow<StoryGraphEditorWindow>("Story Graph");
        }

        public static void Open(StoryGraph graph)
        {
            var window = GetWindow<StoryGraphEditorWindow>("Story Graph");
            window.LoadGraph(graph);
        }

        public static void RequestRefresh(StoryGraph graph)
        {
            foreach (var window in OpenWindows)
            {
                if (window != null && window.currentGraph == graph)
                {
                    window.PopulateView(graph);
                }
            }
        }

        private void OnEnable()
        {
            OpenWindows.Add(this);
            ConstructGraphView();
            ConstructToolbar();
        }

        private void OnDisable()
        {
            OpenWindows.Remove(this);
            if (graphView != null)
            {
                rootVisualElement.Remove(graphView);
            }
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is StoryGraph graph)
            {
                LoadGraph(graph);
            }
        }

        private void ConstructGraphView()
        {
            graphView = new StoryGraphView(this)
            {
                name = "Story Graph View"
            };
            graphView.StretchToParentSize();
            rootVisualElement.Add(graphView);
        }

        private void ConstructToolbar()
        {
            var toolbar = new Toolbar();

            graphLabel = new Label("No Graph Loaded");
            toolbar.Add(graphLabel);

            toolbar.Add(new ToolbarSpacer());

            var newGraphButton = new ToolbarButton(CreateNewGraph) { text = "New Graph" };
            var loadGraphButton = new ToolbarButton(LoadExistingGraph) { text = "Load Graph" };
            var saveJsonButton = new ToolbarButton(SaveGraphJson) { text = "Save JSON" };
            var loadJsonButton = new ToolbarButton(LoadGraphJson) { text = "Load JSON" };
            var validateButton = new ToolbarButton(ValidateGraph) { text = "Validate" };

            toolbar.Add(newGraphButton);
            toolbar.Add(loadGraphButton);
            toolbar.Add(saveJsonButton);
            toolbar.Add(loadJsonButton);
            toolbar.Add(validateButton);

            rootVisualElement.Add(toolbar);
        }

        public void LoadGraph(StoryGraph graph)
        {
            currentGraph = graph;
            graphLabel.text = graph != null ? $"Graph: {graph.GraphName}" : "No Graph Loaded";
            PopulateView(graph);
        }

        private void PopulateView(StoryGraph graph)
        {
            if (graphView == null)
            {
                return;
            }

            graphView.PopulateView(graph);
        }

        private void SaveGraphJson()
        {
            if (currentGraph == null)
            {
                EditorUtility.DisplayDialog("Story Graph", "No graph loaded.", "OK");
                return;
            }

            string path = EditorUtility.SaveFilePanel("Save Story Graph JSON", Application.dataPath, currentGraph.GraphName, "json");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            StorySerializer.SaveToFile(currentGraph, path);
        }

        private void LoadGraphJson()
        {
            string path = EditorUtility.OpenFilePanel("Load Story Graph JSON", Application.dataPath, "json");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            // Clear any previous console errors to get clean error reporting
            Debug.Log($"[StoryGraph] Loading JSON from: {path}");

            var graph = StorySerializer.LoadFromFile(path);
            if (graph == null)
            {
                // Get the last error from console for more details
                var lastError = GetLastErrorMessage();
                string errorMsg = "Failed to load graph JSON.";
                if (!string.IsNullOrEmpty(lastError))
                {
                    errorMsg += $"\n\nDetails:\n{lastError}";
                }
                errorMsg += "\n\nCheck the Console for more details.";

                EditorUtility.DisplayDialog("Story Graph - Error", errorMsg, "OK");
                return;
            }

            // Save the loaded graph as an asset
            string assetPath = EditorUtility.SaveFilePanelInProject(
                "Save Loaded Graph as Asset",
                graph.GraphName,
                "asset",
                "Choose location to save the loaded graph asset");

            if (!string.IsNullOrEmpty(assetPath))
            {
                AssetDatabase.CreateAsset(graph, assetPath);

                // Save all sub-assets (nodes)
                foreach (var node in graph.Nodes)
                {
                    if (node != null)
                    {
                        AssetDatabase.AddObjectToAsset(node, graph);
                    }
                }

                EditorUtility.SetDirty(graph);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"[StoryGraph] Loaded and saved graph to: {assetPath}");
            }

            LoadGraph(graph);
            EditorUtility.DisplayDialog("Story Graph", $"Successfully loaded graph with {graph.Nodes.Count} nodes and {graph.Connections.Count} connections.", "OK");
        }

        private string GetLastErrorMessage()
        {
            // Try to get recent error from log
            var logEntries = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.LogEntries");
            if (logEntries != null)
            {
                var getCounts = logEntries.GetMethod("GetCounts", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                if (getCounts != null)
                {
                    var counts = new object[] { 0, 0, 0 };
                    getCounts.Invoke(null, counts);
                    if ((int)counts[1] > 0) // error count > 0
                    {
                        return "See Console for error details.";
                    }
                }
            }
            return null;
        }

        private void LoadExistingGraph()
        {
            string path = EditorUtility.OpenFilePanelWithFilters(
                "Load Story Graph Asset",
                Application.dataPath,
                new[] { "Story Graph Assets", "asset" });

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            // Convert absolute path to relative path
            string relativePath = path;
            if (path.StartsWith(Application.dataPath))
            {
                relativePath = "Assets" + path.Substring(Application.dataPath.Length);
            }

            var graph = AssetDatabase.LoadAssetAtPath<StoryGraph>(relativePath);
            if (graph == null)
            {
                EditorUtility.DisplayDialog("Story Graph", "Failed to load graph asset.", "OK");
                return;
            }

            LoadGraph(graph);
            Debug.Log($"[StoryGraph] Loaded graph from: {relativePath}");
        }

        private void CreateNewGraph()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create New Story Graph",
                "NewStoryGraph",
                "asset",
                "Choose location for the new story graph asset");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var graph = ScriptableObject.CreateInstance<StoryGraph>();
            graph.Initialize(Guid.NewGuid().ToString(), System.IO.Path.GetFileNameWithoutExtension(path));

            AssetDatabase.CreateAsset(graph, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            LoadGraph(graph);

            // Add a start node by default
            var startNode = graph.CreateNode<StorySystem.Nodes.StartNode>(new Vector2(100, 100));
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();

            PopulateView(graph);

            Debug.Log($"[StoryGraph] Created new graph at: {path}");
        }

        private void ValidateGraph()
        {
            if (currentGraph == null)
            {
                EditorUtility.DisplayDialog("Story Graph", "No graph loaded.", "OK");
                return;
            }

            var errors = currentGraph.Validate();
            if (errors.Count == 0)
            {
                EditorUtility.DisplayDialog("Story Graph", "Graph validation succeeded.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Story Graph", string.Join("\n", errors), "OK");
            }
        }
    }
}
