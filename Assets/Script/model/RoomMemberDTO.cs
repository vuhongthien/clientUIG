using System;
using System.Collections.Generic;

[Serializable]
public class RoomMemberDTO
{
    // ✅ MEMBER INFO (multiplayer)
    public long userId;
    public string username;
    public string avatarId;
    public int level;
    public bool ready;
    public bool host;

    // ✅ USER GAME INFO (từ UserDTO/RoomDTO)
    public int energy;
    public int energyFull;
    public int count;
    public int requestPass;
    public int requestAttack;
    public int petId;
    public int enemyPetId;
    public string nameEnemyPetId;
    public string elementType;
    public List<CardData> cards;
    public List<CardData> cardsSelected;
    public List<PetUserDTO> userPets; 

    public RoomMemberDTO()
    {
        cardsSelected = new List<CardData>();
    }
}