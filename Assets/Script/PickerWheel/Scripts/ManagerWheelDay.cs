using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using EasyUI.PickerWheelUI;

public class ManagerWheelDay : MonoBehaviour
{
    public static ManagerWheelDay Instance;

    [Header("Panel References")]
    public GameObject panelWheelDay;
    public GameObject panelNoticeResult;
    public Button btnBack;

    [Header("Picker Wheel Reference")]
    public PickerWheel pickerWheel;

    [Header("Buttons")]
    public Button btnRun;
    public Button btnRunMany;
    public Text txtBtnRun;
    public Text txtBtnRunMany;
    public Text txtCountWheel; // Hiển thị số lượt quay miễn phí

    [Header("Reward Display - Trong boardReward")]
    public Transform listPanel;

    [Header("Notice Result")]
    public Transform listReward;
    public Button btnGet;
    public Text txtMessage;

    [Header("Reward Prefabs")]
    public GameObject rewardPetPrefab;
    public GameObject rewardAvatarPrefab;
    public GameObject rewardGoldPrefab;
    public GameObject rewardRubyPrefab;
    public GameObject rewardEnergyPrefab;
    public GameObject rewardStonePrefab;

    [Header("Stone Sprites - 5 Hệ, mỗi hệ 7 Level")]
    public Sprite[] stoneFire = new Sprite[7];
    public Sprite[] stoneWater = new Sprite[7];
    public Sprite[] stoneWood = new Sprite[7];
    public Sprite[] stoneEarth = new Sprite[7];
    public Sprite[] stoneMetal = new Sprite[7];

    [Header("Default Icons")]
    public Sprite iconGold;
    public Sprite iconRuby;
    public Sprite iconEnergy;

    [Header("Settings")]
    public int spinCost = 10000;
    public int duplicateCompensation = 100000; // Gold bù khi trùng Pet/Avatar

    // Private variables
    private WheelConfigDTO wheelConfig;
    private List<SpinRewardDTO> currentRewards = new List<SpinRewardDTO>();
    private bool isSpinning = false;
    private bool shouldStopMultiSpin = false;
    private int currentSpinCount = 0;
    private int targetSpinCount = 0;
    private int currentUserGold = 0;
    private int currentFreeSpins = 0; // Số lượt quay miễn phí
    [Header("Notice Confirm")]
    public GameObject panelNoticeConfirm;
    public Text txtConfirmMessage;
    public Button btnConfirmYes;
    public Button btnConfirmNo;

    private int pendingSpinCount = 0;

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
        InitializeButtons();

        if (panelWheelDay != null)
            panelWheelDay.SetActive(false);

        if (panelNoticeResult != null)
            panelNoticeResult.SetActive(false);

        if (panelNoticeConfirm != null)
            panelNoticeConfirm.SetActive(false);
    }

    void InitializeButtons()
    {
        if (btnBack != null)
            btnBack.onClick.AddListener(ClosePanel);

        if (btnRun != null)
            btnRun.onClick.AddListener(() => StartSpin(1));

        if (btnRunMany != null)
            btnRunMany.onClick.AddListener(OnBtnRunManyClicked);

        if (btnGet != null)
            btnGet.onClick.AddListener(CloseNoticeResult);
        if (btnConfirmYes != null)
            btnConfirmYes.onClick.AddListener(OnConfirmYes);

        if (btnConfirmNo != null)
            btnConfirmNo.onClick.AddListener(OnConfirmNo);
    }

    void OnBtnRunManyClicked()
    {
        if (isSpinning)
        {
            shouldStopMultiSpin = true;
            txtBtnRunMany.text = "Đang dừng...";
            Debug.Log("[WheelDay] Stop requested");
        }
        else
        {
            StartSpin(10);
        }
    }

    // ========================================
    // PUBLIC: MỞ PANEL TỪ QUẢNG TRƯỜNG
    // ========================================

    public void OpenWheelPanel()
    {
        Debug.Log("[WheelDay] OpenWheelPanel called");

        if (panelWheelDay == null)
        {
            Debug.LogError("[WheelDay] panelWheelDay is null!");
            return;
        }

        ClearAllUI();

        int userId = PlayerPrefs.GetInt("userId", 0);
        StartCoroutine(LoadWheelDataCoroutine(userId));
    }

    void ClearAllUI()
    {
        ClearListPanel();
        ClearListReward();
        currentRewards.Clear();

        isSpinning = false;
        shouldStopMultiSpin = false;
        currentSpinCount = 0;
        targetSpinCount = 0;
        pendingSpinCount = 0; // THÊM MỚI

        if (panelNoticeResult != null && panelNoticeResult.activeSelf)
        {
            panelNoticeResult.SetActive(false);
        }

        // THÊM MỚI
        if (panelNoticeConfirm != null && panelNoticeConfirm.activeSelf)
        {
            panelNoticeConfirm.SetActive(false);
        }

        Debug.Log("[WheelDay] All UI cleared");
    }

    IEnumerator LoadWheelDataCoroutine(int userId)
    {
        Debug.Log($"[WheelDay] Loading wheel data for userId: {userId}");

        string url = APIConfig.GET_WHEEL_CONFIG(userId);

        WheelConfigDTO configData = null;

        yield return APIManager.Instance.GetRequest<WheelConfigDTO>(
            url,
            (config) =>
            {
                configData = config;
                Debug.Log("[WheelDay] ✓ Wheel config loaded");
            },
            (error) =>
            {
                Debug.LogError($"[WheelDay] ✗ Load error: {error}");
            }
        );

        if (configData != null)
        {
            wheelConfig = configData;
            currentUserGold = configData.userGold;
            currentFreeSpins = configData.userWheel; // Số lượt miễn phí từ server
            SetupWheelPieces();
            ShowPanelWithAnimation();
        }
        else
        {
            Debug.LogError("[WheelDay] Failed to load wheel config");
        }
    }

    void ShowPanelWithAnimation()
    {
        panelWheelDay.SetActive(true);
        panelWheelDay.transform.localScale = Vector3.zero;

        LeanTween.scale(panelWheelDay, Vector3.one, 0.4f)
            .setEase(LeanTweenType.easeOutBack)
            .setOnComplete(() =>
            {
                UpdateButtonsState();

                if (pickerWheel != null)
                {
                    pickerWheel.ResetWheelRotation(0.5f);
                }
            });
    }

    // ========================================
    // SETUP WHEEL PIECES
    // ========================================

    void SetupWheelPieces()
    {
        if (wheelConfig == null || wheelConfig.prizes == null)
        {
            Debug.LogError("[WheelDay] No config to setup wheel!");
            return;
        }

        if (pickerWheel == null)
        {
            Debug.LogError("[WheelDay] PickerWheel is null!");
            return;
        }

        if (pickerWheel.wheelPieces == null || pickerWheel.wheelPieces.Length != wheelConfig.prizes.Count)
        {
            pickerWheel.wheelPieces = new WheelPiece[wheelConfig.prizes.Count];
            for (int i = 0; i < pickerWheel.wheelPieces.Length; i++)
            {
                pickerWheel.wheelPieces[i] = new WheelPiece();
            }
        }

        for (int i = 0; i < wheelConfig.prizes.Count; i++)
        {
            WheelPrizeDTO prize = wheelConfig.prizes[i];
            WheelPiece piece = pickerWheel.wheelPieces[i];

            piece.Icon = GetPrizeSprite(prize);
            piece.Label = prize.prizeName;
            piece.Amount = prize.amount;
            piece.Chance = (float)prize.chance;
            piece.Index = i;
        }

        pickerWheel.SetupWheel();

        Debug.Log($"[WheelDay] Setup {wheelConfig.prizes.Count} wheel pieces");
    }

    Sprite GetPrizeSprite(WheelPrizeDTO prize)
    {
        switch (prize.prizeType)
        {
            case "PET":
                Sprite petSprite = Resources.Load<Sprite>("Image/IconsPet/" + prize.prizeId);
                if (petSprite != null) return petSprite;
                Debug.LogWarning($"[WheelDay] Pet sprite not found: {prize.prizeId}");
                return null;

            case "AVATAR":
                Sprite avatarSprite = Resources.Load<Sprite>("Image/Avt/" + prize.prizeId);
                if (avatarSprite != null) return avatarSprite;
                Debug.LogWarning($"[WheelDay] Avatar sprite not found: {prize.prizeId}");
                return null;

            case "GOLD":
                return iconGold;

            case "RUBY":
                return iconRuby;

            case "ENERGY":
                return iconEnergy;

            case "STONE":
                return GetStoneSprite(prize.elementType, prize.stoneLevel);

            default:
                return iconGold;
        }
    }

    Sprite GetStoneSprite(string elementType, int level)
    {
        if (level < 1 || level > 7)
        {
            Debug.LogWarning($"[WheelDay] Invalid stone level: {level}");
            return null;
        }

        int index = level - 1;

        switch (elementType)
        {
            case "FIRE":
                return stoneFire != null && stoneFire.Length > index ? stoneFire[index] : null;
            case "WATER":
                return stoneWater != null && stoneWater.Length > index ? stoneWater[index] : null;
            case "WOOD":
                return stoneWood != null && stoneWood.Length > index ? stoneWood[index] : null;
            case "EARTH":
                return stoneEarth != null && stoneEarth.Length > index ? stoneEarth[index] : null;
            case "METAL":
                return stoneMetal != null && stoneMetal.Length > index ? stoneMetal[index] : null;
            default:
                Debug.LogWarning($"[WheelDay] Unknown element type: {elementType}");
                return null;
        }
    }

    void UpdateButtonsState()
    {
        // Cập nhật text hiển thị số lượt quay miễn phí
        if (txtCountWheel != null)
        {
            txtCountWheel.text = currentFreeSpins.ToString();
        }

        // Cập nhật button text
        if (txtBtnRun != null)
        {
            if (currentFreeSpins > 0)
            {
                txtBtnRun.text = $"Quay 1\n(Miễn phí)";
            }
            else
            {
                txtBtnRun.text = $"Quay 1\n({FormatVND(spinCost):N0} Gold)";
            }
        }

        if (txtBtnRunMany != null)
        {
            txtBtnRunMany.text = $"Quay 10\n({FormatVND(spinCost * 10):N0} Gold)";
        }
    }

    // ========================================
    // SPIN LOGIC
    // ========================================

    void StartSpin(int count)
    {
        if (isSpinning)
        {
            Debug.LogWarning("[WheelDay] Already spinning!");
            return;
        }

        // Nếu quay 1 lần và có lượt miễn phí
        if (count == 1 && currentFreeSpins > 0)
        {
            Debug.Log("[WheelDay] Using free spin");
            targetSpinCount = 1;
            currentSpinCount = 0;
            shouldStopMultiSpin = false;
            currentRewards.Clear();
            ClearListPanel();
            SetButtonsInteractable(false);
            txtBtnRun.text = "Đang...";

            // Check trước khi quay
            int userId = PlayerPrefs.GetInt("userId", 0);
            StartCoroutine(CheckAndSpinFree(userId));
            return;
        }

        // Nếu không có lượt miễn phí
        int totalCost = spinCost * count;
        if (currentUserGold < totalCost)
        {
            ShowNeedGoldMessage(count);
            return;
        }

        // Hiển thị confirm
        ShowConfirmSpin(count);
    }

    IEnumerator CheckAndSpinFree(int userId)
    {
        isSpinning = true;

        // 1. GỌI API CHECK điều kiện
        SpinCheckDTO checkResult = null;
        yield return CallCheckFreeSpinAPI(userId, (result) =>
        {
            checkResult = result;
        });

        if (checkResult == null || !checkResult.canSpin)
        {
            Debug.LogError($"[WheelDay] Cannot spin: {checkResult?.message}");
            ShowErrorMessage(checkResult?.message ?? "Không thể quay!");
            isSpinning = false;
            SetButtonsInteractable(true);
            UpdateButtonsState();
            yield break;
        }

        // 2. QUAY WHEEL (animation)
        SpinRewardDTO reward = new SpinRewardDTO();
        currentRewards.Add(reward);

        yield return SpinWheelCoroutine(reward);

        // 3. GỌI API LƯU kết quả
        SpinResultDTO spinResult = null;
        yield return CallSaveFreeSpinAPI(userId, reward.prizeIndex, (result) =>
        {
            spinResult = result;
        });

        if (spinResult == null || !spinResult.success)
        {
            Debug.LogError($"[WheelDay] Save failed: {spinResult?.message}");
            ShowErrorMessage("Lỗi lưu kết quả! Vui lòng liên hệ admin.");
            currentRewards.Remove(reward);
            ClearListPanel();
            isSpinning = false;
            SetButtonsInteractable(true);
            UpdateButtonsState();
            yield break;
        }

        // 4. Update UI với kết quả từ server
        currentFreeSpins = spinResult.remainingWheel;
        currentUserGold = spinResult.remainingGold;

        if (spinResult.isDuplicate)
        {
            reward.isDuplicate = true;
            reward.compensationGold = spinResult.compensationGold;
        }

        AddRewardToListPanel(reward);

        isSpinning = false;
        SetButtonsInteractable(true);
        UpdateButtonsState();

        yield return new WaitForSeconds(0.3f);
        ShowNoticeResult();
    }
    void ShowErrorMessage(string message)
    {
        if (txtMessage != null)
        {
            txtMessage.text = message;
        }

        if (panelNoticeResult != null)
        {
            ClearListReward();
            panelNoticeResult.SetActive(true);
            panelNoticeResult.transform.localScale = Vector3.zero;
            LeanTween.scale(panelNoticeResult, Vector3.one, 0.4f).setEase(LeanTweenType.easeOutBack);
        }
    }
    void ShowConfirmSpin(int spinCount)
    {
        if (panelNoticeConfirm == null)
        {
            Debug.LogError("[WheelDay] panelNoticeConfirm is null!");
            return;
        }

        pendingSpinCount = spinCount;
        int totalCost = spinCost * spinCount;

        if (txtConfirmMessage != null)
        {
            if (spinCount == 1)
            {
                txtConfirmMessage.text = $"Bạn không còn lượt quay miễn phí!\n\nXác nhận sử dụng {totalCost:N0} Gold để quay?";
            }
            else
            {
                txtConfirmMessage.text = $"Xác nhận sử dụng {totalCost:N0} Gold để quay {spinCount} lần?";
            }
        }

        panelNoticeConfirm.SetActive(true);
        panelNoticeConfirm.transform.localScale = Vector3.zero;
        LeanTween.scale(panelNoticeConfirm, Vector3.one, 0.4f).setEase(LeanTweenType.easeOutBack);
    }

    void OnConfirmYes()
    {
        CloseConfirmPanel();

        targetSpinCount = pendingSpinCount;
        currentSpinCount = 0;
        shouldStopMultiSpin = false;
        currentRewards.Clear();

        ClearListPanel();
        SetButtonsInteractable(false);

        if (pendingSpinCount == 1)
        {
            txtBtnRun.text = "Đang...";
        }
        else
        {
            txtBtnRunMany.text = "Dừng";
            btnRunMany.interactable = true;
        }

        int userId = PlayerPrefs.GetInt("userId", 0);
        StartCoroutine(CheckAndSpinGold(userId));
    }

    IEnumerator CheckAndSpinGold(int userId)
    {
        isSpinning = true;

        while (currentSpinCount < targetSpinCount && !shouldStopMultiSpin)
        {
            // 1. CHECK điều kiện trước mỗi lượt
            SpinCheckDTO checkResult = null;
            yield return CallCheckGoldSpinAPI(userId, (result) =>
            {
                checkResult = result;
            });

            if (checkResult == null || !checkResult.canSpin)
            {
                Debug.Log($"[WheelDay] Cannot continue: {checkResult?.message}");
                ShowErrorMessage(checkResult?.message ?? "Không đủ Gold!");
                break;
            }

            // 2. QUAY WHEEL
            SpinRewardDTO reward = new SpinRewardDTO();
            currentRewards.Add(reward);

            yield return SpinWheelCoroutine(reward);

            // 3. GỌI API LƯU kết quả
            SpinResultDTO spinResult = null;
            yield return CallSaveGoldSpinAPI(userId, reward.prizeIndex, (result) =>
            {
                spinResult = result;
            });

            if (spinResult == null || !spinResult.success)
            {
                Debug.LogError($"[WheelDay] Save failed: {spinResult?.message}");
                ShowErrorMessage("Lỗi lưu kết quả! Vui lòng liên hệ admin.");
                currentRewards.Remove(reward);
                break;
            }

            // 4. Update từ server
            currentUserGold = spinResult.remainingGold;

            if (spinResult.isDuplicate)
            {
                reward.isDuplicate = true;
                reward.compensationGold = spinResult.compensationGold;
            }

            AddRewardToListPanel(reward);
            currentSpinCount++;

            if (currentSpinCount < targetSpinCount && !shouldStopMultiSpin)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        isSpinning = false;
        shouldStopMultiSpin = false;

        SetButtonsInteractable(true);
        UpdateButtonsState();

        if (currentRewards.Count > 0)
        {
            yield return new WaitForSeconds(0.3f);
            ShowNoticeResult();
        }
    }

    // ========================================
    // API CALLS - MỚI
    // ========================================

    // 1. CHECK điều kiện quay miễn phí
    IEnumerator CallCheckFreeSpinAPI(int userId, Action<SpinCheckDTO> callback)
    {
        string url = APIConfig.CHECK_FREE_SPIN(userId);

        SpinCheckDTO result = null;

        yield return APIManager.Instance.GetRequest<SpinCheckDTO>(
            url,
            (response) =>
            {
                result = response;
                Debug.Log($"[WheelDay] Check free spin: {response.canSpin}");
            },
            (error) =>
            {
                Debug.LogError($"[WheelDay] Check error: {error}");
            }
        );

        callback?.Invoke(result);
    }

    // 2. LƯU kết quả quay miễn phí
    IEnumerator CallSaveFreeSpinAPI(int userId, int prizeIndex, Action<SpinResultDTO> callback)
    {
        string url = APIConfig.SAVE_FREE_SPIN(userId, prizeIndex);

        SpinResultDTO result = null;

        yield return APIManager.Instance.PostRequest<SpinResultDTO>(
            url,
            null,
            (response) =>
            {
                result = response;
                Debug.Log($"[WheelDay] Free spin saved: {response.message}");
            },
            (error) =>
            {
                Debug.LogError($"[WheelDay] Save error: {error}");
            }
        );

        callback?.Invoke(result);
    }

    // 3. CHECK điều kiện quay Gold
    IEnumerator CallCheckGoldSpinAPI(int userId, Action<SpinCheckDTO> callback)
    {
        string url = APIConfig.CHECK_GOLD_SPIN(userId);

        SpinCheckDTO result = null;

        yield return APIManager.Instance.GetRequest<SpinCheckDTO>(
            url,
            (response) =>
            {
                result = response;
                Debug.Log($"[WheelDay] Check gold spin: {response.canSpin}");
            },
            (error) =>
            {
                Debug.LogError($"[WheelDay] Check error: {error}");
            }
        );

        callback?.Invoke(result);
    }

    // 4. LƯU kết quả quay Gold
    IEnumerator CallSaveGoldSpinAPI(int userId, int prizeIndex, Action<SpinResultDTO> callback)
    {
        string url = APIConfig.SAVE_GOLD_SPIN(userId, prizeIndex);

        SpinResultDTO result = null;

        yield return APIManager.Instance.PostRequest<SpinResultDTO>(
            url,
            null,
            (response) =>
            {
                result = response;
                Debug.Log($"[WheelDay] Gold spin saved: {response.message}");
            },
            (error) =>
            {
                Debug.LogError($"[WheelDay] Save error: {error}");
            }
        );

        callback?.Invoke(result);
    }
    // HÀM MỚI: Xử lý khi nhấn No
    void OnConfirmNo()
    {
        CloseConfirmPanel();
        pendingSpinCount = 0;
    }

    // HÀM MỚI: Đóng panel confirm
    void CloseConfirmPanel()
    {
        if (panelNoticeConfirm == null) return;

        LeanTween.scale(panelNoticeConfirm, Vector3.zero, 0.3f)
            .setEase(LeanTweenType.easeInBack)
            .setOnComplete(() =>
            {
                panelNoticeConfirm.SetActive(false);
            });
    }

    // Phương thức mới: Hiển thị thông báo cần gold
    void ShowNeedGoldMessage(int spinCount)
    {
        Debug.Log("[WheelDay] Showing need gold message");

        if (txtMessage != null)
        {
            int requiredGold = spinCost * spinCount;
            int missingGold = requiredGold - currentUserGold;

            if (spinCount == 1)
            {
                txtMessage.text = $"Bạn đã hết lượt quay miễn phí!\n\nCần {spinCost:N0} Gold để quay\n(Còn thiếu {missingGold:N0} Gold)";
            }
            else
            {
                txtMessage.text = $"Không đủ Gold!\n\nCần {requiredGold:N0} Gold để quay {spinCount} lần\n(Còn thiếu {missingGold:N0} Gold)";
            }
        }

        if (panelNoticeResult != null)
        {
            ClearListReward();
            panelNoticeResult.SetActive(true);
            panelNoticeResult.transform.localScale = Vector3.zero;
            LeanTween.scale(panelNoticeResult, Vector3.one, 0.4f).setEase(LeanTweenType.easeOutBack);
        }
    }

    // Quay bằng lượt miễn phí
    IEnumerator SpinWithFreeTicket(int userId)
    {
        isSpinning = true;

        // Gọi API trừ lượt miễn phí
        SpinResultDTO spinResult = null;
        yield return CallSpinFreeAPI(userId, (result) =>
        {
            spinResult = result;
        });

        if (spinResult == null || !spinResult.success)
        {
            Debug.LogError($"[WheelDay] Free spin API failed: {spinResult?.message}");
            isSpinning = false;
            SetButtonsInteractable(true);
            UpdateButtonsState();
            yield break;
        }

        // Update số lượt miễn phí
        currentFreeSpins = spinResult.remainingWheel;

        // Tạo reward object
        SpinRewardDTO reward = new SpinRewardDTO();
        currentRewards.Add(reward);

        // Quay wheel
        yield return SpinWheelCoroutine(reward);

        // Check nếu trùng Pet/Avatar
        if (spinResult.isDuplicate)
        {
            reward.isDuplicate = true;
            reward.compensationGold = duplicateCompensation;
            currentUserGold = duplicateCompensation;
        }

        // Add reward vào list panel
        AddRewardToListPanel(reward);

        isSpinning = false;
        SetButtonsInteractable(true);
        UpdateButtonsState();

        yield return new WaitForSeconds(0.3f);
        ShowNoticeResult();
    }

    // Quay bằng Gold
    IEnumerator SpinCoroutine(int userId)
    {
        isSpinning = true;

        while (currentSpinCount < targetSpinCount && !shouldStopMultiSpin)
        {
            if (currentUserGold < spinCost)
            {
                Debug.Log("[WheelDay] Not enough gold to continue");
                ShowNotEnoughResourceMessage();
                break;
            }

            SpinResultDTO spinResult = null;

            yield return CallSpinAPI(userId, 1, (result) =>
            {
                spinResult = result;
            });

            if (spinResult == null || !spinResult.success)
            {
                Debug.LogError($"[WheelDay] Spin API failed: {spinResult?.message}");
                if (spinResult != null && spinResult.message.Contains("Gold"))
                {
                    ShowNotEnoughResourceMessage();
                }
                break;
            }

            currentUserGold = spinResult.remainingGold;

            SpinRewardDTO reward = new SpinRewardDTO();
            currentRewards.Add(reward);

            yield return SpinWheelCoroutine(reward);

            // Check trùng Pet/Avatar
            if (spinResult.isDuplicate)
            {
                reward.isDuplicate = true;
                reward.compensationGold = duplicateCompensation;
                currentUserGold += duplicateCompensation;
            }

            AddRewardToListPanel(reward);

            currentSpinCount++;

            if (currentSpinCount < targetSpinCount && !shouldStopMultiSpin)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        isSpinning = false;
        shouldStopMultiSpin = false;

        SetButtonsInteractable(true);
        UpdateButtonsState();

        if (currentRewards.Count > 0)
        {
            yield return new WaitForSeconds(0.3f);
            ShowNoticeResult();
        }
    }

    IEnumerator SpinWheelCoroutine(SpinRewardDTO reward)
    {
        bool spinCompleted = false;

        pickerWheel.OnSpinStart(() =>
        {
            Debug.Log("[WheelDay] Wheel spin started");
        });

        pickerWheel.OnSpinEnd((piece) =>
        {
            Debug.Log($"[WheelDay] Wheel spin ended: {piece.Label} (Index: {piece.Index})");

            WheelPrizeDTO prize = wheelConfig.prizes[piece.Index];
            reward.prizeId = prize.prizeId;
            reward.prizeType = prize.prizeType;
            reward.prizeName = prize.prizeName;
            reward.amount = prize.amount;
            reward.elementType = prize.elementType;
            reward.stoneLevel = prize.stoneLevel;
            reward.prizeIndex = piece.Index;

            spinCompleted = true;
        });

        pickerWheel.Spin();

        while (!spinCompleted)
        {
            yield return null;
        }
    }

    // API quay miễn phí (trừ lượt wheel)
    IEnumerator CallSpinFreeAPI(int userId, Action<SpinResultDTO> callback)
    {
        string url = APIConfig.SPIN_WHEEL_FREE(userId);

        SpinResultDTO result = null;

        yield return APIManager.Instance.PostRequest<SpinResultDTO>(
            url,
            null,
            (response) =>
            {
                result = response;
                Debug.Log($"[WheelDay] Free spin success: {response.message}");
            },
            (error) =>
            {
                Debug.LogError($"[WheelDay] Free spin error: {error}");
            }
        );

        callback?.Invoke(result);
    }

    // API quay bằng Gold
    IEnumerator CallSpinAPI(int userId, int spinCount, Action<SpinResultDTO> callback)
    {
        string url = APIConfig.SPIN_WHEEL(userId, spinCount);

        SpinResultDTO result = null;

        yield return APIManager.Instance.PostRequest<SpinResultDTO>(
            url,
            null,
            (response) =>
            {
                result = response;
                Debug.Log($"[WheelDay] Spin success: {response.message}");
            },
            (error) =>
            {
                Debug.LogError($"[WheelDay] Spin error: {error}");
            }
        );

        callback?.Invoke(result);
    }

    // ========================================
    // REWARD DISPLAY
    // ========================================

    void ClearListPanel()
    {
        if (listPanel == null) return;

        int childCount = listPanel.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            Transform child = listPanel.GetChild(i);
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }

        Debug.Log($"[WheelDay] Cleared {childCount} items from listPanel");
    }

    void AddRewardToListPanel(SpinRewardDTO reward)
    {
        if (listPanel == null) return;

        GameObject prefab = GetRewardPrefab(reward.prizeType);
        if (prefab == null)
        {
            Debug.LogWarning($"[WheelDay] No prefab for type: {reward.prizeType}");
            return;
        }

        GameObject rewardObj = Instantiate(prefab, listPanel);
        rewardObj.SetActive(true);

        Image imgIcon = rewardObj.transform.Find("imgIcon")?.GetComponent<Image>();
        if (imgIcon == null) imgIcon = rewardObj.transform.Find("imgtPet")?.GetComponent<Image>();
        if (imgIcon == null) imgIcon = rewardObj.transform.Find("imgtCard")?.GetComponent<Image>();
        if (imgIcon == null) imgIcon = rewardObj.transform.Find("imgStone")?.GetComponent<Image>();
        if (imgIcon == null) imgIcon = rewardObj.transform.Find("imgtAvt")?.GetComponent<Image>();

        if (imgIcon != null)
        {
            imgIcon.sprite = GetRewardSprite(reward);
        }

        Text txtAmount = rewardObj.transform.Find("txtCount")?.GetComponent<Text>();
        if (txtAmount == null) txtAmount = rewardObj.transform.Find("txtAmount")?.GetComponent<Text>();

        if (txtAmount != null)
        {
            if (reward.isDuplicate)
            {
                txtAmount.text = $"+{FormatVND(reward.compensationGold):N0} Gold";
            }
            else if (reward.amount > 1)
            {
                txtAmount.text = "x" + FormatVND(reward.amount);
            }
            else
            {
                txtAmount.text = reward.prizeName;
            }
        }

        rewardObj.transform.localScale = Vector3.zero;
        LeanTween.scale(rewardObj, Vector3.one, 0.3f).setEase(LeanTweenType.easeOutBack);
    }
    public static string FormatVND(long amount)
    {
        return amount.ToString("#,##0").Replace(",", ".");
    }
    GameObject GetRewardPrefab(string prizeType)
    {
        switch (prizeType)
        {
            case "PET": return rewardPetPrefab;
            case "AVATAR": return rewardAvatarPrefab;
            case "GOLD": return rewardGoldPrefab;
            case "RUBY": return rewardRubyPrefab;
            case "ENERGY": return rewardEnergyPrefab;
            case "STONE": return rewardStonePrefab;
            default: return rewardGoldPrefab;
        }
    }

    Sprite GetRewardSprite(SpinRewardDTO reward)
    {
        // Nếu trùng Pet/Avatar thì hiển thị icon Gold
        if (reward.isDuplicate)
        {
            return iconGold;
        }

        switch (reward.prizeType)
        {
            case "PET":
                return Resources.Load<Sprite>("Image/IconsPet/" + reward.prizeId);

            case "AVATAR":
                return Resources.Load<Sprite>("Image/Avt/" + reward.prizeId);

            case "GOLD":
                return iconGold;

            case "RUBY":
                return iconRuby;

            case "ENERGY":
                return iconEnergy;

            case "STONE":
                return GetStoneSprite(reward.elementType, reward.stoneLevel);

            default:
                return iconGold;
        }
    }

    // ========================================
    // NOTICE RESULT
    // ========================================

    void ShowNoticeResult()
    {
        if (panelNoticeResult == null) return;

        ClearListReward();

        foreach (var reward in currentRewards)
        {
            AddRewardToNoticeResult(reward);
        }

        if (txtMessage != null)
        {
            txtMessage.text = $"Bạn đã nhận được {currentRewards.Count} phần thưởng!";
        }

        panelNoticeResult.SetActive(true);
        panelNoticeResult.transform.localScale = Vector3.zero;
        LeanTween.scale(panelNoticeResult, Vector3.one, 0.4f).setEase(LeanTweenType.easeOutBack);
    }

    void ClearListReward()
    {
        if (listReward == null) return;

        int childCount = listReward.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            Transform child = listReward.GetChild(i);
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }

        Debug.Log($"[WheelDay] Cleared {childCount} items from listReward");
    }

    void AddRewardToNoticeResult(SpinRewardDTO reward)
    {
        if (listReward == null) return;

        GameObject prefab = GetRewardPrefab(reward.prizeType);
        if (prefab == null) return;

        GameObject rewardObj = Instantiate(prefab, listReward);
        rewardObj.SetActive(true);

        Image imgIcon = rewardObj.transform.Find("imgIcon")?.GetComponent<Image>();
        if (imgIcon == null) imgIcon = rewardObj.transform.Find("imgtPet")?.GetComponent<Image>();
        if (imgIcon == null) imgIcon = rewardObj.transform.Find("imgtCard")?.GetComponent<Image>();
        if (imgIcon == null) imgIcon = rewardObj.transform.Find("imgStone")?.GetComponent<Image>();
        if (imgIcon == null) imgIcon = rewardObj.transform.Find("imgtAvt")?.GetComponent<Image>();

        if (imgIcon != null)
        {
            imgIcon.sprite = GetRewardSprite(reward);
        }

        Text txtAmount = rewardObj.transform.Find("txtCount")?.GetComponent<Text>();
        if (txtAmount == null) txtAmount = rewardObj.transform.Find("txtAmount")?.GetComponent<Text>();

        if (txtAmount != null)
        {
            if (reward.isDuplicate)
            {
                txtAmount.text = $"+{reward.compensationGold:N0}";
            }
            else
            {
                txtAmount.text = "x" + FormatVND(reward.amount);
            }
        }

        Text txtName = rewardObj.transform.Find("txtName")?.GetComponent<Text>();
        if (txtName == null) txtName = rewardObj.transform.Find("txtLv")?.GetComponent<Text>();

        if (txtName != null)
        {
            if (reward.isDuplicate)
            {
                txtName.text = $"{reward.prizeName} (Trùng - Bù Gold)";
            }
            else
            {
                txtName.text = reward.prizeName;
            }
        }
    }

    void CloseNoticeResult()
    {
        if (panelNoticeResult == null) return;

        LeanTween.scale(panelNoticeResult, Vector3.zero, 0.3f)
            .setEase(LeanTweenType.easeInBack)
            .setOnComplete(() =>
            {
                panelNoticeResult.SetActive(false);
                ClearListReward();

                if (ManagerQuangTruong.Instance != null)
                {
                    ManagerQuangTruong.Instance.RefreshUserInfo(true);
                }
            });
    }

    // ========================================
    // UTILITIES
    // ========================================

    void SetButtonsInteractable(bool interactable)
    {
        if (btnRun != null) btnRun.interactable = interactable;
        if (btnRunMany != null && !isSpinning) btnRunMany.interactable = interactable;
    }

    void ShowNotEnoughResourceMessage()
    {
        Debug.Log("[WheelDay] Not enough resources!");

        if (txtMessage != null)
        {
            if (currentFreeSpins > 0)
            {
                txtMessage.text = $"Bạn còn {currentFreeSpins} lượt quay miễn phí!\nHoặc cần {spinCost:N0} Gold để quay";
            }
            else
            {
                txtMessage.text = $"Không đủ Gold để quay!\nCần {spinCost:N0} Gold";
            }
        }

        if (panelNoticeResult != null)
        {
            ClearListReward();
            panelNoticeResult.SetActive(true);
            panelNoticeResult.transform.localScale = Vector3.zero;
            LeanTween.scale(panelNoticeResult, Vector3.one, 0.4f).setEase(LeanTweenType.easeOutBack);
        }
    }

    public void ClosePanel()
    {
        if (isSpinning)
        {
            Debug.LogWarning("[WheelDay] Cannot close while spinning!");
            return;
        }

        if (panelWheelDay == null) return;

        LeanTween.scale(panelWheelDay, Vector3.zero, 0.3f)
            .setEase(LeanTweenType.easeInBack)
            .setOnComplete(() =>
            {
                panelWheelDay.SetActive(false);

                if (ManagerQuangTruong.Instance != null)
                {
                    ManagerQuangTruong.Instance.RefreshUserInfo(true);
                }
            });
    }
}

[System.Serializable]
public class SpinCheckDTO
{
    public bool canSpin;
    public string message;
    public int userGold;
    public int userWheel;
}