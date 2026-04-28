// ============================================================
// FILE: AudioManager.cs
// AUTHOR: Long + Claude
// DESCRIPTION: Singleton that manages background music and
//              sound effects for the cafe game.
// ============================================================
using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    
    [Header("Background Music")]
    [SerializeField] private AudioClip bgMusic;
    [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.3f;
    
    [Header("Music (New)")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip morningMusic;
    [SerializeField] private AudioClip lunchRushMusic;
    [SerializeField] private AudioClip afternoonMusic;
    [SerializeField] private AudioClip eveningMusic;
    
    [Header("Sound Effects")]
    [SerializeField] private AudioClip brewStartSFX;
    [SerializeField] private AudioClip brewPerfectSFX;
    [SerializeField] private AudioClip brewGoodSFX;
    [SerializeField] private AudioClip brewBadSFX;
    [SerializeField] private AudioClip coinSFX;
    [SerializeField] private AudioClip customerArriveSFX;
    [SerializeField] private AudioClip customerAngySFX;
    
    [Header("SFX (New)")]
    [SerializeField] private AudioClip buttonClick;
    [SerializeField] private AudioClip customerHappy;
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip failSound;
    [SerializeField] private AudioClip moneySound;
    
    [Header("Volume")]
    [SerializeField] [Range(0f, 1f)] private float sfxVolume = 0.5f;
    [SerializeField] [Range(0,1)] private float masterVolume = 1f;
    
    [Header("Audio Sources")]
    private AudioSource musicSource;
    private AudioSource sfxSource;
    [SerializeField] private AudioSource ambientSource;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Create two AudioSources: one for music, one for SFX
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.volume = 1f;
        
        // Single init path: load saved volumes, then apply them all
        LoadVolume();
        ApplyAllVolumes();
    }
    
    void Start()
    {
        PlayMusic();
    }
    
    // ==================== MUSIC ====================
    
    public void PlayMusic()
    {
        if (bgMusic != null && musicSource != null)
        {
            musicSource.clip = bgMusic;
            musicSource.Play();
        }
    }
    
    private Coroutine fadeCoroutine;

    public void PlayMusic(AudioClip clip, bool fade = true)
    {
        if (!musicSource || !clip) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        if (fade && musicSource.isPlaying)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(CrossfadeTo(clip));
        }
        else
        {
            musicSource.clip = clip;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }
    }

    IEnumerator CrossfadeTo(AudioClip newClip)
    {
        float duration = 0.8f;
        float startVol = musicSource.volume;

        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            musicSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
            yield return null;
        }

        musicSource.clip = newClip;
        musicSource.Play();

        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            musicSource.volume = Mathf.Lerp(0f, musicVolume, t / duration);
            yield return null;
        }

        musicSource.volume = musicVolume;
        fadeCoroutine = null;
    }
    
    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }
    
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        ApplyAllVolumes();
        SaveVolume();
    }
    
    public void PlayMenuMusic() => PlayMusic(menuMusic);
    public void PlayMorningMusic() => PlayMusic(morningMusic);
    public void PlayLunchRushMusic() => PlayMusic(lunchRushMusic);
    public void PlayAfternoonMusic() => PlayMusic(afternoonMusic);
    public void PlayEveningMusic() => PlayMusic(eveningMusic);
    
    // ==================== SOUND EFFECTS ====================
    
    // Original SFX helpers
    public void PlayBrewStart()  => PlaySFX(brewStartSFX);
    public void PlayBrewPerfect() => PlaySFX(brewPerfectSFX);
    public void PlayBrewGood()   => PlaySFX(brewGoodSFX);
    public void PlayBrewBad()    => PlaySFX(brewBadSFX);
    public void PlayCoin()       => PlaySFX(coinSFX);
    public void PlayCustomerArrive() => PlaySFX(customerArriveSFX);
    public void PlayCustomerAngry()  => PlaySFX(customerAngySFX);
    
    // New SFX helpers
    public void PlayButtonClick() => PlaySFX(buttonClick);
    public void PlayCustomerEnter() => PlaySFX(customerArriveSFX);
    public void PlayCustomerHappy() => PlaySFX(customerHappy);
    public void PlaySuccess() => PlaySFX(successSound);
    public void PlayFail() => PlaySFX(failSound);
    public void PlayMoney() => PlaySFX(moneySound);
    
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip, sfxVolume);
    }
    
    // ==================== VOLUME CONTROL ====================
    
    public void SetMasterVolume(float v) { masterVolume = Mathf.Clamp01(v); ApplyAllVolumes(); SaveVolume(); }
    public void SetSFXVolume(float volume) { sfxVolume = Mathf.Clamp01(volume); ApplyAllVolumes(); SaveVolume(); }
    
    public float GetMasterVolume() => masterVolume;
    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume() => sfxVolume;
    
    /// <summary>
    /// Applies all volume levels. Master volume uses AudioListener.volume
    /// (Unity's global volume knob), so per-source volumes stay independent.
    /// </summary>
    void ApplyAllVolumes()
    {
        AudioListener.volume = masterVolume;
        if (musicSource) musicSource.volume = musicVolume;
        if (sfxSource) sfxSource.volume = 1f;  // SFX volume applied per-clip in PlayOneShot
    }
    
    void SaveVolume()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
    }
    
    void LoadVolume()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 0.75f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.3f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.5f);
    }
}
