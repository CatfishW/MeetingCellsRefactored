using System.Linq;
using UnityEditor;
using UnityEngine;
using StorySystem.Core;
using StorySystem.Serialization;

namespace StorySystem.Editor
{
    [CustomEditor(typeof(StoryGraph))]
    public class StoryGraphInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            var graph = (StoryGraph)target;

            if (GUILayout.Button("Open Story Graph Editor"))
            {
                StoryGraphEditorWindow.Open(graph);
            }

            if (GUILayout.Button("Validate Graph"))
            {
                var errors = graph.Validate();
                if (errors.Count == 0)
                {
                    EditorUtility.DisplayDialog("Story Graph", "Graph validation succeeded.", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Story Graph", string.Join("\n", errors), "OK");
                }
            }

            if (GUILayout.Button("Save Graph JSON"))
            {
                string path = EditorUtility.SaveFilePanel("Save Story Graph JSON", Application.dataPath, graph.GraphName, "json");
                if (!string.IsNullOrEmpty(path))
                {
                    StorySerializer.SaveToFile(graph, path);
                }
            }
        }
    }
}
