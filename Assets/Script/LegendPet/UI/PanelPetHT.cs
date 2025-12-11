using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace PokiGame.LegendPet
{
    public class PanelPetHT : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject panelObject;
        [SerializeField] private Text petNameText;
        [SerializeField] private Text progressText;
        [SerializeField] private Image progressBar;
        [SerializeField] private Button closeButton;

        [Header("Pet Selection Buttons")]
        [SerializeField] private Button[] btnHTs;
        [SerializeField] private Text[] btnHtTexts; // Tên pet trên button
        [SerializeField] private Image[] btnHtIcons; // Icon pet trên button (optional)

        [Header("Image Panels - Các ImageHT1, ImageHT2, ...")]
        [SerializeField] private GameObject[] imagePanels;

        [Header("Star Info Display")]
        [SerializeField] private Text starWhiteText;
        [SerializeField] private Text starBlueText;
        [SerializeField] private Text starRedText;

        [Header("Confirm Panel")]
        [SerializeField] private GameObject confirmPanelObject;
        [SerializeField] private Image confirmIcon;
        [SerializeField] private Text confirmMessageTxt;
        [SerializeField] private Button confirmBtnOK;
        [SerializeField] private Button confirmBtnCancel;
        [SerializeField] private Sprite starWhiteIcon;
        [SerializeField] private Sprite starBlueIcon;
        [SerializeField] private Sprite starRedIcon;
        [SerializeField] private CanvasGroup confirmCanvasGroup;

        [Header("Other Panels")]
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private GameObject petUnlockPanel;
        [SerializeField] private GameObject PanelPetMain;

        [Header("Debug")]
        [SerializeField] private Button btnTestAPI; // Thêm button test trong Inspector
        [SerializeField] private bool autoShowOnStart = false; // Auto show để test
        public GameObject PanelCardPet;
        public Image imgCard;
        public Text txtDescription;
        public Text namePet;
        public Text txtHp;
        public Text txtMana;
        public Text txtDame;
        public Text txtWee;
        public Text txtLv;
        public Text des;
        public Image imgAtribute;
        public Image imgAtributeOther;

        // Data structures
        private LegendPetBasicInfo[] allPets;
        private LegendPetData currentPetData;
        private long userId;
        private int currentPetIndex = 0;
        private int currentImageIndex = 0;
        private InlayStarRequest lastInlayRequest;

        // Star management
        private Dictionary<long, Button> starButtonDict = new Dictionary<long, Button>();
        private Dictionary<long, Image> starImageDict = new Dictionary<long, Image>();
        private StarSlotData currentConfirmSlotData;
        private Dictionary<long, PetUserDTO> userPetsCache = new Dictionary<long, PetUserDTO>();
        private PetUserDTO currentUserPetData = null;
        private bool isLoadingUserPet = false;


        private void Awake()
        {
            Debug.Log("[PanelPetHT] ===== AWAKE CALLED =====");

            // Setup pet selection buttons
            for (int i = 0; i < btnHTs.Length; i++)
            {
                if (btnHTs[i] != null)
                {
                    int index = i;
                    btnHTs[i].onClick.AddListener(() => OnPetButtonClick(index));
                }
            }

            // Setup confirm panel buttons
            if (confirmBtnOK != null)
                confirmBtnOK.onClick.AddListener(() => OnConfirm(true));

            if (confirmBtnCancel != null)
                confirmBtnCancel.onClick.AddListener(() => OnConfirm(false));

            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);

            // Setup test button
            if (btnTestAPI != null)
            {
                btnTestAPI.onClick.AddListener(TestAPI);
                Debug.Log("[PanelPetHT] Test API button configured");
            }

            // Hide confirm panel initially
            if (confirmPanelObject != null)
                confirmPanelObject.SetActive(false);

            if (StarEventManager.Instance != null)
            {
                StarEventManager.Instance.OnStarCountChanged += OnStarCountChangedHandler;
            }

            Debug.Log("[PanelPetHT] Awake completed");
        }

        private void Start()
        {
            userId = GetCurrentUserId();
            Debug.Log($"[PanelPetHT] Start() - UserID: {userId}");

            // Kiểm tra và log số sao ban đầu
            Debug.Log($"[PanelPetHT] Initial stars:");
            Debug.Log($"  - StarWhite: {PlayerPrefs.GetInt("StarWhite", 0)}");
            Debug.Log($"  - StarBlue: {PlayerPrefs.GetInt("StarBlue", 0)}");
            Debug.Log($"  - StarRed: {PlayerPrefs.GetInt("StarRed", 0)}");

            // Auto show để test
            if (autoShowOnStart)
            {
                Debug.Log("[PanelPetHT] Auto-showing panel for testing...");
                Show();
            }
        }

        public void Show()
        {
            Debug.Log("[PanelPetHT] ===== SHOW() CALLED =====");

            if (panelObject == null)
            {
                Debug.LogError("[PanelPetHT] ❌ panelObject is NULL! Please assign it in Inspector");
                return;
            }

            panelObject.SetActive(true);
            Debug.Log("[PanelPetHT] Panel activated");

            LoadAllPets();
        }

        public void Hide()
        {
            panelObject.SetActive(false);
        }

        private long GetCurrentUserId()
        {
            if (PlayerPrefs.HasKey("UserId"))
            {
                return PlayerPrefs.GetInt("UserId");
            }
            return 1;
        }

        /// <summary>
        /// Bước 1: Load danh sách tất cả Pet
        /// </summary>
        private void LoadAllPets()
        {
            Debug.Log("[PanelPetHT] ===== START LOADING ALL PETS =====");
            ShowLoading(true);

            // Kiểm tra LegendPetAPIService có tồn tại không
            if (LegendPetAPIService.Instance == null)
            {
                Debug.LogError("[PanelPetHT] ❌ LegendPetAPIService.Instance is NULL!");
                ShowLoading(false);
                ShowErrorMessage("LegendPetAPIService không khởi tạo");
                return;
            }

            Debug.Log($"[PanelPetHT] UserID: {userId}");
            Debug.Log("[PanelPetHT] Calling GetAllLegendPets...");

            try
            {
                LegendPetAPIService.Instance.GetAllLegendPets(
                    OnLoadAllPetsSuccess,
                    OnLoadAllPetsError
                );
                Debug.Log("[PanelPetHT] GetAllLegendPets called successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PanelPetHT] ❌ Exception when calling GetAllLegendPets: {ex.Message}\n{ex.StackTrace}");
                ShowLoading(false);
                ShowErrorMessage($"Lỗi gọi API: {ex.Message}");
            }
        }

        private void OnLoadAllPetsSuccess(LegendPetListResponse response)
        {
            Debug.Log("[PanelPetHT] ✅ OnLoadAllPetsSuccess called!");
            ShowLoading(false);

            if (response == null)
            {
                Debug.LogError("[PanelPetHT] ❌ Response is NULL");
                ShowErrorMessage("API trả về null");
                return;
            }

            Debug.Log($"[PanelPetHT] Response received. Pets array is null: {response.pets == null}");

            if (response.pets == null || response.pets.Length == 0)
            {
                Debug.LogError("[PanelPetHT] ❌ No pets found in response");
                ShowErrorMessage("Không tìm thấy Pet nào");

                // Disable tất cả buttons nếu không có data
                for (int i = 0; i < btnHTs.Length; i++)
                {
                    if (btnHTs[i] != null)
                    {
                        btnHTs[i].gameObject.SetActive(false);
                        btnHTs[i].interactable = false;
                    }
                }
                return;
            }

            allPets = response.pets;
            Debug.Log($"[PanelPetHT] ===== ✅ Loaded {allPets.Length} pets =====");

            // Log từng pet
            for (int i = 0; i < allPets.Length; i++)
            {
                Debug.Log($"[PanelPetHT] Pet[{i}]: ID={allPets[i].id}, Name={allPets[i].name}, Unlocked={allPets[i].unlocked}, Progress={allPets[i].progress}%");
            }

            SetupPetButtons();

            // Auto select first pet nếu có
            if (allPets.Length > 0)
            {
                Debug.Log("[PanelPetHT] Auto-selecting first pet...");
                SelectPet(0);
            }
            else
            {
                Debug.LogWarning("[PanelPetHT] No pets to select");
            }
        }

        private void OnLoadAllPetsError(string error)
        {
            Debug.LogError($"[PanelPetHT] ❌ OnLoadAllPetsError called!");
            Debug.LogError($"[PanelPetHT] Error message: {error}");

            ShowLoading(false);
            ShowErrorMessage($"Không thể tải danh sách Pet: {error}");
        }

        /// <summary>
        /// Setup các nút btnHt với thông tin Pet
        /// </summary>
        private void SetupPetButtons()
        {
            for (int i = 0; i < btnHTs.Length; i++)
            {
                if (btnHTs[i] == null) continue;

                if (i < allPets.Length)
                {
                    // Có pet tương ứng
                    btnHTs[i].gameObject.SetActive(true);

                    LegendPetBasicInfo pet = allPets[i];

                    // Set tên pet
                    if (btnHtTexts != null && i < btnHtTexts.Length && btnHtTexts[i] != null)
                    {
                        btnHtTexts[i].text = pet.name;
                    }

                    // Set icon pet (nếu có)
                    if (btnHtIcons != null && i < btnHtIcons.Length && btnHtIcons[i] != null)
                    {
                        // TODO: Load sprite từ Resources hoặc API
                        // btnHtIcons[i].sprite = LoadPetIcon(pet.id);
                    }

                    // Set màu button dựa trên trạng thái
                    UpdatePetButtonVisual(i, pet);

                    // Đảm bảo button có thể click
                    btnHTs[i].interactable = true;

                    Debug.Log($"[PanelPetHT] Setup button {i}: {pet.name} (Unlocked: {pet.unlocked}, Progress: {pet.progress}%)");
                }
                else
                {
                    // Không có pet, ẩn button và disable click
                    btnHTs[i].gameObject.SetActive(false);
                    btnHTs[i].interactable = false;

                    Debug.Log($"[PanelPetHT] Button {i} disabled - no pet data");
                }
            }

            Debug.Log($"[PanelPetHT] Total active buttons: {Mathf.Min(btnHTs.Length, allPets.Length)}/{btnHTs.Length}");
        }

        /// <summary>
        /// Update visual của pet button dựa trên trạng thái
        /// </summary>
        private void UpdatePetButtonVisual(int index, LegendPetBasicInfo pet)
        {
            if (btnHTs[index] == null) return;

            Image buttonImage = btnHTs[index].GetComponent<Image>();
            if (buttonImage != null)
            {
                if (pet.unlocked)
                {
                    // Pet đã unlock - màu xanh lá
                    buttonImage.color = new Color(0.5f, 1f, 0.5f);
                }
                else if (pet.progress >= 100)
                {
                    // Hoàn thành nhưng chưa unlock - màu vàng
                    buttonImage.color = new Color(1f, 1f, 0.5f);
                }
                else
                {
                    // Đang tiến hành - màu trắng
                    buttonImage.color = Color.white;
                }
            }
        }

        /// <summary>
        /// Xử lý khi nhấn nút Pet
        /// </summary>
        private void OnPetButtonClick(int index)
        {
            // Kiểm tra trước khi select
            if (allPets == null || index < 0 || index >= allPets.Length)
            {
                Debug.LogWarning($"[PanelPetHT] Cannot select pet at index {index}. Total pets: {allPets?.Length ?? 0}");
                return;
            }

            SelectPet(index);
        }

        /// <summary>
        /// Chọn Pet và load chi tiết
        /// </summary>
        private void SelectPet(int index)
        {
            if (allPets == null || index < 0 || index >= allPets.Length)
            {
                Debug.LogError($"[PanelPetHT] Invalid pet index: {index}. Total pets: {allPets?.Length ?? 0}");
                return;
            }

            Debug.Log($"[PanelPetHT] ===== Selecting Pet {index}: {allPets[index].name} =====");

            currentPetIndex = index;
            UpdatePetButtonHighlight(index);

            // Load chi tiết Pet
            LoadPetDetail(allPets[index].id);
        }

        /// <summary>
        /// Highlight button đang được chọn
        /// </summary>
        private void UpdatePetButtonHighlight(int selectedIndex)
        {
            for (int i = 0; i < btnHTs.Length && i < allPets.Length; i++)
            {
                if (btnHTs[i] == null) continue;

                Image buttonImage = btnHTs[i].GetComponent<Image>();
                if (buttonImage != null)
                {
                    LegendPetBasicInfo pet = allPets[i];
                    Color baseColor = Color.white;

                    if (pet.unlocked)
                    {
                        baseColor = new Color(0.5f, 1f, 0.5f);
                    }
                    else if (pet.progress >= 100)
                    {
                        baseColor = new Color(1f, 1f, 0.5f);
                    }

                    // Highlight selected button
                    if (i == selectedIndex)
                    {
                        buttonImage.color = baseColor * 1.5f; // Sáng hơn

                        // Add border hoặc scale animation
                        btnHTs[i].transform.DOKill();
                        btnHTs[i].transform.DOScale(1.1f, 0.2f).SetEase(Ease.OutBack);
                    }
                    else
                    {
                        buttonImage.color = baseColor;
                        btnHTs[i].transform.DOKill();
                        btnHTs[i].transform.DOScale(1f, 0.2f);
                    }
                }
            }
        }

        /// <summary>
        /// Bước 2: Load chi tiết Pet
        /// </summary>
        private void LoadPetDetail(long petId)
        {
            Debug.Log($"[PanelPetHT] Loading detail for PetID: {petId}");
            ShowLoading(true);

            LegendPetAPIService.Instance.GetLegendPetInfo(
                userId,
                petId,
                OnLoadPetDetailSuccess,
                OnLoadPetDetailError
            );
        }

        private void OnLoadPetDetailSuccess(LegendPetData data)
{
    ShowLoading(false);

    if (data == null)
    {
        Debug.LogError("[PanelPetHT] Received null pet data");
        return;
    }

    currentPetData = data;
    Debug.Log($"[PanelPetHT] Loaded pet detail: {data.name}, Stars: {data.inlaidStars}/{data.totalStars}, Unlocked: {data.unlocked}");

    // ✨ DEBUG - CHỈ QUAN TÂM images[0]
    if (data.images == null || data.images.Count == 0)
    {
        Debug.LogWarning($"[PanelPetHT] Pet {data.name} has NO images data");
    }
    else
    {
        ImageHTData firstImage = data.images[0];
        int starCount = firstImage?.starSlots?.Count ?? 0;
        Debug.Log($"[PanelPetHT] Pet {data.name} has {starCount} stars in images[0]");
    }

    UpdatePetInfo();
    UpdateStarCount();

    // ✨ HIỂN THỊ ImageHT TƯƠNG ỨNG VỚI PET
    ShowImageHT(currentPetIndex);

    // ✨ KIỂM TRA TRẠNG THÁI PET
    if (data.unlocked)
    {
        Debug.Log($"[PanelPetHT] ✅ Pet {data.name} đã unlock - Hiển thị thông số");
        ClearAllStarButtons();
        HideStarsAndChangeImageColor();
        LoadAndShowPetStats(data.petId);
        
        // ✨ HIỂN THỊ PanelPetMain khi pet đã unlock
        if (PanelPetMain != null)
        {
            PanelPetMain.SetActive(true);
        }
    }
    else
    {
        Debug.Log($"[PanelPetHT] ⭐ Pet {data.name} chưa unlock - Hiển thị sao để khảm");

        // ✨ ẨN PanelPetMain khi pet chưa unlock
        if (PanelPetMain != null)
        {
            PanelPetMain.SetActive(false);
        }

        if (PanelCardPet != null)
        {
            PanelCardPet.SetActive(false);
        }

        HideAllPetStatsUI();
    }
}

        /// <summary>
        /// Ẩn tất cả UI hiển thị thuộc tính pet (khi pet chưa unlock)
        /// </summary>
        private void HideAllPetStatsUI()
        {
            Debug.Log("[PanelPetHT] Hiding all pet stats UI");

            // Ẩn panel card
            if (PanelCardPet != null)
            {
                PanelCardPet.SetActive(false);
            }

            // Clear các text (optional - nếu bạn muốn clear text)
            SetTextIfNotNull(namePet, "");
            SetTextIfNotNull(txtDame, "");
            SetTextIfNotNull(txtHp, "");
            SetTextIfNotNull(txtMana, "");
            SetTextIfNotNull(txtWee, "");
            SetTextIfNotNull(txtLv, "");
            SetTextIfNotNull(des, "");

            // Ẩn attribute images (optional)
            if (imgAtribute != null)
            {
                imgAtribute.enabled = false;
            }

            if (imgAtributeOther != null)
            {
                imgAtributeOther.enabled = false;
            }
        }

        /// <summary>
        /// Load thông số chi tiết của pet đã unlock
        /// </summary>
        private void LoadAndShowPetStats(long petId)
        {
            Debug.Log($"[PanelPetHT] Loading stats for pet ID: {petId}");

            if (isLoadingUserPet)
            {
                Debug.Log("[PanelPetHT] Already loading user pet, waiting...");
                return;
            }

            ShowLoading(true);
            isLoadingUserPet = true;

            LegendPetAPIService.Instance.GetUserPetInfo(
                (int)userId,        // Convert long to int
                (int)petId,         // Convert long to int
                (pet) =>
                {
                    ShowLoading(false);
                    isLoadingUserPet = false;

                    if (pet == null || pet.id == 0)
                    {
                        Debug.LogWarning("[PanelPetHT] User pet not found");
                        return;
                    }

                    Debug.Log($"[PanelPetHT] ✅ Received user pet: ID={pet.id}, PetID={pet.petId}, Name={pet.name}");

                    // Lưu data và hiển thị
                    currentUserPetData = pet;
                    DisplayPetStats(pet);
                },
                (error) =>
                {
                    ShowLoading(false);
                    isLoadingUserPet = false;
                    Debug.LogError($"[PanelPetHT] ❌ Failed to load user pet: {error}");
                    ShowErrorMessage($"Không thể tải thông số pet: {error}");
                }
            );
        }
        /// <summary>
        /// Hiển thị thông số pet
        /// </summary>
        private void DisplayPetStats(PetUserDTO pet)
        {
            Debug.Log($"[PanelPetHT] 📊 Displaying stats for: {pet.name}");
            OnPetsReceived(pet);
        }

        private void ClearAllStarButtons()
        {
            // Clear dictionaries
            starButtonDict.Clear();
            starImageDict.Clear();

            // Disable tất cả star buttons trong TẤT CẢ ImageHT panels
            if (imagePanels != null)
            {
                for (int i = 0; i < imagePanels.Length; i++)
                {
                    if (imagePanels[i] == null) continue;

                    // Tìm tất cả buttons trong panel này
                    Button[] buttons = imagePanels[i].GetComponentsInChildren<Button>(true);
                    foreach (Button btn in buttons)
                    {
                        // Chỉ xử lý star buttons
                        if (btn.transform.parent != null &&
                            (btn.transform.parent.name.ToLower().Contains("sao") ||
                             btn.name.ToLower().Contains("star")))
                        {
                            // Clear tất cả listeners
                            btn.onClick.RemoveAllListeners();

                            // Reset visual
                            btn.interactable = false;
                            btn.transform.DOKill();
                            btn.transform.localScale = Vector3.one;

                            // Reset color
                            ColorBlock cb = btn.colors;
                            cb.colorMultiplier = 1f;
                            btn.colors = cb;

                            Image img = btn.GetComponent<Image>();
                            if (img != null)
                            {
                                img.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                            }
                        }
                    }
                }
            }

            Debug.Log("[PanelPetHT] Cleared all star buttons in all panels");
        }

        private void OnLoadPetDetailError(string error)
        {
            ShowLoading(false);
            Debug.LogError($"[PanelPetHT] Load pet detail error: {error}");
            ShowErrorMessage($"Không thể tải chi tiết Pet: {error}");
        }

        private void UpdatePetInfo()
        {
            if (currentPetData == null) return;

            if (petNameText != null)
                petNameText.text = currentPetData.name;

            float progress = currentPetData.totalStars > 0
                ? (float)currentPetData.inlaidStars / currentPetData.totalStars
                : 0;

            if (progressBar != null)
                progressBar.fillAmount = progress;

            if (progressText != null)
                progressText.text = $"{currentPetData.inlaidStars}/{currentPetData.totalStars}";
        }

        private void UpdateStarCount()
        {
            // Đọc từ PlayerPrefs
            int starWhite = PlayerPrefs.GetInt("StarWhite", 0);
            int starBlue = PlayerPrefs.GetInt("StarBlue", 0);
            int starRed = PlayerPrefs.GetInt("StarRed", 0);

            // Cập nhật UI text
            if (starWhiteText != null)
            {
                starWhiteText.text = starWhite.ToString();
                Debug.Log($"[PanelPetHT] Updated StarWhite UI: {starWhite}");
            }

            if (starBlueText != null)
            {
                starBlueText.text = starBlue.ToString();
                Debug.Log($"[PanelPetHT] Updated StarBlue UI: {starBlue}");
            }

            if (starRedText != null)
            {
                starRedText.text = starRed.ToString();
                Debug.Log($"[PanelPetHT] Updated StarRed UI: {starRed}");
            }
        }

        /// <summary>
        /// Khởi tạo tất cả star buttons
        /// </summary>
        private void InitializeAllStarButtons()
        {
            starButtonDict.Clear();
            starImageDict.Clear();

            if (currentPetData?.images == null || imagePanels == null)
            {
                Debug.LogWarning("[PanelPetHT] Missing pet data or image panels");
                ClearAllStarButtons();
                return;
            }

            if (currentPetData.images.Count == 0)
            {
                Debug.LogWarning("[PanelPetHT] Pet has no images");
                ClearAllStarButtons();
                return;
            }

            int totalStarsInitialized = 0;

            // ✨ DUYỆT THEO ARRAY INDEX - BỎ QUA imageIndex FIELD
            for (int panelIndex = 0; panelIndex < imagePanels.Length && panelIndex < currentPetData.images.Count; panelIndex++)
            {
                GameObject imagePanel = imagePanels[panelIndex];
                ImageHTData imageData = currentPetData.images[panelIndex]; // ← Dùng array index trực tiếp

                if (imagePanel == null)
                {
                    Debug.LogWarning($"[PanelPetHT] ImagePanel at index {panelIndex} is null");
                    continue;
                }

                if (!imagePanel.activeSelf)
                {
                    Debug.Log($"[PanelPetHT] Skipping inactive ImagePanel {panelIndex}");
                    continue;
                }

                if (imageData == null)
                {
                    Debug.LogWarning($"[PanelPetHT] ImageData at index {panelIndex} is null");
                    continue;
                }

                if (imageData.starSlots == null || imageData.starSlots.Count == 0)
                {
                    Debug.LogWarning($"[PanelPetHT] ImageHT{panelIndex + 1} (array index {panelIndex}) has no star slots");
                    ClearStarsInPanel(imagePanel);
                    continue;
                }

                Debug.Log($"[PanelPetHT] Processing ImageHT{panelIndex + 1} with {imageData.starSlots.Count} star slots");

                Transform sao1Container = FindStarGroupContainer(imagePanel.transform, "sao1");
                Transform sao2Container = FindStarGroupContainer(imagePanel.transform, "sao2");
                Transform sao3Container = FindStarGroupContainer(imagePanel.transform, "sao3");

                List<StarSlotData> sao1Slots = new List<StarSlotData>();
                List<StarSlotData> sao2Slots = new List<StarSlotData>();
                List<StarSlotData> sao3Slots = new List<StarSlotData>();

                foreach (var slot in imageData.starSlots)
                {
                    switch (slot.starType)
                    {
                        case 1: sao1Slots.Add(slot); break;
                        case 2: sao2Slots.Add(slot); break;
                        case 3: sao3Slots.Add(slot); break;
                    }
                }

                sao1Slots.Sort((a, b) => a.slotPosition.CompareTo(b.slotPosition));
                sao2Slots.Sort((a, b) => a.slotPosition.CompareTo(b.slotPosition));
                sao3Slots.Sort((a, b) => a.slotPosition.CompareTo(b.slotPosition));

                int mapped = 0;
                mapped += MapStarGroup(sao1Container, sao1Slots, 1);
                mapped += MapStarGroup(sao2Container, sao2Slots, 2);
                mapped += MapStarGroup(sao3Container, sao3Slots, 3);

                totalStarsInitialized += mapped;
                Debug.Log($"[PanelPetHT] ImageHT{panelIndex + 1}: Mapped {mapped} stars");
            }

            Debug.Log($"[PanelPetHT] ===== Total stars initialized: {totalStarsInitialized} =====");
        }
        private void ClearStarsInPanel(GameObject panel)
        {
            if (panel == null) return;

            Button[] buttons = panel.GetComponentsInChildren<Button>(true);
            foreach (Button btn in buttons)
            {
                // Chỉ xử lý star buttons
                if (btn.transform.parent != null &&
                    (btn.transform.parent.name.ToLower().Contains("sao") ||
                     btn.name.ToLower().Contains("star")))
                {
                    btn.interactable = false;
                    btn.transform.DOKill();
                    btn.transform.localScale = Vector3.one;

                    Image img = btn.GetComponent<Image>();
                    if (img != null)
                    {
                        img.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                    }
                }
            }
        }

        private Transform FindStarGroupContainer(Transform parent, string groupName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name.ToLower().Contains(groupName.ToLower()))
                {
                    return child;
                }
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                Transform result = FindStarGroupContainer(child, groupName);
                if (result != null)
                    return result;
            }

            return null;
        }

        private int MapStarGroup(Transform container, List<StarSlotData> slotDataList, int starType)
        {
            if (container == null || slotDataList == null || slotDataList.Count == 0)
                return 0;

            List<Transform> starObjects = new List<Transform>();
            for (int i = 0; i < container.childCount; i++)
            {
                Transform child = container.GetChild(i);
                if (child.gameObject.activeSelf && (child.GetComponent<Button>() != null || child.GetComponent<Image>() != null))
                {
                    starObjects.Add(child);
                }
            }

            starObjects.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

            int mappedCount = 0;

            for (int i = 0; i < Mathf.Min(starObjects.Count, slotDataList.Count); i++)
            {
                Transform starObj = starObjects[i];
                StarSlotData slotData = slotDataList[i];

                Button starButton = starObj.GetComponent<Button>();
                Image starImage = starObj.GetComponent<Image>();

                if (starButton == null)
                    starButton = starObj.GetComponentInParent<Button>() ?? starObj.GetComponentInChildren<Button>();

                if (starImage == null)
                    starImage = starObj.GetComponentInChildren<Image>();

                if (starButton != null && starImage != null)
                {
                    starButtonDict[slotData.slotId] = starButton;
                    starImageDict[slotData.slotId] = starImage;

                    starButton.onClick.RemoveAllListeners();
                    starButton.onClick.AddListener(() => OnStarClicked(slotData));

                    UpdateStarVisual(slotData, starImage, starButton);

                    mappedCount++;
                }
            }

            return mappedCount;
        }

        private void UpdateStarVisual(StarSlotData slotData, Image starImage, Button starButton)
        {
            if (starImage == null || starButton == null) return;

            if (slotData.inlaid)
            {
                starImage.color = Color.white * 5f;
                starButton.interactable = false;
                starButton.transform.DOKill();
                ColorBlock cb = starButton.colors;  // lấy bản sao của ColorBlock
                cb.colorMultiplier = 5f;        // thay đổi giá trị mong muốn
                starButton.colors = cb;
            }
            else if (slotData.canInlay)
            {
                // starImage.color = Color.white;
                starButton.interactable = true;

                starButton.transform.DOKill();
                starButton.transform.localScale = Vector3.one;
                starButton.transform.DOScale(1.1f, 0.5f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
            else
            {
                starImage.color = Color.white;
                starButton.interactable = false;
                starButton.transform.DOKill();
                starButton.transform.localScale = Vector3.one;
            }
        }

        private void ShowImageHT(int petIndex)
        {
            // Validate index
            if (imagePanels == null || imagePanels.Length == 0)
            {
                Debug.LogError("[PanelPetHT] No image panels configured!");
                return;
            }

            // Clamp index trong range hợp lệ
            int validPetIndex = Mathf.Clamp(petIndex, 0, imagePanels.Length - 1);

            if (validPetIndex != petIndex)
            {
                Debug.LogWarning($"[PanelPetHT] Index {petIndex} out of range, using {validPetIndex} instead");
            }

            currentImageIndex = validPetIndex;

            Debug.Log($"[PanelPetHT] ===== ShowImageHT for Pet {validPetIndex} → Display ImageHT{validPetIndex + 1} =====");

            // ✨ BƯỚC 1: Clear tất cả star buttons
            ClearAllStarButtons();

            // ✨ BƯỚC 2: Hiển thị ImageHT tương ứng với Pet
            for (int i = 0; i < imagePanels.Length; i++)
            {
                if (imagePanels[i] != null)
                {
                    bool shouldShow = (i == validPetIndex);
                    imagePanels[i].SetActive(shouldShow);

                    if (shouldShow)
                    {
                        Debug.Log($"[PanelPetHT] ✅ Showing ImageHT{i + 1} for Pet {validPetIndex}");
                    }
                }
            }

            // ✨ BƯỚC 3: Init stars - LUÔN LẤY DATA TỪ images[0]
            if (currentPetData != null && currentPetData.images != null && currentPetData.images.Count > 0)
            {
                ImageHTData firstImageData = currentPetData.images[0]; // ← LUÔN LẤY images[0]

                if (firstImageData != null && firstImageData.starSlots != null && firstImageData.starSlots.Count > 0)
                {
                    Debug.Log($"[PanelPetHT] Found {firstImageData.starSlots.Count} stars in images[0], mapping to ImageHT{validPetIndex + 1}");

                    // Map stars vào panel đang hiển thị (validPetIndex)
                    InitializeStarButtonsForImage(validPetIndex, firstImageData);
                }
                else
                {
                    Debug.LogWarning($"[PanelPetHT] No star data in images[0] for this pet");
                }
            }
            else
            {
                Debug.LogWarning($"[PanelPetHT] No images data for this pet");
            }
        }

        /// <summary>
        /// Khởi tạo star buttons cho ImageHT cụ thể với data cụ thể
        /// </summary>
        private void InitializeStarButtonsForImage(int panelIndex, ImageHTData imageData)
        {
            Debug.Log($"[PanelPetHT] ===== InitializeStarButtonsForImage - Panel:{panelIndex}, Stars:{imageData?.starSlots?.Count ?? 0} =====");

            starButtonDict.Clear();
            starImageDict.Clear();

            if (imagePanels == null || panelIndex < 0 || panelIndex >= imagePanels.Length)
            {
                Debug.LogWarning($"[PanelPetHT] Invalid panel index: {panelIndex}");
                return;
            }

            GameObject imagePanel = imagePanels[panelIndex];

            if (imagePanel == null)
            {
                Debug.LogWarning($"[PanelPetHT] ImagePanel {panelIndex} is null");
                return;
            }

            if (!imagePanel.activeSelf)
            {
                Debug.LogWarning($"[PanelPetHT] ImagePanel {panelIndex} is not active!");
                return;
            }

            if (imageData == null || imageData.starSlots == null || imageData.starSlots.Count == 0)
            {
                Debug.LogWarning($"[PanelPetHT] No star slots data to map");
                return;
            }

            Debug.Log($"[PanelPetHT] ✅ Mapping {imageData.starSlots.Count} stars to ImageHT{panelIndex + 1}");

            // Tìm các container
            Transform sao1Container = FindStarGroupContainer(imagePanel.transform, "sao1");
            Transform sao2Container = FindStarGroupContainer(imagePanel.transform, "sao2");
            Transform sao3Container = FindStarGroupContainer(imagePanel.transform, "sao3");

            Debug.Log($"[PanelPetHT] Containers - sao1:{sao1Container != null}, sao2:{sao2Container != null}, sao3:{sao3Container != null}");

            // Phân loại slots
            List<StarSlotData> sao1Slots = new List<StarSlotData>();
            List<StarSlotData> sao2Slots = new List<StarSlotData>();
            List<StarSlotData> sao3Slots = new List<StarSlotData>();

            foreach (var slot in imageData.starSlots)
            {
                switch (slot.starType)
                {
                    case 1: sao1Slots.Add(slot); break;
                    case 2: sao2Slots.Add(slot); break;
                    case 3: sao3Slots.Add(slot); break;
                }
            }

            Debug.Log($"[PanelPetHT] Classified - Type1:{sao1Slots.Count}, Type2:{sao2Slots.Count}, Type3:{sao3Slots.Count}");

            // Sắp xếp theo position
            sao1Slots.Sort((a, b) => a.slotPosition.CompareTo(b.slotPosition));
            sao2Slots.Sort((a, b) => a.slotPosition.CompareTo(b.slotPosition));
            sao3Slots.Sort((a, b) => a.slotPosition.CompareTo(b.slotPosition));

            // Map stars
            int mapped1 = MapStarGroup(sao1Container, sao1Slots, 1);
            int mapped2 = MapStarGroup(sao2Container, sao2Slots, 2);
            int mapped3 = MapStarGroup(sao3Container, sao3Slots, 3);

            int totalMapped = mapped1 + mapped2 + mapped3;

            Debug.Log($"[PanelPetHT] ✅ ImageHT{panelIndex + 1}: Mapped {totalMapped}/{imageData.starSlots.Count} stars (Type1:{mapped1}, Type2:{mapped2}, Type3:{mapped3})");
        }

        private void OnStarClicked(StarSlotData slotData)
        {
            Debug.Log($"[PanelPetHT] ⭐ Star clicked - SlotID: {slotData.slotId}, Type: {slotData.starType}");

            // Thêm debug để kiểm tra
            Debug.Log($"[PanelPetHT] Current stars in PlayerPrefs:");
            Debug.Log($"  - StarWhite: {PlayerPrefs.GetInt("StarWhite", 0)}");
            Debug.Log($"  - StarBlue: {PlayerPrefs.GetInt("StarBlue", 0)}");
            Debug.Log($"  - StarRed: {PlayerPrefs.GetInt("StarRed", 0)}");
            Debug.Log($"[PanelPetHT] Required stars for this slot: {slotData.requiredStarCount}");

            int currentStarCount = slotData.starType switch
            {
                1 => PlayerPrefs.GetInt("StarWhite", 0),
                2 => PlayerPrefs.GetInt("StarBlue", 0),
                3 => PlayerPrefs.GetInt("StarRed", 0),
                _ => 0
            };

            ShowConfirmPanel(slotData, currentStarCount);
        }

        private void ShowConfirmPanel(StarSlotData slotData, int currentStarCount)
        {
            currentConfirmSlotData = slotData;

            string starTypeName = slotData.starType switch
            {
                1 => "Sao Trắng",
                2 => "Sao Xanh",
                3 => "Sao Đỏ",
                _ => "Sao"
            };

            bool canInlay = currentStarCount >= slotData.requiredStarCount;

            if (confirmMessageTxt != null)
            {
                if (canInlay)
                {
                    confirmMessageTxt.text = $"Bạn có muốn khảm {starTypeName} không?\n" +
                                             $"Cần: {slotData.requiredStarCount} | Hiện có: {currentStarCount}";
                    confirmMessageTxt.color = Color.white;
                }
                else
                {
                    confirmMessageTxt.text = $"Không đủ {starTypeName}!\n" +
                                             $"Cần: {slotData.requiredStarCount} | Hiện có: {currentStarCount}";
                    confirmMessageTxt.color = Color.red;
                }
            }

            if (confirmIcon != null)
            {
                confirmIcon.sprite = slotData.starType switch
                {
                    1 => starWhiteIcon,
                    2 => starBlueIcon,
                    3 => starRedIcon,
                    _ => starWhiteIcon
                };
            }

            if (confirmBtnOK != null)
                confirmBtnOK.interactable = canInlay;

            confirmPanelObject.SetActive(true);

            if (confirmCanvasGroup != null)
            {
                confirmCanvasGroup.alpha = 0;
                confirmCanvasGroup.DOFade(1, 0.3f).SetEase(Ease.OutQuad);
            }

            confirmPanelObject.transform.localScale = Vector3.zero;
            confirmPanelObject.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        }

        private void HideConfirmPanel()
        {
            if (confirmCanvasGroup != null)
            {
                confirmCanvasGroup.DOFade(0, 0.2f).OnComplete(() =>
                {
                    confirmPanelObject.SetActive(false);
                });
            }
            else
            {
                confirmPanelObject.SetActive(false);
            }
        }

        private void OnConfirm(bool confirmed)
        {
            HideConfirmPanel();

            if (confirmed && currentConfirmSlotData != null)
            {
                InlayStar(currentConfirmSlotData);
            }
        }

        private void InlayStar(StarSlotData slotData)
        {
            ShowLoading(true);

            lastInlayRequest = new InlayStarRequest
            {
                userId = userId,
                petId = currentPetData.petId,
                slotId = slotData.slotId
            };

            Debug.Log($"[PanelPetHT] 🔷 Sending inlay request - SlotID: {slotData.slotId}");

            LegendPetAPIService.Instance.InlayStar(
                lastInlayRequest,
                OnInlaySuccess,
                OnInlayError
            );
        }

        private void OnInlaySuccess(InlayStarResponse response)
        {
            ShowLoading(false);
            Debug.Log($"[PanelPetHT] ✅ Inlay success: {response.message}");

            if (response.success)
            {
                // Cập nhật số sao
                PlayerPrefs.SetInt("StarWhite", response.remainingWhiteStars);
                PlayerPrefs.SetInt("StarBlue", response.remainingBlueStars);
                PlayerPrefs.SetInt("StarRed", response.remainingRedStars);
                PlayerPrefs.Save();

                UpdateStarCount();

                if (ManagerQuangTruong.Instance != null)
                {
                    ManagerQuangTruong.Instance.UpdateStarUI(
                        response.remainingWhiteStars,
                        response.remainingBlueStars,
                        response.remainingRedStars
                    );
                }

                if (StarEventManager.Instance != null)
                {
                    StarEventManager.Instance.UpdateStarCount(
                        response.remainingWhiteStars,
                        response.remainingBlueStars,
                        response.remainingRedStars
                    );
                }

                // Xử lý cập nhật slot đã khảm
                bool updated = false;
                if (currentPetData?.images != null)
                {
                    foreach (var imageData in currentPetData.images)
                    {
                        foreach (var star in imageData.starSlots)
                        {
                            if (star.slotId == lastInlayRequest.slotId)
                            {
                                star.inlaid = true;
                                updated = true;

                                UpdateStarSlotVisual(star.slotId, true);
                                PlayStarInlayAnimation(star.slotId, () =>
                                {
                                    currentPetData.inlaidStars++;
                                    UpdatePetInfo();
                                    UpdateAllStarButtonStates();

                                    if (allPets != null && currentPetIndex < allPets.Length)
                                    {
                                        allPets[currentPetIndex].progress =
                                            (int)((float)currentPetData.inlaidStars / currentPetData.totalStars * 100);
                                        UpdatePetButtonVisual(currentPetIndex, allPets[currentPetIndex]);
                                    }

                                    // ✨ KIỂM TRA UNLOCK
                                    if (response.petUnlocked)
                                    {
                                        Debug.Log($"[PanelPetHT] 🎉 Pet vừa được unlock!");

                                        // Cập nhật trạng thái
                                        currentPetData.unlocked = true;

                                        if (allPets != null && currentPetIndex < allPets.Length)
                                        {
                                            allPets[currentPetIndex].unlocked = true;
                                            allPets[currentPetIndex].progress = 100;
                                            UpdatePetButtonVisual(currentPetIndex, allPets[currentPetIndex]);
                                        }

                                        // ✨ ẨN TẤT CẢ SAO
                                        ClearAllStarButtons();
                                        HideStarsAndChangeImageColor();

                                        // ✨ LOAD VÀ HIỂN THỊ THÔNG SỐ PET
                                        LoadAndShowPetStats(currentPetData.petId);

                                        // Hiển thị panel unlock
                                        ShowPetUnlockPanel();
                                    }
                                });
                                break;
                            }
                        }
                        if (updated) break;
                    }
                }

                Debug.Log($"[PanelPetHT] Stars after inlay - White: {response.remainingWhiteStars}, Blue: {response.remainingBlueStars}, Red: {response.remainingRedStars}");
            }
            else
            {
                Debug.LogError($"[PanelPetHT] ❌ Inlay failed: {response.message}");
                ShowErrorMessage(response.message);
            }
        }

        void OnPetsReceived(PetUserDTO pet)
        {
            Debug.Log($"[PanelPetHT] OnPetsReceived called for pet ID: {pet.petId}");

            // Xử lý name - nếu null/empty thì load từ currentPetData
            string displayName = pet.name;
            if (string.IsNullOrEmpty(displayName) && currentPetData != null)
            {
                displayName = currentPetData.name;
            }
            SetTextIfNotNull(namePet, displayName);

            // Xử lý description - nếu null/empty thì load từ currentPetData
            string displayDes = pet.des;
            if (string.IsNullOrEmpty(displayDes) && currentPetData != null)
            {
                displayDes = currentPetData.description; // Nếu LegendPetData có field description
            }
            SetTextIfNotNull(des, displayDes);

            // Các stats
            SetTextIfNotNull(txtDame, pet.attack.ToString());
            SetTextIfNotNull(txtHp, pet.hp.ToString());
            SetTextIfNotNull(txtMana, pet.mana.ToString());
            SetTextIfNotNull(txtWee, $"+{pet.weaknessValue:F2}");
            SetTextIfNotNull(txtLv, $"Lv {pet.maxLevel}");

            // Load attribute images
            LoadAttributeImage(imgAtribute, pet.elementType);
            LoadAttributeImage(imgAtributeOther, pet.elementOther);

            // ✨ LOAD SKILL CARD - Chỉ show khi skillCardId > 0 (không null)
            if (pet.skillCardId > 0)
            {
                Debug.Log($"[PanelPetHT] Pet có skill card ID: {pet.skillCardId}");
                LoadPetSkillCard(pet.skillCardId, displayDes);
            }
            else
            {
                // Ẩn panel khi không có skill card
                if (PanelCardPet != null)
                {
                    PanelCardPet.SetActive(false);
                }
                Debug.Log($"[PanelPetHT] Pet {pet.petId} không có skill card (skillCardId = {pet.skillCardId})");
            }

            Debug.Log($"[PanelPetHT] ✅ Stats displayed successfully:");
            Debug.Log($"  - Name: {displayName}");
            Debug.Log($"  - Level: {pet.level}/{pet.maxLevel}");
            Debug.Log($"  - HP: {pet.hp}");
            Debug.Log($"  - Attack: {pet.attack}");
            Debug.Log($"  - Mana: {pet.mana}");
            Debug.Log($"  - Weakness: {pet.weaknessValue}");
            Debug.Log($"  - Element: {pet.elementType} / {pet.elementOther}");
            Debug.Log($"  - SkillCardId: {pet.skillCardId}");
        }

        void LoadPetSkillCard(int skillCardId, string description)
        {
            if (PanelCardPet == null)
            {
                Debug.LogWarning("PanelCardPet chưa được gán trong Inspector!");
                return;
            }

            string cardPath = $"Image/Card/HT{skillCardId}";
            Sprite cardSprite = Resources.Load<Sprite>(cardPath);

            if (cardSprite != null)
            {
                PanelCardPet.SetActive(true);

                if (imgCard != null)
                {
                    imgCard.sprite = cardSprite;
                    imgCard.enabled = true;
                }

                if (txtDescription != null)
                {
                    txtDescription.text = description;
                }

                Debug.Log($"✓ Đã load skill card cho pet {skillCardId}");
            }
            else
            {
                PanelCardPet.SetActive(false);
                Debug.Log($"Pet {skillCardId} không có skill card");
            }
        }

        void SetTextIfNotNull(Text textComponent, string value)
        {
            if (textComponent != null)
            {
                textComponent.text = value;
            }
        }

        void LoadAttributeImage(Image imageComponent, string attributeName)
        {
            if (imageComponent == null) return;

            Sprite attributeSprite = Resources.Load<Sprite>($"Image/Attribute/{attributeName}");
            Debug.Log($"[PanelPetHT] Loading attribute image: {attributeName}");
            if (attributeSprite != null)
            {
                imageComponent.sprite = attributeSprite;
            }
        }


        private void UpdateStarSlotVisual(long slotId, bool inlaid)
        {
            if (starButtonDict.TryGetValue(slotId, out Button button) &&
                starImageDict.TryGetValue(slotId, out Image image))
            {
                if (inlaid)
                {
                    image.color = Color.white * 5f;
                    button.interactable = false;
                    button.transform.DOKill();
                    ColorBlock cb = button.colors;  // lấy bản sao của ColorBlock
                    cb.colorMultiplier = 5f;        // thay đổi giá trị mong muốn
                    button.colors = cb;
                    button.transform.localScale = Vector3.one;
                }
            }
        }

        /// <summary>
        /// Ẩn tất cả sao và đổi màu ImageHT khi pet đã unlock
        /// </summary>
        private void HideStarsAndChangeImageColor()
        {
            Debug.Log("[PanelPetHT] Hiding all stars and changing image color to white");

            // Ẩn tất cả star buttons
            ClearAllStarButtons();

            // Đổi màu ImageHT hiện tại thành trắng
            if (imagePanels != null && currentImageIndex >= 0 && currentImageIndex < imagePanels.Length)
            {
                GameObject currentImagePanel = imagePanels[currentImageIndex];
                if (currentImagePanel != null)
                {
                    // Tìm Image component của ImageHT
                    Image imageHTComponent = currentImagePanel.GetComponent<Image>();
                    if (imageHTComponent != null)
                    {
                        imageHTComponent.color = Color.white;
                        Debug.Log($"[PanelPetHT] Changed ImageHT{currentImageIndex + 1} color to white");
                    }

                    // Hoặc nếu Image nằm ở child object
                    Image[] childImages = currentImagePanel.GetComponentsInChildren<Image>();
                    foreach (Image img in childImages)
                    {
                        // Chỉ đổi màu image chính, không phải star images
                        if (!img.transform.parent.name.ToLower().Contains("sao") &&
                            !img.name.ToLower().Contains("star"))
                        {
                            img.color = Color.white;
                        }
                    }
                }
            }
        }

        private void PlayStarInlayAnimation(long slotId, Action onComplete)
        {
            if (starImageDict.TryGetValue(slotId, out Image image))
            {
                Transform starTransform = image.transform;
                starTransform.DOKill();

                Sequence seq = DOTween.Sequence();
                seq.Append(starTransform.DOScale(1.5f, 0.3f).SetEase(Ease.OutQuad));
                seq.Append(starTransform.DOScale(1f, 0.3f).SetEase(Ease.InOutQuad));
                seq.OnComplete(() => onComplete?.Invoke());
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        private void OnInlayError(string error)
        {
            ShowLoading(false);
            Debug.LogError($"[PanelPetHT] ❌ Inlay error: {error}");
            ShowErrorMessage($"Lỗi khảm sao: {error}");
        }

        private void ShowPetUnlockPanel()
        {
            Debug.Log("[PanelPetHT] 🔍 ShowPetUnlockPanel called");

            if (petUnlockPanel == null)
            {
                Debug.LogError("[PanelPetHT] ❌ petUnlockPanel is NULL! Please assign in Inspector");
                return;
            }

            Debug.Log($"[PanelPetHT] petUnlockPanel found: {petUnlockPanel.name}");
            Debug.Log($"[PanelPetHT] petUnlockPanel current active state: {petUnlockPanel.activeSelf}");

            petUnlockPanel.SetActive(true);
            Debug.Log("[PanelPetHT] 🎉 Pet Unlocked! Panel activated");

            // Reset scale trước khi animate
            petUnlockPanel.transform.localScale = Vector3.zero;

            // Scale in animation
            petUnlockPanel.transform.DOScale(1f, 0.5f)
                .SetEase(Ease.OutBack)
                .OnStart(() => Debug.Log("[PanelPetHT] Scale in animation started"))
                .OnComplete(() => Debug.Log("[PanelPetHT] Scale in animation completed"));

            // Auto hide sau 3 giây
            DOVirtual.DelayedCall(3f, () =>
            {
                Debug.Log("[PanelPetHT] Starting hide animation");

                petUnlockPanel.transform.DOScale(0f, 0.3f)
                    .OnComplete(() =>
                    {
                        petUnlockPanel.SetActive(false);
                        Debug.Log("[PanelPetHT] Panel hidden");
                    });
            });
        }

        private void ShowLoading(bool show)
        {
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(show);
            }
        }

        private void ShowErrorMessage(string message)
        {
            Debug.LogError($"[PanelPetHT] Error: {message}");
            // TODO: Show error popup to user
        }

        private void OnDestroy()
        {
            if (btnHTs != null)
            {
                foreach (var btn in btnHTs)
                {
                    if (btn != null)
                        btn.onClick.RemoveAllListeners();
                }
            }

            if (confirmBtnOK != null)
                confirmBtnOK.onClick.RemoveAllListeners();

            if (confirmBtnCancel != null)
                confirmBtnCancel.onClick.RemoveAllListeners();

            if (closeButton != null)
                closeButton.onClick.RemoveAllListeners();

            foreach (var button in starButtonDict.Values)
            {
                if (button != null)
                    button.transform.DOKill();
            }

            if (StarEventManager.Instance != null)
            {
                StarEventManager.Instance.OnStarCountChanged -= OnStarCountChangedHandler;
            }

            // ✨ THÊM 2 DÒNG NÀY
            currentUserPetData = null;
            isLoadingUserPet = false;
        }

        private void OnStarCountChangedHandler(int white, int blue, int red)
        {
            Debug.Log($"[PanelPetHT] Star count updated from PointerSpin - White: {white}, Blue: {blue}, Red: {red}");

            // Cập nhật UI hiển thị số sao
            if (starWhiteText != null) starWhiteText.text = white.ToString();
            if (starBlueText != null) starBlueText.text = blue.ToString();
            if (starRedText != null) starRedText.text = red.ToString();

            // Kiểm tra và cập nhật trạng thái các star buttons nếu panel đang mở
            if (panelObject != null && panelObject.activeSelf && currentPetData != null)
            {
                UpdateAllStarButtonStates();
            }
        }

        private void UpdateAllStarButtonStates()
        {
            if (currentPetData?.images == null) return;

            int starWhite = PlayerPrefs.GetInt("StarWhite", 0);
            int starBlue = PlayerPrefs.GetInt("StarBlue", 0);
            int starRed = PlayerPrefs.GetInt("StarRed", 0);

            foreach (var imageData in currentPetData.images)
            {
                foreach (var slot in imageData.starSlots)
                {
                    if (!slot.inlaid) // Chỉ cập nhật các slot chưa khảm
                    {
                        // Kiểm tra xem có đủ sao để khảm không
                        bool canInlayNow = false;
                        switch (slot.starType)
                        {
                            case 1: canInlayNow = starWhite >= slot.requiredStarCount; break;
                            case 2: canInlayNow = starBlue >= slot.requiredStarCount; break;
                            case 3: canInlayNow = starRed >= slot.requiredStarCount; break;
                        }

                        // Chỉ cập nhật nếu trạng thái thay đổi
                        if (canInlayNow != slot.canInlay)
                        {
                            slot.canInlay = canInlayNow;

                            // Cập nhật visual của button
                            if (starButtonDict.TryGetValue(slot.slotId, out Button button) &&
                                starImageDict.TryGetValue(slot.slotId, out Image image))
                            {
                                UpdateStarVisual(slot, image, button);
                            }
                        }
                    }
                }
            }
        }

        // ===== PUBLIC METHODS =====

        public void RefreshData()
        {
            currentUserPetData = null;  // ✨ Reset user pet data
            isLoadingUserPet = false;   // ✨ Reset loading flag
            LoadAllPets();
        }

        public void SelectPetById(long petId)
        {
            if (allPets == null) return;

            for (int i = 0; i < allPets.Length; i++)
            {
                if (allPets[i].id == petId)
                {
                    SelectPet(i);
                    return;
                }
            }
        }

        // ===== DEBUG/TEST METHODS =====

        /// <summary>
        /// Right-click component trong Inspector → Test Show Panel
        /// </summary>
        [ContextMenu("Test - Show Panel")]
        private void ContextMenu_ShowPanel()
        {
            Show();
        }

        /// <summary>
        /// Right-click component trong Inspector → Test API
        /// </summary>
        [ContextMenu("Test - Call API")]
        private void ContextMenu_TestAPI()
        {
            TestAPI();
        }

        /// <summary>
        /// Right-click component trong Inspector → Test Fake Data
        /// </summary>
        [ContextMenu("Test - Load Fake Data")]
        private void ContextMenu_TestFakeData()
        {
            TestWithFakeData();
        }

        /// <summary>
        /// Test API call - gắn vào button để test thủ công
        /// </summary>
        public void TestAPI()
        {
            Debug.Log("===== MANUAL API TEST START =====");

            // Test 1: Check APIManager
            if (APIManager.Instance == null)
            {
                Debug.LogError("❌ APIManager.Instance is NULL!");
                return;
            }
            Debug.Log("✅ APIManager.Instance exists");

            // Test 2: Check LegendPetAPIService
            if (LegendPetAPIService.Instance == null)
            {
                Debug.LogError("❌ LegendPetAPIService.Instance is NULL!");
                return;
            }
            Debug.Log("✅ LegendPetAPIService.Instance exists");

            // Test 3: Check URL
            string url = APIConfig.GET_ALL_LEGEND_PETS;
            Debug.Log($"📍 API URL: {url}");

            if (string.IsNullOrEmpty(url) || url.Contains("your-api-domain"))
            {
                Debug.LogError("❌ API URL not configured! Please update APIConfig.cs");
                return;
            }

            // Test 4: Call API
            Debug.Log("🚀 Calling GetAllLegendPets...");
            LegendPetAPIService.Instance.GetAllLegendPets(
                (response) =>
                {
                    Debug.Log("===== ✅ API TEST SUCCESS =====");
                    if (response == null)
                    {
                        Debug.LogWarning("Response is null");
                    }
                    else if (response.pets == null)
                    {
                        Debug.LogWarning("Response.pets is null");
                    }
                    else
                    {
                        Debug.Log($"✅ Received {response.pets.Length} pets:");
                        for (int i = 0; i < response.pets.Length; i++)
                        {
                            var pet = response.pets[i];
                            Debug.Log($"  [{i}] ID={pet.id}, Name={pet.name}, Unlocked={pet.unlocked}");
                        }
                    }
                },
                (error) =>
                {
                    Debug.LogError("===== ❌ API TEST FAILED =====");
                    Debug.LogError($"Error: {error}");
                }
            );
        }

        /// <summary>
        /// Test với fake data
        /// </summary>
        public void TestWithFakeData()
        {
            Debug.Log("[PanelPetHT] Testing with FAKE DATA");

            // Tạo fake data
            LegendPetListResponse fakeResponse = new LegendPetListResponse
            {
                pets = new LegendPetBasicInfo[]
                {
                    new LegendPetBasicInfo { id = 1, name = "Rồng Lửa", unlocked = false, progress = 30 },
                    new LegendPetBasicInfo { id = 2, name = "Phượng Hoàng", unlocked = false, progress = 50 },
                    new LegendPetBasicInfo { id = 3, name = "Kỳ Lân", unlocked = true, progress = 100 }
                }
            };

            OnLoadAllPetsSuccess(fakeResponse);
        }
    }

}
