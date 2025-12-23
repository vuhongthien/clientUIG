using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

public class ChatManager : MonoBehaviour
{
    [Header("Chat UI")]
    public GameObject chatPanel;
    public Button btnToggleChat;
    public InputField inputMessage;
    public Button btnSend;
    public Transform chatContent;
    public GameObject messagePrefab;
    public ScrollRect scrollRect;

    // ✅ THÊM REFERENCE CHO CONTAINER
    public RectTransform chatContainer;

    [Header("Animation")]
    public float animationDuration = 0.5f;
    public LeanTweenType easeType = LeanTweenType.easeOutCubic;

    // ✅ VỊ TRÍ CLOSED/OPEN
    private Vector2 closedPosition;  // Vị trí khi đóng (ngoài màn hình)
    private Vector2 openPosition;    // Vị trí khi mở (trong màn hình)

    [Header("Connection")]
    public Text txtConnectionStatus;
    public Image imgConnectionIndicator;
    public Color connectedColor = Color.green;
    public Color disconnectedColor = Color.red;

    [Header("Settings")]
    public int maxMessages = 50;
    public string webSocketUrl = "ws://localhost:8080/ws-chat";

    // Private variables
    private WebSocket webSocket;
    private bool isConnected = false;
    private Queue<ChatMessageDTO> messageQueue = new Queue<ChatMessageDTO>();
    private int userId;
    private string username;
    private bool isChatOpen = true;
    public static ChatManager Instance { get; private set; }
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Nếu muốn giữ qua scene
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        userId = PlayerPrefs.GetInt("userId", 0);
        username = ManagerQuangTruong.Instance.txtName.text;

        if (userId == 0)
        {
            Debug.LogError("[ChatManager] User ID not found!");
            return;
        }

        // ✅ SETUP VỊ TRÍ CLOSED/OPEN
        SetupPositions();

        if (chatPanel != null)
            chatPanel.SetActive(true); // ✅ LUÔN ACTIVE, chỉ di chuyển container

        // Setup button listeners
        if (btnToggleChat != null)
            btnToggleChat.onClick.AddListener(ToggleChat);

        if (btnSend != null)
            btnSend.onClick.AddListener(SendMessage);

        // Setup input field
        if (inputMessage != null)
        {
            inputMessage.onEndEdit.AddListener(delegate
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    SendMessage();
            });
        }

    }

    /// <summary>
    /// ✅ SETUP VỊ TRÍ CLOSED (ngoài màn hình) và OPEN (trong màn hình)
    /// </summary>
    void SetupPositions()
    {
        if (chatContainer == null)
        {
            Debug.LogError("[ChatManager] chatContainer is null!");
            return;
        }

        // ✅ Lấy vị trí Y hiện tại
        float currentY = chatContainer.anchoredPosition.y;

        // ✅ VỊ TRÍ MỞ (Left = 0)
        openPosition = new Vector2(-319.58f, currentY);

        // ✅ VỊ TRÍ ĐÓNG (Left = -429.11 - ẩn bên trái)
        closedPosition = new Vector2(-748.11f, currentY);

        Debug.Log($"[ChatManager] Open: {openPosition}, Closed: {closedPosition}");
    }


    /// <summary>
    /// ✅ TOGGLE CHAT với ANIMATION
    /// </summary>
    public void ToggleChat()
    {
        isChatOpen = !isChatOpen;

        if (isChatOpen)
        {
            OpenChat();
        }
        else
        {
            CloseChat();
        }
    }

    /// <summary>
    /// ✅ MỞ CHAT - Animation từ PHẢI vào TRÁI
    /// </summary>
    void OpenChat()
    {
        Debug.Log("[ChatManager] 📂 Opening chat...");

        if (chatContainer == null) return;

        // ✅ ANIMATION: Di chuyển từ closedPosition → openPosition
        LeanTween.cancel(chatContainer.gameObject);

        LeanTween.value(chatContainer.gameObject, UpdateChatPosition, closedPosition, openPosition, animationDuration)
            .setEase(easeType)
            .setOnComplete(() =>
            {
                Debug.Log("[ChatManager] ✅ Chat opened");
            });
    }

    /// <summary>
    /// ✅ ĐÓNG CHAT - Animation từ TRÁI ra PHẢI
    /// </summary>
    void CloseChat()
    {
        Debug.Log("[ChatManager] 📁 Closing chat...");

        if (chatContainer == null) return;

        // ✅ ANIMATION: Di chuyển từ openPosition → closedPosition
        LeanTween.cancel(chatContainer.gameObject);

        LeanTween.value(chatContainer.gameObject, UpdateChatPosition, openPosition, closedPosition, animationDuration)
            .setEase(easeType)
            .setOnComplete(() =>
            {
                Debug.Log("[ChatManager] ✅ Chat closed");
            });
    }

    /// <summary>
    /// ✅ UPDATE POSITION trong animation
    /// </summary>
    void UpdateChatPosition(Vector2 position)
    {
        if (chatContainer != null)
        {
            chatContainer.anchoredPosition = position;
        }
    }

    public void ConnectWebSocket(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogError("[ChatManager] Username not loaded yet!" + name);
            UpdateConnectionStatus("Please wait...", disconnectedColor);
            return;
        }
        username = name;
        Debug.Log($"[ChatManager] 🔌 Connecting to: {webSocketUrl}");
        UpdateConnectionStatus("Connecting...", disconnectedColor);

        webSocket = new WebSocket(webSocketUrl);

        webSocket.OnOpen += OnWebSocketOpen;
        webSocket.OnMessage += OnWebSocketMessage;
        webSocket.OnError += OnWebSocketError;
        webSocket.OnClose += OnWebSocketClose;

        webSocket.Connect();
    }

    private void OnWebSocketOpen(object sender, EventArgs e)
    {
        Debug.Log("[ChatManager] ✅ WebSocket connected!");
        isConnected = true;

        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            UpdateConnectionStatus("Connected", connectedColor);

            if (inputMessage != null)
                inputMessage.interactable = true;
            if (btnSend != null)
                btnSend.interactable = true;
        });

        var joinMessage = new ChatMessageDTO
        {
            userId = userId,
            username = username,
            type = "JOIN"
        };

        SendWebSocketMessage(joinMessage);
    }

    private void OnWebSocketMessage(object sender, MessageEventArgs e)
    {
        string json = e.Data;
        Debug.Log($"[ChatManager] 📨 Message received: {json}");

        try
        {
            ChatMessageDTO message = JsonUtility.FromJson<ChatMessageDTO>(json);

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                messageQueue.Enqueue(message);
            });
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ChatManager] ❌ Parse error: {ex.Message}");
        }
    }

    private void OnWebSocketError(object sender, ErrorEventArgs e)
    {
        Debug.LogError($"[ChatManager] ❌ WebSocket error: {e.Message}");

        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            UpdateConnectionStatus("Error", disconnectedColor);
        });
    }

    private void OnWebSocketClose(object sender, CloseEventArgs e)
    {
        Debug.Log($"[ChatManager] 🔌 WebSocket closed: {e.Code} - {e.Reason}");
        isConnected = false;

        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            UpdateConnectionStatus("Disconnected", disconnectedColor);

            if (inputMessage != null)
                inputMessage.interactable = false;
            if (btnSend != null)
                btnSend.interactable = false;
        });
    }

    private void Update()
    {
        while (messageQueue.Count > 0)
        {
            ChatMessageDTO message = messageQueue.Dequeue();
            DisplayMessage(message);
        }
    }

    public void SendMessage()
    {
        if (!isConnected)
        {
            Debug.LogWarning("[ChatManager] ⚠️ Not connected!");
            return;
        }

        if (inputMessage == null)
            return;

        string messageText = inputMessage.text.Trim();

        if (string.IsNullOrEmpty(messageText))
            return;

        var chatMessage = new ChatMessageDTO
        {
            userId = userId,
            username = username,
            message = messageText,
            type = "CHAT"
        };

        SendWebSocketMessage(chatMessage);

        inputMessage.text = "";
        inputMessage.ActivateInputField();
    }

    void SendWebSocketMessage(ChatMessageDTO messageObj)
    {
        if (webSocket == null || !webSocket.IsAlive)
        {
            Debug.LogWarning("[ChatManager] ⚠️ WebSocket not connected!");
            return;
        }

        string json = JsonUtility.ToJson(messageObj);
        webSocket.Send(json);

        Debug.Log($"[ChatManager] 📤 Message sent: {json}");
    }

    void DisplayMessage(ChatMessageDTO message)
{
    if (messagePrefab == null || chatContent == null)
    {
        Debug.LogError("[ChatManager] ❌ MessagePrefab or ChatContent is null!");
        return;
    }

    // ✅ BỎ QUA JOIN/LEAVE - KHÔNG HIỆN GÌ CẢ
    if (message.type == "JOIN" || message.type == "LEAVE")
    {
        Debug.Log($"[ChatManager] 🚫 Ignoring {message.type} message from {message.username}");
        return; // ✅ THOÁT NGAY, KHÔNG TẠO UI
    }

    // ✅ CHỈ HIỆN NORMAL MESSAGE
    GameObject messageObj = Instantiate(messagePrefab, chatContent);
    messageObj.SetActive(true);

    Text txtUsername = messageObj.transform.Find("txtUsername")?.GetComponent<Text>();
    Text txtMessage = messageObj.transform.Find("txtMessage")?.GetComponent<Text>();
    Text txtTime = messageObj.transform.Find("txtTime")?.GetComponent<Text>();

    if (txtUsername != null)
        txtUsername.text = message.username + ":";
        
    if (txtMessage != null)
        txtMessage.text = message.message;
        
    if (txtTime != null && !string.IsNullOrEmpty(message.timestamp))
    {
        try
        {
            DateTime dt = DateTime.Parse(message.timestamp);
            txtTime.text = dt.ToString("HH:mm");
        }
        catch
        {
            txtTime.text = "";
        }
    }

    // Cleanup old messages
    if (chatContent.childCount > maxMessages)
    {
        Destroy(chatContent.GetChild(0).gameObject);
    }

    Canvas.ForceUpdateCanvases();
    LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent.GetComponent<RectTransform>());
    StartCoroutine(ScrollToBottom());
}

    IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();

        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
            Canvas.ForceUpdateCanvases();
        }
    }

    void UpdateConnectionStatus(string text, Color color)
    {
        if (txtConnectionStatus != null)
            txtConnectionStatus.text = text;
        if (imgConnectionIndicator != null)
            imgConnectionIndicator.color = color;
    }

    private void OnDestroy()
    {
        if (webSocket != null && webSocket.IsAlive)
        {
            webSocket.Close();
        }
    }

    private void OnApplicationQuit()
    {
        if (webSocket != null && webSocket.IsAlive)
        {
            webSocket.Close();
        }
    }
}

[System.Serializable]
public class ChatMessageDTO
{
    public int userId;
    public string username;
    public string message;
    public string timestamp;
    public string type;
}