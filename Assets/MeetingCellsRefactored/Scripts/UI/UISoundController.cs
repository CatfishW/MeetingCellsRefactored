using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace MeetingCellsRefactored.UI
{
    /// <summary>
    /// Handles UI sound effects for buttons, hovers, and transitions
    /// Provides audio feedback for all UI interactions
    /// </summary>
    public class UISoundController : MonoBehaviour
    {
        public static UISoundController Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource uiAudioSource;
        [SerializeField] private AudioSource musicAudioSource;

        [Header("UI Sound Clips")]
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip buttonHoverSound;
        [SerializeField] private AudioClip panelOpenSound;
        [SerializeField] private AudioClip panelCloseSound;
        [SerializeField] private AudioClip toggleOnSound;
        [SerializeField] private AudioClip toggleOffSound;
        [SerializeField] private AudioClip sliderChangeSound;
        [SerializeField] private AudioClip errorSound;
        [SerializeField] private AudioClip successSound;

        [Header("Music Clips")]
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private AudioClip gameplayMusic;

        [Header("Volume Settings")]
        [SerializeField] private float uiVolume = 1f;
        [SerializeField] private float musicVolume = 0.5f;
        [SerializeField] private bool enableUISounds = true;
        [SerializeField] private bool enableMusic = true;

        private Dictionary<Button, AudioClip> buttonClickOverrides = new Dictionary<Button, AudioClip>();
        private float lastSliderSoundTime;
        private const float SLIDER_SOUND_COOLDOWN = 0.05f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Create audio sources if not assigned
            if (uiAudioSource == null)
            {
                uiAudioSource = gameObject.AddComponent<AudioSource>();
                uiAudioSource.playOnAwake = false;
            }
            if (musicAudioSource == null)
            {
                musicAudioSource = gameObject.AddComponent<AudioSource>();
                musicAudioSource.playOnAwake = false;
                musicAudioSource.loop = true;
            }
        }

        private void Start()
        {
            // Subscribe to settings changes
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.OnMasterVolumeChanged += OnMasterVolumeChanged;
                SettingsManager.Instance.OnSfxVolumeChanged += OnSfxVolumeChanged;
                SettingsManager.Instance.OnMusicVolumeChanged += OnMusicVolumeChanged;
            }

            UpdateVolumes();
        }

        private void OnDestroy()
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.OnMasterVolumeChanged -= OnMasterVolumeChanged;
                SettingsManager.Instance.OnSfxVolumeChanged -= OnSfxVolumeChanged;
                SettingsManager.Instance.OnMusicVolumeChanged -= OnMusicVolumeChanged;
            }
        }

        #region Sound Playback

        public void PlayButtonClick()
        {
            if (!enableUISounds || buttonClickSound == null) return;
            PlayUISound(buttonClickSound);
        }

        public void PlayButtonHover()
        {
            if (!enableUISounds || buttonHoverSound == null) return;
            PlayUISound(buttonHoverSound, 0.5f);
        }

        public void PlayPanelOpen()
        {
            if (!enableUISounds || panelOpenSound == null) return;
            PlayUISound(panelOpenSound);
        }

        public void PlayPanelClose()
        {
            if (!enableUISounds || panelCloseSound == null) return;
            PlayUISound(panelCloseSound);
        }

        public void PlayToggleOn()
        {
            if (!enableUISounds || toggleOnSound == null) return;
            PlayUISound(toggleOnSound);
        }

        public void PlayToggleOff()
        {
            if (!enableUISounds || toggleOffSound == null) return;
            PlayUISound(toggleOffSound);
        }

        public void PlaySliderChange()
        {
            if (!enableUISounds || sliderChangeSound == null) return;

            // Cooldown to prevent spam
            if (Time.time - lastSliderSoundTime < SLIDER_SOUND_COOLDOWN) return;
            lastSliderSoundTime = Time.time;

            PlayUISound(sliderChangeSound, 0.3f);
        }

        public void PlayError()
        {
            if (!enableUISounds || errorSound == null) return;
            PlayUISound(errorSound);
        }

        public void PlaySuccess()
        {
            if (!enableUISounds || successSound == null) return;
            PlayUISound(successSound);
        }

        private void PlayUISound(AudioClip clip, float volumeMultiplier = 1f)
        {
            if (clip == null || uiAudioSource == null) return;
            uiAudioSource.PlayOneShot(clip, uiVolume * volumeMultiplier);
        }

        #endregion

        #region Music Control

        public void PlayMenuMusic()
        {
            if (!enableMusic || menuMusic == null) return;
            PlayMusic(menuMusic);
        }

        public void PlayGameplayMusic()
        {
            if (!enableMusic || gameplayMusic == null) return;
            PlayMusic(gameplayMusic);
        }

        private void PlayMusic(AudioClip clip)
        {
            if (musicAudioSource == null) return;

            if (musicAudioSource.clip == clip && musicAudioSource.isPlaying)
                return;

            musicAudioSource.clip = clip;
            musicAudioSource.volume = musicVolume;
            musicAudioSource.Play();
        }

        public void StopMusic()
        {
            if (musicAudioSource != null)
                musicAudioSource.Stop();
        }

        public void PauseMusic()
        {
            if (musicAudioSource != null)
                musicAudioSource.Pause();
        }

        public void ResumeMusic()
        {
            if (musicAudioSource != null && !musicAudioSource.isPlaying)
                musicAudioSource.Play();
        }

        #endregion

        #region Volume Updates

        private void OnMasterVolumeChanged(float volume)
        {
            UpdateVolumes();
        }

        private void OnSfxVolumeChanged(float volume)
        {
            uiVolume = volume;
            UpdateVolumes();
        }

        private void OnMusicVolumeChanged(float volume)
        {
            musicVolume = volume;
            UpdateVolumes();
        }

        private void UpdateVolumes()
        {
            if (musicAudioSource != null)
                musicAudioSource.volume = musicVolume;
        }

        #endregion

        #region Button Setup

        /// <summary>
        /// Automatically adds sound events to all buttons in the scene
        /// Call this after instantiating UI panels
        /// </summary>
        public void SetupButtonSounds(Transform parent)
        {
            var buttons = parent.GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                SetupButtonSound(button);
            }

            var toggles = parent.GetComponentsInChildren<Toggle>(true);
            foreach (var toggle in toggles)
            {
                SetupToggleSound(toggle);
            }

            var sliders = parent.GetComponentsInChildren<Slider>(true);
            foreach (var slider in sliders)
            {
                SetupSliderSound(slider);
            }
        }

        public void SetupButtonSound(Button button)
        {
            if (button == null) return;

            // Add trigger component if not present
            var trigger = button.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = button.gameObject.AddComponent<EventTrigger>();

            // Clear existing triggers to avoid duplicates
            trigger.triggers.RemoveAll(t => t.eventID == EventTriggerType.PointerEnter || 
                                            t.eventID == EventTriggerType.PointerClick);

            // Add hover sound
            var hoverEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerEnter
            };
            hoverEntry.callback.AddListener((data) => PlayButtonHover());
            trigger.triggers.Add(hoverEntry);

            // Add click sound
            button.onClick.AddListener(PlayButtonClick);
        }

        public void SetupToggleSound(Toggle toggle)
        {
            if (toggle == null) return;

            toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn) PlayToggleOn();
                else PlayToggleOff();
            });
        }

        public void SetupSliderSound(Slider slider)
        {
            if (slider == null) return;

            slider.onValueChanged.AddListener((value) => PlaySliderChange());
        }

        #endregion

        #region Properties

        public bool EnableUISounds
        {
            get => enableUISounds;
            set => enableUISounds = value;
        }

        public bool EnableMusic
        {
            get => enableMusic;
            set => enableMusic = value;
        }

        #endregion
    }
}
