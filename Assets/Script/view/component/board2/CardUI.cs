using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Component gắn vào Card prefab
/// Quản lý việc sử dụng card với Dot Skill mini-game
/// </summary>
public class CardUI : MonoBehaviour
{
    private CardData cardData;
    private Button btn;
    private Board board;
    private Active active;

    // ✅ Flags quản lý trạng thái
    private bool hasUsedThisMatch = false;
    private bool hasUsedThisTurn = false;
    private int lastTurnUsed = -1;

    // ✅ STATIC: Theo dõi việc sử dụng buff trong turn hiện tại
    private static int lastBuffUsedTurn = -1;
    private static bool hasUsedBuffThisTurn = false;

    // ✅ Placeholder
    [Header("Placeholder Settings")]
    [Tooltip("Sprite hiển thị khi card đã được sử dụng")]
    public Sprite placeholderSprite;

    [Tooltip("Màu của placeholder (default: gray với alpha 0.5)")]
    public Color placeholderColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    private Sprite originalSprite;
    private bool isPlaceholder = false;

    // ✅ Card Animation System
    [Header("Card Animation Settings")]
    [Tooltip("Thời gian hiển thị card animation (giây)")]
    public float animationDuration = 0.3f;

    [Tooltip("Scale của card khi ở giữa màn hình")]
    public float centerCardScale = 1f;

    [Tooltip("Kích thước card giữa màn hình")]
    private Vector2 centerCardSize = new Vector2(50f, 65f);

    [Header("Card Visual")]
    [Tooltip("Image object hiển thị card (child object)")]
    public Image imgtCard;

    // ✅ TỰ TẠO CANVAS VÀ IMAGE
    private Canvas animationCanvas;
    private Image centerCardImage;

    // ✅ DOT SKILL SYSTEM
    [Header("Dot Skill Settings")]
    [Tooltip("Panel chứa 7 nút mũi tên (tự động tìm hoặc tạo)")]
    public Transform dotSkillPanel;

    [Tooltip("Prefab Image để hiển thị mũi tên")]
    public GameObject arrowPrefab;

    [Tooltip("Thời gian cho phép người chơi gõ phím (giây)")]
    public float dotSkillDuration = 5f;

    [Header("Dot Skill Time Slider")]
    [Tooltip("Slider hiển thị thời gian (tự động tạo nếu null)")]
    public Slider timeSlider;

    [Tooltip("Màu slider khi còn nhiều thời gian")]
    public Color sliderColorNormal = Color.green;

    [Tooltip("Màu slider khi sắp hết thời gian")]
    public Color sliderColorWarning = Color.red;

    [Tooltip("Ngưỡng chuyển màu cảnh báo (% thời gian còn lại)")]
    [Range(0f, 1f)]
    public float warningThreshold = 0.3f;

    // ✅ TIMING ZONES (TRÊN TIME SLIDER)
    [Header("Timing Zones on Time Slider")]
    [Tooltip("Text hiển thị Perfect/Good/Bad (tự động tạo nếu null)")]
    public Text timingText;

    [Tooltip("Màu text Perfect")]
    public Color perfectColor = new Color(0.6f, 0.2f, 0.8f); // ✅ TÍM

    [Tooltip("Màu text Good")]
    public Color goodColor = Color.red; // ✅ ĐỎ

    [Tooltip("Màu text Bad")]
    public Color badColor = Color.green; // ✅ XANH LÁ

    // ✅ BONUS MULTIPLIER
    [Header("Damage Multipliers")]
    [Tooltip("Hệ số nhân dame khi Perfect")]
    [Range(1f, 3f)]
    public float perfectMultiplier = 1.5f;

    [Tooltip("Hệ số nhân dame khi Good")]
    [Range(1f, 2f)]
    public float goodMultiplier = 1.2f;

    [Tooltip("Hệ số nhân dame khi Bad")]
    [Range(0.5f, 1f)]
    public float badMultiplier = 0.8f;

    private List<Image> currentArrows = new List<Image>();
    private string[] directions = { "nutDown", "nutLeft", "nutRight", "nutUp" };
    private int currentDotIndex = 0;
    private int correctDotCount = 0;
    private bool isDotSkillActive = false;
    private float currentTimeValue = 1f;
    private float damageMultiplier = 1f;

    private Dictionary<string, Sprite> blueArrows = new Dictionary<string, Sprite>();
    private Dictionary<string, Sprite> purpleArrows = new Dictionary<string, Sprite>();

    private bool hasFinishedDotSkill = false;

    // ✅ BUTTON REFERENCES (TỰ ĐỘNG TẠO)
    [Header("Control Buttons")]
    [Tooltip("Nút Up (tự động tạo nếu null)")]
    public Button btnUp;

    [Tooltip("Nút Down (tự động tạo nếu null)")]
    public Button btnDown;

    [Tooltip("Nút Left (tự động tạo nếu null)")]
    public Button btnLeft;

    [Tooltip("Nút Right (tự động tạo nếu null)")]
    public Button btnRight;

    [Tooltip("Nút Enter (tự động tạo nếu null)")]
    public Button btnEnter;

    [Header("Timing Zones on Time Slider")]

    [Tooltip("Thời gian bắt đầu Perfect (giây) - mặc định 3.0s")]
    public float perfectStartTime = 3.0f;

    [Tooltip("Thời gian kết thúc Perfect (giây) - mặc định 3.3s")]
    public float perfectEndTime = 3.3f;

    [Tooltip("Thời gian bắt đầu Good đầu tiên (giây) - mặc định 2.5s")]
    public float goodStart1Time = 2.5f;

    [Tooltip("Thời gian kết thúc Good đầu tiên (giây) - mặc định 3.0s")]
    public float goodEnd1Time = 3.0f;

    [Tooltip("Thời gian bắt đầu Good thứ hai (giây) - mặc định 3.3s")]
    public float goodStart2Time = 3.3f;

    [Tooltip("Thời gian kết thúc Good thứ hai (giây) - mặc định 4.2s")]
    public float goodEnd2Time = 4.2f;
    private int timingBonus = 0; // ✅ LƯU BONUS THEO TIMING

    // 2️⃣ THÊM INSPECTOR FIELDS CHO TIMING BONUS (dòng ~80)
    [Header("Timing Bonus (for ATTACK_LEGEND_)")]
    [Tooltip("Bonus correctCount khi Perfect (nếu không gõ đủ 7 nút)")]
    public int perfectBonus = 4;

    [Tooltip("Bonus correctCount khi Good (nếu không gõ đủ 7 nút)")]
    public int goodBonus = 2;

    [Tooltip("Bonus correctCount khi Bad (nếu không gõ đủ 7 nút)")]
    public int badBonus = 0;

    [Tooltip("Màu text Perfect")]

    // ✅ ENTER BUTTON BLINK EFFECT
    private Coroutine blinkCoroutine;

    void Start()
{
    board = FindFirstObjectByType<Board>();
    active = FindFirstObjectByType<Active>();
    btn = GetComponent<Button>();
    if (cardData.cardId == 0)
    {
        gameObject.SetActive(false);
        Debug.Log("[CardUI] Card hidden - no cardData assigned");
        return; // ✅ DỪNG KHỞI TẠO
    }

    // ✅ Setup imgtCard
    if (imgtCard == null)
    {
        Transform imgTransform = transform.Find("imgtCard");
        if (imgTransform != null)
        {
            imgtCard = imgTransform.GetComponent<Image>();
        }
        
        if (imgtCard != null)
        {
            originalSprite = imgtCard.sprite;
            Debug.Log($"[CardUI] Found imgtCard, sprite: {(originalSprite != null ? originalSprite.name : "NULL")}");
        }
        else
        {
            Debug.LogError("[CardUI] imgtCard not found!");
        }
    }
    else
    {
        originalSprite = imgtCard.sprite;
        Debug.Log($"[CardUI] imgtCard assigned, sprite: {(originalSprite != null ? originalSprite.name : "NULL")}");
    }

    // ✅ Tạo Animation Canvas cho tất cả cards
    CreateAnimationCanvas();

    // ✅ CHỈ TẠO DOT SKILL UI NẾU CẦN (sẽ check sau khi có cardData)
    // CreateDotSkillPanel(); // ❌ XÓA DÒNG NÀY
    // CreateTimeSliderWithZones(); // ❌ XÓA DÒNG NÀY
    // CreateTimingText(); // ❌ XÓA DÒNG NÀY

    // ✅ Setup button listeners (nếu có)
    SetupControlButtonListeners();

    // ✅ Load sprites cho Dot Skill
    LoadDotSkillSprites();

    // ✅ Ẩn UI ban đầu
    HideDotSkillUI();

    // ✅ Setup card click
    if (btn != null)
    {
        btn.onClick.AddListener(OnCardClick);
    }

    // ✅ Subscribe events
    if (active != null)
    {
        active.OnTurnStart += OnTurnStart;
    }
}
    /// <summary>
    /// ✅ SETUP LISTENER CHO CÁC BUTTON ĐÃ GÁN SẴN TRONG INSPECTOR
    /// </summary>
    private void SetupControlButtonListeners()
    {
        // ✅ XÓA TẤT CẢ LISTENER CŨ (NẾU CÓ)
        if (btnUp != null)
        {
            btnUp.onClick.RemoveAllListeners();
            btnUp.onClick.AddListener(() => OnDirectionButtonPress("nutUp"));
            Debug.Log("[CardUI] Setup listener for btnUp");
        }

        if (btnDown != null)
        {
            btnDown.onClick.RemoveAllListeners();
            btnDown.onClick.AddListener(() => OnDirectionButtonPress("nutDown"));
            Debug.Log("[CardUI] Setup listener for btnDown");
        }

        if (btnLeft != null)
        {
            btnLeft.onClick.RemoveAllListeners();
            btnLeft.onClick.AddListener(() => OnDirectionButtonPress("nutLeft"));
            Debug.Log("[CardUI] Setup listener for btnLeft");
        }

        if (btnRight != null)
        {
            btnRight.onClick.RemoveAllListeners();
            btnRight.onClick.AddListener(() => OnDirectionButtonPress("nutRight"));
            Debug.Log("[CardUI] Setup listener for btnRight");
        }

        if (btnEnter != null)
        {
            btnEnter.onClick.RemoveAllListeners();
            btnEnter.onClick.AddListener(OnEnterButtonPress);
            btnEnter.interactable = false;
            Debug.Log("[CardUI] Setup listener for btnEnter");
        }
    }

    /// <summary>
    /// ✅ ẨN TẤT CẢ UI DOT SKILL
    /// </summary>
    private void HideDotSkillUI()
    {
        if (dotSkillPanel != null)
        {
            dotSkillPanel.gameObject.SetActive(false);
        }

        if (timeSlider != null)
        {
            timeSlider.gameObject.SetActive(false);
        }

        if (timingText != null)
        {
            timingText.gameObject.SetActive(false);
        }

        // ✅ ẨN TỪNG BUTTON, KHÔNG TẮT PARENT
        if (btnUp != null)
        {
            btnUp.gameObject.SetActive(false);
            btnDown.gameObject.SetActive(false);
            btnLeft.gameObject.SetActive(false);
            btnRight.gameObject.SetActive(false);
            btnEnter.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// ✅ HIỆN TẤT CẢ UI DOT SKILL
    /// </summary>
    private void ShowDotSkillUI()
{
    // ✅ VALIDATE TRƯỚC KHI HIỆN
    if (!ValidateDotSkillComponents())
    {
        Debug.LogError("[CardUI] Cannot show Dot Skill UI - missing components!");
        return;
    }

    if (dotSkillPanel != null)
    {
        dotSkillPanel.gameObject.SetActive(true);
    }

    if (timeSlider != null)
    {
        timeSlider.gameObject.SetActive(true);
    }

    if (btnUp != null)
    {
        btnUp.gameObject.SetActive(true);
        btnUp.interactable = true;

        btnDown.gameObject.SetActive(true);
        btnDown.interactable = true;

        btnLeft.gameObject.SetActive(true);
        btnLeft.interactable = true;

        btnRight.gameObject.SetActive(true);
        btnRight.interactable = true;

        btnEnter.gameObject.SetActive(true);
        btnEnter.interactable = false;
    }
}

    void OnDestroy()
    {
        if (active != null)
        {
            active.OnTurnStart -= OnTurnStart;
        }
    }

    private void CreateDotSkillPanel()
    {
        if (dotSkillPanel != null) return;

        Canvas canvas = animationCanvas;
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas != null)
        {
            Transform panel = canvas.transform.Find("DotSkillPanel");
            if (panel == null)
            {
                GameObject panelObj = new GameObject("DotSkillPanel");
                panelObj.transform.SetParent(canvas.transform);

                RectTransform rt = panelObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0, 0f);
                rt.sizeDelta = new Vector2(500f, 100f);

                HorizontalLayoutGroup layout = panelObj.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 10f;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;

                dotSkillPanel = panelObj.transform;
                panelObj.SetActive(false);

                Debug.Log("[CardUI] Created DotSkillPanel");
            }
            else
            {
                dotSkillPanel = panel;
            }
        }
    }

    private void CreateTimeSliderWithZones()
    {
        // ✅ NẾU ĐÃ GÁN SLIDER TRONG INSPECTOR THÌ SỬ DỤNG LUÔN
        if (timeSlider != null)
        {
            RemoveZonesFromSlider(); // Xóa zones cũ
            return;
        }

        // ✅ TÌM SLIDER CÓ SẴN TRONG SCENE
        timeSlider = FindFirstObjectByType<Slider>();

        if (timeSlider != null)
        {
            Debug.Log($"[CardUI] Found existing TimeSlider: {timeSlider.name}");
            RemoveZonesFromSlider(); // Xóa zones cũ
            return;
        }

        // ✅ NẾU KHÔNG TÌM THẤY, TẠO MỚI
        Canvas canvas = animationCanvas;
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas != null)
        {
            GameObject sliderObj = new GameObject("DotSkillTimeSlider");
            sliderObj.transform.SetParent(canvas.transform);

            RectTransform sliderRT = sliderObj.AddComponent<RectTransform>();
            sliderRT.anchorMin = new Vector2(0.5f, 0.5f);
            sliderRT.anchorMax = new Vector2(0.5f, 0.5f);
            sliderRT.pivot = new Vector2(0.5f, 0.5f);
            sliderRT.anchoredPosition = new Vector2(0, -150f);
            sliderRT.sizeDelta = new Vector2(500f, 40f);

            timeSlider = sliderObj.AddComponent<Slider>();
            timeSlider.minValue = 0f;
            timeSlider.maxValue = 1f;
            timeSlider.value = 1f;
            timeSlider.interactable = false;

            // Tạo Background
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(sliderObj.transform);
            RectTransform bgRT = bg.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

            // Tạo Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform);
            RectTransform fillAreaRT = fillArea.AddComponent<RectTransform>();
            fillAreaRT.anchorMin = Vector2.zero;
            fillAreaRT.anchorMax = Vector2.one;
            fillAreaRT.sizeDelta = new Vector2(-10f, -10f);
            fillAreaRT.offsetMin = new Vector2(5f, 5f);
            fillAreaRT.offsetMax = new Vector2(-5f, -5f);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform);
            RectTransform fillRT = fill.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(1f, 1f, 1f, 0.5f);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.raycastTarget = false;

            timeSlider.fillRect = fillRT;
            timeSlider.targetGraphic = fillImage;

            sliderObj.SetActive(false);

            Debug.Log("[CardUI] Created new DotSkillTimeSlider (no gradient zones)");
        }
    }

    private void RemoveZonesFromSlider()
    {
        if (timeSlider == null) return;

        GameObject sliderObj = timeSlider.gameObject;

        for (int i = sliderObj.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = sliderObj.transform.GetChild(i);
            if (child.name.StartsWith("ColorSegment_") || child.name == "PerfectZone")
            {
                DestroyImmediate(child.gameObject);
            }
        }

        Debug.Log($"[CardUI] Added time-based zone markers to slider");
    }

    private void CreateTimingText()
{
    if (timingText != null) return;

    Canvas canvas = animationCanvas;
    if (canvas == null)
    {
        canvas = FindFirstObjectByType<Canvas>();
    }

    if (canvas == null) return;

    Transform textTransform = canvas.transform.Find("TimingText");

    if (textTransform == null)
    {
        GameObject textObj = new GameObject("TimingText");
        textObj.transform.SetParent(canvas.transform);

        RectTransform textRT = textObj.AddComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.5f, 0.5f);
        textRT.anchorMax = new Vector2(0.5f, 0.5f);
        textRT.pivot = new Vector2(0.5f, 0.5f);
        textRT.anchoredPosition = new Vector2(0, 150f);
        textRT.sizeDelta = new Vector2(300f, 80f);

        timingText = textObj.AddComponent<Text>();
        
        // ✅ THÊM DÒNG NÀY - QUAN TRỌNG!
        timingText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        
        timingText.fontSize = 60;
        timingText.alignment = TextAnchor.MiddleCenter;
        timingText.fontStyle = FontStyle.Bold;
        timingText.text = "";
        timingText.color = Color.white;
        timingText.raycastTarget = false;

        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(3, -3);

        textObj.SetActive(false);

        Debug.Log("[CardUI] Created TimingText with Arial font");
    }
    else
    {
        timingText = textTransform.GetComponent<Text>();
        
        // ✅ ĐẢM BẢO TEXT CÓ SẴN CŨNG CÓ FONT
        if (timingText != null && timingText.font == null)
        {
            timingText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            Debug.Log("[CardUI] Added font to existing TimingText");
        }
    }
}


    private void LoadDotSkillSprites()
    {
        foreach (string dir in directions)
        {
            Sprite blueSprite = Resources.Load<Sprite>($"DotSkillRepare/{dir}");
            Sprite purpleSprite = Resources.Load<Sprite>($"DotSkillComple/{dir}");

            if (blueSprite != null)
                blueArrows[dir] = blueSprite;

            if (purpleSprite != null)
                purpleArrows[dir] = purpleSprite;
        }
    }

    private GameObject CreateDefaultArrowPrefab()
    {
        GameObject prefab = new GameObject("Arrow");
        Image img = prefab.AddComponent<Image>();

        RectTransform rt = img.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(60f, 60f);

        return prefab;
    }

    /// <summary>
    /// ✅ TẠO ANIMATION CANVAS & CENTER CARD IMAGE
    /// </summary>
    private void CreateAnimationCanvas()
    {
        GameObject canvasObj = GameObject.Find("CardAnimationCanvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("CardAnimationCanvas");
            animationCanvas = canvasObj.AddComponent<Canvas>();
            animationCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            animationCanvas.sortingOrder = 1000;

            // ✅ THÊM CANVAS SCALER
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // ✅ THÊM GRAPHIC RAYCASTER (QUAN TRỌNG!)
            GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();
            raycaster.ignoreReversedGraphics = true;
            raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;

            Debug.Log("[CardUI] Created CardAnimationCanvas with GraphicRaycaster");
        }
        else
        {
            animationCanvas = canvasObj.GetComponent<Canvas>();

            // ✅ KIỂM TRA VÀ THÊM GRAPHIC RAYCASTER NẾU CHƯA CÓ
            if (canvasObj.GetComponent<GraphicRaycaster>() == null)
            {
                GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();
                raycaster.ignoreReversedGraphics = true;
                raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
                Debug.Log("[CardUI] Added GraphicRaycaster to existing canvas");
            }
        }

        // ✅ KIỂM TRA VÀ TẠO EVENT SYSTEM NẾU CHƯA CÓ
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("[CardUI] Created EventSystem");
        }

        Transform centerCardTransform = animationCanvas.transform.Find("CenterCardImage");
        GameObject centerCardObj;

        if (centerCardTransform == null)
        {
            centerCardObj = new GameObject("CenterCardImage");
            centerCardObj.transform.SetParent(animationCanvas.transform);

            centerCardImage = centerCardObj.AddComponent<Image>();
            centerCardImage.preserveAspect = true;
            centerCardImage.color = Color.white;
            centerCardImage.raycastTarget = false;

            RectTransform rt = centerCardImage.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(-230f, 270f);
            rt.sizeDelta = centerCardSize;

            centerCardObj.AddComponent<CanvasGroup>();
            centerCardObj.SetActive(false);

            Debug.Log("[CardUI] Created CenterCardImage");
        }
        else
        {
            centerCardImage = centerCardTransform.GetComponent<Image>();
        }
    }

    private void OnTurnStart(int entityIndex)
    {
        if (active == null || cardData == null || cardData.cardId==0) return;

        int currentTurn = active.TurnNumber;
        Debug.Log($"[CardUI] Turn {cardData.cardId} started");
        string elementType = cardData.elementTypeCard.ToUpper();

        if (currentTurn == 1)
        {
            hasUsedBuffThisTurn = false;
            lastBuffUsedTurn = -1;
        }

        if (currentTurn != lastBuffUsedTurn)
        {
            hasUsedBuffThisTurn = false;
            lastBuffUsedTurn = currentTurn;
        }

        if (elementType.Contains("ATTACK"))
        {
            if (currentTurn != lastTurnUsed)
            {
                hasUsedThisTurn = false;

                if (isPlaceholder)
                {
                    isPlaceholder = false;
                }

                UpdateCardVisual();
            }
        }
        else
        {
            UpdateCardVisual();
        }
    }

    public void SetCardData(CardData data)
{
    cardData = data;

    // ✅ NẾU LÀ LEGEND CARD, TẠO DOT SKILL UI
    if (RequiresDotSkillUI())
    {
        Debug.Log($"[CardUI] Setting up Dot Skill UI for {cardData.name}");
        
        CreateDotSkillPanel();
        CreateTimeSliderWithZones();
        CreateTimingText();
        
        // ✅ Validate sau khi tạo
        if (!ValidateDotSkillComponents())
        {
            Debug.LogError($"[CardUI] Failed to create Dot Skill UI for {cardData.name}!");
        }
    }
    else
    {
        Debug.Log($"[CardUI] Normal card {cardData.name} - no Dot Skill UI needed");
    }
}

    public CardData GetCardData()
    {
        return cardData;
    }

/// <summary>
/// ✅ Kiểm tra xem Dot Skill UI có cần thiết không (chỉ cho legend cards)
/// </summary>
private bool RequiresDotSkillUI()
{
    if (cardData == null || cardData.cardId == 0) return false;
    string elementType = cardData.elementTypeCard.ToUpper();
    return elementType == "ATTACK_LEGEND" || elementType == "ATTACK_LEGEND_";
}

/// <summary>
/// ✅ Validate Dot Skill components - CHỈ CHO LEGEND CARDS
/// </summary>
private bool ValidateDotSkillComponents()
{
    if (!RequiresDotSkillUI())
    {
        // Card thường không cần Dot Skill components
        return true;
    }

    // ✅ LEGEND CARD: Kiểm tra các components cần thiết
    List<string> missingComponents = new List<string>();

    if (dotSkillPanel == null) missingComponents.Add("dotSkillPanel");
    if (timeSlider == null) missingComponents.Add("timeSlider");
    if (btnUp == null) missingComponents.Add("btnUp");
    if (btnDown == null) missingComponents.Add("btnDown");
    if (btnLeft == null) missingComponents.Add("btnLeft");
    if (btnRight == null) missingComponents.Add("btnRight");
    if (btnEnter == null) missingComponents.Add("btnEnter");

    if (missingComponents.Count > 0)
    {
        Debug.LogError($"[CardUI] Legend card {cardData.name} missing components: {string.Join(", ", missingComponents)}");
        return false;
    }

    return true;
}
    private bool IsBuffCard()
    {
        if (cardData == null) return false;
        string elementType = cardData.elementTypeCard.ToUpper();
        return elementType == "HEALTH" || elementType == "MANA" || elementType == "POWER";
    }

    private bool IsAttackCard()
    {
        if (cardData == null || cardData.cardId == 0) return false;
        string elementType = cardData.elementTypeCard.ToUpper();
        return elementType == "ATTACK" || elementType == "ATTACK_LEGEND" || elementType == "ATTACK_LEGEND_";
    }

    private bool IsDotSkillCard()
    {
        if (cardData == null || cardData.cardId == 0) return false;
        string elementType = cardData.elementTypeCard.ToUpper();
        return elementType == "ATTACK_LEGEND" || elementType == "ATTACK_LEGEND_";
    }

    public void OnCardClick()
{
    if (board == null ||
        board.currentState != GameState.move ||
        !board.IsPlayerAllowedToMove() ||
        active == null ||
        board.hasDestroyedThisTurn)
    {
        return;
    }

    if (!CanUseCard())
    {
        return;
    }

    if (cardData == null)
    {
        Debug.LogWarning("[CardUI] Cannot use card - cardData is null");
        return;
    }

    // ✅ VALIDATE DOT SKILL COMPONENTS CHO LEGEND CARDS
    if (IsDotSkillCard())
    {
        if (!ValidateDotSkillComponents())
        {
            Debug.LogError($"[CardUI] Cannot use {cardData.name} - missing Dot Skill components!");
            return;
        }
    }

    // ✅ Update flags
    if (IsAttackCard())
    {
        hasUsedThisTurn = true;
        lastTurnUsed = active != null ? active.TurnNumber : -1;
    }
    else if (IsBuffCard())
    {
        hasUsedThisMatch = true;
        hasUsedBuffThisTurn = true;
        lastBuffUsedTurn = active != null ? active.TurnNumber : -1;
    }

    StartCoroutine(PlayCardAnimationSequence());
}

   private IEnumerator PlayCardAnimationSequence()
{
    if (active == null || board == null) yield break;

    board.HideAllItems();
    active.PauseTurn();
    yield return new WaitForSeconds(0.3f);

    if (centerCardImage != null)
{
    // ✅ COPY SPRITE TỪ CARD ĐANG CLICK
    if (imgtCard != null && imgtCard.sprite != null)
    {
        centerCardImage.sprite = imgtCard.sprite;
        Debug.Log($"[CardUI] Display card sprite: {imgtCard.sprite.name}");
    }
    else
    {
        Debug.LogError("[CardUI] imgtCard or its sprite is null!");
        yield break;
    }
        centerCardImage.color = Color.white;

        GameObject centerCardObj = centerCardImage.gameObject;
        centerCardObj.SetActive(true);

        CanvasGroup cg = centerCardObj.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = centerCardObj.AddComponent<CanvasGroup>();

        cg.alpha = 1f;

        centerCardObj.transform.localScale = Vector3.zero;
        LeanTween.scale(centerCardObj, Vector3.one * centerCardScale, 0.4f)
            .setEaseOutBack();

        yield return new WaitForSeconds(0.5f);

        if (IsDotSkillCard())
        {
            yield return StartCoroutine(HandleDotSkillSequence());
        }
        else
        {
            yield return StartCoroutine(HandleCardEffectByElementType());
        }

        // ✅ CHỈ FADE OUT THẺ CHO CARD THƯỜNG (không phải Dot Skill)
        if (!IsDotSkillCard())
        {
            yield return new WaitForSeconds(animationDuration);

            float t = 0f;
            float fadeDuration = 0.3f;

            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
                yield return null;
            }

            cg.alpha = 0f;
            centerCardObj.SetActive(false);
            centerCardObj.transform.localScale = Vector3.one;
        }
    }

    // ✅ CHECK NẾU LÀ ATTACK_LEGEND_ → KHÔNG XỬ LÝ TIẾP (HandleUI() đang chạy)
    if (cardData != null && cardData.elementTypeCard.ToUpper() == "ATTACK_LEGEND_")
    {
        Debug.Log("[CardUI] ATTACK_LEGEND_ completed - letting HandleUI() take over");
        
        ConvertToPlaceholder();

        yield break;
    }

    // ===== XỬ LÝ BÌNH THƯỜNG CHO CÁC CARD KHÁC =====
    
    bool playerDead = active.MauPlayer <= 0;
    bool npcDead = active.MauNPC <= 0;

    if (playerDead || npcDead)
    {
        yield return StartCoroutine(board.ShowGameResultIntegrated(npcDead));
        yield break;
    }

    board.ShowItems();
    yield return new WaitForSeconds(0.1f);

    if (IsBuffCard())
    {
        yield return StartCoroutine(CallAPIUseCard());
    }

    ConvertToPlaceholder();
    yield return new WaitForSeconds(0.5f);

    if (IsBuffCard())
    {
        active.ResumeTurn();
    }
    else if (IsAttackCard())
    {
        if (active.IsPlayerTurn)
            active.EndCurrentTurn();
    }
}

    /// <summary>
    /// ✅ XỬ LÝ DOT SKILL - GÕ 7 PHÍM + NHẤN ENTER ĐỂ HOÀN TẤT
    /// </summary>
private IEnumerator HandleDotSkillSequence()
{
    if (cardData == null || active == null)
    {
        Debug.LogError("[CardUI] Cannot handle Dot Skill - missing dependencies");
        yield break;
    }

    // ✅ VALIDATE COMPONENTS
    if (!ValidateDotSkillComponents())
    {
        Debug.LogError("[CardUI] Cannot start Dot Skill - missing components!");
        yield break;
    }

    string elementType = cardData.elementTypeCard.ToUpper();
    int totalDamage = cardData.value;

    if (elementType == "ATTACK_LEGEND")
    {
        totalDamage += active.attackP;
    }

    // ===== RESET =====
    correctDotCount = 0;
    currentTimeValue = 1f;
    damageMultiplier = badMultiplier;
    timingBonus = badBonus;
    hasFinishedDotSkill = false;

    GenerateDotArrows();
    ShowDotSkillUI();

    if (timeSlider != null)
    {
        timeSlider.value = 1f;
        Image fillImage = timeSlider.fillRect?.GetComponent<Image>();
        if (fillImage != null)
        {
            fillImage.color = sliderColorNormal;
        }
    }

    if (btnEnter != null)
    {
        btnEnter.interactable = false;
    }

    // ===== BẮT ĐẦU MINI-GAME =====
    isDotSkillActive = true;
    float timeLeft = dotSkillDuration;
    float totalTime = dotSkillDuration;

    while (timeLeft > 0 && !hasFinishedDotSkill)
    {
        timeLeft -= Time.deltaTime;
        currentTimeValue = timeLeft / totalTime;

        if (timeSlider != null)
        {
            timeSlider.value = currentTimeValue;
        }

        yield return null;
    }

    // ✅ HIỂN THỊ TIMING TEXT
    if (!hasFinishedDotSkill)
    {
        ShowTimingResult();
    }

    yield return new WaitForSeconds(0.5f);

    if (blinkCoroutine != null)
    {
        StopCoroutine(blinkCoroutine);
        blinkCoroutine = null;
    }

    isDotSkillActive = false;
    HideDotSkillUI();

    yield return new WaitForSeconds(0.3f);

    // ✅ ẨN THẺ TRƯỚC KHI XỬ LÝ
    if (centerCardImage != null)
    {
        GameObject centerCardObj = centerCardImage.gameObject;
        CanvasGroup cg = centerCardObj.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = centerCardObj.AddComponent<CanvasGroup>();

        float t = 0f;
        float fadeDuration = 0.3f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            yield return null;
        }

        cg.alpha = 0f;
        centerCardObj.SetActive(false);
        centerCardObj.transform.localScale = Vector3.one;
    }

    // ===== XỬ LÝ ATTACK_LEGEND_ (PHÁ DOT) =====
    if (elementType == "ATTACK_LEGEND_")
    {
        int dotsToDestroy = correctDotCount + timingBonus;
        dotsToDestroy = Mathf.Clamp(dotsToDestroy, 1, 7);

        Debug.Log($"[CardUI] ATTACK_LEGEND_: Destroying {dotsToDestroy} dots (Keys: {correctDotCount} + Timing: {timingBonus})");

        if (board != null)
        {
            board.ShowItems();
        }
        
        yield return new WaitForSeconds(0.3f);

        if (board != null)
        {
            board.DestroyConfiguredDots(cardData.blue, cardData.green, cardData.red, cardData.white, cardData.yellow, dotsToDestroy);
        }

        ClearDotArrows();
        yield break;
    }

    // ===== XỬ LÝ ATTACK_LEGEND (DAMAGE THƯỜNG) =====
    int damagePerArrow = totalDamage / 7;
    int effectiveCorrectCount = Mathf.Max(correctDotCount, 1);
    int baseDamage = damagePerArrow * effectiveCorrectCount;
    int finalDamage = Mathf.RoundToInt(baseDamage * damageMultiplier);

    Debug.Log($"[CardUI] ATTACK_LEGEND: {correctDotCount}/7 × {damageMultiplier:F1}x = {finalDamage} damage");

    active.MauNPC = Mathf.Max(active.MauNPC - finalDamage, 0);
    active.valueCurrent = finalDamage;
    active.UpdateSlider();

    if (active.playerPetAnimator != null)
    {
        active.playerPetAnimator.SetInteger("key", 2);
    }

    yield return new WaitForSeconds(1f);

    if (active.dameATKPrefad != null)
    {
        active.dameATKPrefad.SetActive(true);

        Text dame = active.dameATKPrefad.transform.Find("txtdame")?.GetComponent<Text>();
        if (dame != null)
            dame.text = finalDamage.ToString();

        yield return new WaitForSeconds(0.5f);
        active.dameATKPrefad.SetActive(false);
    }

    ClearDotArrows();
}
    /// <summary>
    /// ✅ XỬ LÝ KHI NHẤN ENTER - HOÀN TẤT MINI-GAME
    /// </summary>
    private void OnEnterButtonPress()
    {
        if (!isDotSkillActive || hasFinishedDotSkill) return; // ✅ Thêm kiểm tra

        // ✅ HIỂN THỊ KẾT QUẢ NGAY LẬP TỨC
        ShowTimingResult();

        // ✅ SET FLAG ĐỂ THOÁT VÒNG LẶP (nhưng không ẩn UI ngay)
        hasFinishedDotSkill = true;

        // ✅ DỪNG BLINK
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
    }

    private void OnDirectionButtonPress(string direction)
    {
        if (!isDotSkillActive || currentArrows.Count == 0) return;
        CheckDotArrow(direction);
    }

    private void GenerateDotArrows()
    {
        correctDotCount = 0;
        ClearDotArrows();
        currentArrows.Clear();
        currentDotIndex = 0;

        if (dotSkillPanel == null) return;

        GameObject prefab = arrowPrefab;
        if (prefab == null)
        {
            prefab = CreateDefaultArrowPrefab();
        }

        for (int i = 0; i < 7; i++)
        {
            string randomDir = directions[Random.Range(0, directions.Length)];
            GameObject newArrow = Instantiate(prefab, dotSkillPanel);
            Image img = newArrow.GetComponent<Image>();

            if (blueArrows.ContainsKey(randomDir))
            {
                img.sprite = blueArrows[randomDir];
            }

            img.name = randomDir;
            img.raycastTarget = false;
            currentArrows.Add(img);
        }

        Debug.Log("[CardUI] Generated 7 arrows");
    }

    /// <summary>
    /// ✅ HÀM TÁCH RIÊNG ĐỂ HIỂN THỊ TIMING RESULT
    /// </summary>
    private void ShowTimingResult()
    {
        float totalTime = dotSkillDuration;
        float timeLeft = currentTimeValue * totalTime;
        float currentTime = totalTime - timeLeft;

        string result;
        Color resultColor;

        if (currentTime >= perfectStartTime && currentTime <= perfectEndTime)
        {
            result = "PERFECT!";
            resultColor = perfectColor;
            damageMultiplier = perfectMultiplier;
            timingBonus = perfectBonus; // ✅ LƯU BONUS
        }
        else if ((currentTime >= goodStart1Time && currentTime < goodEnd1Time) ||
                 (currentTime > goodStart2Time && currentTime <= goodEnd2Time))
        {
            result = "GOOD!";
            resultColor = goodColor;
            damageMultiplier = goodMultiplier;
            timingBonus = goodBonus; // ✅ LƯU BONUS
        }
        else
        {
            result = "BAD";
            resultColor = badColor;
            damageMultiplier = badMultiplier;
            timingBonus = badBonus; // ✅ LƯU BONUS
        }

        if (timingText != null)
        {
            timingText.gameObject.SetActive(true);
            timingText.text = result;
            timingText.color = resultColor;

            LeanTween.cancel(timingText.gameObject);
            timingText.transform.localScale = Vector3.zero;
            LeanTween.scale(timingText.gameObject, Vector3.one * 1.5f, 0.3f)
                .setEaseOutBack();
        }

        Debug.Log($"[CardUI] Timing: {result} at {currentTime:F2}s (Multiplier: {damageMultiplier:F1}x, Bonus: +{timingBonus}, Keys: {correctDotCount}/7)");
    }

    private void ClearDotArrows()
    {
        if (dotSkillPanel == null) return;

        foreach (Transform child in dotSkillPanel)
        {
            Destroy(child.gameObject);
        }

        currentArrows.Clear();
    }

    void Update()
{
    UpdateCardVisual();

    // ✅ CHỈ XỬ LÝ INPUT NẾU LÀ DOT SKILL CARD
    if (isDotSkillActive && IsDotSkillCard() && currentArrows.Count > 0)
    {
        if (Input.anyKeyDown)
        {
            string keyPressed = GetDirectionFromInput();
            if (keyPressed != null)
            {
                CheckDotArrow(keyPressed);
            }
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            OnEnterButtonPress();
        }
    }
}

    private void CheckDotArrow(string dir)
    {
        if (currentDotIndex >= currentArrows.Count) return;

        Image currentArrow = currentArrows[currentDotIndex];

        if (currentArrow.name == dir)
        {
            // ✅ ĐÚNG
            if (purpleArrows.ContainsKey(dir))
            {
                currentArrow.sprite = purpleArrows[dir];
            }

            currentDotIndex++;
            correctDotCount++;

            Debug.Log($"[CardUI] Correct! {correctDotCount}/7");

            LeanTween.scale(currentArrow.gameObject, Vector3.one * 1.2f, 0.1f)
                .setEaseOutBack()
                .setOnComplete(() =>
                {
                    if (currentArrow != null)
                        LeanTween.scale(currentArrow.gameObject, Vector3.one, 0.1f);
                });

            // ✅ NẾU ĐÃ GÕ ĐỦ 7 → BẬT NÚT ENTER & NHẤP NHÁY
            if (correctDotCount >= 7 && btnEnter != null)
            {
                btnEnter.interactable = true;

                // ✅ BẮT ĐẦU NHẤP NHÁY
                if (blinkCoroutine != null)
                {
                    StopCoroutine(blinkCoroutine);
                }
                blinkCoroutine = StartCoroutine(BlinkEnterButton());
            }
        }
        else
        {
            // ✅ SAI → RESET
            Debug.Log("[CardUI] Wrong key! Reset combo.");
            ResetDotCombo();

            // ✅ TẮT NÚT ENTER
            if (btnEnter != null)
            {
                btnEnter.interactable = false;

                if (blinkCoroutine != null)
                {
                    StopCoroutine(blinkCoroutine);
                    blinkCoroutine = null;
                }
            }

            LeanTween.cancel(currentArrow.gameObject);
            Vector3 originalPos = currentArrow.transform.localPosition;
            LeanTween.moveLocalX(currentArrow.gameObject, originalPos.x + 10f, 0.05f)
                .setLoopPingPong(4)
                .setOnComplete(() =>
                {
                    if (currentArrow != null)
                        currentArrow.transform.localPosition = originalPos;
                });
        }
    }

    /// <summary>
    /// ✅ COROUTINE NHẤP NHÁY NÚT ENTER
    /// </summary>
    private IEnumerator BlinkEnterButton()
    {
        if (btnEnter == null) yield break;

        Image btnImage = btnEnter.GetComponent<Image>();
        if (btnImage == null) yield break;

        Color originalColor = new Color(0.2f, 0.6f, 1f, 0.9f);
        Color highlightColor = new Color(1f, 1f, 0f, 1f); // Vàng sáng

        while (isDotSkillActive && correctDotCount >= 7)
        {
            // Sáng lên
            btnImage.color = highlightColor;
            yield return new WaitForSeconds(0.3f);

            // Tối đi
            btnImage.color = originalColor;
            yield return new WaitForSeconds(0.3f);
        }

        // Reset màu khi kết thúc
        btnImage.color = originalColor;
    }

    private void ResetDotCombo()
    {
        for (int i = 0; i < currentArrows.Count; i++)
        {
            Image arrow = currentArrows[i];
            string dir = arrow.name;

            if (blueArrows.ContainsKey(dir))
            {
                arrow.sprite = blueArrows[dir];
            }
        }

        currentDotIndex = 0;
        correctDotCount = 0;
    }

    private string GetDirectionFromInput()
    {
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) return "nutDown";
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) return "nutLeft";
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) return "nutRight";
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) return "nutUp";
        return null;
    }

    private IEnumerator HandleCardEffectByElementType()
    {
        if (cardData == null || active == null) yield break;

        int value = cardData.value;
        string elementType = cardData.elementTypeCard.ToUpper();

        switch (elementType)
        {
            case "HEALTH":
                active.MauPlayer = Mathf.Min(active.MauPlayer + value, active.maxMau);
                active.valueCurrent = value;
                active.UpdateSlider();

                if (active.healdHP != null)
                {
                    active.anmtHealP.gameObject.SetActive(true);
                    active.healdHP.SetActive(true);
                    active.healdHP.GetComponent<Text>().text = "+ " + value;
                    yield return active.effect.FadeAndMoveUp(active.healdHP);
                    active.healdHP.SetActive(false);
                }
                break;

            case "MANA":
                active.ManaPlayer = Mathf.Min(active.ManaPlayer + value, active.maxMana);
                active.valueCurrent = value;
                active.UpdateSlider();

                if (active.healdMana != null)
                {
                    active.anmtHealP.gameObject.SetActive(true);
                    active.healdMana.SetActive(true);
                    active.healdMana.GetComponent<Text>().text = "+ " + value;
                    yield return active.effect.FadeAndMoveUp(active.healdMana);
                    active.healdMana.SetActive(false);
                }
                break;

            case "POWER":
                active.NoPlayer = Mathf.Min(active.NoPlayer + value, active.maxNo);
                active.valueCurrent = value;
                active.UpdateSlider();

                if (active.healdPower != null)
                {
                    active.anmtHealP.gameObject.SetActive(true);
                    active.healdPower.SetActive(true);
                    active.healdPower.GetComponent<Text>().text = "+ " + value;
                    yield return active.effect.FadeAndMoveUp(active.healdPower);
                    active.healdPower.SetActive(false);
                }
                break;

            case "ATTACK":
                value += active.attackP;
                active.MauNPC = Mathf.Max(active.MauNPC - value, 0);
                active.valueCurrent = value;
                active.UpdateSlider();

                if (active.playerPetAnimator != null)
                    active.playerPetAnimator.SetInteger("key", 2);

                yield return new WaitForSeconds(1f);

                if (active.dameATKPrefad != null)
                {
                    active.dameATKPrefad.SetActive(true);

                    Text dame = active.dameATKPrefad.transform.Find("txtdame")?.GetComponent<Text>();
                    if (dame != null)
                        dame.text = value.ToString();

                    yield return new WaitForSeconds(0.5f);
                    active.dameATKPrefad.SetActive(false);
                }
                break;
        }
    }

    private IEnumerator CallAPIUseCard()
    {
        if (cardData == null) yield break;

        int userId = PlayerPrefs.GetInt("userId", 1);

        UseCardRequest request = new UseCardRequest
        {
            userId = userId,
            cardId = cardData.cardId,
            quantity = 1
        };

        string apiUrl = APIConfig.USE_CARD;

        bool done = false;

        yield return APIManager.Instance.PostRequest<UseCardResponse>(
            apiUrl,
            request,
            onSuccess: (response) => { done = true; },
            onError: (error) => { done = true; }
        );

        float timeout = 5f;
        float elapsed = 0f;
        while (!done && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private bool CanUseCard()
    {
        if (board == null || active == null || cardData == null) return false;
        if (!active.IsPlayerTurn) return false;
        if (!active.IsTurnInProgress) return false;

        if (IsAttackCard())
        {
            if (hasUsedThisTurn) return false;
        }
        else if (IsBuffCard())
        {
            if (hasUsedThisMatch) return false;
            if (hasUsedBuffThisTurn) return false;
        }

        if (!CheckConditionUse()) return false;

        return true;
    }

    private bool CheckConditionUse()
    {
        if (cardData == null || active == null) return false;

        string elementType = cardData.elementTypeCard.ToUpper();

        if (elementType == "ATTACK_LEGEND" || elementType == "ATTACK_LEGEND_")
        {
            if (active.NoPlayer < cardData.power) return false;
            return true;
        }

        if (cardData.conditionUse <= 0) return true;
        if (active.ManaPlayer < cardData.conditionUse) return false;

        return true;
    }

    private void UpdateCardVisual()
    {
        if (btn == null || imgtCard == null) return;

        if (isPlaceholder)
        {
            btn.interactable = false;
            return;
        }

        bool canUse = CanUseCard();
        btn.interactable = canUse;

        if (!canUse)
        {
            imgtCard.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        }
        else
        {
            imgtCard.color = Color.white;
        }
    }

    private void ConvertToPlaceholder()
    {
        if (imgtCard == null) return;

        isPlaceholder = true;

        if (placeholderSprite != null)
        {
            imgtCard.sprite = placeholderSprite;
        }

        imgtCard.color = placeholderColor;

        if (btn != null)
        {
            btn.interactable = false;
        }

        LeanTween.scale(gameObject, Vector3.one * 0.9f, 0.2f).setEaseOutQuad();
    }

    // private void RestoreOriginalCard()
    // {
    //     if (imgtCard == null) return;

    //     if (originalSprite != null)
    //     {
    //         imgtCard.sprite = originalSprite;
    //     }

    //     imgtCard.color = Color.white;

    //     if (btn != null)
    //     {
    //         btn.interactable = true;
    //     }

    //     gameObject.transform.localScale = Vector3.one;
    // }

    public void ResetForNewMatch()
    {
        hasUsedThisMatch = false;
        hasUsedThisTurn = false;
        lastTurnUsed = -1;
        isPlaceholder = false;

        hasUsedBuffThisTurn = false;
        lastBuffUsedTurn = -1;

        UpdateCardVisual();
    }

    public static void ResetAllCardsForNewMatch()
    {
        hasUsedBuffThisTurn = false;
        lastBuffUsedTurn = -1;
    }

    
}