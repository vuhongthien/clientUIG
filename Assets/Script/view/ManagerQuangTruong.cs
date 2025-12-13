using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using EasyUI.PickerWheelUI;

public class ManagerQuangTruong : MonoBehaviour
{
    public GameObject petUIPrefab;
    public Transform petListContainer;
    public GameObject banner;
    public Text namePet;
    public Text txtHp;
    public Text txtMana;
    public Text txtDame;
    public Text txtWee;
    public Text txtLv;
    public Text des;
    public Image imgAtribute;
    public Image flagWheel;
    public Image imgAtributeOther;
    public Text txtVang;
    public Text txtCt;
    public int txtCtint;
    public Text txtNl;
    public Image imgLvUser;
    private string lvUser;
    public Text txtExp;
    public Text txtName;
    public string petId;
    public Slider expslider;
    public Text txtStarWhite;
    public Text txtStarBlue;
    public Text txtStarRed;
    public Button btnBoss;
    public GameObject panelBoss;
    public static ManagerQuangTruong Instance;

    [Header("Energy UI")]
    public Text txtEnergy;
    public Text txtCountdown;
    public Image imgEnergyBar;
    [Header("Wheel Day")]
    public Button btnWheelDay;

    [Header("Stone Images - 5 Hệ, mỗi hệ 7 Level")]
    [Tooltip("Hệ Lửa - 7 level")]
    public Sprite[] stoneFire = new Sprite[7];

    [Tooltip("Hệ Nước - 7 level")]
    public Sprite[] stoneWater = new Sprite[7];

    [Tooltip("Hệ Gió - 7 level")]
    public Sprite[] stoneWind = new Sprite[7];

    [Tooltip("Hệ Đất - 7 level")]
    public Sprite[] stoneEarth = new Sprite[7];

    [Tooltip("Hệ Sét - 7 level")]
    public Sprite[] stoneThunder = new Sprite[7];

    // ========================================
    // GIFTBOX INTEGRATION
    // ========================================

    [Header("GiftBox - Main Panels")]
    public GameObject panelGiftBox;
    public GameObject panelGiftResult;

    [Header("GiftBox - UI Elements")]
    public Text txtGiftTitle;
    public Button btnClaimGift;
    public Transform listReward;

    [Header("GiftBox - Reward Prefabs")]
    public GameObject petRW;
    public GameObject cardRW;
    public GameObject stoneRW;
    public GameObject goldRW;
    public GameObject energyRW;
    public GameObject redStarRW;
    public GameObject whiteStarRW;
    public GameObject bluestarRW;
    public GameObject expRW;
    public GameObject wheelRW;

    [Header("GiftBox - Optional")]
    public GameObject giftBoxIcon;
    public Text txtGiftCount;
    public GameObject giftBoxAnimation;
    public Animator giftBoxAnimator;
    private bool isClaimingGift = false;

    // Private GiftBox variables
    private List<GiftDTO> pendingGifts = new List<GiftDTO>();
    private GiftDTO currentGift;

    [Header("Shop Button")]
    public Button btnShop;
    [Header("Chinh Phuc Panel")]
    public GameObject panelChinhPhuc; // Assign panel Chinh Phuc trong Inspector
    public Button btnChinhPhuc;
    public GameObject loadingPanel;
    public GameObject loadewqingPanel;
    public ManagerWheelDay managerWheelDay;
    private int ruby;
    [Header("Equipment")]
    public Button btnEquipment;
    public ManagerEquipment managerEquipment;
    public Image HC;
    public Image imgAvatar;

    [Header("Background Music")]
    public AudioSource bgmAudioSource;
    public AudioClip bgmClip;
    [Range(0f, 1f)]
    public float bgmVolume = 0.5f;
    public bool loopBGM = true;
    [Header("Sound Effects")]
    public AudioClip clickSound;
    [Range(0f, 1f)]
    public float clickVolume = 0.7f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Debug.Log("QuangTruong--Start");
        SetupButtonSounds();
        PlayBackgroundMusic();
        PlayerPrefs.DeleteKey("ActivePanelIndex");
        PlayerPrefs.Save();

        if (btnBoss != null)
        {
            btnBoss.onClick.AddListener(ShowPanelBoss);
        }

        if (panelBoss != null)
        {
            panelBoss.SetActive(false);
        }
        if (btnEquipment != null)
        {
            btnEquipment.onClick.AddListener(OpenEquipment);
        }

        // Setup GiftBox
        InitializeGiftBox();
        if (btnShop != null)
        {
            btnShop.onClick.AddListener(OpenShop);
        }

        if (btnChinhPhuc != null)
        {
            btnChinhPhuc.onClick.AddListener(OpenChinhPhucPanel);
        }

        // Ẩn panel Chinh Phục ban đầu
        if (panelChinhPhuc != null)
        {
            panelChinhPhuc.SetActive(false);
        }
        if (btnWheelDay != null)
        {
            btnWheelDay.onClick.AddListener(OpenWheelDay);
        }

        StartCoroutine(LoadSceneAfterDelay());
    }




    /// <summary>
    /// Play background music
    /// </summary>
    void PlayBackgroundMusic()
    {
        if (bgmAudioSource == null)
        {
            // Tạo AudioSource nếu chưa có
            bgmAudioSource = gameObject.AddComponent<AudioSource>();
            Debug.Log("[BGM] Created new AudioSource");
        }

        if (bgmClip == null)
        {
            Debug.LogWarning("[BGM] No AudioClip assigned!");
            return;
        }

        // Setup AudioSource
        bgmAudioSource.clip = bgmClip;
        bgmAudioSource.volume = bgmVolume;
        bgmAudioSource.loop = loopBGM;
        bgmAudioSource.playOnAwake = false;

        // Play music
        bgmAudioSource.Play();
        Debug.Log($"[BGM] Playing: {bgmClip.name} (Volume: {bgmVolume})");

    }
    void SetupButtonSounds()
    {
        if (clickSound == null)
        {
            Debug.LogWarning("[Sound] Click sound not assigned!");
            return;
        }

        // Set static variables
        ButtonClickSound.clickSound = clickSound;

        // Tìm TẤT CẢ buttons trong scene (kể cả button đang bị ẩn)
        Button[] allButtons = FindObjectsOfType<Button>(true); // true = include inactive

        int count = 0;
        foreach (Button btn in allButtons)
        {
            // Kiểm tra xem đã có component chưa
            ButtonClickSound clickSoundComponent = btn.GetComponent<ButtonClickSound>();

            if (clickSoundComponent == null)
            {
                // Thêm component nếu chưa có
                clickSoundComponent = btn.gameObject.AddComponent<ButtonClickSound>();
                clickSoundComponent.volume = clickVolume;
                count++;
            }
        }

        Debug.Log($"[Sound] Added click sound to {count} buttons");
    }

    /// <summary>
    /// Stop background music
    /// </summary>
    public void StopBackgroundMusic()
    {
        if (bgmAudioSource != null && bgmAudioSource.isPlaying)
        {
            bgmAudioSource.Stop();
            Debug.Log("[BGM] Stopped");
        }
    }

    /// <summary>
    /// Pause background music
    /// </summary>
    public void PauseBackgroundMusic()
    {
        if (bgmAudioSource != null && bgmAudioSource.isPlaying)
        {
            bgmAudioSource.Pause();
            Debug.Log("[BGM] Paused");
        }
    }

    /// <summary>
    /// Resume background music
    /// </summary>
    public void ResumeBackgroundMusic()
    {
        if (bgmAudioSource != null && !bgmAudioSource.isPlaying)
        {
            bgmAudioSource.UnPause();
            Debug.Log("[BGM] Resumed");
        }
    }

    /// <summary>
    /// Set volume
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = bgmVolume;
            Debug.Log($"[BGM] Volume set to: {bgmVolume}");
        }
    }

    /// <summary>
    /// Fade in background music
    /// </summary>
    public void FadeInBGM(float duration = 2f)
    {
        if (bgmAudioSource == null) return;

        bgmAudioSource.volume = 0f;
        bgmAudioSource.Play();

        LeanTween.value(gameObject, 0f, bgmVolume, duration)
            .setOnUpdate((float val) =>
            {
                if (bgmAudioSource != null)
                    bgmAudioSource.volume = val;
            })
            .setEase(LeanTweenType.easeInOutQuad);

        Debug.Log($"[BGM] Fading in over {duration}s");
    }

    /// <summary>
    /// Fade out background music
    /// </summary>
    public void FadeOutBGM(float duration = 2f, bool stopAfterFade = true)
    {
        if (bgmAudioSource == null) return;

        LeanTween.value(gameObject, bgmAudioSource.volume, 0f, duration)
            .setOnUpdate((float val) =>
            {
                if (bgmAudioSource != null)
                    bgmAudioSource.volume = val;
            })
            .setOnComplete(() =>
            {
                if (stopAfterFade && bgmAudioSource != null)
                {
                    bgmAudioSource.Stop();
                }
            })
            .setEase(LeanTweenType.easeInOutQuad);

        Debug.Log($"[BGM] Fading out over {duration}s");
    }
    public void OpenWheelDay()
    {
        if (managerWheelDay != null)
        {
            managerWheelDay.OpenWheelPanel();
        }
    }

    /// <summary>
    /// Mở panel trang bị
    /// </summary>
    public void OpenEquipment()
    {
        if (managerEquipment != null)
        {
            managerEquipment.OpenEquipmentPanel();
        }
        else
        {
            Debug.LogError("[ManagerQuangTruong] ManagerEquipment is null!");
        }
    }

    /// <summary>
    /// Mở panel Chinh Phục và load data
    /// </summary>
    public void OpenChinhPhucPanel()
    {
        if (panelChinhPhuc == null)
        {
            Debug.LogError("[ManagerQuangTruong] panelChinhPhuc is null!");
            return;
        }

        Debug.Log("[ManagerQuangTruong] Opening Chinh Phuc panel...");

        // ✅ SHOW LOADING NGAY KHI NHẤN BUTTON
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }
        ManagerGame.Instance.ShowLoading();

        // Hiện panel với animation
        panelChinhPhuc.SetActive(true);
        panelChinhPhuc.transform.localScale = Vector3.zero;

        LeanTween.scale(panelChinhPhuc, Vector3.one, 0.5f)
            .setEase(LeanTweenType.easeOutCubic)
            .setOnComplete(() =>
            {
                // Sau khi animation xong, load data
                ManagerChinhPhuc chinhPhucManager = panelChinhPhuc.GetComponent<ManagerChinhPhuc>();
                if (chinhPhucManager != null)
                {
                    // ✅ InitializeAndLoadData sẽ tự hide loading khi load xong
                    chinhPhucManager.InitializeAndLoadData();
                }
                else
                {
                    Debug.LogError("[ManagerQuangTruong] ManagerChinhPhuc component not found!");

                    // ✅ NẾU LỖI THÌ HIDE LOADING
                    ManagerGame.Instance.HideLoading();
                    if (loadingPanel != null)
                    {
                        loadingPanel.SetActive(false);
                    }
                }
            });
    }

    /// <summary>
    /// Mở cửa hàng
    /// </summary>
    public void OpenShop()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OpenShop();
        }
        else
        {
            Debug.LogError("[ManagerQuangTruong] ShopManager.Instance is null!");
        }
    }

    // ========================================
    // GIFTBOX METHODS
    // ========================================

    void InitializeGiftBox()
    {
        Debug.Log("[GiftBox] Initializing...");

        // Setup button
        if (btnClaimGift != null)
        {
            btnClaimGift.onClick.AddListener(ClaimCurrentGift);
            ColorBlock colors = btnClaimGift.colors;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Xám mờ
            btnClaimGift.colors = colors;
        }

        // Hide panels
        if (panelGiftBox != null) panelGiftBox.SetActive(false);
        if (panelGiftResult != null) panelGiftResult.SetActive(false);
        if (giftBoxAnimation != null) giftBoxAnimation.SetActive(false);

        // Clear ListReward nếu có items cũ
        ClearListReward();
    }

    public void CheckForGifts(int userId)
    {
        Debug.Log("═══════════════════════════════════════");
        Debug.Log($"[GiftBox] CheckForGifts() userId={userId}");
        Debug.Log("═══════════════════════════════════════");

        if (userId == 0)
        {
            Debug.LogError("[GiftBox] userId = 0!");
            return;
        }

        if (APIManager.Instance == null)
        {
            Debug.LogError("[GiftBox] APIManager.Instance NULL!");
            return;
        }

        StartCoroutine(CheckGiftsCoroutine(userId));
    }

    IEnumerator CheckGiftsCoroutine(int userId)
    {
        Debug.Log("[GiftBox] → Coroutine started");

        string countUrl = APIConfig.GET_GIFT_COUNT(userId);
        Debug.Log($"[GiftBox] → URL: {countUrl}");

        bool apiCompleted = false;
        GiftCountResponse responseData = null;
        string errorMsg = null;

        yield return APIManager.Instance.GetRequest<GiftCountResponse>(
            countUrl,
            (response) =>
            {
                Debug.Log("[GiftBox] → API Success");
                apiCompleted = true;
                responseData = response;
            },
            (error) =>
            {
                Debug.LogError($"[GiftBox] → API Error: {error}");
                apiCompleted = true;
                errorMsg = error;
            }
        );

        if (!apiCompleted || errorMsg != null || responseData == null)
        {
            Debug.LogError("[GiftBox] API failed or returned null");
            yield break;
        }

        Debug.Log($"[GiftBox] ✓ Gift count: {responseData.count}");
        OnGiftCountReceived(responseData, userId);
    }

    void OnGiftCountReceived(GiftCountResponse response, int userId)
    {
        int count = response.count;
        Debug.Log($"[GiftBox] User has {count} pending gifts");

        if (count > 0)
        {
            if (giftBoxIcon != null)
            {
                giftBoxIcon.SetActive(true);

                if (txtGiftCount != null)
                {
                    txtGiftCount.text = count.ToString();
                }

                PlayGiftNotificationAnimation();
            }

            // CHỈ load data, KHÔNG tự động hiện panel
            StartCoroutine(LoadGiftDetailsCoroutine(userId));
        }
        else
        {
            Debug.Log("[GiftBox] No pending gifts");
            if (giftBoxIcon != null)
            {
                giftBoxIcon.SetActive(false);
            }
        }
    }

    IEnumerator LoadGiftDetailsCoroutine(int userId)
    {
        string url = APIConfig.GET_PENDING_GIFTS(userId);
        Debug.Log($"[GiftBox] → Loading details: {url}");

        List<GiftDTO> giftsData = null;

        yield return APIManager.Instance.GetRequest<List<GiftDTO>>(
            url,
            (gifts) =>
            {
                giftsData = gifts;
                Debug.Log($"[GiftBox] ✓ Loaded {gifts.Count} gifts");
            },
            (error) =>
            {
                Debug.LogError($"[GiftBox] ❌ Load error: {error}");
            }
        );

        if (giftsData != null && giftsData.Count > 0)
        {
            pendingGifts = giftsData;
            // KHÔNG tự động show - chờ user click icon
            Debug.Log("[GiftBox] ✓ Gifts ready - waiting for user to click icon");
        }
    }

    void PlayGiftNotificationAnimation()
    {
        if (giftBoxIcon != null)
        {
            giftBoxIcon.transform.DOScale(1.2f, 0.3f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
    }

    public void ShowGiftBoxAnimation(GiftDTO gift)
    {
        Debug.Log($"[GiftBox] → Showing: {gift.title}");
        currentGift = gift;

        if (giftBoxAnimation != null && giftBoxAnimator != null)
        {
            giftBoxAnimation.SetActive(true);
            giftBoxAnimator.SetTrigger("Open");
            StartCoroutine(ShowGiftDetailAfterAnimation(2f));
        }
        else
        {
            ShowGiftDetail(gift);
        }
    }

    IEnumerator ShowGiftDetailAfterAnimation(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (giftBoxAnimation != null)
        {
            giftBoxAnimation.SetActive(false);
        }

        ShowGiftDetail(currentGift);
    }

    void ShowGiftDetail(GiftDTO gift)
    {
        Debug.Log($"[GiftBox] ShowGiftDetail: {gift.title}");

        if (panelGiftBox == null || panelGiftResult == null)
        {
            Debug.LogError("[GiftBox] Panels NULL!");
            return;
        }

        panelGiftBox.SetActive(true);
        panelGiftResult.SetActive(true);

        if (txtGiftTitle != null)
        {
            txtGiftTitle.text = gift.title;
        }

        // Display rewards (sẽ tự động clear và instantiate)
        DisplayRewards(gift);

        if (panelGiftResult != null)
        {
            panelGiftResult.transform.localScale = Vector3.zero;
            panelGiftResult.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
        }
    }

    void DisplayRewards(GiftDTO gift)
    {
        // Clear ListReward trước
        ClearListReward();

        // Add rewards vào ListReward
        if (gift.gold > 0 && goldRW != null)
        {
            GameObject goldItem = Instantiate(goldRW, listReward);
            goldItem.SetActive(true);
            UpdateRewardUI(goldItem, gift.gold);
        }

        if (gift.energy > 0 && energyRW != null)
        {
            GameObject energyItem = Instantiate(energyRW, listReward);
            energyItem.SetActive(true);
            UpdateRewardUI(energyItem, gift.energy);
        }

        if (gift.exp > 0 && expRW != null)
        {
            GameObject expItem = Instantiate(expRW, listReward);
            expItem.SetActive(true);
            UpdateRewardUI(expItem, gift.exp);
        }

        if (gift.starWhite > 0 && whiteStarRW != null)
        {
            GameObject whiteItem = Instantiate(whiteStarRW, listReward);
            whiteItem.SetActive(true);
            UpdateRewardUI(whiteItem, gift.starWhite);
        }

        if (gift.starBlue > 0 && bluestarRW != null)
        {
            GameObject blueItem = Instantiate(bluestarRW, listReward);
            blueItem.SetActive(true);
            UpdateRewardUI(blueItem, gift.starBlue);
        }

        if (gift.starRed > 0 && redStarRW != null)
        {
            GameObject redItem = Instantiate(redStarRW, listReward);
            redItem.SetActive(true);
            UpdateRewardUI(redItem, gift.starRed);
        }

        if (gift.wheel > 0 && wheelRW != null)
        {
            GameObject wheelItem = Instantiate(wheelRW, listReward);
            wheelItem.SetActive(true);
            UpdateRewardUI(wheelItem, gift.wheel);
        }

        if (gift.petId > 0 && petRW != null)
        {
            GameObject petItem = Instantiate(petRW, listReward);
            petItem.SetActive(true);
            UpdatePetRewardUI(petItem, gift.petId, gift.petName);
        }

        if (gift.cardId > 0 && cardRW != null)
        {
            GameObject cardItem = Instantiate(cardRW, listReward);
            cardItem.SetActive(true);
            UpdateCardRewardUI(cardItem, gift.cardId, gift.cardName);
        }

        // Multiple stones
        if (gift.stones != null && gift.stones.Length > 0 && stoneRW != null)
        {
            foreach (var stone in gift.stones)
            {
                GameObject stoneItem = Instantiate(stoneRW, listReward);
                stoneItem.SetActive(true);
                UpdateStoneRewardUI(stoneItem, stone); // Pass entire stone object
            }
        }

        Debug.Log($"[GiftBox] DisplayRewards complete");
    }

    /// <summary>
    /// Clear tất cả rewards trong ListReward
    /// </summary>
    void ClearListReward()
    {
        if (listReward == null) return;

        // Destroy tất cả children
        foreach (Transform child in listReward)
        {
            Destroy(child.gameObject);
        }

        Debug.Log("[GiftBox] ListReward cleared");
    }

    void UpdateRewardUI(GameObject rewardObj, int amount)
    {
        Text txtCount = rewardObj.transform.Find("txtCount")?.GetComponent<Text>();
        if (txtCount != null)
        {
            txtCount.text = "x" + amount.ToString();
        }
    }

    void UpdatePetRewardUI(GameObject petObj, int petId, string petName)
    {
        Image imgPet = petObj.transform.Find("imgtPet")?.GetComponent<Image>();
        Text txtLv = petObj.transform.Find("txtLv")?.GetComponent<Text>();

        if (imgPet != null)
        {
            Sprite petSprite = Resources.Load<Sprite>("Image/IconsPet/" + petId);
            if (petSprite != null) imgPet.sprite = petSprite;
        }

        if (txtLv != null)
        {
            txtLv.text = petName ?? "Pet";
        }
    }

    void UpdateCardRewardUI(GameObject cardObj, int cardId, string cardName)
    {
        Image imgCard = cardObj.transform.Find("imgCard")?.GetComponent<Image>();
        Text txtCount = cardObj.transform.Find("txtCount")?.GetComponent<Text>();

        if (imgCard != null)
        {
            Sprite cardSprite = Resources.Load<Sprite>("Image/Cards/" + cardId);
            if (cardSprite != null) imgCard.sprite = cardSprite;
        }

        if (txtCount != null)
        {
            txtCount.text = cardName ?? "Card";
        }
    }

    void UpdateStoneRewardUI(GameObject stoneObj, StoneRewardDTO stone)
    {
        Debug.Log($"[Stone] Updating: Type={stone.elementType}, Level={stone.level}, Count={stone.count}");

        Image imgCard = stoneObj.transform.Find("imgStone")?.GetComponent<Image>();
        Text txtCount = stoneObj.transform.Find("txtCount")?.GetComponent<Text>();

        if (imgCard != null)
        {
            Sprite stoneSprite = GetStoneSpriteByTypeAndLevel(stone.elementType, stone.level);
            if (stoneSprite != null)
            {
                imgCard.sprite = stoneSprite;
                Debug.Log($"[Stone] ✓ Sprite assigned: {stoneSprite.name}");
            }
            else
            {
                Debug.LogError($"[Stone] ❌ Sprite NULL: {stone.elementType} Lv{stone.level}");
            }
        }
        else
        {
            Debug.LogError("[Stone] imgCard component NOT FOUND!");
        }

        if (txtCount != null)
        {
            txtCount.text = "x" + stone.count.ToString();
        }
    }

    /// <summary>
    /// Lấy sprite của đá dựa trên elementType và level
    /// </summary>
    Sprite GetStoneSpriteByTypeAndLevel(string elementType, int level)
    {
        Debug.Log($"[GetStoneSprite] Input: Type={elementType}, Level={level}");

        if (level < 1 || level > 7)
        {
            Debug.LogError($"[GiftBox] Invalid stone level: {level}");
            return null;
        }

        int index = level - 1;
        Sprite result = null;

        switch (elementType)
        {
            case "FIRE":
                result = stoneFire != null && stoneFire.Length > index ? stoneFire[index] : null;
                Debug.Log($"[GetStoneSprite] Fire array: Length={stoneFire?.Length}, Index={index}, Found={result != null}");
                break;

            case "WATER":
                result = stoneWater != null && stoneWater.Length > index ? stoneWater[index] : null;
                Debug.Log($"[GetStoneSprite] Water array: Length={stoneWater?.Length}, Index={index}, Found={result != null}");
                break;

            case "WOOD":
                result = stoneWind != null && stoneWind.Length > index ? stoneWind[index] : null;
                break;

            case "EARTH":
                result = stoneEarth != null && stoneEarth.Length > index ? stoneEarth[index] : null;
                break;

            case "METAL":
                result = stoneThunder != null && stoneThunder.Length > index ? stoneThunder[index] : null;
                break;

            default:
                Debug.LogWarning($"[GiftBox] Unknown element type: {elementType}");
                break;
        }

        return result;
    }

    public void ClaimCurrentGift()
    {
        // 🔒 LOCK - Ngăn click nhiều lần
        if (isClaimingGift)
        {
            Debug.LogWarning("[GiftBox] Already claiming, please wait!");
            return;
        }

        if (currentGift == null)
        {
            Debug.LogError("[GiftBox] No gift to claim!");
            return;
        }

        Debug.Log($"[GiftBox] Claiming: {currentGift.title}");

        // 🔒 SET FLAG
        isClaimingGift = true;

        // 🚫 DISABLE BUTTON
        if (btnClaimGift != null)
        {
            btnClaimGift.interactable = false;
        }

        // ManagerGame.Instance.ShowLoading();

        int userId = PlayerPrefs.GetInt("userId", 0);
        StartCoroutine(ClaimGiftCoroutine(currentGift.id, userId));
    }

    IEnumerator ClaimGiftCoroutine(int giftId, int userId)
    {
        string url = APIConfig.CLAIM_GIFT(giftId, userId);
        Debug.Log($"[GiftBox] → Claim URL: {url}");

        yield return APIManager.Instance.PostRequest<GiftDTO>(
            url,
            null,
            OnGiftClaimed,
            OnGiftClaimError
        );
    }

    void OnGiftClaimed(GiftDTO claimedGift)
    {
        // ManagerGame.Instance.HideLoading();
        // Debug.Log($"[GiftBox] ✓ Claimed: {claimedGift.title}");

        isClaimingGift = false;

        if (btnClaimGift != null)
        {
            btnClaimGift.interactable = true;
        }

        pendingGifts.Remove(currentGift);

        if (giftBoxIcon != null && txtGiftCount != null)
        {
            if (pendingGifts.Count > 0)
            {
                txtGiftCount.text = pendingGifts.Count.ToString();
            }
            else
            {
                giftBoxIcon.SetActive(false);
            }
        }
        if (claimedGift.energy > 0 && EnergyManager.Instance != null)
        {
            EnergyManager.Instance.RefreshEnergyFromServer();
        }

        // ✅ Đóng panel với animation
        CloseGiftBoxWithAnimation();
    }

    void CloseGiftBoxWithAnimation()
    {
        if (panelGiftResult != null)
        {
            // Scale + Fade cùng lúc
            LeanTween.scale(panelGiftResult, Vector3.zero, 0.3f)
                .setEase(LeanTweenType.easeInBack);

            CanvasGroup canvasGroup = panelGiftBox.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = panelGiftBox.AddComponent<CanvasGroup>();

            LeanTween.alphaCanvas(canvasGroup, 0f, 0.3f)
                .setOnComplete(() =>
                {
                    panelGiftBox.SetActive(false);
                    panelGiftResult.SetActive(false);
                    canvasGroup.alpha = 1f;
                    ClearListReward();

                    // ✅ Refresh SAU KHI đóng hoàn toàn
                    RefreshUserInfo(true);
                });
        }
        else
        {
            CloseGiftBox();
            RefreshUserInfo(true);
        }
    }


    void OnGiftClaimError(string error)
    {
        ManagerGame.Instance.HideLoading();
        Debug.LogError($"[GiftBox] Claim error: {error}");

        // 🔓 UNLOCK khi có lỗi
        isClaimingGift = false;

        // ✅ RE-ENABLE BUTTON
        if (btnClaimGift != null)
        {
            btnClaimGift.interactable = true;
        }
    }

    public void CloseGiftBox()
    {
        if (panelGiftBox != null) panelGiftBox.SetActive(false);
        if (panelGiftResult != null) panelGiftResult.SetActive(false);
        ClearListReward(); // Clear thay vì hide
    }

    public void OnGiftIconClicked()
    {
        Debug.Log("[GiftBox] Icon clicked!");

        // Stop animation nhấp nháy
        if (giftBoxIcon != null)
        {
            giftBoxIcon.transform.DOKill();
            giftBoxIcon.transform.localScale = Vector3.one;
        }

        if (pendingGifts.Count > 0)
        {
            Debug.Log("[GiftBox] → Showing first gift");
            ShowGiftBoxAnimation(pendingGifts[0]);
        }
        else
        {
            Debug.LogWarning("[GiftBox] No gifts to show, refreshing...");
            int userId = PlayerPrefs.GetInt("userId", 0);
            CheckForGifts(userId);
        }
    }

    // ========================================
    // ORIGINAL METHODS
    // ========================================

    public void RefreshUserInfo(bool silent = false)
    {
        int userId = PlayerPrefs.GetInt("userId", 0);
        if (userId == 0)
        {
            Debug.LogError("[ManagerQuangTruong] User ID not found!");
            return;
        }

        Debug.Log($"[ManagerQuangTruong] Refreshing user info (silent={silent})...");

        if (!silent)
        {
            ManagerGame.Instance.ShowLoading();
        }

        StartCoroutine(RefreshUserCoroutine(userId, silent));
    }

    IEnumerator RefreshUserCoroutine(int userId, bool silent)
    {
        yield return APIManager.Instance.GetRequest<UserDTO>(
            APIConfig.GET_USER(userId),
            (user) => OnUserRefreshed(user, silent),
            (error) => OnRefreshError(error, silent)
        );
    }

    void OnUserRefreshed(UserDTO user, bool silent = false)
    {
        if (!silent)
        {
            ManagerGame.Instance.HideLoading();
        }

        Debug.Log("[ManagerQuangTruong] User info refreshed successfully");

        if (EnergyManager.Instance == null && txtNl != null)
        {
            txtNl.text = user.energy + "/" + user.energyFull;
        }
        else if (EnergyManager.Instance != null)
        {
            EnergyManager.Instance.RefreshEnergyFromServer();
        }

        txtVang.text = FormatVND(user.gold);
        ruby = user.ruby;
        txtCt.text = FormatVND(user.requestAttack);
        txtCtint = user.requestAttack;
        SetupImgLevel(user.lever, imgLvUser);
        lvUser = user.lever.ToString();
        imgAvatar.sprite = Resources.Load<Sprite>("Image/Avt/" + user.avtId);
        UpdateMedalImage(user.lever);
        float expPercent = 0f;
        if (user.exp > 0)
        {
            expPercent = ((float)user.expCurrent / (float)user.exp) * 100f;
        }
        txtExp.text = Mathf.Ceil(expPercent).ToString() + "%";
        txtName.text = user.name;
        expslider.maxValue = 100f;
        expslider.value = expPercent;

        UpdateWheelFlag(user.wheel);
        UpdateStarUI(user.starWhite, user.starBlue, user.starRed);
    }
    public static string FormatVND(long amount)
    {
        return amount.ToString("#,##0").Replace(",", ".");
    }

    void OnRefreshError(string error, bool silent)
    {
        if (!silent)
        {
            ManagerGame.Instance.HideLoading();
        }

        Debug.LogError($"[ManagerQuangTruong] Refresh Error: {error}");
    }

    public void ShowPanelBoss()
    {
        if (panelBoss != null)
        {
            panelBoss.SetActive(true);
        }
    }

    public void HidePanelBoss()
    {
        if (panelBoss != null)
        {
            panelBoss.SetActive(false);
        }
    }

    private IEnumerator LoadSceneAfterDelay()
    {
        int userId = PlayerPrefs.GetInt("userId", 0);
        Debug.Log("userId: " + userId);

        // ✅ SHOW LOADING NGAY TỪ ĐẦU
        ManagerGame.Instance.ShowLoading();
        loadingPanel.SetActive(true);

        if (EnergyManager.Instance != null)
        {
            EnergyManager.Instance.RegisterUI(txtNl, txtCountdown, imgEnergyBar);
            EnergyManager.Instance.RefreshEnergyFromServer();
        }
        else
        {
            yield return StartCoroutine(LoadEnergyInfo(userId));
        }

        yield return APIManager.Instance.GetRequest<List<PetLibDTO>>(APIConfig.GET_ALL_PET, OnPetsReceived, OnError);
        yield return APIManager.Instance.GetRequest<UserDTO>(APIConfig.GET_USER(userId), OnUserReceived, OnError);

        Debug.Log("[ManagerQuangTruong] Checking for gifts...");
        CheckForGifts(userId);

        // ✅ KIỂM TRA XEM CÓ CẦN KHÔI PHỤC STATE KHÔNG
        bool shouldRestore = PlayerPrefs.GetInt("ReturnToRoom", 0) == 1;

        if (shouldRestore)
        {
            // ✅ NẾU CẦN KHÔI PHỤC: GIỮ LOADING, KHÔI PHỤC STATE, SAU ĐÓ MỚI HIDE
            Debug.Log("[ManagerQuangTruong] Restoring state with loading...");
            yield return StartCoroutine(RestorePanelStateWithLoading());
        }
        else
        {
            // ✅ NẾU KHÔNG CẦN KHÔI PHỤC: HIDE LOADING NGAY
            ManagerGame.Instance.HideLoading();
        }
    }
    // ✅ THAY THẾ METHOD RestorePanelStateWithLoading() TRONG ManagerQuangTruong.cs

    /// <summary>
    /// Khôi phục state với loading - Loading LUÔN HIỆN Ở TRÊN CÙNG
    /// </summary>
    private IEnumerator RestorePanelStateWithLoading()
    {
        loadewqingPanel.SetActive(true);
        bool shouldReturnToRoom = PlayerPrefs.GetInt("ReturnToRoom", 0) == 1;
        bool shouldReturnToChinhPhuc = PlayerPrefs.GetInt("ReturnToChinhPhuc", 0) == 1;
        int panelIndex = PlayerPrefs.GetInt("ReturnToPanelIndex", -1);

        if (!shouldReturnToRoom || !shouldReturnToChinhPhuc)
        {
            Debug.Log("[ManagerQuangTruong] No state to restore");
            ManagerGame.Instance.HideLoading();
            if (loadingPanel != null) loadingPanel.SetActive(false);
            yield break;
        }

        Debug.Log("[ManagerQuangTruong] ═══════════════════════════════");
        Debug.Log("[ManagerQuangTruong] RESTORE STATE WITH LOADING");
        Debug.Log("[ManagerQuangTruong] ═══════════════════════════════");

        // ✅ SETUP LOADING - LUÔN Ở TRÊN CÙNG, BLOCK TẤT CẢ
        SetupLoadingPanel();

        // ✅ BƯỚC 1: MỞ CHINH PHỤC (ẨN)
        Debug.Log("[ManagerQuangTruong] [1/7] Opening ChinhPhuc panel (hidden)...");
        CanvasGroup chinhPhucCG = OpenChinhPhucHidden();

        if (chinhPhucCG == null)
        {
            Debug.LogError("[ManagerQuangTruong] Failed to open ChinhPhuc!");
            HideLoadingPanel();
            yield break;
        }

        // ✅ BƯỚC 2: LOAD DATA CHINH PHỤC
        Debug.Log("[ManagerQuangTruong] [2/7] Loading ChinhPhuc data...");
        ManagerChinhPhuc chinhPhucManager = panelChinhPhuc.GetComponent<ManagerChinhPhuc>();
        chinhPhucManager.InitializeAndLoadData();

        yield return new WaitForSeconds(1.0f); // Đợi API
        Debug.Log("[ManagerQuangTruong] ✓ ChinhPhuc data loaded");

        // ✅ ĐẢM BẢO LOADING VẪN Ở TRÊN
        KeepLoadingOnTop();

        // ✅ BƯỚC 3: RESTORE PANEL CỤ THỂ
        if (panelIndex >= 0)
        {
            Debug.Log($"[ManagerQuangTruong] [3/7] Restoring panel {panelIndex}...");
            chinhPhucManager.ShowPanel(panelIndex, withAnimation: false);
            yield return new WaitForSeconds(0.3f);
        }

        // ✅ ĐẢM BẢO LOADING VẪN Ở TRÊN
        KeepLoadingOnTop();

        // ✅ BƯỚC 4: MỞ ROOM (ẨN)
        Debug.Log("[ManagerQuangTruong] [4/7] Opening Room panel (hidden)...");
        ManagerRoom roomManager = FindObjectOfType<ManagerRoom>();

        if (roomManager == null)
        {
            Debug.LogError("[ManagerQuangTruong] ManagerRoom not found!");
            HideLoadingPanel();
            yield break;
        }

        CanvasGroup roomCG = OpenRoomHidden(roomManager);

        // ✅ ĐẢM BẢO LOADING VẪN Ở TRÊN
        KeepLoadingOnTop();

        // ✅ BƯỚC 5: LOAD DATA ROOM
        Debug.Log("[ManagerQuangTruong] [5/7] Loading Room data...");
        yield return roomManager.LoadRoomDataWithoutLoading();

        Debug.Log("[ManagerQuangTruong] ✓ Room data loaded");


        // ✅ ĐẢM BẢO LOADING VẪN Ở TRÊN TRƯỚC KHI HIỆN PANEL
        KeepLoadingOnTop();

        Debug.Log("[ManagerQuangTruong] ✓✓ ALL DATA LOADED!");

        // ✅ BƯỚC 7: HIỆN TẤT CẢ PANEL
        Debug.Log("[ManagerQuangTruong] [7/7] Showing all panels...");

        if (chinhPhucCG != null) chinhPhucCG.alpha = 1f;
        if (roomCG != null) roomCG.alpha = 1f;



        // ✅ XÓA FLAG
        PlayerPrefs.DeleteKey("ReturnToRoom");
        PlayerPrefs.DeleteKey("ReturnToChinhPhuc");
        PlayerPrefs.DeleteKey("ReturnToPanelIndex");
        PlayerPrefs.Save();

        // ✅ ẨN LOADING SAU CÙNG
        Debug.Log("[ManagerQuangTruong] Hiding loading...");
        HideLoadingPanel();

        Debug.Log("[ManagerQuangTruong] ═══════════════════════════════");
        Debug.Log("[ManagerQuangTruong] ✓✓✓ RESTORE COMPLETE!");
        Debug.Log("[ManagerQuangTruong] ═══════════════════════════════");
        loadewqingPanel.SetActive(false);
    }

    // ═══════════════════════════════════════════════════════════
    // HELPER METHODS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Setup loading panel - LUÔN Ở TRÊN, BLOCK TẤT CẢ
    /// </summary>
    private void SetupLoadingPanel()
    {
        if (loadingPanel == null)
        {
            Debug.LogError("[ManagerQuangTruong] loadingPanel is NULL!");
            return;
        }

        // Đảm bảo active
        loadingPanel.SetActive(true);

        // Đưa lên trên cùng trong hierarchy
        loadingPanel.transform.SetAsLastSibling();

        // Setup Canvas với sortingOrder cao nhất
        Canvas loadingCanvas = loadingPanel.GetComponent<Canvas>();
        if (loadingCanvas == null)
        {
            loadingCanvas = loadingPanel.AddComponent<Canvas>();
            loadingCanvas.overrideSorting = true;
        }
        loadingCanvas.sortingOrder = 9999; // ✅ CAO NHẤT

        // Setup CanvasGroup để block input
        CanvasGroup loadingCG = loadingPanel.GetComponent<CanvasGroup>();
        if (loadingCG == null)
        {
            loadingCG = loadingPanel.AddComponent<CanvasGroup>();
        }
        loadingCG.alpha = 1f; // ✅ HIỆN RÕ
        loadingCG.blocksRaycasts = true; // ✅ BLOCK INPUT
        loadingCG.interactable = false; // ✅ KHÔNG TƯƠNG TÁC

        Debug.Log("[ManagerQuangTruong] ✓ Loading setup complete (sortingOrder=9999, alpha=1, blocking)");
    }

    /// <summary>
    /// Đảm bảo loading luôn ở trên cùng
    /// </summary>
    private void KeepLoadingOnTop()
    {
        if (loadingPanel != null)
        {
            loadingPanel.transform.SetAsLastSibling();

            Canvas loadingCanvas = loadingPanel.GetComponent<Canvas>();
            if (loadingCanvas != null)
            {
                loadingCanvas.sortingOrder = 9999;
            }

            CanvasGroup loadingCG = loadingPanel.GetComponent<CanvasGroup>();
            if (loadingCG != null)
            {
                loadingCG.alpha = 1f;
            }

            Debug.Log("[ManagerQuangTruong] ✓ Loading kept on top");
        }
    }

    /// <summary>
    /// Mở ChinhPhuc nhưng ẨN (alpha=0)
    /// </summary>
    private CanvasGroup OpenChinhPhucHidden()
    {
        if (panelChinhPhuc == null)
        {
            Debug.LogError("[ManagerQuangTruong] panelChinhPhuc is null!");
            return null;
        }

        // Mở panel
        panelChinhPhuc.SetActive(true);
        panelChinhPhuc.transform.localScale = Vector3.one;

        // ẨN bằng CanvasGroup
        CanvasGroup cg = panelChinhPhuc.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = panelChinhPhuc.AddComponent<CanvasGroup>();
        }
        cg.alpha = 0f; // ✅ ẨN

        Debug.Log("[ManagerQuangTruong] ✓ ChinhPhuc opened (hidden, alpha=0)");

        return cg;
    }

    /// <summary>
    /// Mở Room nhưng ẨN (alpha=0)
    /// </summary>
    private CanvasGroup OpenRoomHidden(ManagerRoom roomManager)
    {
        if (roomManager.roomPanel == null)
        {
            Debug.LogError("[ManagerQuangTruong] roomPanel is null!");
            return null;
        }

        // Mở panel
        roomManager.roomPanel.SetActive(true);
        roomManager.roomPanel.transform.localScale = Vector3.one;

        // ẨN bằng CanvasGroup
        CanvasGroup cg = roomManager.roomPanel.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = roomManager.roomPanel.AddComponent<CanvasGroup>();
        }
        cg.alpha = 0f; // ✅ ẨN

        Debug.Log("[ManagerQuangTruong] ✓ Room opened (hidden, alpha=0)");

        return cg;
    }

    /// <summary>
    /// Ẩn loading panel
    /// </summary>
    private void HideLoadingPanel()
    {
        Debug.Log("[ManagerQuangTruong] Hiding loading panel...");

        ManagerGame.Instance.HideLoading();

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        Debug.Log("[ManagerQuangTruong] ✓ Loading hidden");
    }
    /// <summary>
    /// Coroutine để khôi phục ChinhPhuc -> Room theo thứ tự
    /// </summary>
    private IEnumerator RestoreChinhPhucAndRoom(ManagerChinhPhuc chinhPhucManager, int panelIndex)
    {
        // Load data cho Chinh Phục
        Debug.Log("[ManagerQuangTruong] Loading ChinhPhuc data...");
        chinhPhucManager.InitializeAndLoadData();

        // Đợi data load xong (tùy logic của bạn, có thể cần điều chỉnh)
        yield return new WaitForSeconds(0.5f);

        // ✅ BƯỚC 3: KHÔI PHỤC PANEL CỤ THỂ (KHÔNG ANIMATION)
        if (panelIndex >= 0)
        {
            Debug.Log($"[ManagerQuangTruong] Restoring panel {panelIndex}...");
            chinhPhucManager.ShowPanel(panelIndex, withAnimation: false);
        }

        yield return new WaitForSeconds(0.2f);

        // ✅ BƯỚC 4: MỞ ROOM PANEL (KHÔNG ANIMATION)
        Debug.Log("[ManagerQuangTruong] Opening Room panel...");
        ManagerRoom roomManager = FindObjectOfType<ManagerRoom>();
        if (roomManager != null)
        {
            // Mở Room NGAY không animation
            if (roomManager.roomPanel != null)
            {
                roomManager.roomPanel.SetActive(true);
                roomManager.roomPanel.transform.localScale = Vector3.one;

                // Set alpha = 1 ngay
                CanvasGroup cg = roomManager.roomPanel.GetComponent<CanvasGroup>();
                if (cg == null) cg = roomManager.roomPanel.AddComponent<CanvasGroup>();
                cg.alpha = 1f;

                // Load data cho Room
                StartCoroutine(roomManager.LoadRoomDataWithLoading());
            }
        }
        else
        {
            Debug.LogError("[ManagerQuangTruong] ManagerRoom not found!");
        }

        // ✅ BƯỚC 5: XÓA FLAG SAU KHI KHÔI PHỤC XONG
        PlayerPrefs.DeleteKey("ReturnToRoom");
        PlayerPrefs.DeleteKey("ReturnToChinhPhuc");
        PlayerPrefs.DeleteKey("ReturnToPanelIndex");
        PlayerPrefs.Save();

        Debug.Log("[ManagerQuangTruong] ✓✓ State restored successfully!");
    }

    private IEnumerator LoadEnergyInfo(int userId)
    {
        string url = APIConfig.GET_ENERGY(userId);

        var request = APIManager.Instance.GetRequest<EnergyInfoDTO>(
            url,
            (energyInfo) =>
            {
                if (txtNl != null)
                {
                    txtNl.text = $"{energyInfo.currentEnergy}/{energyInfo.maxEnergy}";
                }

                Debug.Log($"✓ Energy loaded: {energyInfo.currentEnergy}/{energyInfo.maxEnergy}");
            },
            (error) =>
            {
                Debug.LogError($"Lỗi load energy: {error}");
            }
        );

        yield return request;
    }

    public void BackScene()
    {
        ManagerGame.Instance.BackScene();
    }

    void OnPetsReceived(List<PetLibDTO> pets)
    {
        foreach (var pet in pets)
        {
            GameObject petUIObject = Instantiate(petUIPrefab, petListContainer);
            Image petIcon = petUIObject.transform.Find("imgPet")?.GetComponent<Image>();
            Text txtNamePet = petUIObject.transform.Find("txtName")?.GetComponent<Text>();
            string petID = pet.id.ToString();
            petId = petID;
            Sprite petSprite = Resources.Load<Sprite>("Image/IconsPet/" + petID);
            petUIObject.name = petID;
            petIcon.sprite = petSprite;
            txtNamePet.text = pet.name;
            Button petButton = petUIObject.GetComponent<Button>();
            int capturedPetId = pet.id;
            petButton.onClick.AddListener(() => OnPetClicked(petSprite, pet.name, pet.attack, pet.hp, pet.mana, pet.maxLevel, pet.elementType, pet.elementOther, pet.weaknessValue, pet.des));
        }
    }
    private void UpdateMedalImage(int userLevel)
    {
        int medalLevel = GetMedalLevel(userLevel);

        string spritePath = "Image/hc/" + medalLevel;
        Sprite medalSprite = Resources.Load<Sprite>(spritePath);

        if (medalSprite != null && HC != null)
        {
            HC.sprite = medalSprite;
            Debug.Log($"[Medal] Level {userLevel} → Medal {medalLevel} ({spritePath})");
        }
        else
        {
            Debug.LogWarning($"[Medal] Sprite not found: {spritePath}");
        }
    }
    private int GetMedalLevel(int userLevel)
    {
        if (userLevel < 5)
            return 1;
        else if (userLevel < 10)
            return 5;
        else if (userLevel < 15)
            return 10;
        else if (userLevel < 20)
            return 15;
        else if (userLevel < 25)
            return 20;
        else if (userLevel < 30)
            return 25;
        else if (userLevel < 35)
            return 30;
        else if (userLevel < 40)
            return 35;
        else if (userLevel < 45)
            return 40;
        else if (userLevel < 50)
            return 45;
        else if (userLevel < 55)
            return 50;
        else if (userLevel < 60)
            return 55;
        else // Level > 60
            return 60;
    }

    void OnUserReceived(UserDTO user)
    {
        if (EnergyManager.Instance == null && txtNl != null)
        {
            txtNl.text = user.energy + "/" + user.energyFull;
        }
        txtNl.text = user.energy + "/" + user.energyFull;
        ruby = user.ruby;
        txtVang.text = FormatVND(user.gold);
        txtCt.text = FormatVND(user.requestAttack);
        txtCtint = user.requestAttack;
        SetupImgLevel(user.lever, imgLvUser);
        lvUser = user.lever.ToString();
        imgAvatar.sprite = Resources.Load<Sprite>("Image/Avt/" + user.avtId);
        UpdateMedalImage(user.lever);
        float expPercent = 0f;
        if (user.exp > 0)
        {
            expPercent = ((float)user.expCurrent / (float)user.exp) * 100f;
        }
        txtExp.text = expPercent.ToString("F1") + "%";
        txtName.text = user.name;
        expslider.maxValue = 100f;
        expslider.value = expPercent;

        UpdateWheelFlag(user.wheel);

        PlayerPrefs.SetInt("StarWhite", user.starWhite);
        PlayerPrefs.SetInt("StarBlue", user.starBlue);
        PlayerPrefs.SetInt("StarRed", user.starRed);
        PlayerPrefs.Save();

        Debug.Log($"[ManagerQuangTruong] Stars saved to PlayerPrefs - White: {user.starWhite}, Blue: {user.starBlue}, Red: {user.starRed}");

        UpdateStarUI(user.starWhite, user.starBlue, user.starRed);

        if (StarEventManager.Instance != null)
        {
            StarEventManager.Instance.UpdateStarCount(user.starWhite, user.starBlue, user.starRed);
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

    private void OnDestroy()
    {
        StopBackgroundMusic();
        if (EnergyManager.Instance != null)
        {
            EnergyManager.Instance.UnregisterUI();
        }
    }

    public void UpdateWheelFlag(int wheelCount)
    {
        if (wheelCount > 0)
        {
            flagWheel.gameObject.SetActive(true);
            flagWheel.gameObject.transform.GetChild(0).GetComponent<Text>().text = wheelCount.ToString();
        }
        else
        {
            flagWheel.gameObject.SetActive(false);
        }
    }

    public void UpdateStarUI(int starWhite, int starBlue, int starRed)
    {
        if (txtStarWhite != null)
            txtStarWhite.text = starWhite.ToString();

        if (txtStarBlue != null)
            txtStarBlue.text = starBlue.ToString();

        if (txtStarRed != null)
            txtStarRed.text = starRed.ToString();

        PlayerPrefs.SetInt("StarWhite", starWhite);
        PlayerPrefs.SetInt("StarBlue", starBlue);
        PlayerPrefs.SetInt("StarRed", starRed);
        PlayerPrefs.Save();

        Debug.Log($"[ManagerQuangTruong] UpdateStarUI - White: {starWhite}, Blue: {starBlue}, Red: {starRed}");

        if (StarEventManager.Instance != null)
        {
            StarEventManager.Instance.UpdateStarCount(starWhite, starBlue, starRed);
        }
    }

    void OnPetClicked(Sprite petSprite, string name, int attack, int hp, int mana, int maxLevel, string elementType, string elementOther, double weaknessValue, string txtDes)
    {
        Image bannerPet = banner.GetComponent<Image>();
        bannerPet.sprite = petSprite;
        namePet.text = name;
        txtDame.text = attack.ToString();
        txtHp.text = hp.ToString();
        txtMana.text = mana.ToString();
        des.text = txtDes.ToString();
        txtWee.text = "+" + weaknessValue.ToString();
        txtLv.text = "Lv " + maxLevel.ToString();
        imgAtribute.sprite = Resources.Load<Sprite>("Image/Attribute/" + elementType);
        imgAtributeOther.sprite = Resources.Load<Sprite>("Image/Attribute/" + elementOther);
    }

    void OnError(string error)
    {
        Debug.LogError("API Error: " + error);
    }
}