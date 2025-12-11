using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý âm thanh cho match combo và background music
/// </summary>
public class MatchSoundManager : MonoBehaviour
{
    public static MatchSoundManager Instance { get; private set; }

    [Header("🎵 Match Combo Sounds (Kéo AudioClip vào đây)")]
    [Tooltip("🔊 Sound cho match đầu tiên")]
    public AudioClip matchSound1;
    
    [Tooltip("🔊🔊 Sound cho match lần 2 (combo!)")]
    public AudioClip matchSound2;
    
    [Tooltip("🔊🔊🔊 Sound cho match lần 3 (combo x2!)")]
    public AudioClip matchSound3;
    
    [Tooltip("🔊🔊🔊🔊 Sound cho match lần 4 (combo x3!)")]
    public AudioClip matchSound4;
    
    [Tooltip("🔊🔊🔊🔊🔊 Sound cho match lần 5 (combo x4!)")]
    public AudioClip matchSound5;
    
    [Tooltip("💥 Sound cho match lần 6+ (MAX COMBO!)")]
    public AudioClip matchSound6;

    [Space(20)]
    [Header("🎼 Background Music (Nhạc nền loop)")]
    [Tooltip("Kéo file nhạc nền vào đây")]
    public AudioClip backgroundMusic;

    [Space(20)]
    [Header("🔊 Volume Settings")]
    [Range(0f, 1f)]
    [Tooltip("Âm lượng sound effects (0 = tắt, 1 = max)")]
    public float sfxVolume = 0.8f;
    
    [Range(0f, 1f)]
    [Tooltip("Âm lượng background music (0 = tắt, 1 = max)")]
    public float musicVolume = 0.5f;

    [Space(10)]
    [Header("⚙️ Auto Setup (Tự động tạo AudioSource)")]
    [Tooltip("Bỏ trống - sẽ tự động tạo khi chạy game")]
    public AudioSource sfxSource;
    
    [Tooltip("Bỏ trống - sẽ tự động tạo khi chạy game")]
    public AudioSource musicSource;

    private int currentComboCount = 0;
    private AudioClip[] matchSounds;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // ✅ TỰ ĐỘNG TẠO AudioSource nếu chưa có
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            Debug.Log("[Sound] ✓ Auto-created SFX AudioSource");
        }

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            Debug.Log("[Sound] ✓ Auto-created Music AudioSource");
        }

        // Khởi tạo mảng sounds
        matchSounds = new AudioClip[] 
        { 
            matchSound1, 
            matchSound2, 
            matchSound3, 
            matchSound4, 
            matchSound5, 
            matchSound6 
        };

        // Set volumes
        sfxSource.volume = sfxVolume;
        musicSource.volume = musicVolume;
        
        // ✅ KIỂM TRA VÀ CẢNH BÁO NẾU THIẾU SOUND
        ValidateSounds();
    }

    /// <summary>
    /// Kiểm tra xem đã gán đủ sounds chưa
    /// </summary>
    void ValidateSounds()
    {
        int missingSounds = 0;
        
        for (int i = 0; i < matchSounds.Length; i++)
        {
            if (matchSounds[i] == null)
            {
                Debug.LogWarning($"[Sound] ⚠ Match Sound {i + 1} chưa được gán!");
                missingSounds++;
            }
        }
        
        if (backgroundMusic == null)
        {
            Debug.LogWarning("[Sound] ⚠ Background Music chưa được gán!");
        }
        
        if (missingSounds == 0 && backgroundMusic != null)
        {
            Debug.Log("[Sound] ✓ Tất cả sounds đã được gán đầy đủ!");
        }
    }

    void Start()
    {
        PlayBackgroundMusic();
    }

    /// <summary>
    /// Phát background music
    /// </summary>
    public void PlayBackgroundMusic()
    {
        if (backgroundMusic != null && musicSource != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }

    /// <summary>
    /// Dừng background music
    /// </summary>
    public void StopBackgroundMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    /// <summary>
    /// Phát sound cho match với combo count
    /// </summary>
    /// <param name="comboCount">Số lần match liên tiếp (1-based)</param>
    public void PlayMatchSound(int comboCount)
    {
        // Clamp combo count từ 1-6
        int soundIndex = Mathf.Clamp(comboCount, 1, 6) - 1;
        
        AudioClip soundToPlay = matchSounds[soundIndex];
        
        if (soundToPlay != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(soundToPlay);
            Debug.Log($"[SOUND] Playing match sound {comboCount}: {soundToPlay.name}");
        }
        else
        {
            Debug.LogWarning($"[SOUND] Match sound {comboCount} is missing!");
        }
    }

    /// <summary>
    /// Reset combo count về 0
    /// </summary>
    public void ResetCombo()
    {
        currentComboCount = 0;
    }

    /// <summary>
    /// Tăng combo và phát sound tương ứng
    /// </summary>
    public void IncrementComboAndPlaySound()
    {
        currentComboCount++;
        PlayMatchSound(currentComboCount);
    }

    /// <summary>
    /// Set volume cho SFX
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
    }

    /// <summary>
    /// Set volume cho Music
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
    }

    /// <summary>
    /// Pause/Resume background music
    /// </summary>
    public void ToggleBackgroundMusic(bool play)
    {
        if (musicSource != null)
        {
            if (play && !musicSource.isPlaying)
            {
                musicSource.Play();
            }
            else if (!play && musicSource.isPlaying)
            {
                musicSource.Pause();
            }
        }
    }
}