using UnityEngine;
using UnityEngine.UIElements;

namespace StorySystem.Editor
{
    public static class StoryEditorStyles
    {
        private const string StyleResourcePath = "StoryGraphEditorStyle";

        public static StyleSheet LoadGraphStyle()
        {
            return Resources.Load<StyleSheet>(StyleResourcePath);
        }
    }
}
