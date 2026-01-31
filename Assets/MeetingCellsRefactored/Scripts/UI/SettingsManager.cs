using UnityEngine;
using UnityEngine.Audio;
using System;

namespace MeetingCellsRefactored.UI
{
    /// <summary>
    /// Manages game settings including audio, graphics, and controls
    /// Persists settings using PlayerPrefs
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        [Header("Audio")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private string masterVolumeParam = "MasterVolume";
        [SerializeField] private string musicVolumeParam = "MusicVolume";
        [SerializeField] private string sfxVolumeParam = "SFXVolume";

        [Header("Default Values")]
        [SerializeField] private float defaultMasterVolume = 1f;
        [SerializeField] private float defaultMusicVolume = 0.8f;
        [SerializeField] private float defaultSfxVolume = 1f;
        [SerializeField] private int defaultQualityLevel = 2;
        [SerializeField] private bool defaultFullscreen = true;
        [SerializeField] private int defaultResolutionIndex = -1;

        [Header("Settings Keys")]
        private const string MASTER_VOLUME_KEY = "Setting_MasterVolume";
        private const string MUSIC_VOLUME_KEY = "Setting_MusicVolume";
        private const string SFX_VOLUME_KEY = "Setting_SFXVolume";
        private const string QUALITY_LEVEL_KEY = "Setting_QualityLevel";
        private const string FULLSCREEN_KEY = "Setting_Fullscreen";
        private const string RESOLUTION_INDEX_KEY = "Setting_ResolutionIndex";
        private const string CAMERA_SENSITIVITY_KEY = "Setting_CameraSensitivity";
        private const string INVERT_Y_KEY = "Setting_InvertY";
        private const string VSYNC_KEY = "Setting_VSync";
        private const string FRAME_RATE_KEY = "Setting_FrameRate";

        // Events
        public event Action<float> OnMasterVolumeChanged;
        public event Action<float> OnMusicVolumeChanged;
        public event Action<float> OnSfxVolumeChanged;
        public event Action<int> OnQualityLevelChanged;
        public event Action<bool> OnFullscreenChanged;

        // Current settings
        private float masterVolume;
        private float musicVolume;
        private float sfxVolume;
        private float cameraSensitivity = 1f;
        private bool invertY = false;
        private bool vsync = true;
        private int targetFrameRate = 60;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            LoadAllSettings();
            ApplyAllSettings();
        }

        #region Load/Save

        public void LoadAllSettings()
        {
            masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, defaultMasterVolume);
            musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, defaultMusicVolume);
            sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, defaultSfxVolume);
            cameraSensitivity = PlayerPrefs.GetFloat(CAMERA_SENSITIVITY_KEY, 1f);
            invertY = PlayerPrefs.GetInt(INVERT_Y_KEY, 0) == 1;
            vsync = PlayerPrefs.GetInt(VSYNC_KEY, 1) == 1;
            targetFrameRate = PlayerPrefs.GetInt(FRAME_RATE_KEY, 60);
        }

        public void SaveAllSettings()
        {
            PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, masterVolume);
            PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, musicVolume);
            PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxVolume);
            PlayerPrefs.SetFloat(CAMERA_SENSITIVITY_KEY, cameraSensitivity);
            PlayerPrefs.SetInt(INVERT_Y_KEY, invertY ? 1 : 0);
            PlayerPrefs.SetInt(VSYNC_KEY, vsync ? 1 : 0);
            PlayerPrefs.SetInt(FRAME_RATE_KEY, targetFrameRate);
            PlayerPrefs.Save();
        }

        public void ApplyAllSettings()
        {
            SetMasterVolume(masterVolume, false);
            SetMusicVolume(musicVolume, false);
            SetSfxVolume(sfxVolume, false);
            ApplyGraphicsSettings();
        }

        #endregion

        #region Audio Settings

        public void SetMasterVolume(float volume, bool save = true)
        {
            masterVolume = Mathf.Clamp01(volume);
            float db = LinearToDecibel(masterVolume);
            audioMixer?.SetFloat(masterVolumeParam, db);
            OnMasterVolumeChanged?.Invoke(masterVolume);
            if (save) SaveAllSettings();
        }

        public void SetMusicVolume(float volume, bool save = true)
        {
            musicVolume = Mathf.Clamp01(volume);
            float db = LinearToDecibel(musicVolume);
            audioMixer?.SetFloat(musicVolumeParam, db);
            OnMusicVolumeChanged?.Invoke(musicVolume);
            if (save) SaveAllSettings();
        }

        public void SetSfxVolume(float volume, bool save = true)
        {
            sfxVolume = Mathf.Clamp01(volume);
            float db = LinearToDecibel(sfxVolume);
            audioMixer?.SetFloat(sfxVolumeParam, db);
            OnSfxVolumeChanged?.Invoke(sfxVolume);
            if (save) SaveAllSettings();
        }

        private float LinearToDecibel(float linear)
        {
            if (linear <= 0) return -80f;
            return 20f * Mathf.Log10(linear);
        }

        #endregion

        #region Graphics Settings

        public void SetQualityLevel(int level, bool save = true)
        {
            level = Mathf.Clamp(level, 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(level, true);
            OnQualityLevelChanged?.Invoke(level);
            if (save)
            {
                PlayerPrefs.SetInt(QUALITY_LEVEL_KEY, level);
                PlayerPrefs.Save();
            }
        }

        public void SetFullscreen(bool fullscreen, bool save = true)
        {
            Screen.fullScreen = fullscreen;
            OnFullscreenChanged?.Invoke(fullscreen);
            if (save)
            {
                PlayerPrefs.SetInt(FULLSCREEN_KEY, fullscreen ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public void SetResolution(int width, int height, bool fullscreen, bool save = true)
        {
            Screen.SetResolution(width, height, fullscreen);
            if (save)
            {
                // Find resolution index
                Resolution[] resolutions = Screen.resolutions;
                for (int i = 0; i < resolutions.Length; i++)
                {
                    if (resolutions[i].width == width && resolutions[i].height == height)
                    {
                        PlayerPrefs.SetInt(RESOLUTION_INDEX_KEY, i);
                        PlayerPrefs.Save();
                        break;
                    }
                }
            }
        }

        public void SetVSync(bool enabled, bool save = true)
        {
            vsync = enabled;
            QualitySettings.vSyncCount = enabled ? 1 : 0;
            if (save)
            {
                PlayerPrefs.SetInt(VSYNC_KEY, enabled ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public void SetTargetFrameRate(int fps, bool save = true)
        {
            targetFrameRate = fps;
            Application.targetFrameRate = fps;
            if (save)
            {
                PlayerPrefs.SetInt(FRAME_RATE_KEY, fps);
                PlayerPrefs.Save();
            }
        }

        private void ApplyGraphicsSettings()
        {
            int qualityLevel = PlayerPrefs.GetInt(QUALITY_LEVEL_KEY, defaultQualityLevel);
            bool fullscreen = PlayerPrefs.GetInt(FULLSCREEN_KEY, defaultFullscreen ? 1 : 0) == 1;

            QualitySettings.SetQualityLevel(qualityLevel, true);
            Screen.fullScreen = fullscreen;

            // Apply VSync
            QualitySettings.vSyncCount = vsync ? 1 : 0;
            Application.targetFrameRate = targetFrameRate;
        }

        #endregion

        #region Control Settings

        public void SetCameraSensitivity(float sensitivity, bool save = true)
        {
            cameraSensitivity = Mathf.Clamp(sensitivity, 0.1f, 3f);
            if (save) SaveAllSettings();
        }

        public void SetInvertY(bool invert, bool save = true)
        {
            invertY = invert;
            if (save) SaveAllSettings();
        }

        #endregion

        #region Reset

        public void ResetToDefaults()
        {
            SetMasterVolume(defaultMasterVolume, false);
            SetMusicVolume(defaultMusicVolume, false);
            SetSfxVolume(defaultSfxVolume, false);
            SetQualityLevel(defaultQualityLevel, false);
            SetFullscreen(defaultFullscreen, false);
            SetCameraSensitivity(1f, false);
            SetInvertY(false, false);
            SetVSync(true, false);
            SetTargetFrameRate(60, false);

            SaveAllSettings();
        }

        #endregion

        #region Properties

        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float SfxVolume => sfxVolume;
        public float CameraSensitivity => cameraSensitivity;
        public bool InvertY => invertY;
        public bool VSync => vsync;
        public int TargetFrameRate => targetFrameRate;

        public int CurrentQualityLevel => QualitySettings.GetQualityLevel();
        public bool IsFullscreen => Screen.fullScreen;

        #endregion
    }
}
