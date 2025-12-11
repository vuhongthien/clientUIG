using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enum loại thẻ - mapping với backend ElementTypeCard
/// </summary>
public enum ElementTypeCard
{
    HEALTH,     // Hồi máu
    ATTACK,     // Tấn công
    MANA,       // Hồi mana
    POWER       // Hồi nộ/power
}

/// <summary>
/// Data class cho Card - mapping với CardDTO từ backend
/// </summary>
[Serializable]
public class CardData
{
    public long id;              // ID trong bảng user_card
    public long cardId;          // ID của loại card
    public string name;          // Tên card
    public string description;   // Mô tả
    public string elementTypeCard; // "HEALTH", "ATTACK", "MANA", "POWER"
    public int value;            // Giá trị hiệu ứng (VD: +100 HP, -200 dame)
    public int maxLevel;         // Level tối đa
    public int count;            // Số lượng còn lại
    public int level;            // Level hiện tại
    public long conditionUse;    // Điều kiện sử dụng (VD: cần 50 mana)
    public long power;    // Điều kiện sử dụng (VD: cần 50 nộ)
    public int green;
    public int blue;
    public int red ;
    public int yellow ;
    public int white ;

    /// <summary>
    /// Parse elementTypeCard string sang enum
    /// </summary>
    public ElementTypeCard GetElementType()
    {
        if (Enum.TryParse(elementTypeCard, true, out ElementTypeCard result))
        {
            return result;
        }
        return ElementTypeCard.HEALTH; // Default
    }

    /// <summary>
    /// Kiểm tra card có thể sử dụng không (còn số lượng)
    /// </summary>
    public bool CanUse()
    {
        return count > 0;
    }
}

/// <summary>
/// Wrapper cho danh sách card (dùng cho JSON serialize)
/// </summary>
[Serializable]
public class CardListWrapper
{
    public List<CardData> cards = new List<CardData>();
}

/// <summary>
/// DTO để gửi lên API khi sử dụng card
/// </summary>
[Serializable]
public class UseCardRequest
{
    public long userId;
    public long cardId;
    public int quantity = 1;
}

/// <summary>
/// Response từ API sau khi sử dụng card
/// </summary>
[Serializable]
public class UseCardResponse
{
    public bool success;
    public string message;
    public int remainingCount;
}
