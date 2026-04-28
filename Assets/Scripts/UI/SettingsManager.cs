// ============================================================
// FILE: SettingsManager.cs
// AUTHOR: Long + Claude
// DESCRIPTION: Manages game settings (audio, display)
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SettingsManager : MonoBehaviour
{
    [Header("Audio Sliders")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    
    [Header("Display")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    
    [Header("Buttons")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button backButton;
    
    private Resolution[] resolutions;
    
    void Start()
    {
        InitializeResolutions();
        LoadSettings();
        
        // Setup listeners
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        if (applyButton != null)
            applyButton.onClick.AddListener(ApplySettings);
    }
    
    // ==================== INITIALIZATION ====================
    
    void InitializeResolutions()
    {
        if (resolutionDropdown == null) return;
        
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        
        List<string> options = new List<string>();
        int currentIndex = 0;
        
        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = $"{resolutions[i].width} x {resolutions[i].height}";
            options.Add(option);
            
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentIndex = i;
            }
        }
        
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = PlayerPrefs.GetInt("ResolutionIndex", currentIndex);
        resolutionDropdown.RefreshShownValue();
    }
    
    void LoadSettings()
    {
        float master = Mathf.Clamp01(PlayerPrefs.GetFloat("MasterVolume", 0.75f));
        float music = Mathf.Clamp01(PlayerPrefs.GetFloat("MusicVolume", 0.3f));
        float sfx = Mathf.Clamp01(PlayerPrefs.GetFloat("SFXVolume", 0.5f));

        if (masterVolumeSlider != null)
            masterVolumeSlider.value = master;
        if (musicVolumeSlider != null)
            musicVolumeSlider.value = music;
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = sfx;
        if (fullscreenToggle != null)
            fullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", 1) == 1;

        // Apply loaded values through AudioManager (the single volume authority)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(master);
            AudioManager.Instance.SetMusicVolume(music);
            AudioManager.Instance.SetSFXVolume(sfx);
        }
    }
    
    // ==================== AUDIO ====================
    
    public void SetMasterVolume(float value)
    {
        float v = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("MasterVolume", v);
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMasterVolume(v);
    }
    
    public void SetMusicVolume(float value)
    {
        float v = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("MusicVolume", v);
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(v);
    }
    
    public void SetSFXVolume(float value)
    {
        float v = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("SFXVolume", v);
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(v);
    }
    
    // ==================== DISPLAY ====================
    
    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
    }
    
    public void ApplySettings()
    {
        // Apply resolution
        if (resolutionDropdown != null && resolutions != null && resolutions.Length > 0)
        {
            int index = Mathf.Clamp(resolutionDropdown.value, 0, resolutions.Length - 1);
            Resolution res = resolutions[index];
            Screen.SetResolution(res.width, res.height, Screen.fullScreen);
            PlayerPrefs.SetInt("ResolutionIndex", index);
        }
        
        PlayerPrefs.Save();
        Debug.Log("[Settings] Settings saved!");
    }
}
