using Unity.Entities;
using Unity.Collections;
using UnityEngine;
using StorySystem.Core;

namespace StorySystem.ECS
{
    /// <summary>
    /// Converts StoryGraph ScriptableObjects to ECS entities
    /// </summary>
    public class StoryGraphBaker : MonoBehaviour
    {
        [SerializeField] private StoryGraph storyGraph;
        [SerializeField] private bool bakeOnStart = true;

        public StoryGraph StoryGraph => storyGraph;

        class StoryGraphBakerInternal : Baker<StoryGraphBaker>
        {
            public override void Bake(StoryGraphBaker authoring)
            {
                if (authoring.storyGraph == null) return;

                var entity = GetEntity(TransformUsageFlags.None);

                // Create blob asset from graph data
                var blobAsset = BakeGraphToBlob(authoring.storyGraph);

                AddComponent(entity, new StoryGraphData
                {
                    GraphBlob = blobAsset
                });

                // Add tag to identify this as a story graph entity
                AddComponent<StoryGraphTag>(entity);
            }

            private BlobAssetReference<StoryGraphBlob> BakeGraphToBlob(StoryGraph graph)
            {
                using var builder = new BlobBuilder(Allocator.Temp);
                ref var root = ref builder.ConstructRoot<StoryGraphBlob>();

                // Bake nodes
                var nodes = graph.Nodes;
                var nodesArray = builder.Allocate(ref root.Nodes, nodes.Count);

                for (int i = 0; i < nodes.Count; i++)
                {
                    var node = nodes[i];
                    nodesArray[i] = new StoryNodeData
                    {
                        NodeId = node.NodeId.GetHashCode(),
                        NodeType = GetNodeTypeId(node),
                        Position = new Unity.Mathematics.float2(node.Position.x, node.Position.y),
                        // Port data would be serialized here
                    };
                }

                // Bake connections
                var connections = graph.Connections;
                var connectionsArray = builder.Allocate(ref root.Connections, connections.Count);

                for (int i = 0; i < connections.Count; i++)
                {
                    var conn = connections[i];
                    connectionsArray[i] = new StoryConnectionData
                    {
                        OutputNodeId = conn.OutputNodeId.GetHashCode(),
                        OutputPortId = conn.OutputPortId.GetHashCode(),
                        InputNodeId = conn.InputNodeId.GetHashCode(),
                        InputPortId = conn.InputPortId.GetHashCode()
                    };
                }

                // Bake variables
                var variables = graph.Variables;
                var variablesArray = builder.Allocate(ref root.Variables, variables.Count);

                for (int i = 0; i < variables.Count; i++)
                {
                    var var = variables[i];
                    variablesArray[i] = new StoryVariableDef
                    {
                        VariableId = var.Name.GetHashCode(),
                        Type = ConvertVariableType(var.Type),
                        // Default values would be set here
                    };
                }

                root.NodeCount = nodes.Count;
                root.ConnectionCount = connections.Count;
                root.VariableCount = variables.Count;

                return builder.CreateBlobAssetReference<StoryGraphBlob>(Allocator.Persistent);
            }

            private int GetNodeTypeId(StoryNode node)
            {
                return node switch
                {
                    Nodes.StartNode => 0,
                    Nodes.DialogueNode => 1,
                    Nodes.ChoiceNode => 2,
                    Nodes.ConditionNode => 3,
                    Nodes.WaitNode => 4,
                    Nodes.EventNode => 5,
                    Nodes.AudioNode => 6,
                    Nodes.CameraNode => 7,
                    Nodes.EndNode => 8,
                    _ => -1
                };
            }

            private VariableType ConvertVariableType(StorySystem.Core.VariableType type)
            {
                return type switch
                {
                    StorySystem.Core.VariableType.Float => VariableType.Float,
                    StorySystem.Core.VariableType.Int => VariableType.Int,
                    StorySystem.Core.VariableType.Bool => VariableType.Bool,
                    StorySystem.Core.VariableType.String => VariableType.String,
                    _ => VariableType.Float
                };
            }
        }
    }

    public struct StoryGraphTag : IComponentData { }
}
