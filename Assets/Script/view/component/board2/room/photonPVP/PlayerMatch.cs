using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMatch : MonoBehaviour
{
    [SerializeField] private Text nameText;
    [SerializeField] private Image imgUser;
    [SerializeField] private Text txtLv;
    [SerializeField] private Image petImg;
    [SerializeField] private GameObject readyIndicator;
    
    public int PlayerActorNumber { get; private set; }
    
    private bool isDataLoaded = false; // Flag để tránh load API nhiều lần
    private static Dictionary<string, UserDTO> userCache = new Dictionary<string, UserDTO>(); // Cache user data
    
    public void setUpPlayer(Player player, string name)
    {
        PlayerActorNumber = player.ActorNumber;
        
        // Chỉ load data nếu chưa load hoặc là player khác
        if (!isDataLoaded)
        {
            StartCoroutine(LoadPlayerData(name));
        }
        
        // Chủ phòng luôn hiển thị là sẵn sàng
        if (player.IsMasterClient)
        {
            SetReadyState(true);
        }
        else
        {
            if (player.CustomProperties.TryGetValue("isReady", out object isReady))
            {
                SetReadyState((bool)isReady);
            }
        }
    }
    
    private IEnumerator LoadPlayerData(string userId)
    {
        if (isDataLoaded)
        {
            Debug.Log($"PlayerMatch: Data already loaded for {userId}");
            yield break;
        }

        // Check cache first
        if (userCache.ContainsKey(userId))
        {
            Debug.Log($"PlayerMatch: Using cached data for user {userId}");
            OnUserReceived(userCache[userId]);
            yield break;
        }

        Debug.Log($"PlayerMatch: Loading data for user {userId}");
        yield return APIManager.Instance.GetRequest<UserDTO>(APIConfig.GET_USER(int.Parse(userId)), OnUserReceived, OnError);
    }
    
    void OnUserReceived(UserDTO user)
    {
        if (isDataLoaded)
        {
            Debug.Log("PlayerMatch: Data already processed, ignoring duplicate");
            return;
        }

        isDataLoaded = true;
        
        // Cache user data
        string userId = user.id.ToString();
        if (!userCache.ContainsKey(userId))
        {
            userCache[userId] = user;
        }

        // Update UI với data từ API
        if (txtLv != null) txtLv.text = "Lv" + user.lever.ToString();
        if (nameText != null) nameText.text = user.name;
        
        // Load avatar
        if (imgUser != null)
        {
            Sprite avtSprite = Resources.Load<Sprite>("Image/Avt/" + user.avtId);
            if (avtSprite != null) imgUser.sprite = avtSprite;
        }
        
        // KHÔNG tự động load pet từ API - sẽ được set từ Room Properties
        // Pet sẽ được update thông qua SetSelectedPet() từ LobbyManager/ManagerPVP

        Debug.Log($"PlayerMatch: Loaded user data for {user.name} - pet will be set separately");
    }
    
    void OnError(string error)
    {
        Debug.LogError("PlayerMatch API Error: " + error);
        isDataLoaded = false; // Allow retry on error
    }
    
    public void SetReadyState(bool isReady)
    {
        if (readyIndicator != null) readyIndicator.SetActive(isReady);
        if (nameText != null) nameText.color = isReady ? Color.green : Color.white;
    }
    
    /// <summary>
    /// Method để set pet selection cho player (được gọi từ ManagerPVP/LobbyManager)
    /// </summary>
    public void SetSelectedPet(int petId)
    {
        if (petId <= 0) 
        {
            Debug.LogWarning($"PlayerMatch: Invalid petId: {petId}");
            return;
        }
        
        if (petImg == null)
        {
            Debug.LogWarning("PlayerMatch: petImg is null");
            return;
        }
        
        // Load và hiển thị sprite của pet được chọn
        Sprite petSprite = Resources.Load<Sprite>("Image/IconsPet/" + petId);
        if (petSprite != null)
        {
            petImg.sprite = petSprite;
            Debug.Log($"PlayerMatch: Set pet sprite for petId: {petId}");
        }
        else
        {
            Debug.LogWarning($"PlayerMatch: Could not load pet sprite for petId: {petId}");
        }
    }

    /// <summary>
    /// Update both pet and avatar if needed
    /// </summary>
    public void UpdatePlayerVisuals(int petId, int avtId = -1)
    {
        // Update pet
        SetSelectedPet(petId);
        
        // Update avatar if provided
        if (avtId > 0 && imgUser != null)
        {
            Sprite avtSprite = Resources.Load<Sprite>("Image/Avt/" + avtId);
            if (avtSprite != null)
            {
                imgUser.sprite = avtSprite;
                Debug.Log($"PlayerMatch: Updated avatar to {avtId}");
            }
        }
    }

    /// <summary>
    /// Reset component state (useful for object pooling)
    /// </summary>
    public void ResetState()
    {
        isDataLoaded = false;
        PlayerActorNumber = 0;
        
        if (nameText != null) nameText.text = "";
        if (txtLv != null) txtLv.text = "";
        if (readyIndicator != null) readyIndicator.SetActive(false);
        if (nameText != null) nameText.color = Color.white;
    }

    // Clear cache when needed (call this when changing scenes or rooms)
    public static void ClearUserCache()
    {
        userCache.Clear();
        Debug.Log("PlayerMatch: User cache cleared");
    }
}