using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class BossItem : MonoBehaviour
{
    [Header("UI Elements - Tự động tìm nếu không gán")]
    public Image imgBoss;
    public Image imgStatus; // Image để đổi màu theo trạng thái
    public Text txtName;
    public Text txtTime;
    public Text txtStatus;
    public Button btnFight;
    public GameObject upcomingBadge;
    public GameObject endedOverlay;
    public GameObject anmt; // Animation object - hiện khi boss ACTIVE
    
    [Header("Panel Notice")]
    public GameObject panelNotice; // Gán PanelNotice có sẵn vào đây
    public Text txtNoticeMessage; // Gán Text message vào đây (tùy chọn)
    
    private WorldBossDTO bossData;
    private DateTime startTime;
    private DateTime endTime;
    private Transform uiTransform;
    private Outline txtStatusOutline;
    
    // Màu sắc theo trạng thái
    private Color colorActive = new Color(0.18f, 0.63f, 1f);      // #2EA1FF - Đang diễn ra
    private Color colorUpcoming = new Color(1f, 0.92f, 0.18f);    // #FFEA2E - Sắp diễn ra
    private Color colorEnded = new Color(1f, 0.25f, 0f);          // #FF4100 - Kết thúc

    void Awake()
    {
        Debug.Log($"[{gameObject.name}] Awake called");
        
        // Tìm UI transform
        uiTransform = transform.Find("UI");
        if (uiTransform == null)
        {
            Debug.LogError($"[{gameObject.name}] Không tìm thấy UI transform!");
            return;
        }
        
        AutoFindUIComponents();
    }

    void AutoFindUIComponents()
    {
        if (uiTransform == null)
        {
            Debug.LogError($"[{gameObject.name}] uiTransform is null in AutoFindUIComponents");
            return;
        }

        // Tìm Image trong UI
        if (imgStatus == null)
        {
            Transform imgTransform = uiTransform.Find("Image");
            if (imgTransform != null)
            {
                imgStatus = imgTransform.GetComponent<Image>();
                if (imgStatus != null)
                    Debug.Log($"[{gameObject.name}] ✓ Found Image");
                else
                    Debug.LogWarning($"[{gameObject.name}] ✗ Image transform found but no Image component");
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] ✗ Cannot find Image under UI");
            }
        }

        // Tìm txtTime
        if (txtTime == null)
        {
            Transform txtTimeTransform = uiTransform.Find("txtTime");
            if (txtTimeTransform != null)
            {
                txtTime = txtTimeTransform.GetComponent<Text>();
                if (txtTime != null)
                    Debug.Log($"[{gameObject.name}] ✓ Found txtTime");
                else
                    Debug.LogWarning($"[{gameObject.name}] ✗ txtTime transform found but no Text component");
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] ✗ Cannot find txtTime under UI");
            }
        }

        // Tìm txtName
        if (txtName == null)
        {
            Transform txtNameTransform = uiTransform.Find("txtName");
            if (txtNameTransform != null)
            {
                txtName = txtNameTransform.GetComponent<Text>();
                if (txtName != null)
                    Debug.Log($"[{gameObject.name}] ✓ Found txtName");
                else
                    Debug.LogWarning($"[{gameObject.name}] ✗ txtName transform found but no Text component");
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] ✗ Cannot find txtName under UI");
            }
        }

        // Tìm txtStatus
        if (txtStatus == null)
        {
            Transform txtStatusTransform = uiTransform.Find("txtStatus");
            if (txtStatusTransform != null)
            {
                txtStatus = txtStatusTransform.GetComponent<Text>();
                
                if (txtStatus != null)
                {
                    // Thêm Outline
                    txtStatusOutline = txtStatusTransform.GetComponent<Outline>();
                    if (txtStatusOutline == null)
                    {
                        txtStatusOutline = txtStatusTransform.gameObject.AddComponent<Outline>();
                        txtStatusOutline.effectDistance = new Vector2(2, -2);
                    }
                    Debug.Log($"[{gameObject.name}] ✓ Found txtStatus");
                }
                else
                {
                    Debug.LogWarning($"[{gameObject.name}] ✗ txtStatus transform found but no Text component");
                }
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] ✗ Cannot find txtStatus under UI");
            }
        }

        // Tìm imgBoss (Image chính của Boss)
        if (imgBoss == null)
        {
            imgBoss = GetComponent<Image>();
            if (imgBoss == null)
            {
                // Tìm Image đầu tiên không phải trong UI
                Image[] images = GetComponentsInChildren<Image>(true); // Include inactive
                foreach (var img in images)
                {
                    if (img != null && img.transform != uiTransform && img.transform.parent != uiTransform)
                    {
                        imgBoss = img;
                        break;
                    }
                }
            }
            
            if (imgBoss != null)
                Debug.Log($"[{gameObject.name}] ✓ Found imgBoss");
            else
                Debug.LogWarning($"[{gameObject.name}] ✗ Cannot find imgBoss");
        }

        // Tìm Button
        if (btnFight == null)
        {
            btnFight = GetComponentInChildren<Button>(true);
            if (btnFight != null)
                Debug.Log($"[{gameObject.name}] ✓ Found btnFight");
            else
                Debug.LogWarning($"[{gameObject.name}] ✗ Cannot find btnFight");
        }

        // Tìm anmt object (trong UI, cùng cấp với txtTime)
        if (anmt == null)
        {
            Transform anmtTransform = uiTransform.Find("anmt");
            if (anmtTransform != null)
            {
                anmt = anmtTransform.gameObject;
                Debug.Log($"[{gameObject.name}] ✓ Found anmt in UI");
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] ✗ Cannot find anmt in UI");
            }
        }

        Debug.Log($"[{gameObject.name}] Summary - imgStatus: {imgStatus != null}, imgBoss: {imgBoss != null}, txtName: {txtName != null}, txtTime: {txtTime != null}, txtStatus: {txtStatus != null}, btnFight: {btnFight != null}, anmt: {anmt != null}");
    }

    public void SetupBoss(WorldBossDTO boss)
    {
        if (boss == null)
    {
        Debug.LogError($"[{gameObject.name}] SetupBoss: boss data is NULL!");
        return;
    }

    // THÊM ĐOẠN NÀY: Kiểm tra và tìm lại components nếu null
    if (txtName == null || txtTime == null || txtStatus == null)
    {
        Debug.LogWarning($"[{gameObject.name}] UI components are NULL, re-finding...");
        
        if (uiTransform == null)
        {
            uiTransform = transform.Find("UI");
        }
        
        if (uiTransform != null)
        {
            AutoFindUIComponents();
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] Cannot find UI transform!");
            return;
        }
    }

    Debug.Log($"[{gameObject.name}] SetupBoss: {boss.bossName}");
    
    bossData = boss;
        
        // Load sprite
        LoadBossSprite(boss.petId);
        
        // Set tên boss
        if (txtName != null)
        {
            txtName.text = boss.bossName;
            Debug.Log($"[{gameObject.name}] ✓ Set name: {boss.bossName}");
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] ✗ txtName is NULL!");
        }
        
        // Parse thời gian
        try
        {
            if (!string.IsNullOrEmpty(boss.startTime))
                startTime = DateTime.Parse(boss.startTime);
            else
                Debug.LogWarning($"[{gameObject.name}] startTime is empty");

            if (!string.IsNullOrEmpty(boss.endTime))
                endTime = DateTime.Parse(boss.endTime);
            else
                Debug.LogWarning($"[{gameObject.name}] endTime is empty");
        }
        catch (Exception e)
        {
            Debug.LogError($"[{gameObject.name}] Error parsing time: {e.Message}");
            return;
        }
        
        // Setup button
        if (btnFight != null)
        {
            btnFight.onClick.RemoveAllListeners();
            btnFight.onClick.AddListener(() => OnFightClicked());
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] btnFight is null, cannot setup click listener");
        }
        
        UpdateUI();
    }

    void LoadBossSprite(long petId)
    {
        if (imgBoss == null)
        {
            Debug.LogWarning($"[{gameObject.name}] imgBoss is null, cannot load sprite");
            return;
        }

        string spritePath = "Image/IconsPet/" + petId;
        Sprite bossSprite = Resources.Load<Sprite>(spritePath);
        
        if (bossSprite != null)
        {
            imgBoss.sprite = bossSprite;
            Debug.Log($"[{gameObject.name}] ✓ Loaded sprite: {spritePath}");
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] ✗ Cannot load sprite: {spritePath}");
        }
    }

    public void UpdateCountdown()
    {
        if (bossData == null)
        {
            Debug.LogWarning($"[{gameObject.name}] UpdateCountdown: bossData is null");
            return;
        }
        
        DateTime now = DateTime.Now;
        
        if (now < startTime)
        {
            bossData.status = "UPCOMING";
            TimeSpan timeUntilStart = startTime - now;
            
            if (txtTime != null)
            {
                txtTime.text = FormatTimeSpan(timeUntilStart);
            }
        }
        else if (now > endTime)
        {
            bossData.status = "ENDED";
            if (txtTime != null)
            {
                txtTime.text = "Kết thúc";
            }
        }
        else
        {
            bossData.status = "ACTIVE";
            TimeSpan timeUntilEnd = endTime - now;
            
            if (txtTime != null)
            {
                txtTime.text = FormatTimeSpan(timeUntilEnd);
            }
        }
        
        UpdateUI();
    }

    void UpdateUI()
    {
        if (bossData == null)
        {
            Debug.LogWarning($"[{gameObject.name}] UpdateUI: bossData is null");
            return;
        }

        // Ẩn/hiện badges
        if (upcomingBadge != null)
            upcomingBadge.SetActive(bossData.status == "UPCOMING");
        
        if (endedOverlay != null)
            endedOverlay.SetActive(bossData.status == "ENDED");
        
        // Hiện/ẩn anmt - CHỈ hiện khi boss ACTIVE
        if (anmt != null)
            anmt.SetActive(bossData.status == "ACTIVE");
        
        // Enable/Disable button - KHÔNG DISABLE NỮA, cho phép click để hiện thông báo
        if (btnFight != null)
        {
            // Luôn enable button để có thể click
            btnFight.interactable = true;
        }
        
        // Đổi màu theo trạng thái
        switch (bossData.status)
        {
            case "UPCOMING":
                if (imgStatus != null)
                    imgStatus.color = colorUpcoming;
                    
                if (txtStatus != null)
                {
                    txtStatus.text = "Sắp diễn ra";
                    txtStatus.color = Color.white;
                }
                
                if (txtStatusOutline != null)
                    txtStatusOutline.effectColor = colorUpcoming;
                break;
                
            case "ACTIVE":
                if (imgStatus != null)
                    imgStatus.color = colorActive;
                    
                if (txtStatus != null)
                {
                    txtStatus.text = "Đang diễn ra";
                    txtStatus.color = Color.white;
                }
                
                if (txtStatusOutline != null)
                    txtStatusOutline.effectColor = colorActive;
                break;
                
            case "ENDED":
                if (imgStatus != null)
                    imgStatus.color = colorEnded;
                    
                if (txtStatus != null)
                {
                    txtStatus.text = "Đã kết thúc";
                    txtStatus.color = Color.white;
                }
                
                if (txtStatusOutline != null)
                    txtStatusOutline.effectColor = colorEnded;
                break;
        }
    }

    string FormatTimeSpan(TimeSpan time)
    {
        if (time.TotalDays >= 1)
            return $"{(int)time.TotalDays}d {time.Hours}h";
        else if (time.TotalHours >= 1)
            return $"{time.Hours}h {time.Minutes}m";
        else if (time.TotalMinutes >= 1)
            return $"{time.Minutes}m {time.Seconds}s";
        else
            return $"{time.Seconds}s";
    }

    void OnFightClicked()
{
    if (bossData == null)
    {
        Debug.Log($"[{gameObject.name}] Boss data is null!");
        return;
    }

    // Kiểm tra trạng thái boss
    if (bossData.status == "UPCOMING")
    {
        ShowNotice("Chưa tới thời gian đánh!");
        Debug.Log($"[{gameObject.name}] Boss {bossData.bossName} chưa tới thời gian");
        return;
    }
    
    if (bossData.status == "ENDED")
    {
        ShowNotice("Boss đã kết thúc!");
        Debug.Log($"[{gameObject.name}] Boss {bossData.bossName} đã kết thúc");
        return;
    }
    
    if (bossData.remainingAttempts <= 0)
    {
        ShowNotice("Bạn đã hết lượt đánh!");
        Debug.Log($"[{gameObject.name}] Hết lượt đánh boss {bossData.bossName}");
        return;
    }
    
    if (bossData.status == "ACTIVE")
    {
        // ✅ SET FLAG BOSS BATTLE TRƯỚC KHI VÀO MATCH
        PlayerPrefs.SetString("IsBossBattle", "true");
        
        // Lưu thông tin boss vào PlayerPrefs
        PlayerPrefs.SetInt("CurrentBossId", (int)bossData.id);
        PlayerPrefs.SetInt("SelectedPetId", (int)bossData.petId);
        
        // ✅ MỚI: Lưu element type của boss để dùng cho reward
        PlayerPrefs.SetString("BossElementType", bossData.elementType);
        
        // Lấy pet hiện tại của user
        int userPetId = PlayerPrefs.GetInt("userPetId", 1);
        
        // ✅ SAVE TOÀN BỘ TRƯỚC KHI CHUYỂN SCENE
        PlayerPrefs.Save();
        
        Debug.Log($"[{gameObject.name}] ========================================");
        Debug.Log($"[{gameObject.name}] STARTING BOSS BATTLE");
        Debug.Log($"[{gameObject.name}] Boss Name: {bossData.bossName}");
        Debug.Log($"[{gameObject.name}] Boss PetId: {bossData.petId}");
        Debug.Log($"[{gameObject.name}] Boss Element: {bossData.elementType}");
        Debug.Log($"[{gameObject.name}] User PetId: {userPetId}");
        Debug.Log($"[{gameObject.name}] IsBossBattle: true");
        Debug.Log($"[{gameObject.name}] ========================================");
        
        // Load scene đấu Boss
        UnityEngine.SceneManagement.SceneManager.LoadScene("Match");
    }
}

    void ShowNotice(string message)
    {
        if (panelNotice == null)
        {
            Debug.LogError($"[{gameObject.name}] PanelNotice chưa được gán trong Inspector!");
            return;
        }
        
        // Hiện PanelNotice
        panelNotice.SetActive(true);
        
        // Animation xuất hiện với DOTween
        RectTransform panelRect = panelNotice.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            // Reset scale về 0
            panelRect.localScale = Vector3.zero;
            
            // Scale từ 0 -> 1 với hiệu ứng bounce
            panelRect.DOScale(1f, 0.3f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true); // Không bị ảnh hưởng bởi Time.timeScale
        }
        
        // Tìm và setup button btnGet
        Transform btnGetTransform = panelNotice.transform.Find("UI/btnGet");
        if (btnGetTransform != null)
        {
            Button btnGet = btnGetTransform.GetComponent<Button>();
            if (btnGet != null)
            {
                // Xóa listener cũ và thêm listener mới
                btnGet.onClick.RemoveAllListeners();
                btnGet.onClick.AddListener(() => CloseNotice());
                Debug.Log($"[{gameObject.name}] ✓ Setup btnGet listener");
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] btnGet transform found but no Button component");
            }
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] Cannot find btnGet at UI/btnGet");
        }
        
        // Cập nhật message
        if (txtNoticeMessage != null)
        {
            // Nếu đã gán txtNoticeMessage trong Inspector
            txtNoticeMessage.text = message;
            Debug.Log($"[{gameObject.name}] Hiển thị thông báo: {message}");
        }
        else
        {
            // Nếu chưa gán, tự động tìm theo path
            Transform messageTransform = panelNotice.transform.Find("UI/btnGet/message");
            if (messageTransform != null)
            {
                Text messageText = messageTransform.GetComponent<Text>();
                if (messageText != null)
                {
                    messageText.text = message;
                    Debug.Log($"[{gameObject.name}] Hiển thị thông báo: {message}");
                }
                else
                {
                    Debug.LogWarning($"[{gameObject.name}] Không tìm thấy Text component trong message");
                }
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] Không tìm thấy message transform tại UI/btnGet/message");
            }
        }
    }
    
    void CloseNotice()
    {
        if (panelNotice != null)
        {
            RectTransform panelRect = panelNotice.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                // Scale từ 1 -> 0 với hiệu ứng smooth
                panelRect.DOScale(0f, 0.2f)
                    .SetEase(Ease.InBack)
                    .SetUpdate(true)
                    .OnComplete(() => 
                    {
                        // Tắt panel sau khi animation xong
                        panelNotice.SetActive(false);
                        Debug.Log($"[{gameObject.name}] PanelNotice closed");
                    });
            }
            else
            {
                // Nếu không có RectTransform thì tắt trực tiếp
                panelNotice.SetActive(false);
                Debug.Log($"[{gameObject.name}] PanelNotice closed (no animation)");
            }
        }
    }
}