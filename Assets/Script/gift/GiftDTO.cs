using System;

[Serializable]
public class GiftDTO
{
    public int id;
    public int userId;
    public string title;
    public string description;
    public string giftType; // "INDIVIDUAL" or "ALL_USERS"
    public string status; // "PENDING", "CLAIMED", "EXPIRED"
    
    // Rewards
    public int gold;
    public int energy;
    public int exp;
    public int starWhite;
    public int starBlue;
    public int starRed;
    public int ruby;
    public int wheel;
    
    public int wheelDay;
    public int avtId;
    // Complex rewards
    public int petId;
    public string petName;
    
    public int cardId;
    public string cardName;
    
    // Multiple stones support
    public StoneRewardDTO[] stones;
    
    public string createdAt;
    public string expiredAt;
    public string claimedAt;
}

[Serializable]
public class StoneRewardDTO
{
    public int stoneId;
    public int count;
    public string stoneName;
    public string elementType; // "FIRE", "WATER", "WIND", "EARTH", "THUNDER"
    public int level; // 1-7
}

[Serializable]
public class GiftCountResponse
{
    public int count;
}