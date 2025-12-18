using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// ManagerEquipment v2.3 - THÊM TAB THÔNG TIN USER:
/// - Tab Pet
/// - Tab Avatar
/// - Tab Thông tin User (clone từ ManagerTop)
/// </summary>
public class ManagerEquipment : MonoBehaviour
{
    [Header("Main Panel")]
    public GameObject panelEquipment;
    public Button btnClose;

    [Header("Tab Buttons")]
    public Button btnPet;
    public Button btnAvt;
    public Button btnUserInfo; // ✅ TAB MỚI

    [Header("Content Panels")]
    public GameObject bgPet;
    public GameObject ListAvt;
    public GameObject panelUserInfo; // ✅ PANEL MỚI

    [Header("Pet List")]
    public Transform ListPet;
    public GameObject PET; // Prefab

    [Header("Avatar List")]
    public GameObject AVTPET; // Prefab

    [Header("Navigation")]
    public Button btnLeft;
    public Button btnRight;
    public Text txtPageInfo;

    [Header("Stats Display - Panel chính")]
    public Text txtDame;
    public Text txtMana;
    public Text txtMau;

    // ============================================================
    // ✅ USER INFO TAB - Components
    // ============================================================
    [Header("User Info Tab - Basic Info")]
    public Image imgUserInfoAvatar;
    public Text txtUserInfoName;
    public Image imgUserInfoLevel;
    public Animator anmtUserInfoPet;

    [Header("User Info Tab - Current Pet Stats")]
    public Text txtUserInfoAttack;
    public Text txtUserInfoHP;
    public Text txtUserInfoMana;

    [Header("User Info Tab - Pet List")]
    public Transform userInfoPetListContent;
    public GameObject userInfoPetItemPrefab;

    [Header("User Info Tab - Stone List")]
    public Transform userInfoStoneListContent;
    public GameObject userInfoStoneItemPrefab;

    // Private variables
    private int userId;
    private int currentPage = 0;
    private int totalPages = 1;
    private int currentTab = 0; // 0: Pet, 1: Avatar, 2: UserInfo

    private List<PetEquipmentDTO> allPets = new List<PetEquipmentDTO>();
    private List<AvatarEquipmentDTO> allAvatars = new List<AvatarEquipmentDTO>();

    private long currentEquippedPetId = -1;
    private long currentEquippedAvatarId = -1;

    private const int PETS_PER_PAGE = 10;
    private const int AVATARS_PER_PAGE = 3;

    private CanvasGroup panelCanvasGroup;
    private Dictionary<int, Sprite> stoneDictionary;
    public GameObject panelSettings;

    public Button btnOpenSettings; // ✅ THÊM

    private SettingsManager settingsManager;

    void Start()
    {
        userId = PlayerPrefs.GetInt("userId", 0);
settingsManager = FindObjectOfType<SettingsManager>();
        if (settingsManager == null && panelSettings != null)
        {
            GameObject settingsObj = new GameObject("SettingsManager");
            settingsManager = settingsObj.AddComponent<SettingsManager>();
            settingsManager.panelSettings = panelSettings;
        }
        if (userId == 0)
        {
            Debug.LogError("[ManagerEquipment] User ID not found!");
            return;
        }

        InitializeUI();
        InitializeStoneDictionary(); // ✅ Khởi tạo dictionary cho đá

        if (panelEquipment != null)
        {
            panelCanvasGroup = panelEquipment.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = panelEquipment.AddComponent<CanvasGroup>();
            }

            panelEquipment.SetActive(false);
        }
    }

    void InitializeUI()
    {
        if (btnClose != null)
            btnClose.onClick.AddListener(ClosePanel);

        if (btnPet != null)
            btnPet.onClick.AddListener(() => SwitchTab(0));

        if (btnAvt != null)
            btnAvt.onClick.AddListener(() => SwitchTab(1));

        if (btnUserInfo != null)
            btnUserInfo.onClick.AddListener(() => SwitchTab(2)); // ✅ TAB THÔNG TIN

        if (btnLeft != null)
            btnLeft.onClick.AddListener(PreviousPage);

        if (btnRight != null)
            btnRight.onClick.AddListener(NextPage);

        if (bgPet != null) bgPet.SetActive(false);
        if (ListAvt != null) ListAvt.SetActive(false);
        if (panelUserInfo != null) panelUserInfo.SetActive(false); // ✅ Ẩn panel user info
        if (btnOpenSettings != null && settingsManager != null)
        {
            btnOpenSettings.onClick.AddListener(() => settingsManager.OpenSettings());
        }
    }

    // ============================================================
    // ✅ KHỞI TẠO STONE DICTIONARY
    // ============================================================
    void InitializeStoneDictionary()
    {
        stoneDictionary = new Dictionary<int, Sprite>();

        // Hệ Lửa (ID 1-7)
        for (int i = 0; i < ManagerQuangTruong.Instance.stoneFire.Length; i++)
        {
            if (ManagerQuangTruong.Instance.stoneFire[i] != null)
            {
                int stoneId = i + 1;
                stoneDictionary[stoneId] = ManagerQuangTruong.Instance.stoneFire[i];
            }
        }

        // Hệ Nước (ID 8-14)
        for (int i = 0; i < ManagerQuangTruong.Instance.stoneWater.Length; i++)
        {
            if (ManagerQuangTruong.Instance.stoneWater[i] != null)
            {
                int stoneId = 8 + i;
                stoneDictionary[stoneId] = ManagerQuangTruong.Instance.stoneWater[i];
            }
        }

        // Hệ Gió (ID 15-21)
        for (int i = 0; i < ManagerQuangTruong.Instance.stoneWind.Length; i++)
        {
            if (ManagerQuangTruong.Instance.stoneWind[i] != null)
            {
                int stoneId = 15 + i;
                stoneDictionary[stoneId] = ManagerQuangTruong.Instance.stoneWind[i];
            }
        }

        // Hệ Đất (ID 22-28)
        for (int i = 0; i < ManagerQuangTruong.Instance.stoneEarth.Length; i++)
        {
            if (ManagerQuangTruong.Instance.stoneEarth[i] != null)
            {
                int stoneId = 22 + i;
                stoneDictionary[stoneId] = ManagerQuangTruong.Instance.stoneEarth[i];
            }
        }

        // Hệ Sét (ID 29-35)
        for (int i = 0; i < ManagerQuangTruong.Instance.stoneThunder.Length; i++)
        {
            if (ManagerQuangTruong.Instance.stoneThunder[i] != null)
            {
                int stoneId = 29 + i;
                stoneDictionary[stoneId] = ManagerQuangTruong.Instance.stoneThunder[i];
            }
        }

        Debug.Log($"[ManagerEquipment] Stone dictionary initialized with {stoneDictionary.Count} sprites");
    }

    Sprite GetStoneSprite(long stoneId)
    {
        if (stoneDictionary != null && stoneDictionary.ContainsKey((int)stoneId))
        {
            return stoneDictionary[(int)stoneId];
        }

        Debug.LogWarning($"[ManagerEquipment] Không tìm thấy sprite cho đá ID: {stoneId}");
        return null;
    }

    public void OpenEquipmentPanel()
    {
        if (panelEquipment == null)
        {
            Debug.LogError("[ManagerEquipment] panelEquipment is null!");
            return;
        }

        Debug.Log("[ManagerEquipment] Opening equipment panel...");

        panelEquipment.SetActive(true);

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad);
        }

        currentPage = 0;
        SwitchTab(2); // Mặc định mở tab Pet
    }

    void ClosePanel()
    {
        if (panelEquipment != null && panelCanvasGroup != null)
        {
            panelCanvasGroup.DOFade(0f, 0.2f)
                .SetEase(Ease.InQuad)
                .OnComplete(() => panelEquipment.SetActive(false));
        }
    }

    // ============================================================
    // ✅ SWITCH TAB - CẬP NHẬT
    // ============================================================
    void SwitchTab(int tabIndex)
    {
        currentTab = tabIndex;
        currentPage = 0;

        // Ẩn tất cả panels
        if (bgPet != null) bgPet.SetActive(false);
        if (ListAvt != null) ListAvt.SetActive(false);
        if (panelUserInfo != null) panelUserInfo.SetActive(false);

        switch (tabIndex)
        {
            case 0: // Pet Tab
                Debug.Log("[ManagerEquipment] Switching to Pet tab");
                if (bgPet != null) bgPet.SetActive(true);
                HighlightButton(btnPet, new[] { btnAvt, btnUserInfo });
                StartCoroutine(LoadPetsData());
                
                // Hiện navigation cho Pet
                ShowNavigation(true);
                break;

            case 1: // Avatar Tab
                Debug.Log("[ManagerEquipment] Switching to Avatar tab");
                if (ListAvt != null) ListAvt.SetActive(true);
                HighlightButton(btnAvt, new[] { btnPet, btnUserInfo });
                StartCoroutine(LoadAvatarsData());
                
                // Hiện navigation cho Avatar
                ShowNavigation(true);
                break;

            case 2: // User Info Tab
                Debug.Log("[ManagerEquipment] Switching to User Info tab");
                if (panelUserInfo != null) panelUserInfo.SetActive(true);
                HighlightButton(btnUserInfo, new[] { btnPet, btnAvt });
                StartCoroutine(LoadUserInfoData());
                
                // Ẩn navigation cho User Info (không cần phân trang)
                ShowNavigation(false);
                break;
        }
    }

    void HighlightButton(Button activeBtn, Button[] inactiveBtns)
    {
        if (activeBtn != null)
        {
            ColorBlock colors = activeBtn.colors;
            colors.normalColor = Color.yellow;
            activeBtn.colors = colors;
        }

        foreach (var btn in inactiveBtns)
        {
            if (btn != null)
            {
                ColorBlock colors = btn.colors;
                colors.normalColor = Color.white;
                btn.colors = colors;
            }
        }
    }

    void ShowNavigation(bool show)
    {
        if (btnLeft != null) btnLeft.gameObject.SetActive(show);
        if (btnRight != null) btnRight.gameObject.SetActive(show);
        if (txtPageInfo != null) txtPageInfo.gameObject.SetActive(show);
    }

    // ============================================================
    // LOAD DATA
    // ============================================================

    IEnumerator LoadPetsData()
    {
        Debug.Log($"[ManagerEquipment] Loading pets - Page {currentPage}");

        string url = APIConfig.GET_USER_PETS(userId, currentPage, PETS_PER_PAGE);

        yield return APIManager.Instance.GetRequest<List<PetEquipmentDTO>>(
            url,
            OnPetsLoaded,
            OnLoadError
        );
    }

    void OnPetsLoaded(List<PetEquipmentDTO> pets)
    {
        Debug.Log($"[ManagerEquipment] Loaded {pets.Count} pets");

        allPets = pets;

        currentEquippedPetId = -1;
        foreach (var pet in pets)
        {
            if (pet.equipped)
            {
                currentEquippedPetId = pet.petId;
                Debug.Log($"[ManagerEquipment] Current equipped pet: {currentEquippedPetId}");
                break;
            }
        }

        StartCoroutine(LoadEquipmentCount());
        DisplayPets();
    }

    IEnumerator LoadAvatarsData()
    {
        Debug.Log($"[ManagerEquipment] Loading avatars - Page {currentPage}");

        string url = APIConfig.GET_USER_AVATARS(userId, currentPage, AVATARS_PER_PAGE);

        yield return APIManager.Instance.GetRequest<List<AvatarEquipmentDTO>>(
            url,
            OnAvatarsLoaded,
            OnLoadError
        );
    }

    void OnAvatarsLoaded(List<AvatarEquipmentDTO> avatars)
    {
        Debug.Log($"[ManagerEquipment] Loaded {avatars.Count} avatars");

        allAvatars = avatars;

        currentEquippedAvatarId = -1;
        foreach (var avatar in avatars)
        {
            if (avatar.equipped)
            {
                currentEquippedAvatarId = avatar.avatarId;
                Debug.Log($"[ManagerEquipment] Current equipped avatar: {currentEquippedAvatarId}");
                break;
            }
        }

        StartCoroutine(LoadEquipmentCount());
        DisplayAvatars();
    }

    // ============================================================
    // ✅ LOAD USER INFO DATA
    // ============================================================
    IEnumerator LoadUserInfoData()
    {
        Debug.Log($"[ManagerEquipment] Loading user info - User ID: {userId}");

        string url = APIConfig.GET_USER_DETAIL(userId);

        yield return APIManager.Instance.GetRequest<UserDetailData>(
            url,
            OnUserInfoLoaded,
            OnLoadError
        );
    }

    void OnUserInfoLoaded(UserDetailData userDetail)
    {
        Debug.Log($"[ManagerEquipment] User info loaded: {userDetail.userName}");

        DisplayUserInfo(userDetail);
    }

    IEnumerator LoadEquipmentCount()
    {
        string url = APIConfig.GET_EQUIPMENT_COUNT(userId);

        yield return APIManager.Instance.GetRequest<EquipmentCountDTO>(
            url,
            (count) =>
            {
                if (currentTab == 0) // Pet
                {
                    totalPages = count.petPages;
                }
                else if (currentTab == 1) // Avatar
                {
                    totalPages = count.avatarPages;
                }

                UpdatePageInfo();
                UpdateNavigationButtons();
            },
            (error) => Debug.LogError($"[ManagerEquipment] Count error: {error}")
        );
    }

    void OnLoadError(string error)
    {
        Debug.LogError($"[ManagerEquipment] Load error: {error}");
    }

    // ============================================================
    // DISPLAY UI - PET & AVATAR (GIỮ NGUYÊN)
    // ============================================================

    void DisplayPets()
    {
        foreach (Transform child in ListPet)
        {
            Destroy(child.gameObject);
        }

        foreach (var pet in allPets)
        {
            GameObject petObj = Instantiate(PET, ListPet);
            petObj.SetActive(true);

            SetupPetItem(petObj, pet);
        }

        Debug.Log($"[ManagerEquipment] Displayed {allPets.Count} pets");
    }

    void SetupPetItem(GameObject petObj, PetEquipmentDTO pet)
    {
        Button btnSelect = petObj.transform.Find("btnSelect")?.GetComponent<Button>();
        Text txtChon = btnSelect?.transform.Find("chon")?.GetComponent<Text>();
        Image anmtP = petObj.transform.Find("anmtP")?.GetComponent<Image>();
        Text txtlv = petObj.transform.Find("txtlv")?.GetComponent<Text>();
        Animator petAnimator = petObj.transform.Find("anmtP")?.GetComponent<Animator>();
        
        if (txtlv != null)
        {
            txtlv.text = "Lv" + pet.level.ToString();
        }

        if (anmtP != null)
        {
            Sprite petSprite = Resources.Load<Sprite>($"Image/IconsPet/{pet.petId}");
            if (petSprite != null)
            {
                anmtP.sprite = petSprite;
            }
            else
            {
                Debug.LogWarning($"[ManagerEquipment] Pet sprite not found: Image/IconsPet/{pet.petId}");
            }
        }

        if (btnSelect != null)
        {
            if (pet.equipped)
            {
                btnSelect.interactable = false;

                if (txtChon != null)
                {
                    txtChon.text = "Đã chọn";
                }

                Debug.Log($"[ManagerEquipment] Pet {pet.petId} is equipped");
            }
            else
            {
                btnSelect.interactable = true;

                if (txtChon != null)
                {
                    txtChon.text = "Chọn";
                }

                btnSelect.onClick.RemoveAllListeners();
                btnSelect.onClick.AddListener(() => OnSelectPet(pet, petObj));
            }
        }

        if (petAnimator != null)
        {
            AnimationClip[] clips = Resources.LoadAll<AnimationClip>($"Pets/{pet.petId}");

            if (clips != null && clips.Length > 0)
            {
                ReplaceAnimations(petAnimator, clips);
                Debug.Log($"[ManagerEquipment] Loaded {clips.Length} animations for pet {pet.petId}");
            }
        }
    }

    void ReplaceAnimations(Animator animator, AnimationClip[] newClips)
    {
        if (animator == null) return;

        RuntimeAnimatorController originalController = animator.runtimeAnimatorController;

        if (originalController == null)
        {
            Debug.LogWarning("[ManagerEquipment] Animator has no controller!");
            return;
        }

        AnimatorOverrideController overrideController = new AnimatorOverrideController(originalController);

        foreach (AnimationClip newClip in newClips)
        {
            foreach (var clip in overrideController.animationClips)
            {
                if (clip.name == newClip.name)
                {
                    overrideController[clip] = newClip;
                }
            }
        }

        animator.runtimeAnimatorController = overrideController;
    }

    void DisplayAvatars()
    {
        foreach (Transform child in ListAvt.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (var avatar in allAvatars)
        {
            GameObject avtObj = Instantiate(AVTPET, ListAvt.transform);
            avtObj.SetActive(true);

            SetupAvatarItem(avtObj, avatar);
        }

        Debug.Log($"[ManagerEquipment] Displayed {allAvatars.Count} avatars");
    }

    void SetupAvatarItem(GameObject avtObj, AvatarEquipmentDTO avatar)
    {
        Button btnSelect = avtObj.transform.Find("btnSelect")?.GetComponent<Button>();
        Text txtChon = avtObj.transform.Find("btnSelect")?.GetComponent<Text>();

        Text prefabTxtDame = avtObj.transform.Find("txtDame")?.GetComponent<Text>();
        Text prefabTxtMana = avtObj.transform.Find("txtMana")?.GetComponent<Text>();
        Text prefabTxtMau = avtObj.transform.Find("txtMau")?.GetComponent<Text>();

        if (prefabTxtDame != null)
            prefabTxtDame.text = avatar.attack.ToString();

        if (prefabTxtMana != null)
            prefabTxtMana.text = avatar.mana.ToString();

        if (prefabTxtMau != null)
            prefabTxtMau.text = avatar.hp.ToString();

        Image avtImage = avtObj.GetComponent<Image>();
        if (avtImage != null)
        {
            Sprite avatarSprite = Resources.Load<Sprite>($"Image/Avt/{avatar.avatarId}");
            if (avatarSprite != null)
            {
                avtImage.sprite = avatarSprite;
            }
            else
            {
                Debug.LogWarning($"[ManagerEquipment] Avatar sprite not found: Image/Avt/{avatar.avatarId}");
            }
        }

        if (btnSelect != null)
        {
            if (avatar.equipped)
            {
                btnSelect.interactable = false;

                if (txtChon != null)
                {
                    txtChon.text = "Đã chọn";
                }

                Debug.Log($"[ManagerEquipment] Avatar {avatar.avatarId} is equipped");
            }
            else
            {
                btnSelect.interactable = true;

                if (txtChon != null)
                {
                    txtChon.text = "Chọn";
                }

                btnSelect.onClick.RemoveAllListeners();
                btnSelect.onClick.AddListener(() => OnSelectAvatar(avatar, avtObj));
            }
        }
    }

    void SetupImgLevel(int level, Image imgLvUser)
    {
        // Load sprite theo level
        imgLvUser.sprite = Resources.Load<Sprite>("Image/hclv/level " + level);

        // Get RectTransform
        RectTransform rectTransform = imgLvUser.GetComponent<RectTransform>();

        // Set size theo level
        if (level >= 1 && level <= 9)
        {
            rectTransform.sizeDelta = new Vector2(40.61f, 35.88f);
        }
        else if (level >= 10 && level <= 14)
        {
            rectTransform.sizeDelta = new Vector2(43.79f, 37.9f);
        }
        else if (level >= 15 && level <= 47)
        {
            rectTransform.sizeDelta = new Vector2(61.35f, 63.51f);
        }
        else if (level >= 48 && level <= 49)
        {
            rectTransform.sizeDelta = new Vector2(70.85f, 73.35f);
        }
        else if (level >= 50 && level <= 60)
        {
            rectTransform.sizeDelta = new Vector2(114.54f, 95.67f);
        }
    }

    // ============================================================
    // ✅ DISPLAY USER INFO
    // ============================================================
    void DisplayUserInfo(UserDetailData userDetail)
    {
        // Hiển thị thông tin cơ bản
        if (txtUserInfoName != null)
        {
            txtUserInfoName.text = userDetail.userName;
        }

        SetupImgLevel(userDetail.level, imgUserInfoLevel);

        // Load hình ảnh avatar
        if (imgUserInfoAvatar != null)
        {
            string imagePath = $"Image/Avt/{userDetail.avtId}";
            Sprite userSprite = Resources.Load<Sprite>(imagePath);
            if (userSprite != null)
            {
                imgUserInfoAvatar.sprite = userSprite;
            }
            else
            {
                Debug.LogWarning($"[ManagerEquipment] Avatar image not found: {imagePath}");
            }
        }

        // Hiển thị thông tin pet đang dùng
        if (userDetail.currentPet != null)
        {
            // Load animation pet
            if (anmtUserInfoPet != null)
            {
                TrySetupPetAnimation(anmtUserInfoPet, userDetail.currentPet.petId.ToString());
            }

            // Hiển thị stats
            if (txtUserInfoAttack != null)
                txtUserInfoAttack.text = userDetail.currentPet.attack.ToString();

            if (txtUserInfoHP != null)
                txtUserInfoHP.text = userDetail.currentPet.hp.ToString();

            if (txtUserInfoMana != null)
                txtUserInfoMana.text = userDetail.currentPet.mana.ToString();
        }

        // Hiển thị danh sách pet
        if (userDetail.allPets != null)
            DisplayUserInfoPetList(userDetail.allPets);

        // Hiển thị danh sách đá
        if (userDetail.stones != null)
            DisplayUserInfoStoneList(userDetail.stones);
    }

    void DisplayUserInfoPetList(List<UserPetInfo> pets)
    {
        if (userInfoPetListContent == null) return;

        // Xóa các item cũ
        foreach (Transform child in userInfoPetListContent)
        {
            Destroy(child.gameObject);
        }

        // Tạo item mới cho mỗi pet
        if (userInfoPetItemPrefab != null)
        {
            foreach (var pet in pets)
            {
                GameObject item = Instantiate(userInfoPetItemPrefab, userInfoPetListContent);
                SetupUserInfoPetItem(item, pet);
            }
        }

        Debug.Log($"[ManagerEquipment] Displayed {pets.Count} pets in user info");
    }

    void SetupUserInfoPetItem(GameObject item, UserPetInfo pet)
    {
        Image imgPet = item.transform.Find("imgtPet")?.GetComponent<Image>();
        Image imgHe = item.transform.Find("imgHe")?.GetComponent<Image>();
        Text txtPetLevel = item.transform.Find("txtLv")?.GetComponent<Text>();

        // Load hình ảnh pet từ Image/IconsPet
        if (imgPet != null)
        {
            string petImagePath = $"Image/IconsPet/{pet.petId}";
            Sprite petSprite = Resources.Load<Sprite>(petImagePath);
            if (petSprite != null)
                imgPet.sprite = petSprite;
            else
                Debug.LogWarning($"[ManagerEquipment] Pet icon not found: {petImagePath}");
        }

        // Load hình ảnh hệ
        if (imgHe != null)
        {
            string elementPath = $"Image/Attribute/{pet.elementType}";
            Sprite elementSprite = Resources.Load<Sprite>(elementPath);
            if (elementSprite != null)
                imgHe.sprite = elementSprite;
            else
                Debug.LogWarning($"[ManagerEquipment] Element icon not found: {elementPath}");
        }

        // Hiển thị level
        if (txtPetLevel != null)
            txtPetLevel.text = "Lv." + pet.level;
    }

    void DisplayUserInfoStoneList(List<StoneInfo> stones)
    {
        if (userInfoStoneListContent == null) return;

        // Xóa các item cũ
        foreach (Transform child in userInfoStoneListContent)
        {
            Destroy(child.gameObject);
        }

        // Tạo item mới cho mỗi đá
        if (userInfoStoneItemPrefab != null)
        {
            foreach (var stone in stones)
            {
                GameObject item = Instantiate(userInfoStoneItemPrefab, userInfoStoneListContent);
                SetupUserInfoStoneItem(item, stone);
            }
        }

        Debug.Log($"[ManagerEquipment] Displayed {stones.Count} stones in user info");
    }

    void SetupUserInfoStoneItem(GameObject item, StoneInfo stone)
    {
        Image imgStone = item.transform.Find("imgStone")?.GetComponent<Image>();
        Text txtCount = item.transform.Find("txtnum")?.GetComponent<Text>();

        // Load hình ảnh đá từ dictionary
        if (imgStone != null)
        {
            Sprite stoneSprite = GetStoneSprite(stone.stoneId);
            if (stoneSprite != null)
            {
                imgStone.sprite = stoneSprite;
            }
            else
            {
                Debug.LogWarning($"[ManagerEquipment] Không tìm thấy sprite cho đá ID: {stone.stoneId}");
            }
        }

        // Hiển thị số lượng
        if (txtCount != null)
            txtCount.text = stone.count.ToString();
    }

    // ============================================================
    // ✅ SETUP PET ANIMATION - DÙNG CHO TAB USER INFO
    // ============================================================
    bool TrySetupPetAnimation(Animator petAnimator, string petID)
    {
        if (petAnimator == null)
        {
            Debug.LogWarning("[ManagerEquipment] Không tìm thấy Animator cho pet");
            return false;
        }

        try
        {
            // BƯỚC 1: Set image backup
            Image petImage = petAnimator.GetComponent<Image>();
            if (petImage == null)
            {
                petImage = petAnimator.gameObject.AddComponent<Image>();
                Debug.Log($"[ManagerEquipment][Pet {petID}] Added Image component");
            }

            // Load và set sprite
            string spritePath = $"Image/IconsPet/{petID}";
            Sprite petSprite = Resources.Load<Sprite>(spritePath);

            if (petSprite != null)
            {
                petImage.sprite = petSprite;
                petImage.enabled = true;
                Debug.Log($"[ManagerEquipment][Pet {petID}] ✓ Set image backup: {spritePath}");
            }
            else
            {
                Debug.LogWarning($"[ManagerEquipment][Pet {petID}] Không tìm thấy icon tại {spritePath}");
            }

            // BƯỚC 2: Thử load animation IdleT
            string clipPath = $"Pets/{petID}/IdleT";
            AnimationClip idleClip = Resources.Load<AnimationClip>(clipPath);

            Debug.Log($"[ManagerEquipment][Pet {petID}] Loading animation from: {clipPath}");

            if (idleClip == null)
            {
                Debug.LogWarning($"[ManagerEquipment][Pet {petID}] Không tìm thấy IdleT clip - Dùng image tĩnh");
                petAnimator.enabled = false;
                return true;
            }

            // BƯỚC 3: Có animation thì setup
            RuntimeAnimatorController baseController = petAnimator.runtimeAnimatorController;

            if (baseController == null)
            {
                Debug.LogError($"[ManagerEquipment][Pet {petID}] Animator không có base controller! Dùng image thay thế.");
                petAnimator.enabled = false;
                return true;
            }

            Debug.Log($"[ManagerEquipment][Pet {petID}] Base controller: {baseController.name}");

            AnimatorOverrideController overrideController = new AnimatorOverrideController(baseController);
            overrideController.name = $"Override_{petID}_{petAnimator.GetInstanceID()}";

            // Override clip IdleT
            Debug.Log($"[ManagerEquipment][Pet {petID}] Override clip: IdleT (length: {idleClip.length}s)");
            overrideController["IdleT"] = idleClip;

            // Apply override controller
            petAnimator.runtimeAnimatorController = overrideController;
            petAnimator.enabled = true;

            // Force refresh animator
            petAnimator.Rebind();
            petAnimator.Update(0f);

            // Ensure GameObject active
            if (!petAnimator.gameObject.activeSelf)
            {
                petAnimator.gameObject.SetActive(true);
                Debug.Log($"[ManagerEquipment][Pet {petID}] Activated GameObject");
            }

            Debug.Log($"[ManagerEquipment][Pet {petID}] ✓ Setup thành công IdleT animation");

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[ManagerEquipment][Pet {petID}] Lỗi: {e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    // ============================================================
    // SELECT LOGIC - FIXED (GIỮ NGUYÊN)
    // ============================================================

    void OnSelectPet(PetEquipmentDTO pet, GameObject petObj)
    {
        Debug.Log($"[ManagerEquipment] Selecting pet: {pet.name} (ID: {pet.petId})");
        StartCoroutine(EquipPetCoroutine(pet, petObj));
    }

    IEnumerator EquipPetCoroutine(PetEquipmentDTO pet, GameObject petObj)
    {
        string url = APIConfig.EQUIP_PET(userId);
        EquipPetRequest request = new EquipPetRequest { petId = pet.petId };

        yield return APIManager.Instance.PostRequest<EquipResponse>(
            url,
            request,
            (response) => OnPetEquipped(response, pet, petObj),
            (error) =>
            {
                Debug.LogError($"[ManagerEquipment] Equip pet error: {error}");
            }
        );
    }

    void OnPetEquipped(EquipResponse response, PetEquipmentDTO pet, GameObject petObj)
    {
        Debug.Log($"[ManagerEquipment] Pet equipped successfully: {response.message}");

        currentEquippedPetId = pet.petId;
        UpdateStatsDisplay(pet.attack, pet.mana, pet.hp);

        StartCoroutine(LoadPetsData());
    }

    void OnSelectAvatar(AvatarEquipmentDTO avatar, GameObject avtObj)
    {
        ManagerQuangTruong.Instance.imgAvatar.sprite = Resources.Load<Sprite>($"Image/Avt/{avatar.avatarId}");
        imgUserInfoAvatar.sprite = Resources.Load<Sprite>($"Image/Avt/{avatar.avatarId}");
        Debug.Log($"[ManagerEquipment] Selecting avatar: {avatar.name} (ID: {avatar.avatarId})");
        StartCoroutine(EquipAvatarCoroutine(avatar, avtObj));
    }

    IEnumerator EquipAvatarCoroutine(AvatarEquipmentDTO avatar, GameObject avtObj)
    {
        string url = APIConfig.EQUIP_AVATAR(userId);
        EquipAvatarRequest request = new EquipAvatarRequest { avatarId = avatar.avatarId };

        yield return APIManager.Instance.PostRequest<EquipResponse>(
            url,
            request,
            (response) => OnAvatarEquipped(response, avatar, avtObj),
            (error) =>
            {
                Debug.LogError($"[ManagerEquipment] Equip avatar error: {error}");
            }
        );
    }

    void OnAvatarEquipped(EquipResponse response, AvatarEquipmentDTO avatar, GameObject avtObj)
    {
        Debug.Log($"[ManagerEquipment] Avatar equipped successfully: {response.message}");

        currentEquippedAvatarId = avatar.avatarId;
        UpdateStatsDisplay(avatar.attack, avatar.mana, avatar.hp);

        StartCoroutine(LoadAvatarsData());
    }

    void UpdateStatsDisplay(int attack, int mana, int hp)
    {
        if (txtDame != null)
            txtDame.text = attack.ToString();

        if (txtMana != null)
            txtMana.text = mana.ToString();

        if (txtMau != null)
            txtMau.text = hp.ToString();

        Debug.Log($"[ManagerEquipment] Stats updated - HP: {hp}, ATK: {attack}, MANA: {mana}");
    }

    // ============================================================
    // PAGINATION (GIỮ NGUYÊN)
    // ============================================================

    void PreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;

            if (currentTab == 0)
            {
                StartCoroutine(LoadPetsData());
            }
            else if (currentTab == 1)
            {
                StartCoroutine(LoadAvatarsData());
            }
        }
    }

    void NextPage()
    {
        if (currentPage < totalPages - 1)
        {
            currentPage++;

            if (currentTab == 0)
            {
                StartCoroutine(LoadPetsData());
            }
            else if (currentTab == 1)
            {
                StartCoroutine(LoadAvatarsData());
            }
        }
    }

    void UpdatePageInfo()
    {
        if (txtPageInfo != null)
        {
            txtPageInfo.text = $"Page {currentPage + 1}/{totalPages}";
        }
    }

    void UpdateNavigationButtons()
    {
        if (btnLeft != null)
        {
            btnLeft.interactable = currentPage > 0;
        }

        if (btnRight != null)
        {
            btnRight.interactable = currentPage < totalPages - 1;
        }
    }
}