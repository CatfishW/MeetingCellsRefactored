using UnityEngine;
using StorySystem.Execution;
using StorySystem.Nodes;

namespace StorySystem.Runtime.Examples
{
    /// <summary>
    /// Example event handler that responds to story events
    /// Attach to the same GameObject as StoryManager or in the scene
    /// </summary>
    public class StoryEventHandlerExample : MonoBehaviour, IStoryEventHandler
    {
        [Header("Camera")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private float cameraTransitionSpeed = 2f;

        [Header("Audio")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Effects")]
        [SerializeField] private ParticleSystem effectPrefab;

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            // Register with StoryManager
            if (StoryManager.Instance != null)
            {
                StoryManager.Instance.RegisterEventHandler(this);
            }
        }

        private void OnDestroy()
        {
            if (StoryManager.Instance != null)
            {
                StoryManager.Instance.UnregisterEventHandler(this);
            }
        }

        public bool CanHandle(StoryEventData eventData)
        {
            // Handle camera and audio events
            return eventData.category == "Camera" ||
                   eventData.category == "Audio" ||
                   eventData.category == "Effects";
        }

        public void HandleEvent(StoryEventData eventData)
        {
            switch (eventData.category)
            {
                case "Camera":
                    HandleCameraEvent(eventData);
                    break;
                case "Audio":
                    HandleAudioEvent(eventData);
                    break;
                case "Effects":
                    HandleEffectEvent(eventData);
                    break;
            }
        }

        private void HandleCameraEvent(StoryEventData eventData)
        {
            if (eventData.parameters.TryGetValue("action", out var action))
            {
                string actionStr = action.ToString();

                switch (actionStr)
                {
                    case "MoveTo":
                        if (eventData.parameters.TryGetValue("targetPosition", out var pos))
                        {
                            Debug.Log($"[StoryEvent] Moving camera to: {pos}");
                            // Implement camera movement here
                        }
                        break;

                    case "Shake":
                        if (eventData.parameters.TryGetValue("shakeIntensity", out var intensity) &&
                            eventData.parameters.TryGetValue("shakeDuration", out var duration))
                        {
                            Debug.Log($"[StoryEvent] Shaking camera: intensity={intensity}, duration={duration}");
                            // Implement camera shake here
                        }
                        break;

                    case "FadeIn":
                        Debug.Log("[StoryEvent] Fading camera in");
                        break;

                    case "FadeOut":
                        Debug.Log("[StoryEvent] Fading camera out");
                        break;
                }
            }
        }

        private void HandleAudioEvent(StoryEventData eventData)
        {
            if (eventData.parameters.TryGetValue("action", out var action))
            {
                string actionStr = action.ToString();

                switch (actionStr)
                {
                    case "Play":
                        if (eventData.parameters.TryGetValue("clipPath", out var clipPath))
                        {
                            Debug.Log($"[StoryEvent] Playing audio: {clipPath}");
                            // Load and play audio clip
                        }
                        break;

                    case "Stop":
                        Debug.Log("[StoryEvent] Stopping audio");
                        if (musicSource != null) musicSource.Stop();
                        break;

                    case "FadeIn":
                        Debug.Log("[StoryEvent] Fading audio in");
                        break;

                    case "FadeOut":
                        Debug.Log("[StoryEvent] Fading audio out");
                        break;
                }
            }
        }

        private void HandleEffectEvent(StoryEventData eventData)
        {
            if (eventData.parameters.TryGetValue("effectName", out var effectName))
            {
                Debug.Log($"[StoryEvent] Playing effect: {effectName}");

                if (effectPrefab != null)
                {
                    Instantiate(effectPrefab, Vector3.zero, Quaternion.identity);
                }
            }
        }
    }
}
