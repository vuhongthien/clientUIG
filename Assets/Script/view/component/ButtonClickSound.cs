using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Tự động phát sound khi click button
/// Volume thay đổi theo SFX settings
/// </summary>
public class ButtonClickSound : MonoBehaviour, IPointerClickHandler
{
    public static AudioClip clickSound;
    public static AudioSource audioSource;
    
    [Range(0f, 1f)]
    public float volume = 0.7f;
    
    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        
        // Tạo AudioSource chung cho tất cả buttons (chỉ tạo 1 lần)
        if (audioSource == null)
        {
            GameObject audioObject = new GameObject("ButtonClickAudioSource");
            DontDestroyOnLoad(audioObject);
            audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            
            // ✅ LOAD VOLUME BAN ĐẦU TỪ PLAYERPREFS
            UpdateAudioSourceVolume();
        }
    }

    void OnEnable()
    {
        // ✅ SUBSCRIBE VÀO EVENTS
        AudioEventManager.OnSFXVolumeChanged += OnVolumeChanged;
        AudioEventManager.OnMasterVolumeChanged += OnVolumeChanged;
    }

    void OnDisable()
    {
        // ✅ UNSUBSCRIBE
        AudioEventManager.OnSFXVolumeChanged -= OnVolumeChanged;
        AudioEventManager.OnMasterVolumeChanged -= OnVolumeChanged;
    }

    /// <summary>
    /// ✅ Callback khi volume thay đổi
    /// </summary>
    void OnVolumeChanged(float newValue)
    {
        UpdateAudioSourceVolume();
    }

    /// <summary>
    /// ✅ Cập nhật volume của AudioSource theo settings
    /// </summary>
    void UpdateAudioSourceVolume()
    {
        if (audioSource == null) return;
        
        AudioSettings settings = AudioSettingsManager.GetSavedSettings();
        
        // Volume = SFX * Master (không nhân với base volume ở đây)
        audioSource.volume = settings.sfxVolume * settings.masterVolume;
        
        Debug.Log($"[ButtonClickSound] AudioSource volume updated: {audioSource.volume} (SFX={settings.sfxVolume}, Master={settings.masterVolume})");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Chỉ phát sound nếu button đang interactable
        if (button != null && button.interactable && clickSound != null && audioSource != null)
        {
            // ✅ SỬ DỤNG base volume * AudioSource.volume (đã có SFX * Master)
            float finalVolume = volume * audioSource.volume;
            audioSource.PlayOneShot(clickSound, finalVolume);
        }
    }
}