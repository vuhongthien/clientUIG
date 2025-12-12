using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon.StructWrapping;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ManagerRoom : MonoBehaviour
{
    public GameObject roomPanel; // Main room panel
    public GameObject loading;
    public GameObject panelPet;
    public GameObject panelInvite;
    public GameObject panelCard;
    public GameObject btnClosePet;
    public GameObject btnCloseCard;
    public Button btnBackToChinhPhuc; // Button để quay lại Chinh Phục

    private bool isRotatingPet = false;
    private bool isRotatingCard = false;
    public float rotationSpeed = 200f;
    public Animator animator;
    public Animator enemyPet;
    public Image imgPet;
    public Image imgEnemyPet;
    public Image imgUser;
    public GameObject petUIPrefab;
    public Transform petListContainer;
    public Text txtVang;
    public Text txtCt;
    public Text txtNl;
    public Text txtLvRoom;
    public Text txtManaRoom;
    public Text txtUsername;
    public Text txtCount;
    public Text txtNamePetEnemy;
    [Header("Card Selection")]
    public GameObject panelSelectCards;    // Panel chọn thẻ
    public Button btnStartBattle;          // Nút bắt đầu chiến đấu
    public ToggleManager toggleManager;    // Manager chọn thẻ
    [Header("Energy Warning")]
    [Tooltip("Panel thông báo hết năng lượng")]
    public GameObject energyWarningPanel;

    [Tooltip("Text hiển thị thông báo")]
    public Text energyWarningText;

    [Tooltip("Button OK để đóng thông báo")]
    public Button energyWarningOkButton;

    private int currentUserEnergy = 0;

    private RoomDTO roomData;
    public List<CardData> selectedCards = new List<CardData>();

    private void Start()
    {
        Debug.Log("[ManagerRoom] Start - Room panel initialized");

        // Ẩn room panel ban đầu
        if (roomPanel != null)
        {
            roomPanel.SetActive(false);
        }

        // Ẩn loading ban đầu
        if (loading != null)
        {
            loading.SetActive(false);
        }
        SetupCardSelection();
        // Setup back button
        if (btnBackToChinhPhuc != null)
        {
            btnBackToChinhPhuc.onClick.AddListener(CloseRoomPanel);
        }
        PlayerPrefs.DeleteKey("SelectedCards");
        PlayerPrefs.Save();
    }

    /// <summary>
    /// ✅ NEW: Mở Room panel - KHÔNG animation, CHỈ loading
    /// </summary>
    public void OpenRoomPanel()
    {
        Debug.Log("[ManagerRoom] Opening Room panel with loading...");

        if (roomPanel == null)
        {
            Debug.LogError("[ManagerRoom] roomPanel is not assigned!");
            return;
        }

        if (loading == null)
        {
            Debug.LogError("[ManagerRoom] loading panel is not assigned!");
            return;
        }

        // ✅ BƯỚC 1: ẨN ROOM PANEL (nếu đang hiện)
        roomPanel.SetActive(false);

        // ✅ BƯỚC 2: SHOW LOADING NGAY LẬP TỨC
        ShowLoadingInstant();

        // ✅ BƯỚC 3: LOAD DATA
        StartCoroutine(LoadRoomDataWithLoading());
    }

    /// <summary>
    /// ✅ Show loading NGAY (không animation)
    /// </summary>
    private void ShowLoadingInstant()
    {
        if (loading == null) return;

        Debug.Log("[ManagerRoom] → Showing loading");

        loading.SetActive(true);
        loading.transform.localScale = Vector3.one; // Hiện ngay, không scale animation
    }

    /// <summary>
    /// ✅ Hide loading NGAY (không animation)
    /// </summary>
    private void HideLoadingInstant()
    {
        if (loading == null) return;

        Debug.Log("[ManagerRoom] → Hiding loading");

        loading.SetActive(false);
    }

    /// <summary>
    /// ✅ Load data với loading - sau đó hiện Room panel
    /// </summary>
    public IEnumerator LoadRoomDataWithLoading()
    {
        Debug.Log("[ManagerRoom] → Loading room data...");
        int userId = PlayerPrefs.GetInt("userId", 1);
        int selectedPetId = PlayerPrefs.GetInt("SelectedPetId", 1);

        Debug.Log($"[ManagerRoom] userId: {userId}, selectedPetId: {selectedPetId}");

        bool allRequestsCompleted = false;
        int completedRequests = 0;
        int totalRequests = 3;

        // ✅ LOAD USER (1/3)
        yield return APIManager.Instance.GetRequest<UserDTO>(
            APIConfig.GET_USER(userId),
            (user) =>
            {
                OnUserReceived(user);
                completedRequests++;
                Debug.Log($"[ManagerRoom] ✓ User data loaded ({completedRequests}/{totalRequests})");
            },
            OnError
        );

        // ✅ LOAD PETS (2/3)
        yield return APIManager.Instance.GetRequest<List<PetUserDTO>>(
            APIConfig.GET_ALL_PET_USERS(userId),
            (pets) =>
            {
                OnPetsReceived(pets);
                completedRequests++;
                Debug.Log($"[ManagerRoom] ✓ Pets data loaded ({completedRequests}/{totalRequests})");
            },
            OnError
        );

        // ✅ LOAD ROOM (3/3)
        yield return APIManager.Instance.GetRequest<RoomDTO>(
            APIConfig.GET_ROOM_USERS(userId, selectedPetId),
            (room) =>
            {
                OnRoomReceived(room);
                completedRequests++;
                Debug.Log($"[ManagerRoom] ✓ Room data loaded ({completedRequests}/{totalRequests})");
                allRequestsCompleted = true;
            },
            OnError
        );

        // ✅ ĐỢI TẤT CẢ REQUESTS HOÀN THÀNH
        while (!allRequestsCompleted)
        {
            yield return null;
        }

        Debug.Log("[ManagerRoom] ✓✓ All data loaded successfully");

        // ✅ BƯỚC 4: HIDE LOADING
        HideLoadingInstant();

        // ✅ BƯỚC 5: HIỆN ROOM PANEL (không animation)
        ShowRoomPanelInstant();
    }

    /// <summary>
    /// ✅ Hiện Room panel NGAY (không animation)
    /// </summary>
    private void ShowRoomPanelInstant()
    {
        if (roomPanel == null) return;

        Debug.Log("[ManagerRoom] → Showing room panel");

        roomPanel.SetActive(true);
        roomPanel.transform.localScale = Vector3.one;

        // Set alpha = 1
        CanvasGroup cg = roomPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = roomPanel.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
    }

    /// <summary>
    /// ✅ Load room data KHÔNG show loading (dùng khi restore state)
    /// </summary>
    public IEnumerator LoadRoomDataWithoutLoading()
    {
        Debug.Log("[ManagerRoom] Loading room data (no loading panel)...");

        int userId = PlayerPrefs.GetInt("userId", 1);
        int selectedPetId = PlayerPrefs.GetInt("SelectedPetId", 1);

        Debug.Log($"[ManagerRoom] userId: {userId}, selectedPetId: {selectedPetId}");

        // Load user info
        yield return APIManager.Instance.GetRequest<UserDTO>(
            APIConfig.GET_USER(userId),
            OnUserReceived,
            OnError
        );

        yield return new WaitForSeconds(0.1f);

        // Load pets data
        yield return APIManager.Instance.GetRequest<List<PetUserDTO>>(
            APIConfig.GET_ALL_PET_USERS(userId),
            OnPetsReceived,
            OnError
        );

        yield return new WaitForSeconds(0.1f);

        // Load room data
        yield return APIManager.Instance.GetRequest<RoomDTO>(
            APIConfig.GET_ROOM_USERS(userId, selectedPetId),
            OnRoomReceived,
            OnError
        );

        yield return new WaitForSeconds(0.2f);

        Debug.Log("[ManagerRoom] ✓✓ Data loaded and UI rendered successfully!");
    }

    /// <summary>
    /// Đóng Room panel và quay lại Chinh Phục
    /// </summary>
    [Header("Transition")]
    public GameObject fadeOverlay; // Gán 1 Image đen fullscreen

    public void CloseRoomPanel()
    {
        Debug.Log("[ManagerRoom] Closing Room panel...");

        if (roomPanel == null) return;

        StartCoroutine(FadeTransition());
    }

    private IEnumerator FadeTransition()
    {
        // ✅ Setup fade overlay
        if (fadeOverlay != null)
        {
            fadeOverlay.SetActive(true);
            CanvasGroup overlayCanvas = fadeOverlay.GetComponent<CanvasGroup>();
            if (overlayCanvas == null)
            {
                overlayCanvas = fadeOverlay.AddComponent<CanvasGroup>();
            }
            overlayCanvas.alpha = 0f;

            // Fade to black
            LeanTween.alphaCanvas(overlayCanvas, 1f, 0.3f)
                .setEase(LeanTweenType.easeInQuad);

            yield return new WaitForSeconds(0.3f);
        }

        // ✅ Đóng Room panel
        if (roomPanel != null)
        {
            roomPanel.SetActive(false);
        }

        // ✅ Mở Chinh Phục
        ManagerChinhPhuc chinhPhucManager = FindObjectOfType<ManagerChinhPhuc>();
        if (chinhPhucManager != null)
        {
            chinhPhucManager.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(0.1f);

        // ✅ Fade from black
        if (fadeOverlay != null)
        {
            CanvasGroup overlayCanvas = fadeOverlay.GetComponent<CanvasGroup>();

            LeanTween.alphaCanvas(overlayCanvas, 0f, 0.3f)
                .setEase(LeanTweenType.easeOutQuad)
                .setOnComplete(() =>
                {
                    fadeOverlay.SetActive(false);
                });
        }
    }

    public void ShowPetPanel()
    {
        if (panelPet == null) return;

        panelPet.SetActive(true);
        isRotatingPet = true;

        // Animate panel entry
        panelPet.transform.localScale = Vector3.zero;
        LeanTween.scale(panelPet, Vector3.one, 0.4f)
            .setEaseOutBack();
    }

    public void HidePetPanel()
    {
        if (panelPet == null) return;

        isRotatingPet = false;

        LeanTween.scale(panelPet, Vector3.zero, 0.25f)
            .setEaseInBack()
            .setOnComplete(() => panelPet.SetActive(false));
    }

    public void ShowInvitePanel()
    {
        if (panelInvite == null) return;

        panelInvite.SetActive(true);
        // Animate panel entry
        panelInvite.transform.localScale = Vector3.zero;
        LeanTween.scale(panelInvite, Vector3.one, 0.4f)
            .setEaseOutBack();
    }

    public void HideInvitePanel()
    {
        if (panelInvite == null) return;
        LeanTween.scale(panelInvite, Vector3.zero, 0.25f)
            .setEaseInBack()
            .setOnComplete(() => panelInvite.SetActive(false));
    }

    public void ShowCardPanel()
    {
        if (panelCard == null) return;

        panelCard.SetActive(true);
        isRotatingCard = true;

        // Animate panel entry
        panelCard.transform.localScale = Vector3.zero;
        LeanTween.scale(panelCard, Vector3.one, 0.4f)
            .setEaseOutBack();
    }

    public void HideCardPanel()
    {
        if (panelCard == null) return;

        isRotatingCard = false;

        LeanTween.scale(panelCard, Vector3.zero, 0.25f)
            .setEaseInBack()
            .setOnComplete(() => panelCard.SetActive(false));
    }

    private void Update()
    {
        if (isRotatingPet && btnClosePet != null)
        {
            btnClosePet.transform.Rotate(rotationSpeed * Time.deltaTime, 0f, 0f);
        }

        if (isRotatingCard && btnCloseCard != null)
        {
            btnCloseCard.transform.Rotate(rotationSpeed * Time.deltaTime, 0f, 0f);
        }
    }

    void OnPetsReceived(List<PetUserDTO> pets)
    {
        Debug.Log($"[ManagerRoom] Received {pets.Count} pets");

        // Clear existing pets
        foreach (Transform child in petListContainer)
        {
            Destroy(child.gameObject);
        }

        int selectedPetId = PlayerPrefs.GetInt("SelectedPetId", 1);

        for (int i = 0; i < pets.Count; i++)
        {
            var pet = pets[i];
            GameObject petUIObject = Instantiate(petUIPrefab, petListContainer);

            Image petIcon = petUIObject.transform.Find("imgtPet")?.GetComponent<Image>();
            Image imgHe = petUIObject.transform.Find("imgHe")?.GetComponent<Image>();
            Text txtLv = petUIObject.transform.Find("txtLv")?.GetComponent<Text>();

            string petID = pet.petId.ToString();
            Sprite petSprite = Resources.Load<Sprite>("Image/IconsPet/" + petID);

            if (imgHe != null)
            {
                imgHe.sprite = Resources.Load<Sprite>("Image/Attribute/" + pet.elementType);
            }

            petUIObject.name = petID;

            if (petIcon != null && petSprite != null)
            {
                petIcon.sprite = petSprite;
            }

            if (txtLv != null)
            {
                txtLv.text = "Lv" + pet.level;
            }

            Button petButton = petUIObject.GetComponent<Button>();
            if (petButton != null)
            {
                petButton.onClick.AddListener(() => OnPetClicked(petID));
            }

            // ✅ KHÔNG animate pet entry nữa - hiện luôn
            petUIObject.transform.localScale = Vector3.one;
        }

        // Load enemy pet
        OnEnemyPet(selectedPetId.ToString());
    }

    void OnRoomReceived(RoomDTO room)
    {
        Debug.Log("[ManagerRoom] Room data received");
        roomData = room;
        if (roomData.cards != null && roomData.cards.Count > 0)
        {
            Debug.Log($"[ManagerRoom] ✓ Found {roomData.cards.Count} cards");

            // Hiển thị cards lên Toggle để chọn
            DisplayCardsForSelection(roomData.cards);

        }
        else
        {
            Debug.LogWarning("[ManagerRoom] No cards found for user!");
        }

        if (txtCount != null)
        {
            if (room.count >= room.requestPass)
            {
                txtCount.text = $"<color=yellow>{room.count}</color>/{room.requestPass}";
            }
            else
            {
                txtCount.text = $"<color=red>{room.count}</color>/{room.requestPass}";
            }
        }

        if (txtNamePetEnemy != null)
        {
            txtNamePetEnemy.text = room.nameEnemyPetId;
        }

        OnPetClicked(room.petId.ToString());

        PlayerPrefs.SetInt("userPetId", room.petId);
        PlayerPrefs.SetInt("count", room.count);
        PlayerPrefs.SetInt("requestPass", room.requestPass);
        // PlayerPrefs.SetString("elementType", room.elementType);
        PlayerPrefs.SetString("BossElementType", room.elementType);
        PlayerPrefs.Save();
    }

    // ✅ THAY THẾ METHOD DisplayCardsForSelection TRONG ManagerRoom.cs

    /// <summary>
    /// ✅ ĐỔ CARDS ĐỘNG VÀO ListToggle (tạo Toggle prefab cho mỗi card)
    /// </summary>
    void DisplayCardsForSelection(List<CardData> cards)
    {
        if (toggleManager == null || toggleManager.listToggle == null)
        {
            Debug.LogError("[ManagerRoom] ToggleManager or listToggle is null!");
            return;
        }

        Debug.Log($"[ManagerRoom] ✓ Displaying {cards.Count} cards in ListToggle");

        // ✅ XÓA TẤT CẢ TOGGLE CŨ VÀ SELECTEDIMAGES
        toggleManager.ClearAllToggles();

        // ✅ KHÔNG LOAD previouslySelectedCardIds NỮA - luôn bắt đầu từ đầu

        // ✅ TẠO TOGGLE MỚI CHO MỖI CARD (tất cả đều isOn = false)
        for (int i = 0; i < cards.Count; i++)
        {
            CardData card = cards[i];

            GameObject toggleObj = CreateCardToggle(card, i);

            if (toggleObj != null)
            {
                toggleObj.transform.SetParent(toggleManager.listToggle.transform, false);

                // ✅ KHÔNG RESTORE - tất cả toggle đều bắt đầu với isOn = false

                // Animation
                toggleObj.transform.localScale = Vector3.zero;
                LeanTween.scale(toggleObj, Vector3.one, 0.3f)
                    .setEaseOutBack()
                    .setDelay(i * 0.05f);
            }
        }

        Debug.Log($"[ManagerRoom] ✓ Cards displayed (no restoration - fresh start)");
    }

    /// <summary>
    /// ✅ TẠO TOGGLE CHO MỘT CARD
    /// </summary>
    GameObject CreateCardToggle(CardData card, int index)
    {
        // ✅ OPTION 1: Nếu có Toggle Prefab
        if (toggleManager.togglePrefab != null)
        {
            GameObject toggleObj = Instantiate(toggleManager.togglePrefab);
            toggleManager.RegisterToggle(toggleObj.GetComponent<Toggle>());
            SetupToggle(toggleObj, card);
            return toggleObj;
        }

        // ✅ OPTION 2: Tạo Toggle động (nếu không có prefab)
        else
        {
            return CreateToggleDynamic(card, index);
        }
    }

    /// <summary>
    /// ✅ SETUP TOGGLE VỚI CARD DATA
    /// </summary>
    void SetupToggle(GameObject toggleObj, CardData card)
    {
        // Gắn CardData
        CardToggleData toggleData = toggleObj.GetComponent<CardToggleData>();
        if (toggleData == null)
        {
            toggleData = toggleObj.AddComponent<CardToggleData>();
        }
        toggleData.cardData = card;

        // Load sprite
        Image[] images = toggleObj.GetComponentsInChildren<Image>();
        if (images.Length > 1)
        {
            Sprite cardSprite = Resources.Load<Sprite>($"Image/Card/card{card.cardId}");
            if (cardSprite != null)
            {
                images[1].sprite = cardSprite;
                Debug.Log($"[ManagerRoom] ✓ Loaded sprite for card {images[1].gameObject.name} (ID: {card.cardId})");
            }
            else
            {
                Debug.LogWarning($"[ManagerRoom] Sprite not found: Image/Card/card{card.cardId}");
            }
        }

        // ✅ KIỂM TRA NẾU LÀ THẺ ATTACK
        bool isAttackCard = card.elementTypeCard != null && card.elementTypeCard.ToUpper() == "ATTACK";

        // Set text (nếu có)
        Text[] texts = toggleObj.GetComponentsInChildren<Text>();
        foreach (Text txt in texts)
        {
            if (txt.name.Contains("Name"))
            {
                txt.text = card.name;
            }
            else if (txt.name.Contains("Level"))
            {
                txt.text = $"Lv.{card.level}";
            }
            else if (txt.name.Contains("Count"))
            {
                // ✅ NẾU LÀ THẺ ATTACK THÌ KHÔNG HIỂN THỊ COUNT
                if (isAttackCard)
                {
                    txt.text = "";
                }
                else
                {
                    txt.text = $"x{card.count}";
                }
            }
            else if (txt.name.Contains("Value"))
            {
                txt.text = card.value.ToString();
            }
        }

        // Setup Toggle component
        Toggle toggle = toggleObj.GetComponent<Toggle>();
        if (toggle != null)
        {
            toggle.isOn = false;
            toggle.group = toggleManager.listToggle.GetComponent<ToggleGroup>();
        }
    }

    /// <summary>
    /// ✅ TẠO TOGGLE ĐỘNG (nếu không có prefab)
    /// </summary>
    GameObject CreateToggleDynamic(CardData card, int index)
    {
        // Tạo GameObject cho Toggle
        GameObject toggleObj = new GameObject($"Toggle_Card_{card.cardId}");

        // Add Toggle component
        Toggle toggle = toggleObj.AddComponent<Toggle>();
        toggle.isOn = false;

        // Add RectTransform
        RectTransform rt = toggleObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100, 100);

        // Add Background Image
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(toggleObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = Color.white;
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;

        // Add Card Image
        GameObject cardImgObj = new GameObject("CardImage");
        cardImgObj.transform.SetParent(toggleObj.transform, false);
        Image cardImage = cardImgObj.AddComponent<Image>();

        // Load sprite
        Sprite cardSprite = Resources.Load<Sprite>($"Card/card{card.cardId}");
        if (cardSprite != null)
        {
            cardImage.sprite = cardSprite;
        }

        RectTransform cardRt = cardImgObj.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.1f, 0.1f);
        cardRt.anchorMax = new Vector2(0.9f, 0.9f);
        cardRt.sizeDelta = Vector2.zero;

        // Add Checkmark
        GameObject checkObj = new GameObject("Checkmark");
        checkObj.transform.SetParent(toggleObj.transform, false);
        Image checkImage = checkObj.AddComponent<Image>();
        checkImage.color = Color.green;
        checkObj.SetActive(false);

        toggle.targetGraphic = bgImage;
        toggle.graphic = checkImage;

        // Gắn CardData
        CardToggleData toggleData = toggleObj.AddComponent<CardToggleData>();
        toggleData.cardData = card;

        return toggleObj;
    }

    void OnUserReceived(UserDTO user)
    {
        Debug.Log("[ManagerRoom] User data received");

        // ✅ LƯU NĂNG LƯỢNG HIỆN TẠI
        currentUserEnergy = user.energy;
        Debug.Log($"[ManagerRoom] Current energy: {currentUserEnergy}");

        if (txtNl != null)
        {
            txtNl.text = user.energy + "/" + user.energyFull;
        }
        if (txtVang != null)
        {
            txtVang.text = user.gold.ToString();
        }
        if (txtCt != null)
        {
            txtCt.text = user.requestAttack.ToString();
        }
        if (txtLvRoom != null)
        {
            txtLvRoom.text = "Lv" + user.lever.ToString();
        }
        if (txtManaRoom != null)
        {
            txtManaRoom.text = user.energy + "/" + user.energyFull;
        }
        if (txtUsername != null)
        {
            txtUsername.text = user.name;
        }

        if (imgUser != null)
        {
            Sprite petSprite = Resources.Load<Sprite>("Image/Avt/" + user.avtId);
            if (petSprite != null)
            {
                imgUser.sprite = petSprite;
            }
        }
    }

    /// <summary>
    /// ✅ XỬ LÝ KHI NHẤN NÚT BẮT ĐẦU
    /// </summary>
    // ✅ THAY ĐỔI TRONG ManagerRoom.cs

    /// <summary>
    /// ✅ XỬ LÝ KHI NHẤN NÚT BẮT ĐẦU - CHỈ LƯU KHI NHẤN NÀY
    /// </summary>
    public void OnStartBattle()
    {
        Debug.Log("[ManagerRoom] Start Battle clicked");

        if (toggleManager == null)
        {
            Debug.LogError("[ManagerRoom] ToggleManager is null!");
            return;
        }

        // Lấy cards đã chọn
        selectedCards = toggleManager.GetSelectedCards();

        if (selectedCards.Count == 0)
        {
            Debug.LogWarning("[ManagerRoom] No cards selected!");
            // Có thể hiện warning UI
            return;
        }

        Debug.Log($"[ManagerRoom] ✓ Selected {selectedCards.Count} cards:");
        foreach (var card in selectedCards)
        {
            Debug.Log($"  - {card.name} (ID: {card.cardId})");
        }

        // ✅ CHỈ LƯU VÀO PLAYERPREFS KHI NHẤN START BATTLE
        CardListWrapper wrapper = new CardListWrapper { cards = selectedCards };
        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString("SelectedCards", json);
        PlayerPrefs.Save();

        Debug.Log("[ManagerRoom] ✓ Saved selected cards to PlayerPrefs");

    }

    /// <summary>
    /// ✅ HIỂN THỊ THÔNG BÁO HẾT NĂNG LƯỢNG
    /// </summary>
    private void ShowEnergyWarning()
    {
        if (energyWarningPanel == null)
        {
            Debug.LogWarning("[ManagerRoom] Energy warning panel not assigned!");
            return;
        }

        Debug.Log("[ManagerRoom] → Showing energy warning");

        // ✅ HIỂN THỊ PANEL
        energyWarningPanel.SetActive(true);

        // ✅ SET TEXT
        if (energyWarningText != null)
        {
            energyWarningText.text = "Bạn đã hết năng lượng!\nVui lòng nạp thêm năng lượng để tiếp tục.";
        }

        // ✅ SETUP BUTTON - CHỈ ĐÓNG POPUP
        if (energyWarningOkButton != null)
        {
            energyWarningOkButton.onClick.RemoveAllListeners();
            energyWarningOkButton.onClick.AddListener(() =>
            {
                HideEnergyWarning();
                // ✅ KHÔNG GỌI ReturnToQuangTruong() NỮA
            });

            // Đổi text button
            Text btnText = energyWarningOkButton.GetComponentInChildren<Text>();
            if (btnText != null)
            {
                btnText.text = "Đóng";
            }
        }

        // ✅ ANIMATION PANEL
        energyWarningPanel.transform.localScale = Vector3.zero;
        LeanTween.scale(energyWarningPanel, Vector3.one, 0.4f)
            .setEaseOutBack()
            .setIgnoreTimeScale(true);

        // ✅ FADE IN
        CanvasGroup cg = energyWarningPanel.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = energyWarningPanel.AddComponent<CanvasGroup>();
        }
        cg.alpha = 0f;
        LeanTween.alphaCanvas(cg, 1f, 0.3f).setIgnoreTimeScale(true);
    }

    /// <summary>
    /// ✅ ẨN THÔNG BÁO NĂNG LƯỢNG
    /// </summary>
    private void HideEnergyWarning()
    {
        if (energyWarningPanel == null) return;

        Debug.Log("[ManagerRoom] → Hiding energy warning");

        LeanTween.scale(energyWarningPanel, Vector3.zero, 0.3f)
            .setEaseInBack()
            .setIgnoreTimeScale(true)
            .setOnComplete(() => energyWarningPanel.SetActive(false));
    }

    /// <summary>
    /// ✅ TRỞ VỀ QUẢNG TRƯỜNG KHI HẾT NĂNG LƯỢNG
    /// </summary>
    private void ReturnToQuangTruong()
    {
        Debug.Log("[ManagerRoom] Returning to QuangTruong - Out of energy");

        // ✅ XÓA FLAGS
        PlayerPrefs.DeleteKey("ReturnToRoom");
        PlayerPrefs.DeleteKey("ReturnToChinhPhuc");
        PlayerPrefs.DeleteKey("ReturnToPanelIndex");
        PlayerPrefs.DeleteKey("SelectedCards");
        PlayerPrefs.Save();

        // ✅ LOAD SCENE
        LeanTween.cancelAll();
        LeanTween.reset();
        UnityEngine.SceneManagement.SceneManager.LoadScene("QuangTruong");
    }

    /// <summary>
    /// ✅ GỌI TRONG Start() HOẶC OpenRoomPanel()
    /// </summary>
    private void SetupCardSelection()
    {
        if (btnStartBattle != null)
        {
            btnStartBattle.onClick.AddListener(OnStartBattle);
        }
    }

    void ReplaceAnimations(AnimationClip[] newClips)
    {
        if (animator == null) return;

        RuntimeAnimatorController originalController = animator.runtimeAnimatorController;
        AnimatorOverrideController overrideController = new AnimatorOverrideController(originalController);

        foreach (AnimationClip newClip in newClips)
        {
            foreach (var pair in overrideController.animationClips)
            {
                if (pair.name == newClip.name)
                {
                    overrideController[pair] = newClip;
                }
            }
        }

        animator.runtimeAnimatorController = overrideController;
    }

    void ReplaceAnimationsEnemyPet(AnimationClip[] newClips)
    {
        if (enemyPet == null) return;

        RuntimeAnimatorController originalController = enemyPet.runtimeAnimatorController;
        AnimatorOverrideController overrideController = new AnimatorOverrideController(originalController);

        foreach (AnimationClip newClip in newClips)
        {
            foreach (var pair in overrideController.animationClips)
            {
                if (pair.name == newClip.name)
                {
                    overrideController[pair] = newClip;
                }
            }
        }

        enemyPet.runtimeAnimatorController = overrideController;
    }

    void OnPetClicked(string petId)
    {
        PlayerPrefs.SetInt("userPetId", int.Parse(petId));
        PlayerPrefs.Save();

        AnimationClip[] clips = Resources.LoadAll<AnimationClip>($"Pets/{petId}");

        if (clips != null && clips.Length > 0 && animator != null)
        {
            ReplaceAnimations(clips);
        }
        else if (imgPet != null)
        {
            Sprite sprite = Resources.Load<Sprite>("Image/IconsPet/" + petId);
            if (sprite != null)
            {
                imgPet.sprite = sprite;
            }
        }
    }

    void OnEnemyPet(string petId)
    {
        AnimationClip[] clips = Resources.LoadAll<AnimationClip>($"Pets/{petId}");

        if (clips != null && clips.Length > 0 && enemyPet != null)
        {
            ReplaceAnimationsEnemyPet(clips);
        }
        else if (imgEnemyPet != null)
        {
            Sprite sprite = Resources.Load<Sprite>("Image/IconsPet/" + petId);
            if (sprite != null)
            {
                imgEnemyPet.sprite = sprite;
            }
        }
    }

    void OnError(string error)
    {
        Debug.LogError("[ManagerRoom] API Error: " + error);

        // ✅ NẾU CÓ LỖI, HIDE LOADING
        HideLoadingInstant();
    }

    private void OnDestroy()
    {
        // Cancel all animations
        LeanTween.cancel(gameObject);

        if (roomPanel != null) LeanTween.cancel(roomPanel);
        if (loading != null) LeanTween.cancel(loading);
        if (panelPet != null) LeanTween.cancel(panelPet);
        if (panelCard != null) LeanTween.cancel(panelCard);
    }

    public void LoadScene(string nameScene)
    {
        // ✅ KIỂM TRA NĂNG LƯỢNG TRƯỚC KHI VÀO MATCH
        if (nameScene == "Match")
        {
            if (currentUserEnergy <= 1)
            {
                Debug.LogWarning($"[ManagerRoom] ⚠ Cannot start battle - Insufficient energy: {currentUserEnergy}");
                ShowEnergyWarning();
                return; // ✅ DỪNG LẠI, KHÔNG LOAD SCENE
            }
        }

        // ✅ GỌI OnStartBattle ĐỂ LƯU TRẠNG THÁI TRƯỚC KHI VÀO MATCH
        OnStartBattle();

        // ✅ LƯU ĐẦY ĐỦ TRẠNG THÁI TRƯỚC KHI CHUYỂN
        if (nameScene == "Match")
        {
            int activePanelIndex = PlayerPrefs.GetInt("ActivePanelIndex", -1);

            PlayerPrefs.SetInt("ReturnToRoom", 1);
            PlayerPrefs.SetInt("ReturnToChinhPhuc", 1);
            PlayerPrefs.SetInt("ReturnToPanelIndex", activePanelIndex);
            PlayerPrefs.Save();

            Debug.Log($"[ManagerRoom] Saved state: PanelIndex={activePanelIndex}");
            Debug.Log($"[ManagerRoom] ✓ Energy check passed: {currentUserEnergy} > 1");
        }

        LeanTween.cancelAll();
        LeanTween.reset();
        SceneManager.LoadScene(nameScene);
    }
    /// <summary>
    /// ✅ XÓA TRẠNG THÁI ĐÃ LƯU (gọi khi hoàn thành Match hoặc muốn reset)
    /// </summary>
    public void ClearSelectedCardsState()
    {
        PlayerPrefs.DeleteKey("SelectedCards");
        PlayerPrefs.Save();

        Debug.Log("[ManagerRoom] ✓ Cleared selected cards state");
    }
}