using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PhotonRoom : MonoBehaviourPunCallbacks
{
    public static PhotonRoom instance;

    public InputField input;
    // public Transform roomContent;
    // public UIRoomProfile roomPrefab;
    public List<RoomInfo> updatedRooms;
    public List<RoomProfile> rooms = new List<RoomProfile>();
    public string roomName;
    public Transform list;
    public GameObject room;
    public Button refreshButton;


    protected void Awake()
    {
        PhotonRoom.instance = this;//Dont do this in your game
        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(RefreshRoomList);
        }
    }
    public void RefreshRoomList()
{
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.JoinLobby(); // Kích hoạt cập nhật danh sách phòng
            Debug.Log("Refreshing room list...");
            this.UpdateRoomProfileUI();
    }
}

    public virtual void Create()
    {
        if (!PhotonNetwork.IsConnected) return;

        roomName = GetRoomName();
        string petId = ManagerDauTruong.Instance.petId;

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2;
        roomOptions.CustomRoomProperties = new Hashtable()
        {
            { "pet_user1", petId },
            { "pet_user2", "" }, // Thêm pet_user2 rỗng từ đầu
            { "playerCount", 1 }
        };
        roomOptions.CustomRoomPropertiesForLobby = new string[] { "pet_user1", "pet_user2", "playerCount" };

        PhotonNetwork.CreateRoom(roomName, roomOptions);
        ManagerGame.Instance.LoadScene("RoomPVP");
    }

    // Thêm callback khi có người chơi tham gia phòng
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Hashtable props = new Hashtable();

            if (newPlayer.CustomProperties.TryGetValue("petId", out object petId))
            {
                props["pet_user2"] = ManagerDauTruong.Instance.petId;
                Debug.Log("Updated pet_user2: " + petId);
            }

            props["playerCount"] = PhotonNetwork.CurrentRoom.PlayerCount;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            // CHỈ cập nhật UI nếu đang ở scene sảnh
            if (IsInLobbyScene())
            {
                UpdateRoomProfileUI();
            }
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        // Cập nhật tất cả phòng khi có thay đổi
        foreach (RoomProfile room in rooms)
        {
            if (room.name == PhotonNetwork.CurrentRoom.Name)
            {
                room.CustomProperties = PhotonNetwork.CurrentRoom.CustomProperties;
                break;
            }
        }
    }

    string GetRoomName()
    {
        string name = "";
        for (int i = 0; i < 6; i++)
        {
            name += UnityEngine.Random.Range(0, 9).ToString();
        }
        return name;
    }


    public virtual void Join(string roomName)
    {
        Debug.Log(transform.name + ": Join Room " + roomName);
        PhotonNetwork.JoinRoom(roomName);
    }

    public virtual void JoinWithId()
    {
        if (input.text.Length == 0)
        {
            input.text = "000000";
        }
        PhotonNetwork.JoinRoom(input.text);

    }
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log("Không tìm thấy phòng! Vui lòng thử lại.");

        // Hoặc hiển thị popup thông báo
    }

    public virtual void Leave()
    {
        Debug.Log(transform.name + ": Leave Room");
        PhotonNetwork.LeaveRoom();
    }


    public override void OnCreatedRoom()
    {
        Debug.Log("OnCreatedRoom");
        // ManagerGame.Instance.LoadScene("RoomPVP");

    }
    
public override void OnJoinedRoom()
    {
        Debug.Log("OnJoinedRoom: " + PhotonNetwork.CurrentRoom.Name);

        // Nếu là người chơi thứ 2, cập nhật pet_user2
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2 && !PhotonNetwork.IsMasterClient)
        {
            Hashtable props = new Hashtable();
            props["pet_user2"] = ManagerDauTruong.Instance.petId;
            props["playerCount"] = 2;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
    }
    public void UpdateRoomPropertiesForNewPlayer()
    {
        Hashtable props = new Hashtable();
        props["pet_user2"] = ManagerDauTruong.Instance.petId;
        props["playerCount"] = PhotonNetwork.CurrentRoom.PlayerCount;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Hashtable props = new Hashtable();
            props["pet_user2"] = "";
            props["playerCount"] = PhotonNetwork.CurrentRoom.PlayerCount;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            // CHỈ cập nhật UI nếu đang ở scene sảnh
            if (IsInLobbyScene())
            {
                UpdateRoomProfileUI();
            }
        }
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Đã rời phòng");

        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.JoinLobby(); // Đảm bảo tham gia lobby
            Debug.Log("chuyển về lobby...");
        }
        ManagerGame.Instance.LoadScene("DauTruong");

    }


    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log("OnCreateRoomFailed: " + message);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log("OnRoomListUpdate");
        this.updatedRooms = roomList;

        foreach (RoomInfo roomInfo in roomList)
        {
            if (roomInfo.RemovedFromList) this.RoomRemove(roomInfo);
            else this.RoomAdd(roomInfo);
        }

        // CHỈ cập nhật UI nếu đang ở scene sảnh
        if (IsInLobbyScene())
        {
            this.UpdateRoomProfileUI();
        }
    }

    protected virtual void RoomAdd(RoomInfo roomInfo)
    {
        RoomProfile roomProfile;

        roomProfile = this.RoomByName(roomInfo.Name);
        if (roomProfile != null) return;

        roomProfile = new RoomProfile
        {
            name = roomInfo.Name,
            CustomProperties = roomInfo.CustomProperties
        };
        this.rooms.Add(roomProfile);

    }
    public bool IsInLobbyScene()
    {
        return SceneManager.GetActiveScene().name == "DauTruong";
    }

    public virtual void UpdateRoomProfileUI()
    {
        // CHỈ cập nhật UI nếu đang ở scene sảnh
        if (!IsInLobbyScene()) return;

        this.ClearRoomProfileUI();

        foreach (RoomProfile roomProfile in this.rooms)
        {
            GameObject r = Instantiate(room, list);
            Text nameR = r.transform.Find("roomName")?.GetComponent<Text>();
            Image user1 = r.transform.Find("user1")?.GetComponent<Image>();
            Image user2 = r.transform.Find("user12")?.GetComponent<Image>();
            Button roomButton = r.transform.Find("btnJoin")?.GetComponent<Button>();

            roomButton.onClick.RemoveAllListeners();
            roomButton.onClick.AddListener(() => Join(roomProfile.name));

            nameR.text = roomProfile.name;

            if (roomProfile.CustomProperties != null)
            {
                // Hiển thị pet 1
                SetPetImageBasedOnProperty(user1, roomProfile, "pet_user1");

                // Hiển thị pet 2
                SetPetImageBasedOnProperty(user2, roomProfile, "pet_user2");

                // Cập nhật trạng thái nút Join
                int playerCount = (int)roomProfile.CustomProperties["playerCount"];
                roomButton.interactable = playerCount < 2;
            }
        }
    }

    // Hàm hỗ trợ mới để thiết lập ảnh pet
    private void SetPetImageBasedOnProperty(Image image, RoomProfile roomProfile, string propertyName)
{
    if (roomProfile.CustomProperties.TryGetValue(propertyName, out object petIdObj))
    {
        string petId = petIdObj?.ToString();
        
        if (!string.IsNullOrEmpty(petId))
        {
            SetPetImage(image, petId);
            image.gameObject.SetActive(true);
        }
        else
        {
            image.gameObject.SetActive(false);
        }
    }
    else
    {
        image.gameObject.SetActive(false);
    }
}

    protected virtual void UpdateRoomProfileUIJoin()
    {

        foreach (RoomProfile roomProfile in this.rooms)
        {
            GameObject r = Instantiate(room, list);
            Text nameR = r.transform.Find("roomName")?.GetComponent<Text>();
            Image user1 = r.transform.Find("user1")?.GetComponent<Image>();
            Image user2 = r.transform.Find("user12")?.GetComponent<Image>();
            Button roomButton = r.transform.Find("btnJoin")?.GetComponent<Button>();

            roomButton.onClick.RemoveAllListeners();
            roomButton.onClick.AddListener(() => Join(roomProfile.name));

            nameR.text = roomProfile.name;

            if (roomProfile.CustomProperties != null)
            {
                // Hiển thị pet 1
                if (roomProfile.CustomProperties.TryGetValue("pet_user1", out object petId1))
                {
                    SetPetImage(user1, petId1.ToString());
                }
                else
                {
                    user1.gameObject.SetActive(false);
                }

                // Hiển thị pet 2
                if (roomProfile.CustomProperties.TryGetValue("pet_user2", out object petId2))
                {
                    if (!string.IsNullOrEmpty(petId2.ToString()))
                    {
                        SetPetImage(user2, petId2.ToString());
                    }
                    else
                    {
                        user2.gameObject.SetActive(false);
                    }
                }
                else
                {
                    user2.gameObject.SetActive(false);
                }

                // Cập nhật trạng thái nút Join
                int playerCount = (int)roomProfile.CustomProperties["playerCount"];
                roomButton.interactable = playerCount < 2;
            }
        }
    }

    private void SetPetImage(Image image, string petId)
    {
        if (string.IsNullOrEmpty(petId)) return;

        Sprite petSprite = LoadPetSprite(petId);
        if (petSprite != null)
        {
            image.sprite = petSprite;
            image.gameObject.SetActive(true);
        }
        else
        {
            image.gameObject.SetActive(false);
        }
    }

    private Sprite LoadPetSprite(string petId)
    {
        return Resources.Load<Sprite>($"Image/IconsPet/{petId}");
    }

    protected virtual void ClearRoomProfileUI()
    {
        foreach (Transform child in this.list)
        {
            Destroy(child.gameObject);
        }
    }

    protected virtual void RoomRemove(RoomInfo roomInfo)
    {
        RoomProfile roomProfile = this.RoomByName(roomInfo.Name);
        if (roomProfile == null) return;
        this.rooms.Remove(roomProfile);
    }

    protected virtual RoomProfile RoomByName(string name)
    {
        foreach (RoomProfile roomProfile in this.rooms)
        {
            if (roomProfile.name == name) return roomProfile;
        }
        return null;
    }

}