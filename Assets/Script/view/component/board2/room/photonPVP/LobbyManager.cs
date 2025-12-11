using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private PlayerMatch playerMatches;
    [SerializeField] private Transform[] lobbyPlayer;
    [SerializeField] private Button btnStart;
    [SerializeField] private Text btnStartText;

    private PhotonView _photonView;

    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
    }

    private void Start()
    {
        btnStart.gameObject.SetActive(true);
        UpdateButtonForPlayerRole();
    }

    public void OnStartButtonClicked()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            StartGame();
        }
        else
        {
            ToggleReady();
        }
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Join Room");

        int userId = PlayerPrefs.GetInt("userId", 1);
        int petId = GetSelectedPetId();

        // Set Player Custom Properties
        var playerProps = new ExitGames.Client.Photon.Hashtable
        {
            ["petId"] = petId,
            ["userId"] = userId,
            ["isReady"] = false // Mặc định chưa sẵn sàng
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);

        // Set Room Properties for pet selection
        SetPlayerPetInRoom(userId, petId);

        LoadRoom();
        UpdateButtonForPlayerRole();
        
        if (PhotonRoom.instance != null)
        {
            PhotonRoom.instance.UpdateRoomProfileUI();
        }
    }

    /// <summary>
    /// Set pet selection vào Room Properties để các player khác có thể truy cập
    /// </summary>
    private void SetPlayerPetInRoom(int userId, int petId)
    {
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogWarning("Cannot set room properties - not in room!");
            return;
        }

        string roomPetKey = $"player_{userId}_petId";
        string roomUserNameKey = $"player_{userId}_name";
        
        var roomProps = new ExitGames.Client.Photon.Hashtable
        {
            [roomPetKey] = petId,
            [roomUserNameKey] = PlayerPrefs.GetString("username", "Player")
        };
        
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
        Debug.Log($"Set room property: {roomPetKey} = {petId}");
    }

    /// <summary>
    /// Get pet ID từ ManagerDauTruong hoặc PlayerPrefs
    /// </summary>
    private int GetSelectedPetId()
    {
        // Priority 1: From ManagerDauTruong
        if (ManagerDauTruong.Instance != null && !string.IsNullOrEmpty(ManagerDauTruong.Instance.petId))
        {
            if (int.TryParse(ManagerDauTruong.Instance.petId, out int petId))
            {
                return petId;
            }
        }

        // Priority 2: From PlayerPrefs
        return PlayerPrefs.GetInt("userPetId", 1);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        LoadRoom();
        LogRoomProperties(); // Debug: Log all room properties
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        // Clean up room properties của player đã rời
        CleanupPlayerRoomProperties(otherPlayer);
        LoadRoom();
        CheckAllPlayersReady();
    }

    /// <summary>
    /// Dọn dẹp Room Properties của player đã rời phòng
    /// </summary>
    private void CleanupPlayerRoomProperties(Player leftPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Get userId from player custom properties
        if (leftPlayer.CustomProperties.ContainsKey("userId"))
        {
            int leftUserId = (int)leftPlayer.CustomProperties["userId"];
            string petKey = $"player_{leftUserId}_petId";
            string nameKey = $"player_{leftUserId}_name";

            // Remove properties bằng cách set null
            var roomProps = new ExitGames.Client.Photon.Hashtable
            {
                [petKey] = null,
                [nameKey] = null
            };
            
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
            Debug.Log($"Cleaned up room properties for left player: {leftUserId}");
        }
    }

    public void ToggleReady()
    {
        bool currentReadyState = (bool)(PhotonNetwork.LocalPlayer.CustomProperties["isReady"] ?? false);
        var props = new ExitGames.Client.Photon.Hashtable
        {
            ["isReady"] = !currentReadyState
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        // Cập nhật UI ngay lập tức cho người chơi hiện tại
        btnStartText.text = !currentReadyState ? "Hủy sẵn sàng" : "Sẵn sàng";
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey("isReady"))
        {
            UpdatePlayerReadyUI(targetPlayer, (bool)changedProps["isReady"]);
            CheckAllPlayersReady();
        }

        // If pet selection changed, update room properties
        if (changedProps.ContainsKey("petId") && changedProps.ContainsKey("userId"))
        {
            int userId = (int)changedProps["userId"];
            int petId = (int)changedProps["petId"];
            SetPlayerPetInRoom(userId, petId);
        }
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
{
    // Kiểm tra xem có phải chỉ là pet property update không
    bool isPetUpdate = false;
    bool hasOtherUpdates = false;
    
    foreach (var prop in propertiesThatChanged)
    {
        string key = prop.Key.ToString();
        if (key.StartsWith("player_") && key.EndsWith("_petId"))
        {
            isPetUpdate = true;
            Debug.Log($"Room property updated: {prop.Key} = {prop.Value}");
        }
        else
        {
            hasOtherUpdates = true;
        }
    }
    
    // Chỉ reload room UI nếu có updates khác ngoài pet selection
    if (hasOtherUpdates || !isPetUpdate)
    {
        LoadRoom();
    }
    else
    {
        Debug.Log("Skipping LoadRoom() - only pet selection updates");
    }
}

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        // Cập nhật UI cho tất cả người chơi
        LoadRoom();
        
        // Đặt master client mới thành sẵn sàng
        if (newMasterClient == PhotonNetwork.LocalPlayer)
        {
            var props = new ExitGames.Client.Photon.Hashtable
            {
                ["isReady"] = true
            };
            newMasterClient.SetCustomProperties(props);
        }
    }

    private void UpdateButtonForPlayerRole()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            btnStartText.text = "Bắt đầu";
            CheckAllPlayersReady();
        }
        else
        {
            bool isReady = (bool)(PhotonNetwork.LocalPlayer.CustomProperties["isReady"] ?? false);
            btnStartText.text = isReady ? "Hủy sẵn sàng" : "Sẵn sàng";
        }
    }

    private void LoadRoom()
{
    Debug.Log("LoadRoom() called");
    
    // Xóa tất cả UI player cũ
    foreach (var lobby in lobbyPlayer)
    {
        foreach (Transform child in lobby.transform)
        {
            if (child != null) 
                Destroy(child.gameObject);
        }
    }

    // Lấy danh sách người chơi đã sắp xếp
    Player[] sortedPlayers = PhotonNetwork.PlayerList
        .OrderBy(p => p.ActorNumber)
        .ToArray();

    // Tạo UI cho từng người chơi
    for (int i = 0; i < sortedPlayers.Length; i++)
    {
        if (i >= lobbyPlayer.Length) break;

        Player player = sortedPlayers[i];
        PlayerMatch p = Instantiate(playerMatches, lobbyPlayer[i]);
        p.transform.localPosition = Vector3.zero;
        p.transform.localRotation = Quaternion.identity;

        // Xoay avatar của chủ phòng
        if (!player.IsMasterClient)
        {
            Transform imgUser = p.transform.Find("imgUser");
            if (imgUser != null)
            {
                imgUser.localRotation = Quaternion.Euler(0, 180, 0);
            }
        }

        Debug.Log("player.ActorNumber: " + player.ActorNumber);
        p.setUpPlayer(player, player.NickName);

        // Set up player name for tracking
        if (player.CustomProperties.ContainsKey("userId"))
        {
            int userId = (int)player.CustomProperties["userId"];
            p.transform.name = userId.ToString();
            
            // ĐỢI API load xong rồi mới set pet selection
            StartCoroutine(DelayedDisplayPlayerPetSelection(p, userId, player));
        }

        // Cập nhật trạng thái ready
        if (player.CustomProperties.TryGetValue("isReady", out object isReady))
        {
            p.SetReadyState((bool)isReady);
        }
    }

    UpdateButtonForPlayerRole();
}

/// <summary>
/// Delay việc hiển thị pet selection để đảm bảo API data đã load xong
/// </summary>
private IEnumerator DelayedDisplayPlayerPetSelection(PlayerMatch playerMatch, int userId, Player player)
{
    // Đợi PlayerMatch load API data xong
    yield return new WaitForSeconds(1.5f);
    
    // Ưu tiên Room Properties trước
    string roomPetKey = $"player_{userId}_petId";
    if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(roomPetKey))
    {
        int roomPetId = (int)PhotonNetwork.CurrentRoom.CustomProperties[roomPetKey];
        Debug.Log($"Setting pet from ROOM properties: User {userId} = Pet {roomPetId}");
        playerMatch.SetSelectedPet(roomPetId);
        yield break;
    }
    
    // Fallback: Player Properties
    if (player.CustomProperties.ContainsKey("petId"))
    {
        int playerPetId = (int)player.CustomProperties["petId"];
        Debug.Log($"Setting pet from PLAYER properties: User {userId} = Pet {playerPetId}");
        playerMatch.SetSelectedPet(playerPetId);
        yield break;
    }
    
    Debug.Log($"No pet selection found for user {userId} - using default from API");
}

    private void UpdatePlayerReadyUI(Player player, bool isReady)
    {
        // Tìm UI player tương ứng và cập nhật
        for (int i = 0; i < lobbyPlayer.Length; i++)
        {
            if (lobbyPlayer[i].childCount > 0)
            {
                PlayerMatch playerMatch = lobbyPlayer[i].GetComponentInChildren<PlayerMatch>();
                if (playerMatch != null && playerMatch.PlayerActorNumber == player.ActorNumber)
                {
                    playerMatch.SetReadyState(isReady);
                    break;
                }
            }
        }

        // Cập nhật nút local player ngay lập tức
        if (player == PhotonNetwork.LocalPlayer && !PhotonNetwork.IsMasterClient)
        {
            btnStartText.text = isReady ? "Hủy sẵn sàng" : "Sẵn sàng";
        }
    }

    private void CheckAllPlayersReady()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        bool allReady = true;
        
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            // Master client không cần ready
            if (player.IsMasterClient) continue;

            if (!player.CustomProperties.ContainsKey("isReady") || 
                !(bool)player.CustomProperties["isReady"])
            {
                allReady = false;
                break;
            }
        }

        btnStart.interactable = allReady;
    }

    public void StartGame()
    {
        if (!PhotonNetwork.IsMasterClient || !btnStart.interactable) return;

        // Log room properties trước khi start game
        LogRoomProperties();

        // Chỉ thiết lập properties cơ bản, không load UI
        // ManagerMatchPVP sẽ xử lý việc load data từ API
        SetupBasicPhotonProperties();
        
        PhotonNetwork.LoadLevel("MatchPVP");
    }

    /// <summary>
    /// Thiết lập properties cơ bản cho Photon trước khi vào game
    /// ManagerMatchPVP sẽ load data chi tiết từ API sau
    /// </summary>
    private void SetupBasicPhotonProperties()
    {
        if (PhotonNetwork.LocalPlayer == null) return;

        int userId = PlayerPrefs.GetInt("userId", 1);
        int petId = GetSelectedPetId();

        var props = new ExitGames.Client.Photon.Hashtable
        {
            ["petId"] = petId,
            ["userId"] = userId,
            ["isReady"] = true // Đánh dấu ready khi bắt đầu game
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        
        // Đảm bảo room properties cũng được set
        SetPlayerPetInRoom(userId, petId);
        
        Debug.Log($"Basic Photon properties set before entering game: User={userId}, Pet={petId}");
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        Debug.Log("Đã rời phòng");
    }

    /// <summary>
    /// Debug method: Log tất cả Room Properties
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogRoomProperties()
    {
        Debug.Log("=== ROOM PROPERTIES ===");
        foreach (var prop in PhotonNetwork.CurrentRoom.CustomProperties)
        {
            Debug.Log($"{prop.Key}: {prop.Value}");
        }
        
        Debug.Log("=== PLAYER PROPERTIES ===");
        foreach (var player in PhotonNetwork.PlayerList)
        {
            Debug.Log($"Player {player.NickName} (ID: {player.ActorNumber}):");
            foreach (var prop in player.CustomProperties)
            {
                Debug.Log($"  {prop.Key}: {prop.Value}");
            }
        }
    }

    /// <summary>
    /// Public method để get pet ID của một player từ Room Properties
    /// </summary>
    public int GetPlayerPetFromRoom(int userId)
    {
        string roomPetKey = $"player_{userId}_petId";
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(roomPetKey))
        {
            return (int)PhotonNetwork.CurrentRoom.CustomProperties[roomPetKey];
        }
        return -1; // Not found
    }

    /// <summary>
    /// Public method để get tất cả player selections từ Room Properties
    /// </summary>
    public Dictionary<int, int> GetAllPlayerPetSelections()
    {
        Dictionary<int, int> selections = new Dictionary<int, int>();
        
        foreach (var prop in PhotonNetwork.CurrentRoom.CustomProperties)
        {
            string key = prop.Key.ToString();
            if (key.StartsWith("player_") && key.EndsWith("_petId"))
            {
                // Extract userId from key: "player_123_petId" -> 123
                string userIdStr = key.Substring(7, key.Length - 13); // Remove "player_" and "_petId"
                if (int.TryParse(userIdStr, out int userId) && int.TryParse(prop.Value.ToString(), out int petId))
                {
                    selections[userId] = petId;
                }
            }
        }
        
        return selections;
    }
}