using UnityEngine;
using StorySystem.Core;

namespace StorySystem.Nodes
{
    /// <summary>
    /// End point node for a story graph
    /// </summary>
    public class EndNode : StoryNode
    {
        [SerializeField] private string endLabel = "End";
        [SerializeField] private EndType endType = EndType.Complete;

        public string EndLabel { get => endLabel; set => endLabel = value; }
        public EndType EndType { get => endType; set => endType = value; }

        public override string DisplayName => "End";
        public override string Category => "Flow";
        public override Color NodeColor => new Color(0.6f, 0.2f, 0.2f);

        protected override void SetupPorts()
        {
            // End nodes only have input
            AddInputPort("End", "input");
        }

        public override StoryNodeResult Execute(StoryContext context)
        {
            context.MarkComplete();
            return StoryNodeResult.End();
        }

        public override System.Collections.Generic.Dictionary<string, object> GetSerializationData()
        {
            var data = base.GetSerializationData();
            data["endLabel"] = endLabel;
            data["endType"] = endType.ToString();
            return data;
        }

        public override void LoadSerializationData(System.Collections.Generic.Dictionary<string, object> data)
        {
            base.LoadSerializationData(data);
            if (data.TryGetValue("endLabel", out var label)) endLabel = label.ToString();
            if (data.TryGetValue("endType", out var type)) 
                System.Enum.TryParse<EndType>(type.ToString(), out endType);
        }
    }

    public enum EndType
    {
        Complete,       // Story completed successfully
        Failed,         // Story failed
        Checkpoint,     // Save point
        Transition      // Transition to another story
    }
}