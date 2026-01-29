using System;
using System.Collections.Generic;
using UnityEngine;
using StorySystem.Core;

namespace StorySystem.Nodes
{
    /// <summary>
    /// Node for presenting player choices
    /// </summary>
    public class ChoiceNode : StoryNode
    {
        [SerializeField] [TextArea(2, 5)] private string promptText;
        [SerializeField] private List<Choice> choices = new List<Choice>();
        [SerializeField] private float choiceTimeout = 0f;
        [SerializeField] private int defaultChoiceIndex = -1;
        [SerializeField] private bool shuffleChoices = false;

        public string PromptText { get => promptText; set => promptText = value; }
        public List<Choice> Choices => choices;
        public float ChoiceTimeout { get => choiceTimeout; set => choiceTimeout = value; }
        public int DefaultChoiceIndex { get => defaultChoiceIndex; set => defaultChoiceIndex = value; }
        public bool ShuffleChoices { get => shuffleChoices; set => shuffleChoices = value; }

        public override string DisplayName => "Choice";
        public override string Category => "Dialogue";
        public override Color NodeColor => new Color(0.8f, 0.6f, 0.2f);

        protected override void SetupPorts()
        {
            AddInputPort("Input", "input");
            // Output ports are created dynamically based on choices
        }

        public void AddChoice(string text, string conditionVariable = null)
        {
            var choice = new Choice
            {
                choiceId = Guid.NewGuid().ToString(),
                text = text,
                conditionVariable = conditionVariable
            };
            choices.Add(choice);
            
            // Add corresponding output port
            var port = AddOutputPort($"Choice {choices.Count}", choice.choiceId);
        }

        public void RemoveChoice(int index)
        {
            if (index >= 0 && index < choices.Count)
            {
                var choice = choices[index];
                choices.RemoveAt(index);
                outputPorts.RemoveAll(p => p.PortId == choice.choiceId);
            }
        }

        public void UpdateChoicePorts()
        {
            outputPorts.Clear();
            for (int i = 0; i < choices.Count; i++)
            {
                AddOutputPort($"Choice {i + 1}: {choices[i].text}", choices[i].choiceId);
            }
        }

        public override void OnEnter(StoryContext context)
        {
            base.OnEnter(context);
            context.SetTempData("currentChoice", this);
            
            // Filter available choices based on conditions
            var availableChoices = new List<Choice>();
            foreach (var choice in choices)
            {
                if (IsChoiceAvailable(choice, context))
                {
                    availableChoices.Add(choice);
                }
            }
            context.SetTempData("availableChoices", availableChoices);
        }

        private bool IsChoiceAvailable(Choice choice, StoryContext context)
        {
            if (string.IsNullOrEmpty(choice.conditionVariable))
                return true;

            return context.GetVariable<bool>(choice.conditionVariable, true);
        }

        public override StoryNodeResult Execute(StoryContext context)
        {
            // This node waits for player input
            // The UI will call SelectChoice when player makes a choice
            return StoryNodeResult.WaitForInput();
        }

        public string SelectChoice(int choiceIndex, StoryContext context)
        {
            if (choiceIndex < 0 || choiceIndex >= choices.Count)
            {
                Debug.LogError($"Invalid choice index: {choiceIndex}");
                return null;
            }

            var choice = choices[choiceIndex];
            
            // Set the selected choice variable
            if (!string.IsNullOrEmpty(choice.setVariable))
            {
                context.SetVariable(choice.setVariable, choice.setValue);
            }

            // Track choice for analytics/history
            context.SetTempData("lastChoice", choice);
            context.SetVariable($"choice_{NodeId}", choiceIndex);

            return choice.choiceId;
        }

        public override List<string> Validate()
        {
            var errors = base.Validate();
            if (choices.Count == 0)
            {
                errors.Add($"Choice node '{NodeId}' has no choices");
            }
            return errors;
        }

        public override Dictionary<string, object> GetSerializationData()
        {
            var data = base.GetSerializationData();
            data["promptText"] = promptText;
            data["choiceTimeout"] = choiceTimeout;
            data["defaultChoiceIndex"] = defaultChoiceIndex;
            data["shuffleChoices"] = shuffleChoices;
            
            var choicesData = new List<Dictionary<string, object>>();
            foreach (var choice in choices)
            {
                choicesData.Add(new Dictionary<string, object>
                {
                    { "choiceId", choice.choiceId },
                    { "text", choice.text },
                    { "conditionVariable", choice.conditionVariable },
                    { "setVariable", choice.setVariable },
                    { "setValue", choice.setValue }
                });
            }
            data["choices"] = choicesData;
            
            return data;
        }

        public override void LoadSerializationData(Dictionary<string, object> data)
        {
            base.LoadSerializationData(data);
            if (data.TryGetValue("promptText", out var prompt)) promptText = prompt.ToString();
            if (data.TryGetValue("choiceTimeout", out var timeout)) choiceTimeout = Convert.ToSingle(timeout);
            if (data.TryGetValue("defaultChoiceIndex", out var defIndex)) defaultChoiceIndex = Convert.ToInt32(defIndex);
            if (data.TryGetValue("shuffleChoices", out var shuffle)) shuffleChoices = Convert.ToBoolean(shuffle);
            
            if (data.TryGetValue("choices", out var choicesObj) && choicesObj is List<object> choicesList)
            {
                choices.Clear();
                foreach (var choiceObj in choicesList)
                {
                    if (choiceObj is Dictionary<string, object> choiceData)
                    {
                        var choice = new Choice();
                        if (choiceData.TryGetValue("choiceId", out var id)) choice.choiceId = id.ToString();
                        if (choiceData.TryGetValue("text", out var text)) choice.text = text.ToString();
                        if (choiceData.TryGetValue("conditionVariable", out var cond)) choice.conditionVariable = cond.ToString();
                        if (choiceData.TryGetValue("setVariable", out var setVar)) choice.setVariable = setVar.ToString();
                        if (choiceData.TryGetValue("setValue", out var setVal)) choice.setValue = setVal;
                        choices.Add(choice);
                    }
                }
                UpdateChoicePorts();
            }
        }
    }

    [Serializable]
    public class Choice
    {
        public string choiceId;
        public string text;
        public string localizedKey;
        public string conditionVariable;
        public string setVariable;
        public object setValue;
        public Sprite icon;
    }
}