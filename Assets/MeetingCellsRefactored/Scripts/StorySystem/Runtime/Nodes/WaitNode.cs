using System;
using System.Collections.Generic;
using UnityEngine;
using StorySystem.Core;

namespace StorySystem.Nodes
{
    /// <summary>
    /// Node for waiting (time, input, or condition)
    /// </summary>
    public class WaitNode : StoryNode
    {
        [SerializeField] private WaitType waitType = WaitType.Time;
        [SerializeField] private float waitTime = 1f;
        [SerializeField] private string conditionVariable;
        [SerializeField] private ConditionOperator conditionOperator = ConditionOperator.IsTrue;
        [SerializeField] private string conditionValue;
        [SerializeField] private bool useUnscaledTime = false;
        [SerializeField] private string inputAction;

        public WaitType WaitType { get => waitType; set => waitType = value; }
        public float WaitTime { get => waitTime; set => waitTime = value; }
        public string ConditionVariable { get => conditionVariable; set => conditionVariable = value; }
        public ConditionOperator ConditionOperator { get => conditionOperator; set => conditionOperator = value; }
        public string ConditionValue { get => conditionValue; set => conditionValue = value; }
        public bool UseUnscaledTime { get => useUnscaledTime; set => useUnscaledTime = value; }
        public string InputAction { get => inputAction; set => inputAction = value; }

        public override string DisplayName => "Wait";
        public override string Category => "Flow";
        public override Color NodeColor => new Color(0.5f, 0.5f, 0.7f);

        public override StoryNodeResult Execute(StoryContext context)
        {
            switch (waitType)
            {
                case WaitType.Time:
                    return StoryNodeResult.Wait(waitTime, "output");

                case WaitType.Input:
                    return StoryNodeResult.WaitForInput("output");

                case WaitType.Condition:
                    return StoryNodeResult.WaitForCondition(() =>
                    {
                        object value = ParseValue(conditionValue);
                        return context.EvaluateCondition(conditionVariable, conditionOperator, value);
                    }, "output");

                case WaitType.Frame:
                    return StoryNodeResult.Wait(0f, "output");

                default:
                    return StoryNodeResult.Continue("output");
            }
        }

        private object ParseValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            if (bool.TryParse(value, out bool boolResult)) return boolResult;
            if (int.TryParse(value, out int intResult)) return intResult;
            if (float.TryParse(value, out float floatResult)) return floatResult;
            return value;
        }

        public override Dictionary<string, object> GetSerializationData()
        {
            var data = base.GetSerializationData();
            data["waitType"] = waitType.ToString();
            data["waitTime"] = waitTime;
            data["conditionVariable"] = conditionVariable;
            data["conditionOperator"] = conditionOperator.ToString();
            data["conditionValue"] = conditionValue;
            data["useUnscaledTime"] = useUnscaledTime;
            data["inputAction"] = inputAction;
            return data;
        }

        public override void LoadSerializationData(Dictionary<string, object> data)
        {
            base.LoadSerializationData(data);
            if (data.TryGetValue("waitType", out var type))
                Enum.TryParse<WaitType>(type.ToString(), out waitType);
            if (data.TryGetValue("waitTime", out var time)) waitTime = Convert.ToSingle(time);
            if (data.TryGetValue("conditionVariable", out var condVar)) conditionVariable = condVar.ToString();
            if (data.TryGetValue("conditionOperator", out var op))
                Enum.TryParse<ConditionOperator>(op.ToString(), out conditionOperator);
            if (data.TryGetValue("conditionValue", out var val)) conditionValue = val.ToString();
            if (data.TryGetValue("useUnscaledTime", out var unscaled)) useUnscaledTime = Convert.ToBoolean(unscaled);
            if (data.TryGetValue("inputAction", out var action)) inputAction = action.ToString();
        }
    }

    public enum WaitType
    {
        Time,
        Input,
        Condition,
        Frame
    }
}