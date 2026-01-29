using System;
using System.Collections.Generic;
using UnityEngine;
using StorySystem.Core;

namespace StorySystem.Nodes
{
    /// <summary>
    /// Node for playing audio (music, SFX, ambient)
    /// </summary>
    public class AudioNode : StoryNode
    {
        [SerializeField] private AudioClip audioClip;
        [SerializeField] private string audioClipPath;
        [SerializeField] private AudioNodeType audioType = AudioNodeType.SFX;
        [SerializeField] private AudioAction action = AudioAction.Play;
        [SerializeField] private float volume = 1f;
        [SerializeField] private float fadeTime = 0f;
        [SerializeField] private bool loop = false;
        [SerializeField] private bool waitForCompletion = false;
        [SerializeField] private string audioChannel = "default";

        public AudioClip AudioClip { get => audioClip; set => audioClip = value; }
        public string AudioClipPath { get => audioClipPath; set => audioClipPath = value; }
        public AudioNodeType AudioType { get => audioType; set => audioType = value; }
        public AudioAction Action { get => action; set => action = value; }
        public float Volume { get => volume; set => volume = value; }
        public float FadeTime { get => fadeTime; set => fadeTime = value; }
        public bool Loop { get => loop; set => loop = value; }
        public bool WaitForCompletion { get => waitForCompletion; set => waitForCompletion = value; }
        public string AudioChannel { get => audioChannel; set => audioChannel = value; }

        public override string DisplayName => "Audio";
        public override string Category => "Audio";
        public override Color NodeColor => new Color(0.2f, 0.7f, 0.8f);

        public override StoryNodeResult Execute(StoryContext context)
        {
            // Audio playback is handled through events
            var audioData = new StoryEventData
            {
                eventName = "StoryAudio",
                category = "Audio",
                sourceNodeId = NodeId,
                parameters = new Dictionary<string, object>
                {
                    { "clipPath", audioClipPath },
                    { "audioType", audioType.ToString() },
                    { "action", action.ToString() },
                    { "volume", volume },
                    { "fadeTime", fadeTime },
                    { "loop", loop },
                    { "channel", audioChannel }
                }
            };

            if (audioClip != null)
            {
                audioData.parameters["clip"] = audioClip;
            }

            StoryManager.Instance?.TriggerEvent(audioData);

            if (waitForCompletion && audioClip != null && action == AudioAction.Play)
            {
                return StoryNodeResult.Wait(audioClip.length, "output");
            }

            return StoryNodeResult.Continue("output");
        }

        public override Dictionary<string, object> GetSerializationData()
        {
            var data = base.GetSerializationData();
            data["audioClipPath"] = audioClipPath;
            data["audioType"] = audioType.ToString();
            data["action"] = action.ToString();
            data["volume"] = volume;
            data["fadeTime"] = fadeTime;
            data["loop"] = loop;
            data["waitForCompletion"] = waitForCompletion;
            data["audioChannel"] = audioChannel;
            return data;
        }

        public override void LoadSerializationData(Dictionary<string, object> data)
        {
            base.LoadSerializationData(data);
            if (data.TryGetValue("audioClipPath", out var path)) audioClipPath = path.ToString();
            if (data.TryGetValue("audioType", out var type))
                Enum.TryParse<AudioNodeType>(type.ToString(), out audioType);
            if (data.TryGetValue("action", out var act))
                Enum.TryParse<AudioAction>(act.ToString(), out action);
            if (data.TryGetValue("volume", out var vol)) volume = Convert.ToSingle(vol);
            if (data.TryGetValue("fadeTime", out var fade)) fadeTime = Convert.ToSingle(fade);
            if (data.TryGetValue("loop", out var lp)) loop = Convert.ToBoolean(lp);
            if (data.TryGetValue("waitForCompletion", out var wait)) waitForCompletion = Convert.ToBoolean(wait);
            if (data.TryGetValue("audioChannel", out var channel)) audioChannel = channel.ToString();
        }
    }

    public enum AudioNodeType
    {
        SFX,
        Music,
        Ambient,
        Voice
    }

    public enum AudioAction
    {
        Play,
        Stop,
        Pause,
        Resume,
        FadeIn,
        FadeOut,
        CrossFade
    }
}