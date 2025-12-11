using System;
using System.Collections.Generic;
using UnityEngine;

namespace PokiGame.LegendPet
{
    // ===== PET DATA MODELS =====
    
    [Serializable]
    public class LegendPetData
    {
        public long petId;
        public string name;
        public string description;
        public int totalStars;
        public int inlaidStars;
        public bool unlocked;
        public List<ImageHTData> images;
    }

    [Serializable]
    public class ImageHTData
    {
        public int imageIndex;
        public List<StarSlotData> starSlots;
    }

    [Serializable]
    public class StarSlotData
    {
        public long slotId;
        public int starType; // 1=white, 2=blue, 3=red
        public int slotPosition;
        public int requiredStarCount;
        public bool inlaid;
        public bool canInlay;
    }

    // ===== INLAY STAR =====
    
    [Serializable]
    public class InlayStarRequest
    {
        public long userId;
        public long petId;
        public long slotId;
    }

    [Serializable]
    public class InlayStarResponse
    {
        public bool success;
        public string message;
        public int remainingWhiteStars;
        public int remainingBlueStars;
        public int remainingRedStars;
        public bool petUnlocked;
    }

    // ===== USER STAR INFO =====
    
    [Serializable]
    public class UserStarInfo
    {
        public int starWhite;
        public int starBlue;
        public int starRed;
    }

    // ===== PET LIST RESPONSES - MULTIPLE FORMATS =====

    /// <summary>
    /// Format 1: Object wrapper với field "pets"
    /// {"pets": [...]}
    /// </summary>
    [Serializable]
    public class LegendPetListResponse
    {
        public LegendPetBasicInfo[] pets;
    }

    /// <summary>
    /// Format 2: Object wrapper với field "data"
    /// {"data": [...]}
    /// </summary>
    [Serializable]
    public class LegendPetListResponseData
    {
        public LegendPetBasicInfo[] data;
    }

    /// <summary>
    /// Format 3: Nested data
    /// {"data": {"pets": [...]}}
    /// </summary>
    [Serializable]
    public class LegendPetListResponseNested
    {
        public PetDataWrapper data;
    }

    [Serializable]
    public class PetDataWrapper
    {
        public LegendPetBasicInfo[] pets;
    }

    /// <summary>
    /// Format 4: With success flag
    /// {"success": true, "pets": [...]}
    /// </summary>
    [Serializable]
    public class LegendPetListResponseWithSuccess
    {
        public bool success;
        public string message;
        public LegendPetBasicInfo[] pets;
    }

    // ===== BASIC PET INFO =====
    
    [Serializable]
    public class LegendPetBasicInfo
    {
        public long id;
        public string name;
        public string description;
        public bool unlocked;
        public int progress; // 0-100
    }

    // ===== HELPER CLASS TO PARSE ANY FORMAT =====
    
    public static class LegendPetResponseParser
    {
        /// <summary>
        /// Thử parse response với nhiều format khác nhau
        /// </summary>
        public static LegendPetBasicInfo[] ParseResponse(string jsonText)
        {
            if (string.IsNullOrEmpty(jsonText))
            {
                Debug.LogError("[Parser] Empty JSON");
                return null;
            }

            Debug.Log($"[Parser] Trying to parse JSON: {jsonText.Substring(0, Mathf.Min(200, jsonText.Length))}...");

            // Try Format 1: {"pets": [...]}
            try
            {
                var response1 = JsonUtility.FromJson<LegendPetListResponse>(jsonText);
                if (response1?.pets != null && response1.pets.Length > 0)
                {
                    Debug.Log($"[Parser] ✅ Parsed as Format 1 (pets): {response1.pets.Length} pets");
                    return response1.pets;
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"[Parser] Format 1 failed: {ex.Message}");
            }

            // Try Format 2: {"data": [...]}
            try
            {
                var response2 = JsonUtility.FromJson<LegendPetListResponseData>(jsonText);
                if (response2?.data != null && response2.data.Length > 0)
                {
                    Debug.Log($"[Parser] ✅ Parsed as Format 2 (data): {response2.data.Length} pets");
                    return response2.data;
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"[Parser] Format 2 failed: {ex.Message}");
            }

            // Try Format 3: {"data": {"pets": [...]}}
            try
            {
                var response3 = JsonUtility.FromJson<LegendPetListResponseNested>(jsonText);
                if (response3?.data?.pets != null && response3.data.pets.Length > 0)
                {
                    Debug.Log($"[Parser] ✅ Parsed as Format 3 (nested): {response3.data.pets.Length} pets");
                    return response3.data.pets;
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"[Parser] Format 3 failed: {ex.Message}");
            }

            // Try Format 4: {"success": true, "pets": [...]}
            try
            {
                var response4 = JsonUtility.FromJson<LegendPetListResponseWithSuccess>(jsonText);
                if (response4?.pets != null && response4.pets.Length > 0)
                {
                    Debug.Log($"[Parser] ✅ Parsed as Format 4 (with success): {response4.pets.Length} pets");
                    return response4.pets;
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"[Parser] Format 4 failed: {ex.Message}");
            }

            // Try Format 5: Direct array [...]
            try
            {
                // JsonUtility không support array trực tiếp, cần wrap
                string wrappedJson = $"{{\"pets\":{jsonText}}}";
                var response5 = JsonUtility.FromJson<LegendPetListResponse>(wrappedJson);
                if (response5?.pets != null && response5.pets.Length > 0)
                {
                    Debug.Log($"[Parser] ✅ Parsed as Format 5 (direct array): {response5.pets.Length} pets");
                    return response5.pets;
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"[Parser] Format 5 failed: {ex.Message}");
            }

            Debug.LogError("[Parser] ❌ All formats failed!");
            return null;
        }
    }
}