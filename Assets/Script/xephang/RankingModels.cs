using System;
using System.Collections.Generic;

[Serializable]
public class TopRankingData
{
    public long userId;
    public string userName;
    public long currentPetId;
    public long avtId; // ID của avatar
    public int level;
    public int totalCombatPower;
    public int rank;
}

[Serializable]
public class UserDetailData
{
    public long userId;
    public string userName;
    public int level;
    public long currentPetId;
    public long avtId; // ID của avatar
    public PetDetailInfo currentPet;
    public List<UserPetInfo> allPets;
    public List<StoneInfo> stones;
}

[Serializable]
public class PetDetailInfo
{
    public long petId;
    public string petName;
    public int level;
    public int attack;
    public int hp;
    public int mana;
}

[Serializable]
public class UserPetInfo
{
    public long petId;
    public int level;
    public string elementType;
}

[Serializable]
public class StoneInfo
{
    public long stoneId;
    public string stoneName;
    public int count;
    public string elementType;
    public int level;
}