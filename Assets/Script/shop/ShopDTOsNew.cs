using System;
using System.Collections.Generic;

// =====================================================
// SHOP DTOs - Unity Version
// =====================================================

[Serializable]
public class ShopItemDTO
{
    public int id; // shop.id trong database (QUAN TRỌNG - dùng để mua)
    public string itemType;
    public string name;
    public int price;
    public string currencyType;
    public int value;
    public string elementType; // Cho stone
    public int level; // Cho stone
    public long cardId; // Cho card
    public bool owned; // Luôn false cho items
}

[Serializable]
public class ShopPetDTO
{
    public long id; // pet.id
    public long shopId;
    public string name;
    public int attack;
    public int hp;
    public int mana;
    public int price;
    public string currencyType;
    public string elementType;
    public int purchaseCount; // 0 hoặc 1
    public int maxPurchasePerDay; // 1
    public bool canPurchase;
}

[Serializable]
public class ShopAvatarDTO
{
    public long id; // avatar.id
    public long shopId; 
    public string name;
    public int hp;
    public int attack;
    public int mana;
    public int price;
    public string currencyType;
    public bool owned;
}

[Serializable]
public class ShopDataResponse
{
    public List<ShopItemDTO> items;
    public List<ShopPetDTO> pets;
    public List<ShopAvatarDTO> avatars;
}

[Serializable]
public class PurchaseRequest
{
    public int userId;
    public long shopId; // ⚠️ QUAN TRỌNG: shopId thay vì itemId
}

[Serializable]
public class PurchaseResponse
{
    public bool success;
    public string message;
    public int newGold;
    public int newRuby;
}
