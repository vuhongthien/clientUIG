using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class RoomWebSocketManager : MonoBehaviour
{
    private static RoomWebSocketManager _instance;
    public event Action<RoomDTO> OnRoomJoined;
    public event Action<string> OnJoinError;
    public event Action<RoomDTO> OnRoomUpdated;
    public event Action<long, int> OnPetUpdated;           // (userId, petId)
    public event Action<long, List<CardData>> OnCardsUpdated;
    public event Action<long> OnRoomLeft;  // ✅ EVENT ĐÃ CÓ

    public static RoomWebSocketManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject obj = new GameObject("RoomWebSocketManager");
                _instance = obj.AddComponent<RoomWebSocketManager>();
                DontDestroyOnLoad(obj);
            }
            return _instance;
        }
    }

    private StompClient stompClient;
    private bool isConnected = false;
    private int currentUserId;

    // Events
    public event Action<List<OnlineUserDTO>> OnOnlineUsersUpdated;
    public event Action<RoomInviteDTO> OnInviteReceived;
    public event Action<RoomInviteDTO> OnInviteResponseReceived;
    public event Action<long, string, bool> OnRoomClosed;
    public event Action<bool, int, int> OnReadyStatusChanged;
    public event Action<long, bool> OnMemberReadyChanged;
    public event Action<long, string> OnKicked; // (roomId, reason)

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnReadyStatusMessage(string body)
    {
        Debug.Log($"[STOMP] Ready status: {body}");

        try
        {
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(body);

            bool allReady = bool.Parse(data["allReady"].ToString());
            int readyCount = int.Parse(data["readyCount"].ToString());
            int totalMembers = int.Parse(data["totalMembers"].ToString());

            OnReadyStatusChanged?.Invoke(allReady, readyCount, totalMembers);
        }
        catch (Exception e)
        {
            Debug.LogError($"[STOMP] Error parsing ready status: {e.Message}");
        }
    }

    public void SetReady(long roomId, int userId, bool ready)
    {
        if (!isConnected)
        {
            Debug.LogWarning("[STOMP] Not connected!");
            return;
        }

        var data = new Dictionary<string, object>
    {
        { "roomId", roomId },
        { "userId", userId },
        { "ready", ready }
    };

        string json = JsonConvert.SerializeObject(data);
        stompClient.Send("/app/room/set-ready", json);

        Debug.Log($"[STOMP] Sent ready status: {ready}");
    }

    public void Connect(int userId, string username, int avatarId, int level)
    {
        if (isConnected) return;

        currentUserId = userId;
        stompClient = new StompClient();

        stompClient.Connect("ws://localhost:8080/ws-room", () =>
        {
            Debug.Log("[STOMP] Connected successfully!");
            isConnected = true;

            // Existing subscriptions
            stompClient.Subscribe("/topic/online-users", OnOnlineUsersMessage);
            stompClient.Subscribe($"/queue/invite/{userId}", OnInviteMessage);
            stompClient.Subscribe($"/queue/invite-response/{userId}", OnInviteResponseMessage);
            stompClient.Subscribe($"/queue/room-joined/{userId}", OnRoomJoinedMessage);
            stompClient.Subscribe($"/queue/join-error/{userId}", OnJoinErrorMessage);
            stompClient.Subscribe($"/queue/room-update/{userId}", OnRoomUpdateMessage);
            stompClient.Subscribe($"/queue/room-created/{userId}", OnRoomCreatedMessage);
            stompClient.Subscribe($"/queue/pet-update/{userId}", OnPetUpdateMessage);
            stompClient.Subscribe($"/queue/cards-update/{userId}", OnCardsUpdateMessage);
            stompClient.Subscribe($"/queue/room-closed/{userId}", OnRoomClosedMessage);
            stompClient.Subscribe($"/topic/room-closed/{userId}", OnRoomClosedMessage);
            stompClient.Subscribe($"/queue/room-left/{userId}", OnRoomLeftMessage);
            stompClient.Subscribe($"/queue/ready-status/{userId}", OnReadyStatusMessage);
            stompClient.Subscribe($"/queue/room-kicked/{userId}", OnKickedMessage);
            // ✅ SUBSCRIBE READY UPDATE
            stompClient.Subscribe($"/queue/ready-update/{userId}", OnReadyUpdateMessage);

            // Send connect message
            var data = new OnlineUserDTO
            {
                userId = userId,
                username = username,
                avatarId = avatarId.ToString(),
                level = level
            };

            string json = JsonConvert.SerializeObject(data);
            stompClient.Send("/app/room/connect", json);
        });
    }
    /// <summary>
    /// ✅ XỬ LÝ MESSAGE BỊ KICK
    /// </summary>
    private void OnKickedMessage(string body)
    {
        Debug.Log($"[STOMP] ========================================");
        Debug.Log($"[STOMP] ⚠️ KICKED FROM ROOM");
        Debug.Log($"[STOMP] Body: {body}");

        try
        {
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(body);

            long roomId = long.Parse(data["roomId"].ToString());
            string reason = data.ContainsKey("reason") ? data["reason"].ToString() : "Bạn đã bị kick";

            Debug.Log($"[STOMP]   Room ID: {roomId}");
            Debug.Log($"[STOMP]   Reason: {reason}");
            Debug.Log($"[STOMP] ========================================");

            OnKicked?.Invoke(roomId, reason);
        }
        catch (Exception e)
        {
            Debug.LogError($"[STOMP] Error parsing kicked message: {e.Message}");
        }
    }

    /// <summary>
    /// ✅ GỬI REQUEST KICK MEMBER
    /// </summary>
    public void KickMember(long roomId, int hostUserId, int kickedUserId)
    {
        if (!isConnected)
        {
            Debug.LogWarning("[STOMP] Not connected!");
            return;
        }

        var data = new Dictionary<string, object>
    {
        { "roomId", roomId },
        { "hostUserId", hostUserId },
        { "kickedUserId", kickedUserId }
    };

        string json = JsonConvert.SerializeObject(data);
        stompClient.Send("/app/room/kick-member", json);

        Debug.Log($"[STOMP] Sent kick request: roomId={roomId}, kicked={kickedUserId}");
    }
    /// <summary>
    /// ✅ XỬ LÝ MESSAGE READY UPDATE (CHỈ CẬP NHẬT READY, KHÔNG GHI ĐÈ DATA)
    /// </summary>
    private void OnReadyUpdateMessage(string body)
    {
        Debug.Log($"[STOMP] Ready update: {body}");

        try
        {
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(body);

            long userId = long.Parse(data["userId"].ToString());
            bool ready = bool.Parse(data["ready"].ToString());

            Debug.Log($"[STOMP] Member {userId} ready status: {ready}");

            OnMemberReadyChanged?.Invoke(userId, ready);
        }
        catch (Exception e)
        {
            Debug.LogError($"[STOMP] Error parsing ready update: {e.Message}");
        }
    }

    /// <summary>
    /// ✅ XỬ LÝ MESSAGE ROOM CLOSED (Host rời)
    /// </summary>
    private void OnRoomClosedMessage(string body)
    {
        Debug.Log($"[STOMP] ========================================");
        Debug.Log($"[STOMP] 🚨 ROOM CLOSED MESSAGE RECEIVED");
        Debug.Log($"[STOMP] Body: {body}");

        try
        {
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(body);

            long roomId = long.Parse(data["roomId"].ToString());
            string reason = data.ContainsKey("reason") ? data["reason"].ToString() : "Phòng đã đóng";
            bool isHost = data.ContainsKey("host") && bool.Parse(data["host"].ToString());

            Debug.Log($"[STOMP]   Room ID: {roomId}");
            Debug.Log($"[STOMP]   Reason: {reason}");
            Debug.Log($"[STOMP]   Is Host: {isHost}");
            Debug.Log($"[STOMP] ========================================");

            OnRoomClosed?.Invoke(roomId, reason, isHost);
        }
        catch (Exception e)
        {
            Debug.LogError($"[STOMP] Error parsing room closed: {e.Message}");
        }
    }

    /// <summary>
    /// ✅ NEW: XỬ LÝ MESSAGE ROOM LEFT (Member rời)
    /// </summary>
    private void OnRoomLeftMessage(string body)
    {
        Debug.Log($"[STOMP] ========================================");
        Debug.Log($"[STOMP] 🔔 ROOM LEFT MESSAGE RECEIVED");
        Debug.Log($"[STOMP] Body: {body}");

        try
        {
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(body);

            // ✅ KIỂM TRA SUCCESS
            bool success = data.ContainsKey("success") && bool.Parse(data["success"].ToString());

            if (!success)
            {
                string error = data.ContainsKey("error") ? data["error"].ToString() : "Lỗi không xác định";
                Debug.LogError($"[STOMP] Leave room failed: {error}");

                // ✅ VẪN ĐÓNG PANEL (vì user muốn thoát)
                OnRoomLeft?.Invoke(0);
                return;
            }

            // ✅ THÀNH CÔNG
            long roomId = data.ContainsKey("roomId") ? long.Parse(data["roomId"].ToString()) : 0;
            string reason = data.ContainsKey("reason") ? data["reason"].ToString() : "Đã rời phòng";

            Debug.Log($"[STOMP]   ✓ Success: {reason}");
            Debug.Log($"[STOMP]   Room ID: {roomId}");
            Debug.Log($"[STOMP] ========================================");

            OnRoomLeft?.Invoke(roomId);
        }
        catch (Exception e)
        {
            Debug.LogError($"[STOMP] Error parsing room left: {e.Message}");

            // ✅ FALLBACK: VẪN ĐÓNG PANEL
            OnRoomLeft?.Invoke(0);
        }
    }

    // ... rest of your existing methods (UpdateRoomPet, UpdateRoomCards, etc.) ...

    public void UpdateRoomPet(long roomId, int petId)
    {
        if (!isConnected)
        {
            Debug.LogWarning("[STOMP] Not connected!");
            return;
        }

        var data = new Dictionary<string, object>
        {
            { "roomId", roomId },
            { "userId", currentUserId },
            { "petId", petId }
        };

        string json = JsonConvert.SerializeObject(data);
        stompClient.Send("/app/room/update-pet", json);

        Debug.Log($"[STOMP] Sent update pet: roomId={roomId}, petId={petId}");
    }

    public void UpdateRoomCards(long roomId, List<CardData> cards)
    {
        if (!isConnected)
        {
            Debug.LogWarning("[STOMP] Not connected!");
            return;
        }

        var data = new Dictionary<string, object>
        {
            { "roomId", roomId },
            { "userId", currentUserId },
            { "cards", cards }
        };

        string json = JsonConvert.SerializeObject(data);
        stompClient.Send("/app/room/update-cards", json);

        Debug.Log($"[STOMP] Sent update cards: roomId={roomId}, count={cards.Count}");
    }

    private void OnPetUpdateMessage(string body)
    {
        Debug.Log($"[STOMP] Pet updated: {body}");

        try
        {
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(body);
            long userId = long.Parse(data["userId"].ToString());
            int petId = int.Parse(data["petId"].ToString());

            OnPetUpdated?.Invoke(userId, petId);
        }
        catch (Exception e)
        {
            Debug.LogError($"[STOMP] Error parsing pet update: {e.Message}");
        }
    }

    private void OnCardsUpdateMessage(string body)
    {
        Debug.Log($"[STOMP] Cards updated: {body}");

        try
        {
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(body);
            long userId = long.Parse(data["userId"].ToString());

            var cardsJson = JsonConvert.SerializeObject(data["cards"]);
            List<CardData> cards = JsonConvert.DeserializeObject<List<CardData>>(cardsJson);

            OnCardsUpdated?.Invoke(userId, cards);
        }
        catch (Exception e)
        {
            Debug.LogError($"[STOMP] Error parsing cards update: {e.Message}");
        }
    }

    public void JoinRoomById(long roomId, int userId)
    {
        if (!isConnected)
        {
            Debug.LogWarning("[STOMP] Not connected!");
            return;
        }

        var data = new Dictionary<string, object>
        {
            { "roomId", roomId },
            { "userId", userId }
        };

        string json = JsonConvert.SerializeObject(data);
        stompClient.Send("/app/room/join-by-id", json);

        Debug.Log($"[STOMP] Sent join request for room {roomId}");
    }

    private void OnRoomJoinedMessage(string body)
    {
        Debug.Log($"[STOMP] Room joined: {body}");
        var room = JsonConvert.DeserializeObject<RoomDTO>(body);
        OnRoomJoined?.Invoke(room);
    }

    private void OnJoinErrorMessage(string body)
    {
        Debug.Log($"[STOMP] Join error: {body}");
        OnJoinError?.Invoke(body);
    }

    private void OnRoomUpdateMessage(string body)
    {
        Debug.Log($"[STOMP] Room updated: {body}");
        var room = JsonConvert.DeserializeObject<RoomDTO>(body);
        OnRoomUpdated?.Invoke(room);
    }

    private void OnRoomCreatedMessage(string body)
    {
        Debug.Log($"[STOMP] Room created: {body}");
        var room = JsonConvert.DeserializeObject<RoomDTO>(body);
        OnRoomJoined?.Invoke(room);
    }

    public void Disconnect(int userId)
    {
        if (!isConnected) return;

        stompClient.Send("/app/room/disconnect", userId.ToString());
        stompClient.Disconnect();
        isConnected = false;
    }

    public void RequestOnlineUsers(int userId)
    {
        if (!isConnected)
        {
            Debug.LogWarning("[STOMP] Not connected!");
            return;
        }

        stompClient.Send("/app/room/get-online-users", userId.ToString());
    }

    public void SendInvite(long roomId, long fromUserId, string fromUsername, long toUserId)
    {
        var invite = new RoomInviteDTO
        {
            roomId = roomId,
            fromUserId = fromUserId,
            fromUsername = fromUsername,
            toUserId = toUserId
        };

        string json = JsonConvert.SerializeObject(invite);
        stompClient.Send("/app/room/send-invite", json);
    }

    public void AcceptInvite(long inviteId)
    {
        stompClient.Send("/app/room/accept-invite", inviteId.ToString());
    }

    public void DeclineInvite(long inviteId)
    {
        stompClient.Send("/app/room/decline-invite", inviteId.ToString());
    }

    private void OnOnlineUsersMessage(string body)
    {
        Debug.Log($"[STOMP] Online users: {body}");
        var users = JsonConvert.DeserializeObject<List<OnlineUserDTO>>(body);
        OnOnlineUsersUpdated?.Invoke(users);
    }

    private void OnInviteMessage(string body)
    {
        Debug.Log($"[STOMP] Invite received: {body}");
        var invite = JsonConvert.DeserializeObject<RoomInviteDTO>(body);
        OnInviteReceived?.Invoke(invite);
    }

    private void OnInviteResponseMessage(string body)
    {
        Debug.Log($"[STOMP] Invite response: {body}");
        var response = JsonConvert.DeserializeObject<RoomInviteDTO>(body);
        OnInviteResponseReceived?.Invoke(response);
    }

    public void CreateRoom(RoomDTO roomData)
    {
        if (!isConnected)
        {
            Debug.LogWarning("[STOMP] Not connected!");
            return;
        }

        string json = JsonConvert.SerializeObject(roomData);
        stompClient.Send("/app/room/create", json);
    }

    public void LeaveRoom(long userId)
    {
        if (!isConnected)
        {
            Debug.LogWarning("[STOMP] Not connected!");
            return;
        }

        stompClient.Send("/app/room/leave", userId.ToString());
    }

    public void StartMatch(List<long> userIds)
    {
        string json = JsonConvert.SerializeObject(userIds);
        stompClient.Send("/app/room/start-match", json);
    }

    public void EndMatch(List<long> userIds)
    {
        string json = JsonConvert.SerializeObject(userIds);
        stompClient.Send("/app/room/end-match", json);
    }

    private void OnDestroy()
    {
        if (isConnected)
        {
            stompClient.Disconnect();
        }
    }

    public void JoinRoomByIdWithFullInfo(long roomId, RoomDTO currentRoomData)
    {
        if (!isConnected)
        {
            Debug.LogWarning("[STOMP] Not connected!");
            return;
        }

        int userId = PlayerPrefs.GetInt("userId", 1);

        var memberInfo = new RoomMemberDTO
        {
            userId = userId,
            username = PlayerPrefs.GetString("Username", "Player"),
            avatarId = PlayerPrefs.GetInt("AvatarId", 1).ToString(),
            level = PlayerPrefs.GetInt("UserLevel", 1),
            host = false,
            ready = false,

            energy = currentRoomData.energy,
            energyFull = currentRoomData.energyFull,
            count = currentRoomData.count,
            requestPass = currentRoomData.requestPass,
            requestAttack = currentRoomData.requestAttack,
            petId = currentRoomData.petId,
            enemyPetId = currentRoomData.enemyPetId,
            nameEnemyPetId = currentRoomData.nameEnemyPetId,
            elementType = currentRoomData.elementType,
        };

        var data = new Dictionary<string, object>
        {
            { "roomId", roomId },
            { "memberInfo", memberInfo }
        };

        string json = JsonConvert.SerializeObject(data);
        stompClient.Send("/app/room/join-with-full-info", json);

        Debug.Log($"[STOMP] Sent join request with full info for room {roomId}");
    }

}