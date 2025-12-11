using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Tự động phát sound khi click button
/// Gắn script này vào bất kỳ button nào
/// </summary>
public class ButtonClickSound : MonoBehaviour, IPointerClickHandler
{
    public static AudioClip clickSound; // Static để share giữa tất cả buttons
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
            DontDestroyOnLoad(audioObject); // Giữ qua scene
            audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f; // 2D sound
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Chỉ phát sound nếu button đang interactable
        if (button != null && button.interactable && clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound, volume);
        }
    }
}