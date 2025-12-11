using System;
using System.Collections.Generic;

// ===== PET EQUIPMENT DTO =====
[Serializable]
public class PetEquipmentDTO
{
    public long id;
    public long petId;
    public string name;
    public int level;
    public int hp;
    public int attack;
    public int mana;
    public string elementType;
    public bool equipped;
}

[Serializable]
public class PetEquipmentListWrapper
{
    public List<PetEquipmentDTO> data;
}

// ===== AVATAR EQUIPMENT DTO =====
[Serializable]
public class AvatarEquipmentDTO
{
    public long id;
    public long avatarId;
    public string name;
    public int hp;
    public int attack;
    public int mana;
    public string blind;
    public bool equipped;
}

[Serializable]
public class AvatarEquipmentListWrapper
{
    public List<AvatarEquipmentDTO> data;
}

// ===== EQUIP REQUEST =====
[Serializable]
public class EquipPetRequest
{
    public long petId;
}

[Serializable]
public class EquipAvatarRequest
{
    public long avatarId;
}

// ===== EQUIP RESPONSE =====
[Serializable]
public class EquipResponse
{
    public bool success;
    public string message;
    public long equippedPetId;
    public long equippedAvatarId;
}

// ===== COUNT RESPONSE =====
[Serializable]
public class EquipmentCountDTO
{
    public int petCount;
    public int avatarCount;
    public int petPages;
    public int avatarPages;
}
