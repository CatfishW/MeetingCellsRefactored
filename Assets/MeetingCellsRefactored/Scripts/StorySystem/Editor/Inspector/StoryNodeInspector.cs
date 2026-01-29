using System.Linq;
using UnityEditor;
using UnityEngine;
using StorySystem.Core;
using StorySystem.Nodes;

namespace StorySystem.Editor
{
    [CustomEditor(typeof(StoryNode), true)]
    public class StoryNodeInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var node = (StoryNode)target;

            EditorGUILayout.LabelField("Node Id", node.NodeId);
            EditorGUILayout.Space();

            DrawDefaultInspector();

            EditorGUILayout.Space();

            if (node is ChoiceNode choiceNode)
            {
                if (GUILayout.Button("Refresh Choice Ports"))
                {
                    choiceNode.UpdateChoicePorts();
                    EditorUtility.SetDirty(choiceNode);
                    RefreshGraphForNode(choiceNode);
                }
            }

            if (GUILayout.Button("Open Graph Editor"))
            {
                var graph = FindOwningGraph(node);
                if (graph != null)
                {
                    StoryGraphEditorWindow.Open(graph);
                }
            }
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
            string path = AssetDatabase.GetAssetPath(node);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            return assets.OfType<StoryGraph>().FirstOrDefault();
        }
    }
}
