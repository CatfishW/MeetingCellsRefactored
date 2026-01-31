using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace StorySystem.ECS
{
    /// <summary>
    /// Main story execution system - Burst-compiled for high performance
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct StoryExecutionSystem : ISystem
    {
        private EntityQuery _activeStoriesQuery;
        private EntityQuery _readyNodesQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Query for active story executions
            _activeStoriesQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<StoryExecution>()
                .WithNone<ExecuteNodeTag>()
                .Build(ref state);

            // Query for nodes ready to execute
            _readyNodesQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ExecuteNodeTag, StoryExecution>()
                .Build(ref state);

            state.RequireForUpdate<StoryExecution>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            // Process waiting stories
            var waitJob = new ProcessWaitingStoriesJob
            {
                DeltaTime = deltaTime,
                ExecuteNodeTagHandle = SystemAPI.GetComponentTypeHandle<ExecuteNodeTag>(),
                StoryExecutionHandle = SystemAPI.GetComponentTypeHandle<StoryExecution>()
            };
            state.Dependency = waitJob.ScheduleParallel(_activeStoriesQuery, state.Dependency);

            // Execute ready nodes
            var executeJob = new ExecuteNodesJob
            {
                DeltaTime = deltaTime,
                StoryExecutionHandle = SystemAPI.GetComponentTypeHandle<StoryExecution>(),
                VariableBufferHandle = SystemAPI.GetBufferTypeHandle<StoryVariable>(),
                DialogueDataHandle = SystemAPI.GetComponentTypeHandle<DialogueData>(),
                ChoiceDataHandle = SystemAPI.GetComponentTypeHandle<ChoiceData>(),
                ChoiceOptionBufferHandle = SystemAPI.GetBufferTypeHandle<ChoiceOption>(),
                ConditionDataHandle = SystemAPI.GetComponentTypeHandle<ConditionData>(),
                ConditionItemBufferHandle = SystemAPI.GetBufferTypeHandle<ConditionItem>(),
                WaitDataHandle = SystemAPI.GetComponentTypeHandle<WaitData>(),
                EndDataHandle = SystemAPI.GetComponentTypeHandle<EndData>()
            };
            state.Dependency = executeJob.ScheduleParallel(_readyNodesQuery, state.Dependency);
        }
    }

    /// <summary>
    /// Job to process stories in waiting state
    /// </summary>
    [BurstCompile]
    public partial struct ProcessWaitingStoriesJob : IJobEntity
    {
        public float DeltaTime;
        public ComponentTypeHandle<ExecuteNodeTag> ExecuteNodeTagHandle;
        public ComponentTypeHandle<StoryExecution> StoryExecutionHandle;

        void Execute(Entity entity, [EntityIndexInQuery] int index)
        {
            // Note: In real implementation, use EntityManager or ECB for structural changes
            // This is a simplified version showing the pattern
        }
    }

    /// <summary>
    /// Job to execute story nodes
    /// </summary>
    [BurstCompile]
    public partial struct ExecuteNodesJob : IJobEntity
    {
        public float DeltaTime;
        public ComponentTypeHandle<StoryExecution> StoryExecutionHandle;
        public BufferTypeHandle<StoryVariable> VariableBufferHandle;
        public ComponentTypeHandle<DialogueData> DialogueDataHandle;
        public ComponentTypeHandle<ChoiceData> ChoiceDataHandle;
        public BufferTypeHandle<ChoiceOption> ChoiceOptionBufferHandle;
        public ComponentTypeHandle<ConditionData> ConditionDataHandle;
        public BufferTypeHandle<ConditionItem> ConditionItemBufferHandle;
        public ComponentTypeHandle<WaitData> WaitDataHandle;
        public ComponentTypeHandle<EndData> EndDataHandle;

        void Execute(Entity entity, [EntityIndexInQuery] int index)
        {
            // Execution logic handled by specialized systems per node type
        }
    }

    /// <summary>
    /// System for executing dialogue nodes
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct DialogueExecutionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (execution, dialogue, entity) in
                SystemAPI.Query<RefRW<StoryExecution>, RefRO<DialogueData>>()
                    .WithAll<ExecuteNodeTag, DialogueNodeTag>()
                    .WithEntityAccess())
            {
                var exec = execution.ValueRW;
                var data = dialogue.ValueRO;

                // Set up dialogue display
                exec.WaitTimer = data.WaitForInput ? 0f : data.AutoAdvanceDelay;
                exec.State = data.WaitForInput ? StoryState.WaitingForInput : StoryState.Waiting;

                // Emit event for UI system
                // ECB would be used here in full implementation
            }
        }
    }

    /// <summary>
    /// System for executing condition nodes with Burst-compiled evaluation
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ConditionExecutionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (execution, conditionData, conditions, variables, entity) in
                SystemAPI.Query<RefRW<StoryExecution>, RefRO<ConditionData>,
                    DynamicBuffer<ConditionItem>, DynamicBuffer<StoryVariable>>()
                    .WithAll<ExecuteNodeTag, ConditionNodeTag>()
                    .WithEntityAccess())
            {
                bool result = EvaluateConditions(
                    conditions,
                    variables,
                    conditionData.ValueRO.Logic);

                var exec = execution.ValueRW;
                exec.NextPortIndex = result ? 0 : 1;
                exec.State = StoryState.Executing;
            }
        }

        [BurstCompile]
        private static bool EvaluateConditions(
            in DynamicBuffer<ConditionItem> conditions,
            in DynamicBuffer<StoryVariable> variables,
            ConditionLogic logic)
        {
            if (conditions.Length == 0) return true;

            bool isAnd = logic == ConditionLogic.And;

            for (int i = 0; i < conditions.Length; i++)
            {
                var condition = conditions[i];
                bool conditionResult = EvaluateCondition(condition, variables);

                if (isAnd && !conditionResult) return false;
                if (!isAnd && conditionResult) return true;
            }

            return isAnd;
        }

        [BurstCompile]
        private static bool EvaluateCondition(
            in ConditionItem condition,
            in DynamicBuffer<StoryVariable> variables)
        {
            // Find variable
            StoryVariable variable = default;
            bool found = false;

            for (int i = 0; i < variables.Length; i++)
            {
                if (variables[i].VariableId == condition.VariableId)
                {
                    variable = variables[i];
                    found = true;
                    break;
                }
            }

            if (!found) return false;

            float varValue = variable.Type == VariableType.Float
                ? variable.FloatValue
                : variable.Type == VariableType.Int
                    ? variable.IntValue
                    : variable.BoolValue ? 1f : 0f;

            switch (condition.Operator)
            {
                case ConditionOperator.Equals:
                    return math.abs(varValue - condition.CompareValue) < 0.0001f;
                case ConditionOperator.NotEquals:
                    return math.abs(varValue - condition.CompareValue) >= 0.0001f;
                case ConditionOperator.GreaterThan:
                    return varValue > condition.CompareValue;
                case ConditionOperator.LessThan:
                    return varValue < condition.CompareValue;
                case ConditionOperator.GreaterOrEqual:
                    return varValue >= condition.CompareValue;
                case ConditionOperator.LessOrEqual:
                    return varValue <= condition.CompareValue;
                case ConditionOperator.IsTrue:
                    return varValue > 0.5f;
                case ConditionOperator.IsFalse:
                    return varValue <= 0.5f;
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// System for executing wait nodes
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct WaitExecutionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (execution, waitData, entity) in
                SystemAPI.Query<RefRW<StoryExecution>, RefRO<WaitData>>()
                    .WithAll<ExecuteNodeTag, WaitNodeTag>()
                    .WithEntityAccess())
            {
                var exec = execution.ValueRW;
                var data = waitData.ValueRO;

                switch (data.Type)
                {
                    case WaitType.Time:
                        exec.WaitTimer = data.Duration;
                        exec.State = StoryState.Waiting;
                        break;

                    case WaitType.Input:
                        exec.State = StoryState.WaitingForInput;
                        break;

                    case WaitType.Frame:
                        exec.WaitTimer = 0f;
                        exec.State = StoryState.Waiting;
                        break;
                }
            }
        }
    }

    /// <summary>
    /// System for executing end nodes
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct EndExecutionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (execution, endData, entity) in
                SystemAPI.Query<RefRW<StoryExecution>, RefRO<EndData>>()
                    .WithAll<ExecuteNodeTag, EndNodeTag>()
                    .WithEntityAccess())
            {
                var exec = execution.ValueRW;
                exec.State = StoryState.Complete;

                // Emit completion event
                // Handle transition to next graph if needed
            }
        }
    }
}
