using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using StorySystem.Serialization;

namespace StorySystem.Editor
{
    public class StoryNodeSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private StoryGraphView graphView;
        private Texture2D indentIcon;

        public void Initialize(StoryGraphView view)
        {
            graphView = view;

            if (indentIcon == null)
            {
                indentIcon = new Texture2D(1, 1);
                indentIcon.SetPixel(0, 0, new Color(0, 0, 0, 0));
                indentIcon.Apply();
            }
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var entries = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Node"), 0)
            };

            var categories = NodeFactory.GetNodeTypesByCategories();
            foreach (var category in categories)
            {
                entries.Add(new SearchTreeGroupEntry(new GUIContent(category.Key), 1));
                foreach (var type in category.Value)
                {
                    entries.Add(new SearchTreeEntry(new GUIContent(type.Name, indentIcon))
                    {
                        level = 2,
                        userData = type
                    });
                }
            }

            return entries;
        }

        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            if (graphView == null)
            {
                return false;
            }

            if (entry.userData is Type nodeType)
            {
                Vector2 mousePosition = graphView.GetLocalMousePosition(context.screenMousePosition);
                graphView.CreateNode(nodeType, mousePosition);
                return true;
            }

            return false;
        }
    }
}
