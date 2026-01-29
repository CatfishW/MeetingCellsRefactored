using UnityEditor.Experimental.GraphView;
using StorySystem.Core;

namespace StorySystem.Editor
{
    public class StoryEdgeView : Edge
    {
        public StoryConnection ConnectionData { get; set; }
    }
}
