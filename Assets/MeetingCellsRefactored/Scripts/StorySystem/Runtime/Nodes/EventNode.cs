using System;
using System.Collections.Generic;
using UnityEngine;
using StorySystem.Core;

namespace StorySystem.Nodes
{
    /// <summary>
    /// Node for triggering game events
    /// </summary>
    public class EventNode : StoryNode
    {
        [SerializeField] private string eventName;
        [SerializeField] private string eventCategory;
        [SerializeField] private List<EventParameter> parameters = new List<EventParameter>();
        [SerializeField] private bool waitForCompletion = false;
        [SerializeField] private float timeout = 0f;

        public string EventName { get => eventName; set => eventName = value; }
        public string EventCategory { get => eventCategory; set => eventCategory = value; }
        public List<EventParameter> Parameters => parameters;
        public bool WaitForCompletion { get => waitForCompletion; set => waitForCompletion = value; }
        public float Timeout { get => timeout; set => timeout = value; }

        public override string DisplayName => "Event";
        public override string Category => "Events";
        public override Color NodeColor => new Color(0.8f, 0.3f, 0.5f);

        protected override void SetupPorts()
        {
            AddInputPort("Input", "input");
            AddOutputPort("Output", "output");
            AddOutputPort("On Timeout", "timeout");
        }

        public void AddParameter(string name, object value)
        {
            parameters.Add(new EventParameter { name = name, value = value?.ToString() });
        }

        public override StoryNodeResult Execute(StoryContext context)
        {
            // Build event data
            var eventData = new StoryEventData
            {
                eventName = eventName,
                category = eventCategory,
                sourceNodeId = NodeId,
                parameters = new Dictionary<string, object>()
            };

            foreach (var param in parameters)
            {
                eventData.parameters[param.name] = ProcessParameterValue(param.value, context);
            }

            // Trigger the event through the manager
            StoryManager.Instance?.TriggerEvent(eventData);

            if (waitForCompletion)
            {
                // Wait for event to complete
                bool completed = false;
                Action onComplete = () => completed = true;
                context.SetTempData("eventCompleteCallback", onComplete);
                
                return StoryNodeResult.WaitForCondition(() => completed, "output");
            }

            return StoryNodeResult.Continue("output");
        }

        private object ProcessParameterValue(string value, StoryContext context)
        {
            if (string.IsNullOrEmpty(value)) return value;

            // Check if it's a variable reference
            if (value.StartsWith("$"))
            {
                string varName = value.Substring(1);
                return context.GetVariable(varName);
            }

            return value;
        }

        public override Dictionary<string, object> GetSerializationData()
        {
            var data = base.GetSerializationData();
            data["eventName"] = eventName;
            data["eventCategory"] = eventCategory;
            data["waitForCompletion"] = waitForCompletion;
            data["timeout"] = timeout;
            
            var paramsData = new List<Dictionary<string, object>>();
            foreach (var param in parameters)
            {
                paramsData.Add(new Dictionary<string, object>
                {
                    { "name", param.name },
                    { "value", param.value },
                    { "type", param.type.ToString() }
                });
            }
            data["parameters"] = paramsData;
            
            return data;
        }

        public override void LoadSerializationData(Dictionary<string, object> data)
        {
            base.LoadSerializationData(data);
            if (data.TryGetValue("eventName", out var name)) eventName = name.ToString();
            if (data.TryGetValue("eventCategory", out var cat)) eventCategory = cat.ToString();
            if (data.TryGetValue("waitForCompletion", out var wait)) waitForCompletion = Convert.ToBoolean(wait);
            if (data.TryGetValue("timeout", out var to)) timeout = Convert.ToSingle(to);
            
            if (data.TryGetValue("parameters", out var paramsObj) && paramsObj is List<object> paramsList)
            {
                parameters.Clear();
                foreach (var paramObj in paramsList)
                {
                    if (paramObj is Dictionary<string, object> paramData)
                    {
                        var param = new EventParameter();
                        if (paramData.TryGetValue("name", out var pName)) param.name = pName.ToString();
                        if (paramData.TryGetValue("value", out var pValue)) param.value = pValue.ToString();
                        if (paramData.TryGetValue("type", out var pType))
                            Enum.TryParse<ParameterType>(pType.ToString(), out param.type);
                        parameters.Add(param);
                    }
                }
            }
        }
    }

    [Serializable]
    public class EventParameter
    {
        public string name;
        public string value;
        public ParameterType type = ParameterType.String;
    }

    public enum ParameterType
    {
        String,
        Int,
        Float,
        Bool,
        Variable
    }

    public class StoryEventData
    {
        public string eventName;
        public string category;
        public string sourceNodeId;
        public Dictionary<string, object> parameters;
    }
}