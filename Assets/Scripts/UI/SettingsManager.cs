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
        if (masterVolumeSlider != null)
            masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 0.75f);
        if (musicVolumeSlider != null)
            musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.75f);
        if (fullscreenToggle != null)
            fullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
    }
    
    // ==================== AUDIO ====================
    
    public void SetMasterVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
    }
    
    public void SetMusicVolume(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(value);
    }
    
    public void SetSFXVolume(float value)
    {
        PlayerPrefs.SetFloat("SFXVolume", value);
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(value);
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
            int index = resolutionDropdown.value;
            Resolution res = resolutions[index];
            Screen.SetResolution(res.width, res.height, Screen.fullScreen);
            PlayerPrefs.SetInt("ResolutionIndex", index);
        }
        
        PlayerPrefs.Save();
        Debug.Log("[Settings] Settings saved!");
    }
}
