using System;
using System.Collections.Generic;
using UnityEngine;
using StorySystem.Core;

namespace StorySystem.Nodes
{
    /// <summary>
    /// Node for branching based on conditions
    /// </summary>
    public class ConditionNode : StoryNode
    {
        [SerializeField] private List<Condition> conditions = new List<Condition>();
        [SerializeField] private ConditionLogic logic = ConditionLogic.And;

        public List<Condition> Conditions => conditions;
        public ConditionLogic Logic { get => logic; set => logic = value; }

        public override string DisplayName => "Condition";
        public override string Category => "Flow";
        public override Color NodeColor => new Color(0.6f, 0.4f, 0.8f);

        protected override void SetupPorts()
        {
            AddInputPort("Input", "input");
            AddOutputPort("True", "true");
            AddOutputPort("False", "false");
        }

        public void AddCondition(string variableName, ConditionOperator op, object value)
        {
            conditions.Add(new Condition
            {
                variableName = variableName,
                operatorType = op,
                compareValue = value?.ToString()
            });
        }

        public override StoryNodeResult Execute(StoryContext context)
        {
            bool result = EvaluateConditions(context);
            return StoryNodeResult.Continue(result ? "true" : "false");
        }

        private bool EvaluateConditions(StoryContext context)
        {
            if (conditions.Count == 0) return true;

            if (logic == ConditionLogic.And)
            {
                foreach (var condition in conditions)
                {
                    if (!EvaluateSingleCondition(condition, context))
                        return false;
                }
                return true;
            }
            else // Or
            {
                foreach (var condition in conditions)
                {
                    if (EvaluateSingleCondition(condition, context))
                        return true;
                }
                return false;
            }
        }

        private bool EvaluateSingleCondition(Condition condition, StoryContext context)
        {
            object compareValue = ParseValue(condition.compareValue);
            return context.EvaluateCondition(condition.variableName, condition.operatorType, compareValue);
        }

        private object ParseValue(string value)
        {
            if (bool.TryParse(value, out bool boolResult))
                return boolResult;
            if (int.TryParse(value, out int intResult))
                return intResult;
            if (float.TryParse(value, out float floatResult))
                return floatResult;
            return value;
        }

        public override Dictionary<string, object> GetSerializationData()
        {
            var data = base.GetSerializationData();
            data["logic"] = logic.ToString();
            
            var conditionsData = new List<Dictionary<string, object>>();
            foreach (var condition in conditions)
            {
                conditionsData.Add(new Dictionary<string, object>
                {
                    { "variableName", condition.variableName },
                    { "operatorType", condition.operatorType.ToString() },
                    { "compareValue", condition.compareValue }
                });
            }
            data["conditions"] = conditionsData;
            
            return data;
        }

        public override void LoadSerializationData(Dictionary<string, object> data)
        {
            base.LoadSerializationData(data);
            if (data.TryGetValue("logic", out var logicStr))
                Enum.TryParse<ConditionLogic>(logicStr.ToString(), out logic);
            
            if (data.TryGetValue("conditions", out var conditionsObj) && conditionsObj is List<object> conditionsList)
            {
                conditions.Clear();
                foreach (var condObj in conditionsList)
                {
                    if (condObj is Dictionary<string, object> condData)
                    {
                        var condition = new Condition();
                        if (condData.TryGetValue("variableName", out var name)) condition.variableName = name.ToString();
                        if (condData.TryGetValue("operatorType", out var op)) 
                            Enum.TryParse<ConditionOperator>(op.ToString(), out condition.operatorType);
                        if (condData.TryGetValue("compareValue", out var val)) condition.compareValue = val.ToString();
                        conditions.Add(condition);
                    }
                }
            }
        }
    }

    [Serializable]
    public class Condition
    {
        public string variableName;
        public ConditionOperator operatorType;
        public string compareValue;
    }

    public enum ConditionLogic
    {
        And,
        Or
    }
}