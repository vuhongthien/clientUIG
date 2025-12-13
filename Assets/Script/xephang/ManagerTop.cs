using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

public class ManagerTop : MonoBehaviour
{
    [Header("UI References")]
    public Button btnTop;
    public GameObject PanelXepHang;
    public Button btnBack;

    [Header("Ranking Items - Có sẵn 9 userT")]
    public Transform[] userTItems = new Transform[9]; // Kéo thả 9 userT vào đây

    [Header("User Detail Panel")]
    public GameObject panelDetailTop;
    public Button btnCloseDetail;
    public Image imgUserDetail;
    public Text txtUserNameDetail;
    public Image imgUserLevelDetail;
    public Animator anmtCurrentPet;

    [Header("Current Pet Stats")]
    public Text txtAttack;
    public Text txtHP;
    public Text txtMana;

    [Header("Pet List")]
    public Transform petListContent;
    public GameObject petItemPrefab;

    [Header("Stone List")]
    public Transform stoneListContent;
    public GameObject stoneItemPrefab;

    [Header("Stone Images - 5 Hệ, mỗi hệ 7 Level")]
    [Tooltip("Hệ Lửa - 7 level")]
    public Sprite[] stoneFire = new Sprite[7]; // ID 1-7

    [Tooltip("Hệ Nước - 7 level")]
    public Sprite[] stoneWater = new Sprite[7]; // ID 8-14

    [Tooltip("Hệ Gió - 7 level")]
    public Sprite[] stoneWind = new Sprite[7]; // ID 15-21

    [Tooltip("Hệ Đất - 7 level")]
    public Sprite[] stoneEarth = new Sprite[7]; // ID 22-28

    [Tooltip("Hệ Sét - 7 level")]
    public Sprite[] stoneThunder = new Sprite[7]; // ID 29-35

    [Header("Animation Settings")]
    public float panelAnimDuration = 0.3f;
    public float itemAnimDelay = 0.05f;
    public LeanTweenType easeType = LeanTweenType.easeOutBack;

    private List<TopRankingData> currentRankings = new List<TopRankingData>();
    private Dictionary<int, Sprite> stoneDictionary;
    private CanvasGroup panelXepHangCanvasGroup;
    private CanvasGroup panelDetailCanvasGroup;

    void Start()
    {
        // Setup CanvasGroup cho panels
        SetupCanvasGroups();

        // Khởi tạo dictionary cho đá
        InitializeStoneDictionary();

        // Đăng ký sự kiện click cho các button
        btnTop.onClick.AddListener(OnTopButtonClicked);
        btnBack.onClick.AddListener(OnCloseRankingClicked);

        if (btnCloseDetail != null)
            btnCloseDetail.onClick.AddListener(OnCloseDetailClicked);

        // Ẩn các panel ban đầu
        PanelXepHang.SetActive(false);
        if (panelDetailTop != null)
            panelDetailTop.SetActive(false);
    }

    void SetupCanvasGroups()
    {
        // Setup cho PanelXepHang
        panelXepHangCanvasGroup = PanelXepHang.GetComponent<CanvasGroup>();
        if (panelXepHangCanvasGroup == null)
            panelXepHangCanvasGroup = PanelXepHang.AddComponent<CanvasGroup>();

        // Setup cho panelDetailTop
        if (panelDetailTop != null)
        {
            panelDetailCanvasGroup = panelDetailTop.GetComponent<CanvasGroup>();
            if (panelDetailCanvasGroup == null)
                panelDetailCanvasGroup = panelDetailTop.AddComponent<CanvasGroup>();
        }
    }

    void InitializeStoneDictionary()
    {
        stoneDictionary = new Dictionary<int, Sprite>();

        // Hệ Lửa (ID 1-7)
        for (int i = 0; i < stoneFire.Length; i++)
        {
            if (stoneFire[i] != null)
            {
                int stoneId = i + 1;
                stoneDictionary[stoneId] = stoneFire[i];
            }
            else
            {
                Debug.LogWarning($"Stone Lửa Level {i + 1} (ID: {i + 1}) chưa được gán!");
            }
        }

        // Hệ Nước (ID 8-14)
        for (int i = 0; i < stoneWater.Length; i++)
        {
            if (stoneWater[i] != null)
            {
                int stoneId = 8 + i;
                stoneDictionary[stoneId] = stoneWater[i];
            }
            else
            {
                Debug.LogWarning($"Stone Nước Level {i + 1} (ID: {8 + i}) chưa được gán!");
            }
        }

        // Hệ Gió (ID 15-21)
        for (int i = 0; i < stoneWind.Length; i++)
        {
            if (stoneWind[i] != null)
            {
                int stoneId = 15 + i;
                stoneDictionary[stoneId] = stoneWind[i];
            }
            else
            {
                Debug.LogWarning($"Stone Gió Level {i + 1} (ID: {15 + i}) chưa được gán!");
            }
        }

        // Hệ Đất (ID 22-28)
        for (int i = 0; i < stoneEarth.Length; i++)
        {
            if (stoneEarth[i] != null)
            {
                int stoneId = 22 + i;
                stoneDictionary[stoneId] = stoneEarth[i];
            }
            else
            {
                Debug.LogWarning($"Stone Đất Level {i + 1} (ID: {22 + i}) chưa được gán!");
            }
        }

        // Hệ Sét (ID 29-35)
        for (int i = 0; i < stoneThunder.Length; i++)
        {
            if (stoneThunder[i] != null)
            {
                int stoneId = 29 + i;
                stoneDictionary[stoneId] = stoneThunder[i];
            }
            else
            {
                Debug.LogWarning($"Stone Sét Level {i + 1} (ID: {29 + i}) chưa được gán!");
            }
        }

        Debug.Log($"=== STONE LOADING SUMMARY ===");
        Debug.Log($"Hệ Lửa: {CountValidSprites(stoneFire)}/7 sprites");
        Debug.Log($"Hệ Nước: {CountValidSprites(stoneWater)}/7 sprites");
        Debug.Log($"Hệ Gió: {CountValidSprites(stoneWind)}/7 sprites");
        Debug.Log($"Hệ Đất: {CountValidSprites(stoneEarth)}/7 sprites");
        Debug.Log($"Hệ Sét: {CountValidSprites(stoneThunder)}/7 sprites");
        Debug.Log($"Tổng: {stoneDictionary.Count}/35 ảnh đá đã load");
    }

    int CountValidSprites(Sprite[] sprites)
    {
        int count = 0;
        foreach (var sprite in sprites)
        {
            if (sprite != null) count++;
        }
        return count;
    }

    Sprite GetStoneSprite(long stoneId)
    {
        if (stoneDictionary != null && stoneDictionary.ContainsKey((int)stoneId))
        {
            return stoneDictionary[(int)stoneId];
        }

        Debug.LogWarning($"Không tìm thấy sprite cho đá ID: {stoneId}");
        return null;
    }

    void OnTopButtonClicked()
    {
        // Animation mở panel
        PanelXepHang.SetActive(true);
        AnimateOpenPanel(PanelXepHang, panelXepHangCanvasGroup);
        
        // Scale animation cho button
        LeanTween.scale(btnTop.gameObject, Vector3.one * 0.9f, 0.1f)
            .setEaseInOutQuad()
            .setOnComplete(() => {
                LeanTween.scale(btnTop.gameObject, Vector3.one, 0.1f).setEaseInOutQuad();
            });

        LoadTop9Ranking();
    }

    void OnCloseRankingClicked()
    {
        AnimateClosePanel(PanelXepHang, panelXepHangCanvasGroup);
        
        // Scale animation cho button
        LeanTween.scale(btnBack.gameObject, Vector3.one * 0.9f, 0.1f)
            .setEaseInOutQuad()
            .setOnComplete(() => {
                LeanTween.scale(btnBack.gameObject, Vector3.one, 0.1f).setEaseInOutQuad();
            });
    }

    void OnCloseDetailClicked()
    {
        if (panelDetailTop != null)
        {
            AnimateClosePanel(panelDetailTop, panelDetailCanvasGroup);
            
            // Scale animation cho button
            LeanTween.scale(btnCloseDetail.gameObject, Vector3.one * 0.9f, 0.1f)
                .setEaseInOutQuad()
                .setOnComplete(() => {
                    LeanTween.scale(btnCloseDetail.gameObject, Vector3.one, 0.1f).setEaseInOutQuad();
                });
        }
    }

    // Animation mở panel với scale + fade
    void AnimateOpenPanel(GameObject panel, CanvasGroup canvasGroup)
    {
        // Reset transform
        panel.transform.localScale = Vector3.zero;
        canvasGroup.alpha = 0f;

        // Scale animation
        LeanTween.scale(panel, Vector3.one, panelAnimDuration)
            .setEase(easeType);

        // Fade animation
        LeanTween.alphaCanvas(canvasGroup, 1f, panelAnimDuration)
            .setEase(LeanTweenType.easeInOutQuad);
    }

    // Animation đóng panel với scale + fade
    void AnimateClosePanel(GameObject panel, CanvasGroup canvasGroup)
    {
        // Scale animation
        LeanTween.scale(panel, Vector3.zero, panelAnimDuration)
            .setEase(LeanTweenType.easeInBack);

        // Fade animation
        LeanTween.alphaCanvas(canvasGroup, 0f, panelAnimDuration)
            .setEase(LeanTweenType.easeInOutQuad)
            .setOnComplete(() => {
                panel.SetActive(false);
            });
    }

    void LoadTop9Ranking()
    {
        StartCoroutine(FetchTop9Ranking());
    }

    IEnumerator FetchTop9Ranking()
    {
        string url = APIConfig.GET_TOP9_RANKING;

        yield return StartCoroutine(APIManager.Instance.GetRequest<List<TopRankingData>>(
            url,
            onSuccess: (rankings) =>
            {
                currentRankings = rankings;
                DisplayRankings(rankings);
            },
            onError: (error) =>
            {
                Debug.LogError("Failed to load rankings: " + error);
            }
        ));
    }

    void DisplayRankings(List<TopRankingData> rankings)
    {
        // Ẩn tất cả userT trước
        for (int i = 0; i < userTItems.Length; i++)
        {
            if (userTItems[i] != null)
                userTItems[i].gameObject.SetActive(false);
        }

        // Hiển thị và set dữ liệu cho từng ranking với animation tuần tự
        for (int i = 0; i < rankings.Count && i < userTItems.Length; i++)
        {
            if (userTItems[i] != null)
            {
                int index = i; // Capture index for closure
                userTItems[index].gameObject.SetActive(true);
                SetupRankingItem(userTItems[index], rankings[index]);
                AnimateRankingItem(userTItems[index].gameObject, index);
            }
        }
    }

    // Animation cho từng ranking item
    void AnimateRankingItem(GameObject item, int index)
    {
        // Reset
        item.transform.localScale = Vector3.zero;
        
        CanvasGroup canvasGroup = item.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = item.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        float delay = index * itemAnimDelay;

        // Scale animation với delay tuần tự
        LeanTween.scale(item, Vector3.one, 0.4f)
            .setDelay(delay)
            .setEase(LeanTweenType.easeOutBack);

        // Fade animation
        LeanTween.alphaCanvas(canvasGroup, 1f, 0.3f)
            .setDelay(delay)
            .setEase(LeanTweenType.easeInOutQuad);

        // Thêm bounce nhẹ
        LeanTween.moveLocalY(item, item.transform.localPosition.y, 0.3f)
            .setDelay(delay)
            .setFrom(item.transform.localPosition.y - 30f)
            .setEase(LeanTweenType.easeOutQuad);
    }

    void SetupRankingItem(Transform userT, TopRankingData ranking)
    {
        // Lấy các component từ userT
        Image imgUser = userT.Find("imgUser")?.GetComponent<Image>();
        Animator anmtP = userT.Find("anmtP")?.GetComponent<Animator>();
        Text txtName = userT.Find("txtName")?.GetComponent<Text>();
        Text txtLC = userT.Find("txtLC")?.GetComponent<Text>();
        Text txtTOP = userT.Find("txtTOP")?.GetComponent<Text>();
        Button btnUserItem = userT.GetComponent<Button>();

        // Set dữ liệu
        if (txtName != null)
            txtName.text = ranking.userName;

        if (txtLC != null)
            txtLC.text = ranking.totalCombatPower.ToString();

        if (txtTOP != null)
            txtTOP.text = "Top " + ranking.rank;

        // Load hình ảnh user
        if (imgUser != null)
        {
            string imagePath = $"Image/Avt/{ranking.avtId}";
            Sprite userSprite = Resources.Load<Sprite>(imagePath);
            if (userSprite != null)
                imgUser.sprite = userSprite;
            else
                Debug.LogWarning($"Image not found: {imagePath}");
        }

        // Load animation pet
        if (anmtP != null && ranking.currentPetId > 0)
        {
            TrySetupPetAnimation(anmtP, ranking.currentPetId.ToString());
        }

        // Thêm sự kiện click để hiện detail với animation
        if (btnUserItem != null)
        {
            btnUserItem.onClick.RemoveAllListeners();
            btnUserItem.onClick.AddListener(() => {
                // Scale animation khi click
                LeanTween.scale(userT.gameObject, Vector3.one * 1.1f, 0.1f)
                    .setEaseInOutQuad()
                    .setOnComplete(() => {
                        LeanTween.scale(userT.gameObject, Vector3.one, 0.1f)
                            .setEaseInOutQuad();
                    });
                
                OnUserItemClicked(ranking.userId);
            });
        }
    }

    void OnUserItemClicked(long userId)
    {
        StartCoroutine(FetchUserDetail((int)userId));
    }

    IEnumerator FetchUserDetail(int userId)
    {
        string url = APIConfig.GET_USER_DETAIL(userId);

        yield return StartCoroutine(APIManager.Instance.GetRequest<UserDetailData>(
            url,
            onSuccess: (userDetail) =>
            {
                DisplayUserDetail(userDetail);
            },
            onError: (error) =>
            {
                Debug.LogError("Failed to load user detail: " + error);
            }
        ));
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

    void DisplayUserDetail(UserDetailData userDetail)
    {
        if (panelDetailTop != null)
        {
            panelDetailTop.SetActive(true);
            AnimateOpenPanel(panelDetailTop, panelDetailCanvasGroup);
        }

        // Hiển thị thông tin cơ bản với animation
        if (txtUserNameDetail != null)
        {
            txtUserNameDetail.text = userDetail.userName;
            AnimateText(txtUserNameDetail.gameObject, 0.1f);
        }

        SetupImgLevel(userDetail.level, imgUserLevelDetail);

        // Load hình ảnh user với animation
        if (imgUserDetail != null)
        {
            string imagePath = $"Image/Avt/{userDetail.avtId}";
            Sprite userSprite = Resources.Load<Sprite>(imagePath);
            if (userSprite != null)
            {
                imgUserDetail.sprite = userSprite;
                AnimateImage(imgUserDetail.gameObject, 0.2f);
            }
        }

        // Hiển thị thông tin pet đang dùng
        if (userDetail.currentPet != null)
        {
            // Load animation pet
            if (anmtCurrentPet != null)
            {
                TrySetupPetAnimation(anmtCurrentPet, userDetail.currentPet.petId.ToString());
                AnimateImage(anmtCurrentPet.gameObject, 0.25f);
            }

            // Hiển thị stats với animation
            if (txtAttack != null)
            {
                AnimateNumberText(txtAttack, 0, userDetail.currentPet.attack, 0.3f, 0.5f);
            }

            if (txtHP != null)
            {
                AnimateNumberText(txtHP, 0, userDetail.currentPet.hp, 0.35f, 0.5f);
            }

            if (txtMana != null)
            {
                AnimateNumberText(txtMana, 0, userDetail.currentPet.mana, 0.4f, 0.5f);
            }
        }

        // Hiển thị danh sách pet
        if (userDetail.allPets != null)
            DisplayPetList(userDetail.allPets);

        // Hiển thị danh sách đá
        if (userDetail.stones != null)
            DisplayStoneList(userDetail.stones);
    }

    // Animation cho text
    void AnimateText(GameObject textObj, float delay)
    {
        textObj.transform.localScale = Vector3.zero;
        LeanTween.scale(textObj, Vector3.one, 0.3f)
            .setDelay(delay)
            .setEase(LeanTweenType.easeOutBack);
    }

    // Animation cho image
    void AnimateImage(GameObject imgObj, float delay)
    {
        imgObj.transform.localScale = Vector3.zero;
        LeanTween.scale(imgObj, Vector3.one, 0.4f)
            .setDelay(delay)
            .setEase(LeanTweenType.easeOutBack);
        
        // Thêm rotation nhẹ
        imgObj.transform.rotation = Quaternion.Euler(0, 0, 10f);
        LeanTween.rotateZ(imgObj, 0f, 0.4f)
            .setDelay(delay)
            .setEase(LeanTweenType.easeOutBack);
    }

    // Animation cho số đếm lên
    void AnimateNumberText(Text textComponent, int fromValue, int toValue, float delay, float duration)
    {
        textComponent.transform.localScale = Vector3.zero;
        LeanTween.scale(textComponent.gameObject, Vector3.one, 0.3f)
            .setDelay(delay)
            .setEase(LeanTweenType.easeOutBack);

        // Counter animation
        LeanTween.value(textComponent.gameObject, fromValue, toValue, duration)
            .setDelay(delay + 0.2f)
            .setOnUpdate((float val) => {
                textComponent.text = Mathf.RoundToInt(val).ToString();
            })
            .setEase(LeanTweenType.easeOutQuad);
    }

    void DisplayPetList(List<UserPetInfo> pets)
{
    if (petListContent == null) return;

    // Xóa các item cũ
    foreach (Transform child in petListContent)
    {
        Destroy(child.gameObject);
    }

    // Tạo item mới cho mỗi pet
    if (petItemPrefab != null)
    {
        for (int i = 0; i < pets.Count; i++)
        {
            GameObject item = Instantiate(petItemPrefab, petListContent);
            SetupPetItem(item, pets[i]);
            
            // Đợi 1 frame để Layout tính toán
            StartCoroutine(DelayedAnimatePetItem(item, i));
        }
    }
}

// Thêm coroutine để đợi Layout rebuild
IEnumerator DelayedAnimatePetItem(GameObject item, int index)
{
    yield return null; // Đợi 1 frame
    AnimatePetItem(item, index);
}

    // Animation cho pet item
    // Animation cho pet item - FIX
void AnimatePetItem(GameObject item, int index)
{
    // Để Layout tính toán trước
    LayoutRebuilder.ForceRebuildLayoutImmediate(petListContent.GetComponent<RectTransform>());
    
    item.transform.localScale = Vector3.zero;
    
    float delay = 0.5f + (index * 0.05f);
    
    LeanTween.scale(item, Vector3.one, 0.3f)
        .setDelay(delay)
        .setEase(LeanTweenType.easeOutBack);

    // KHÔNG DÙNG moveLocalX vì sẽ conflict với Layout!
    // Chỉ dùng fade thay thế
    CanvasGroup canvasGroup = item.GetComponent<CanvasGroup>();
    if (canvasGroup == null)
        canvasGroup = item.AddComponent<CanvasGroup>();
    
    canvasGroup.alpha = 0f;
    LeanTween.alphaCanvas(canvasGroup, 1f, 0.3f)
        .setDelay(delay)
        .setEase(LeanTweenType.easeInOutQuad);
}

    void SetupPetItem(GameObject item, UserPetInfo pet)
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
                Debug.LogWarning($"Pet icon not found: {petImagePath}");
        }

        if (imgHe != null)
        {
            string petImagePath = $"Image/Attribute/{pet.elementType}";
            Sprite petSprite = Resources.Load<Sprite>(petImagePath);
            if (petSprite != null)
                imgHe.sprite = petSprite;
            else
                Debug.LogWarning($"Pet he not found: {petImagePath}");
        }

        // Hiển thị level
        if (txtPetLevel != null)
            txtPetLevel.text = "Lv." + pet.level;
    }

    void DisplayStoneList(List<StoneInfo> stones)
    {
        if (stoneListContent == null) return;

        // Xóa các item cũ
        foreach (Transform child in stoneListContent)
        {
            Destroy(child.gameObject);
        }

        // Tạo item mới cho mỗi đá (nếu có prefab)
        if (stoneItemPrefab != null)
        {
            for (int i = 0; i < stones.Count; i++)
            {
                GameObject item = Instantiate(stoneItemPrefab, stoneListContent);
                SetupStoneItem(item, stones[i]);
                AnimateStoneItem(item, i);
            }
        }
    }

    // Animation cho stone item
    void AnimateStoneItem(GameObject item, int index)
    {
        item.transform.localScale = Vector3.zero;
        
        float delay = 0.6f + (index * 0.04f);
        
        LeanTween.scale(item, Vector3.one, 0.3f)
            .setDelay(delay)
            .setEase(LeanTweenType.easeOutBack);

        // Fade animation
        CanvasGroup canvasGroup = item.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = item.AddComponent<CanvasGroup>();
        
        canvasGroup.alpha = 0f;
        LeanTween.alphaCanvas(canvasGroup, 1f, 0.3f)
            .setDelay(delay)
            .setEase(LeanTweenType.easeInOutQuad);
    }

    void SetupStoneItem(GameObject item, StoneInfo stone)
    {
        Image imgStone = item.transform.Find("imgStone")?.GetComponent<Image>();
        Text txtCount = item.transform.Find("txtnum")?.GetComponent<Text>();

        // Load hình ảnh đá từ 5 list theo hệ
        if (imgStone != null)
        {
            Sprite stoneSprite = GetStoneSprite(stone.stoneId);
            if (stoneSprite != null)
            {
                imgStone.sprite = stoneSprite;
            }
            else
            {
                Debug.LogWarning($"Không tìm thấy sprite cho đá ID: {stone.stoneId}");
            }
        }

        // Hiển thị số lượng
        if (txtCount != null)
            txtCount.text = stone.count.ToString();
    }

    bool TrySetupPetAnimation(Animator petAnimator, string petID)
    {
        if (petAnimator == null)
        {
            Debug.LogWarning("Không tìm thấy Animator cho pet");
            return false;
        }

        try
        {
            // BƯỚC 1: LUÔN LUÔN set image trước làm backup
            UnityEngine.UI.Image petImage = petAnimator.GetComponent<UnityEngine.UI.Image>();
            if (petImage == null)
            {
                petImage = petAnimator.gameObject.AddComponent<UnityEngine.UI.Image>();
                Debug.Log($"[Pet {petID}] Added Image component");
            }

            // Load và set sprite từ Resources/Image/IconsPet
            string spritePath = $"Image/IconsPet/{petID}";
            Sprite petSprite = Resources.Load<Sprite>(spritePath);

            if (petSprite != null)
            {
                petImage.sprite = petSprite;
                petImage.enabled = true; // LUÔN ENABLED!
                Debug.Log($"[Pet {petID}] ✓ Set image backup: {spritePath}");
            }
            else
            {
                Debug.LogWarning($"[Pet {petID}] Không tìm thấy icon tại {spritePath}");
            }

            // BƯỚC 2: Thử load animation IdleT
            string clipPath = $"Pets/{petID}/IdleT";
            AnimationClip idleClip = Resources.Load<AnimationClip>(clipPath);

            Debug.Log($"[Pet {petID}] Loading animation from: {clipPath}");

            if (idleClip == null)
            {
                Debug.LogWarning($"[Pet {petID}] Không tìm thấy IdleT clip - Dùng image tĩnh");

                // Tắt animator, image sẽ hiển thị
                petAnimator.enabled = false;
                return true;
            }

            // BƯỚC 3: Có animation thì setup
            RuntimeAnimatorController baseController = petAnimator.runtimeAnimatorController;

            if (baseController == null)
            {
                Debug.LogError($"[Pet {petID}] Animator không có base controller! Dùng image thay thế.");
                petAnimator.enabled = false;
                return true;
            }

            Debug.Log($"[Pet {petID}] Base controller: {baseController.name}");

            AnimatorOverrideController overrideController = new AnimatorOverrideController(baseController);
            overrideController.name = $"Override_{petID}_{petAnimator.GetInstanceID()}";

            // Override clip IdleT
            Debug.Log($"[Pet {petID}] Override clip: IdleT (length: {idleClip.length}s)");
            overrideController["IdleT"] = idleClip;

            // Apply override controller
            petAnimator.runtimeAnimatorController = overrideController;
            petAnimator.enabled = true;

            // KHÔNG TẮT IMAGE - Để làm backup nếu animation lỗi!
            // petImage.enabled vẫn = true

            // Force refresh animator
            petAnimator.Rebind();
            petAnimator.Update(0f);

            // Ensure GameObject active
            if (!petAnimator.gameObject.activeSelf)
            {
                petAnimator.gameObject.SetActive(true);
                Debug.Log($"[Pet {petID}] Activated GameObject");
            }

            Debug.Log($"[Pet {petID}] ✓ Setup thành công IdleT animation (Image làm backup)");

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Pet {petID}] Lỗi: {e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    void OnDestroy()
    {
        // Cancel tất cả LeanTween animations khi destroy
        LeanTween.cancel(gameObject);
        
        if (PanelXepHang != null)
            LeanTween.cancel(PanelXepHang);
        
        if (panelDetailTop != null)
            LeanTween.cancel(panelDetailTop);
    }
}