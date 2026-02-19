// ============================================================
// FILE: AudioManager.cs
// AUTHOR: Long + Claude
// DESCRIPTION: Singleton that manages background music and
//              sound effects for the cafe game.
// ============================================================
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    
    [Header("Background Music")]
    [SerializeField] private AudioClip bgMusic;
    [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.3f;
    
    [Header("Sound Effects")]
    [SerializeField] private AudioClip brewStartSFX;
    [SerializeField] private AudioClip brewPerfectSFX;
    [SerializeField] private AudioClip brewGoodSFX;
    [SerializeField] private AudioClip brewBadSFX;
    [SerializeField] private AudioClip coinSFX;
    [SerializeField] private AudioClip customerArriveSFX;
    [SerializeField] private AudioClip customerAngySFX;
    [SerializeField] [Range(0f, 1f)] private float sfxVolume = 0.5f;
    
    private AudioSource musicSource;
    private AudioSource sfxSource;
    
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
        musicSource.volume = musicVolume;
        
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.volume = sfxVolume;
    }
    
    void Start()
    {
        // Load saved volume from PlayerPrefs
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.3f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.5f);
        musicSource.volume = musicVolume;
        
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
    
    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }
    
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null) musicSource.volume = musicVolume;
    }
    
    // ==================== SOUND EFFECTS ====================
    
    public void PlayBrewStart()  => PlaySFX(brewStartSFX);
    public void PlayBrewPerfect() => PlaySFX(brewPerfectSFX);
    public void PlayBrewGood()   => PlaySFX(brewGoodSFX);
    public void PlayBrewBad()    => PlaySFX(brewBadSFX);
    public void PlayCoin()       => PlaySFX(coinSFX);
    public void PlayCustomerArrive() => PlaySFX(customerArriveSFX);
    public void PlayCustomerAngry()  => PlaySFX(customerAngySFX);
    
    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip, sfxVolume);
    }
    
    // ==================== VOLUME CONTROL ====================
    
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }
}
