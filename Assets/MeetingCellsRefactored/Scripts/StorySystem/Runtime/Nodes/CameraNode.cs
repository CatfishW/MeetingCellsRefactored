using System;
using System.Collections.Generic;
using UnityEngine;
using StorySystem.Core;
using StorySystem.Execution;

namespace StorySystem.Nodes
{
    /// <summary>
    /// Node for camera operations
    /// </summary>
    public class CameraNode : StoryNode
    {
        [SerializeField] private CameraAction cameraAction = CameraAction.MoveTo;
        [SerializeField] private Vector3 targetPosition;
        [SerializeField] private Vector3 targetRotation;
        [SerializeField] private float fieldOfView = 60f;
        [SerializeField] private Transform targetTransform;
        [SerializeField] private string targetObjectPath;
        [SerializeField] private float duration = 1f;
        [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private bool waitForCompletion = true;
        [SerializeField] private float shakeIntensity = 0.5f;
        [SerializeField] private float shakeDuration = 0.5f;

        public CameraAction CameraAction { get => cameraAction; set => cameraAction = value; }
        public Vector3 TargetPosition { get => targetPosition; set => targetPosition = value; }
        public Vector3 TargetRotation { get => targetRotation; set => targetRotation = value; }
        public float FieldOfView { get => fieldOfView; set => fieldOfView = value; }
        public Transform TargetTransform { get => targetTransform; set => targetTransform = value; }
        public string TargetObjectPath { get => targetObjectPath; set => targetObjectPath = value; }
        public float Duration { get => duration; set => duration = value; }
        public AnimationCurve EaseCurve { get => easeCurve; set => easeCurve = value; }
        public bool WaitForCompletion { get => waitForCompletion; set => waitForCompletion = value; }
        public float ShakeIntensity { get => shakeIntensity; set => shakeIntensity = value; }
        public float ShakeDuration { get => shakeDuration; set => shakeDuration = value; }

        public override string DisplayName => "Camera";
        public override string Category => "Cinematic";
        public override Color NodeColor => new Color(0.8f, 0.6f, 0.8f);

        protected override void SetupPorts()
        {
            AddInputPort("Input", "input");
            AddOutputPort("Output", "output");
        }

        public override StoryNodeResult Execute(StoryContext context)
        {
            var cameraData = new StoryEventData
            {
                eventName = "StoryCamera",
                category = "Camera",
                sourceNodeId = NodeId,
                parameters = new Dictionary<string, object>
                {
                    { "action", cameraAction.ToString() },
                    { "targetPosition", targetPosition },
                    { "targetRotation", targetRotation },
                    { "fieldOfView", fieldOfView },
                    { "targetObjectPath", targetObjectPath },
                    { "duration", duration },
                    { "shakeIntensity", shakeIntensity },
                    { "shakeDuration", shakeDuration }
                }
            };

            StoryManager.Instance?.TriggerEvent(cameraData);

            if (waitForCompletion && duration > 0)
            {
                return StoryNodeResult.Wait(duration, "output");
            }

            return StoryNodeResult.Continue("output");
        }

        public override Dictionary<string, object> GetSerializationData()
        {
            var data = base.GetSerializationData();
            data["cameraAction"] = cameraAction.ToString();
            data["targetPosition"] = new { x = targetPosition.x, y = targetPosition.y, z = targetPosition.z };
            data["targetRotation"] = new { x = targetRotation.x, y = targetRotation.y, z = targetRotation.z };
            data["fieldOfView"] = fieldOfView;
            data["targetObjectPath"] = targetObjectPath;
            data["duration"] = duration;
            data["waitForCompletion"] = waitForCompletion;
            data["shakeIntensity"] = shakeIntensity;
            data["shakeDuration"] = shakeDuration;
            return data;
        }

        public override void LoadSerializationData(Dictionary<string, object> data)
        {
            base.LoadSerializationData(data);
            if (data.TryGetValue("cameraAction", out var action))
                Enum.TryParse<CameraAction>(action.ToString(), out cameraAction);
            if (data.TryGetValue("targetPosition", out var pos) && pos is Dictionary<string, object> posDict)
                targetPosition = new Vector3(
                    Convert.ToSingle(posDict["x"]),
                    Convert.ToSingle(posDict["y"]),
                    Convert.ToSingle(posDict["z"]));
            if (data.TryGetValue("targetRotation", out var rot) && rot is Dictionary<string, object> rotDict)
                targetRotation = new Vector3(
                    Convert.ToSingle(rotDict["x"]),
                    Convert.ToSingle(rotDict["y"]),
                    Convert.ToSingle(rotDict["z"]));
            if (data.TryGetValue("fieldOfView", out var fov)) fieldOfView = Convert.ToSingle(fov);
            if (data.TryGetValue("targetObjectPath", out var path)) targetObjectPath = path.ToString();
            if (data.TryGetValue("duration", out var dur)) duration = Convert.ToSingle(dur);
            if (data.TryGetValue("waitForCompletion", out var wait)) waitForCompletion = Convert.ToBoolean(wait);
            if (data.TryGetValue("shakeIntensity", out var intensity)) shakeIntensity = Convert.ToSingle(intensity);
            if (data.TryGetValue("shakeDuration", out var shakeDur)) shakeDuration = Convert.ToSingle(shakeDur);
        }
    }

    public enum CameraAction
    {
        MoveTo,
        LookAt,
        Follow,
        Zoom,
        Shake,
        Reset,
        FadeIn,
        FadeOut,
        SetActive
    }
}