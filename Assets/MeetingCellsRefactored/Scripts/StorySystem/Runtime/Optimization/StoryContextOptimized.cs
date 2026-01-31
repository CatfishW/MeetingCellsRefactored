using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using StorySystem.Core;

namespace StorySystem.Optimization
{
    /// <summary>
    /// High-performance replacement for StoryContext using NativeCollections
    /// Drop-in replacement with Burst-compatible operations
    /// </summary>
    public class StoryContextOptimized : IDisposable
    {
        // Use NativeHashMap for O(1) lookups without boxing
        private NativeHashMap<int, float> floatVariables;
        private NativeHashMap<int, int> intVariables;
        private NativeHashMap<int, bool> boolVariables;
        private NativeHashMap<int, FixedString128Bytes> stringVariables;

        // Temp data storage
        private Dictionary<string, object> tempData;
        private Stack<int> nodeHistory;

        public StoryGraph CurrentGraph { get; set; }
        public StoryNode CurrentNode { get; set; }
        public bool IsPaused { get; set; }
        public bool IsComplete { get; private set; }

        public event Action<int, object, object> OnVariableChanged;
        public event Action<StoryNode> OnNodeChanged;

        public StoryContextOptimized()
        {
            InitializeCollections();
        }

        public StoryContextOptimized(StoryGraph graph)
        {
            CurrentGraph = graph;
            InitializeCollections();
            InitializeFromGraph();
        }

        private void InitializeCollections()
        {
            floatVariables = new NativeHashMap<int, float>(32, Allocator.Persistent);
            intVariables = new NativeHashMap<int, int>(32, Allocator.Persistent);
            boolVariables = new NativeHashMap<int, bool>(32, Allocator.Persistent);
            stringVariables = new NativeHashMap<int, FixedString128Bytes>(16, Allocator.Persistent);
            tempData = new Dictionary<string, object>();
            nodeHistory = new Stack<int>(32);
        }

        private void InitializeFromGraph()
        {
            if (CurrentGraph == null) return;

            foreach (var variable in CurrentGraph.Variables)
            {
                int hash = variable.Name.GetHashCode();
                object defaultValue = variable.GetDefaultValue();
                switch (variable.Type)
                {
                    case VariableType.Float:
                        if (defaultValue is float f)
                            floatVariables[hash] = f;
                        break;
                    case VariableType.Int:
                        if (defaultValue is int i)
                            intVariables[hash] = i;
                        break;
                    case VariableType.Bool:
                        if (defaultValue is bool b)
                            boolVariables[hash] = b;
                        break;
                    case VariableType.String:
                        stringVariables[hash] = new FixedString128Bytes(defaultValue?.ToString() ?? "");
                        break;
                }
            }
        }

        #region Variable Access

        public void SetVariable(string name, float value)
        {
            int hash = name.GetHashCode();
            float oldValue = floatVariables.TryGetValue(hash, out float existing) ? existing : 0f;
            floatVariables[hash] = value;
            OnVariableChanged?.Invoke(hash, oldValue, value);
        }

        public void SetVariable(string name, int value)
        {
            int hash = name.GetHashCode();
            int oldValue = intVariables.TryGetValue(hash, out int existing) ? existing : 0;
            intVariables[hash] = value;
            OnVariableChanged?.Invoke(hash, oldValue, value);
        }

        public void SetVariable(string name, bool value)
        {
            int hash = name.GetHashCode();
            bool oldValue = boolVariables.TryGetValue(hash, out bool existing) ? existing : false;
            boolVariables[hash] = value;
            OnVariableChanged?.Invoke(hash, oldValue, value);
        }

        public void SetVariable(string name, string value)
        {
            int hash = name.GetHashCode();
            FixedString128Bytes newValue = new FixedString128Bytes(value);
            FixedString128Bytes oldValue = stringVariables.TryGetValue(hash, out FixedString128Bytes existing)
                ? existing : default;
            stringVariables[hash] = newValue;
            OnVariableChanged?.Invoke(hash, oldValue.ToString(), value);
        }

        public float GetFloat(string name, float defaultValue = 0f)
        {
            int hash = name.GetHashCode();
            return floatVariables.TryGetValue(hash, out float value) ? value : defaultValue;
        }

        public int GetInt(string name, int defaultValue = 0)
        {
            int hash = name.GetHashCode();
            return intVariables.TryGetValue(hash, out int value) ? value : defaultValue;
        }

        public bool GetBool(string name, bool defaultValue = false)
        {
            int hash = name.GetHashCode();
            return boolVariables.TryGetValue(hash, out bool value) ? value : defaultValue;
        }

        public string GetString(string name, string defaultValue = "")
        {
            int hash = name.GetHashCode();
            return stringVariables.TryGetValue(hash, out FixedString128Bytes value)
                ? value.ToString() : defaultValue;
        }

        public bool HasVariable(string name)
        {
            int hash = name.GetHashCode();
            return floatVariables.ContainsKey(hash) ||
                   intVariables.ContainsKey(hash) ||
                   boolVariables.ContainsKey(hash) ||
                   stringVariables.ContainsKey(hash);
        }

        public void IncrementVariable(string name, float amount = 1f)
        {
            int hash = name.GetHashCode();
            float current = floatVariables.TryGetValue(hash, out float value) ? value : 0f;
            floatVariables[hash] = current + amount;
            OnVariableChanged?.Invoke(hash, current, current + amount);
        }

        public void IncrementVariable(string name, int amount = 1)
        {
            int hash = name.GetHashCode();
            int current = intVariables.TryGetValue(hash, out int value) ? value : 0;
            intVariables[hash] = current + amount;
            OnVariableChanged?.Invoke(hash, current, current + amount);
        }

        #endregion

        #region Condition Evaluation

        public bool EvaluateCondition(string variableName, ConditionOperator op, object compareValue)
        {
            int hash = variableName.GetHashCode();

            // Try to get value from appropriate storage
            if (floatVariables.TryGetValue(hash, out float floatValue))
            {
                return EvaluateFloatCondition(floatValue, op, compareValue);
            }
            if (intVariables.TryGetValue(hash, out int intValue))
            {
                return EvaluateIntCondition(intValue, op, compareValue);
            }
            if (boolVariables.TryGetValue(hash, out bool boolValue))
            {
                return EvaluateBoolCondition(boolValue, op, compareValue);
            }
            if (stringVariables.TryGetValue(hash, out FixedString128Bytes stringValue))
            {
                return EvaluateStringCondition(stringValue.ToString(), op, compareValue);
            }

            return false;
        }

        private bool EvaluateFloatCondition(float value, ConditionOperator op, object compareValue)
        {
            float compare = Convert.ToSingle(compareValue);
            return op switch
            {
                ConditionOperator.Equals => Math.Abs(value - compare) < 0.0001f,
                ConditionOperator.NotEquals => Math.Abs(value - compare) >= 0.0001f,
                ConditionOperator.GreaterThan => value > compare,
                ConditionOperator.LessThan => value < compare,
                ConditionOperator.GreaterOrEqual => value >= compare,
                ConditionOperator.LessOrEqual => value <= compare,
                _ => false
            };
        }

        private bool EvaluateIntCondition(int value, ConditionOperator op, object compareValue)
        {
            int compare = Convert.ToInt32(compareValue);
            return op switch
            {
                ConditionOperator.Equals => value == compare,
                ConditionOperator.NotEquals => value != compare,
                ConditionOperator.GreaterThan => value > compare,
                ConditionOperator.LessThan => value < compare,
                ConditionOperator.GreaterOrEqual => value >= compare,
                ConditionOperator.LessOrEqual => value <= compare,
                _ => false
            };
        }

        private bool EvaluateBoolCondition(bool value, ConditionOperator op, object compareValue)
        {
            bool compare = Convert.ToBoolean(compareValue);
            return op switch
            {
                ConditionOperator.Equals => value == compare,
                ConditionOperator.NotEquals => value != compare,
                ConditionOperator.IsTrue => value,
                ConditionOperator.IsFalse => !value,
                _ => false
            };
        }

        private bool EvaluateStringCondition(string value, ConditionOperator op, object compareValue)
        {
            string compare = compareValue?.ToString() ?? "";
            return op switch
            {
                ConditionOperator.Equals => value == compare,
                ConditionOperator.NotEquals => value != compare,
                ConditionOperator.Contains => value.Contains(compare),
                _ => false
            };
        }

        #endregion

        #region Temp Data

        public void SetTempData(string key, object value)
        {
            tempData[key] = value;
        }

        public T GetTempData<T>(string key, T defaultValue = default)
        {
            if (tempData.TryGetValue(key, out var value) && value is T typed)
            {
                return typed;
            }
            return defaultValue;
        }

        public void ClearTempData()
        {
            tempData.Clear();
        }

        #endregion

        #region Navigation History

        public void PushNodeToHistory(string nodeId)
        {
            nodeHistory.Push(nodeId.GetHashCode());
        }

        public string PopNodeFromHistory()
        {
            return nodeHistory.Count > 0 ? nodeHistory.Pop().ToString() : null;
        }

        public string PeekHistory()
        {
            return nodeHistory.Count > 0 ? nodeHistory.Peek().ToString() : null;
        }

        public void ClearHistory()
        {
            nodeHistory.Clear();
        }

        #endregion

        public void SetCurrentNode(StoryNode node)
        {
            if (CurrentNode != null)
            {
                PushNodeToHistory(CurrentNode.NodeId);
            }
            CurrentNode = node;
            OnNodeChanged?.Invoke(node);
        }

        public void MarkComplete()
        {
            IsComplete = true;
        }

        public void Reset()
        {
            floatVariables.Clear();
            intVariables.Clear();
            boolVariables.Clear();
            stringVariables.Clear();
            tempData.Clear();
            nodeHistory.Clear();
            CurrentNode = null;
            IsPaused = false;
            IsComplete = false;
            InitializeFromGraph();
        }

        public Dictionary<string, object> SaveState()
        {
            var state = new Dictionary<string, object>();

            foreach (var kvp in floatVariables)
            {
                state[kvp.Key.ToString()] = kvp.Value;
            }
            foreach (var kvp in intVariables)
            {
                state[kvp.Key.ToString()] = kvp.Value;
            }
            foreach (var kvp in boolVariables)
            {
                state[kvp.Key.ToString()] = kvp.Value;
            }
            foreach (var kvp in stringVariables)
            {
                state[kvp.Key.ToString()] = kvp.Value.ToString();
            }

            return state;
        }

        public void LoadState(Dictionary<string, object> state)
        {
            Reset();

            foreach (var kvp in state)
            {
                int hash = kvp.Key.GetHashCode();
                switch (kvp.Value)
                {
                    case float f:
                        floatVariables[hash] = f;
                        break;
                    case int i:
                        intVariables[hash] = i;
                        break;
                    case bool b:
                        boolVariables[hash] = b;
                        break;
                    case string s:
                        stringVariables[hash] = new FixedString128Bytes(s);
                        break;
                }
            }
        }

        public void Dispose()
        {
            if (floatVariables.IsCreated) floatVariables.Dispose();
            if (intVariables.IsCreated) intVariables.Dispose();
            if (boolVariables.IsCreated) boolVariables.Dispose();
            if (stringVariables.IsCreated) stringVariables.Dispose();
        }
    }
}
