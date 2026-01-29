using System;
using System.Collections.Generic;
using UnityEngine;
using StorySystem.Core;

namespace StorySystem.Nodes
{
    /// <summary>
    /// Node for displaying dialogue
    /// </summary>
    public class DialogueNode : StoryNode
    {
        [SerializeField] private string speakerName;
        [SerializeField] private string speakerId;
        [SerializeField] private Sprite speakerPortrait;
        [SerializeField] private string speakerEmotion = "neutral";
        [SerializeField] [TextArea(3, 10)] private string dialogueText;
        [SerializeField] private string localizedTextKey;
        [SerializeField] private AudioClip voiceClip;
        [SerializeField] private float textSpeed = 0.05f;
        [SerializeField] private bool waitForInput = true;
        [SerializeField] private float autoAdvanceDelay = 0f;
        [SerializeField] private List<DialogueEffect> effects = new List<DialogueEffect>();

        public string SpeakerName { get => speakerName; set => speakerName = value; }
        public string SpeakerId { get => speakerId; set => speakerId = value; }
        public Sprite SpeakerPortrait { get => speakerPortrait; set => speakerPortrait = value; }
        public string SpeakerEmotion { get => speakerEmotion; set => speakerEmotion = value; }
        public string DialogueText { get => dialogueText; set => dialogueText = value; }
        public string LocalizedTextKey { get => localizedTextKey; set => localizedTextKey = value; }
        public AudioClip VoiceClip { get => voiceClip; set => voiceClip = value; }
        public float TextSpeed { get => textSpeed; set => textSpeed = value; }
        public bool WaitForInput { get => waitForInput; set => waitForInput = value; }
        public float AutoAdvanceDelay { get => autoAdvanceDelay; set => autoAdvanceDelay = value; }
        public List<DialogueEffect> Effects => effects;

        public override string DisplayName => "Dialogue";
        public override string Category => "Dialogue";
        public override Color NodeColor => new Color(0.3f, 0.5f, 0.8f);

        protected override void SetupPorts()
        {
            AddInputPort("Input", "input");
            AddOutputPort("Output", "output");
        }

        public override void OnEnter(StoryContext context)
        {
            base.OnEnter(context);
            
            // Store dialogue data in temp for UI to access
            context.SetTempData("currentDialogue", this);
        }

        public override StoryNodeResult Execute(StoryContext context)
        {
            // Process variable substitution in text
            string processedText = ProcessText(dialogueText, context);
            context.SetTempData("processedDialogueText", processedText);

            if (waitForInput)
            {
                return StoryNodeResult.WaitForInput("output");
            }
            else if (autoAdvanceDelay > 0)
            {
                return StoryNodeResult.Wait(autoAdvanceDelay, "output");
            }
            
            return StoryNodeResult.Continue("output");
        }

        private string ProcessText(string text, StoryContext context)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Replace {variableName} with actual values
            string result = text;
            int startIndex = 0;
            
            while ((startIndex = result.IndexOf('{', startIndex)) != -1)
            {
                int endIndex = result.IndexOf('}', startIndex);
                if (endIndex == -1) break;

                string varName = result.Substring(startIndex + 1, endIndex - startIndex - 1);
                object value = context.GetVariable(varName);
                string replacement = value?.ToString() ?? $"[{varName}]";
                
                result = result.Substring(0, startIndex) + replacement + result.Substring(endIndex + 1);
                startIndex += replacement.Length;
            }

            return result;
        }

        public override List<string> Validate()
        {
            var errors = base.Validate();
            if (string.IsNullOrEmpty(dialogueText) && string.IsNullOrEmpty(localizedTextKey))
            {
                errors.Add($"Dialogue node '{NodeId}' has no text content");
            }
            return errors;
        }

        public override Dictionary<string, object> GetSerializationData()
        {
            var data = base.GetSerializationData();
            data["speakerName"] = speakerName;
            data["speakerId"] = speakerId;
            data["speakerEmotion"] = speakerEmotion;
            data["dialogueText"] = dialogueText;
            data["localizedTextKey"] = localizedTextKey;
            data["textSpeed"] = textSpeed;
            data["waitForInput"] = waitForInput;
            data["autoAdvanceDelay"] = autoAdvanceDelay;
            return data;
        }

        public override void LoadSerializationData(Dictionary<string, object> data)
        {
            base.LoadSerializationData(data);
            if (data.TryGetValue("speakerName", out var name)) speakerName = name.ToString();
            if (data.TryGetValue("speakerId", out var id)) speakerId = id.ToString();
            if (data.TryGetValue("speakerEmotion", out var emotion)) speakerEmotion = emotion.ToString();
            if (data.TryGetValue("dialogueText", out var text)) dialogueText = text.ToString();
            if (data.TryGetValue("localizedTextKey", out var key)) localizedTextKey = key.ToString();
            if (data.TryGetValue("textSpeed", out var speed)) textSpeed = Convert.ToSingle(speed);
            if (data.TryGetValue("waitForInput", out var wait)) waitForInput = Convert.ToBoolean(wait);
            if (data.TryGetValue("autoAdvanceDelay", out var delay)) autoAdvanceDelay = Convert.ToSingle(delay);
        }
    }

    [Serializable]
    public class DialogueEffect
    {
        public DialogueEffectType type;
        public float value;
        public string parameter;
    }

    public enum DialogueEffectType
    {
        None,
        ScreenShake,
        TextWobble,
        ColorFlash,
        CameraZoom
    }
}