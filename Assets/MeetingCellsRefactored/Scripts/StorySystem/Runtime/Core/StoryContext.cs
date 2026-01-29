using System;
using System.Collections.Generic;
using UnityEngine;

namespace StorySystem.Core
{
    /// <summary>
    /// Runtime context for story execution. Holds variables, state, and references.
    /// </summary>
    public class StoryContext
    {
        private Dictionary<string, object> variables = new Dictionary<string, object>();
        private Dictionary<string, object> tempData = new Dictionary<string, object>();
        private Stack<string> nodeHistory = new Stack<string>();
        
        public StoryGraph CurrentGraph { get; set; }
        public StoryNode CurrentNode { get; set; }
        public bool IsPaused { get; set; }
        public bool IsComplete { get; private set; }

        public event Action<string, object, object> OnVariableChanged;
        public event Action<StoryNode> OnNodeChanged;

        public StoryContext()
        {
            Initialize();
        }

        public StoryContext(StoryGraph graph)
        {
            CurrentGraph = graph;
            Initialize();
        }

        private void Initialize()
        {
            if (CurrentGraph == null) return;

            // Initialize variables from graph
            foreach (var variable in CurrentGraph.Variables)
            {
                variables[variable.Name] = variable.GetDefaultValue();
            }
        }

        #region Variable Access

        public void SetVariable(string name, object value)
        {
            object oldValue = variables.ContainsKey(name) ? variables[name] : null;
            variables[name] = value;
            OnVariableChanged?.Invoke(name, oldValue, value);
        }

        public T GetVariable<T>(string name, T defaultValue = default)
        {
            if (variables.TryGetValue(name, out var value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        public object GetVariable(string name)
        {
            return variables.TryGetValue(name, out var value) ? value : null;
        }

        public bool HasVariable(string name)
        {
            return variables.ContainsKey(name);
        }

        public void IncrementVariable(string name, float amount = 1)
        {
            float current = GetVariable<float>(name);
            SetVariable(name, current + amount);
        }

        public bool EvaluateCondition(string variableName, ConditionOperator op, object compareValue)
        {
            var value = GetVariable(variableName);
            if (value == null) return false;

            try
            {
                switch (op)
                {
                    case ConditionOperator.Equals:
                        return value.Equals(compareValue);
                    case ConditionOperator.NotEquals:
                        return !value.Equals(compareValue);
                    case ConditionOperator.GreaterThan:
                        return Convert.ToDouble(value) > Convert.ToDouble(compareValue);
                    case ConditionOperator.LessThan:
                        return Convert.ToDouble(value) < Convert.ToDouble(compareValue);
                    case ConditionOperator.GreaterOrEqual:
                        return Convert.ToDouble(value) >= Convert.ToDouble(compareValue);
                    case ConditionOperator.LessOrEqual:
                        return Convert.ToDouble(value) <= Convert.ToDouble(compareValue);
                    case ConditionOperator.Contains:
                        return value.ToString().Contains(compareValue.ToString());
                    case ConditionOperator.IsTrue:
                        return Convert.ToBoolean(value);
                    case ConditionOperator.IsFalse:
                        return !Convert.ToBoolean(value);
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Temp Data (Per-execution data)

        public void SetTempData(string key, object value)
        {
            tempData[key] = value;
        }

        public T GetTempData<T>(string key, T defaultValue = default)
        {
            if (tempData.TryGetValue(key, out var value))
            {
                try
                {
                    return (T)value;
                }
                catch
                {
                    return defaultValue;
                }
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
            nodeHistory.Push(nodeId);
        }

        public string PopNodeFromHistory()
        {
            return nodeHistory.Count > 0 ? nodeHistory.Pop() : null;
        }

        public string PeekHistory()
        {
            return nodeHistory.Count > 0 ? nodeHistory.Peek() : null;
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
            variables.Clear();
            tempData.Clear();
            nodeHistory.Clear();
            CurrentNode = null;
            IsPaused = false;
            IsComplete = false;
            Initialize();
        }

        public Dictionary<string, object> SaveState()
        {
            return new Dictionary<string, object>(variables);
        }

        public void LoadState(Dictionary<string, object> state)
        {
            variables = new Dictionary<string, object>(state);
        }
    }

    public enum ConditionOperator
    {
        Equals,
        NotEquals,
        GreaterThan,
        LessThan,
        GreaterOrEqual,
        LessOrEqual,
        Contains,
        IsTrue,
        IsFalse
    }
}