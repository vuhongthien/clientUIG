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
    public Animator enemyPet;
    public Image imgPet;
    public Image imgEnemyPet;
    public Image imgUser;
    public GameObject petUIPrefab;
    public Transform petListContainer;
    public Text txtVang;
    public Text txtCt;
    public Text txtNl;
    public Image imgLvRoom;
    public Text txtManaRoom;
    public Text txtUsername;
    public Text txtCount;
    public Text txtNamePetEnemy;
    [Header("Card Selection")]
    public GameObject panelSelectCards;    // Panel chọn thẻ
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

    [Header("Invite System")]
    public GameObject panelInviteList;
    public Transform inviteListContainer;
    public GameObject userInvitePrefab;
    public Text txtInviteCount;

    private long currentRoomId;
    private List<OnlineUserDTO> onlineUsers = new List<OnlineUserDTO>();

    [Header("Invite Popup")]
    public GameObject panelInvitePopup;
    public Text txtInviteMessage;
    public Button btnAcceptInvite;
    public Button btnDeclineInvite;
    [Header("Room Info")]
    public Text txtIdRoom;          // Hiển thị Room ID
    public Button btnCopyRoomId;

    [Header("Join Room System")]
    public GameObject panelJoinRoom;
    public InputField inputRoomId;
    public Button btnJoinRoom;
    public Button btnCloseJoinPanel;
    public Text txtJoinError;
    public Button btnShowJoinPanel;

    [Header("Room Members Display")]
    public Transform memberListContainer;  // Container để hiển thị members
    public GameObject memberUIPrefab;

    private RoomInviteDTO currentInvite;
    [Header("Card Display in Member")]
    public GameObject cardIconPrefab;
    private List<CardData> availableCards = new List<CardData>();
    private List<PetUserDTO> availablePets = new List<PetUserDTO>();
    [Header("Room Closed Notification")]
    public GameObject panelRoomClosed;
    public Text txtRoomClosedMessage;
    public Button btnRoomClosedOk;
    [Header("Ready & Start System")]
    public Button btnReady;          // Nút sẵn sàng (cho member)
    public Button btnStartBattle;    // Nút bắt đầu (cho host)
    public Text txtReadyStatus;      // Hiển thị trạng thái "X/Y người đã sẵn sàng"
    public GameObject readyIndicator; // Icon sẵn sàng của chính mình

    private bool isHost = false;
    private bool isReady = false;
    private bool allMembersReady = false;
    private void Start()
    {
        Debug.Log("[ManagerRoom] Start - Room panel initialized");

        if (roomPanel != null)
        {
            roomPanel.SetActive(false);
        }

        if (loading != null)
        {
            loading.SetActive(false);
        }

        SetupCardSelection();

        if (btnBackToChinhPhuc != null)
        {
            btnBackToChinhPhuc.onClick.AddListener(CloseRoomPanel);
        }

        SetupJoinRoomPanel();
        SetupWebSocket();

        if (btnCopyRoomId != null)
        {
            btnCopyRoomId.onClick.AddListener(CopyRoomIdToClipboard);
        }

        PlayerPrefs.DeleteKey("SelectedCards");
        PlayerPrefs.Save();

        // ✅ THÊM: SUBSCRIBE VÀO EVENT CARDS CHANGED
        if (toggleManager != null)
        {
            toggleManager.OnCardsChanged += OnCardsChangedInToggle;
            Debug.Log("[ManagerRoom] ✓ Subscribed to ToggleManager.OnCardsChanged");
        }
        if (btnRoomClosedOk != null)
        {
            btnRoomClosedOk.onClick.AddListener(() =>
            {
                HideRoomClosedNotification();
                ReturnToChinhPhuc();
            });
        }
        SetupReadySystem();
    }
    /// <summary>
    /// ✅ SETUP HỆ THỐNG READY
    /// </summary>
    private void SetupReadySystem()
    {
        // Setup button listeners
        if (btnReady != null)
        {
            btnReady.onClick.RemoveAllListeners();
            btnReady.onClick.AddListener(OnReadyButtonClicked);
        }

        if (btnStartBattle != null)
        {
            btnStartBattle.onClick.RemoveAllListeners();
            btnStartBattle.onClick.AddListener(OnStartBattleClicked);
        }

        // Subscribe WebSocket events
        RoomWebSocketManager.Instance.OnReadyStatusChanged += OnReadyStatusChanged;
    }
    /// <summary>
    /// ✅ XỬ LÝ KHI HOST NHẤN NÚT BẮT ĐẦU
    /// </summary>
    private void OnStartBattleClicked()
    {
        if (!isHost)
        {
            Debug.LogWarning("[ManagerRoom] Only host can start battle!");
            return;
        }

        if (!allMembersReady)
        {
            Debug.LogWarning("[ManagerRoom] Not all members are ready!");
            ShowErrorMessage("Chưa đủ người sẵn sàng!");
            return;
        }

        // ✅ KIỂM TRA NĂNG LƯỢNG
        if (currentUserEnergy <= 1)
        {
            Debug.LogWarning($"[ManagerRoom] Insufficient energy: {currentUserEnergy}");
            ShowEnergyWarning();
            return;
        }

        // ✅ KIỂM TRA ĐÃ CHỌN CARDS CHƯA
        if (selectedCards == null || selectedCards.Count == 0)
        {
            Debug.LogWarning("[ManagerRoom] No cards selected!");
            ShowErrorMessage("Vui lòng chọn thẻ bài trước khi bắt đầu!");
            return;
        }

        // ✅ KIỂM TRA ĐÃ CHỌN PET CHƯA
        int userPetId = PlayerPrefs.GetInt("userPetId", 0);
        if (userPetId <= 0)
        {
            Debug.LogWarning("[ManagerRoom] No pet selected!");
            ShowErrorMessage("Vui lòng chọn pet trước khi bắt đầu!");
            return;
        }

        Debug.Log("[ManagerRoom] ✓ All conditions met - Starting battle!");

        // Start match
        LoadScene("Match");
    }

    /// <summary>
    /// ✅ HIỂN THỊ THÔNG BÁO LỖI
    /// </summary>
    private void ShowErrorMessage(string message)
    {
        if (txtJoinError != null)
        {
            txtJoinError.text = message;
            txtJoinError.color = Color.red;
            txtJoinError.gameObject.SetActive(true);
            StartCoroutine(HideErrorMessageAfterDelay(3f));
        }
    }

    private IEnumerator HideErrorMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (txtJoinError != null)
        {
            txtJoinError.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// ✅ XỬ LÝ KHI NHẤN NÚT SẴN SÀNG
    /// </summary>
    private void OnReadyButtonClicked()
    {
        if (isHost)
        {
            Debug.LogWarning("[ManagerRoom] Host cannot use ready button!");
            return;
        }

        // Toggle ready state
        isReady = !isReady;

        Debug.Log($"[ManagerRoom] Setting ready status: {isReady}");

        // Send to server
        int userId = PlayerPrefs.GetInt("userId", 0);
        RoomWebSocketManager.Instance.SetReady(currentRoomId, userId, isReady);

        // Update UI immediately
        UpdateReadyButtonUI();
    }

    /// <summary>
    /// ✅ CẬP NHẬT UI NÚT READY
    /// </summary>
    private void UpdateReadyButtonUI()
    {
        if (btnReady == null) return;

        Text btnText = btnReady.GetComponentInChildren<Text>();
        Image btnImage = btnReady.GetComponent<Image>();

        if (isReady)
        {
            if (btnText != null) btnText.text = "Hủy";
            if (btnImage != null) btnImage.color = new Color(1f, 0.5f, 0.5f); // Màu đỏ nhạt
        }
        else
        {
            if (btnText != null) btnText.text = "Sẵn sàng";
        }

        // Update ready indicator
        if (readyIndicator != null)
        {
            readyIndicator.SetActive(isReady);
        }
    }

    /// <summary>
    /// ✅ XỬ LÝ KHI NHẬN READY STATUS TỪ SERVER
    /// </summary>
    private void OnReadyStatusChanged(bool allReady, int readyCount, int totalMembers)
    {
        Debug.Log($"[ManagerRoom] Ready status: {readyCount}/{totalMembers} members ready, allReady={allReady}");

        allMembersReady = allReady;

        // Update status text
        if (txtReadyStatus != null)
        {
            // ✅ HIỂN THỊ THEO LOGIC MỚI
            if (totalMembers == 0)
            {
                // Chỉ có host solo
                txtReadyStatus.text = "Sẵn sàng bắt đầu";
            }
            else
            {
                // Có members
                txtReadyStatus.text = $"{readyCount}/{totalMembers} người đã sẵn sàng";
            }
        }

        // Update start button state
        UpdateStartButtonState();
    }

    /// <summary>
    /// ✅ CẬP NHẬT TRẠNG THÁI NÚT BẮT ĐẦU
    /// </summary>
    private void UpdateStartButtonState()
    {
        if (btnStartBattle == null) return;

        if (isHost)
        {
            // ✅ HOST: Enable nếu tất cả đã ready (hoặc chỉ có 1 người)
            btnStartBattle.interactable = allMembersReady;

            Text btnText = btnStartBattle.GetComponentInChildren<Text>();
            if (btnText != null)
            {
                btnText.text = "Bắt đầu";
            }

            // Change color
            Image btnImage = btnStartBattle.GetComponent<Image>();
            if (btnImage != null)
            {
                btnImage.color = allMembersReady
                    ? Color.white // Green - có thể bắt đầu
                    : new Color(0.5f, 0.5f, 0.5f); // Gray - chưa thể bắt đầu
            }

            Debug.Log($"[ManagerRoom] Host button state: interactable={allMembersReady}");
        }
        else
        {
            // Member: Hide start button, show ready button
            btnStartBattle.gameObject.SetActive(false);
            if (btnReady != null)
            {
                btnReady.gameObject.SetActive(true);
            }
        }
    }
    private void Awake()
    {
        // ✅ TĂNG SỐ SLOTS CHO LEANTWEEN
        LeanTween.init(2000); // Tăng từ 400 (default) lên 2000
        Debug.Log("[ManagerRoom] LeanTween initialized with 2000 slots");
    }

    /// <summary>
    /// ✅ XỬ LÝ KHI CARDS THAY ĐỔI TRONG TOGGLE MANAGER
    /// </summary>
    private void OnCardsChangedInToggle(List<CardData> currentCards)
    {
        Debug.Log($"[ManagerRoom] ========== CARDS CHANGED ==========");
        Debug.Log($"[ManagerRoom] Current selected: {currentCards.Count} cards");

        // ✅ CẬP NHẬT SELECTED CARDS
        selectedCards = new List<CardData>(currentCards);

        // ✅ CẬP NHẬT VÀO MEMBER TRONG ROOMDATA
        int currentUserId = PlayerPrefs.GetInt("userId", 0);

        if (roomData != null && roomData.members != null)
        {
            foreach (var member in roomData.members)
            {
                if (member.userId == currentUserId)
                {
                    // ✅ CẬP NHẬT CARDS
                    member.cards = new List<CardData>(currentCards);

                    Debug.Log($"[ManagerRoom] ✓ Updated member {member.username} cards: {member.cards.Count}");

                    // ✅ TÌM VÀ CẬP NHẬT NGAY PANELCARDUSER (KHÔNG REFRESH TẤT CẢ)
                    UpdateCurrentMemberCardsUI(member);

                    break;
                }
            }
        }

        Debug.Log($"[ManagerRoom] ========================================");
    }
    /// <summary>
    /// ✅ CẬP NHẬT CHỈ PANELCARDUSER CỦA MEMBER HIỆN TẠI (KHÔNG REFRESH TẤT CẢ)
    /// </summary>
    private void UpdateCurrentMemberCardsUI(RoomMemberDTO member)
    {
        if (memberListContainer == null)
        {
            Debug.LogWarning("[ManagerRoom] memberListContainer is null");
            return;
        }

        Debug.Log($"[ManagerRoom] → Updating cards UI for {member.username}...");

        int currentUserId = PlayerPrefs.GetInt("userId", 0);

        // ✅ TÌM PREFAB CỦA MEMBER HIỆN TẠI
        foreach (Transform child in memberListContainer)
        {
            Text txtUsername = child.Find("txtUsername")?.GetComponent<Text>();

            if (txtUsername != null)
            {
                string displayName = txtUsername.text.Replace(" (You)", "").Trim();

                if (displayName == member.username && member.userId == currentUserId)
                {
                    Debug.Log($"[ManagerRoom] ✓ Found current member prefab: {child.name}");

                    // ✅ TÌM PanelCardUser
                    Transform panelCardUser = child.Find("PanelCardUser");

                    if (panelCardUser != null)
                    {
                        Debug.Log($"[ManagerRoom] ✓ Found PanelCardUser");

                        // ✅ CẬP NHẬT CARDS
                        DisplayMemberCards(panelCardUser, member.cards);
                    }
                    else
                    {
                        Debug.LogError($"[ManagerRoom] ✗ PanelCardUser not found!");

                        // Debug: List children
                        Debug.Log($"[ManagerRoom] Available children in {child.name}:");
                        foreach (Transform subChild in child)
                        {
                            Debug.Log($"  - {subChild.name}");
                        }
                    }

                    break;
                }
            }
        }
    }
    /// <summary>
    /// ✅ SETUP PANEL JOIN ROOM
    /// </summary>
    private void SetupJoinRoomPanel()
    {
        if (btnJoinRoom != null)
        {
            btnJoinRoom.onClick.AddListener(OnJoinRoomClicked);
        }

        if (btnCloseJoinPanel != null)
        {
            btnCloseJoinPanel.onClick.AddListener(HideJoinRoomPanel);
        }

        if (btnShowJoinPanel != null)
        {
            btnShowJoinPanel.onClick.AddListener(ShowJoinRoomPanel);
        }

        if (panelJoinRoom != null)
        {
            panelJoinRoom.SetActive(false);
        }
    }
    private void ShowInvitePopup(RoomInviteDTO invite)
    {
        currentInvite = invite;

        if (panelInvitePopup != null)
        {
            panelInvitePopup.SetActive(true);

            if (txtInviteMessage != null)
            {
                txtInviteMessage.text = invite.message;
            }

            if (btnAcceptInvite != null)
            {
                btnAcceptInvite.onClick.RemoveAllListeners();
                btnAcceptInvite.onClick.AddListener(AcceptInvite);
            }

            if (btnDeclineInvite != null)
            {
                btnDeclineInvite.onClick.RemoveAllListeners();
                btnDeclineInvite.onClick.AddListener(DeclineInvite);
            }

            // Animation
            panelInvitePopup.transform.localScale = Vector3.zero;
            LeanTween.scale(panelInvitePopup, Vector3.one, 0.4f)
                .setEaseOutBack();
        }
    }

    private void AcceptInvite()
    {
        if (currentInvite == null) return;

        RoomWebSocketManager.Instance.AcceptInvite(currentInvite.inviteId);
        HideInvitePopup();

        // TODO: Join room
        Debug.Log($"[ManagerRoom] Joining room {currentInvite.roomId}...");
    }

    private void DeclineInvite()
    {
        if (currentInvite == null) return;

        RoomWebSocketManager.Instance.DeclineInvite(currentInvite.inviteId);
        HideInvitePopup();
    }

    private void HideInvitePopup()
    {
        if (panelInvitePopup == null) return;

        LeanTween.scale(panelInvitePopup, Vector3.zero, 0.25f)
            .setEaseInBack()
            .setOnComplete(() => panelInvitePopup.SetActive(false));
    }

    private void UpdateInviteCount()
    {
        if (txtInviteCount != null)
        {
            txtInviteCount.text = onlineUsers.Count.ToString();
        }
    }

    /// <summary>
    /// ✅ SETUP WEBSOCKET - SUBSCRIBE EVENTS
    /// </summary>
    private void SetupWebSocket()
    {
        // Subscribe existing events
        RoomWebSocketManager.Instance.OnOnlineUsersUpdated += OnOnlineUsersReceived;
        RoomWebSocketManager.Instance.OnInviteReceived += OnInviteReceived;
        RoomWebSocketManager.Instance.OnInviteResponseReceived += OnInviteResponseReceived;
        RoomWebSocketManager.Instance.OnRoomJoined += OnRoomJoinedSuccess;
        RoomWebSocketManager.Instance.OnJoinError += OnRoomJoinError;
        RoomWebSocketManager.Instance.OnRoomUpdated += OnRoomUpdateReceived;
        RoomWebSocketManager.Instance.OnPetUpdated += OnPetUpdatedFromServer;
        RoomWebSocketManager.Instance.OnCardsUpdated += OnCardsUpdatedFromServer;
        RoomWebSocketManager.Instance.OnRoomClosed += OnRoomClosed;
        RoomWebSocketManager.Instance.OnRoomLeft += OnRoomLeft;

        // ✅ SUBSCRIBE READY UPDATE
        RoomWebSocketManager.Instance.OnMemberReadyChanged += OnMemberReadyChanged;
        RoomWebSocketManager.Instance.OnKicked += OnKicked;
    }

    /// <summary>
    /// ✅ XỬ LÝ KHI BỊ KICK
    /// </summary>
    private void OnKicked(long roomId, string reason)
    {
        Debug.Log($"[ManagerRoom] ========================================");
        Debug.Log($"[ManagerRoom] ⚠️ KICKED FROM ROOM!");
        Debug.Log($"[ManagerRoom] Room ID: {roomId}");
        Debug.Log($"[ManagerRoom] Reason: {reason}");
        Debug.Log($"[ManagerRoom] ========================================");

        // ✅ RESET STATE
        currentRoomId = 0;
        roomData = null;
        selectedCards.Clear();
        availableCards.Clear();
        availablePets.Clear();
        isReady = false;
        isHost = false;
        allMembersReady = false;

        // ✅ HIỂN THỊ THÔNG BÁO
        ShowKickedNotification(reason);
    }

    /// <summary>
    /// ✅ HIỂN THỊ POPUP BỊ KICK
    /// </summary>
    private void ShowKickedNotification(string reason)
    {
        if (panelRoomClosed == null)
        {
            Debug.LogWarning("[ManagerRoom] panelRoomClosed not assigned!");
            ReturnToChinhPhuc();
            return;
        }

        panelRoomClosed.SetActive(true);

        if (txtRoomClosedMessage != null)
        {
            txtRoomClosedMessage.text = $"⚠️ {reason}";
        }

        // ✅ ANIMATION
        panelRoomClosed.transform.localScale = Vector3.zero;
        LeanTween.scale(panelRoomClosed, Vector3.one, 0.4f)
            .setEaseOutBack()
            .setIgnoreTimeScale(true);

        // ✅ Fade in
        CanvasGroup cg = panelRoomClosed.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = panelRoomClosed.AddComponent<CanvasGroup>();
        }
        cg.alpha = 0f;
        LeanTween.alphaCanvas(cg, 1f, 0.3f).setIgnoreTimeScale(true);
    }

    /// <summary>
    /// ✅ XỬ LÝ KHI MEMBER READY STATUS THAY ĐỔI (CHỈ CẬP NHẬT UI, KHÔNG GHI ĐÈ DATA)
    /// </summary>
    private void OnMemberReadyChanged(long userId, bool ready)
    {
        Debug.Log($"[ManagerRoom] Member {userId} ready changed: {ready}");

        // ✅ CẬP NHẬT ROOM DATA
        if (roomData != null && roomData.members != null)
        {
            foreach (var member in roomData.members)
            {
                if (member.userId == userId)
                {
                    member.ready = ready;
                    Debug.Log($"[ManagerRoom] ✓ Updated member {member.username} ready status: {ready}");
                    break;
                }
            }
        }

        // ✅ CẬP NHẬT CHỈ READY INDICATOR (KHÔNG REFRESH TOÀN BỘ)
        UpdateMemberReadyIndicator(userId, ready);
    }

    /// <summary>
    /// ✅ CẬP NHẬT CHỈ READY INDICATOR CỦA 1 MEMBER (KHÔNG REFRESH TẤT CẢ)
    /// </summary>
    private void UpdateMemberReadyIndicator(long userId, bool ready)
    {
        if (memberListContainer == null)
        {
            Debug.LogWarning("[ManagerRoom] memberListContainer is null");
            return;
        }

        Debug.Log($"[ManagerRoom] → Updating ready indicator for userId={userId}...");

        // ✅ TÌM PREFAB CỦA MEMBER NÀY
        foreach (Transform child in memberListContainer)
        {
            // Tìm theo userId (lưu trong tag hoặc name)
            RoomMemberDTO memberData = null;

            if (roomData != null && roomData.members != null)
            {
                // Tìm member data
                foreach (var member in roomData.members)
                {
                    if (member.userId == userId)
                    {
                        memberData = member;
                        break;
                    }
                }
            }

            if (memberData != null)
            {
                // Kiểm tra tên username
                Text txtUsername = child.Find("txtUsername")?.GetComponent<Text>();

                if (txtUsername != null)
                {
                    string displayName = txtUsername.text.Replace(" (You)", "").Trim();

                    if (displayName == memberData.username)
                    {
                        // ✅ TÌM VÀ CẬP NHẬT READY INDICATOR
                        GameObject readyIndicator = child.Find("txtready")?.gameObject;

                        if (readyIndicator != null)
                        {
                            readyIndicator.SetActive(ready);
                            Debug.Log($"[ManagerRoom] ✓ Updated ready indicator for {memberData.username}: {ready}");
                        }
                        else
                        {
                            Debug.LogWarning($"[ManagerRoom] Ready indicator not found for {memberData.username}");
                        }

                        break;
                    }
                }
            }
        }
    }
    /// <summary>
    /// ✅ XỬ LÝ KHI MEMBER RỜI PHÒNG THÀNH CÔNG
    /// </summary>
    private void OnRoomLeft(long roomId)
    {
        Debug.Log($"[ManagerRoom] ========================================");
        Debug.Log($"[ManagerRoom] ✅ LEFT ROOM SUCCESSFULLY!");
        Debug.Log($"[ManagerRoom] Room ID: {roomId}");
        Debug.Log($"[ManagerRoom] ========================================");

        // ✅ RESET STATE
        currentRoomId = 0;
        roomData = null;
        selectedCards.Clear();
        availableCards.Clear();
        availablePets.Clear();

        // ✅ ĐÓNG ROOM PANEL VÀ QUAY VỀ CHINH PHỤC
        ReturnToChinhPhuc();
        isReady = false;
        isHost = false;
        allMembersReady = false;
    }
    /// <summary>
    /// ✅ XỬ LÝ KHI PHÒNG BỊ ĐÓNG (HOST RỜI)
    /// </summary>
    private void OnRoomClosed(long roomId, string reason, bool isHost)
    {
        Debug.Log($"[ManagerRoom] ========================================");
        Debug.Log($"[ManagerRoom] 🚨 ROOM CLOSED!");
        Debug.Log($"[ManagerRoom] Room ID: {roomId}");
        Debug.Log($"[ManagerRoom] Reason: {reason}");
        Debug.Log($"[ManagerRoom] Is Host: {isHost}");
        Debug.Log($"[ManagerRoom] Current Room ID: {currentRoomId}");
        Debug.Log($"[ManagerRoom] ========================================");

        // ✅ KIỂM TRA XEM CÓ PHẢI PHÒNG HIỆN TẠI KHÔNG
        if (currentRoomId != roomId)
        {
            Debug.Log("[ManagerRoom] → Not current room, ignoring");
            return;
        }

        // ✅ RESET ROOM STATE
        currentRoomId = 0;
        roomData = null;
        selectedCards.Clear();
        availableCards.Clear();
        availablePets.Clear();

        // ✅ HIỂN THỊ THÔNG BÁO
        ShowRoomClosedNotification(reason, isHost);
    }
    /// <summary>
    /// ✅ HIỂN THỊ POPUP THÔNG BÁO PHÒNG ĐÓNG
    /// </summary>
    private void ShowRoomClosedNotification(string reason, bool isHost)
    {
        if (panelRoomClosed == null)
        {
            Debug.LogWarning("[ManagerRoom] panelRoomClosed not assigned!");
            // ✅ Fallback: tự động quay về nếu không có panel
            ReturnToChinhPhuc();
            return;
        }

        panelRoomClosed.SetActive(true);

        if (txtRoomClosedMessage != null)
        {
            if (isHost)
            {
                txtRoomClosedMessage.text = "Bạn đã rời phòng!";
            }
            else
            {
                txtRoomClosedMessage.text = $"⚠️ {reason}";
            }
        }

        // ✅ ANIMATION
        panelRoomClosed.transform.localScale = Vector3.zero;
        LeanTween.scale(panelRoomClosed, Vector3.one, 0.4f)
            .setEaseOutBack()
            .setIgnoreTimeScale(true);

        // ✅ Fade in
        CanvasGroup cg = panelRoomClosed.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = panelRoomClosed.AddComponent<CanvasGroup>();
        }
        cg.alpha = 0f;
        LeanTween.alphaCanvas(cg, 1f, 0.3f).setIgnoreTimeScale(true);
    }

    /// <summary>
    /// ✅ ẨN THÔNG BÁO
    /// </summary>
    private void HideRoomClosedNotification()
    {
        if (panelRoomClosed == null) return;

        LeanTween.scale(panelRoomClosed, Vector3.zero, 0.3f)
            .setEaseInBack()
            .setIgnoreTimeScale(true)
            .setOnComplete(() => panelRoomClosed.SetActive(false));
    }
    /// <summary>
    /// ✅ TRỞ VỀ CHINH PHỤC
    /// </summary>
    private void ReturnToChinhPhuc()
    {
        Debug.Log("[ManagerRoom] Returning to Chinh Phuc...");

        // ✅ ĐÓNG ROOM PANEL
        if (roomPanel != null)
        {
            roomPanel.SetActive(false);
        }

        // ✅ MỞ CHINH PHỤC
        ManagerChinhPhuc chinhPhucManager = FindObjectOfType<ManagerChinhPhuc>();
        if (chinhPhucManager != null)
        {
            chinhPhucManager.gameObject.SetActive(true);
        }

        // ✅ CLEAR STATE
        PlayerPrefs.DeleteKey("ReturnToRoom");
        PlayerPrefs.DeleteKey("SelectedCards");
        PlayerPrefs.Save();
    }

    /// <summary>
    /// ✅ XỬ LÝ KHI PET CẬP NHẬT TỪ SERVER (player khác đổi pet)
    /// </summary>
    private void OnPetUpdatedFromServer(long userId, int petId)
    {
        Debug.Log($"[ManagerRoom] Pet updated from server: userId={userId}, petId={petId}");

        // ✅ Nếu là chính user này → bỏ qua (đã update local rồi)
        int currentUserId = PlayerPrefs.GetInt("userId", 0);
        if (userId == currentUserId)
        {
            Debug.Log($"[ManagerRoom] → Skipping self update");
            return;
        }

        // ✅ Cập nhật pet của member trong roomData
        if (roomData != null && roomData.members != null)
        {
            bool found = false;
            foreach (var member in roomData.members)
            {
                if (member.userId == userId)
                {
                    member.petId = petId;
                    found = true;
                    Debug.Log($"[ManagerRoom] → Updated member {member.username} pet to {petId}");
                    break;
                }
            }

            if (found)
            {
                // ✅ REFRESH UI PREFABS
                DisplayRoomMembers(roomData.members);
            }
        }
    }

    /// <summary>
    /// ✅ XỬ LÝ KHI CARDS CẬP NHẬT TỪ SERVER (player khác chọn cards)
    /// </summary>
    private void OnCardsUpdatedFromServer(long userId, List<CardData> selectedCards)
    {
        Debug.Log($"[ManagerRoom] Cards updated from server: userId={userId}, count={selectedCards.Count}");

        // ✅ Nếu là chính user này → bỏ qua
        int currentUserId = PlayerPrefs.GetInt("userId", 0);
        if (userId == currentUserId)
        {
            Debug.Log($"[ManagerRoom] → Skipping self update");
            return;
        }

        // ✅ Cập nhật SELECTED CARDS của member trong roomData
        if (roomData != null && roomData.members != null)
        {
            bool found = false;
            foreach (var member in roomData.members)
            {
                if (member.userId == userId)
                {
                    // ✅ CẬP NHẬT CHỈ CARDS ĐÃ CHỌN (không phải tất cả cards)
                    member.cards = selectedCards;
                    found = true;
                    Debug.Log($"[ManagerRoom] → Updated member {member.username} selected cards: {selectedCards.Count}");
                    break;
                }
            }

            // ✅ REFRESH UI ĐỂ HIỂN THỊ CARDS ĐÃ CHỌN
            if (found)
            {
                DisplayRoomMembers(roomData.members);
            }
        }
    }
    /// <summary>
    /// ✅ HELPER: Clear cards display (nếu cần)
    /// </summary>
    private void ClearMemberCards(Transform panelCardUser)
    {
        if (panelCardUser == null) return;

        foreach (Transform child in panelCardUser)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// ✅ CẬP NHẬT KHI ROOM UPDATE
    /// </summary>
    private void OnRoomUpdateReceived(RoomDTO room)
    {
        Debug.Log($"[ManagerRoom] Room updated: {room.roomId}");

        roomData = room;
        DisplayRoomMembers(room.members);

        if (txtIdRoom != null)
        {
            txtIdRoom.text = $"{room.roomId}";
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

        // ✅ CẬP NHẬT READY STATUS - CHỈ ĐẾM MEMBERS (KHÔNG TÍNH HOST)
        if (room.members != null)
        {
            int readyCount = 0;
            int totalNonHostMembers = 0;

            foreach (var member in room.members)
            {
                if (!member.host)  // Chỉ đếm members
                {
                    totalNonHostMembers++;
                    if (member.ready) readyCount++;
                }
            }

            // ✅ allReady = true nếu:
            // - Chỉ có host (totalNonHostMembers == 0)
            // - Tất cả members đã ready (readyCount == totalNonHostMembers)
            bool allReady = (totalNonHostMembers == 0) || (readyCount == totalNonHostMembers);

            OnReadyStatusChanged(allReady, readyCount, totalNonHostMembers);

            Debug.Log($"[ManagerRoom] Updated ready status: {readyCount}/{totalNonHostMembers} members ready, allReady={allReady}");
        }
    }

    /// <summary>
    /// ✅ XỬ LÝ KHI JOIN ROOM BỊ LỖI
    /// </summary>
    private void OnRoomJoinError(string errorMessage)
    {
        Debug.LogWarning($"[ManagerRoom] Join error: {errorMessage}");

        if (loading != null)
        {
            loading.SetActive(false);
        }

        ShowJoinError(errorMessage);
    }

    /// <summary>
    /// ✅ XỬ LÝ KHI JOIN ROOM THÀNH CÔNG
    /// </summary>
    private void OnRoomJoinedSuccess(RoomDTO room)
    {
        Debug.Log($"[ManagerRoom] ========================================");
        Debug.Log($"[ManagerRoom] ✓✓ ROOM JOINED SUCCESSFULLY!");
        Debug.Log($"[ManagerRoom] ✓ Room ID: {room.roomId}");
        Debug.Log($"[ManagerRoom] ✓ Enemy Pet ID: {room.enemyPetId}");  // ← CHECK LOG NÀY
        Debug.Log($"[ManagerRoom] ========================================");
        currentUserEnergy = room.energy;
        int currentUserId = PlayerPrefs.GetInt("userId", 0);
        isHost = (room.hostUserId == currentUserId);
        Debug.Log($"[ManagerRoom] User role: {(isHost ? "HOST" : "MEMBER")}");

        // ✅ SETUP UI THEO VAI TRÒ
        if (isHost)
        {
            // Host: Show start button, hide ready button
            if (btnStartBattle != null)
            {
                btnStartBattle.gameObject.SetActive(true);

                // ✅ ĐẾM SỐ MEMBERS (KHÔNG TÍNH HOST)
                int nonHostCount = 0;
                if (room.members != null)
                {
                    foreach (var member in room.members)
                    {
                        if (!member.host) nonHostCount++;
                    }
                }

                // ✅ NẾU KHÔNG CÓ MEMBER NÀO → ENABLE NGAY
                if (nonHostCount == 0)
                {
                    btnStartBattle.interactable = true;
                    allMembersReady = true;
                    Debug.Log("[ManagerRoom] Solo room - start button enabled immediately");
                }
                else
                {
                    btnStartBattle.interactable = false;
                    allMembersReady = false;
                    Debug.Log($"[ManagerRoom] Room has {nonHostCount} members - waiting for all ready");
                }
            }

            if (btnReady != null)
            {
                btnReady.gameObject.SetActive(false);
            }

            // Host tự động ready
            isReady = true;
        }
        else
        {
            // Member: Show ready button, hide start button
            if (btnReady != null)
            {
                btnReady.gameObject.SetActive(true);
                UpdateReadyButtonUI();
            }

            if (btnStartBattle != null)
            {
                btnStartBattle.gameObject.SetActive(false);
            }

            isReady = false;
            allMembersReady = false;
        }
        // ✅ HIDE LOADING
        if (loading != null)
        {
            loading.SetActive(false);
        }

        HideJoinRoomPanel();

        // ✅ LƯU ROOM DATA
        roomData = room;
        currentRoomId = room.roomId;

        // ✅ HIỂN THỊ UI (bao gồm enemyPet)
        DisplayJoinedRoom(room);  // ← CHECK METHOD NÀY CÓ ĐƯỢC GỌI KHÔNG

        // ✅ SHOW ROOM PANEL
        if (roomPanel != null)
        {
            roomPanel.SetActive(true);
        }
        if (room.members != null)
        {
            int readyCount = 0;
            int totalNonHostMembers = 0;

            foreach (var member in room.members)
            {
                if (!member.host)
                {
                    totalNonHostMembers++;
                    if (member.ready) readyCount++;
                }
            }

            bool allReady = (totalNonHostMembers == 0) || (readyCount == totalNonHostMembers);

            OnReadyStatusChanged(allReady, readyCount, totalNonHostMembers);
        }
    }

    /// <summary>
    /// ✅ HIỂN THỊ THÔNG TIN PHÒNG SAU KHI JOIN
    /// </summary>
    private void DisplayJoinedRoom(RoomDTO room)
    {
        Debug.Log($"[ManagerRoom] Displaying room with ID: {room.roomId}");

        // ✅ HIỂN THỊ ROOM ID 5 SỐ
        if (txtIdRoom != null)
        {
            txtIdRoom.text = $"{room.roomId}";
            Debug.Log($"[ManagerRoom] → UI displays: {room.roomId}");
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
        int currentUserId = PlayerPrefs.GetInt("userId", 0);
        int myPetId = room.petId;  // fallback
        foreach (var member in room.members)
        {
            if (member.userId == currentUserId)
            {
                myPetId = member.petId;  // ← LẤY PET CỦA MÌNH!
                break;
            }
        }
        OnPetClicked(myPetId.ToString());
        OnEnemyPet(room.enemyPetId.ToString());

        // Load cards + pets

        if (room.members != null)
        {
            foreach (var member in room.members)
            {
                if (member.userId == currentUserId)
                {
                    if (member.cards != null && member.cards.Count > 0)
                    {
                        availableCards = new List<CardData>(member.cards);
                        Debug.Log($"[ManagerRoom] ✅ Loaded my cards: {availableCards.Count}");
                        DisplayCardsForSelection(availableCards);
                    }

                    if (member.userPets != null && member.userPets.Count > 0)
                    {
                        availablePets = new List<PetUserDTO>(member.userPets);
                        Debug.Log($"[ManagerRoom] ✅ Loaded my pets: {availablePets.Count}");
                        DisplayPetsForSelection(availablePets);
                    }

                    break;
                }
            }
        }

        DisplayRoomMembers(room.members);

        PlayerPrefs.SetInt("userPetId", room.petId);
        PlayerPrefs.SetInt("count", room.count);
        PlayerPrefs.SetInt("requestPass", room.requestPass);
        PlayerPrefs.SetString("BossElementType", room.elementType);
        PlayerPrefs.Save();

        currentRoomId = room.roomId;  // ✅ 5 số
    }

    /// <summary>
    /// ✅ HIỂN THỊ PETS CỦA PLAYER HIỆN TẠI
    /// </summary>
    private void DisplayPetsForSelection(List<PetUserDTO> pets)
    {
        if (petListContainer == null)
        {
            Debug.LogWarning("[ManagerRoom] petListContainer is null");
            return;
        }

        Debug.Log($"[ManagerRoom] Displaying {pets.Count} pets for selection");

        // Clear existing pets
        foreach (Transform child in petListContainer)
        {
            Destroy(child.gameObject);
        }


        foreach (var pet in pets)
        {
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

            // Hiện ngay - không animation
            petUIObject.transform.localScale = Vector3.one;

            Debug.Log($"[ManagerRoom] ✓ Added pet {pet.petId} Lv.{pet.level}");
        }

    }

    /// <summary>
    /// ✅ HIỂN THỊ DANH SÁCH MEMBERS VỚI CARDS
    /// </summary>
    private void DisplayRoomMembers(List<RoomMemberDTO> members)
    {
        if (members == null || memberListContainer == null)
        {
            Debug.LogWarning("[ManagerRoom] members or memberListContainer is null");
            return;
        }

        Debug.Log($"[ManagerRoom] ========== DISPLAYING {members.Count} MEMBERS ==========");

        LeanTween.cancel(memberListContainer.gameObject);

        foreach (Transform child in memberListContainer)
        {
            LeanTween.cancel(child.gameObject);
            Destroy(child.gameObject);
        }

        int currentUserId = PlayerPrefs.GetInt("userId", 0);

        foreach (var member in members)
        {
            // ✅ LOG CARDS CỦA MEMBER NÀY
            Debug.Log($"[ManagerRoom] Member: {member.username} (ID: {member.userId})");
            Debug.Log($"  - Cards available: {(member.cards != null ? member.cards.Count : 0)}");
            Debug.Log($"  - Cards selected: {(member.cardsSelected != null ? member.cardsSelected.Count : 0)}");

            GameObject memberObj = Instantiate(memberUIPrefab, memberListContainer);
            memberObj.transform.localScale = Vector3.one;

            // Setup UI components
            Image imgAvatar = memberObj.transform.Find("imgUser")?.GetComponent<Image>();
            Text txtUsername = memberObj.transform.Find("txtUsername")?.GetComponent<Text>();
            Text txtEnergy = memberObj.transform.Find("txtNl")?.GetComponent<Text>();
            Text txtPass = memberObj.transform.Find("txtPass")?.GetComponent<Text>();
            Image imgLv = memberObj.transform.Find("imgLevel")?.GetComponent<Image>();
            Animator memberAnimator = memberObj.transform.Find("anmtPet")?.GetComponent<Animator>();
            GameObject readyIndicator = memberObj.transform.Find("txtready")?.gameObject;
            GameObject hostBadge = memberObj.transform.Find("key")?.gameObject;
            Transform panelCardUser = memberObj.transform.Find("PanelCardUser");
            Button btnKick = memberObj.transform.Find("btnKick")?.GetComponent<Button>();
            if (imgLv != null)
            {
                SetupImgLevel(member.level, imgLv);
            }

            if (txtEnergy != null)
            {
                txtEnergy.text = member.energy + "/" + member.energyFull;
            }

            if (txtPass != null)
            {
                txtPass.text = member.count + "/" + member.requestPass;
            }

            if (memberAnimator != null)
            {
                LoadPetAnimationForMember(memberAnimator, member.petId);
            }

            if (imgAvatar != null)
            {
                Sprite avatar = Resources.Load<Sprite>("Image/Avt/" + member.avatarId);
                if (avatar != null)
                {
                    imgAvatar.sprite = avatar;
                }
            }

            if (txtUsername != null)
            {
                txtUsername.text = member.username;

                if (member.userId == currentUserId)
                {
                    txtUsername.text = member.username + " (You)";
                }
            }

            if (readyIndicator != null)
            {
                readyIndicator.SetActive(member.ready);
            }

            if (hostBadge != null)
            {
                hostBadge.SetActive(member.host);
            }
            if (btnKick != null)
            {
                // ✅ CHỈ HIỆN NÚT KICK NẾU:
                // 1. User hiện tại là host
                // 2. Member này KHÔNG phải host (không kick chính mình)
                bool showKickButton = isHost && !member.host;

                btnKick.gameObject.SetActive(showKickButton);

                if (showKickButton)
                {
                    // ✅ SETUP CLICK LISTENER
                    btnKick.onClick.RemoveAllListeners();

                    long memberIdToKick = member.userId;
                    string memberNameToKick = member.username;

                    btnKick.onClick.AddListener(() => OnKickButtonClicked(memberIdToKick, memberNameToKick));

                    Debug.Log($"[ManagerRoom] ✓ Kick button enabled for {member.username}");
                }
            }
            else
            {
                Debug.LogWarning($"[ManagerRoom] btnKick not found in member prefab!");
            }
            // ✅ HIỂN THỊ SELECTED CARDS (cards đã chọn)
            if (panelCardUser != null)
            {
                // Hiển thị cardsSelected, KHÔNG phải cards (cards là available)
                DisplayMemberCards(panelCardUser, member.cardsSelected);
            }
        }

        Debug.Log("[ManagerRoom] ========================================");
        if (roomData != null && roomData.enemyPetId > 0)
        {
            Debug.Log($"[ManagerRoom] → Force loading enemy pet: {roomData.enemyPetId}");
            OnEnemyPet(roomData.enemyPetId.ToString());
        }
    }
    /// <summary>
    /// ✅ XỬ LÝ KHI NHẤN NÚT KICK
    /// </summary>
    private void OnKickButtonClicked(long kickedUserId, string kickedUsername)
    {
        Debug.Log($"[ManagerRoom] Kick button clicked: userId={kickedUserId}, username={kickedUsername}");

        // ✅ HIỂN THỊ XÁC NHẬN (optional)
        if (txtJoinError != null)
        {
            txtJoinError.text = $"Bạn có chắc muốn kick {kickedUsername}?";
            txtJoinError.color = Color.yellow;
            txtJoinError.gameObject.SetActive(true);
            StartCoroutine(HideErrorMessageAfterDelay(2f));
        }

        // ✅ GỬI REQUEST KICK
        int hostUserId = PlayerPrefs.GetInt("userId", 0);
        RoomWebSocketManager.Instance.KickMember(currentRoomId, hostUserId, (int)kickedUserId);

        Debug.Log($"[ManagerRoom] ✓ Sent kick request for {kickedUsername}");
    }

    /// <summary>
    /// ✅ HIỂN THỊ SELECTED CARDS TRONG PanelCardUser
    /// </summary>
    private void DisplayMemberCards(Transform panelCardUser, List<CardData> selectedCards)
    {
        if (panelCardUser == null)
        {
            Debug.LogWarning("[ManagerRoom] PanelCardUser is null");
            return;
        }

        // Cancel animations của panel
        LeanTween.cancel(panelCardUser.gameObject);

        // Clear old cards
        foreach (Transform child in panelCardUser)
        {
            LeanTween.cancel(child.gameObject);
            Destroy(child.gameObject);
        }

        if (selectedCards == null || selectedCards.Count == 0)
        {
            Debug.Log("[ManagerRoom] No cards to display");
            return;
        }

        Debug.Log($"[ManagerRoom] Displaying {selectedCards.Count} cards in PanelCardUser");

        // Display cards WITHOUT ANIMATION (hiện ngay)
        for (int i = 0; i < selectedCards.Count; i++)
        {
            CardData card = selectedCards[i];
            GameObject cardObj = null;

            if (cardIconPrefab != null)
            {
                cardObj = Instantiate(cardIconPrefab, panelCardUser);

                Button btn = cardObj.GetComponent<Button>();
                if (btn != null) Destroy(btn);

                SelectedCardUI selectedUI = cardObj.GetComponent<SelectedCardUI>();
                if (selectedUI != null) Destroy(selectedUI);

                Image cardImage = cardObj.GetComponent<Image>();
                if (cardImage == null)
                {
                    cardImage = cardObj.GetComponentInChildren<Image>();
                }

                if (cardImage != null)
                {
                    Sprite cardSprite = Resources.Load<Sprite>($"Image/Card/card{card.cardId}");
                    if (cardSprite != null)
                    {
                        cardImage.sprite = cardSprite;
                    }
                    else
                    {
                        cardImage.color = GetCardColor(card.elementTypeCard);
                    }
                }
            }
            else
            {
                cardObj = new GameObject($"Card_{card.cardId}");
                cardObj.transform.SetParent(panelCardUser, false);

                Image cardImage = cardObj.AddComponent<Image>();
                Sprite cardSprite = Resources.Load<Sprite>($"Image/Card/card{card.cardId}");

                if (cardSprite != null)
                {
                    cardImage.sprite = cardSprite;
                }
                else
                {
                    cardImage.color = GetCardColor(card.elementTypeCard);
                }

                RectTransform rt = cardObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(50, 70);
            }

            // ✅ HIỆN NGAY - KHÔNG ANIMATION
            if (cardObj != null)
            {
                cardObj.transform.localScale = Vector3.one; // Hiện ngay

                int currentUserId = PlayerPrefs.GetInt("userId", 0);
                if (roomData != null && roomData.members != null)
                {
                    foreach (var member in roomData.members)
                    {
                        if (member.userId == currentUserId)
                        {
                            MemberCardUI memberCardUI = cardObj.AddComponent<MemberCardUI>();
                            memberCardUI.Setup(card, toggleManager, member.userId);
                            break;
                        }
                    }
                }
            }
        }

        Debug.Log($"[ManagerRoom] ✓ Done displaying cards");
    }


    /// <summary>
    /// ✅ HELPER: Lấy màu theo elementType
    /// </summary>
    private Color GetCardColor(string elementType)
    {
        if (string.IsNullOrEmpty(elementType)) return Color.gray;

        switch (elementType.ToUpper())
        {
            case "FIRE": return new Color(1f, 0.3f, 0.3f); // Red
            case "WATER": return new Color(0.3f, 0.6f, 1f); // Blue
            case "GRASS": return new Color(0.3f, 1f, 0.3f); // Green
            case "ELECTRIC": return new Color(1f, 1f, 0.3f); // Yellow
            case "ATTACK": return new Color(1f, 0.5f, 0f); // Orange
            case "BUFF": return new Color(0.7f, 0.3f, 1f); // Purple
            default: return Color.cyan;
        }
    }

    /// <summary>
    /// ✅ LOAD PET ANIMATION CHO MEMBER
    /// </summary>
    private void LoadPetAnimationForMember(Animator memberAnimator, int petId)
    {
        if (memberAnimator == null)
        {
            Debug.LogWarning("[ManagerRoom] Member animator is null");
            return;
        }

        Debug.Log($"[ManagerRoom] Loading pet animation for petId={petId}");

        // Load animation clips
        AnimationClip[] clips = Resources.LoadAll<AnimationClip>($"Pets/{petId}");

        if (clips != null && clips.Length > 0)
        {
            // Replace animations
            ReplaceAnimations(memberAnimator, clips);

            // Ensure animator is enabled
            memberAnimator.enabled = true;

            Debug.Log($"[ManagerRoom] ✓ Loaded {clips.Length} animation clips for pet {petId}");
        }
        else
        {
            Debug.LogWarning($"[ManagerRoom] No animation clips found for pet {petId}");

            // Fallback to static image
            Image petImage = memberAnimator.GetComponent<Image>();
            if (petImage != null)
            {
                memberAnimator.enabled = false;
                Sprite petSprite = Resources.Load<Sprite>("Image/IconsPet/" + petId);
                if (petSprite != null)
                {
                    petImage.sprite = petSprite;
                    petImage.enabled = true;
                    Debug.Log($"[ManagerRoom] ✓ Loaded static sprite for pet {petId}");
                }
            }
        }
    }

    /// <summary>
    /// ✅ REPLACE ANIMATIONS - TỔNG QUÁT
    /// </summary>
    void ReplaceAnimations(Animator targetAnimator, AnimationClip[] newClips)
    {
        if (targetAnimator == null)
        {
            Debug.LogWarning("[ManagerRoom] Target animator is null");
            return;
        }

        if (targetAnimator.runtimeAnimatorController == null)
        {
            Debug.LogWarning("[ManagerRoom] Animator has no runtime controller");
            return;
        }

        RuntimeAnimatorController originalController = targetAnimator.runtimeAnimatorController;
        AnimatorOverrideController overrideController = new AnimatorOverrideController(originalController);

        int replacedCount = 0;
        foreach (AnimationClip newClip in newClips)
        {
            foreach (var pair in overrideController.animationClips)
            {
                if (pair.name == newClip.name)
                {
                    overrideController[pair] = newClip;
                    replacedCount++;
                    break;
                }
            }
        }

        targetAnimator.runtimeAnimatorController = overrideController;

        Debug.Log($"[ManagerRoom] Replaced {replacedCount}/{newClips.Length} animation clips");
    }
    public void HideInviteList()
    {
        if (panelInviteList == null) return;

        LeanTween.scale(panelInviteList, Vector3.zero, 0.25f)
            .setEaseInBack()
            .setOnComplete(() => panelInviteList.SetActive(false));
    }

    private void OnOnlineUsersReceived(List<OnlineUserDTO> users)
    {
        Debug.Log($"[ManagerRoom] Received {users.Count} online users");

        onlineUsers = users;
        DisplayOnlineUsers();
    }

    private void DisplayOnlineUsers()
    {
        // Clear old
        foreach (Transform child in inviteListContainer)
        {
            Destroy(child.gameObject);
        }

        // Create new
        foreach (var user in onlineUsers)
        {
            GameObject userObj = Instantiate(userInvitePrefab, inviteListContainer);

            // Setup UI
            Image imgAvatar = userObj.transform.Find("imgAvatar")?.GetComponent<Image>();
            Text txtUsername = userObj.transform.Find("txtUsername")?.GetComponent<Text>();
            Image imgLevel = userObj.transform.Find("imgLevel")?.GetComponent<Image>();
            Button btnInvite = userObj.transform.Find("btnInvite")?.GetComponent<Button>();

            if (imgAvatar != null)
            {
                Sprite avatar = Resources.Load<Sprite>("Image/Avt/" + user.avatarId);
                if (avatar != null) imgAvatar.sprite = avatar;
            }

            if (txtUsername != null)
            {
                txtUsername.text = user.username;
            }

            SetupImgLevel(user.level, imgLevel);

            if (btnInvite != null)
            {
                btnInvite.onClick.AddListener(() => InviteUser(user));
            }

            // Animation
            userObj.transform.localScale = Vector3.zero;
            int index = inviteListContainer.childCount - 1;
            LeanTween.scale(userObj, Vector3.one, 0.3f)
                .setEaseOutBack()
                .setDelay(index * 0.05f);
        }

        UpdateInviteCount();
    }

    /// <summary>
    /// ✅ SEND INVITE (LONG ROOM ID)
    /// </summary>
    private void InviteUser(OnlineUserDTO user)
    {
        Debug.Log($"[ManagerRoom] Inviting {user.username}...");

        int userId = PlayerPrefs.GetInt("userId", 0);
        string username = PlayerPrefs.GetString("Username", "Player");

        // ✅ currentRoomId là long
        RoomWebSocketManager.Instance.SendInvite(
            currentRoomId,
            userId,
            username,
            user.userId
        );

        Debug.Log($"[ManagerRoom] Invite sent to {user.username}!");
    }

    private void OnInviteReceived(RoomInviteDTO invite)
    {
        Debug.Log($"[ManagerRoom] Received invite from {invite.fromUsername}");

        // Show popup
        ShowInvitePopup(invite);
    }

    private void OnInviteResponseReceived(RoomInviteDTO response)
    {
        Debug.Log($"[ManagerRoom] Invite response: {response.status}");

        if (response.status == "ACCEPTED")
        {
            Debug.Log("User accepted invite!");
            // TODO: Add user to room
        }
        else if (response.status == "DECLINED")
        {
            Debug.Log("User declined invite");
        }
    }

    public void ShowInvitePanel()
    {
        if (panelInviteList == null) return;

        panelInviteList.SetActive(true);

        // Request online users
        int userId = PlayerPrefs.GetInt("userId", 0);
        RoomWebSocketManager.Instance.RequestOnlineUsers(userId);

        // Animation
        panelInviteList.transform.localScale = Vector3.zero;
        LeanTween.scale(panelInviteList, Vector3.one, 0.4f)
            .setEaseOutBack();
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

        bool allRequestsCompleted = false;
        int completedRequests = 0;
        int totalRequests = 3;

        // Load room
        yield return APIManager.Instance.GetRequest<RoomDTO>(
            APIConfig.GET_ROOM_USERS(userId, selectedPetId),
            (room) =>
            {
                OnRoomReceived(room);
                completedRequests++;
                allRequestsCompleted = true;

                // ✅ TẠO WEBSOCKET ROOM - Chờ response để có roomId
                CreateWebSocketRoom(room);
            },
            OnError
        );

        // Đợi tất cả load xong
        while (!allRequestsCompleted)
        {
            yield return null;
        }

        Debug.Log("[ManagerRoom] ✓✓ All data loaded, waiting for socket response...");

        // ✅ KHÔNG hide loading và show room panel ở đây nữa!
        // Chờ OnRoomJoinedSuccess() để hiển thị
    }

    private void CreateWebSocketRoom(RoomDTO room)
    {
        Debug.Log($"[ManagerRoom] Creating WebSocket room...");

        room.hostUserId = PlayerPrefs.GetInt("userId", 1);
        room.hostUsername = PlayerPrefs.GetString("Username", "Player");

        // Gửi lên server
        RoomWebSocketManager.Instance.CreateRoom(room);

        Debug.Log("[ManagerRoom] → Waiting for server to create roomId...");
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

    /// <summary>
    /// ✅ CẬP NHẬT CloseRoomPanel() - GỬI LEAVE REQUEST
    /// </summary>
    public void CloseRoomPanel()
    {
        Debug.Log("[ManagerRoom] Host closing room panel...");

        // ✅ GỬI LEAVE REQUEST LÊN SERVER (server sẽ tự động kick tất cả)
        int userId = PlayerPrefs.GetInt("userId", 1);
        RoomWebSocketManager.Instance.LeaveRoom(userId);

        // ✅ CHỜ SERVER GỬI ROOM_CLOSED MESSAGE
        // Không cần tự đóng panel ở đây, sẽ đóng khi nhận OnRoomClosed()
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

        // ✅ HIỂN THỊ AVAILABLE PETS (từ availablePets)
        if (availablePets != null && availablePets.Count > 0)
        {
            Debug.Log($"[ManagerRoom] Opening pet selection panel with {availablePets.Count} available pets");
            DisplayPetsForSelection(availablePets);
        }
        else
        {
            Debug.LogError("[ManagerRoom] No available pets to display!");
        }

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

    public void HideInvitePanel()
    {
        if (panelInvite == null) return;
        LeanTween.scale(panelInvite, Vector3.zero, 0.25f)
            .setEaseInBack()
            .setOnComplete(() => panelInvite.SetActive(false));
    }

    /// <summary>
    /// ✅ MỞ PANEL CHỌN CARD - HIỂN THỊ AVAILABLE CARDS
    /// </summary>
    public void ShowCardPanel()
    {
        if (panelCard == null) return;

        panelCard.SetActive(true);
        isRotatingCard = true;

        // ✅ HIỂN THỊ AVAILABLE CARDS (từ availableCards, KHÔNG phải roomData.cards)
        if (availableCards != null && availableCards.Count > 0)
        {
            Debug.Log($"[ManagerRoom] Opening card selection panel with {availableCards.Count} available cards");
            DisplayCardsForSelection(availableCards);
        }
        else
        {
            Debug.LogError("[ManagerRoom] No available cards to display!");
        }

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
        Debug.Log($"[ManagerRoom] Received {pets.Count} pets from API");

        // ✅ KIỂM TRA: Nếu đã có pets từ room → skip
        if (availablePets != null && availablePets.Count > 0)
        {
            Debug.Log("[ManagerRoom] → Already have pets from room, skipping API pets");

            // Chỉ load enemy pet
            int selectedPetId = PlayerPrefs.GetInt("SelectedPetId", 1);
            OnEnemyPet(selectedPetId.ToString());

            return;
        }

        // ✅ Nếu chưa có → dùng pets từ API
        availablePets = pets;
        DisplayPetsForSelection(pets);
    }

    /// <summary>
    /// ✅ LOAD ROOM DATA - LƯU AVAILABLE CARDS
    /// </summary>
    void OnRoomReceived(RoomDTO room)
    {
        Debug.Log("[ManagerRoom] Room data received from API");
        roomData = room;

        // ✅ KHÔNG set roomId = id
        // ✅ KHÔNG hiển thị UI ở đây
        // Chờ socket response

        // Load data của chính mình
        int userId = PlayerPrefs.GetInt("userId", 1);

        if (room.members != null)
        {
            foreach (var member in room.members)
            {
                if (member.userId == userId)
                {
                    if (member.cards != null && member.cards.Count > 0)
                    {
                        availableCards = new List<CardData>(member.cards);
                        Debug.Log($"[ManagerRoom] ✅ Loaded MY cards: {availableCards.Count}");
                    }

                    if (member.userPets != null && member.userPets.Count > 0)
                    {
                        availablePets = new List<PetUserDTO>(member.userPets);
                        Debug.Log($"[ManagerRoom] ✅ Loaded MY pets: {availablePets.Count}");
                    }

                    break;
                }
            }
        }

        // Reset selected cards
        if (room.members != null)
        {
            foreach (var member in room.members)
            {
                member.cardsSelected = new List<CardData>();
            }
        }

        PlayerPrefs.SetInt("userPetId", room.petId);
        PlayerPrefs.SetInt("count", room.count);
        PlayerPrefs.SetInt("requestPass", room.requestPass);
        PlayerPrefs.SetString("BossElementType", room.elementType);
        PlayerPrefs.Save();

        Debug.Log("[ManagerRoom] → API data saved, waiting for socket roomId...");
    }


    /// <summary>
    /// ✅ MỞ PANEL NHẬP ROOM ID
    /// </summary>
    public void ShowJoinRoomPanel()
    {
        if (panelJoinRoom == null) return;

        panelJoinRoom.SetActive(true);

        if (inputRoomId != null)
        {
            inputRoomId.text = "";
        }

        if (txtJoinError != null)
        {
            txtJoinError.text = "";
            txtJoinError.gameObject.SetActive(false);
        }

        panelJoinRoom.transform.localScale = Vector3.zero;
        LeanTween.scale(panelJoinRoom, Vector3.one, 0.4f)
            .setEaseOutBack();
    }
    /// <summary>
    /// ✅ JOIN ROOM BY ID (5 SỐ)
    /// </summary>
    private void OnJoinRoomClicked()
    {
        if (inputRoomId == null || string.IsNullOrEmpty(inputRoomId.text.Trim()))
        {
            ShowJoinError("Vui lòng nhập Room ID!");
            return;
        }

        // ✅ Lấy Room ID
        string roomIdText = inputRoomId.text.Trim();

        // ✅ VALIDATE: phải có đúng 5 ký tự
        if (roomIdText.Length != 5)
        {
            ShowJoinError("Room ID phải có 5 số!");
            return;
        }

        // ✅ VALIDATE: chỉ chấp nhận số (0-9)
        if (!System.Text.RegularExpressions.Regex.IsMatch(roomIdText, @"^\d{5}$"))
        {
            ShowJoinError("Room ID chỉ được chứa số!");
            return;
        }

        // ✅ Parse sang long
        long roomId;
        if (!long.TryParse(roomIdText, out roomId))
        {
            ShowJoinError("Room ID không hợp lệ!");
            return;
        }

        Debug.Log($"[ManagerRoom] Joining room {roomId}...");

        // Show loading
        if (loading != null)
        {
            loading.SetActive(true);
        }

        // ✅ Load user data và join
        StartCoroutine(LoadUserRoomDataAndJoin(roomId));
    }
    private IEnumerator LoadUserRoomDataAndJoin(long targetRoomId)
    {
        int userId = PlayerPrefs.GetInt("userId", 1);
        int selectedPetId = PlayerPrefs.GetInt("SelectedPetId", 1);

        RoomDTO userRoomData = null;
        bool dataLoaded = false;

        // Load room data của chính user
        yield return APIManager.Instance.GetRequest<RoomDTO>(
            APIConfig.GET_ROOM_USERS(userId, selectedPetId),
            (room) =>
            {
                userRoomData = room;
                dataLoaded = true;
                Debug.Log("[ManagerRoom] ✓ User room data loaded for joining");
            },
            (error) =>
            {
                Debug.LogError($"[ManagerRoom] Failed to load user data: {error}");
                if (loading != null)
                {
                    loading.SetActive(false);
                }
                ShowJoinError("Không thể tải thông tin của bạn!");
            }
        );

        // Đợi load xong
        while (!dataLoaded && userRoomData == null)
        {
            yield return null;
        }

        if (userRoomData == null)
        {
            yield break;
        }

        // ✅ Gọi WebSocket để join room với STRING roomId
        RoomWebSocketManager.Instance.JoinRoomByIdWithFullInfo(targetRoomId, userRoomData);
    }

    /// <summary>
    /// ✅ GỌI API ĐỂ JOIN ROOM
    /// </summary>
    private IEnumerator JoinRoomById(long roomId)
    {
        if (loading != null)
        {
            loading.SetActive(true);
        }

        int userId = PlayerPrefs.GetInt("userId", 1);

        // ✅ OPTION 1: Nếu có API join room
        yield return APIManager.Instance.GetRequest<RoomDTO>(
            APIConfig.JOIN_ROOM(roomId, userId), // Bạn cần tạo endpoint này
            (room) =>
            {
                OnRoomJoined(room);
            },
            (error) =>
            {
                ShowJoinError("Không tìm thấy phòng hoặc phòng đã đầy!");
                if (loading != null)
                {
                    loading.SetActive(false);
                }
            }
        );

        // ✅ OPTION 2: Nếu dùng WebSocket
        // RoomWebSocketManager.Instance.JoinRoom(roomId, userId);
    }

    /// <summary>
    /// ✅ XỬ LÝ KHI JOIN ROOM THÀNH CÔNG
    /// </summary>
    private void OnRoomJoined(RoomDTO room)
    {
        Debug.Log($"[ManagerRoom] ✓ Joined room {room.id} successfully!");

        if (loading != null)
        {
            loading.SetActive(false);
        }

        HideJoinRoomPanel();

        // Load room data
        OnRoomReceived(room);

        // Show room panel
        if (roomPanel != null)
        {
            roomPanel.SetActive(true);
        }
    }

    /// <summary>
    /// ✅ HIỂN THỊ LỖI JOIN
    /// </summary>
    private void ShowJoinError(string message)
    {
        if (txtJoinError != null)
        {
            txtJoinError.text = message;
            txtJoinError.gameObject.SetActive(true);
            StartCoroutine(HideJoinErrorAfterDelay(3f));
        }
    }

    private IEnumerator HideJoinErrorAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (txtJoinError != null)
        {
            txtJoinError.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// ✅ COPY ROOM ID VÀO CLIPBOARD (để share)
    /// </summary>
    public void CopyRoomIdToClipboard()
    {
        if (currentRoomId <= 0)
        {
            Debug.LogWarning("[ManagerRoom] No room ID to copy!");
            return;
        }

        GUIUtility.systemCopyBuffer = currentRoomId.ToString();
        Debug.Log($"[ManagerRoom] ✓ Copied Room ID: {currentRoomId}");

        // Show feedback
        if (txtJoinError != null)
        {
            txtJoinError.text = $"✓ Đã copy Room ID: {currentRoomId}";
            txtJoinError.color = Color.green;
            txtJoinError.gameObject.SetActive(true);
            StartCoroutine(HideJoinErrorAfterDelay(2f));
        }
    }

    /// <summary>
    /// ✅ SHOW TOAST MESSAGE (optional helper)
    /// </summary>
    private void ShowToast(string message)
    {
        // Implement toast UI nếu cần
        Debug.Log($"[Toast] {message}");
    }
    /// <summary>
    /// ✅ ẨN PANEL JOIN ROOM
    /// </summary>
    public void HideJoinRoomPanel()
    {
        if (panelJoinRoom == null) return;

        LeanTween.scale(panelJoinRoom, Vector3.zero, 0.25f)
            .setEaseInBack()
            .setOnComplete(() => panelJoinRoom.SetActive(false));
    }

    /// <summary>
    /// ✅ ĐỔ AVAILABLE CARDS VÀO PANEL CHỌN
    /// </summary>
    void DisplayCardsForSelection(List<CardData> availableCards)
    {
        if (toggleManager == null || toggleManager.listToggle == null)
        {
            Debug.LogError("[ManagerRoom] ToggleManager or listToggle is null!");
            return;
        }

        Debug.Log($"[ManagerRoom] Displaying {availableCards.Count} available cards for selection");

        // ✅ CANCEL ANIMATIONS CŨ
        LeanTween.cancel(toggleManager.listToggle);
        foreach (Transform child in toggleManager.listToggle.transform)
        {
            LeanTween.cancel(child.gameObject);
        }

        // Clear old toggles
        toggleManager.ClearAllToggles();

        // Create new toggles WITHOUT ANIMATION
        for (int i = 0; i < availableCards.Count; i++)
        {
            CardData card = availableCards[i];
            GameObject toggleObj = CreateCardToggle(card, i);

            if (toggleObj != null)
            {
                toggleObj.transform.SetParent(toggleManager.listToggle.transform, false);

                // ✅ HIỆN NGAY - KHÔNG ANIMATION
                toggleObj.transform.localScale = Vector3.one;
            }
        }

        Debug.Log($"[ManagerRoom] ✓ {availableCards.Count} cards ready for selection");
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
        checkImage.color = Color.white;
        checkObj.SetActive(false);

        toggle.targetGraphic = bgImage;
        toggle.graphic = checkImage;

        // Gắn CardData
        CardToggleData toggleData = toggleObj.AddComponent<CardToggleData>();
        toggleData.cardData = card;

        return toggleObj;
    }

    void SetupImgLevel(int level, Image imgLvUser)
    {
        // ✅ CHECK NULL TRƯỚC KHI SỬ DỤNG
        if (imgLvUser == null)
        {
            Debug.LogWarning("[ManagerRoom] imgLvUser is null, skipping SetupImgLevel");
            return;
        }

        // Load sprite theo level
        Sprite levelSprite = Resources.Load<Sprite>("Image/hclv/level " + level);

        if (levelSprite != null)
        {
            imgLvUser.sprite = levelSprite;
        }
        else
        {
            Debug.LogWarning($"[ManagerRoom] Level sprite not found for level {level}");
        }

        // ✅ CHECK NULL TRƯỚC KHI GET COMPONENT
        RectTransform rectTransform = imgLvUser.GetComponent<RectTransform>();

        if (rectTransform == null)
        {
            Debug.LogWarning("[ManagerRoom] RectTransform is null");
            return;
        }

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

        SetupImgLevel(user.lever, imgLvRoom);
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
    /// ✅ XÁC NHẬN CHỌN CARDS - CHỈ ĐÓNG PANEL VÀ GỬI WEBSOCKET
    /// </summary>
    public void OnStartBattle()
    {
        Debug.Log("[ManagerRoom] Confirming card selection (closing panel)...");

        if (selectedCards == null || selectedCards.Count == 0)
        {
            Debug.LogWarning("[ManagerRoom] No cards selected!");
            return;
        }

        // ✅ LƯU VÀO PLAYERPREFS
        CardListWrapper wrapper = new CardListWrapper { cards = selectedCards };
        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString("SelectedCards", json);
        PlayerPrefs.Save();

        Debug.Log($"[ManagerRoom] ✓ Confirmed {selectedCards.Count} cards");

        // ✅ GỬI UPDATE LÊN WEBSOCKET
        if (currentRoomId > 0)
        {
            RoomWebSocketManager.Instance.UpdateRoomCards(currentRoomId, selectedCards);
        }

        // ✅ ĐÓNG PANEL
        HideCardPanel();
    }
    /// <summary>
    /// ✅ CẬP NHẬT CHỈ CARDS CỦA 1 MEMBER (không refresh toàn bộ)
    /// </summary>
    private void UpdateMemberCardsUI(RoomMemberDTO member)
    {
        if (memberListContainer == null)
        {
            Debug.LogWarning("[ManagerRoom] memberListContainer is null");
            return;
        }

        Debug.Log($"[ManagerRoom] Updating cards UI for {member.username}...");

        // ✅ TÌM PREFAB CỦA MEMBER NÀY
        foreach (Transform child in memberListContainer)
        {
            // Tìm theo username
            Text txtUsername = child.Find("txtUsername")?.GetComponent<Text>();

            if (txtUsername != null && (txtUsername.text == member.username || txtUsername.text == member.username + " (You)"))
            {
                // ✅ TÌM PanelCardUser
                Transform panelCardUser = child.Find("PanelCardUser");

                if (panelCardUser != null)
                {
                    Debug.Log($"[ManagerRoom] ✓ Found PanelCardUser for {member.username}");

                    // ✅ XÓA CARDS CŨ VÀ HIỂN THỊ MỚI
                    DisplayMemberCards(panelCardUser, member.cards);
                }
                else
                {
                    Debug.LogError($"[ManagerRoom] ✗ PanelCardUser not found in prefab {child.name}");

                    // ✅ DEBUG: List tất cả children
                    Debug.Log($"[ManagerRoom] Available children in {child.name}:");
                    foreach (Transform subChild in child)
                    {
                        Debug.Log($"  - {subChild.name}");
                    }
                }

                break;
            }
        }
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

    public Animator animator;
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
        int newPetId = int.Parse(petId);

        PlayerPrefs.SetInt("userPetId", newPetId);
        PlayerPrefs.Save();

        // ✅ CẬP NHẬT ANIMATION CHO PET LỚN (nếu có)
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

        // ✅ CẬP NHẬT PET TRONG ROOMDATA
        if (roomData != null)
        {
            roomData.petId = newPetId;

            // ✅ CẬP NHẬT PET TRONG MEMBER CỦA CHÍNH MÌNH
            int currentUserId = PlayerPrefs.GetInt("userId", 0);
            if (roomData.members != null)
            {
                foreach (var member in roomData.members)
                {
                    if (member.userId == currentUserId)
                    {
                        member.petId = newPetId;
                        Debug.Log($"[ManagerRoom] ✓ Updated local member pet to {newPetId}");
                        break;
                    }
                }
            }

            // ❌ KHÔNG GỌI DisplayRoomMembers() NỮA - CHỈ UPDATE PET ANIMATOR
            UpdateMyPetInMemberList(currentUserId, newPetId);
        }


        // ✅ GỬI UPDATE LÊN WEBSOCKET (nếu đang trong room với nhiều người)
        if (currentRoomId > 0)
        {
            Debug.Log($"[ManagerRoom] → Sending pet update to server: {newPetId}");
            RoomWebSocketManager.Instance.UpdateRoomPet(currentRoomId, newPetId);
        }
    }
    /// <summary>
    /// ✅ CẬP NHẬT CHỈ PET ANIMATOR CỦA MÌNH TRONG MEMBER LIST (KHÔNG REFRESH TẤT CẢ)
    /// </summary>
    private void UpdateMyPetInMemberList(int userId, int petId)
    {
        if (memberListContainer == null)
        {
            Debug.LogWarning("[ManagerRoom] memberListContainer is null");
            return;
        }

        Debug.Log($"[ManagerRoom] → Updating pet animator for userId={userId}, petId={petId}");

        // ✅ TÌM PREFAB CỦA MEMBER HIỆN TẠI
        foreach (Transform child in memberListContainer)
        {
            RoomMemberDTO memberData = null;

            if (roomData != null && roomData.members != null)
            {
                // Tìm member data
                foreach (var member in roomData.members)
                {
                    if (member.userId == userId)
                    {
                        memberData = member;
                        break;
                    }
                }
            }

            if (memberData != null)
            {
                // Kiểm tra tên username
                Text txtUsername = child.Find("txtUsername")?.GetComponent<Text>();

                if (txtUsername != null)
                {
                    string displayName = txtUsername.text.Replace(" (You)", "").Trim();

                    if (displayName == memberData.username)
                    {
                        // ✅ TÌM VÀ CẬP NHẬT PET ANIMATOR
                        Animator memberAnimator = child.Find("anmtPet")?.GetComponent<Animator>();

                        if (memberAnimator != null)
                        {
                            LoadPetAnimationForMember(memberAnimator, petId);
                            Debug.Log($"[ManagerRoom] ✓ Updated pet animator for {memberData.username}: petId={petId}");
                        }
                        else
                        {
                            Debug.LogWarning($"[ManagerRoom] Pet animator not found for {memberData.username}");
                        }

                        break;
                    }
                }
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

    /// <summary>
    /// ✅ UNSUBSCRIBE TRONG OnDestroy()
    /// </summary>
    private void OnDestroy()
    {
        // Cancel ALL LeanTween animations
        LeanTween.cancelAll();

        // Unsubscribe events
        if (toggleManager != null)
        {
            toggleManager.OnCardsChanged -= OnCardsChangedInToggle;
        }

        if (RoomWebSocketManager.Instance != null)
        {
            RoomWebSocketManager.Instance.OnOnlineUsersUpdated -= OnOnlineUsersReceived;
            RoomWebSocketManager.Instance.OnInviteReceived -= OnInviteReceived;
            RoomWebSocketManager.Instance.OnInviteResponseReceived -= OnInviteResponseReceived;
            RoomWebSocketManager.Instance.OnRoomJoined -= OnRoomJoinedSuccess;
            RoomWebSocketManager.Instance.OnJoinError -= OnRoomJoinError;
            RoomWebSocketManager.Instance.OnRoomUpdated -= OnRoomUpdateReceived;
            RoomWebSocketManager.Instance.OnPetUpdated -= OnPetUpdatedFromServer;
            RoomWebSocketManager.Instance.OnCardsUpdated -= OnCardsUpdatedFromServer;
            RoomWebSocketManager.Instance.OnRoomClosed -= OnRoomClosed;
            RoomWebSocketManager.Instance.OnRoomLeft -= OnRoomLeft;
            RoomWebSocketManager.Instance.OnReadyStatusChanged -= OnReadyStatusChanged;

            // ✅ UNSUBSCRIBE READY UPDATE
            RoomWebSocketManager.Instance.OnMemberReadyChanged -= OnMemberReadyChanged;
            RoomWebSocketManager.Instance.OnKicked -= OnKicked;
        }
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