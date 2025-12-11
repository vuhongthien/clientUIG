using System;
using UnityEngine;

namespace PokiGame.LegendPet
{
    /// <summary>
    /// Service class để gọi API Legend Pet với debug logs
    /// </summary>
    public class LegendPetAPIService : MonoBehaviour
    {
        private static LegendPetAPIService instance;
        public static LegendPetAPIService Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("LegendPetAPIService");
                    instance = go.AddComponent<LegendPetAPIService>();
                    DontDestroyOnLoad(go);
                    Debug.Log("[LegendPetAPIService] Instance created");
                }
                return instance;
            }
        }

        /// <summary>
        /// Lấy danh sách tất cả Pet Huyền Thoại
        /// </summary>
        public void GetAllLegendPets(Action<LegendPetListResponse> onSuccess, Action<string> onError)
        {
            Debug.Log("[LegendPetAPIService] GetAllLegendPets called");
            
            // Kiểm tra APIManager
            if (APIManager.Instance == null)
            {
                string error = "APIManager.Instance is NULL!";
                Debug.LogError($"[LegendPetAPIService] ❌ {error}");
                onError?.Invoke(error);
                return;
            }

            // Kiểm tra URL
            string url = APIConfig.GET_ALL_LEGEND_PETS;
            if (string.IsNullOrEmpty(url))
            {
                string error = "GET_ALL_LEGEND_PETS URL is empty!";
                Debug.LogError($"[LegendPetAPIService] ❌ {error}");
                onError?.Invoke(error);
                return;
            }

            Debug.Log($"[LegendPetAPIService] Calling API: {url}");
            
            try
            {
                // Gọi API với raw text trước để xem response
                StartCoroutine(GetAllLegendPetsWithRawResponse(url, onSuccess, onError));
                
                Debug.Log("[LegendPetAPIService] Coroutine started");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LegendPetAPIService] ❌ Exception: {ex.Message}\n{ex.StackTrace}");
                onError?.Invoke(ex.Message);
            }
        }

        /// <summary>
        /// Coroutine để get raw response text
        /// </summary>
        private System.Collections.IEnumerator GetAllLegendPetsWithRawResponse(
            string url, 
            Action<LegendPetListResponse> onSuccess, 
            Action<string> onError)
        {
            Debug.Log($"[LegendPetAPIService] Starting request to: {url}");
            
            using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    string rawText = request.downloadHandler.text;
                    Debug.Log($"[LegendPetAPIService] ✅ Raw Response ({rawText.Length} chars):");
                    Debug.Log($"[LegendPetAPIService] Response Text: {rawText}");

                    try
                    {
                        // Dùng parser để thử nhiều format
                        LegendPetBasicInfo[] pets = LegendPetResponseParser.ParseResponse(rawText);
                        
                        if (pets == null || pets.Length == 0)
                        {
                            Debug.LogError("[LegendPetAPIService] ❌ No pets found in response or parse failed");
                            Debug.LogError($"[LegendPetAPIService] Raw text was: {rawText}");
                            onError?.Invoke("No pets found or invalid format");
                        }
                        else
                        {
                            Debug.Log($"[LegendPetAPIService] ✅ Successfully parsed {pets.Length} pets");
                            
                            // Tạo response object
                            LegendPetListResponse response = new LegendPetListResponse
                            {
                                pets = pets
                            };
                            
                            onSuccess?.Invoke(response);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[LegendPetAPIService] ❌ Parse error: {ex.Message}");
                        Debug.LogError($"[LegendPetAPIService] Stack trace: {ex.StackTrace}");
                        onError?.Invoke($"Parse error: {ex.Message}");
                    }
                }
                else
                {
                    string error = $"HTTP Error: {request.error}";
                    Debug.LogError($"[LegendPetAPIService] ❌ {error}");
                    onError?.Invoke(error);
                }
            }
        }

        /// <summary>
        /// Lấy thông tin Pet Huyền Thoại
        /// </summary>
        public void GetLegendPetInfo(long userId, long petId, Action<LegendPetData> onSuccess, Action<string> onError)
        {
            Debug.Log($"[LegendPetAPIService] GetLegendPetInfo called - UserID: {userId}, PetID: {petId}");
            
            string url = APIConfig.GET_LEGEND_PET_INFO((int)userId, (int)petId);
            Debug.Log($"[LegendPetAPIService] URL: {url}");
            
            try
            {
                StartCoroutine(APIManager.Instance.GetRequest<LegendPetData>(
                    url,
                    (response) => {
                        Debug.Log($"[LegendPetAPIService] ✅ GetLegendPetInfo Success!");
                        onSuccess?.Invoke(response);
                    },
                    (error) => {
                        Debug.LogError($"[LegendPetAPIService] ❌ GetLegendPetInfo Error: {error}");
                        onError?.Invoke(error);
                    }
                ));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LegendPetAPIService] ❌ Exception: {ex.Message}");
                onError?.Invoke(ex.Message);
            }
        }

        /// <summary>
        /// Khảm sao
        /// </summary>
        public void InlayStar(InlayStarRequest requestData, Action<InlayStarResponse> onSuccess, Action<string> onError)
        {
            Debug.Log($"[LegendPetAPIService] InlayStar called - SlotID: {requestData.slotId}");
            
            string url = APIConfig.POST_INLAY_STAR;
            Debug.Log($"[LegendPetAPIService] URL: {url}");
            
            try
            {
                StartCoroutine(APIManager.Instance.PostRequest_Generic<InlayStarResponse>(
                    url, 
                    requestData,
                    (response) => {
                        Debug.Log($"[LegendPetAPIService] ✅ InlayStar Success!");
                        onSuccess?.Invoke(response);
                    },
                    (error) => {
                        Debug.LogError($"[LegendPetAPIService] ❌ InlayStar Error: {error}");
                        onError?.Invoke(error);
                    }
                ));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LegendPetAPIService] ❌ Exception: {ex.Message}");
                onError?.Invoke(ex.Message);
            }
        }

        /// <summary>
        /// Mở khóa Pet Huyền Thoại (sau khi khảm đủ sao)
        /// </summary>
        public void UnlockLegendPet(long userId, long petId, Action<UnlockPetResponse> onSuccess, Action<string> onError)
        {
            Debug.Log($"[LegendPetAPIService] 🔓 UnlockLegendPet called - UserID: {userId}, PetID: {petId}");

            string url = APIConfig.POST_UNLOCK_LEGEND_PET;
            Debug.Log($"[LegendPetAPIService] URL: {url}");

            // Tạo request data
            UnlockPetRequest requestData = new UnlockPetRequest
            {
                userId = userId,
                petId = petId
            };

            try
            {
                StartCoroutine(APIManager.Instance.PostRequest_Generic<UnlockPetResponse>(
                    url,
                    requestData,
                    (response) =>
                    {
                        Debug.Log($"[LegendPetAPIService] ✅ UnlockLegendPet Success!");
                        Debug.Log($"[LegendPetAPIService] Message: {response.message}");
                        onSuccess?.Invoke(response);
                    },
                    (error) =>
                    {
                        Debug.LogError($"[LegendPetAPIService] ❌ UnlockLegendPet Error: {error}");
                        onError?.Invoke(error);
                    }
                ));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LegendPetAPIService] ❌ Exception: {ex.Message}");
                onError?.Invoke(ex.Message);
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết một pet cụ thể của user
        /// </summary>
        public void GetUserPetInfo(int userId, int petId, Action<PetUserDTO> onSuccess, Action<string> onError)
        {
            Debug.Log($"[LegendPetAPIService] GetUserPetInfo called - UserID: {userId}, PetID: {petId}");

            string url = APIConfig.GET_PET_USERS(userId, petId);
            Debug.Log($"[LegendPetAPIService] URL: {url}");

            try
            {
                StartCoroutine(GetUserPetInfoWithRawResponse(url, onSuccess, onError));
                Debug.Log("[LegendPetAPIService] GetUserPetInfo coroutine started");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LegendPetAPIService] ❌ Exception: {ex.Message}");
                onError?.Invoke(ex.Message);
            }
        }
private System.Collections.IEnumerator GetUserPetInfoWithRawResponse(
    string url, 
    Action<PetUserDTO> onSuccess, 
    Action<string> onError)
{
    Debug.Log($"[LegendPetAPIService] Starting GetUserPetInfo request to: {url}");
    
    using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(url))
    {
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();

        if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            string rawText = request.downloadHandler.text;
            Debug.Log($"[LegendPetAPIService] ✅ GetUserPetInfo Raw Response ({rawText.Length} chars):");
            Debug.Log($"[LegendPetAPIService] Response Text: {rawText}");

            try
            {
                // Parse trực tiếp vì response là object không có wrapper
                PetUserDTO pet = JsonUtility.FromJson<PetUserDTO>(rawText);
                
                if (pet == null || pet.id == 0)
                {
                    Debug.LogWarning("[LegendPetAPIService] ⚠️ User pet not found or invalid");
                    onError?.Invoke("User pet not found");
                }
                else
                {
                    Debug.Log($"[LegendPetAPIService] ✅ Successfully parsed user pet:");
                    Debug.Log($"  - ID: {pet.id}");
                    Debug.Log($"  - UserID: {pet.userId}");
                    Debug.Log($"  - PetID: {pet.petId}");
                    Debug.Log($"  - Name: {pet.name ?? "NULL"}");
                    Debug.Log($"  - Level: {pet.level}/{pet.maxLevel}");
                    Debug.Log($"  - HP: {pet.hp}");
                    Debug.Log($"  - Attack: {pet.attack}");
                    Debug.Log($"  - Mana: {pet.mana}");
                    Debug.Log($"  - Element: {pet.elementType} / {pet.elementOther}");
                    Debug.Log($"  - WeaknessValue: {pet.weaknessValue}");
                    Debug.Log($"  - SkillCardId: {pet.skillCardId}");
                    Debug.Log($"  - ManaSkillCard: {pet.manaSkillCard}");
                    
                    onSuccess?.Invoke(pet);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LegendPetAPIService] ❌ Parse error: {ex.Message}");
                Debug.LogError($"[LegendPetAPIService] Stack trace: {ex.StackTrace}");
                Debug.LogError($"[LegendPetAPIService] Raw response was: {rawText}");
                onError?.Invoke($"Parse error: {ex.Message}");
            }
        }
        else
        {
            string error = $"HTTP Error: {request.error}";
            Debug.LogError($"[LegendPetAPIService] ❌ {error}");
            onError?.Invoke(error);
        }
    }
}
    }

    // ===== REQUEST & RESPONSE MODELS =====

    /// <summary>
    /// Request để unlock pet
    /// </summary>
    [Serializable]
    public class UnlockPetRequest
    {
        public long userId;
        public long petId;
    }

    /// <summary>
    /// Response sau khi unlock pet
    /// </summary>
    [Serializable]
    public class UnlockPetResponse
    {
        public bool success;
        public string message;
        
        // Optional: Thông tin pet sau khi unlock
        public LegendPetData petData;
        
        // Optional: Rewards khi unlock pet
        public UnlockRewards rewards;
    }

    /// <summary>
    /// Phần thưởng khi unlock pet (optional)
    /// </summary>
    [Serializable]
    public class UnlockRewards
    {
        public int coins;
        public int gems;
        public int experience;
        public string[] items; // Các item đặc biệt
    }
}