using UnityEditor;
using UnityEngine.UIElements;

namespace StorySystem.Editor
{
    public static class StoryEditorStyles
    {
        private const string StyleResourceName = "StoryGraphEditorStyle.uss";

        public static StyleSheet LoadGraphStyle()
        {
            return EditorGUIUtility.Load(StyleResourceName) as StyleSheet;
        }
    }
}
