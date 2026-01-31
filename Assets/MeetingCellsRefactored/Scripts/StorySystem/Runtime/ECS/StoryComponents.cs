using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace StorySystem.ECS
{
    /// <summary>
    /// ECS Components for high-performance story system
    /// </summary>

    // Core story execution component
    public struct StoryExecution : IComponentData
    {
        public Entity CurrentNode;
        public Entity GraphEntity;
        public StoryState State;
        public float WaitTimer;
        public int NextPortIndex;
        public bool IsPaused;
        public bool InputReceived;
    }

    public enum StoryState
    {
        Idle,
        Executing,
        Waiting,
        WaitingForInput,
        Complete
    }

    // Graph data - stored as blob asset for cache efficiency
    public struct StoryGraphData : IComponentData
    {
        public BlobAssetReference<StoryGraphBlob> GraphBlob;
    }

    // Blob structure for immutable graph data
    public struct StoryGraphBlob
    {
        public int NodeCount;
        public int ConnectionCount;
        public int VariableCount;

        // Stored as contiguous arrays in blob
        public BlobArray<StoryNodeData> Nodes;
        public BlobArray<StoryConnectionData> Connections;
        public BlobArray<StoryVariableDef> Variables;
    }

    public struct StoryNodeData
    {
        public int NodeId;
        public int NodeType;
        public int InputPortStart;
        public int InputPortCount;
        public int OutputPortStart;
        public int OutputPortCount;
        public float2 Position;
        public int DataOffset; // Offset into node-specific data blob
    }

    public struct StoryConnectionData
    {
        public int OutputNodeId;
        public int OutputPortId;
        public int InputNodeId;
        public int InputPortId;
    }

    public struct StoryVariableDef
    {
        public int VariableId;
        public VariableType Type;
        public float DefaultFloat;
        public int DefaultInt;
        public bool DefaultBool;
    }

    // Runtime variable storage - dynamic buffer per story
    public struct StoryVariable : IBufferElementData
    {
        public int VariableId;
        public VariableType Type;
        public float FloatValue;
        public int IntValue;
        public bool BoolValue;
    }

    public enum VariableType : byte
    {
        Float,
        Int,
        Bool,
        String // String stored as hash/lookup
    }

    // Node type tags for fast filtering
    public struct DialogueNodeTag : IComponentData { }
    public struct ChoiceNodeTag : IComponentData { }
    public struct ConditionNodeTag : IComponentData { }
    public struct WaitNodeTag : IComponentData { }
    public struct EventNodeTag : IComponentData { }
    public struct AudioNodeTag : IComponentData { }
    public struct CameraNodeTag : IComponentData { }
    public struct EndNodeTag : IComponentData { }

    // Dialogue node data
    public struct DialogueData : IComponentData
    {
        public int SpeakerId; // String hash
        public int EmotionId; // String hash
        public int TextId; // String hash for localization
        public float TextSpeed;
        public bool WaitForInput;
        public float AutoAdvanceDelay;
        public int VoiceClipId; // Audio reference
    }

    // Choice node data
    public struct ChoiceData : IComponentData
    {
        public int ChoiceCount;
        public float Timeout;
        public int DefaultChoiceIndex;
    }

    // Choice option buffer
    public struct ChoiceOption : IBufferElementData
    {
        public int TextId;
        public int ConditionVariableId; // -1 if no condition
        public ConditionOperator ConditionOp;
        public float ConditionValue;
        public int TargetNodeId;
    }

    // Condition node data
    public struct ConditionData : IComponentData
    {
        public int ConditionCount;
        public ConditionLogic Logic;
        public int TrueTargetNodeId;
        public int FalseTargetNodeId;
    }

    // Condition buffer
    public struct ConditionItem : IBufferElementData
    {
        public int VariableId;
        public ConditionOperator Operator;
        public float CompareValue;
    }

    public enum ConditionOperator : byte
    {
        Equals,
        NotEquals,
        GreaterThan,
        LessThan,
        GreaterOrEqual,
        LessOrEqual,
        IsTrue,
        IsFalse
    }

    public enum ConditionLogic : byte
    {
        And,
        Or
    }

    // Wait node data
    public struct WaitData : IComponentData
    {
        public WaitType Type;
        public float Duration;
        public int ConditionVariableId; // For condition wait
    }

    public enum WaitType : byte
    {
        Time,
        Input,
        Condition,
        Frame
    }

    // Event node data
    public struct EventData : IComponentData
    {
        public int EventId; // String hash of event name
        public int ParameterCount;
        public float Timeout;
    }

    public struct EventParameter : IBufferElementData
    {
        public int NameId;
        public ParameterType Type;
        public float FloatValue;
        public int IntValue;
        public bool BoolValue;
    }

    public enum ParameterType : byte
    {
        Float,
        Int,
        Bool,
        String
    }

    // Audio node data
    public struct AudioData : IComponentData
    {
        public int AudioId;
        public AudioType Type;
        public float Volume;
        public float FadeIn;
        public float FadeOut;
        public bool WaitForComplete;
    }

    public enum AudioType : byte
    {
        SFX,
        Music,
        Ambient,
        Voice
    }

    // Camera node data
    public struct CameraData : IComponentData
    {
        public CameraAction Action;
        public float3 TargetPosition;
        public float Duration;
        public float2 ShakeIntensity;
        public float ZoomLevel;
    }

    public enum CameraAction : byte
    {
        Move,
        LookAt,
        Follow,
        Zoom,
        Shake,
        Fade
    }

    // End node data
    public struct EndData : IComponentData
    {
        public EndType Type;
        public int NextGraphId; // For transition
    }

    public enum EndType : byte
    {
        Complete,
        Failed,
        Checkpoint,
        Transition
    }

    // Command buffer for structural changes
    public struct StoryCommandBuffer : IComponentData
    {
        public EntityCommandBuffer CommandBuffer;
    }

    // Node execution result
    public struct NodeExecutionResult : IComponentData
    {
        public ExecutionResultType Type;
        public int NextNodeId;
        public float WaitTime;
    }

    public enum ExecutionResultType : byte
    {
        Continue,
        Wait,
        WaitForInput,
        End
    }

    // Tag for nodes that need processing this frame
    public struct ExecuteNodeTag : IComponentData, IEnableableComponent { }

    // Tag for pending variable updates
    public struct VariableUpdateTag : IComponentData, IEnableableComponent { }

    // Story event for inter-system communication
    public struct StoryEvent : IBufferElementData
    {
        public int EventType;
        public int NodeId;
        public int Data;
    }

    public enum StoryEventType : int
    {
        NodeEntered,
        NodeExited,
        VariableChanged,
        DialogueDisplayed,
        ChoicePresented,
        StoryCompleted,
        EventTriggered
    }
}
