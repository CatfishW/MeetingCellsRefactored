using System;
using System.Collections.Generic;
using UnityEngine;
using StorySystem.Core;

namespace StorySystem.Nodes
{
    /// <summary>
    /// Node for setting/modifying variables
    /// </summary>
    public class VariableNode : StoryNode
    {
        [SerializeField] private List<VariableOperation> operations = new List<VariableOperation>();

        public List<VariableOperation> Operations => operations;

        public override string DisplayName => "Variable";
        public override string Category => "Logic";
        public override Color NodeColor => new Color(0.4f, 0.7f, 0.4f);

        public void AddOperation(string variableName, VariableOperationType operation, string value)
        {
            operations.Add(new VariableOperation
            {
                variableName = variableName,
                operation = operation,
                value = value
            });
        }

        public override StoryNodeResult Execute(StoryContext context)
        {
            foreach (var operation in operations)
            {
                ExecuteOperation(operation, context);
            }
            return StoryNodeResult.Continue("output");
        }

        private void ExecuteOperation(VariableOperation op, StoryContext context)
        {
            object value = ParseValue(op.value, context);

            switch (op.operation)
            {
                case VariableOperationType.Set:
                    context.SetVariable(op.variableName, value);
                    break;

                case VariableOperationType.Add:
                    float currentAdd = context.GetVariable<float>(op.variableName);
                    context.SetVariable(op.variableName, currentAdd + Convert.ToSingle(value));
                    break;

                case VariableOperationType.Subtract:
                    float currentSub = context.GetVariable<float>(op.variableName);
                    context.SetVariable(op.variableName, currentSub - Convert.ToSingle(value));
                    break;

                case VariableOperationType.Multiply:
                    float currentMul = context.GetVariable<float>(op.variableName);
                    context.SetVariable(op.variableName, currentMul * Convert.ToSingle(value));
                    break;

                case VariableOperationType.Divide:
                    float currentDiv = context.GetVariable<float>(op.variableName);
                    float divisor = Convert.ToSingle(value);
                    if (divisor != 0)
                        context.SetVariable(op.variableName, currentDiv / divisor);
                    break;

                case VariableOperationType.Toggle:
                    bool currentToggle = context.GetVariable<bool>(op.variableName);
                    context.SetVariable(op.variableName, !currentToggle);
                    break;

                case VariableOperationType.Append:
                    string currentAppend = context.GetVariable<string>(op.variableName, "");
                    context.SetVariable(op.variableName, currentAppend + value.ToString());
                    break;

                case VariableOperationType.Random:
                    if (value.ToString().Contains(","))
                    {
                        var parts = value.ToString().Split(',');
                        if (parts.Length == 2 &&
                            float.TryParse(parts[0], out float min) &&
                            float.TryParse(parts[1], out float max))
                        {
                            context.SetVariable(op.variableName, UnityEngine.Random.Range(min, max));
                        }
                    }
                    break;
            }
        }

        private object ParseValue(string value, StoryContext context)
        {
            if (string.IsNullOrEmpty(value)) return value;

            // Variable reference
            if (value.StartsWith("$"))
            {
                return context.GetVariable(value.Substring(1));
            }

            // Try to parse as various types
            if (bool.TryParse(value, out bool boolResult)) return boolResult;
            if (int.TryParse(value, out int intResult)) return intResult;
            if (float.TryParse(value, out float floatResult)) return floatResult;

            return value;
        }

        public override Dictionary<string, object> GetSerializationData()
        {
            var data = base.GetSerializationData();
            
            var opsData = new List<Dictionary<string, object>>();
            foreach (var op in operations)
            {
                opsData.Add(new Dictionary<string, object>
                {
                    { "variableName", op.variableName },
                    { "operation", op.operation.ToString() },
                    { "value", op.value }
                });
            }
            data["operations"] = opsData;
            
            return data;
        }

        public override void LoadSerializationData(Dictionary<string, object> data)
        {
            base.LoadSerializationData(data);
            
            if (data.TryGetValue("operations", out var opsObj) && opsObj is List<object> opsList)
            {
                operations.Clear();
                foreach (var opObj in opsList)
                {
                    if (opObj is Dictionary<string, object> opData)
                    {
                        var op = new VariableOperation();
                        if (opData.TryGetValue("variableName", out var name)) op.variableName = name.ToString();
                        if (opData.TryGetValue("operation", out var opType))
                            Enum.TryParse<VariableOperationType>(opType.ToString(), out op.operation);
                        if (opData.TryGetValue("value", out var val)) op.value = val.ToString();
                        operations.Add(op);
                    }
                }
            }
        }
    }

    [Serializable]
    public class VariableOperation
    {
        public string variableName;
        public VariableOperationType operation;
        public string value;
    }

    public enum VariableOperationType
    {
        Set,
        Add,
        Subtract,
        Multiply,
        Divide,
        Toggle,
        Append,
        Random
    }
}