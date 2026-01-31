using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using StorySystem.Core;

namespace StorySystem.Serialization
{
    /// <summary>
    /// Factory for creating story nodes from serialized data
    /// </summary>
    public static class NodeFactory
    {
        private static Dictionary<string, Type> nodeTypeCache;
        
        static NodeFactory()
        {
            BuildTypeCache();
        }

        private static void BuildTypeCache()
        {
            nodeTypeCache = new Dictionary<string, Type>();

            // Find all types that inherit from StoryNode
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            foreach (var assembly in assemblies)
            {
                try
                {
                    var nodeTypes = assembly.GetTypes()
                        .Where(t => typeof(StoryNode).IsAssignableFrom(t) && !t.IsAbstract);

                    foreach (var type in nodeTypes)
                    {
                        nodeTypeCache[type.FullName] = type;
                        nodeTypeCache[type.Name] = type; // Also cache by simple name
                    }
                }
                catch
                {
                    // Skip assemblies that can't be loaded
                }
            }
        }

        public static StoryNode CreateNode(Dictionary<string, object> data)
        {
            if (!data.TryGetValue("nodeType", out var typeObj))
            {
                Debug.LogError("Node data missing 'nodeType' field");
                return null;
            }

            string typeName = typeObj.ToString();
            
            if (!nodeTypeCache.TryGetValue(typeName, out var nodeType))
            {
                // Try to find by simple name
                var simpleName = typeName.Split('.').Last();
                if (!nodeTypeCache.TryGetValue(simpleName, out nodeType))
                {
                    Debug.LogError($"Unknown node type: {typeName}");
                    return null;
                }
            }

            var node = ScriptableObject.CreateInstance(nodeType) as StoryNode;
            if (node == null)
            {
                Debug.LogError($"Failed to create node of type: {nodeType}");
                return null;
            }

            // Load position first
            Vector2 position = Vector2.zero;
            if (data.TryGetValue("position", out var posObj) && posObj is Dictionary<string, object> posDict)
            {
                position = new Vector2(
                    Convert.ToSingle(posDict["x"]),
                    Convert.ToSingle(posDict["y"])
                );
            }

            // Initialize with ID and position
            string nodeId = data.TryGetValue("nodeId", out var idObj) ? idObj.ToString() : Guid.NewGuid().ToString();
            node.Initialize(nodeId, position);

            // Ensure ports are set up (some nodes may need explicit port setup)
            // The Initialize method calls SetupPorts(), but we verify here
            if (node.InputPorts.Count == 0 && node.OutputPorts.Count == 0)
            {
                Debug.LogWarning($"Node {nodeId} of type {typeName} has no ports after initialization. Calling SetupPorts() again.");
                var setupMethod = nodeType.GetMethod("SetupPorts", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                setupMethod?.Invoke(node, null);
            }

            // Load custom data
            if (data.TryGetValue("data", out var customData) && customData is Dictionary<string, object> customDict)
            {
                node.LoadSerializationData(customDict);
            }
            else
            {
                node.LoadSerializationData(data);
            }

            return node;
        }

        public static StoryNode CreateNode<T>(Vector2 position) where T : StoryNode
        {
            var node = ScriptableObject.CreateInstance<T>();
            node.Initialize(Guid.NewGuid().ToString(), position);
            return node;
        }

        public static StoryNode CreateNode(Type nodeType, Vector2 position)
        {
            if (!typeof(StoryNode).IsAssignableFrom(nodeType))
            {
                throw new ArgumentException($"Type {nodeType} is not a StoryNode");
            }

            var node = ScriptableObject.CreateInstance(nodeType) as StoryNode;
            node.Initialize(Guid.NewGuid().ToString(), position);
            return node;
        }

        public static IEnumerable<Type> GetAllNodeTypes()
        {
            return nodeTypeCache.Values.Distinct();
        }

        public static IEnumerable<Type> GetNodeTypesByCategory(string category)
        {
            foreach (var type in GetAllNodeTypes())
            {
                var instance = ScriptableObject.CreateInstance(type) as StoryNode;
                if (instance != null && instance.Category == category)
                {
                    ScriptableObject.DestroyImmediate(instance);
                    yield return type;
                }
                else if (instance != null)
                {
                    ScriptableObject.DestroyImmediate(instance);
                }
            }
        }

        public static Dictionary<string, List<Type>> GetNodeTypesByCategories()
        {
            var result = new Dictionary<string, List<Type>>();

            foreach (var type in GetAllNodeTypes())
            {
                var instance = ScriptableObject.CreateInstance(type) as StoryNode;
                if (instance != null)
                {
                    string category = instance.Category ?? "Other";
                    
                    if (!result.ContainsKey(category))
                    {
                        result[category] = new List<Type>();
                    }
                    result[category].Add(type);
                    
                    ScriptableObject.DestroyImmediate(instance);
                }
            }

            return result;
        }
    }
}