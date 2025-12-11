using System;
using UnityEngine;

public class StarEventManager : MonoBehaviour
{
    public static StarEventManager Instance;
    
    public event Action<int, int, int> OnStarCountChanged;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[StarEventManager] ✅ Instance created");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void UpdateStarCount(int white, int blue, int red)
    {
        Debug.Log($"[StarEventManager] Broadcasting star update - White: {white}, Blue: {blue}, Red: {red}");
        
        // Cập nhật PlayerPrefs
        PlayerPrefs.SetInt("StarWhite", white);
        PlayerPrefs.SetInt("StarBlue", blue);
        PlayerPrefs.SetInt("StarRed", red);
        PlayerPrefs.Save();
        
        // Trigger event cho tất cả listeners
        OnStarCountChanged?.Invoke(white, blue, red);
    }
}