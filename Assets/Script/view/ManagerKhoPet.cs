using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ManagerKhoPet : MonoBehaviour
{
    [Header("Pet UI")]
    public GameObject petUIPrefab;
    public Transform petListContainer;
    public GameObject LoadingPanel;
    public Text txtVang;
    public Text txtCt;
    public Text txtNl;
    public Animator animator;
    public Text namePet;
    public Text txtHp;
    public Text txtMana;
    public Text txtDame;
    public Text txtWee;
    public Text txtLv;
    public Text des;
    public Image imgAtribute;
    public Image imgAtributeOther;
    public Animator imgPetAnimator;

    [Header("Stone Upgrade UI")]
    public GameObject panelStone;
    public Transform stoneListContainer;
    public GameObject stonePrefab;
    public GameObject panelUpdate;
    public Transform updateSlotsContainer;
    public Text txtUpgradePercent;
    public Button btnUpdate;

    [Header("Stone Sprites - Kéo thả sprite theo thứ tự Lv1, Lv2, Lv3, Lv4, Lv5")]
    public List<Sprite> fireStoneSprites = new List<Sprite>();
    public List<Sprite> waterStoneSprites = new List<Sprite>();
    public List<Sprite> earthStoneSprites = new List<Sprite>();
    public List<Sprite> electricStoneSprites = new List<Sprite>();
    public List<Sprite> woodStoneSprites = new List<Sprite>();

    private PetUserDTO firstPet;
    private PetUserDTO currentSelectedPet;
    private StoneResponse allStones;

    private StoneDTO[] selectedStones = new StoneDTO[3];
    private Dictionary<int, int> tempStoneCount = new Dictionary<int, int>();

    [Header("Upgrade Animation")]
    public GameObject anmtUpdatePet;
    public Text txtResultUpdate;

    private Color originalResultColor;
    public Toggle toggleProtection;
    public Text messageText;
    private UserDTO currentUser;

    [Header("Skill Card UI")]
    public GameObject PanelCardPet;
    public Image imgCard;
    public Text txtDescription;

    [Header("Stone Upgrade System")]
    public GameObject PanelUpdateStone;
    public Button btnOpenStoneUpgrade;
    public Button btnCloseStoneUpgrade;
    public GameObject PanelHe;
    public Button btnHeFire;
    public Button btnHeWater;
    public Button btnHeEarth;
    public Button btnHeMetal;
    public Button btnHeWood;
    public GameObject PanelStoneUpgrade;
    public Transform stoneUpgradeListContainer;
    public GameObject PanelUpdateStone2;
    public Transform stoneUpgradeSlotsContainer;
    public GameObject StoneMain;
    public Image imgStoneMain;
    public Text txtStoneMainLevel;
    public Text txtUpgradePercentStone;
    public Button btnUpgradeStone;
    public Text txtResultUpdateStone;
    public GameObject anmtUpdateStone;
    public Toggle toggleUpgradeAll; // ✅ THÊM: Toggle nâng cấp hết
    public Text messageTextStone; // ✅ THÊM: Text hiển thị message cho đá

    private string currentSelectedElement;
    private StoneDTO[] selectedStonesForUpgrade = new StoneDTO[3];
    private Dictionary<int, int> tempStoneCountUpgrade = new Dictionary<int, int>();
    private Color originalResultColorStone;

    [Header("LeanTween Animation Settings")]
    public float panelAnimDuration = 0.2f;
    public float itemAnimDelay = 0.02f;
    public LeanTweenType easeType = LeanTweenType.easeOutBack;

    private CanvasGroup panelStoneCanvasGroup;
    private CanvasGroup panelUpdateCanvasGroup;
    private CanvasGroup panelUpdateStoneCanvasGroup;
    private CanvasGroup panelCardPetCanvasGroup;

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

    private void Start()
    {
        SetupCanvasGroups();
        SetupButtonSounds();
        PlayBackgroundMusic();
        StartCoroutine(LoadSceneAfterDelay());

        if (btnUpdate != null)
        {
            btnUpdate.onClick.AddListener(OnUpgradeButtonClicked);
        }

        if (toggleProtection != null)
        {
            toggleProtection.onValueChanged.AddListener(OnToggleProtectionChanged);
            toggleProtection.SetIsOnWithoutNotify(false);
            if (messageText != null)
            {
                messageText.gameObject.SetActive(false);
            }
        }

        if (txtResultUpdate != null)
        {
            originalResultColor = txtResultUpdate.color;
            txtResultUpdate.gameObject.SetActive(false);
        }

        if (btnOpenStoneUpgrade != null)
            btnOpenStoneUpgrade.onClick.AddListener(OpenStoneUpgradePanel);

        if (btnCloseStoneUpgrade != null)
            btnCloseStoneUpgrade.onClick.AddListener(CloseStoneUpgradePanel);

        if (btnUpgradeStone != null)
            btnUpgradeStone.onClick.AddListener(OnUpgradeStoneClicked);

        if (btnHeFire != null)
            btnHeFire.onClick.AddListener(() => SelectElement("FIRE"));
        if (btnHeWater != null)
            btnHeWater.onClick.AddListener(() => SelectElement("WATER"));
        if (btnHeEarth != null)
            btnHeEarth.onClick.AddListener(() => SelectElement("EARTH"));
        if (btnHeMetal != null)
            btnHeMetal.onClick.AddListener(() => SelectElement("METAL"));
        if (btnHeWood != null)
            btnHeWood.onClick.AddListener(() => SelectElement("WOOD"));

        if (txtResultUpdateStone != null)
        {
            originalResultColorStone = txtResultUpdateStone.color;
            txtResultUpdateStone.gameObject.SetActive(false);
        }

        if (PanelUpdateStone != null)
            PanelUpdateStone.SetActive(false);

        if (PanelStoneUpgrade != null)
            PanelStoneUpgrade.SetActive(false);

        if (StoneMain != null)
            StoneMain.SetActive(false);

        // ✅ THÊM: Ẩn animation đá ban đầu
        if (anmtUpdateStone != null)
            anmtUpdateStone.SetActive(false);

        // ✅ THÊM: Setup toggle upgrade all
        if (toggleUpgradeAll != null)
        {
            toggleUpgradeAll.onValueChanged.AddListener(OnToggleUpgradeAllChanged);
            toggleUpgradeAll.SetIsOnWithoutNotify(false);
            if (messageTextStone != null)
            {
                messageTextStone.gameObject.SetActive(false);
            }
        }
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

    void SetupCanvasGroups()
    {
        if (panelStone != null)
        {
            panelStoneCanvasGroup = panelStone.GetComponent<CanvasGroup>();
            if (panelStoneCanvasGroup == null)
                panelStoneCanvasGroup = panelStone.AddComponent<CanvasGroup>();
        }

        if (panelUpdate != null)
        {
            panelUpdateCanvasGroup = panelUpdate.GetComponent<CanvasGroup>();
            if (panelUpdateCanvasGroup == null)
                panelUpdateCanvasGroup = panelUpdate.AddComponent<CanvasGroup>();
        }

        if (PanelUpdateStone != null)
        {
            panelUpdateStoneCanvasGroup = PanelUpdateStone.GetComponent<CanvasGroup>();
            if (panelUpdateStoneCanvasGroup == null)
                panelUpdateStoneCanvasGroup = PanelUpdateStone.AddComponent<CanvasGroup>();
        }

        if (PanelCardPet != null)
        {
            panelCardPetCanvasGroup = PanelCardPet.GetComponent<CanvasGroup>();
            if (panelCardPetCanvasGroup == null)
                panelCardPetCanvasGroup = PanelCardPet.AddComponent<CanvasGroup>();
        }
    }

    // ==================== LEANTWEEN ANIMATION HELPERS ====================

    void AnimateOpenPanel(GameObject panel, CanvasGroup canvasGroup)
    {
        if (panel == null || canvasGroup == null) return;

        panel.transform.localScale = Vector3.zero;
        canvasGroup.alpha = 0f;

        LeanTween.scale(panel, Vector3.one, panelAnimDuration)
            .setEase(easeType);

        LeanTween.alphaCanvas(canvasGroup, 1f, panelAnimDuration)
            .setEase(LeanTweenType.easeInOutQuad);
    }

    void AnimateClosePanel(GameObject panel, CanvasGroup canvasGroup)
    {
        if (panel == null || canvasGroup == null) return;

        LeanTween.scale(panel, Vector3.zero, panelAnimDuration)
            .setEase(LeanTweenType.easeInBack);

        LeanTween.alphaCanvas(canvasGroup, 0f, panelAnimDuration)
            .setEase(LeanTweenType.easeInOutQuad)
            .setOnComplete(() =>
            {
                panel.SetActive(false);
            });
    }

    void AnimateButtonClick(Button button)
    {
        if (button == null) return;

        LeanTween.scale(button.gameObject, Vector3.one * 0.9f, 0.1f)
            .setEaseInOutQuad()
            .setOnComplete(() =>
            {
                LeanTween.scale(button.gameObject, Vector3.one, 0.1f).setEaseInOutQuad();
            });
    }

    void AnimateItemAppear(GameObject item, int index, float baseDelay = 0f)
    {
        item.transform.localScale = Vector3.zero;

        CanvasGroup canvasGroup = item.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = item.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        float delay = baseDelay + (index * itemAnimDelay);

        LeanTween.scale(item, Vector3.one, 0.4f)
            .setDelay(delay)
            .setEase(LeanTweenType.easeOutBack);

        LeanTween.alphaCanvas(canvasGroup, 1f, 0.3f)
            .setDelay(delay)
            .setEase(LeanTweenType.easeInOutQuad);
    }

    void AnimateStatUpdate(Text textComponent, int oldValue, int newValue, float delay = 0f)
    {
        if (textComponent == null) return;

        // Scale pulse
        LeanTween.scale(textComponent.gameObject, Vector3.one * 1.2f, 0.2f)
            .setDelay(delay)
            .setEase(LeanTweenType.easeOutQuad)
            .setOnComplete(() =>
            {
                LeanTween.scale(textComponent.gameObject, Vector3.one, 0.2f)
                    .setEase(LeanTweenType.easeInOutQuad);
            });

        // Counter animation
        LeanTween.value(textComponent.gameObject, oldValue, newValue, 0.5f)
            .setDelay(delay)
            .setOnUpdate((float val) =>
            {
                textComponent.text = Mathf.RoundToInt(val).ToString();
            })
            .setEase(LeanTweenType.easeOutQuad);
    }

    void AnimateImageRotate(GameObject imageObj, float delay = 0f)
    {
        imageObj.transform.rotation = Quaternion.Euler(0, 0, -10f);
        LeanTween.rotateZ(imageObj, 10f, 0.3f)
            .setDelay(delay)
            .setEase(LeanTweenType.easeInOutQuad)
            .setLoopPingPong();
    }

    // ==================== STONE UPGRADE METHODS ====================

    // ✅ THÊM: Xử lý toggle upgrade all
    void OnToggleUpgradeAllChanged(bool isOn)
    {
        if (messageTextStone != null)
        {
            messageTextStone.gameObject.SetActive(false);

            if (isOn)
            {
                // Kiểm tra đủ vàng không
                if (currentUser != null && currentUser.gold < 5000)
                {
                    // Không đủ vàng -> tắt toggle và hiện thông báo
                    toggleUpgradeAll.SetIsOnWithoutNotify(false);
                    messageTextStone.text = "Bạn đã hết gold ^^!";
                    messageTextStone.gameObject.SetActive(true);
                    StartCoroutine(HideStoneMessageAfterDelay(3f));
                }
                else
                {
                    // ✅ SỬA: Message rõ ràng hơn
                    messageTextStone.text = "Chọn 1 loại đá, sau đó click nút nâng cấp để nâng HẾT!";
                    messageTextStone.gameObject.SetActive(true);
                }
            }
        }
    }

    // ✅ THÊM: Ẩn message stone sau delay
    IEnumerator HideStoneMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (messageTextStone != null)
        {
            messageTextStone.gameObject.SetActive(false);
        }
    }

    void OpenStoneUpgradePanel()
    {
        if (PanelUpdateStone != null)
        {
            PanelUpdateStone.SetActive(true);
            AnimateOpenPanel(PanelUpdateStone, panelUpdateStoneCanvasGroup);
        }

        if (PanelHe != null)
        {
            PanelHe.SetActive(true);
            // Animate element buttons
            AnimateElementButtons();
        }

        ResetStoneUpgradeUI();
    }

    void AnimateElementButtons()
    {
        Button[] elementButtons = { btnHeFire, btnHeWater, btnHeEarth, btnHeMetal, btnHeWood };

        for (int i = 0; i < elementButtons.Length; i++)
        {
            if (elementButtons[i] != null)
            {
                AnimateItemAppear(elementButtons[i].gameObject, i, 0.1f);
            }
        }
    }

    void CloseStoneUpgradePanel()
    {
        if (PanelUpdateStone != null)
        {
            AnimateClosePanel(PanelUpdateStone, panelUpdateStoneCanvasGroup);
        }

        ResetStoneUpgradeUI();
    }

    void SelectElement(string element)
    {
        currentSelectedElement = element;

        selectedStonesForUpgrade = new StoneDTO[3];
        UpdateStoneUpgradeSlotsUI();

        if (PanelStoneUpgrade != null)
        {
            PanelStoneUpgrade.SetActive(true);
            // Animate panel appear
            PanelStoneUpgrade.transform.localScale = Vector3.zero;
            LeanTween.scale(PanelStoneUpgrade, Vector3.one, 0.3f)
                .setEase(LeanTweenType.easeOutBack);
        }

        LoadStonesForUpgrade(element);
    }

    void LoadStonesForUpgrade(string elementType)
    {
        if (allStones == null || stoneUpgradeListContainer == null)
            return;

        foreach (Transform child in stoneUpgradeListContainer)
        {
            Destroy(child.gameObject);
        }

        tempStoneCountUpgrade.Clear();

        StoneDTO[] stones = GetStonesForElement(elementType);

        if (stones == null || stones.Length == 0)
        {
            Debug.Log($"Không có đá cho hệ {elementType}");
            return;
        }

        int maxLevel = 0;
        foreach (var stone in stones)
        {
            if (stone.lever > maxLevel)
            {
                maxLevel = stone.lever;
            }
        }

        int displayedCount = 0;
        foreach (var stone in stones)
        {
            if (stone.lever >= maxLevel)
            {
                Debug.Log($"Bỏ qua {stone.name} (Level {stone.lever}) - Đá level tối đa không thể nâng cấp");
                continue;
            }

            tempStoneCountUpgrade[stone.idStone] = stone.count;

            GameObject stoneObj = Instantiate(stonePrefab, stoneUpgradeListContainer);
            SetupStoneUpgradeUI(stoneObj, stone);
            displayedCount++;
        }

        Debug.Log($"✓ Đã load {displayedCount}/{stones.Length} loại đá hệ {elementType} để nâng cấp (bỏ qua level {maxLevel})");
    }

    void SetupStoneUpgradeUI(GameObject stoneObj, StoneDTO stone)
    {
        Image imgStone = stoneObj.transform.Find("imgStone")?.GetComponent<Image>();
        if (imgStone != null)
        {
            Sprite stoneSprite = GetStoneSpriteByElement(stone.elementType, stone.lever);
            if (stoneSprite != null)
            {
                imgStone.sprite = stoneSprite;
            }
        }

        Text txtCount = stoneObj.transform.Find("txtnum")?.GetComponent<Text>();
        if (txtCount != null)
        {
            txtCount.text = stone.count.ToString();
        }

        Button btnStone = stoneObj.GetComponent<Button>();
        if (btnStone != null)
        {
            btnStone.onClick.AddListener(() =>
            {
                AnimateButtonClick(btnStone); // ✅ THÊM animation click
                OnStoneUpgradeClicked(stone, txtCount);
            });
        }

        if (stone.count == 0 && btnStone != null)
        {
            btnStone.interactable = false;
        }
SetupButtonSounds();
        // ✅ THÊM: Animate stone upgrade item
        int index = stoneUpgradeListContainer.childCount - 1;
        AnimateItemAppear(stoneObj, index, 0.3f);
    }

    void OnStoneUpgradeClicked(StoneDTO stone, Text txtCount)
    {
        // ✅ KIỂM TRA: Nếu toggle upgrade all đang bật thì không cho chọn thủ công
        if (toggleUpgradeAll != null && toggleUpgradeAll.isOn)
        {
            ShowStoneErrorMessage("Đang ở chế độ nâng cấp tất cả! Hãy tắt toggle trước.");
            return;
        }

        int emptySlot = -1;
        for (int i = 0; i < 3; i++)
        {
            if (selectedStonesForUpgrade[i] == null)
            {
                emptySlot = i;
                break;
            }
        }

        if (emptySlot == -1)
        {
            Debug.Log("Đã đầy 3 slot!");
            return;
        }

        if (emptySlot > 0)
        {
            int firstStoneLevel = selectedStonesForUpgrade[0]?.lever ?? 0;
            if (firstStoneLevel != 0 && stone.lever != firstStoneLevel)
            {
                ShowStoneErrorMessage("Phải chọn 3 viên đá cùng level!");
                return;
            }
        }

        if (!tempStoneCountUpgrade.ContainsKey(stone.idStone) || tempStoneCountUpgrade[stone.idStone] <= 0)
        {
            Debug.Log($"Hết {stone.name}!");
            return;
        }

        selectedStonesForUpgrade[emptySlot] = stone;
        tempStoneCountUpgrade[stone.idStone]--;

        if (txtCount != null)
        {
            txtCount.text = tempStoneCountUpgrade[stone.idStone].ToString();
        }

        Debug.Log($"Đã chọn: {stone.name} vào slot {emptySlot + 1}");

        UpdateStoneUpgradeSlotsUI();
    }

    void UpdateStoneUpgradeSlotsUI()
    {
        if (stoneUpgradeSlotsContainer == null) return;

        for (int i = 0; i < 3; i++)
        {
            Transform slot = stoneUpgradeSlotsContainer.Find($"btnStone ({i + 1})");
            if (slot == null) continue;

            Image imgStone = slot.Find("imgStone")?.GetComponent<Image>();
            Text txtnum = slot.Find("txtnum")?.GetComponent<Text>();
            Button btn = slot.GetComponent<Button>();

            if (selectedStonesForUpgrade[i] != null)
            {
                if (imgStone != null)
                {
                    Sprite stoneSprite = GetStoneSpriteByElement(selectedStonesForUpgrade[i].elementType, selectedStonesForUpgrade[i].lever);
                    if (stoneSprite != null)
                    {
                        imgStone.sprite = stoneSprite;
                        imgStone.enabled = true;

                        // ✅ THÊM: Pop animation
                        imgStone.transform.localScale = Vector3.zero;
                        LeanTween.scale(imgStone.gameObject, Vector3.one, 0.3f)
                            .setEase(LeanTweenType.easeOutBack);
                    }
                }

                if (txtnum != null)
                {
                    txtnum.text = "1";
                }

                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    int slotIndex = i;
                    btn.onClick.AddListener(() =>
                    {
                        AnimateButtonClick(btn); // ✅ THÊM animation click
                        RemoveStoneFromUpgradeSlot(slotIndex);
                    });
                }
            }
            else
            {
                if (imgStone != null)
                {
                    imgStone.enabled = false;
                }

                if (txtnum != null)
                {
                    txtnum.text = "";
                }

                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                }
            }
        }

        UpdateStoneMainDisplay();
        CalculateStoneUpgradePercent();
    }

    void RemoveStoneFromUpgradeSlot(int slotIndex)
    {
        if (selectedStonesForUpgrade[slotIndex] == null) return;

        StoneDTO removedStone = selectedStonesForUpgrade[slotIndex];

        if (tempStoneCountUpgrade.ContainsKey(removedStone.idStone))
        {
            tempStoneCountUpgrade[removedStone.idStone]++;
        }

        selectedStonesForUpgrade[slotIndex] = null;

        Debug.Log($"Đã bỏ chọn {removedStone.name} từ slot {slotIndex + 1}");

        UpdateStoneUpgradeListUI();
        UpdateStoneUpgradeSlotsUI();
    }

    void UpdateStoneUpgradeListUI()
    {
        foreach (Transform stoneObj in stoneUpgradeListContainer)
        {
            Button btn = stoneObj.GetComponent<Button>();
            if (btn == null) continue;

            Text txtCount = stoneObj.transform.Find("txtnum")?.GetComponent<Text>();
            if (txtCount != null)
            {
                StoneDTO[] allElementStones = GetStonesForElement(currentSelectedElement);
                if (allElementStones != null)
                {
                    foreach (var stone in allElementStones)
                    {
                        if (tempStoneCountUpgrade.ContainsKey(stone.idStone))
                        {
                            Image imgStone = stoneObj.transform.Find("imgStone")?.GetComponent<Image>();
                            if (imgStone != null)
                            {
                                Sprite stoneSprite = GetStoneSpriteByElement(stone.elementType, stone.lever);
                                if (imgStone.sprite == stoneSprite)
                                {
                                    txtCount.text = tempStoneCountUpgrade[stone.idStone].ToString();
                                    btn.interactable = tempStoneCountUpgrade[stone.idStone] > 0;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    void UpdateStoneMainDisplay()
    {
        if (StoneMain == null) return;

        if (selectedStonesForUpgrade[0] == null)
        {
            StoneMain.SetActive(false);
            return;
        }

        StoneMain.SetActive(true);

        int currentLevel = selectedStonesForUpgrade[0].lever;
        int nextLevel = currentLevel + 1;
        string elementType = selectedStonesForUpgrade[0].elementType;

        if (imgStoneMain != null)
        {
            Sprite nextLevelSprite = GetStoneSpriteByElement(elementType, nextLevel);
            if (nextLevelSprite != null)
            {
                imgStoneMain.sprite = nextLevelSprite;
            }
        }

        if (txtStoneMainLevel != null)
        {
            txtStoneMainLevel.text = $"Lv{nextLevel}";
        }
    }

    void CalculateStoneUpgradePercent()
    {
        if (txtUpgradePercentStone == null) return;

        int stoneCount = 0;
        for (int i = 0; i < 3; i++)
        {
            if (selectedStonesForUpgrade[i] != null) stoneCount++;
        }

        if (stoneCount == 0)
        {
            txtUpgradePercentStone.text = "0%";
            return;
        }

        if (stoneCount == 3)
        {
            int firstLevel = selectedStonesForUpgrade[0].lever;
            bool sameLev = true;
            for (int i = 1; i < 3; i++)
            {
                if (selectedStonesForUpgrade[i].lever != firstLevel)
                {
                    sameLev = false;
                    break;
                }
            }

            if (sameLev)
            {
                txtUpgradePercentStone.text = "80%";
            }
            else
            {
                txtUpgradePercentStone.text = "0%";
            }
        }
        else
        {
            txtUpgradePercentStone.text = "0%";
        }
    }

    void OnUpgradeStoneClicked()
    {
        bool isUpgradeAll = toggleUpgradeAll != null && toggleUpgradeAll.isOn;

        if (isUpgradeAll)
        {
            if (selectedStonesForUpgrade[0] == null)
            {
                ShowStoneErrorMessage("Hãy chọn loại đá muốn nâng cấp hết!");
                return;
            }

            UpgradeAllStonesOfSelectedType();
        }
        else
        {
            UpgradeThreeStones();
        }

        // ✅ THÊM: Button animation
        if (btnUpgradeStone != null)
        {
            AnimateButtonClick(btnUpgradeStone);
        }
    }
    void UpgradeAllStonesOfSelectedType()
    {
        // Kiểm tra đã chọn đá chưa
        if (selectedStonesForUpgrade[0] == null)
        {
            ShowStoneErrorMessage("Hãy chọn loại đá muốn nâng cấp hết!");
            return;
        }

        // Kiểm tra vàng
        if (currentUser == null || currentUser.gold < 5000)
        {
            ShowStoneErrorMessage("Bạn đã hết gold ^^!");
            return;
        }

        // ✅ CHỈ LẤY LOẠI ĐÁ ĐÃ CHỌN
        StoneDTO selectedStone = selectedStonesForUpgrade[0];

        Debug.Log($"🎯 Chọn nâng cấp HẾT loại: {selectedStone.name} Lv{selectedStone.lever}");

        // Kiểm tra có đủ đá không (ít nhất 3 viên)
        if (selectedStone.count < 3)
        {
            ShowStoneErrorMessage($"Không đủ {selectedStone.name}! Cần ít nhất 3 viên.");
            return;
        }

        // Tính số nhóm có thể nâng cấp
        int totalGroups = selectedStone.count / 3;
        int totalStones = totalGroups * 3;

        Debug.Log($"📊 Có {selectedStone.count} viên {selectedStone.name} → {totalGroups} nhóm × 3 viên");

        if (totalGroups == 0)
        {
            ShowStoneErrorMessage("Không đủ đá để nâng cấp (cần ít nhất 3 viên)!");
            return;
        }

        if (btnUpgradeStone != null)
        {
            btnUpgradeStone.interactable = false;
        }

        // ✅ TRỪ 5000 GOLD VÀ CẬP NHẬT DB
        StartCoroutine(DeductGoldAndUpgradeSelectedStone(selectedStone, totalGroups));
    }
    IEnumerator DeductGoldAndUpgradeSelectedStone(StoneDTO selectedStone, int totalGroups)
    {
        int userId = PlayerPrefs.GetInt("userId", 1);

        // Tạo request trừ vàng
        var deductGoldRequest = new DeductGoldRequestDTO
        {
            userId = userId,
            amount = 5000,
            reason = "upgrade_all_stones"
        };

        bool goldDeducted = false;
        string errorMessage = "";

        // Gọi API trừ vàng
        var apiCall = APIManager.Instance.PostRequest_Generic<DeductGoldResponseDTO>(
            APIConfig.DEDUCT_GOLD,
            deductGoldRequest,
            (response) =>
            {
                goldDeducted = true;
                Debug.Log($"✓ Đã trừ 5000 gold. Còn lại: {response.remainingGold}");

                // Cập nhật gold hiển thị ngay
                if (currentUser != null)
                {
                    currentUser.gold = response.remainingGold;
                    SetTextIfNotNull(txtVang, FormatVND(currentUser.gold));
                }
            },
            (error) =>
            {
                goldDeducted = false;
                errorMessage = error;
                Debug.LogError($"Lỗi trừ gold: {error}");
            }
        );

        yield return apiCall;

        if (!goldDeducted)
        {
            ShowStoneErrorMessage($"Lỗi trừ gold: {errorMessage}");

            if (btnUpgradeStone != null)
            {
                btnUpgradeStone.interactable = true;
            }
            yield break;
        }

        // Nếu trừ gold thành công, bắt đầu nâng cấp
        yield return StartCoroutine(UpgradeSelectedStoneSequence(selectedStone, totalGroups));
    }
    IEnumerator UpgradeSelectedStoneSequence(StoneDTO selectedStone, int totalGroups)
    {
        // ✅ TẠO DANH SÁCH CHỈ CHO LOẠI ĐÁ ĐÃ CHỌN
        List<StoneGroupDTO> stoneGroups = new List<StoneGroupDTO>();

        for (int i = 0; i < totalGroups; i++)
        {
            // Random success cho mỗi nhóm
            float random = UnityEngine.Random.Range(0f, 100f);
            bool success = random <= 80f;

            stoneGroups.Add(new StoneGroupDTO
            {
                stoneId = selectedStone.idStone,
                quantity = 3,
                success = success
            });
        }

        if (stoneGroups.Count == 0)
        {
            ShowStoneErrorMessage("Không có đá nào để nâng cấp!");

            // ✅ TẮT TOGGLE NẾU KHÔNG CÓ ĐÁ
            if (toggleUpgradeAll != null && toggleUpgradeAll.isOn)
            {
                toggleUpgradeAll.SetIsOnWithoutNotify(false);
                if (messageTextStone != null)
                    messageTextStone.gameObject.SetActive(false);
            }

            if (btnUpgradeStone != null)
                btnUpgradeStone.interactable = true;
            yield break;
        }

        Debug.Log($"📦 Sẽ nâng cấp {totalGroups} nhóm × 3 viên {selectedStone.name} Lv{selectedStone.lever}");

        // ✅ HIỂN THỊ ANIMATION
        if (anmtUpdateStone != null)
        {
            anmtUpdateStone.SetActive(true);
        }

        // ✅ TẠO REQUEST CHO BATCH UPGRADE
        var batchRequest = new StoneBatchUpgradeRequestDTO
        {
            userId = PlayerPrefs.GetInt("userId", 1),
            stoneGroups = stoneGroups.ToArray()
        };

        bool apiSuccess = false;
        StoneBatchUpgradeResponseDTO response = null;

        // ✅ CHỈ GỬI 1 API DUY NHẤT
        var apiCall = APIManager.Instance.PostRequest_Generic<StoneBatchUpgradeResponseDTO>(
            APIConfig.BATCH_UPGRADE_STONES,
            batchRequest,
            (res) =>
            {
                apiSuccess = true;
                response = res;
                Debug.Log($"✅ Batch upgrade hoàn tất: Success={res.successCount}, Fail={res.failCount}");
            },
            (error) =>
            {
                apiSuccess = false;
                Debug.LogError($"❌ Lỗi batch upgrade: {error}");
            }
        );

        yield return apiCall;

        // ✅ ẨN ANIMATION
        if (anmtUpdateStone != null)
        {
            Animator animator = anmtUpdateStone.GetComponent<Animator>();
            if (animator != null)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                yield return new WaitForSeconds(stateInfo.length);
            }
            anmtUpdateStone.SetActive(false);
        }

        // ✅ HIỂN THỊ KẾT QUẢ
        if (apiSuccess && response != null)
        {
            string resultMessage = response.message;

            if (txtResultUpdateStone != null)
            {
                txtResultUpdateStone.gameObject.SetActive(true);
                txtResultUpdateStone.text = resultMessage;
                txtResultUpdateStone.color = response.successCount > 0 ? originalResultColorStone : Color.gray;

                if (txtResultUpdateStone.color == Color.gray)
                {
                    Outline outline = txtResultUpdateStone.gameObject.GetComponent<Outline>();
                    if (outline == null)
                    {
                        outline = txtResultUpdateStone.gameObject.AddComponent<Outline>();
                    }
                    outline.effectColor = Color.white;
                }
                StartCoroutine(AnimateTextFlyUp(txtResultUpdateStone, 2f, 100f));
                StartCoroutine(HideStoneResultAfterDelay(5f));
            }

            // ✅ RELOAD DỮ LIỆU 1 LẦN
            yield return StartCoroutine(ReloadAllStonesAfterUpgrade());
        }
        else
        {
            ShowStoneErrorMessage("Lỗi khi nâng cấp đá!");
        }

        // ✅ TẮT TOGGLE SAU KHI HOÀN THÀNH
        if (toggleUpgradeAll != null && toggleUpgradeAll.isOn)
        {
            toggleUpgradeAll.SetIsOnWithoutNotify(false);

            if (messageTextStone != null)
            {
                messageTextStone.gameObject.SetActive(false);
            }

            Debug.Log("✓ Đã tắt toggle upgrade all");
        }

        // ✅ CLEAR SELECTED STONES
        selectedStonesForUpgrade = new StoneDTO[3];
        UpdateStoneUpgradeSlotsUI();

        if (btnUpgradeStone != null)
        {
            btnUpgradeStone.interactable = true;
        }
    }

    // ✅ THÊM: Nâng cấp hết tất cả đá cùng loại
    void UpgradeAllStones()
    {
        // Kiểm tra đã chọn hệ chưa
        if (string.IsNullOrEmpty(currentSelectedElement))
        {
            ShowStoneErrorMessage("Chưa chọn hệ đá!");
            return;
        }

        // Kiểm tra vàng
        if (currentUser == null || currentUser.gold < 5000)
        {
            ShowStoneErrorMessage("Bạn đã hết gold ^^!");
            return;
        }

        // Lấy tất cả đá của hệ đã chọn
        StoneDTO[] stones = GetStonesForElement(currentSelectedElement);
        if (stones == null || stones.Length == 0)
        {
            ShowStoneErrorMessage("Không có đá để nâng cấp!");
            return;
        }

        // Tìm level cao nhất
        int maxLevel = 0;
        foreach (var stone in stones)
        {
            if (stone.lever > maxLevel)
                maxLevel = stone.lever;
        }

        // Lấy tất cả đá có thể nâng cấp (loại bỏ level max và đá hết)
        List<StoneDTO> availableStones = new List<StoneDTO>();
        foreach (var stone in stones)
        {
            if (stone.lever < maxLevel && stone.count > 0)
            {
                availableStones.Add(stone);
            }
        }

        if (availableStones.Count == 0)
        {
            ShowStoneErrorMessage("Không có đá nào có thể nâng cấp!");
            return;
        }

        // Tính tổng số lượng đá có thể nâng
        int totalStones = 0;
        foreach (var stone in availableStones)
        {
            totalStones += stone.count;
        }

        Debug.Log($"Tổng {totalStones} viên đá có thể nâng cấp");

        if (btnUpgradeStone != null)
        {
            btnUpgradeStone.interactable = false;
        }

        // ✅ TRỪ 5000 GOLD VÀ CẬP NHẬT DB
        StartCoroutine(DeductGoldAndUpgradeAll(availableStones));
    }

    // ✅ THÊM: Coroutine trừ vàng trước khi upgrade all
    IEnumerator DeductGoldAndUpgradeAll(List<StoneDTO> availableStones)
    {
        int userId = PlayerPrefs.GetInt("userId", 1);

        // Tạo request trừ vàng
        var deductGoldRequest = new DeductGoldRequestDTO
        {
            userId = userId,
            amount = 5000,
            reason = "upgrade_all_stones"
        };

        bool goldDeducted = false;
        string errorMessage = "";

        // Gọi API trừ vàng
        var apiCall = APIManager.Instance.PostRequest_Generic<DeductGoldResponseDTO>(
            APIConfig.DEDUCT_GOLD, // Cần thêm endpoint này
            deductGoldRequest,
            (response) =>
            {
                goldDeducted = true;
                Debug.Log($"✓ Đã trừ 5000 gold. Còn lại: {response.remainingGold}");

                // Cập nhật gold hiển thị ngay
                if (currentUser != null)
                {
                    currentUser.gold = response.remainingGold;
                    SetTextIfNotNull(txtVang, FormatVND(currentUser.gold));
                }
            },
            (error) =>
            {
                goldDeducted = false;
                errorMessage = error;
                Debug.LogError($"Lỗi trừ gold: {error}");
            }
        );

        yield return apiCall;

        if (!goldDeducted)
        {
            ShowStoneErrorMessage($"Lỗi trừ gold: {errorMessage}");

            if (btnUpgradeStone != null)
            {
                btnUpgradeStone.interactable = true;
            }
            yield break;
        }

        // Nếu trừ gold thành công, bắt đầu nâng cấp
        yield return StartCoroutine(UpgradeAllStonesSequence(availableStones));
    }

    // ✅ THÊM: Coroutine nâng cấp tất cả đá
    IEnumerator UpgradeAllStonesSequence(List<StoneDTO> availableStones)
    {
        // ✅ TẠO DANH SÁCH TẤT CẢ NHÓM ĐÁ 1 LẦN
        List<StoneGroupDTO> stoneGroups = new List<StoneGroupDTO>();

        foreach (var stone in availableStones)
        {
            int count = stone.count;

            // Chia thành các nhóm 3 viên
            while (count >= 3)
            {
                // Random success cho mỗi nhóm
                float random = UnityEngine.Random.Range(0f, 100f);
                bool success = random <= 80f;

                stoneGroups.Add(new StoneGroupDTO
                {
                    stoneId = stone.idStone,
                    quantity = 3,
                    success = success
                });

                count -= 3;
            }
        }

        if (stoneGroups.Count == 0)
        {
            ShowStoneErrorMessage("Không có đá nào để nâng cấp!");

            // ✅ TẮT TOGGLE NẾU KHÔNG CÓ ĐÁ
            if (toggleUpgradeAll != null && toggleUpgradeAll.isOn)
            {
                toggleUpgradeAll.SetIsOnWithoutNotify(false);
                if (messageTextStone != null)
                    messageTextStone.gameObject.SetActive(false);
            }

            if (btnUpgradeStone != null)
                btnUpgradeStone.interactable = true;
            yield break;
        }

        Debug.Log($"📦 Sẽ gửi 1 API batch upgrade cho {stoneGroups.Count} nhóm đá");

        // ✅ HIỂN THỊ ANIMATION
        if (anmtUpdateStone != null)
        {
            anmtUpdateStone.SetActive(true);
        }

        // ✅ TẠO REQUEST CHO BATCH UPGRADE
        var batchRequest = new StoneBatchUpgradeRequestDTO
        {
            userId = PlayerPrefs.GetInt("userId", 1),
            stoneGroups = stoneGroups.ToArray()
        };

        bool apiSuccess = false;
        StoneBatchUpgradeResponseDTO response = null;

        // ✅ CHỈ GỬI 1 API DUY NHẤT
        var apiCall = APIManager.Instance.PostRequest_Generic<StoneBatchUpgradeResponseDTO>(
            APIConfig.BATCH_UPGRADE_STONES,
            batchRequest,
            (res) =>
            {
                apiSuccess = true;
                response = res;
                Debug.Log($"✅ Batch upgrade hoàn tất: Success={res.successCount}, Fail={res.failCount}");
            },
            (error) =>
            {
                apiSuccess = false;
                Debug.LogError($"❌ Lỗi batch upgrade: {error}");
            }
        );

        yield return apiCall;

        // ✅ ẨN ANIMATION
        if (anmtUpdateStone != null)
        {
            Animator animator = anmtUpdateStone.GetComponent<Animator>();
            if (animator != null)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                yield return new WaitForSeconds(stateInfo.length);
            }
            anmtUpdateStone.SetActive(false);
        }

        // ✅ HIỂN THỊ KẾT QUẢ
        if (apiSuccess && response != null)
        {
            string resultMessage = response.message;

            if (txtResultUpdateStone != null)
            {
                txtResultUpdateStone.gameObject.SetActive(true);
                txtResultUpdateStone.text = resultMessage;
                txtResultUpdateStone.color = response.successCount > 0 ? originalResultColorStone : Color.gray;

                if (txtResultUpdateStone.color == Color.gray)
                {
                    Outline outline = txtResultUpdateStone.gameObject.GetComponent<Outline>();
                    if (outline == null)
                    {
                        outline = txtResultUpdateStone.gameObject.AddComponent<Outline>();
                    }
                    outline.effectColor = Color.white;
                }

                StartCoroutine(HideStoneResultAfterDelay(5f));
            }

            // ✅ RELOAD DỮ LIỆU 1 LẦN
            yield return StartCoroutine(ReloadAllStonesAfterUpgrade());
        }
        else
        {
            ShowStoneErrorMessage("Lỗi khi nâng cấp đá!");
        }

        // ✅ TẮT TOGGLE SAU KHI HOÀN THÀNH
        if (toggleUpgradeAll != null && toggleUpgradeAll.isOn)
        {
            toggleUpgradeAll.SetIsOnWithoutNotify(false);

            if (messageTextStone != null)
            {
                messageTextStone.gameObject.SetActive(false);
            }

            Debug.Log("✓ Đã tắt toggle upgrade all");
        }

        if (btnUpgradeStone != null)
        {
            btnUpgradeStone.interactable = true;
        }
    }

    // ==================== THÊM DTO MỚI ====================

    [Serializable]
    public class StoneGroupDTO
    {
        public long stoneId;
        public int quantity;
        public bool success;
    }

    [Serializable]
    public class StoneBatchUpgradeRequestDTO
    {
        public int userId;
        public StoneGroupDTO[] stoneGroups;
    }

    [Serializable]
    public class StoneBatchUpgradeResponseDTO
    {
        public bool success;
        public string message;
        public int successCount;
        public int failCount;
        public string[] details; // Chi tiết từng lần upgrade (optional)
    }

    // ✅ THÊM: API call cho upgrade all (có callback)
    IEnumerator UpgradeStoneAPI_ForAll(StoneUpgradeRequestDTO request, System.Action<bool> callback)
    {
        string stoneIdsJson = string.Join(",", request.stoneIds);
        string json = $"{{\"userId\":{request.userId},\"stoneIds\":[{stoneIdsJson}],\"success\":{request.success.ToString().ToLower()},\"upgradeAll\":{request.upgradeAll.ToString().ToLower()}}}";

        bool success = false;

        var apiCall = APIManager.Instance.PostRequest_Generic<StoneUpgradeResponseDTO>(
            APIConfig.UPGRADE_STONE,
            request,
            (response) =>
            {
                success = response.success;
                Debug.Log($"Upgrade: {response.message}");
            },
            (error) =>
            {
                success = false;
                Debug.LogError($"Lỗi: {error}");
            }
        );

        yield return apiCall;

        callback?.Invoke(success);
    }

    // ✅ THÊM: Nâng cấp 3 viên bình thường
    void UpgradeThreeStones()
    {
        int stoneCount = 0;
        for (int i = 0; i < 3; i++)
        {
            if (selectedStonesForUpgrade[i] != null) stoneCount++;
        }

        if (stoneCount < 3)
        {
            ShowStoneErrorMessage("Phải chọn đủ 3 viên đá!");
            return;
        }

        int firstLevel = selectedStonesForUpgrade[0].lever;
        for (int i = 1; i < 3; i++)
        {
            if (selectedStonesForUpgrade[i].lever != firstLevel)
            {
                ShowStoneErrorMessage("3 viên đá phải cùng level!");
                return;
            }
        }

        if (btnUpgradeStone != null)
        {
            btnUpgradeStone.interactable = false;
        }

        float random = UnityEngine.Random.Range(0f, 100f);
        bool success = random <= 80f;

        List<long> stoneList = new List<long>();
        for (int i = 0; i < 3; i++)
        {
            if (selectedStonesForUpgrade[i] != null)
            {
                stoneList.Add(selectedStonesForUpgrade[i].idStone);
            }
        }

        var upgradeRequest = new StoneUpgradeRequestDTO
        {
            userId = PlayerPrefs.GetInt("userId", 1),
            stoneIds = stoneList.ToArray(),
            success = success,
            upgradeAll = false
        };

        StartCoroutine(UpgradeStoneAPI(upgradeRequest));
    }

    IEnumerator UpgradeStoneAPI(StoneUpgradeRequestDTO request)
    {
        string stoneIdsJson = string.Join(",", request.stoneIds);
        string json = $"{{\"userId\":{request.userId},\"stoneIds\":[{stoneIdsJson}],\"success\":{request.success.ToString().ToLower()},\"upgradeAll\":{request.upgradeAll.ToString().ToLower()}}}";
        Debug.Log($"📤 Stone Upgrade JSON: {json}");

        var apiCall = APIManager.Instance.PostRequest_Generic<StoneUpgradeResponseDTO>(
            APIConfig.UPGRADE_STONE,
            request,
            OnStoneUpgradeSuccess,
            OnStoneUpgradeError
        );

        yield return apiCall;
    }

    void OnStoneUpgradeSuccess(StoneUpgradeResponseDTO response)
    {
        Debug.Log($"Stone upgrade result: {response.message}");

        // ✅ THÊM: Hiển thị animation và đợi nó chạy xong
        if (anmtUpdateStone != null)
        {
            anmtUpdateStone.SetActive(true);
            StartCoroutine(WaitForStoneAnimationThenReload(response));
        }
        else
        {
            // Nếu không có animation thì xử lý bình thường
            HandleStoneUpgradeResult(response);
        }
    }

    // ✅ THÊM: Coroutine đợi animation đá chạy xong
    IEnumerator WaitForStoneAnimationThenReload(StoneUpgradeResponseDTO response)
    {
        // Lấy Animator component từ anmtUpdateStone
        Animator animator = anmtUpdateStone.GetComponent<Animator>();

        if (animator != null)
        {
            // Chờ animation clip chạy xong
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            float animationLength = stateInfo.length;

            Debug.Log($"Animation stone sẽ chạy trong {animationLength} giây");

            // Chờ animation chạy xong
            yield return new WaitForSeconds(animationLength);
        }
        else
        {
            // Nếu không có Animator, chờ 2 giây mặc định
            Debug.LogWarning("Không tìm thấy Animator trên anmtUpdateStone, chờ 2 giây mặc định");
            yield return new WaitForSeconds(2f);
        }

        // Ẩn animation
        if (anmtUpdateStone != null)
        {
            anmtUpdateStone.SetActive(false);
        }

        // Xử lý kết quả và reload data
        HandleStoneUpgradeResult(response);
    }

    // ✅ THÊM: Method xử lý kết quả upgrade đá
    void HandleStoneUpgradeResult(StoneUpgradeResponseDTO response)
    {
        if (txtResultUpdateStone != null)
        {
            txtResultUpdateStone.gameObject.SetActive(true);
            txtResultUpdateStone.text = response.message;

            if (response.success)
            {
                txtResultUpdateStone.color = originalResultColorStone;
                Outline outline = txtResultUpdateStone.gameObject.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = txtResultUpdateStone.gameObject.AddComponent<Outline>();
                }
                outline.effectColor = Color.red;
            }
            else
            {
                txtResultUpdateStone.color = Color.gray;
                Outline outline = txtResultUpdateStone.gameObject.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = txtResultUpdateStone.gameObject.AddComponent<Outline>();
                }
                outline.effectColor = Color.white;
            }

            // ✅ THÊM: Hiệu ứng bay lên
            StartCoroutine(AnimateTextFlyUp(txtResultUpdateStone, 2f, 100f));
            StartCoroutine(HideStoneResultAfterDelay(3f));
        }

        selectedStonesForUpgrade = new StoneDTO[3];
        UpdateStoneUpgradeSlotsUI();
        StartCoroutine(ReloadAllStonesAfterUpgrade());

        if (btnUpgradeStone != null)
        {
            btnUpgradeStone.interactable = true;
        }
    }

    // ✅ THÊM: Coroutine để reload tất cả dữ liệu đá sau khi upgrade
    IEnumerator ReloadAllStonesAfterUpgrade()
    {
        int userId = PlayerPrefs.GetInt("userId", 1);

        var stonesRequest = APIManager.Instance.GetRequest<StoneResponse>(
            APIConfig.GET_STONES(userId),
            OnAllStonesReloadedAfterUpgrade,
            OnError
        );

        yield return stonesRequest;
    }

    // ✅ THÊM: Callback xử lý sau khi reload stones
    void OnAllStonesReloadedAfterUpgrade(StoneResponse stones)
    {
        allStones = stones;
        Debug.Log("✓ Đã reload tất cả dữ liệu đá sau khi upgrade!");

        // 1. Reload danh sách đá trong PanelUpdateStone (nếu đang mở)
        if (!string.IsNullOrEmpty(currentSelectedElement))
        {
            LoadStonesForUpgrade(currentSelectedElement);
        }

        // 2. Reload danh sách đá ở panel chính (nếu đang chọn pet)
        if (currentSelectedPet != null)
        {
            LoadStonesForElement(currentSelectedPet.elementType);
        }
    }

    void OnStoneUpgradeError(string error)
    {
        Debug.LogError($"Lỗi upgrade stone: {error}");

        if (txtResultUpdateStone != null)
        {
            txtResultUpdateStone.gameObject.SetActive(true);
            txtResultUpdateStone.text = "Lỗi: " + error;
            txtResultUpdateStone.color = Color.red;
            StartCoroutine(HideStoneResultAfterDelay(3f));
        }

        if (btnUpgradeStone != null)
        {
            btnUpgradeStone.interactable = true;
        }
    }

    IEnumerator HideStoneResultAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (txtResultUpdateStone != null)
        {
            txtResultUpdateStone.gameObject.SetActive(false);
            txtResultUpdateStone.color = originalResultColorStone;
            Outline outline = txtResultUpdateStone.gameObject.GetComponent<Outline>();
            if (outline == null)
            {
                outline = txtResultUpdateStone.gameObject.AddComponent<Outline>();
            }
            outline.effectColor = Color.red;
        }
    }

    void ShowStoneErrorMessage(string message)
    {
        Debug.Log(message);

        if (txtResultUpdateStone != null)
        {
            txtResultUpdateStone.gameObject.SetActive(true);
            txtResultUpdateStone.text = message;
            txtResultUpdateStone.color = Color.gray;
            Outline outline = txtResultUpdateStone.gameObject.GetComponent<Outline>();
            if (outline == null)
            {
                outline = txtResultUpdateStone.gameObject.AddComponent<Outline>();
            }
            outline.effectColor = Color.white;
            StartCoroutine(HideStoneResultAfterDelay(3f));
        }
    }

    void ResetStoneUpgradeUI()
    {
        selectedStonesForUpgrade = new StoneDTO[3];
        tempStoneCountUpgrade.Clear();
        currentSelectedElement = "";

        if (PanelStoneUpgrade != null)
            PanelStoneUpgrade.SetActive(false);

        if (StoneMain != null)
            StoneMain.SetActive(false);

        if (txtUpgradePercentStone != null)
            txtUpgradePercentStone.text = "0%";

        UpdateStoneUpgradeSlotsUI();
    }

    // ==================== ORIGINAL PET METHODS ====================

    void OnToggleProtectionChanged(bool isOn)
    {
        if (messageText != null)
        {
            messageText.gameObject.SetActive(false);

            if (isOn)
            {
                if (currentUser != null && currentUser.gold < 5000)
                {
                    toggleProtection.SetIsOnWithoutNotify(false);
                    messageText.text = "Bạn đã hết gold ^^!";
                    messageText.gameObject.SetActive(true);
                    StartCoroutine(HideMessageAfterDelay(3f));
                }
                else
                {
                    messageText.text = "Bảo vệ pet khỏi giảm cấp (-5000 gold)";
                    messageText.gameObject.SetActive(true);
                }
            }
        }
    }

    IEnumerator HideMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (messageText != null)
        {
            messageText.gameObject.SetActive(false);
        }
    }

    private IEnumerator LoadSceneAfterDelay()
    {
        ManagerGame.Instance.LoadingPanel = LoadingPanel;
        int userId = PlayerPrefs.GetInt("userId", 1);
        ManagerGame.Instance.ShowLoading();

        var petsRequest = APIManager.Instance.GetRequest<List<PetUserDTO>>(
            APIConfig.GET_ALL_PET_USERS(userId),
            OnPetsReceived,
            OnError
        );

        var userRequest = APIManager.Instance.GetRequest<UserDTO>(
            APIConfig.GET_USER(userId),
            OnUserReceived,
            OnError
        );

        var stonesRequest = APIManager.Instance.GetRequest<StoneResponse>(
            APIConfig.GET_STONES(userId),
            OnStonesReceived,
            OnError
        );

        yield return petsRequest;
        yield return userRequest;
        yield return stonesRequest;

        ManagerGame.Instance.HideLoading();

        if (firstPet != null)
        {
            OnPetClicked(
                firstPet.petId.ToString(),
                firstPet.name,
                firstPet.attack,
                firstPet.hp,
                firstPet.mana,
                firstPet.level,
                firstPet.elementType,
                firstPet.elementOther,
                firstPet.weaknessValue,
                firstPet.des,
                firstPet,
                firstPet.skillCardId
            );
        }
    }

    void OnPetsReceived(List<PetUserDTO> pets)
    {
        if (pets != null && pets.Count > 0)
        {
            firstPet = pets[0];
        }

        foreach (var pet in pets)
        {
            GameObject petUIObject = Instantiate(petUIPrefab, petListContainer);
            SetupPetUI(petUIObject, pet);
        }
    }

    void OnStonesReceived(StoneResponse stones)
    {
        allStones = stones;
        Debug.Log("✓ Đã load stones thành công!");
    }

    void SetupPetUI(GameObject petUIObject, PetUserDTO pet)
    {
        Transform imgPetTransform = petUIObject.transform.Find("imgtPet");
        if (imgPetTransform == null)
        {
            Debug.LogError("Không tìm thấy thành phần imgPet trong prefab!");
            return;
        }

        Image petIcon = imgPetTransform.GetComponent<Image>();
        Animator petAnimator = imgPetTransform.GetComponent<Animator>();
        string petID = pet.petId.ToString();

        SetupFallbackImage(petIcon, petID, petAnimator);
        TrySetupPetAnimation(petAnimator, petID);

        SetupPetInfo(petUIObject, pet, petID);
SetupButtonSounds();
        // ✅ THÊM: Animate pet item khi spawn
        int index = petListContainer.childCount - 1;
        AnimateItemAppear(petUIObject, index);
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
            AnimationClip[] clips = Resources.LoadAll<AnimationClip>($"Pets/{petID}");

            if (clips.Length == 0)
            {
                Debug.LogWarning($"Không tìm thấy animation clips cho pet {petID}");
                return false;
            }

            RuntimeAnimatorController baseController = petAnimator.runtimeAnimatorController;

            if (baseController == null)
            {
                Debug.LogError($"Pet {petID}: Animator không có controller!");
                return false;
            }

            AnimatorOverrideController overrideController = new AnimatorOverrideController(baseController);
            overrideController.name = $"Override_{petID}_{petAnimator.GetInstanceID()}";

            int overrideCount = 0;
            foreach (var clip in clips)
            {
                overrideController[clip.name] = clip;
                overrideCount++;
            }

            if (overrideCount == 0)
            {
                Debug.LogWarning($"Pet {petID}: Không override được clip nào!");
                return false;
            }

            petAnimator.runtimeAnimatorController = overrideController;
            petAnimator.enabled = true;

            Debug.Log($"✓ Pet {petID}: Setup thành công {overrideCount} animation clips");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Lỗi khi tải animation cho pet {petID}: {e.Message}");
            return false;
        }
    }

    void SetupFallbackImage(Image petIcon, string petID, Animator petAnimator)
    {
        if (petIcon == null) return;

        Sprite fallbackSprite = Resources.Load<Sprite>($"Image/IconsPet/{petID}");
        if (fallbackSprite != null)
        {
            petIcon.sprite = fallbackSprite;
        }
        else
        {
            Debug.LogError($"Không tìm thấy hình tĩnh cho pet {petID}");
        }

        if (petAnimator != null)
        {
            petAnimator.enabled = false;
        }
    }

    void SetupPetInfo(GameObject petUIObject, PetUserDTO pet, string petID)
    {
        Image imgHe = petUIObject.transform.Find("imgHe")?.GetComponent<Image>();
        if (imgHe != null)
        {
            Sprite attributeSprite = Resources.Load<Sprite>($"Image/Attribute/{pet.elementType}");
            if (attributeSprite != null)
            {
                imgHe.sprite = attributeSprite;
            }
        }

        Text txtLv = petUIObject.transform.Find("txtLv")?.GetComponent<Text>();
        if (txtLv != null)
        {
            txtLv.text = $"Lv{pet.level}";
        }

        Button petButton = petUIObject.GetComponent<Button>();
        if (petButton != null)
        {
            petButton.onClick.AddListener(() => OnPetClicked(
                petID,
                pet.name,
                pet.attack,
                pet.hp,
                pet.mana,
                pet.level,
                pet.elementType,
                pet.elementOther,
                pet.weaknessValue,
                pet.des,
                pet,
                pet.skillCardId
            ));
        }
    }

    void OnPetClicked(string petId, string name, int attack, int hp, int mana,
                 int maxLevel, string elementType, string elementOther,
                 double weaknessValue, string txtDes, PetUserDTO pet, int skillCardId)
    {
        Debug.Log($"=== CLICK PET {petId} ===");
        currentSelectedPet = pet;

        selectedStones = new StoneDTO[3];
        UpdateSelectedStonesUI();

        if (imgPetAnimator != null)
        {
            RuntimeAnimatorController baseController = imgPetAnimator.runtimeAnimatorController;

            if (baseController == null)
            {
                Debug.LogError("⚠️ imgPetAnimator chưa có RuntimeAnimatorController! Hãy gán trong Inspector");
                return;
            }

            AnimationClip[] clips = Resources.LoadAll<AnimationClip>($"Pets/{petId}");

            if (clips.Length == 0)
            {
                Debug.LogWarning($"Không có animation cho pet {petId}");
                return;
            }

            Debug.Log($"✓ Tìm thấy {clips.Length} animation clips cho pet {petId}");

            AnimatorOverrideController overrideController = new AnimatorOverrideController(baseController);
            overrideController.name = $"PetOverride_{petId}";

            foreach (var clip in clips)
            {
                overrideController[clip.name] = clip;
                Debug.Log($"  - Override clip: {clip.name}");
            }

            imgPetAnimator.enabled = false;
            imgPetAnimator.runtimeAnimatorController = overrideController;
            imgPetAnimator.enabled = true;
            imgPetAnimator.Rebind();
            imgPetAnimator.Update(0f);
            imgPetAnimator.Play(clips[0].name, 0, 0f);

            Debug.Log($"✓ Animation đã kích hoạt cho pet {petId}!");

            // ✅ THÊM: Scale animation cho pet animator
            imgPetAnimator.transform.localScale = Vector3.zero;
            LeanTween.scale(imgPetAnimator.gameObject, Vector3.one, 0.5f)
                .setEase(LeanTweenType.easeOutBack);
        }

        // ✅ THÊM: Animate stats với counter
        int oldAttack = currentSelectedPet != null ? currentSelectedPet.attack : 0;
        int oldHp = currentSelectedPet != null ? currentSelectedPet.hp : 0;
        int oldMana = currentSelectedPet != null ? currentSelectedPet.mana : 0;

        AnimateStatUpdate(txtDame, oldAttack, attack, 0.1f);
        AnimateStatUpdate(txtHp, oldHp, hp, 0.15f);
        AnimateStatUpdate(txtMana, oldMana, mana, 0.2f);

        SetTextIfNotNull(namePet, name);
        SetTextIfNotNull(des, txtDes);
        SetTextIfNotNull(txtWee, $"+{weaknessValue}");
        SetTextIfNotNull(txtLv, $"Lv {maxLevel}");

        // ✅ THÊM: Animate name
        if (namePet != null)
        {
            namePet.transform.localScale = Vector3.zero;
            LeanTween.scale(namePet.gameObject, Vector3.one, 0.3f)
                .setDelay(0.05f)
                .setEase(LeanTweenType.easeOutBack);
        }

        LoadAttributeImage(imgAtribute, elementType);
        LoadAttributeImage(imgAtributeOther, elementOther);

        // ✅ THÊM: Animate attribute images
        if (imgAtribute != null)
        {
            imgAtribute.transform.localScale = Vector3.zero;
            LeanTween.scale(imgAtribute.gameObject, Vector3.one, 0.3f)
                .setDelay(0.25f)
                .setEase(LeanTweenType.easeOutBack);
        }

        if (imgAtributeOther != null)
        {
            imgAtributeOther.transform.localScale = Vector3.zero;
            LeanTween.scale(imgAtributeOther.gameObject, Vector3.one, 0.3f)
                .setDelay(0.3f)
                .setEase(LeanTweenType.easeOutBack);
        }

        LoadStonesForElement(elementType);
        LoadPetSkillCard(skillCardId, txtDes);
    }

    public static string FormatVND(long amount)
    {
        return amount.ToString("#,##0").Replace(",", ".");
    }

    void LoadStonesForElement(string elementType)
    {
        if (allStones == null || stoneListContainer == null || stonePrefab == null)
        {
            Debug.LogError("Missing stones data or UI components!");
            return;
        }

        foreach (Transform child in stoneListContainer)
        {
            Destroy(child.gameObject);
        }

        tempStoneCount.Clear();

        StoneDTO[] stones = GetStonesForElement(elementType);

        if (stones == null || stones.Length == 0)
        {
            Debug.Log($"Không có đá cho hệ {elementType}");
            return;
        }

        foreach (var stone in stones)
        {
            tempStoneCount[stone.idStone] = stone.count;
        }

        foreach (var stone in stones)
        {
            GameObject stoneObj = Instantiate(stonePrefab, stoneListContainer);
            SetupStoneUI(stoneObj, stone);
        }

        Debug.Log($"✓ Đã load {stones.Length} loại đá hệ {elementType}");
    }

    StoneDTO[] GetStonesForElement(string element)
    {
        switch (element.ToUpper())
        {
            case "FIRE": return allStones.FIRE;
            case "WATER": return allStones.WATER;
            case "EARTH": return allStones.EARTH;
            case "METAL": return allStones.METAL;
            case "WOOD": return allStones.WOOD;
            default: return null;
        }
    }

    void SetupStoneUI(GameObject stoneObj, StoneDTO stone)
    {
        Image imgStone = stoneObj.transform.Find("imgStone")?.GetComponent<Image>();
        if (imgStone != null)
        {
            Sprite stoneSprite = GetStoneSpriteByElement(stone.elementType, stone.lever);
            if (stoneSprite != null)
            {
                imgStone.sprite = stoneSprite;
            }
            else
            {
                Debug.LogWarning($"Chưa gán sprite cho {stone.elementType} Lv{stone.lever}");
            }
        }

        Text txtCount = stoneObj.transform.Find("txtnum")?.GetComponent<Text>();
        if (txtCount != null)
        {
            txtCount.text = stone.count.ToString();
        }

        Button btnStone = stoneObj.GetComponent<Button>();
        if (btnStone != null)
        {
            btnStone.onClick.AddListener(() =>
            {
                AnimateButtonClick(btnStone); // ✅ THÊM animation click
                OnStoneClicked(stone, txtCount);
            });
        }

        if (stone.count == 0 && btnStone != null)
        {
            btnStone.interactable = false;
        }

        // ✅ THÊM: Animate stone item khi spawn
        int index = stoneListContainer.childCount - 1;
        AnimateItemAppear(stoneObj, index, 0.2f);
    }

    Sprite GetStoneSpriteByElement(string elementType, int level)
    {
        int index = level - 1;

        List<Sprite> spriteList = null;

        switch (elementType.ToUpper())
        {
            case "FIRE": spriteList = fireStoneSprites; break;
            case "WATER": spriteList = waterStoneSprites; break;
            case "EARTH": spriteList = earthStoneSprites; break;
            case "METAL": spriteList = electricStoneSprites; break;
            case "WOOD": spriteList = woodStoneSprites; break;
        }

        if (spriteList != null && index >= 0 && index < spriteList.Count)
        {
            return spriteList[index];
        }

        return null;
    }

    void OnStoneClicked(StoneDTO stone, Text txtCount)
    {
        int emptySlot = -1;
        for (int i = 0; i < 3; i++)
        {
            if (selectedStones[i] == null)
            {
                emptySlot = i;
                break;
            }
        }

        if (emptySlot == -1)
        {
            Debug.Log("Đã đầy 3 slot!");
            return;
        }

        if (!tempStoneCount.ContainsKey(stone.idStone) || tempStoneCount[stone.idStone] <= 0)
        {
            Debug.Log($"Hết {stone.name}!");
            return;
        }

        selectedStones[emptySlot] = stone;

        tempStoneCount[stone.idStone]--;

        if (txtCount != null)
        {
            txtCount.text = tempStoneCount[stone.idStone].ToString();
        }

        Debug.Log($"Đã chọn: {stone.name} vào slot {emptySlot + 1} (Còn: {tempStoneCount[stone.idStone]})");

        UpdateSelectedStonesUI();
    }

    void UpdateSelectedStonesUI()
    {
        if (updateSlotsContainer == null) return;

        for (int i = 0; i < 3; i++)
        {
            Transform slot = updateSlotsContainer.Find($"btnStone ({i + 1})");
            if (slot == null) continue;

            Image imgStone = slot.Find("imgStone")?.GetComponent<Image>();
            Text txtnum = slot.Find("txtnum")?.GetComponent<Text>();
            Button btn = slot.GetComponent<Button>();

            if (selectedStones[i] != null)
            {
                if (imgStone != null)
                {
                    Sprite stoneSprite = GetStoneSpriteByElement(selectedStones[i].elementType, selectedStones[i].lever);
                    if (stoneSprite != null)
                    {
                        imgStone.sprite = stoneSprite;
                        imgStone.enabled = true;

                        // ✅ THÊM: Pop animation khi đá được thêm vào
                        imgStone.transform.localScale = Vector3.zero;
                        LeanTween.scale(imgStone.gameObject, Vector3.one, 0.3f)
                            .setEase(LeanTweenType.easeOutBack);
                    }
                }

                if (txtnum != null)
                {
                    txtnum.text = "1";
                }

                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    int slotIndex = i;
                    btn.onClick.AddListener(() =>
                    {
                        AnimateButtonClick(btn); // ✅ THÊM animation click
                        RemoveStoneFromSlot(slotIndex);
                    });
                }
            }
            else
            {
                if (imgStone != null)
                {
                    imgStone.enabled = false;
                }

                if (txtnum != null)
                {
                    txtnum.text = "";
                }

                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                }
            }
        }

        CalculateUpgradePercent();
    }

    void RemoveStoneFromSlot(int slotIndex)
    {
        if (selectedStones[slotIndex] == null) return;

        StoneDTO removedStone = selectedStones[slotIndex];

        if (tempStoneCount.ContainsKey(removedStone.idStone))
        {
            tempStoneCount[removedStone.idStone]++;
        }

        selectedStones[slotIndex] = null;

        Debug.Log($"Đã bỏ chọn {removedStone.name} từ slot {slotIndex + 1}");

        UpdateStoneListUI();

        UpdateSelectedStonesUI();
    }

    void UpdateStoneListUI()
    {
        foreach (Transform stoneObj in stoneListContainer)
        {
            Button btn = stoneObj.GetComponent<Button>();
            if (btn == null) continue;

            Text txtCount = stoneObj.transform.Find("txtnum")?.GetComponent<Text>();
            if (txtCount != null)
            {
                StoneDTO[] allElementStones = GetStonesForElement(currentSelectedPet.elementType);
                if (allElementStones != null)
                {
                    foreach (var stone in allElementStones)
                    {
                        if (tempStoneCount.ContainsKey(stone.idStone))
                        {
                            Image imgStone = stoneObj.transform.Find("imgStone")?.GetComponent<Image>();
                            if (imgStone != null)
                            {
                                Sprite stoneSprite = GetStoneSpriteByElement(stone.elementType, stone.lever);
                                if (imgStone.sprite == stoneSprite)
                                {
                                    txtCount.text = tempStoneCount[stone.idStone].ToString();

                                    btn.interactable = tempStoneCount[stone.idStone] > 0;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    void CalculateUpgradePercent()
    {
        if (txtUpgradePercent == null) return;

        if (currentSelectedPet == null)
        {
            txtUpgradePercent.text = "0%";
            return;
        }

        int petLevel = currentSelectedPet.level;
        float totalValue = 0f;
        int stoneCount = 0;

        for (int i = 0; i < 3; i++)
        {
            if (selectedStones[i] != null)
            {
                stoneCount++;
                float stoneRate = CalculateStoneRate(petLevel, selectedStones[i].lever);
                totalValue += stoneRate;
            }
        }

        if (stoneCount == 0)
        {
            txtUpgradePercent.text = "0%";
            return;
        }

        totalValue = Mathf.Min(totalValue, 100f);

        string displayText;
        if (totalValue < 1f)
        {
            displayText = totalValue.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + "%";
        }
        else
        {
            displayText = Mathf.RoundToInt(totalValue).ToString() + "%";
        }
        txtUpgradePercent.text = displayText;

        Debug.Log($"Tỷ lệ nâng cấp: {totalValue}%");
    }

    float CalculateStoneRate(int petLevel, int stoneLevel)
    {
        float rate = 11f * Mathf.Pow(3f, stoneLevel - petLevel);

        if (rate < 0.005f)
        {
            return 0f;
        }

        return rate;
    }

    void OnUpgradeButtonClicked()
    {
        if (currentSelectedPet == null)
        {
            ShowErrorMessage("Chưa chọn pet!");
            return;
        }

        int stoneCount = 0;
        for (int i = 0; i < 3; i++)
        {
            if (selectedStones[i] != null) stoneCount++;
        }

        if (stoneCount == 0)
        {
            ShowErrorMessage("Chưa chọn đá!");
            return;
        }

        if (btnUpdate != null)
        {
            btnUpdate.interactable = false;
            AnimateButtonClick(btnUpdate); // ✅ THÊM animation click
        }

        float successRate = GetUpgradeSuccessRate();
        Debug.Log($"Đang nâng cấp {currentSelectedPet.name} với tỉ lệ {successRate}%");

        float random = UnityEngine.Random.Range(0f, 100f);
        bool success = random <= successRate;

        List<long> stoneList = new List<long>();
        for (int i = 0; i < 3; i++)
        {
            if (selectedStones[i] != null)
            {
                stoneList.Add(selectedStones[i].idStone);
            }
        }

        bool preventDowngrade = toggleProtection != null && toggleProtection.isOn;

        var upgradeRequest = new PetUpgradeRequestDTO
        {
            userPetId = currentSelectedPet.id,
            stoneIds = stoneList.ToArray(),
            success = success,
            preventDowngrade = preventDowngrade
        };

        StartCoroutine(UpgradePetAPI(upgradeRequest));
    }

    void ShowErrorMessage(string message)
    {
        Debug.Log(message);

        if (txtResultUpdate != null)
        {
            txtResultUpdate.gameObject.SetActive(true);
            txtResultUpdate.text = message;
            txtResultUpdate.color = Color.gray;
            Outline outline = txtResultUpdate.gameObject.GetComponent<Outline>();
            if (outline == null)
            {
                outline = txtResultUpdate.gameObject.AddComponent<Outline>();
            }
            outline.effectColor = Color.white;
            StartCoroutine(HideResultAfterDelay(3f));
        }
    }

    IEnumerator UpgradePetAPI(PetUpgradeRequestDTO request)
    {
        string stoneIdsJson = string.Join(",", request.stoneIds);
        string json = $"{{\"userPetId\":{request.userPetId},\"stoneIds\":[{stoneIdsJson}],\"success\":{request.success.ToString().ToLower()}}}";
        Debug.Log($"📤 Manual JSON: {json}");

        var apiCall = APIManager.Instance.PostRequest_Generic<PetUpgradeResponseDTO>(
            APIConfig.UPGRADE_PET,
            request,
            OnUpgradeSuccess,
            OnUpgradeError
        );

        yield return apiCall;
    }

    void OnUpgradeSuccess(PetUpgradeResponseDTO response)
    {
        Debug.Log($"Upgrade result: {response.message}");

        if (anmtUpdatePet != null)
        {
            anmtUpdatePet.SetActive(true);
            StartCoroutine(WaitForAnimationThenReload(response));
        }
        else
        {
            HandleUpgradeResult(response);
        }
    }

    IEnumerator WaitForAnimationThenReload(PetUpgradeResponseDTO response)
    {
        Animator animator = anmtUpdatePet.GetComponent<Animator>();

        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            float animationLength = stateInfo.length;

            Debug.Log($"Animation sẽ chạy trong {animationLength} giây");

            yield return new WaitForSeconds(animationLength);
        }
        else
        {
            Debug.LogWarning("Không tìm thấy Animator trên anmtUpdatePet, chờ 2 giây mặc định");
            yield return new WaitForSeconds(2f);
        }

        if (anmtUpdatePet != null)
        {
            anmtUpdatePet.SetActive(false);
        }

        HandleUpgradeResult(response);
    }

    void HandleUpgradeResult(PetUpgradeResponseDTO response)
    {
        if (txtResultUpdate != null)
        {
            txtResultUpdate.gameObject.SetActive(true);
            txtResultUpdate.text = response.message;

            if (response.success)
            {
                txtResultUpdate.color = originalResultColor;
            }
            else
            {
                txtResultUpdate.color = Color.gray;
                Outline outline = txtResultUpdate.gameObject.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = txtResultUpdate.gameObject.AddComponent<Outline>();
                }
                outline.effectColor = Color.white;
            }

            // ✅ THÊM: Hiệu ứng bay lên
            StartCoroutine(AnimateTextFlyUp(txtResultUpdate, 2f, 100f));
            StartCoroutine(HideResultAfterDelay(3f));
        }

        selectedStones = new StoneDTO[3];
        UpdateSelectedStonesUI();
        StartCoroutine(ReloadDataAfterUpgrade());

        if (btnUpdate != null)
        {
            btnUpdate.interactable = true;
        }
    }

    void OnUpgradeError(string error)
    {
        Debug.LogError($"Lỗi upgrade: {error}");

        if (txtResultUpdate != null)
        {
            txtResultUpdate.gameObject.SetActive(true);
            txtResultUpdate.text = "Lỗi: " + error;
            txtResultUpdate.color = Color.red;
            Outline outline = txtResultUpdate.gameObject.GetComponent<Outline>();
            if (outline == null)
            {
                outline = txtResultUpdate.gameObject.AddComponent<Outline>();
            }
            outline.effectColor = Color.white;
            StartCoroutine(HideResultAfterDelay(3f));
        }

        if (btnUpdate != null)
        {
            btnUpdate.interactable = true;
        }
    }

    IEnumerator HideResultAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (txtResultUpdate != null)
        {
            txtResultUpdate.gameObject.SetActive(false);
            txtResultUpdate.color = originalResultColor;
            Outline outline = txtResultUpdate.gameObject.GetComponent<Outline>();
            if (outline == null)
            {
                outline = txtResultUpdate.gameObject.AddComponent<Outline>();
            }
            outline.effectColor = Color.red;
        }
    }

    IEnumerator ReloadDataAfterUpgrade()
    {
        int userId = PlayerPrefs.GetInt("userId", 1);

        var petsRequest = APIManager.Instance.GetRequest<List<PetUserDTO>>(
            APIConfig.GET_ALL_PET_USERS(userId),
            OnPetsReloaded,
            OnError
        );

        var stonesRequest = APIManager.Instance.GetRequest<StoneResponse>(
            APIConfig.GET_STONES(userId),
            OnStonesReloaded,
            OnError
        );

        var userRequest = APIManager.Instance.GetRequest<UserDTO>(
            APIConfig.GET_USER(userId),
            OnUserReceived,
            OnError
        );

        yield return petsRequest;
        yield return stonesRequest;
        yield return userRequest;
    }

    void OnPetsReloaded(List<PetUserDTO> pets)
    {
        foreach (Transform child in petListContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var pet in pets)
        {
            GameObject petUIObject = Instantiate(petUIPrefab, petListContainer);
            SetupPetUI(petUIObject, pet);
        }

        if (currentSelectedPet != null)
        {
            var updatedPet = pets.FirstOrDefault(p => p.id == currentSelectedPet.id);
            if (updatedPet != null)
            {
                currentSelectedPet = updatedPet;

                OnPetClicked(
                    updatedPet.petId.ToString(),
                    updatedPet.name,
                    updatedPet.attack,
                    updatedPet.hp,
                    updatedPet.mana,
                    updatedPet.level,
                    updatedPet.elementType,
                    updatedPet.elementOther,
                    updatedPet.weaknessValue,
                    updatedPet.des,
                    updatedPet,
                    updatedPet.skillCardId
                );
            }
        }
    }

    void OnStonesReloaded(StoneResponse stones)
    {
        allStones = stones;

        if (currentSelectedPet != null)
        {
            LoadStonesForElement(currentSelectedPet.elementType);
        }
    }

    [Serializable]
    public class PetUpgradeRequestDTO
    {
        public long userPetId;
        public long[] stoneIds;
        public bool success;
        public bool preventDowngrade;
    }

    [Serializable]
    public class PetUpgradeResponseDTO
    {
        public bool success;
        public string message;
        public PetUserDTO updatedPet;
    }

    [Serializable]
    public class StoneUpgradeRequestDTO
    {
        public int userId;
        public long[] stoneIds;
        public bool success;
        public bool upgradeAll; // ✅ THÊM: Field đánh dấu upgrade all
    }

    [Serializable]
    public class StoneUpgradeResponseDTO
    {
        public bool success;
        public string message;
    }

    // ✅ THÊM: DTO cho trừ vàng
    [Serializable]
    public class DeductGoldRequestDTO
    {
        public int userId;
        public int amount;
        public string reason;
    }

    [Serializable]
    public class DeductGoldResponseDTO
    {
        public bool success;
        public string message;
        public int remainingGold;
    }

    float GetUpgradeSuccessRate()
    {
        if (currentSelectedPet == null)
            return 0f;

        int petLevel = currentSelectedPet.level;
        float totalValue = 0f;

        for (int i = 0; i < 3; i++)
        {
            if (selectedStones[i] != null)
            {
                float stoneRate = CalculateStoneRate(petLevel, selectedStones[i].lever);
                totalValue += stoneRate;
            }
        }

        return Mathf.Min(totalValue, 100f);
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
        if (attributeSprite != null)
        {
            imageComponent.sprite = attributeSprite;
        }
    }

    void OnUserReceived(UserDTO user)
    {
        currentUser = user;
        SetTextIfNotNull(txtNl, $"{user.energy}/{user.energyFull}");
        SetTextIfNotNull(txtVang, FormatVND(user.gold));
        SetTextIfNotNull(txtCt, FormatVND(user.requestAttack));

        if (toggleProtection != null && toggleProtection.isOn && user.gold < 5000)
        {
            toggleProtection.SetIsOnWithoutNotify(false);

            if (messageText != null)
            {
                messageText.text = "Bạn đã hết gold ^^!";
                messageText.gameObject.SetActive(true);
                StartCoroutine(HideMessageAfterDelay(3f));
            }
        }

        // ✅ THÊM: Kiểm tra toggle upgrade all đá
        if (toggleUpgradeAll != null && toggleUpgradeAll.isOn && user.gold < 5000)
        {
            toggleUpgradeAll.SetIsOnWithoutNotify(false);

            if (messageTextStone != null)
            {
                messageTextStone.text = "Bạn đã hết gold ^^!";
                messageTextStone.gameObject.SetActive(true);
                StartCoroutine(HideStoneMessageAfterDelay(3f));
            }
        }
    }

    public void BackScene()
    {
        ManagerGame.Instance.BackScene();
    }

    void OnError(string error)
    {
        Debug.LogError($"Lỗi API: {error}");
        ManagerGame.Instance.HideLoading();
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

                // ✅ THÊM: Card flip animation
                imgCard.transform.localScale = new Vector3(0, 1, 1);
                LeanTween.scaleX(imgCard.gameObject, 1f, 0.4f)
                    .setDelay(0.5f)
                    .setEase(LeanTweenType.easeOutBack);
            }

            if (txtDescription != null)
            {
                txtDescription.text = description;

                // ✅ THÊM: Description fade in
                CanvasGroup descGroup = txtDescription.GetComponent<CanvasGroup>();
                if (descGroup == null)
                    descGroup = txtDescription.gameObject.AddComponent<CanvasGroup>();

                descGroup.alpha = 0f;
                LeanTween.alphaCanvas(descGroup, 1f, 0.5f)
                    .setDelay(0.7f)
                    .setEase(LeanTweenType.easeInOutQuad);
            }

            Debug.Log($"✓ Đã load skill card cho pet {skillCardId}");
        }
        else
        {
            PanelCardPet.SetActive(false);
            Debug.Log($"Pet {skillCardId} không có skill card");
        }
    }
    // ✅ THÊM: Hiệu ứng bay lên cho text
    IEnumerator AnimateTextFlyUp(Text textComponent, float duration = 1f, float moveDistance = 50f)
    {
        if (textComponent == null) yield break;

        Vector3 startPos = textComponent.rectTransform.anchoredPosition;
        Vector3 targetPos = startPos + new Vector3(0, moveDistance, 0);

        float elapsed = 0f;
        Color originalColor = textComponent.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Bay lên
            textComponent.rectTransform.anchoredPosition = Vector3.Lerp(startPos, targetPos, t);

            // Mờ dần (fade out)
            Color newColor = originalColor;
            newColor.a = Mathf.Lerp(1f, 0f, t);
            textComponent.color = newColor;

            yield return null;
        }

        // Reset về vị trí ban đầu
        textComponent.rectTransform.anchoredPosition = startPos;
        textComponent.color = originalColor;
        textComponent.gameObject.SetActive(false);
    }
    // Thêm vào cuối class ManagerKhoPet
    void OnDestroy()
    {
        LeanTween.cancel(gameObject);

        if (PanelUpdateStone != null)
            LeanTween.cancel(PanelUpdateStone);

        if (panelUpdate != null)
            LeanTween.cancel(panelUpdate);

        if (PanelCardPet != null)
            LeanTween.cancel(PanelCardPet);

        // ✅ Cancel tất cả animations
        LeanTween.cancelAll();
    }

    void OnDisable()
    {
        LeanTween.cancel(gameObject);
    }

    void OnApplicationQuit()
    {
        LeanTween.cancelAll();
    }
}

[Serializable]
public class StoneDTO
{
    public int idUser;
    public int idStone;
    public int count;
    public string name;
    public int lever;
    public string elementType;
}

[Serializable]
public class StoneResponse
{
    public StoneDTO[] FIRE;
    public StoneDTO[] WATER;
    public StoneDTO[] EARTH;
    public StoneDTO[] WOOD;
    public StoneDTO[] METAL;
}