using System;
using UnityEngine;

namespace StorySystem.Core
{
    /// <summary>
    /// Defines a variable used in the story
    /// </summary>
    [Serializable]
    public class StoryVariable
    {
        [SerializeField] private string variableName;
        [SerializeField] private VariableType variableType;
        [SerializeField] private string stringValue;
        [SerializeField] private float floatValue;
        [SerializeField] private int intValue;
        [SerializeField] private bool boolValue;

        public string Name => variableName;
        public VariableType Type => variableType;

        public StoryVariable(string name, VariableType type)
        {
            variableName = name;
            variableType = type;
        }

        public object GetDefaultValue()
        {
            switch (variableType)
            {
                case VariableType.String: return stringValue ?? "";
                case VariableType.Float: return floatValue;
                case VariableType.Int: return intValue;
                case VariableType.Bool: return boolValue;
                default: return null;
            }
        }

        public void SetDefaultValue(object value)
        {
            switch (variableType)
            {
                case VariableType.String:
                    stringValue = value?.ToString() ?? "";
                    break;
                case VariableType.Float:
                    floatValue = Convert.ToSingle(value);
                    break;
                case VariableType.Int:
                    intValue = Convert.ToInt32(value);
                    break;
                case VariableType.Bool:
                    boolValue = Convert.ToBoolean(value);
                    break;
            }
        }
    }

    public enum VariableType
    {
        String,
        Float,
        Int,
        Bool
    }
}