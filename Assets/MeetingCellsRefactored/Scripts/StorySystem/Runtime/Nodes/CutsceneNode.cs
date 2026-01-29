using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using StorySystem.Core;

namespace StorySystem.Nodes
{
    /// <summary>
    /// Node for playing cutscenes using Timeline or custom animations
    /// </summary>
    public class CutsceneNode : StoryNode
    {
        [SerializeField] private string cutsceneName;
        [SerializeField] private PlayableAsset timelineAsset;
        [SerializeField] private string cutsceneId;
        [SerializeField] private CutsceneType cutsceneType = CutsceneType.Timeline;
        [SerializeField] private bool skippable = true;
        [SerializeField] private float skipHoldTime = 1f;
        [SerializeField] private bool pauseGameplay = true;
        [SerializeField] private bool hideUI = true;
        [SerializeField] private List<CutsceneBinding> bindings = new List<CutsceneBinding>();

        public string CutsceneName { get => cutsceneName; set => cutsceneName = value; }
        public PlayableAsset TimelineAsset { get => timelineAsset; set => timelineAsset = value; }
        public string CutsceneId { get => cutsceneId; set => cutsceneId = value; }
        public CutsceneType CutsceneType { get => cutsceneType; set => cutsceneType = value; }
        public bool Skippable { get => skippable; set => skippable = value; }
        public float SkipHoldTime { get => skipHoldTime; set => skipHoldTime = value; }
        public bool PauseGameplay { get => pauseGameplay; set => pauseGameplay = value; }
        public bool HideUI { get => hideUI; set => hideUI = value; }
        public List<CutsceneBinding> Bindings => bindings;

        public override string DisplayName => "Cutscene";
        public override string Category => "Cinematic";
        public override Color NodeColor => new Color(0.9f, 0.5f, 0.1f);

        protected override void SetupPorts()
        {
            AddInputPort("Input", "input");
            AddOutputPort("Complete", "complete");
            AddOutputPort("Skipped", "skipped");
        }

        public override void OnEnter(StoryContext context)
        {
            base.OnEnter(context);
            context.SetTempData("currentCutscene", this);
            
            if (pauseGameplay)
            {
                Time.timeScale = 0f;
            }
        }

        public override StoryNodeResult Execute(StoryContext context)
        {
            // Cutscene playback is handled by CutsceneUI/CutsceneManager
            // This node waits for completion
            return StoryNodeResult.WaitForCondition(() =>
            {
                var cutsceneComplete = context.GetTempData<bool>("cutsceneComplete", false);
                return cutsceneComplete;
            }, "complete");
        }

        public override void OnExit(StoryContext context)
        {
            base.OnExit(context);
            
            if (pauseGameplay)
            {
                Time.timeScale = 1f;
            }
            
            context.SetTempData("cutsceneComplete", false);
        }

        public void OnCutsceneComplete(StoryContext context, bool wasSkipped)
        {
            context.SetTempData("cutsceneComplete", true);
            context.SetTempData("cutsceneSkipped", wasSkipped);
        }

        public override Dictionary<string, object> GetSerializationData()
        {
            var data = base.GetSerializationData();
            data["cutsceneName"] = cutsceneName;
            data["cutsceneId"] = cutsceneId;
            data["cutsceneType"] = cutsceneType.ToString();
            data["skippable"] = skippable;
            data["skipHoldTime"] = skipHoldTime;
            data["pauseGameplay"] = pauseGameplay;
            data["hideUI"] = hideUI;
            return data;
        }

        public override void LoadSerializationData(Dictionary<string, object> data)
        {
            base.LoadSerializationData(data);
            if (data.TryGetValue("cutsceneName", out var name)) cutsceneName = name.ToString();
            if (data.TryGetValue("cutsceneId", out var id)) cutsceneId = id.ToString();
            if (data.TryGetValue("cutsceneType", out var type))
                Enum.TryParse<CutsceneType>(type.ToString(), out cutsceneType);
            if (data.TryGetValue("skippable", out var skip)) skippable = Convert.ToBoolean(skip);
            if (data.TryGetValue("skipHoldTime", out var holdTime)) skipHoldTime = Convert.ToSingle(holdTime);
            if (data.TryGetValue("pauseGameplay", out var pause)) pauseGameplay = Convert.ToBoolean(pause);
            if (data.TryGetValue("hideUI", out var hide)) hideUI = Convert.ToBoolean(hide);
        }
    }

    [Serializable]
    public class CutsceneBinding
    {
        public string trackName;
        public string objectPath;
        public UnityEngine.Object boundObject;
    }

    public enum CutsceneType
    {
        Timeline,
        Animation,
        Video,
        Custom
    }
}