using UnityEngine;
using UnityEditor;
using StorySystem.Core;
using StorySystem.Serialization;
using StorySystem.Nodes;

namespace StorySystem.Editor.Tests
{
    public static class StorySystemTestEditor
    {
        [MenuItem("Story System/Tests/Test JSON Serialization")]
        public static void TestJsonSerialization()
        {
            Debug.Log("=== Testing Story Graph JSON Serialization ===");

            // Create a test graph
            var graph = ScriptableObject.CreateInstance<StoryGraph>();
            graph.Initialize(System.Guid.NewGuid().ToString(), "TestGraph");

            // Create nodes
            var startNode = graph.CreateNode<StartNode>(new Vector2(100, 100));
            var cutsceneNode = graph.CreateNode<CutsceneNode>(new Vector2(350, 100));
            var audioNode = graph.CreateNode<AudioNode>(new Vector2(600, 100));

            Debug.Log($"Created {graph.Nodes.Count} nodes");
            Debug.Log($"StartNode outputs: {startNode.OutputPorts.Count}, port ID: {startNode.OutputPorts[0]?.PortId}");
            Debug.Log($"CutsceneNode inputs: {cutsceneNode.InputPorts.Count}, port ID: {cutsceneNode.InputPorts[0]?.PortId}");
            Debug.Log($"CutsceneNode outputs: {cutsceneNode.OutputPorts.Count}");
            foreach (var port in cutsceneNode.OutputPorts)
            {
                Debug.Log($"  Cutscene output: {port.PortName} (ID: {port.PortId})");
            }

            // Create connections using the actual port IDs
            var conn1 = graph.CreateConnection(
                startNode.NodeId, "output",
                cutsceneNode.NodeId, "input"
            );
            if (conn1 != null)
                Debug.Log($"Created connection 1: Start.output -> Cutscene.input");
            else
                Debug.LogError("Failed to create connection 1");

            var conn2 = graph.CreateConnection(
                cutsceneNode.NodeId, "complete",
                audioNode.NodeId, "input"
            );
            if (conn2 != null)
                Debug.Log($"Created connection 2: Cutscene.complete -> Audio.input");
            else
                Debug.LogError("Failed to create connection 2");

            Debug.Log($"Total connections: {graph.Connections.Count}");

            // Serialize to JSON
            string json = StorySerializer.ToJson(graph, true);
            Debug.Log("=== Serialized JSON ===");
            Debug.Log(json);

            // Save to file
            string path = Application.dataPath + "/TestOutput.json";
            System.IO.File.WriteAllText(path, json);
            Debug.Log($"Saved JSON to: {path}");

            // Now test deserialization
            Debug.Log("=== Testing Deserialization ===");
            var loadedGraph = StorySerializer.FromJson(json);

            if (loadedGraph != null)
            {
                Debug.Log($"Loaded graph: {loadedGraph.GraphName}");
                Debug.Log($"Nodes: {loadedGraph.Nodes.Count}");
                Debug.Log($"Connections: {loadedGraph.Connections.Count}");

                foreach (var conn in loadedGraph.Connections)
                {
                    var outputNode = loadedGraph.GetNode(conn.OutputNodeId);
                    var inputNode = loadedGraph.GetNode(conn.InputNodeId);
                    Debug.Log($"Connection: {outputNode?.DisplayName}.{conn.OutputPortId} -> {inputNode?.DisplayName}.{conn.InputPortId}");
                }
            }
            else
            {
                Debug.LogError("Failed to load graph from JSON!");
            }

            // Test loading from file
            Debug.Log("=== Testing Load From File ===");
            var fileGraph = StorySerializer.LoadFromFile(path);
            if (fileGraph != null)
            {
                Debug.Log($"Successfully loaded from file: {fileGraph.GraphName} with {fileGraph.Nodes.Count} nodes");
            }
            else
            {
                Debug.LogError("Failed to load from file!");
            }

            Debug.Log("=== Test Complete ===");
        }

        [MenuItem("Story System/Tests/Test Load Existing JSON")]
        public static void TestLoadExistingJson()
        {
            string[] testFiles = new[]
            {
                Application.dataPath + "/TestGraphWithConnections.json",
                Application.dataPath + "/NewStoryGraph.json",
                Application.dataPath + "/MeetingCellsStory.json"
            };

            foreach (var path in testFiles)
            {
                if (System.IO.File.Exists(path))
                {
                    Debug.Log($"=== Testing: {System.IO.Path.GetFileName(path)} ===");
                    var graph = StorySerializer.LoadFromFile(path);
                    if (graph != null)
                    {
                        Debug.Log($"SUCCESS: Loaded {graph.GraphName} with {graph.Nodes.Count} nodes and {graph.Connections.Count} connections");
                        foreach (var conn in graph.Connections)
                        {
                            var outNode = graph.GetNode(conn.OutputNodeId);
                            var inNode = graph.GetNode(conn.InputNodeId);
                            Debug.Log($"  Connection: {outNode?.DisplayName}.{conn.OutputPortId} -> {inNode?.DisplayName}.{conn.InputPortId}");
                        }
                    }
                    else
                    {
                        Debug.LogError($"FAILED to load: {path}");
                    }
                }
            }
        }

        [MenuItem("Story System/Tests/Test Empty JSON")]
        public static void TestEmptyJson()
        {
            string emptyJson = @"{
                ""graphId"": ""test-empty"",
                ""graphName"": ""EmptyTest"",
                ""description"": """",
                ""nodes"": [],
                ""connections"": [],
                ""variables"": [],
                ""viewState"": {
                    ""offset"": { ""x"": 0, ""y"": 0 },
                    ""scale"": 1
                }
            }";

            Debug.Log("=== Testing Empty JSON ===");
            var graph = StorySerializer.FromJson(emptyJson);
            if (graph != null)
            {
                Debug.Log($"SUCCESS: Loaded empty graph: {graph.GraphName}");
            }
            else
            {
                Debug.LogError("FAILED to load empty JSON");
            }
        }
    }
}
