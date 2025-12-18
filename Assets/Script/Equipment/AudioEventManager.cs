using UnityEngine;
using System;

/// <summary>
/// Quản lý events cho audio settings
/// Cho phép components subscribe vào thay đổi volume
/// </summary>
public static class AudioEventManager
{
    // Event khi SFX volume thay đổi
    public static event Action<float> OnSFXVolumeChanged;
    
    // Event khi Master volume thay đổi
    public static event Action<float> OnMasterVolumeChanged;
    
    /// <summary>
    /// Gọi khi SFX volume thay đổi
    /// </summary>
    public static void NotifySFXVolumeChanged(float newVolume)
    {
        OnSFXVolumeChanged?.Invoke(newVolume);
        Debug.Log($"[AudioEvent] SFX Volume Changed: {newVolume * 100}%");
    }
    
    /// <summary>
    /// Gọi khi Master volume thay đổi
    /// </summary>
    public static void NotifyMasterVolumeChanged(float newVolume)
    {
        OnMasterVolumeChanged?.Invoke(newVolume);
        Debug.Log($"[AudioEvent] Master Volume Changed: {newVolume * 100}%");
    }
}