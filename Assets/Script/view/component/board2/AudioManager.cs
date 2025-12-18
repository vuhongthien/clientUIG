using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource bgmSource;      // Nhạc nền
    public AudioSource sfxSource;      // Hiệu ứng âm thanh

    [Header("Background Music - 6 Tracks")]
    [Tooltip("Sẽ random 1 trong 6 bài này khi bắt đầu")]
    public AudioClip[] backgroundMusics = new AudioClip[6];

    [Header("Match Sounds (6 loại viên)")]
    [Tooltip("Thứ tự: xanh, xanhduong, do, tim, trang, vang")]
    public AudioClip[] matchSounds = new AudioClip[6];

    [Header("Special Sounds")]
    public AudioClip swordClickSound;   // Click vào kim cương/kiếm

    [Header("Settings")]
    [Range(0f, 1f)] public float bgmVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    [Header("Debug Info")]
    [SerializeField] private int currentBGMIndex = -1; // Để xem track nào đang phát

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SetupAudioSources();
    }

    private void Start()
    {
        PlayRandomBackgroundMusic();
        LoadAudioSettings();
    }

    void LoadAudioSettings()
    {
        AudioSettings settings = AudioSettingsManager.GetSavedSettings();

        SetBGMVolume(settings.bgmVolume * settings.masterVolume);
        SetSFXVolume(settings.sfxVolume * settings.masterVolume);

        Debug.Log($"[AudioManager] Settings loaded: BGM={settings.bgmVolume}, SFX={settings.sfxVolume}");
    }

    void SetupAudioSources()
    {
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
        }
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }
        sfxSource.loop = false;
        sfxSource.volume = sfxVolume;
    }

    // ==================== BACKGROUND MUSIC ====================

    /// <summary>
    /// ✅ RANDOM 1 TRONG 6 TRACK
    /// </summary>
    public void PlayRandomBackgroundMusic()
    {
        if (backgroundMusics == null || backgroundMusics.Length == 0)
        {
            Debug.LogWarning("[AudioManager] No background music assigned!");
            return;
        }

        // Lọc những track không null
        var validTracks = new System.Collections.Generic.List<AudioClip>();
        for (int i = 0; i < backgroundMusics.Length; i++)
        {
            if (backgroundMusics[i] != null)
            {
                validTracks.Add(backgroundMusics[i]);
            }
        }

        if (validTracks.Count == 0)
        {
            Debug.LogWarning("[AudioManager] All background music clips are null!");
            return;
        }

        // Random 1 track
        currentBGMIndex = Random.Range(0, validTracks.Count);
        AudioClip selectedTrack = validTracks[currentBGMIndex];

        if (bgmSource != null)
        {
            bgmSource.clip = selectedTrack;
            bgmSource.Play();
            Debug.Log($"[AudioManager] ♪ Playing BGM Track #{currentBGMIndex + 1}: {selectedTrack.name}");
        }
    }

    /// <summary>
    /// ✅ CHUYỂN SANG TRACK KHÁC (OPTIONAL)
    /// </summary>
    public void PlayNextBackgroundMusic()
    {
        PlayRandomBackgroundMusic();
    }

    public void StopBackgroundMusic()
    {
        if (bgmSource != null)
            bgmSource.Stop();
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmSource != null)
            bgmSource.volume = bgmVolume;
    }

    // ==================== MATCH SOUNDS ====================
    /// <summary>
    /// Phát âm thanh khi phá viên theo tag
    /// </summary>
    public void PlayMatchSound(string dotTag)
    {
        int index = GetSoundIndexFromTag(dotTag);

        if (index >= 0 && index < matchSounds.Length && matchSounds[index] != null)
        {
            sfxSource.PlayOneShot(matchSounds[index], sfxVolume);
        }
    }

    /// <summary>
    /// Mapping tag → sound index
    /// </summary>
    private int GetSoundIndexFromTag(string tag)
    {
        switch (tag)
        {
            case "xanh Dot": return 0; // Xanh lá
            case "xanhduong Dot": return 1; // Xanh dương
            case "do Dot": return 2; // Đỏ
            case "tim Dot": return 3; // Tím
            case "trang Dot": return 4; // Trắng
            case "vang Dot": return 5; // Vàng (kim cương)
            default:
                Debug.LogWarning($"[AudioManager] Unknown dot tag: {tag}");
                return -1;
        }
    }

    // ==================== SPECIAL SOUNDS ====================
    public void PlaySwordClickSound()
    {
        if (swordClickSound != null)
        {
            sfxSource.PlayOneShot(swordClickSound, sfxVolume);
        }
    }

    // ==================== COMBO SYSTEM (OPTIONAL) ====================
    [Header("Combo Settings")]
    public float comboPitchIncrement = 0.1f;
    private int currentCombo = 0;

    public void PlayMatchSoundWithCombo(string dotTag, int comboCount)
    {
        int index = GetSoundIndexFromTag(dotTag);

        if (index >= 0 && index < matchSounds.Length && matchSounds[index] != null)
        {
            // Tăng pitch theo combo (tối đa 1.5x)
            float pitch = 1f + Mathf.Min(comboCount * comboPitchIncrement, 0.5f);

            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(matchSounds[index], sfxVolume);

            // Reset pitch sau 0.2s
            StartCoroutine(ResetPitchAfterDelay(0.2f));
        }
    }

    IEnumerator ResetPitchAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        sfxSource.pitch = 1f;
    }

    public void ResetCombo()
    {
        currentCombo = 0;
    }

    // ==================== VOLUME CONTROL ====================
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
            sfxSource.volume = sfxVolume;
    }

    public void MuteAll()
    {
        if (bgmSource != null) bgmSource.mute = true;
        if (sfxSource != null) sfxSource.mute = true;
    }

    public void UnmuteAll()
    {
        if (bgmSource != null) bgmSource.mute = false;
        if (sfxSource != null) sfxSource.mute = false;
    }

    // ==================== DEBUG ====================
    public string GetCurrentTrackName()
    {
        return bgmSource?.clip?.name ?? "None";
    }
}