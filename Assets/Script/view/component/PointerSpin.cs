using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PointerSpin : MonoBehaviour
{
    [Header("Pointer Settings")]
    public Transform pointerTransform;
    public int numberOfSections = 8;
    public float spinDuration = 3f;
    public AnimationCurve spinCurve;

    [Header("Prize Settings")]
    public string[] prizes;
    public float[] prizeAngles;

    [Header("UI Elements")]
    public Button spinButton;
    public Text resultText;
    public Text titleText;
    public Text countdownText;
    public Text yourWheel;

    [Header("Result Panel")]
    public GameObject panelResult;
    public Button btnOke;
    public Animator animatorRW;
    public string animationName = "anmtRW";

    [Header("Visual Effects")]
    public ParticleSystem spinParticles;
    public ParticleSystem winParticles;

    private bool isSpinning = false;
    private float targetAngle;
    private float startAngle;
    private float spinStartTime;
    private string currentPrize = "";
    private int currentWheelCount = 0;
    private int userId;
    private int targetSection = 0; // Lưu section được chọn

    void Start()
    {
        spinButton.onClick.AddListener(OnSpinButtonClicked);

        if (btnOke != null)
            btnOke.onClick.AddListener(ClosePanelResult);

        if (panelResult != null)
            panelResult.SetActive(false);

        if (animatorRW != null)
            animatorRW.gameObject.SetActive(false);

        userId = PlayerPrefs.GetInt("userId", 0);
        InitializeAllSettings();
        UpdateUI();

        // Lấy số lượt quay hiện tại từ ManagerQuangTruong
        StartCoroutine(CheckWheelCount());
    }

    private IEnumerator CheckWheelCount()
    {
        yield return APIManager.Instance.GetRequest<WheelResponse>(
            APIConfig.CHECK_WHEEL(userId),
            OnWheelCheckReceived,
            OnError
        );
    }

    private void OnWheelCheckReceived(WheelResponse response)
    {
        currentWheelCount = response.wheel;
        UpdateWheelUI();
        UpdateYourWheelUI();
    }

    private void UpdateWheelUI()
    {
        // Cập nhật UI hiển thị số lượt quay
        if (titleText != null)
            titleText.text = $"VÒNG QUAY MAY MẮN (Còn {currentWheelCount} lượt)";
    }
    
    private void UpdateYourWheelUI()
    {
        if (yourWheel != null)
        {
            yourWheel.text = $"Bạn có {currentWheelCount} vòng quay";
        }
    }

    private void OnSpinButtonClicked()
    {
        // Kiểm tra số lượt trước khi quay
        if (currentWheelCount <= 0)
        {
            ShowNoWheelPanel();
            return;
        }

        StartSpin();
    }

    private void ShowNoWheelPanel()
    {
        if (panelResult != null)
        {
            panelResult.SetActive(true);
        }
        
        // Hiển thị thông báo hết lượt trong resultText
        if (resultText != null)
        {
            // Xóa outline nếu có
            Outline outline = resultText.GetComponent<Outline>();
            if (outline != null)
                Destroy(outline);
                
            resultText.text = "Bạn đã hết lượt quay!\nVui lòng quay lại sau.";
        }
    }

    private void InitializeAllSettings()
    {
        if (prizes == null || prizes.Length == 0)
        {
            prizes = new string[numberOfSections];
            for (int i = 0; i < numberOfSections; i++)
            {
                prizes[i] = "Prize " + (i + 1);
            }
        }
    }

    private void UpdateUI()
    {
        if (titleText != null)
            titleText.text = "VÒNG QUAY MAY MẮN";

        if (resultText != null)
            resultText.text = "Nhấn SPIN để bắt đầu!";

        if (countdownText != null)
            countdownText.text = "";
    }

    void Update()
    {
        if (isSpinning)
        {
            float elapsedTime = Time.time - spinStartTime;
            float t = elapsedTime / spinDuration;

            if (countdownText != null)
            {
                float remaining = spinDuration - elapsedTime;
                countdownText.text = Mathf.CeilToInt(remaining).ToString();
            }

            if (t < 1f)
            {
                float easedT = spinCurve.Evaluate(t);
                float currentAngle = Mathf.Lerp(startAngle, targetAngle, easedT);
                pointerTransform.eulerAngles = new Vector3(0, 0, currentAngle);
            }
            else
            {
                pointerTransform.eulerAngles = new Vector3(0, 0, targetAngle);
                isSpinning = false;
                OnSpinComplete();
            }
        }
    }

    public void StartSpin()
    {
        if (!isSpinning)
        {
            if (prizeAngles == null || prizeAngles.Length == 0)
            {
                InitializeAllSettings();
            }

            // Bắt đầu quay NGAY LẬP TỨC
            if (spinParticles != null)
                spinParticles.Play();

            // Chọn section dựa trên xác suất
            targetSection = GetRandomSectionByProbability();
            StartCoroutine(SpinPointer(targetSection));

            // Gọi API trong background (không đợi)
            StartCoroutine(DeductWheelInBackground());
        }
    }

    private int GetRandomSectionByProbability()
    {
        // Random số từ 0-100
        float randomValue = Random.Range(0f, 100f);
        
        // Tìm section tương ứng dựa trên xác suất
        // Giả sử prizes array có format: ["Sao Trắng", "Sao Xanh", "Sao Đỏ", ...]
        
        // 50% - Sao Trắng
        if (randomValue < 50f)
        {
            return GetRandomSectionByPrizeType("sao trắng", "sao trang");
        }
        // 30% - Sao Xanh (50% -> 80%)
        else if (randomValue < 80f)
        {
            return GetRandomSectionByPrizeType("sao xanh");
        }
        // 20% - Sao Đỏ (80% -> 100%)
        else
        {
            return GetRandomSectionByPrizeType("sao đỏ", "sao do");
        }
    }

    private int GetRandomSectionByPrizeType(params string[] prizeKeywords)
    {
        // Tìm tất cả section có chứa từ khóa
        List<int> matchingSections = new List<int>();
        
        for (int i = 0; i < prizes.Length; i++)
        {
            string prizeLower = prizes[i].ToLower();
            foreach (string keyword in prizeKeywords)
            {
                if (prizeLower.Contains(keyword))
                {
                    matchingSections.Add(i);
                    break;
                }
            }
        }
        
        // Nếu tìm thấy section phù hợp, random trong danh sách đó
        if (matchingSections.Count > 0)
        {
            int randomIndex = Random.Range(0, matchingSections.Count);
            return matchingSections[randomIndex];
        }
        
        // Fallback: random bất kỳ nếu không tìm thấy
        return Random.Range(0, numberOfSections);
    }

    private IEnumerator DeductWheelInBackground()
    {
        // Không show loading để không làm gián đoạn animation
        yield return APIManager.Instance.PostRequest<SpinResponse>(
            APIConfig.SPIN_WHEEL(userId),
            null,
            OnSpinSuccessBackground,
            OnSpinError
        );
    }

    private void OnSpinSuccessBackground(SpinResponse response)
    {
        if (response.success)
        {
            currentWheelCount = response.remainingWheel;
            UpdateWheelUI();
            UpdateYourWheelUI();

            if (ManagerQuangTruong.Instance != null)
            {
                ManagerQuangTruong.Instance.UpdateWheelFlag(currentWheelCount);
            }
        }
    }

    private void OnSpinError(string error)
    {
        Debug.LogError("Spin API Error: " + error);
    }

    private IEnumerator SpinPointer(int targetSec)
    {
        isSpinning = true;
        spinButton.interactable = false;

        if (resultText != null)
            resultText.text = "Đang quay...";

        startAngle = pointerTransform.eulerAngles.z;
        targetAngle = prizeAngles[targetSec];

        int fullRotations = 5;
        targetAngle += 360f * fullRotations;

        spinStartTime = Time.time;

        yield return null;
    }

    private void OnSpinComplete()
    {
        spinButton.interactable = true;

        if (spinParticles != null)
            spinParticles.Stop();

        if (winParticles != null)
            winParticles.Play();

        float finalAngle = pointerTransform.eulerAngles.z % 360f;
        if (finalAngle < 0) finalAngle += 360f;

        int winningSection = FindClosestSection(finalAngle);
        currentPrize = prizes[winningSection];

        if (resultText != null)
        {
            UpdateResultTextWithOutline(currentPrize);
        }

        if (countdownText != null)
            countdownText.text = "";

        // Cập nhật sao vào database và UI
        StartCoroutine(UpdateStarReward(currentPrize));

        StartCoroutine(ShowAnimationThenPanel());
    }

    private IEnumerator UpdateStarReward(string prize)
    {
        string prizeLower = prize.ToLower();
        string starType = "";
        int starAmount = 1; // Số sao mặc định nhận được

        // Xác định loại sao
        if (prizeLower.Contains("sao đỏ") || prizeLower.Contains("sao do"))
        {
            starType = "red";
        }
        else if (prizeLower.Contains("sao xanh"))
        {
            starType = "blue";
        }
        else if (prizeLower.Contains("sao trắng") || prizeLower.Contains("sao trang"))
        {
            starType = "white";
        }

        // Nếu có loại sao hợp lệ, gọi API cập nhật
        if (!string.IsNullOrEmpty(starType))
        {
            UpdateStarRequest request = new UpdateStarRequest
            {
                userId = userId,
                starType = starType,
                amount = starAmount
            };

            yield return APIManager.Instance.PostRequest<UpdateStarResponse>(
                APIConfig.UPDATE_STAR,
                request,
                OnStarUpdateSuccess,
                OnStarUpdateError
            );
        }
    }

    private void OnStarUpdateSuccess(UpdateStarResponse response)
{
    Debug.Log($"[PointerSpin] Star update success - White: {response.starWhite}, Blue: {response.starBlue}, Red: {response.starRed}");
    
    if (response.success)
    {
        // Cập nhật PlayerPrefs TRƯỚC
        PlayerPrefs.SetInt("StarWhite", response.starWhite);
        PlayerPrefs.SetInt("StarBlue", response.starBlue);
        PlayerPrefs.SetInt("StarRed", response.starRed);
        PlayerPrefs.Save();
        
        // Cập nhật UI trong ManagerQuangTruong
        if (ManagerQuangTruong.Instance != null)
        {
            ManagerQuangTruong.Instance.UpdateStarUI(
                response.starWhite,
                response.starBlue,
                response.starRed
            );
        }
        
        // Trigger event để PanelPetHT biết
        if (StarEventManager.Instance != null)
        {
            StarEventManager.Instance.UpdateStarCount(
                response.starWhite,
                response.starBlue,
                response.starRed
            );
        }
        else
        {
            Debug.LogWarning("[PointerSpin] StarEventManager.Instance is NULL!");
        }
    }
}

    private void OnStarUpdateError(string error)
    {
        Debug.LogError("Update Star API Error: " + error);
    }

    private void UpdateResultTextWithOutline(string prize)
    {
        string prizeLower = prize.ToLower();

        Outline outline = resultText.GetComponent<Outline>();
        if (outline != null)
        {
            Destroy(outline);
        }

        resultText.text = "Chúc mừng! Bạn nhận được: " + prize;

        if (prizeLower.Contains("sao đỏ") || prizeLower.Contains("sao do"))
        {
            outline = resultText.gameObject.AddComponent<Outline>();
            Color redColor;
            ColorUtility.TryParseHtmlString("#CE370E", out redColor);
            outline.effectColor = redColor;
            outline.effectDistance = new Vector2(2, -2);
        }
        else if (prizeLower.Contains("sao xanh"))
        {
            outline = resultText.gameObject.AddComponent<Outline>();
            Color blueColor;
            ColorUtility.TryParseHtmlString("#1E90FF", out blueColor);
            outline.effectColor = blueColor;
            outline.effectDistance = new Vector2(2, -2);
        }
        else if (prizeLower.Contains("sao trắng") || prizeLower.Contains("sao trang"))
        {
            outline = resultText.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            outline.effectDistance = new Vector2(2, -2);
        }
    }

    private IEnumerator ShowAnimationThenPanel()
    {
        if (animatorRW != null)
        {
            animatorRW.gameObject.SetActive(true);
            animatorRW.Play(animationName);

            yield return new WaitForEndOfFrame();

            AnimatorStateInfo stateInfo = animatorRW.GetCurrentAnimatorStateInfo(0);
            float animLength = stateInfo.length;

            yield return new WaitForSeconds(animLength);

            animatorRW.gameObject.SetActive(false);
        }

        // Hiển thị panel với resultText đã được cập nhật
        if (panelResult != null)
        {
            panelResult.SetActive(true);
        }
    }

    private void ClosePanelResult()
    {
        if (panelResult != null)
            panelResult.SetActive(false);
    }

    private int FindClosestSection(float angle)
    {
        int closestSection = 0;
        float minDifference = Mathf.Abs(angle - prizeAngles[0]);

        for (int i = 1; i < numberOfSections; i++)
        {
            float difference = Mathf.Abs(angle - prizeAngles[i]);
            if (difference < minDifference)
            {
                minDifference = difference;
                closestSection = i;
            }
        }
        return closestSection;
    }

    void OnError(string error)
    {
        Debug.LogError("API Error: " + error);
    }

    void OnDestroy()
    {
        if (spinButton != null)
            spinButton.onClick.RemoveListener(OnSpinButtonClicked);
        if (btnOke != null)
            btnOke.onClick.RemoveListener(ClosePanelResult);
    }
}

// DTO Classes
[System.Serializable]
public class WheelResponse
{
    public int userId;
    public int wheel;
    public bool hasWheel;
}

[System.Serializable]
public class SpinResponse
{
    public bool success;
    public string message;
    public int remainingWheel;
    public int userId;
}

[System.Serializable]
public class UpdateStarRequest
{
    public int userId;
    public string starType; // "white", "blue", "red"
    public int amount;
}

[System.Serializable]
public class UpdateStarResponse
{
    public bool success;
    public string message;
    public int starWhite;
    public int starBlue;
    public int starRed;
}