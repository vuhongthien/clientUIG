// using System;
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;

// [System.Serializable]
// public class RewardStone
// {
//     public int level;      // 1-7
//     public string element; // Fire, Water, Earth, Wind, Electric
//     public int quantity;   // Số lượng
// }

// [System.Serializable]
// public class GameReward
// {
//     public List<RewardStone> stones = new List<RewardStone>();
//     public bool receivedPet = false;
//     public string petElement;
//     public int petId;
// }

// public class GameResultManager : MonoBehaviour
// {
//     [Header("References")]
//     public Active active;
//     public Board board;

//     [Header("UI References")]
//     public GameObject panelResult;           // PanelResult root
//     public GameObject background;            // Background image
//     public GameObject rewards;               // Rewards container
//     public GameObject anmtRW;                // Animation BlindBox
//     public GameObject listReward;            // ListReward container
//     public GameObject itemRewardStone;       // Prefab stone
//     public GameObject itemRewardPet;         // Prefab pet
//     public Text txtResultTitle;              // Text "Victory/Defeat" (optional)
//     public Button btnGet;                    // Button continue (optional)

//     [Header("Reward Settings - 5 Elements x 7 Levels")]
//     [Tooltip("Fire Stones: Index 0-6 = Lv1-7")]
//     public Sprite[] fireStones = new Sprite[7];

//     [Tooltip("Water Stones: Index 0-6 = Lv1-7")]
//     public Sprite[] waterStones = new Sprite[7];

//     [Tooltip("Earth Stones: Index 0-6 = Lv1-7")]
//     public Sprite[] earthStones = new Sprite[7];

//     [Tooltip("Wind Stones: Index 0-6 = Lv1-7")]
//     public Sprite[] windStones = new Sprite[7];

//     [Tooltip("Electric Stones: Index 0-6 = Lv1-7")]
//     public Sprite[] electricStones = new Sprite[7];

//     [Header("Debug Info")]
//     private bool isGameOver = false;
//     private string enemyPetElement;
//     private int enemyPetId;
//     private int currentCount;
//     private int requestPass;

//     public static GameResultManager Instance { get; private set; }

//     private void Awake()
//     {
//         if (Instance == null)
//         {
//             Instance = this;
//         }
//         else
//         {
//             Destroy(gameObject);
//         }
//     }

//     void Start()
//     {
//         active = FindFirstObjectByType<Active>();
//         board = FindFirstObjectByType<Board>();

//         // Load thông tin từ PlayerPrefs
//         enemyPetElement = PlayerPrefs.GetString("elementType", "Fire");
//         enemyPetId = PlayerPrefs.GetInt("SelectedPetId", 1);
//         currentCount = PlayerPrefs.GetInt("count", 0);
//         requestPass = PlayerPrefs.GetInt("requestPass", 5);

//         // Ẩn panel kết quả ban đầu
//         if (panelResult != null)
//         {
//             panelResult.SetActive(false);
//         }


//         Debug.Log($"[GAME] Initialized: count={currentCount}/{requestPass}, enemyPet={enemyPetId}, element={enemyPetElement}");
//     }

//     // ==================== REWARD CALCULATION ====================

//     private GameReward CalculateReward(bool playerWon, int turnCount)
//     {
//         GameReward reward = new GameReward();
//         reward.petElement = enemyPetElement;
//         reward.petId = enemyPetId;

//         if (!playerWon)
//         {
//             // Thua: chỉ nhận 3 viên đá lv1
//             reward.stones.Add(new RewardStone
//             {
//                 level = 1,
//                 element = enemyPetElement,
//                 quantity = 3
//             });
//             Debug.Log($"[REWARD] Defeat: 3x Lv1 stones ({enemyPetElement})");
//             return reward;
//         }

//         // ========== THẮNG: Kiểm tra điều kiện nhận thưởng ==========

//         if (currentCount >= requestPass)
//         {
//             // Đủ điều kiện: CHỈ nhận pet
//             reward.receivedPet = true;
//             Debug.Log($"[REWARD] Victory: Received Enemy Pet! ({enemyPetElement}, ID: {enemyPetId})");
//             return reward;
//         }

//         // Chưa đủ điều kiện: Nhận đá
//         int totalStones = CalculateTotalStones(turnCount);
//         DistributeStones(reward, totalStones, turnCount);

//         return reward;
//     }

//     /// <summary>
//     /// CÔNG THỨC TÍNH TỔNG SỐ ĐÁ:
//     /// - Càng ít turn (đánh nhanh) = càng nhiều thưởng
//     /// - Formula: Total = 10 - (turnCount / 5)
//     /// - Min: 5 viên, Max: 10 viên
//     /// 
//     /// Turn 1-5:   10 viên
//     /// Turn 6-10:   9 viên
//     /// Turn 11-15:  8 viên
//     /// Turn 16-20:  7 viên
//     /// Turn 21-25:  6 viên
//     /// Turn 26+:    5 viên
//     /// </summary>
//     private int CalculateTotalStones(int turnCount)
//     {
//         int total = 10 - (turnCount / 5);
//         return Mathf.Clamp(total, 5, 10);
//     }

//     /// <summary>
//     /// PHÂN BỔ ĐÁ THEO 7 LEVEL
//     /// - Hệ thống 7 level: Lv1 (thường) → Lv7 (siêu hiếm)
//     /// - Xác suất giảm dần từ Lv1 → Lv7
//     /// - Turn càng ít = xác suất đá cao cấp càng lớn
//     /// </summary>
//     private void DistributeStones(GameReward reward, int totalStones, int turnCount)
//     {
//         Dictionary<int, int> stoneCounts = new Dictionary<int, int>();
//         for (int i = 1; i <= 7; i++)
//         {
//             stoneCounts[i] = 0;
//         }

//         Debug.Log($"[REWARD] Turn: {turnCount}, Total: {totalStones} stones");

//         // Random phân bổ từng viên
//         for (int i = 0; i < totalStones; i++)
//         {
//             int level = RollStoneLevel(turnCount);
//             stoneCounts[level]++;
//         }

//         // Thêm vào reward (từ cao → thấp để hiển thị đẹp)
//         for (int level = 7; level >= 1; level--)
//         {
//             if (stoneCounts[level] > 0)
//             {
//                 reward.stones.Add(new RewardStone
//                 {
//                     level = level,
//                     element = enemyPetElement,
//                     quantity = stoneCounts[level]
//                 });
//                 Debug.Log($"[REWARD] Lv{level}: {stoneCounts[level]} stones");
//             }
//         }
//     }

//     /// <summary>
//     /// ROLL RANDOM LEVEL DỰA TRÊN TURN
//     /// - Turn càng ít → xác suất đá cao cấp càng cao
//     /// - Sử dụng weighted random
//     /// </summary>
//     private int RollStoneLevel(int turnCount)
//     {
//         float[] weights = new float[7];

//         // ========== CÔNG THỨC WEIGHT THEO TURN ==========

//         if (turnCount <= 10)
//         {
//             // Turn 1-10: Dễ ra đá cao
//             weights[0] = 100f; // Lv1: 100%
//             weights[1] = 90f;  // Lv2: 90%
//             weights[2] = 50f;  // Lv3: 50%
//             weights[3] = 30f;  // Lv4: 30%
//             weights[4] = 15f;  // Lv5: 15%
//             weights[5] = 5f;   // Lv6: 5%
//             weights[6] = 2f;   // Lv7: 2%
//         }
//         else if (turnCount <= 20)
//         {
//             // Turn 11-20: Trung bình
//             weights[0] = 100f; // Lv1: 100%
//             weights[1] = 80f;  // Lv2: 80%
//             weights[2] = 40f;  // Lv3: 40%
//             weights[3] = 20f;  // Lv4: 20%
//             weights[4] = 8f;   // Lv5: 8%
//             weights[5] = 3f;   // Lv6: 3%
//             weights[6] = 1f;   // Lv7: 1%
//         }
//         else if (turnCount <= 30)
//         {
//             // Turn 21-30: Khó ra đá cao
//             weights[0] = 100f; // Lv1: 100%
//             weights[1] = 70f;  // Lv2: 70%
//             weights[2] = 30f;  // Lv3: 30%
//             weights[3] = 12f;  // Lv4: 12%
//             weights[4] = 5f;   // Lv5: 5%
//             weights[5] = 1.5f; // Lv6: 1.5%
//             weights[6] = 0.5f; // Lv7: 0.5%
//         }
//         else
//         {
//             // Turn 31+: Rất khó ra đá cao
//             weights[0] = 100f; // Lv1: 100%
//             weights[1] = 60f;  // Lv2: 60%
//             weights[2] = 25f;  // Lv3: 25%
//             weights[3] = 8f;   // Lv4: 8%
//             weights[4] = 3f;   // Lv5: 3%
//             weights[5] = 1f;   // Lv6: 1%
//             weights[6] = 0.3f; // Lv7: 0.3%
//         }

//         // Weighted random selection
//         float totalWeight = 0f;
//         foreach (float w in weights)
//         {
//             totalWeight += w;
//         }

//         float randomValue = UnityEngine.Random.Range(0f, totalWeight);
//         float cumulativeWeight = 0f;

//         for (int i = 0; i < 7; i++)
//         {
//             cumulativeWeight += weights[i];
//             if (randomValue <= cumulativeWeight)
//             {
//                 return i + 1; // Return level 1-7
//             }
//         }

//         return 1; // Fallback
//     }

//     // ==================== UI DISPLAY ====================

//     public IEnumerator ShowGameResult(bool playerWon, int turnCount)
//     {

//         if (isGameOver)
//         {
//             Debug.LogWarning("[GAME RESULT] Already showing result, skipping...");
//             yield break;
//         }

//         isGameOver = true;

//         Debug.Log($"[GAME RESULT] Starting display - Player {(playerWon ? "WON" : "LOST")}");
//         // 1. TẮT BÀN CỜ (Fade out effect)
//         if (board != null)
//         {
//             board.gameObject.SetActive(false);
//         }

//         yield return new WaitForSecondsRealtime(0.3f);


//         // 2. HIỂN THỊ PANELRESULT
//         if (panelResult != null)
//         {
//             panelResult.SetActive(true);

//             // Fade in effect cho background
//             if (background != null)
//             {
//                 CanvasGroup bgCanvas = background.GetComponent<CanvasGroup>();
//                 if (bgCanvas == null)
//                 {
//                     bgCanvas = background.AddComponent<CanvasGroup>();
//                 }
//                 bgCanvas.alpha = 0f;
//                 LeanTween.alphaCanvas(bgCanvas, 1f, 0.5f).setIgnoreTimeScale(true);
//             }
//         }

//         // 3. DỪNG THỜI GIAN
//         active.PauseTurn();

//         // 4. HIỂN THỊ TITLE (Victory/Defeat) - Nếu có
//         if (txtResultTitle != null)
//         {
//             txtResultTitle.text = playerWon ? $"Thắng" : $"Thua";
//         }

//         yield return new WaitForSecondsRealtime(0.5f);

//         // 5. CHẠY ANIMATION ANMTRW (BlindBox)
//         if (anmtRW != null)
//         {
//             anmtRW.SetActive(true);
//             Animator animator = anmtRW.GetComponent<Animator>();
//             if (animator != null)
//             {
//                 animator.SetTrigger("Open");
//                 Debug.Log("[ANIMATION] BlindBox opening...");
//                 // ✅ Chờ animator hoàn thành hoặc timeout sau 3s
//                 float t = 0f, timeout = 3f;
//                 yield return new WaitUntil(() =>
//                 {
//                     t += Time.unscaledDeltaTime;
//                     var st = animator.GetCurrentAnimatorStateInfo(0);
//                     return (!animator.IsInTransition(0) && st.normalizedTime >= 1f) || t >= timeout;
//                 });

//             }
//             else
//             {
//                 Debug.LogWarning("[ANIMATION] Animator not found on anmtRW!");
//             }

//             // Chờ animator hoàn thành, thay vì fix 2s
//             yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f);


//             // Ẩn anmtRW sau khi animation xong
//             anmtRW.SetActive(false);
//         }

//         // 6. TÍNH TOÁN PHẦN THƯỞNG
//         GameReward reward = CalculateReward(playerWon, turnCount);

//         // 7. HIỂN THỊ REWARDS CONTAINER
//         if (rewards != null)
//         {
//             rewards.SetActive(true);
//         }

//         // 8. HIỂN THỊ CÁC ITEMS PHẦN THƯỞNG
//         yield return StartCoroutine(DisplayRewards(reward));

//         // 9. LƯU DỮ LIỆU LÊN SERVER
//         yield return StartCoroutine(SaveRewardToServer(reward, playerWon));

//         // 10. KHÔI PHỤC THỜI GIAN (cho các animation UI tiếp theo)
//         Time.timeScale = 1f;

//         Debug.Log("[GAME RESULT] Sequence completed!");
//     }

//     private IEnumerator DisplayRewards(GameReward reward)
//     {
//         if (listReward == null)
//         {
//             Debug.LogWarning("[REWARD] ListReward not assigned in Inspector!");
//             yield break;
//         }

//         // Xóa phần thưởng cũ (nếu có)
//         foreach (Transform child in listReward.transform)
//         {
//             Destroy(child.gameObject);
//         }

//         yield return new WaitForSecondsRealtime(0.2f);

//         if (reward.receivedPet)
//         {
//             // ========== HIỂN THỊ PET ==========
//             yield return StartCoroutine(DisplayPetReward(reward));
//         }
//         else
//         {
//             // ========== HIỂN THỊ ĐÁ ==========
//             yield return StartCoroutine(DisplayStoneRewards(reward));
//         }
//     }

//     private IEnumerator DisplayPetReward(GameReward reward)
//     {
//         if (itemRewardPet == null)
//         {
//             Debug.LogWarning("[REWARD] ItemRewardPet prefab not assigned!");
//             yield break;
//         }

//         Debug.Log($"[REWARD] Displaying Pet: {reward.petElement} (ID: {reward.petId})");

//         // Instantiate prefab pet
//         GameObject petReward = Instantiate(itemRewardPet, listReward.transform);

//         // Tìm Image component trong prefab
//         Image petIcon = petReward.transform.Find("Image")?.GetComponent<Image>();

//         if (petIcon != null)
//         {
//             // Load sprite từ Resources
//             string petPath = "Image/IconsPet/" + reward.petId.ToString();
//             Sprite petSprite = Resources.Load<Sprite>(petPath);

//             if (petSprite != null)
//             {
//                 petIcon.sprite = petSprite;
//                 Debug.Log($"[REWARD] Pet sprite loaded: {petPath}");
//             }
//             else
//             {
//                 Debug.LogWarning($"[REWARD] Pet sprite not found at: Resources/{petPath}");
//             }
//         }
//         else
//         {
//             Debug.LogWarning("[REWARD] 'Image' child not found in ItemRewardPet prefab!");
//         }

//         // Animation xuất hiện (Scale 0 → 1)
//         petReward.transform.localScale = Vector3.zero;

//         // Sử dụng LeanTween với unscaled time (vì timeScale = 0)
//         LeanTween.scale(petReward, Vector3.one, 0.8f)
//             .setEaseOutElastic()
//             .setIgnoreTimeScale(true)
//             .setDelay(0.3f);

//         yield return new WaitForSecondsRealtime(1.2f);

//         Debug.Log("[REWARD] Pet display completed!");
//     }

//     private IEnumerator DisplayStoneRewards(GameReward reward)
//     {
//         if (itemRewardStone == null)
//         {
//             Debug.LogWarning("[REWARD] ItemRewardStone prefab not assigned!");
//             yield break;
//         }

//         Debug.Log($"[REWARD] Displaying {reward.stones.Count} types of stones");

//         float delay = 0f;

//         // Hiển thị từng loại đá (từ level cao → thấp)
//         foreach (var stone in reward.stones)
//         {
//             for (int i = 0; i < stone.quantity; i++)
//             {
//                 // Instantiate prefab đá
//                 GameObject stoneReward = Instantiate(itemRewardStone, listReward.transform);

//                 // Tìm components trong prefab
//                 Image stoneIcon = stoneReward.transform.Find("Image")?.GetComponent<Image>();
//                 Text countText = stoneReward.transform.Find("txtCount")?.GetComponent<Text>();

//                 // Set sprite đá
//                 if (stoneIcon != null)
//                 {
//                     Sprite stoneSprite = GetStoneSprite(stone.element, stone.level);
//                     if (stoneSprite != null)
//                     {
//                         stoneIcon.sprite = stoneSprite;
//                     }
//                     else
//                     {
//                         Debug.LogWarning($"[REWARD] Stone sprite not found: {stone.element} Lv{stone.level}");
//                     }
//                 }
//                 else
//                 {
//                     Debug.LogWarning("[REWARD] 'Image' child not found in ItemRewardStone prefab!");
//                 }

//                 // Set text level
//                 if (countText != null)
//                 {
//                     countText.text = $"Lv{stone.level}";
//                     countText.color = GetStoneLevelColor(stone.level);
//                 }
//                 else
//                 {
//                     Debug.LogWarning("[REWARD] 'txtCount' child not found in ItemRewardStone prefab!");
//                 }

//                 // Animation xuất hiện với cascade (mỗi viên cách nhau 0.15s)
//                 stoneReward.transform.localScale = Vector3.zero;

//                 LeanTween.scale(stoneReward, Vector3.one, 0.4f)
//                     .setEaseOutBack()
//                     .setIgnoreTimeScale(true)
//                     .setDelay(delay);

//                 delay += 0.05f;
//             }
//         }

//         yield return new WaitForSecondsRealtime(delay + 0.5f);

//         Debug.Log("[REWARD] Stones display completed!");
//     }

//     /// <summary>
//     /// Lấy Sprite đá theo element và level
//     /// </summary>
//     private Sprite GetStoneSprite(string element, int level)
//     {
//         int index = level - 1; // Level 1-7 → Index 0-6

//         if (index < 0 || index >= 7)
//         {
//             Debug.LogError($"[REWARD] Invalid stone level: {level}");
//             return null;
//         }

//         switch (element.ToLower())
//         {
//             case "fire":
//                 return (fireStones != null && index < fireStones.Length) ? fireStones[index] : null;
//             case "water":
//                 return (waterStones != null && index < waterStones.Length) ? waterStones[index] : null;
//             case "earth":
//                 return (earthStones != null && index < earthStones.Length) ? earthStones[index] : null;
//             case "wind":
//                 return (windStones != null && index < windStones.Length) ? windStones[index] : null;
//             case "electric":
//                 return (electricStones != null && index < electricStones.Length) ? electricStones[index] : null;
//             default:
//                 Debug.LogWarning($"[REWARD] Unknown element: {element}");
//                 return null;
//         }
//     }

//     /// <summary>
//     /// Màu sắc theo level đá
//     /// </summary>
//     private Color GetStoneLevelColor(int level)
//     {
//         switch (level)
//         {
//             case 7: return new Color(1f, 0f, 1f);       // Magenta (Legendary)
//             case 6: return new Color(1f, 0.5f, 0f);     // Orange (Epic)
//             case 5: return new Color(0.5f, 0f, 1f);     // Purple (Rare)
//             case 4: return new Color(0f, 0.5f, 1f);     // Blue (Uncommon)
//             case 3: return new Color(0f, 1f, 0.5f);     // Green
//             case 2: return new Color(0.8f, 0.8f, 0.8f); // Silver
//             default: return Color.white;                // White (Common)
//         }
//     }

//     // ==================== API CALLS ====================

//     private IEnumerator SaveRewardToServer(GameReward reward, bool playerWon)
//     {
//         int userId = PlayerPrefs.GetInt("userId", 1);

//         if (reward.receivedPet)
//         {
//             // API: Thêm pet mới
//             // string apiUrl = APIConfig.ADD_PET_TO_USER(userId, reward.petId);
//             // Debug.Log($"[API] Calling: {apiUrl}");

//             // yield return APIManager.Instance.PostRequest(apiUrl, null, OnPetAddedSuccess, OnAPIError);

//             // Reset count
//             currentCount++;
//             PlayerPrefs.SetInt("count", currentCount);
//             Debug.Log($"[API] Pet added, count reset to 0");
//         }
//         else
//         {
//             // API: Thêm đá nâng cấp
//             foreach (var stone in reward.stones)
//             {
//                 // string apiUrl = APIConfig.ADD_STONE_TO_USER(userId, stone.element, stone.level, stone.quantity);
//                 // Debug.Log($"[API] Calling: {apiUrl}");

//                 // yield return APIManager.Instance.PostRequest(apiUrl, null, OnStoneAddedSuccess, OnAPIError);
//                 yield return new WaitForSecondsRealtime(0.1f); // Tránh spam API
//             }

//             // Nếu thắng, tăng count
//             if (playerWon)
//             {
//                 currentCount++;
//                 PlayerPrefs.SetInt("count", currentCount);
//                 Debug.Log($"[API] Count increased: {currentCount}/{requestPass}");
//             }
//         }

//         PlayerPrefs.Save();
//     }

//     private void OnPetAddedSuccess(string response)
//     {
//         Debug.Log("[API SUCCESS] Pet added: " + response);
//     }

//     private void OnStoneAddedSuccess(string response)
//     {
//         Debug.Log("[API SUCCESS] Stones added: " + response);
//     }

//     private void OnAPIError(string error)
//     {
//         Debug.LogError("[API ERROR] " + error);
//     }

//     // ==================== PUBLIC METHODS ====================

//     /// <summary>
//     /// Gọi từ Button để quay về menu
//     /// </summary>
//     public void ReturnToMenu()
//     {
//         Time.timeScale = 1f; // Khôi phục time trước khi chuyển scene

//         if (ManagerGame.Instance != null)
//         {
//             ManagerGame.Instance.BackScene();
//         }
//         else
//         {
//             Debug.LogWarning("[GAME] ManagerGame instance not found!");
//         }
//     }

//     /// <summary>
//     /// Gọi từ Button để chơi lại
//     /// </summary>
//     public void PlayAgain()
//     {
//         Time.timeScale = 1f; // Khôi phục time trước khi reload

//         UnityEngine.SceneManagement.SceneManager.LoadScene(
//             UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
//         );
//     }
// }

// // ==================== API CONFIG - THÊM VÀO APIConfig.cs ====================
// /*
// public static class APIConfig
// {
//     private const string BASE_URL = "https://your-api-url.com/api";
    
//     public static string ADD_PET_TO_USER(int userId, int petId)
//     {
//         return $"{BASE_URL}/users/{userId}/pets/{petId}";
//     }
    
//     public static string ADD_STONE_TO_USER(int userId, string element, int level, int quantity)
//     {
//         return $"{BASE_URL}/users/{userId}/stones?element={element}&level={level}&quantity={quantity}";
//     }
// }
// */