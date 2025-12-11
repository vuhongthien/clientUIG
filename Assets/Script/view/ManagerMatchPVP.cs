using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class ManagerMatchPVP : MonoBehaviourPun
{
    [Header("UI Elements")]
    public GameObject LoadingPanel;
    
    [Header("User Stats")]
    public Text txtHpUser;
    public Text txtManaUser;
    public Text txtPowerUser;
    public Text txtWeeUser;
    public Text txtUsername;
    public Text txtNLUser;
    public Image attributeUser;
    public Image imgPetUser;
    public Animator anmtPetUser;
    
    [Header("User Sliders")]
    public Slider sliderHpUser;
    public Slider sliderManaUser;
    public Slider sliderPowerUser;
    
    [Header("Enemy Stats")]
    public Text txtHpEnemy;
    public Text txtManaEnemy;
    public Text txtPowerEnemy;
    public Text txtWeeEnemy;
    public Text txtusernameEnemy;
    public Image attributeEnemy;
    public Image imgPetEnemy;
    public Animator anmtPetEnemy;
    
    [Header("Enemy Sliders")]
    public Slider sliderHpEnemy;
    public Slider sliderManaEnemy;
    public Slider sliderPowerEnemy;

    private ActivePVP active;
    
    // Player Info
    private UserInfo currentUser;
    private UserInfo enemyUser;
    
    // Loading flags
    private bool hasLoadedUserInfo = false;
    private bool hasLoadedEnemyInfo = false;
    private bool hasLoadedUserPet = false;
    private bool hasLoadedEnemyPet = false;

    [System.Serializable]
    public class UserInfo
    {
        public int userId;
        public string userName;
        public int petId;
        public bool isLocalPlayer;
        
        public UserInfo(int id, string name, int pet, bool local)
        {
            userId = id;
            userName = name;
            petId = pet;
            isLocalPlayer = local;
        }
    }

    public void LoadInfoMatch()
    {
        active = FindFirstObjectByType<ActivePVP>();
        StartCoroutine(LoadMatchData());
    }

    private IEnumerator LoadMatchData()
    {
        ManagerGame.Instance.LoadingPanel = LoadingPanel;
        ManagerGame.Instance.ShowLoading();

        // Reset loading flags
        ResetLoadingFlags();

        // Step 1: Identify User and Enemy
        IdentifyPlayersInMatch();

        if (currentUser == null || enemyUser == null)
        {
            Debug.LogError("Cannot identify players in match!");
            ManagerGame.Instance.HideLoading();
            yield break;
        }

        Debug.Log($"Match identified - User: {currentUser.userName}(ID:{currentUser.userId}, Pet:{currentUser.petId}) vs Enemy: {enemyUser.userName}(ID:{enemyUser.userId}, Pet:{enemyUser.petId})");

        // Step 2: Load User info and pet data
        yield return StartCoroutine(LoadUserData());
        
        // Step 3: Load Enemy info and pet data  
        yield return StartCoroutine(LoadEnemyData());

        // Step 4: Wait for all data to load
        yield return StartCoroutine(WaitForAllDataLoaded());

        ManagerGame.Instance.HideLoading();
        Debug.Log("Match data loaded successfully!");
    }

    private void ResetLoadingFlags()
    {
        hasLoadedUserInfo = false;
        hasLoadedEnemyInfo = false;
        hasLoadedUserPet = false;
        hasLoadedEnemyPet = false;
    }

    private void IdentifyPlayersInMatch()
    {
        // Get local user info
        int localUserId = PlayerPrefs.GetInt("userId", 1);
        string localUserName = PlayerPrefs.GetString("username", "Player");
        int localPetId = GetLocalPlayerPetId();

        currentUser = new UserInfo(localUserId, localUserName, localPetId, true);

        // Find enemy player from PhotonNetwork
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player != PhotonNetwork.LocalPlayer)
            {
                // Get enemy info from Photon properties or fallback
                int enemyUserId = GetPlayerUserId(player);
                string enemyUserName = player.NickName;
                int enemyPetId = GetEnemyPetId(player);

                enemyUser = new UserInfo(enemyUserId, enemyUserName, enemyPetId, false);
                break;
            }
        }

        Debug.Log($"Players identified - Current: {currentUser.userName}(Pet:{currentUser.petId}), Enemy: {enemyUser?.userName}(Pet:{enemyUser?.petId})");
    }

    private int GetLocalPlayerPetId()
    {
        // Priority 1: From Photon properties
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("petId"))
        {
            object petIdObj = PhotonNetwork.LocalPlayer.CustomProperties["petId"];
            if (int.TryParse(petIdObj.ToString(), out int petId))
            {
                return petId;
            }
        }

        // Priority 2: From ManagerDauTruong
        if (ManagerDauTruong.Instance != null && !string.IsNullOrEmpty(ManagerDauTruong.Instance.petId))
        {
            if (int.TryParse(ManagerDauTruong.Instance.petId, out int petId))
            {
                return petId;
            }
        }

        // Fallback: From PlayerPrefs
        return PlayerPrefs.GetInt("userPetId", 1);
    }

    private int GetPlayerUserId(Photon.Realtime.Player player)
    {
        if (player.CustomProperties.ContainsKey("userId"))
        {
            object userIdObj = player.CustomProperties["userId"];
            if (int.TryParse(userIdObj.ToString(), out int userId))
            {
                return userId;
            }
        }
        
        // Fallback - có thể cần logic khác tùy vào cách bạn lưu userId
        return 1; // hoặc giá trị mặc định
    }

    private int GetEnemyPetId(Photon.Realtime.Player enemyPlayer)
    {
        int enemyUserId = GetPlayerUserId(enemyPlayer);
        
        // Method 1: From Room Properties (Primary method)
        string roomPetKey = $"player_{enemyUserId}_petId";
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(roomPetKey))
        {
            object petIdObj = PhotonNetwork.CurrentRoom.CustomProperties[roomPetKey];
            if (int.TryParse(petIdObj.ToString(), out int roomPetId))
            {
                Debug.Log($"Enemy petId from Room Properties: {roomPetId} (Key: {roomPetKey})");
                return roomPetId;
            }
        }

        // Method 2: From Player Custom Properties (Fallback)
        if (enemyPlayer.CustomProperties.ContainsKey("petId"))
        {
            object petIdObj = enemyPlayer.CustomProperties["petId"];
            if (int.TryParse(petIdObj.ToString(), out int petId))
            {
                Debug.Log($"Enemy petId from Player Custom Properties: {petId}");
                return petId;
            }
        }

        // Method 3: Default fallback
        int defaultEnemyPet = 1;
        Debug.LogWarning($"Cannot find enemy petId, using default: {defaultEnemyPet}");
        return defaultEnemyPet;
    }

    // Method to set pet selection in Room Properties
    public void SetPlayerPetInRoom(int userId, int petId)
    {
        if (!PhotonNetwork.InRoom) return;

        string roomPetKey = $"player_{userId}_petId";
        var roomProps = new ExitGames.Client.Photon.Hashtable
        {
            [roomPetKey] = petId
        };
        
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
        Debug.Log($"Set room property: {roomPetKey} = {petId}");
    }

    // Method to get all players' pet selections from Room Properties
    public void LogAllPlayerPetsInRoom()
    {
        Debug.Log("=== ROOM PET SELECTIONS ===");
        foreach (var prop in PhotonNetwork.CurrentRoom.CustomProperties)
        {
            if (prop.Key.ToString().StartsWith("player_") && prop.Key.ToString().EndsWith("_petId"))
            {
                Debug.Log($"{prop.Key}: {prop.Value}");
            }
        }
    }

    private IEnumerator LoadUserData()
    {
        Debug.Log($"Loading user data - UserID: {currentUser.userId}, PetID: {currentUser.petId}");

        // Load user basic info
        yield return StartCoroutine(APIManager.Instance.GetRequest<UserDTO>(
            APIConfig.GET_USER(currentUser.userId), 
            OnCurrentUserReceived, 
            OnUserLoadError));

        // Load user's pet data
        yield return StartCoroutine(APIManager.Instance.GetRequest<PetUserDTO>(
            APIConfig.GET_PET_USERS_MATCH(currentUser.userId, currentUser.petId, enemyUser.petId), 
            OnCurrentUserPetReceived, 
            OnUserPetLoadError));
    }

    private IEnumerator LoadEnemyData()
    {
        Debug.Log($"Loading enemy data - UserID: {enemyUser.userId}");

        // Load enemy basic info first
        yield return StartCoroutine(APIManager.Instance.GetRequest<UserDTO>(
            APIConfig.GET_USER(enemyUser.userId), 
            OnEnemyUserReceived, 
            OnEnemyLoadError));

        // Option A: Nếu bạn đã có enemyUser.petId từ Photon Properties
        if (enemyUser.petId > 0)
        {
            Debug.Log($"Loading enemy pet with known petId: {enemyUser.petId}");
            yield return StartCoroutine(APIManager.Instance.GetRequest<PetUserDTO>(
                APIConfig.GET_PET_USERS_MATCH(enemyUser.userId, enemyUser.petId, currentUser.petId), 
                OnEnemyPetReceived, 
                OnEnemyPetLoadError));
        }
        else
        {
            // Option B: Load enemy pet bằng cách khác
            Debug.LogWarning("Enemy petId not available, trying alternative methods...");
            
            // Method 1: Gọi API để lấy pet đang active của enemy user
            // yield return StartCoroutine(APIManager.Instance.GetRequest<PetUserDTO>(
            //     APIConfig.GET_USER_ACTIVE_PET(enemyUser.userId), 
            //     OnEnemyActivePetReceived, 
            //     OnEnemyPetLoadError));

            // Method 2: Sử dụng pet mặc định (ID = 1) để test
            enemyUser.petId = 1; // Default pet ID
            Debug.LogWarning($"Using default enemy petId: {enemyUser.petId}");
            
            yield return StartCoroutine(APIManager.Instance.GetRequest<PetUserDTO>(
                APIConfig.GET_PET_USERS_MATCH(enemyUser.userId, enemyUser.petId, currentUser.petId), 
                OnEnemyPetReceived, 
                OnEnemyPetLoadError));
        }
    }

    private IEnumerator WaitForAllDataLoaded()
    {
        float timeout = 10f; // 10 second timeout
        float timer = 0f;

        while ((!hasLoadedUserInfo || !hasLoadedEnemyInfo || !hasLoadedUserPet || !hasLoadedEnemyPet) && timer < timeout)
        {
            timer += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        if (timer >= timeout)
        {
            Debug.LogWarning("Timeout waiting for match data to load!");
        }
    }

    // === USER DATA CALLBACKS ===
    void OnCurrentUserReceived(UserDTO user)
    {
        txtUsername.text = user.name;
        currentUser.userName = user.name; // Update with actual name from API
        hasLoadedUserInfo = true;
        
        Debug.Log($"Current user loaded: {user.name}");
    }

    void OnCurrentUserPetReceived(PetUserDTO pet)
    {
        // Setup User UI
        SetupUserStats(pet);
        SetupUserPet(pet.petId.ToString());
        
        // Setup Active game data for user
        if (active != null)
        {
            SetupActiveUserData(pet);
        }

        hasLoadedUserPet = true;
        Debug.Log($"Current user pet loaded: {pet.name} (ID: {pet.petId})");
    }

    // === ENEMY DATA CALLBACKS ===
    void OnEnemyUserReceived(UserDTO user)
    {
        txtusernameEnemy.text = user.name;
        enemyUser.userName = user.name; // Update with actual name from API
        hasLoadedEnemyInfo = true;
        
        Debug.Log($"Enemy user loaded: {user.name}");
    }

    void OnEnemyPetReceived(PetUserDTO pet)
    {
        // Setup Enemy UI
        SetupEnemyStats(pet);
        SetupEnemyPet(pet.petId.ToString());
        
        // Setup Active game data for enemy
        if (active != null)
        {
            SetupActiveEnemyData(pet);
        }

        hasLoadedEnemyPet = true;
        Debug.Log($"Enemy pet loaded: {pet.name} (ID: {pet.petId})");
    }

    // === UI SETUP METHODS ===
    private void SetupUserStats(PetUserDTO pet)
    {
        int maxHp = pet.hp + (pet.hp * 70 / 100);
        int maxMana = pet.mana + 200;

        txtHpUser.text = $"{pet.hp}/{maxHp}";
        txtManaUser.text = $"0/{maxMana}";
        txtPowerUser.text = "0/200";
        
        sliderHpUser.maxValue = 1f;
        sliderManaUser.maxValue = 1f;
        sliderPowerUser.maxValue = 1f;
        sliderHpUser.value = (float) pet.hp / maxHp;
        sliderManaUser.value = 0;
        sliderPowerUser.value = 0;
        

        // Weakness display
        if (pet.weaknessValue == 1)
        {
            txtWeeUser.text = $"Lv{pet.level} <color=#FF0000>+0</color>";
        }
        else
        {
            txtWeeUser.text = $"Lv{pet.level} <color=#00FF00>+{pet.weaknessValue}</color>";
        }

        attributeUser.sprite = Resources.Load<Sprite>("Image/Attribute/" + pet.elementType);
    }

    private void SetupEnemyStats(PetUserDTO pet)
    {
        int maxHp = pet.hp + (pet.hp * 70 / 100);
        int maxMana = pet.mana + 200;

        txtHpEnemy.text = $"{pet.hp}/{maxHp}";
        txtManaEnemy.text = $"0/{maxMana}";
        txtPowerEnemy.text = "0/200";
        
        sliderHpEnemy.maxValue =1f;
        sliderManaEnemy.maxValue = 1f;
        sliderPowerEnemy.maxValue = 1f;
        sliderHpEnemy.value = (float) pet.hp / maxHp;
        sliderManaEnemy.value = 0;
        sliderPowerEnemy.value = 0;

        // Weakness display
        if (pet.weaknessValue == 1)
        {
            txtWeeEnemy.text = $"Lv{pet.level} <color=#FF0000>+0</color>";
        }
        else
        {
            txtWeeEnemy.text = $"Lv{pet.level} <color=#00FF00>+{pet.weaknessValue}</color>";
        }

        attributeEnemy.sprite = Resources.Load<Sprite>("Image/Attribute/" + pet.elementType);
    }

    private void SetupActiveUserData(PetUserDTO pet)
    {
        active.attackP = pet.attack;
        active.maxMau = pet.hp + (pet.hp * 70 / 100);
        active.MauPlayer = pet.hp;
        active.maxNo = 200;
        active.maxMana = pet.mana + 200;
        active.ManaPlayer = 0;
        active.NoPlayer = 0;
    }

    private void SetupActiveEnemyData(PetUserDTO pet)
    {
        active.attackE = pet.attack;
        active.maxMauNPC = pet.hp + (pet.hp * 70 / 100);
        active.MauNPC = pet.hp;
        active.maxNoNPC = 200;
        active.maxManaNPC = pet.mana + 200;
        active.ManaNPC = 0;
        active.NoNPC = 0;
    }

    // === PET VISUAL SETUP ===
    void SetupUserPet(string petId)
    {
        AnimationClip[] clips = Resources.LoadAll<AnimationClip>($"Pets/{petId}");
        Debug.Log($"User pet animations loaded: {clips.Length} clips for pet {petId}");
        
        if (clips != null && clips.Length > 0 && anmtPetUser != null)
        {
            ReplaceAnimationsPet(clips, anmtPetUser);
        }
        else
        {
            imgPetUser.sprite = Resources.Load<Sprite>("Image/IconsPet/" + petId);
        }
    }

    void SetupEnemyPet(string petId)
    {
        AnimationClip[] clips = Resources.LoadAll<AnimationClip>($"Pets/{petId}");
        Debug.Log($"Enemy pet animations loaded: {clips.Length} clips for pet {petId}");
        
        if (clips != null && clips.Length > 0 && anmtPetEnemy != null)
        {
            ReplaceAnimationsPet(clips, anmtPetEnemy);
        }
        else
        {
            imgPetEnemy.sprite = Resources.Load<Sprite>("Image/IconsPet/" + petId);
        }
    }

    void ReplaceAnimationsPet(AnimationClip[] newClips, Animator animator)
    {
        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning("Animator controller is null!");
            return;
        }

        RuntimeAnimatorController originalController = animator.runtimeAnimatorController;
        AnimatorOverrideController overrideController = new AnimatorOverrideController(originalController);

        foreach (AnimationClip newClip in newClips)
        {
            foreach (var clip in overrideController.animationClips)
            {
                if (clip.name == newClip.name)
                {
                    Debug.Log($"Replacing animation: {clip.name} with {newClip.name}");
                    overrideController[clip] = newClip;
                    break;
                }
            }
        }
        
        animator.runtimeAnimatorController = overrideController;
    }

    // === ERROR HANDLERS ===
    void OnUserLoadError(string error)
    {
        Debug.LogError($"User load error: {error}");
        hasLoadedUserInfo = true; // Prevent infinite wait
    }

    void OnUserPetLoadError(string error)
    {
        Debug.LogError($"User pet load error: {error}");
        hasLoadedUserPet = true; // Prevent infinite wait
    }

    void OnEnemyLoadError(string error)
    {
        Debug.LogError($"Enemy load error: {error}");
        hasLoadedEnemyInfo = true; // Prevent infinite wait
    }

    void OnEnemyPetLoadError(string error)
    {
        Debug.LogError($"Enemy pet load error: {error}");
        hasLoadedEnemyPet = true; // Prevent infinite wait
    }

    public void BackScene()
    {
        ManagerGame.Instance.BackScene();
    }

    // === DEBUG INFO ===
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void LogMatchInfo()
    {
        Debug.Log("=== MATCH INFO ===");
        Debug.Log($"Current User: {currentUser?.userName} (ID: {currentUser?.userId}, Pet: {currentUser?.petId})");
        Debug.Log($"Enemy User: {enemyUser?.userName} (ID: {enemyUser?.userId}, Pet: {enemyUser?.petId})");
        Debug.Log($"Loading Status - User: {hasLoadedUserInfo}/{hasLoadedUserPet}, Enemy: {hasLoadedEnemyInfo}/{hasLoadedEnemyPet}");
    }
}