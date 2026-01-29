using StorySystem.Nodes;

namespace StorySystem.Execution
{
    /// <summary>
    /// Interface for handling story events
    /// </summary>
    public interface IStoryEventHandler
    {
        /// <summary>
        /// Check if this handler can process the given event
        /// </summary>
        bool CanHandle(StoryEventData eventData);

        /// <summary>
        /// Handle the event
        /// </summary>
        void HandleEvent(StoryEventData eventData);
    }
}