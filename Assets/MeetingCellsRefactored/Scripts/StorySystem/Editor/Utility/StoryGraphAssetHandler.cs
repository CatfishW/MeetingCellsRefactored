using System;
using UnityEditor;
using UnityEngine;
using StorySystem.Core;

namespace StorySystem.Editor
{
    public static class StoryGraphAssetHandler
    {
        [MenuItem("Assets/Create/Story System/Story Graph", priority = 10)]
        public static void CreateStoryGraphAsset()
        {
            var graph = ScriptableObject.CreateInstance<StoryGraph>();
            graph.Initialize(Guid.NewGuid().ToString(), "New Story Graph");

            string path = GetSelectedPathOrFallback();
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{path}/NewStoryGraph.asset");

            AssetDatabase.CreateAsset(graph, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = graph;
            EditorGUIUtility.PingObject(graph);
        }

        private static string GetSelectedPathOrFallback()
        {
            string path = "Assets";
            foreach (var obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
            {
                string assetPath = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                if (System.IO.Directory.Exists(assetPath))
                {
                    path = assetPath;
                }
                else
                {
                    path = System.IO.Path.GetDirectoryName(assetPath)?.Replace('\\', '/') ?? "Assets";
                }
                break;
            }
            return path;
        }
    }
}
