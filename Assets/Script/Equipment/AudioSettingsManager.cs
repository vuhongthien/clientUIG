using UnityEngine;

/// <summary>
/// Quản lý audio settings qua PlayerPrefs
/// Mỗi scene tự động load settings khi start
/// </summary>
public class AudioSettingsManager : MonoBehaviour
{
    [Header("Volume Settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const string BGM_VOLUME_KEY = "BGMVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";

    private void Start()
    {
        LoadSettings();
        ApplySettingsToCurrentScene();
    }

    /// <summary>
    /// ✅ Load settings từ PlayerPrefs
    /// </summary>
    public void LoadSettings()
    {
        masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
        bgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 0.5f);
        sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 0.8f);

        Debug.Log($"[AudioSettings] Loaded: Master={masterVolume}, BGM={bgmVolume}, SFX={sfxVolume}");
    }

    /// <summary>
    /// ✅ Save settings vào PlayerPrefs
    /// </summary>
    public void SaveSettings()
    {
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, masterVolume);
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, bgmVolume);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxVolume);
        PlayerPrefs.Save();

        Debug.Log($"[AudioSettings] Saved: Master={masterVolume}, BGM={bgmVolume}, SFX={sfxVolume}");
    }

    /// <summary>
    /// ✅ Áp dụng settings cho scene hiện tại
    /// </summary>
    public void ApplySettingsToCurrentScene()
    {
        // Áp dụng cho AudioManager (Board scene)
        AudioManager audioManager = FindObjectOfType<AudioManager>();
        if (audioManager != null)
        {
            audioManager.SetBGMVolume(bgmVolume * masterVolume);
            audioManager.SetSFXVolume(sfxVolume * masterVolume);
            Debug.Log("[AudioSettings] Applied to AudioManager");
        }

        // Áp dụng cho ManagerQuangTruong
        ManagerQuangTruong quangTruong = FindObjectOfType<ManagerQuangTruong>();
        if (quangTruong != null)
        {
            quangTruong.SetBGMVolume(bgmVolume * masterVolume);
            Debug.Log("[AudioSettings] Applied to ManagerQuangTruong");
        }

        // Áp dụng cho ManagerKhoPet
        ManagerKhoPet khoPet = FindObjectOfType<ManagerKhoPet>();
        if (khoPet != null)
        {
            khoPet.SetBGMVolume(bgmVolume * masterVolume);
            Debug.Log("[AudioSettings] Applied to ManagerKhoPet");
        }

        Debug.Log("[AudioSettings] Applied to current scene");
    }

    /// <summary>
    /// ✅ Set master volume (0-1)
    /// </summary>
    public void SetMasterVolume(float volume)
{
    masterVolume = Mathf.Clamp01(volume);
    SaveSettings();
    ApplySettingsToCurrentScene();
    
    // ✅ THÔNG BÁO EVENT
    AudioEventManager.NotifyMasterVolumeChanged(masterVolume);
}

    /// <summary>
    /// ✅ Set BGM volume (0-1)
    /// </summary>
public void SetBGMVolume(float volume)
{
    bgmVolume = Mathf.Clamp01(volume);
    SaveSettings();
    ApplySettingsToCurrentScene();
    
    // BGM không cần event vì không ảnh hưởng buttons
}

    /// <summary>
    /// ✅ Set SFX volume (0-1)
    /// </summary>
public void SetSFXVolume(float volume)
{
    sfxVolume = Mathf.Clamp01(volume);
    SaveSettings();
    ApplySettingsToCurrentScene();
    
    // ✅ THÔNG BÁO EVENT
    AudioEventManager.NotifySFXVolumeChanged(sfxVolume);
}

    /// <summary>
    /// ✅ Static helper - Load settings từ PlayerPrefs không cần instance
    /// </summary>
    public static AudioSettings GetSavedSettings()
    {
        return new AudioSettings
        {
            masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f),
            bgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 0.5f),
            sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 0.8f)
        };
    }
}

/// <summary>
/// DTO để truyền settings
/// </summary>
[System.Serializable]
public class AudioSettings
{
    public float masterVolume;
    public float bgmVolume;
    public float sfxVolume;
}