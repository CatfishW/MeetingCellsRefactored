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

            var saveJsonButton = new ToolbarButton(SaveGraphJson) { text = "Save JSON" };
            var loadJsonButton = new ToolbarButton(LoadGraphJson) { text = "Load JSON" };
            var validateButton = new ToolbarButton(ValidateGraph) { text = "Validate" };

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

            var graph = StorySerializer.LoadFromFile(path);
            if (graph == null)
            {
                EditorUtility.DisplayDialog("Story Graph", "Failed to load graph JSON.", "OK");
                return;
            }

            LoadGraph(graph);
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
