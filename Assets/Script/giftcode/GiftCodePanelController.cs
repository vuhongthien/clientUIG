using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using DG.Tweening;

public class GiftCodePanelController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject giftCodePanel;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private InputField codeInputField;   // ⬅ InputField thường
    [SerializeField] private Button redeemButton;
    [SerializeField] private Text messageText;             // ⬅ Text thường

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private LeanTweenType easeType = LeanTweenType.easeInOutQuad;

    private CanvasGroup canvasGroup;
    private bool isAnimating = false;
    private bool isProcessing = false;

    [Header("Panel Notice")]
    public GameObject panelNotice;
    public Text txtNoticeMessage;

    void Start()
    {
        canvasGroup = giftCodePanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = giftCodePanel.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        giftCodePanel.SetActive(false);

        // if (messageText != null) messageText.text = "";

        if (openButton != null) openButton.onClick.AddListener(OpenPanel);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
        if (redeemButton != null) redeemButton.onClick.AddListener(OnRedeemClicked);
    }

    public void OpenPanel()
    {
        if (isAnimating || giftCodePanel.activeSelf) return;

        isAnimating = true;
        giftCodePanel.SetActive(true);
        canvasGroup.alpha = 0f;

        if (codeInputField != null) codeInputField.text = "";
        // if (messageText != null) messageText.text = "";

        LeanTween.alphaCanvas(canvasGroup, 1f, animationDuration)
            .setEase(easeType)
            .setOnComplete(() => isAnimating = false);
    }

    public void ClosePanel()
    {
        if (isAnimating || !giftCodePanel.activeSelf) return;

        isAnimating = true;

        LeanTween.alphaCanvas(canvasGroup, 0f, animationDuration)
            .setEase(easeType)
            .setOnComplete(() =>
            {
                giftCodePanel.SetActive(false);
                isAnimating = false;
            });
    }

    private void OnRedeemClicked()
    {
        if (isProcessing) return;

        string code = codeInputField.text.Trim().ToUpper();

        if (string.IsNullOrEmpty(code))
        {
            ShowMessage("Vui lòng nhập mã gift code!", Color.red);
            return;
        }

        StartCoroutine(RedeemGiftCode(code));
    }

    private IEnumerator RedeemGiftCode(string code)
    {
        isProcessing = true;
        if (redeemButton != null) redeemButton.interactable = false;

        ShowMessage("Đang xử lý...", Color.yellow);

        int userId = PlayerPrefs.GetInt("userId", 0);
        if (userId == 0)
        {
            ShowMessage("Lỗi: Không tìm thấy User ID!", Color.red);
            isProcessing = false;
            redeemButton.interactable = true;
            yield break;
        }

        string url = APIConfig.REDEEM_GIFT_CODE(userId, code);
        Debug.Log($"[GiftCode] Redeeming: {code} for userId={userId}");

        yield return APIManager.Instance.PostRequestRaw(
            url,
            null,
            OnRedeemSuccess,
            OnRedeemError
        );

        isProcessing = false;
        redeemButton.interactable = true;
    }

    private void OnRedeemSuccess(string response)
    {
        Debug.Log($"[GiftCode] Success: {response}");

        try
        {
            GiftCodeResponse result = JsonUtility.FromJson<GiftCodeResponse>(response);

            if (result.success)
            {
                codeInputField.text = "";
                StartCoroutine(CloseAfterDelay(0.5f));

                int userId = PlayerPrefs.GetInt("userId", 0);
                if (ManagerQuangTruong.Instance != null)
                {
                    ManagerQuangTruong.Instance.RefreshUserInfo();
                    ManagerQuangTruong.Instance.CheckForGifts(userId);
                }
            }
            else
            {
                ShowMessage(result.message, Color.red);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[GiftCode] Parse error: {e.Message}");
            ShowMessage("Lỗi xử lý dữ liệu!", Color.red);
        }
    }

    private void OnRedeemError(string error)
    {
        Debug.LogError($"[GiftCode] ErrorRaw: {error}");

        // --- 1) Tìm JSON thật trong đoạn chuỗi (search { ... }) ---
        int jsonStart = error.IndexOf("{");
        int jsonEnd = error.LastIndexOf("}");

        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            string json = error.Substring(jsonStart, (jsonEnd - jsonStart) + 1);

            try
            {
                GiftCodeResponse result = JsonUtility.FromJson<GiftCodeResponse>(json);
                ShowMessage(result.message, Color.red);
                return;
            }
            catch (Exception ex)
            {
                Debug.LogError("[GiftCode] JSON parse error: " + ex.Message);
            }
        }

        // --- 2) Nếu không tìm thấy JSON ---
        ShowMessage("Lỗi kết nối server!", Color.red);
    }


    private IEnumerator CloseAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ClosePanel();
    }

    private void ShowMessage(string message, Color color)
    {
        if (messageText != null)
        {
            messageText.text = message;
            messageText.color = color;
        }
    }

    void OnDestroy()
    {
        if (openButton != null) openButton.onClick.RemoveListener(OpenPanel);
        if (closeButton != null) closeButton.onClick.RemoveListener(ClosePanel);
        if (redeemButton != null) redeemButton.onClick.RemoveListener(OnRedeemClicked);

        LeanTween.cancel(giftCodePanel);
    }
}

[Serializable]
public class GiftCodeResponse
{
    public bool success;
    public string message;
}
