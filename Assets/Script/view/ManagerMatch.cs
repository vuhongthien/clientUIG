using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ManagerMatch : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject LoadingPanel;
    public Text txtHpUser;
    public Text txtManaUser;
    public Text txtPowerUser;
    public Text txtWeeUser;
    public Text txtUsername;
    public Text txtHpEnemy;
    public Text txtManaEnemy;
    public Text txtPowerEnemy;
    public Text txtWeeEnemy;
    public Text txtusernameEnemy;
    public Text txtNLUser;
    public Image attributeUser;
    public Image attributeEnemy;
    public Image imgPetUser;
    public Image imgPetEnemy;
    public Animator anmtPetUser;
    public Animator anmtPetEnemy;
    public Slider sliderHpUser;
    public Slider sliderManaUser;
    public Slider sliderPowerUser;
    public Slider sliderHpEnemy;
    public Slider sliderManaEnemy;
    public Slider sliderPowerEnemy;
    public CardData cardData;
    private Active active;
    [Header("Boss Battle")]
    private bool isBossBattle = false;
    private long currentBossScheduleId;
    private int totalDamageDealt = 0;
    public PetUserDTO uPetsMatch;
    public PetUserDTO ePetsMatch;
    public static ManagerMatch Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        active = FindFirstObjectByType<Active>();
        StartCoroutine(LoadSceneAfterDelay());
    }

    private IEnumerator LoadSceneAfterDelay()
    {
        ManagerGame.Instance.LoadingPanel = LoadingPanel;
        ManagerGame.Instance.ShowLoading();

        int userId = PlayerPrefs.GetInt("userId", 1);
        int enemyPet = PlayerPrefs.GetInt("SelectedPetId", 1);
        int userPetId = PlayerPrefs.GetInt("userPetId", 1);

        // ✅ KIỂM TRA BOSS BATTLE
        isBossBattle = PlayerPrefs.GetString("IsBossBattle", "false") == "true";

        if (isBossBattle)
        {
            currentBossScheduleId = PlayerPrefs.GetInt("CurrentBossId", 0);
            Debug.Log($"[ManagerMatch] ========== BOSS BATTLE MODE ==========");
            Debug.Log($"[ManagerMatch] Boss Schedule ID: {currentBossScheduleId}");
            Debug.Log($"[ManagerMatch] User ID: {userId}");

            // ✅ DÙNG PostRequestRaw VÌ RESPONSE LÀ PLAIN TEXT
            bool attemptUsed = false;
            string errorMessage = "";

            yield return APIManager.Instance.PostRequestRaw(
                APIConfig.USE_BOSS_ATTEMPT(userId, currentBossScheduleId),
                null, // Không cần body
                (response) =>
                {
                    Debug.Log($"[ManagerMatch] ✓ Boss attempt used: {response}");
                    attemptUsed = true;
                },
                (error) =>
                {
                    Debug.LogError($"[ManagerMatch] ✗ Failed to use boss attempt: {error}");
                    errorMessage = error;
                }
            );

            // ✅ KIỂM TRA KẾT QUẢ
            if (!attemptUsed)
            {
                ManagerGame.Instance.HideLoading();

                Debug.LogError($"[ManagerMatch] Cannot start boss battle: {errorMessage}");

                // Hiện thông báo cho user (tùy chọn)
                // ShowErrorNotification(errorMessage);

                // Xóa flag boss battle
                PlayerPrefs.SetString("IsBossBattle", "false");
                PlayerPrefs.Save();

                // Quay lại scene trước
                yield return new WaitForSeconds(1f);
                string previousScene = PlayerPrefs.GetString("PreviousScene", "Home");
                UnityEngine.SceneManagement.SceneManager.LoadScene(previousScene);
                yield break; // Dừng coroutine
            }

            Debug.Log("[ManagerMatch] Boss attempt successfully used, loading match...");
        }
        else
        {
            Debug.Log($"[ManagerMatch] ========== PVP MODE ==========");
        }

        // ✅ LOAD DỮ LIỆU PET (CHUNG CHO CẢ BOSS VÀ PVP)
        yield return APIManager.Instance.GetRequest<PetUserDTO>(
            APIConfig.GET_PET_USERS_MATCH(userId, userPetId, enemyPet),
            OnPetsReceived,
            OnError
        );

        yield return APIManager.Instance.GetRequest<PetUserDTO>(
            APIConfig.GET_ENEMYPET_USERS_MATCH(enemyPet, userPetId),
            OnEPetsReceived,
            OnError
        );

        yield return APIManager.Instance.GetRequest<UserDTO>(
            APIConfig.GET_USER(userId),
            OnUserReceived,
            OnError
        );

        if (Board.Instance != null)
        {
            Board.Instance.InitializeCards();
            Debug.Log("[ManagerMatch] Cards initialized with API data");
        }
        ManagerGame.Instance.HideLoading();
    }

    public void BackScene()
    {
        // ✅ XÓA FLAG BOSS BATTLE KHI QUAY LẠI
        if (isBossBattle)
        {
            PlayerPrefs.SetString("IsBossBattle", "false");
            PlayerPrefs.DeleteKey("BossElementType"); // ✅ Xóa element type
            PlayerPrefs.Save();

            Debug.Log("[ManagerMatch] Exiting boss battle, returning to QuangTruong");
        }

        // ✅ LUÔN VỀ SCENE QUANGTRUONG
        UnityEngine.SceneManagement.SceneManager.LoadScene("QuangTruong");
    }

    // ✅ PUBLIC METHOD ĐỂ ACTIVE.CS CẬP NHẬT DAMAGE
    public void AddBossDamage(int damage)
    {
        if (isBossBattle)
        {
            totalDamageDealt += damage;
            Debug.Log($"[ManagerMatch] Total Boss Damage: {totalDamageDealt}");
        }
    }

    public bool IsBossBattle()
    {
        return isBossBattle;
    }

    public int GetTotalBossDamage()
    {
        return totalDamageDealt;
    }

    void OnEPetsReceived(PetUserDTO pets)
    {
        ePetsMatch = pets;
        txtHpEnemy.text = pets.hp.ToString() + "/" + (pets.hp + (pets.hp * 70 / 100));
        txtManaEnemy.text = 0 + "/" + (pets.mana + 200);
        txtPowerEnemy.text = 0 + "/" + 200;
        txtusernameEnemy.text = pets.name;
        sliderHpEnemy.maxValue = pets.hp + (pets.hp * 70 / 100);
        sliderManaEnemy.maxValue = pets.mana + 200;
        sliderPowerEnemy.maxValue = 200;
        sliderPowerEnemy.value = 0;
        sliderHpEnemy.value = pets.hp;
        sliderManaEnemy.value = 0;

        active.attackE = pets.attack;
        active.maxMauNPC = pets.hp + (pets.hp * 70 / 100);
        active.MauNPC = pets.hp;
        active.maxNoNPC = 200;
        active.maxManaNPC = pets.mana + 200;
        active.ManaNPC = 0;
        active.NoNPC = 0;
        if (pets.weaknessValue == 1)
        {
            txtWeeEnemy.text = "Lv" + pets.level + " <color=#FF0000>+0</color>";
        }
        else
        {
            txtWeeEnemy.text = "Lv" + pets.level + " <color=#00FF00>+" + pets.weaknessValue + "</color>";
        }
        OnEnemyPet(pets.petId.ToString());
        attributeEnemy.sprite = Resources.Load<Sprite>("Image/Attribute/" + pets.elementType);


    }

    void OnPetsReceived(PetUserDTO pets)
    {
        uPetsMatch = pets;
        txtHpUser.text = pets.hp.ToString() + "/" + (pets.hp + (pets.hp * 70 / 100));
        txtManaUser.text = 0 + "/" + (pets.mana + 200);
        txtPowerUser.text = 0 + "/" + 200;
        sliderHpUser.maxValue = pets.hp + (pets.hp * 70 / 100);
        sliderManaUser.maxValue = pets.mana + 200;
        sliderPowerUser.maxValue = 200;
        sliderHpUser.value = pets.hp;
        sliderManaUser.value = 0;
        sliderPowerUser.value = 0;

        active.attackP = pets.attack;
        active.maxMau = pets.hp + (pets.hp * 70 / 100);
        active.MauPlayer = pets.hp;
        active.maxNo = 200;
        active.maxMana = pets.mana + 200;
        active.ManaPlayer = 0;
        active.NoPlayer = 0;
        if (pets.weaknessValue == 1)
        {
            txtWeeUser.text = "Lv" + pets.level + " <color=#FF0000>+0</color>";
        }
        else
        {
            txtWeeUser.text = "Lv" + pets.level + " <color=#00FF00>+" + pets.weaknessValue + "</color>";
        }
        OnPet(pets.petId.ToString());
        attributeUser.sprite = Resources.Load<Sprite>("Image/Attribute/" + pets.elementType);
        Debug.Log("Attribute:---- " + pets.cardDTO.power);
        Board.Instance.cardData = pets.cardDTO;


    }

    void OnUserReceived(UserDTO user)
    {
        txtUsername.text = user.name;
        txtNLUser.text = user.energy.ToString();


    }

    void OnError(string error)
    {
        Debug.LogError("API Error: " + error);
    }

    void OnPet(string petId)
    {
        AnimationClip[] clips = Resources.LoadAll<AnimationClip>($"Pets/{petId}");
        Debug.Log("Số lượng Animation Clips: " + clips.Length);
        if (clips != null && anmtPetUser != null)
        {
            Debug.Log("Thay đổi Animation Clips cho: " + petId);
            ReplaceAnimationsPet(clips);
        }
        if (clips.Length == 0)
        {
            imgPetUser.sprite = Resources.Load<Sprite>("Image/IconsPet/" + petId);
        }
    }

    void ReplaceAnimationsPet(AnimationClip[] newClips)
    {
        RuntimeAnimatorController originalController = anmtPetUser.runtimeAnimatorController;
        AnimatorOverrideController overrideController = new AnimatorOverrideController(originalController);

        foreach (AnimationClip newClip in newClips)
        {
            foreach (var pair in overrideController.animationClips)
            {
                if (pair.name == newClip.name) // So sánh theo tên
                {
                    Debug.Log("So sánh tên: " + pair.name + " với " + newClip.name);
                    overrideController[pair] = newClip; // Gán Animation Clip mới
                }
            }
        }
        anmtPetUser.runtimeAnimatorController = overrideController;
    }

    void OnEnemyPet(string petId)
    {
        AnimationClip[] clips = Resources.LoadAll<AnimationClip>($"Pets/{petId}");
        Debug.Log("Số lượng Animation Clips: " + clips.Length);
        if (clips != null && anmtPetEnemy != null)
        {
            Debug.Log("Thay đổi Animation Clips cho: " + petId);
            ReplaceAnimationsEnemyPet(clips);
        }
        if (clips.Length == 0)
        {
            imgPetEnemy.sprite = Resources.Load<Sprite>("Image/IconsPet/" + petId);
        }
    }

    void ReplaceAnimationsEnemyPet(AnimationClip[] newClips)
    {
        // Tạo một AnimatorOverrideController để thay thế Animation Clips
        RuntimeAnimatorController originalController = anmtPetEnemy.runtimeAnimatorController;
        AnimatorOverrideController overrideController = new AnimatorOverrideController(originalController);

        // Duyệt qua tất cả các Animation Clips và thay thế
        foreach (AnimationClip newClip in newClips)
        {
            foreach (var pair in overrideController.animationClips)
            {
                if (pair.name == newClip.name) // So sánh theo tên
                {
                    Debug.Log("So sánh tên: " + pair.name + " với " + newClip.name);
                    overrideController[pair] = newClip; // Gán Animation Clip mới
                }
            }
        }
        // Gán AnimatorOverrideController mới cho Animator
        anmtPetEnemy.runtimeAnimatorController = overrideController;
    }

}
