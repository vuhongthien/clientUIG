using System;
using System.Collections.Generic;

[Serializable]
public class WheelConfigDTO
{
    public List<WheelPrizeDTO> prizes;
    public int spinCost;
    public int userGold;
    public int userWheel;
}

[Serializable]
public class WheelPrizeDTO
{
    public int index;
    public string prizeType;    // PET, AVATAR, GOLD, RUBY, ENERGY, STONE
    public int prizeId;
    public string prizeName;
    public int amount;
    public float chance;
    public string rarity;       // LEGENDARY, EPIC, RARE, COMMON
    public string iconPath;
    public string elementType;  // FIRE, WATER, WOOD, EARTH, METAL
    public int stoneLevel;
}

[Serializable]
public class SpinResultDTO
{
    public List<SpinRewardDTO> rewards;
    public int totalGoldSpent;
    public int remainingGold;
    public int remainingWheel;
    public bool isDuplicate;
    public int compensationGold;
    public bool success;
    public string message;
}

[Serializable]
public class SpinRewardDTO
{
    public int spinIndex;
    public int prizeIndex;
    public string prizeType;
    public int prizeId;
    public string prizeName;
    public int amount;
    public string rarity;
    public string iconPath;
    public string elementType;
    public int stoneLevel;
    public bool isDuplicate;
    public int compensationGold;
}
