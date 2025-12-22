using UnityEngine;

public class APIConfig : MonoBehaviour
{

    private static string DOMAIN = "pokiguard.online";

    private static string BASE_URL = "https://"+ DOMAIN;
    public static string VERSION = "1.0.0";
    public static string SOCKET = "wss://"+ DOMAIN + "/ws-chat";

    // GET APIs
    public static string GET_PETS_ENEMYS(int id) => $"{BASE_URL}/pets/{id}";
    public static string GET_ALL_PET = $"{BASE_URL}/pets";
    public static string GET_USER(int id) => $"{BASE_URL}/users/{id}";
    public static string GET_USER_BY_USERNAME(string userName) => $"{BASE_URL}/users/by-user-name/{userName}";
    public static string GET_ALL_PET_USERS(int id) => $"{BASE_URL}/userPets/{id}";
    public static string GET_PET_USERS(int id, int petId) => $"{BASE_URL}/userPets/info/{id}/{petId}";
    public static string GET_ROOM_USERS(int id, int idPet) => $"{BASE_URL}/users/room/{id}/{idPet}";
    public static string GET_PET_USERS_MATCH(int id, int idPet, int ePetId) => $"{BASE_URL}/userPets/match/{id}/{idPet}/{ePetId}";
    public static string GET_ENEMYPET_USERS_MATCH(int ePetId, int idPet) => $"{BASE_URL}/pets/infoEnemyPet/{ePetId}/{idPet}";
    public static string DOWN_ENERGY(int id) => $"{BASE_URL}/users/energy/{id}";
    public static string UP_CT(int id, int requestAttack) => $"{BASE_URL}/users/ct/{id}/{requestAttack}";

    // Stone API
    public static string GET_STONES(int userId) => $"{BASE_URL}/stone/{userId}";

    // POST APIs
    public static string POST_USER_LOGIN => $"{BASE_URL}/users/login";

    // ===== REWARD APIS =====
    public static string COUNT_PASS_TO_USER(int userId) => $"{BASE_URL}/reward/{userId}/countPass";
    public static string ADD_PET_TO_USER(int userId) => $"{BASE_URL}/reward/{userId}/pets";
    public static string ADD_STONE_TO_USER(int userId) => $"{BASE_URL}/reward/{userId}/stones";

    public static string UPGRADE_PET => $"{BASE_URL}/upgrade";

    public static string UPGRADE_STONE => $"{BASE_URL}/upgrade/stones";
    public static string DEDUCT_GOLD = BASE_URL + "/users/deduct-gold";
    public static string BATCH_UPGRADE_STONES = BASE_URL + "/upgrade/batch-upgrade-stones";

    // Wheel endpoints
    public static string CHECK_WHEEL(int userId) => BASE_URL + "/wheelLegend/check/" + userId;
    public static string SPIN_WHEEL(int userId) => BASE_URL + "/wheelLegend/spin/" + userId;
    public static string ADD_WHEEL(int userId, int amount) => BASE_URL + "/wheelLegend/add/" + userId + "?amount=" + amount;
    public static string UPDATE_STAR = BASE_URL + "/users/update-star";

    // Legend Pet endpoints
    public static string GET_ALL_LEGEND_PETS => $"{BASE_URL}/api/legend-pets";
    public static string GET_LEGEND_PET_INFO(int userId, int petId) => $"{BASE_URL}/api/legend-pets/{petId}/user/{userId}";
    public static string POST_INLAY_STAR => $"{BASE_URL}/api/legend-pets/inlay";
    public static readonly string POST_UNLOCK_LEGEND_PET = $"{BASE_URL}/api/legend-pets/unlock";

    // ===== WORLD BOSS APIS =====
    public static string GET_ALL_WORLD_BOSSES(int userId) => $"{BASE_URL}/api/world-boss/user/{userId}";
    public static string GET_WORLD_BOSS_DETAIL(int bossId, int userId) => $"{BASE_URL}/api/world-boss/{bossId}/user/{userId}";
    public static string POST_ATTACK_BOSS => $"{BASE_URL}/api/world-boss/attack";
    public static string GET_BOSS_ATTEMPTS(int userId, int bossId) => $"{BASE_URL}/api/world-boss/attempts/{userId}/{bossId}";

    public static string SUBMIT_BOSS_DAMAGE(long userId, long bossScheduleId)
    {
        return BASE_URL + $"/api/world-boss/{bossScheduleId}/damage?userId={userId}";
    }

    public static string GET_BOSS_RANKING(long bossScheduleId)
    {
        return BASE_URL + $"/api/world-boss/{bossScheduleId}/ranking";
    }

    public static string CLAIM_BOSS_REWARD(long userId, long bossScheduleId)
    {
        return BASE_URL + $"/api/world-boss/{bossScheduleId}/reward?userId={userId}";
    }

    public static string USE_BOSS_ATTEMPT(long userId, long bossScheduleId)
    {
        return BASE_URL + $"/api/world-boss/attempt/use?userId={userId}&bossScheduleId={bossScheduleId}";
    }

    public static string GET_BOSS_RANKING(int userId)
    {
        return BASE_URL + $"/api/boss-ranking/{userId}";
    }

    public static string CLAIM_BOSS_REWARD(int userId, int bossScheduleId)
    {
        return BASE_URL + $"/api/boss-ranking/claim-reward?userId={userId}&bossScheduleId={bossScheduleId}";
    }

    // ===== RANKING APIS =====
    public static string GET_TOP9_RANKING => $"{BASE_URL}/api/ranking/top9";
    public static string GET_USER_DETAIL(int userId) => $"{BASE_URL}/api/ranking/user/{userId}";

    public static string GET_ENERGY(int userId) => $"{BASE_URL}/api/energy/{userId}";
    public static string CONSUME_ENERGY(int userId, int amount) =>
        $"{BASE_URL}/api/energy/{userId}/consume?amount={amount}";

    /// <summary>
    /// GET /api/gifts/user/{userId}/pending
    /// Lấy danh sách quà chưa nhận
    /// </summary>
    public static string GET_PENDING_GIFTS(int userId)
    {
        return BASE_URL + $"/api/gifts/user/{userId}/pending";
    }

    /// <summary>
    /// GET /api/gifts/user/{userId}/count
    /// Đếm số quà chưa nhận
    /// </summary>
    public static string GET_GIFT_COUNT(int userId)
    {
        return BASE_URL + $"/api/gifts/user/{userId}/count";
    }

    /// <summary>
    /// GET /api/gifts/user/{userId}/claimed
    /// Lịch sử quà đã nhận
    /// </summary>
    public static string GET_CLAIMED_GIFTS(int userId)
    {
        return BASE_URL + $"/api/gifts/user/{userId}/claimed";
    }

    /// <summary>
    /// POST /api/gifts/claim/{giftId}?userId={userId}
    /// Nhận quà
    /// </summary>
    public static string CLAIM_GIFT(int giftId, int userId)
    {
        return BASE_URL + $"/api/gifts/claim/{giftId}?userId={userId}";
    }

    // === ADMIN ENDPOINTS ===

    /// <summary>
    /// POST /api/gifts/send/individual
    /// Gửi quà cho 1 user
    /// </summary>
    public static string SEND_GIFT_INDIVIDUAL()
    {
        return BASE_URL + "/api/gifts/send/individual";
    }

    /// <summary>
    /// POST /api/gifts/send/all
    /// Gửi quà cho tất cả user
    /// </summary>
    public static string SEND_GIFT_ALL()
    {
        return BASE_URL + "/api/gifts/send/all";
    }

    public static string GET_SHOP_DATA(int userId)
    {
        return $"{BASE_URL}/api/shop/{userId}";
    }

    /// <summary>
    /// Mua item trong shop
    /// POST /shop/purchase
    /// Body: { userId, itemType, itemId }
    /// </summary>
    public static string PURCHASE_ITEM()
    {
        return $"{BASE_URL}/api/shop/purchase";
    }

    public static string GET_WHEEL_CONFIG(int userId)
    {
        return BASE_URL + "/api/wheel/config/" + userId;
    }

    public static string SPIN_WHEEL_FREE(int userId)
    {
        return $"{BASE_URL}/api/wheel/spin-free/{userId}";
    }

    public static string SPIN_WHEEL_FREE(int userId, int prizeIndex)
    {
        return $"{BASE_URL}/api/wheel/spin-free/{userId}?prizeIndex={prizeIndex}";
    }

    public static string SPIN_WHEEL(int userId, int prizeIndex)
    {
        return $"{BASE_URL}/api/wheel/spin/{userId}?prizeIndex={prizeIndex}";
    }

    public static string CHECK_FREE_SPIN(int userId)
    {
        return $"{BASE_URL}/api/wheel/check-free/{userId}";
    }
    
    public static string CHECK_GOLD_SPIN(int userId)
    {
        return $"{BASE_URL}/api/wheel/check-gold/{userId}";
    }
    
    // LƯU sau khi quay xong
    public static string SAVE_FREE_SPIN(int userId, int prizeIndex)
    {
        return $"{BASE_URL}/api/wheel/save-free/{userId}?prizeIndex={prizeIndex}";
    }
    
    public static string SAVE_GOLD_SPIN(int userId, int prizeIndex)
    {
        return $"{BASE_URL}/api/wheel/save-gold/{userId}?prizeIndex={prizeIndex}";
    }

    public static string REDEEM_GIFT_CODE(long userId, string code) => 
        $"{BASE_URL}/api/giftcode/redeem?userId={userId}&code={code}";

    // ===== CARD APIS =====
    
    /// <summary>
    /// GET /api/user-cards/{userId}
    /// Lấy danh sách card của user
    /// </summary>
    public static string GET_USER_CARDS(int userId) => $"{BASE_URL}/api/user-cards/{userId}";
    
    /// <summary>
    /// POST /api/user-cards/use
    /// Sử dụng card - giảm số lượng trong DB
    /// Body: { userId, cardId, quantity }
    /// </summary>
    public static string USE_CARD => $"{BASE_URL}/api/user-cards/use";
    
    /// <summary>
    /// PUT /api/user-cards/{userId}/{cardId}
    /// Cập nhật số lượng card
    /// </summary>
    public static string UPDATE_CARD_COUNT(int userId, long cardId) => 
        $"{BASE_URL}/api/user-cards/{userId}/{cardId}";
    
    /// <summary>
    /// POST /api/user-cards/add
    /// Thêm card cho user (reward, shop, etc.)
    /// Body: { userId, cardId, quantity }
    /// </summary>
    public static string ADD_CARD_TO_USER => $"{BASE_URL}/api/user-cards/add";

    // ===== EQUIPMENT APIS =====

    /// <summary>
    /// GET /api/equipment/{userId}/pets?page=0&size=10
    /// Lấy danh sách pet của user (phân trang)
    /// </summary>
    public static string GET_USER_PETS(int userId, int page = 0, int size = 10)
    {
        return $"{BASE_URL}/api/equipment/{userId}/pets?page={page}&size={size}";
    }
    
    /// <summary>
    /// GET /api/equipment/{userId}/avatars?page=0&size=3
    /// Lấy danh sách avatar của user (phân trang)
    /// </summary>
    public static string GET_USER_AVATARS(int userId, int page = 0, int size = 3)
    {
        return $"{BASE_URL}/api/equipment/{userId}/avatars?page={page}&size={size}";
    }
    
    /// <summary>
    /// POST /api/equipment/{userId}/equip-pet
    /// Trang bị pet
    /// Body: { "petId": 1 }
    /// </summary>
    public static string EQUIP_PET(int userId)
    {
        return $"{BASE_URL}/api/equipment/{userId}/equip-pet";
    }
    
    /// <summary>
    /// POST /api/equipment/{userId}/equip-avatar
    /// Trang bị avatar
    /// Body: { "avatarId": 1 }
    /// </summary>
    public static string EQUIP_AVATAR(int userId)
    {
        return $"{BASE_URL}/api/equipment/{userId}/equip-avatar";
    }
    
    /// <summary>
    /// GET /api/equipment/{userId}/count
    /// Đếm số lượng pet và avatar
    /// </summary>
    public static string GET_EQUIPMENT_COUNT(int userId)
    {
        return $"{BASE_URL}/api/equipment/{userId}/count";
    }

    public static string ADD_EXP(int userId)
    {
        return $"{BASE_URL}/reward/{userId}/exp";
    }

    public static string ADD_GOLD(int userId)
    {
        return $"{BASE_URL}/reward/{userId}/gold";
    }
}