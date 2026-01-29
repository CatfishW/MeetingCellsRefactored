using System.Collections.Generic;
using UnityEngine;
using StorySystem.Core;

namespace StorySystem.Nodes
{
    /// <summary>
    /// Entry point node for a story graph
    /// </summary>
    public class StartNode : StoryNode
    {
        [SerializeField] private string startLabel = "Start";
        [SerializeField] private bool isDefaultStart = true;

        public string StartLabel { get => startLabel; set => startLabel = value; }
        public bool IsDefaultStart { get => isDefaultStart; set => isDefaultStart = value; }

        public override string DisplayName => "Start";
        public override string Category => "Flow";
        public override Color NodeColor => new Color(0.2f, 0.6f, 0.2f);

        protected override void SetupPorts()
        {
            // Start nodes only have output
            AddOutputPort("Start", "output");
        }

        public override StoryNodeResult Execute(StoryContext context)
        {
            return StoryNodeResult.Continue("output");
        }

        public override List<string> Validate()
        {
            var errors = base.Validate();
            if (string.IsNullOrEmpty(startLabel))
            {
                errors.Add($"Start node '{NodeId}' has no label");
            }
            return errors;
        }

        public override Dictionary<string, object> GetSerializationData()
        {
            var data = base.GetSerializationData();
            data["startLabel"] = startLabel;
            data["isDefaultStart"] = isDefaultStart;
            return data;
        }

        public override void LoadSerializationData(Dictionary<string, object> data)
        {
            base.LoadSerializationData(data);
            if (data.TryGetValue("startLabel", out var label)) startLabel = label.ToString();
            if (data.TryGetValue("isDefaultStart", out var isDefault)) isDefaultStart = (bool)isDefault;
        }
    }
}