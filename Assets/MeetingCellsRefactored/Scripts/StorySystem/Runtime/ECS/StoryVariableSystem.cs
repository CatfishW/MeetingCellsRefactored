using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace StorySystem.ECS
{
    /// <summary>
    /// High-performance variable management system
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct StoryVariableSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StoryExecution>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Process variable operations in parallel
            var job = new VariableOperationJob
            {
                VariableBufferHandle = SystemAPI.GetBufferTypeHandle<StoryVariable>()
            };

            var query = SystemAPI.QueryBuilder()
                .WithAll<StoryExecution>()
                .WithAll<VariableUpdateTag>()
                .Build();

            state.Dependency = job.ScheduleParallel(query, state.Dependency);
        }
    }

    /// <summary>
    /// Job for batch variable operations
    /// </summary>
    [BurstCompile]
    public partial struct VariableOperationJob : IJobEntity
    {
        public BufferTypeHandle<StoryVariable> VariableBufferHandle;

        void Execute([EntityIndexInQuery] int index, ref DynamicBuffer<StoryVariable> variables)
        {
            // Batch operations handled here
        }
    }

    /// <summary>
    /// Burst-compiled variable operations
    /// </summary>
    public static class StoryVariableOperations
    {
        [BurstCompile]
        public static bool TryGetVariable(in DynamicBuffer<StoryVariable> variables, int variableId, out StoryVariable result)
        {
            for (int i = 0; i < variables.Length; i++)
            {
                if (variables[i].VariableId == variableId)
                {
                    result = variables[i];
                    return true;
                }
            }
            result = default;
            return false;
        }

        [BurstCompile]
        public static void SetVariable(ref DynamicBuffer<StoryVariable> variables, int variableId, float value)
        {
            for (int i = 0; i < variables.Length; i++)
            {
                if (variables[i].VariableId == variableId)
                {
                    var var = variables[i];
                    var.FloatValue = value;
                    var.Type = VariableType.Float;
                    variables[i] = var;
                    return;
                }
            }

            // Add new variable
            variables.Add(new StoryVariable
            {
                VariableId = variableId,
                Type = VariableType.Float,
                FloatValue = value
            });
        }

        [BurstCompile]
        public static void SetVariable(ref DynamicBuffer<StoryVariable> variables, int variableId, int value)
        {
            for (int i = 0; i < variables.Length; i++)
            {
                if (variables[i].VariableId == variableId)
                {
                    var var = variables[i];
                    var.IntValue = value;
                    var.Type = VariableType.Int;
                    variables[i] = var;
                    return;
                }
            }

            variables.Add(new StoryVariable
            {
                VariableId = variableId,
                Type = VariableType.Int,
                IntValue = value
            });
        }

        [BurstCompile]
        public static void SetVariable(ref DynamicBuffer<StoryVariable> variables, int variableId, bool value)
        {
            for (int i = 0; i < variables.Length; i++)
            {
                if (variables[i].VariableId == variableId)
                {
                    var var = variables[i];
                    var.BoolValue = value;
                    var.Type = VariableType.Bool;
                    variables[i] = var;
                    return;
                }
            }

            variables.Add(new StoryVariable
            {
                VariableId = variableId,
                Type = VariableType.Bool,
                BoolValue = value
            });
        }

        [BurstCompile]
        public static float GetFloat(in DynamicBuffer<StoryVariable> variables, int variableId, float defaultValue = 0f)
        {
            if (TryGetVariable(variables, variableId, out var variable))
            {
                return variable.Type switch
                {
                    VariableType.Float => variable.FloatValue,
                    VariableType.Int => variable.IntValue,
                    VariableType.Bool => variable.BoolValue ? 1f : 0f,
                    _ => defaultValue
                };
            }
            return defaultValue;
        }

        [BurstCompile]
        public static int GetInt(in DynamicBuffer<StoryVariable> variables, int variableId, int defaultValue = 0)
        {
            if (TryGetVariable(variables, variableId, out var variable))
            {
                return variable.Type switch
                {
                    VariableType.Float => (int)variable.FloatValue,
                    VariableType.Int => variable.IntValue,
                    VariableType.Bool => variable.BoolValue ? 1 : 0,
                    _ => defaultValue
                };
            }
            return defaultValue;
        }

        [BurstCompile]
        public static bool GetBool(in DynamicBuffer<StoryVariable> variables, int variableId, bool defaultValue = false)
        {
            if (TryGetVariable(variables, variableId, out var variable))
            {
                return variable.Type switch
                {
                    VariableType.Float => variable.FloatValue > 0.5f,
                    VariableType.Int => variable.IntValue > 0,
                    VariableType.Bool => variable.BoolValue,
                    _ => defaultValue
                };
            }
            return defaultValue;
        }

        [BurstCompile]
        public static void IncrementVariable(ref DynamicBuffer<StoryVariable> variables, int variableId, float amount)
        {
            float current = GetFloat(variables, variableId, 0f);
            SetVariable(ref variables, variableId, current + amount);
        }

        [BurstCompile]
        public static void MultiplyVariable(ref DynamicBuffer<StoryVariable> variables, int variableId, float factor)
        {
            float current = GetFloat(variables, variableId, 0f);
            SetVariable(ref variables, variableId, current * factor);
        }

        /// <summary>
        /// Batch evaluate conditions for multiple stories in parallel
        /// </summary>
        [BurstCompile]
        public static void BatchEvaluateConditions(
            in NativeArray<ConditionItem> conditions,
            in NativeArray<int> conditionCounts,
            in NativeArray<ConditionLogic> logics,
            in NativeArray<StoryVariable> variableData,
            in NativeArray<int> variableOffsets,
            ref NativeArray<bool> results)
        {
            for (int i = 0; i < conditionCounts.Length; i++)
            {
                int offset = i == 0 ? 0 : conditionCounts[i - 1];
                int count = conditionCounts[i];
                int varOffset = variableOffsets[i];

                bool isAnd = logics[i] == ConditionLogic.And;
                bool result = isAnd;

                for (int j = 0; j < count; j++)
                {
                    var condition = conditions[offset + j];
                    bool conditionMet = false;

                    // Find variable in this story's variable block
                    for (int k = varOffset; k < varOffset + variableOffsets[i + 1] - varOffset; k++)
                    {
                        if (variableData[k].VariableId == condition.VariableId)
                        {
                            conditionMet = EvaluateSingleCondition(variableData[k], condition);
                            break;
                        }
                    }

                    if (isAnd && !conditionMet)
                    {
                        result = false;
                        break;
                    }
                    if (!isAnd && conditionMet)
                    {
                        result = true;
                        break;
                    }
                }

                results[i] = result;
            }
        }

        [BurstCompile]
        private static bool EvaluateSingleCondition(StoryVariable variable, ConditionItem condition)
        {
            float value = variable.Type switch
            {
                VariableType.Float => variable.FloatValue,
                VariableType.Int => variable.IntValue,
                VariableType.Bool => variable.BoolValue ? 1f : 0f,
                _ => 0f
            };

            return condition.Operator switch
            {
                ConditionOperator.Equals => math.abs(value - condition.CompareValue) < 0.0001f,
                ConditionOperator.NotEquals => math.abs(value - condition.CompareValue) >= 0.0001f,
                ConditionOperator.GreaterThan => value > condition.CompareValue,
                ConditionOperator.LessThan => value < condition.CompareValue,
                ConditionOperator.GreaterOrEqual => value >= condition.CompareValue,
                ConditionOperator.LessOrEqual => value <= condition.CompareValue,
                ConditionOperator.IsTrue => value > 0.5f,
                ConditionOperator.IsFalse => value <= 0.5f,
                _ => false
            };
        }
    }

    /// <summary>
    /// Component for batched variable operations
    /// </summary>
    public struct VariableOperation : IBufferElementData
    {
        public VariableOpType Operation;
        public int VariableId;
        public float FloatValue;
        public int IntValue;
        public bool BoolValue;
    }

    public enum VariableOpType : byte
    {
        SetFloat,
        SetInt,
        SetBool,
        Increment,
        Decrement,
        Multiply,
        Divide,
        Toggle
    }
}
