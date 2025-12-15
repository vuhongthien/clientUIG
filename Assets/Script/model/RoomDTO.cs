using System;
using System.Collections.Generic;

[Serializable]
public class RoomDTO
{
    // ✅ EXISTING FIELDS - GIỮ NGUYÊN
    public int id;
    public int energy;
    public int energyFull;
    public int count;
    public int requestPass;
    public int requestAttack;
    public string name;
    public int lever;
    public int petId;
    public int enemyPetId;
    public string nameEnemyPetId;
    public string elementType;

    // ✅ NEW FIELDS - THÊM MỚI CHO MULTIPLAYER
    public long roomId;           // ID của room (để join)
    public long hostUserId;       // ID của người tạo room
    public string hostUsername;   // Tên của người tạo room
    public List<RoomMemberDTO> members;  // Danh sách members trong room
    public string status;         // WAITING, READY, IN_MATCH
    public int maxPlayers;        // Số lượng player tối đa (default: 2)

    public RoomDTO()
    {
        members = new List<RoomMemberDTO>();
        status = "WAITING";
        maxPlayers = 2;
    }

    // ✅ Helper method để tương thích với backend Java
    public bool isFull()
    {
        return members != null && members.Count >= maxPlayers;
    }
}