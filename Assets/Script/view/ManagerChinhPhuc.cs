using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ManagerChinhPhuc : MonoBehaviour
{
    public GameObject LoadingPanel;
    public GameObject[] panels;
    public Button[] buttons;
    public GameObject panelMain;
    public GameObject backBtn;
    public Text txtVang;
    public Text txtCt;
    public Text txtNl;
    public GameObject notice;
    public Button cancleNotice;

    [Header("UI")]
    [SerializeField] private Material grayscaleUIMaterial;

    [Header("Animation Settings")]
    [SerializeField] private float buttonPopDelay = 0.08f;
    [SerializeField] private float panelSlideSpeed = 0.4f;
    [SerializeField] private bool enableParticleEffects = true;

    private Material _runtimeGrayMat;
    private bool isDataLoaded = false;
    private List<GroupDTO> cachedPetData;

    [Header("Close Button")]
    public Button btnClose;

    private void Start()
    {
        Debug.Log("ChinhPhuc--Start");

        // Setup close button
        if (btnClose != null)
        {
            btnClose.onClick.AddListener(ClosePanel);
        }

        // Setup back button để đóng panel main
        if (backBtn != null)
        {
            Button backButton = backBtn.GetComponent<Button>();
            if (backButton != null)
            {
                backButton.onClick.AddListener(BackScene);
            }
        }

        // Ẩn tất cả các panel ban đầu
        HideAllPanels();

        // Gán sự kiện cho các nút với animation
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            buttons[i].onClick.AddListener(() => ShowPanel(index));

            // Thêm hover animation
            AddButtonHoverEffect(buttons[i].gameObject);
        }

        // Setup notice button
        if (cancleNotice != null)
        {
            cancleNotice.onClick.AddListener(ToggleNotice);
        }
    }

    /// <summary>
    /// Đóng panel Chinh Phục với animation
    /// </summary>
    public void ClosePanel()
    {
        Debug.Log("[ChinhPhuc] Closing panel...");

        // ✅ XÓA FLAG NẾU ĐÓNG PANEL THỦ CÔNG
        PlayerPrefs.DeleteKey("ChinhPhucWasOpen");
        PlayerPrefs.Save();

        HideAllPanels();

        GameObject mainPanel = this.gameObject;

        LeanTween.scale(mainPanel, Vector3.zero, 0.3f)
            .setEase(LeanTweenType.easeInBack)
            .setOnComplete(() =>
            {
                mainPanel.SetActive(false);
                mainPanel.transform.localScale = Vector3.one;
                Debug.Log("[ChinhPhuc] Panel closed");
            });
    }

    /// <summary>
    /// Khôi phục trạng thái panel sau khi quay về
    /// </summary>
    public void RestoreState(int panelIndex)
    {
        Debug.Log($"[ChinhPhuc] Restoring state - PanelIndex: {panelIndex}");

        if (panelIndex < 0 || panelIndex >= panels.Length)
        {
            Debug.LogWarning($"[ChinhPhuc] Invalid panel index: {panelIndex}");
            return;
        }

        // Hiện panel main NGAY (không animation)
        if (panelMain != null)
        {
            panelMain.SetActive(true);

            CanvasGroup cg = panelMain.GetComponent<CanvasGroup>();
            if (cg == null) cg = panelMain.AddComponent<CanvasGroup>();
            cg.alpha = 1f; // Hiện ngay, không fade
        }

        // Hiện back button NGAY
        if (backBtn != null)
        {
            backBtn.SetActive(true);
            // backBtn.transform.localScale = Vector3.one;
        }

        // Mở đúng panel đã chọn NGAY (không animation)
        foreach (GameObject panel in panels)
        {
            panel.SetActive(false);
        }

        GameObject targetPanel = panels[panelIndex];
        targetPanel.SetActive(true);

        // Đặt position đúng ngay lập tức
        RectTransform rt = targetPanel.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = Vector3.zero; // Hoặc vị trí ban đầu của nó
        }

        Debug.Log($"[ChinhPhuc] ✓ State restored - Panel {panelIndex} active");
    }

    // ✅ THÊM: Method ShowPanel với option không animation
    /// <summary>
    /// ✅ UPDATED: Hiện loading khi nhấn island thay vì animation
    /// </summary>
    public void ShowPanel(int index, bool withAnimation = true)
    {
        Debug.Log($"Button clicked, index: {index}, animation: {withAnimation}");

        // ✅ HIỆN LOADING NGAY KHI NHẤN
        if (withAnimation && LoadingPanel != null)
        {
            LoadingPanel.SetActive(true);
            LoadingPanel.transform.localScale = Vector3.one; // Hiện ngay, không scale animation
        }

        // ✅ Delay một chút để loading hiện rõ, sau đó mới hiện panel
        StartCoroutine(ShowPanelWithLoadingCoroutine(index, withAnimation));
    }

    /// <summary>
    /// ✅ Coroutine để show panel sau khi loading hiện
    /// </summary>
    private IEnumerator ShowPanelWithLoadingCoroutine(int index, bool withAnimation)
    {
        // Đợi 0.3-0.5s để người dùng thấy loading (tuỳ chỉnh theo ý)
        yield return new WaitForSeconds(1f);

        // ✅ ẨN LOADING
        if (LoadingPanel != null)
        {
            LoadingPanel.SetActive(false);
        }

        // ✅ Hiện back button
        if (backBtn != null)
        {
            backBtn.SetActive(true);
            // backBtn.transform.localScale = Vector3.one; // Không animation
        }

        // ✅ Hiện panel main
        if (panelMain != null)
        {
            panelMain.SetActive(true);

            CanvasGroup cg = panelMain.GetComponent<CanvasGroup>();
            if (cg == null) cg = panelMain.AddComponent<CanvasGroup>();
            cg.alpha = 1f; // Hiện ngay, không fade
        }

        // ✅ Lưu index
        PlayerPrefs.SetInt("ActivePanelIndex", index);
        PlayerPrefs.Save();

        // ✅ Tắt tất cả panel
        foreach (GameObject panel in panels)
        {
            panel.SetActive(false);
        }

        // ✅ Hiện panel được chọn
        GameObject targetPanel = panels[index];
        targetPanel.SetActive(true);

        // ✅ Set position ngay lập tức, không slide animation
        RectTransform rt = targetPanel.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = Vector3.zero;
        }

        Debug.Log($"[ChinhPhuc] ✓ Panel {index} shown (with loading)");
    }

    /// <summary>
    /// Method này được gọi từ ManagerQuangTruong khi nhấn button "Chinh Phục"
    /// </summary>
    public void InitializeAndLoadData(System.Action onComplete = null)
    {
        StartCoroutine(LoadDataCoroutine(onComplete));
    }

    // ✅ THAY THẾ METHOD LoadDataCoroutine() TRONG ManagerChinhPhuc

    /// <summary>
    /// ✅ IMPROVED: Load data với loading - Hide loading khi xong
    /// </summary>
    private IEnumerator LoadDataCoroutine(System.Action onComplete)
    {
        // Nếu đã có cached data, không cần load lại
        if (isDataLoaded && cachedPetData != null && cachedPetData.Count > 0)
        {
            Debug.Log("[ChinhPhuc] Using cached data");

            // ✅ VẪN PHẢI HIDE LOADING NẾU ĐANG SHOW
            ManagerGame.Instance.HideLoading();
            if (LoadingPanel != null)
            {
                LoadingPanel.SetActive(false);
            }

            yield break;
        }

        int userId = PlayerPrefs.GetInt("userId", 1);

        // ✅ KIỂM TRA XEM CÓ ĐANG RESTORE STATE KHÔNG
        bool isRestoring = PlayerPrefs.GetInt("ReturnToRoom", 0) == 1;

        Debug.Log($"[ChinhPhuc] Loading data from API... (isRestoring={isRestoring})");

        // ✅ NẾU KHÔNG RESTORE, LOADING ĐÃ ĐƯỢC SHOW BỞI OpenChinhPhucPanel()
        // NẾU RESTORE, LOADING ĐƯỢC SHOW BỞI ManagerQuangTruong

        bool apiCompleted = false;
        List<GroupDTO> loadedData = null;
        string errorMessage = null;

        yield return APIManager.Instance.GetRequest<List<GroupDTO>>(
            APIConfig.GET_PETS_ENEMYS(userId),
            (data) =>
            {
                loadedData = data;
                apiCompleted = true;
                Debug.Log($"[ChinhPhuc] ✓ API Success - {data.Count} groups loaded");
            },
            (error) =>
            {
                errorMessage = error;
                apiCompleted = true;
                Debug.LogError($"[ChinhPhuc] ✗ API Error: {error}");
            }
        );

        // Đợi API hoàn thành
        while (!apiCompleted)
        {
            yield return null;
        }

        // ✅ HIDE LOADING SAU KHI LOAD XONG (CHO CẢ 2 TRƯỜNG HỢP)
        Debug.Log("[ChinhPhuc] → Hiding loading after API complete");
        ManagerGame.Instance.HideLoading();
        if (LoadingPanel != null)
        {
            LoadingPanel.SetActive(false);
        }

        if (loadedData != null)
        {
            OnReceived(loadedData);
            isDataLoaded = true;

            if (!isRestoring)
            {
                // ✅ CHỈ ANIMATE NẾU KHÔNG RESTORE
                AnimateInitialUI();
            }
        }
        else if (errorMessage != null)
        {
            OnError(errorMessage);
        }
        yield return new WaitForSeconds(0.5f); // Buffer time

        onComplete?.Invoke(); // ✅ GỌI CALLBACK

        Debug.Log("[ManagerChinhPhuc] Data loaded completely!");
    }

    /// <summary>
    /// ✅ Show loading panel với animation
    /// </summary>
    private void ShowLoadingPanel()
    {
        if (LoadingPanel == null)
        {
            Debug.LogWarning("[ChinhPhuc] LoadingPanel not assigned!");
            return;
        }

        Debug.Log("[ChinhPhuc] → Showing loading panel");

        LoadingPanel.SetActive(true);

        // Scale animation
        LoadingPanel.transform.localScale = Vector3.zero;
        LeanTween.scale(LoadingPanel, Vector3.one, 0.3f)
            .setEaseOutBack();
    }

    /// <summary>
    /// ✅ Hide loading panel với animation
    /// </summary>
    private void HideLoadingPanel()
    {
        if (LoadingPanel == null || !LoadingPanel.activeSelf)
        {
            return;
        }

        Debug.Log("[ChinhPhuc] → Hiding loading panel");

        LeanTween.scale(LoadingPanel, Vector3.zero, 0.25f)
            .setEaseInBack()
            .setOnComplete(() =>
            {
                LoadingPanel.SetActive(false);
                LoadingPanel.transform.localScale = Vector3.one; // Reset scale
            });
    }

    private void AnimateInitialUI()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                buttons[i].transform.localScale = Vector3.zero;
                LeanTween.scale(buttons[i].gameObject, Vector3.one, 0.4f)
                    .setEaseOutBack()
                    .setDelay(i * 0.1f);
            }
        }

        if (txtVang != null)
        {
            AnimateTextFadeIn(txtVang, 0.3f);
        }
        if (txtCt != null)
        {
            AnimateTextFadeIn(txtCt, 0.4f);
        }
        if (txtNl != null)
        {
            AnimateTextFadeIn(txtNl, 0.5f);
        }
    }

    private void AnimateTextFadeIn(Text text, float delay)
    {
        if (text == null) return;

        Color originalColor = text.color;
        text.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0);

        LeanTween.value(text.gameObject, 0f, originalColor.a, 0.3f)
            .setOnUpdate((float val) =>
            {
                text.color = new Color(originalColor.r, originalColor.g, originalColor.b, val);
            })
            .setDelay(delay);
    }

    private void AddButtonHoverEffect(GameObject buttonObj)
    {
        UnityEngine.EventSystems.EventTrigger trigger = buttonObj.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (trigger == null)
        {
            trigger = buttonObj.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        }

        UnityEngine.EventSystems.EventTrigger.Entry entryEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
        entryEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data) =>
        {
            LeanTween.cancel(buttonObj);
            LeanTween.scale(buttonObj, Vector3.one * 1.1f, 0.2f)
                .setEaseOutBack();
        });
        trigger.triggers.Add(entryEnter);

        UnityEngine.EventSystems.EventTrigger.Entry entryExit = new UnityEngine.EventSystems.EventTrigger.Entry();
        entryExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) =>
        {
            LeanTween.cancel(buttonObj);
            LeanTween.scale(buttonObj, Vector3.one, 0.2f)
                .setEaseOutBack();
        });
        trigger.triggers.Add(entryExit);
    }

    public void OnReceived(List<GroupDTO> petE)
    {
        if (petE == null || petE.Count == 0)
        {
            Debug.LogError("[ChinhPhuc] Pet enemy data is null or empty!");
            return;
        }

        Debug.Log($"[ChinhPhuc] ✓ Processing {petE.Count} pet groups");

        cachedPetData = petE;

        for (int i = 0; i < petE.Count; i++)
        {
            if (i >= panels.Length)
            {
                Debug.LogWarning($"[ChinhPhuc] Panel index {i} out of range!");
                break;
            }

            GroupDTO panelData = petE[i];
            GameObject panel = panels[i];

            if (panel == null)
            {
                Debug.LogError($"[ChinhPhuc] Panel {i} is null!");
                continue;
            }

            Button[] buttons = panel.GetComponentsInChildren<Button>(true);

            Debug.Log($"[ChinhPhuc] Panel {i} ({panelData.name}): {buttons.Length} buttons found");

            for (int j = 0; j < buttons.Length; j++)
            {
                if (buttons[j] == null)
                {
                    Debug.LogWarning($"[ChinhPhuc] Button {j} in panel {i} is null!");
                    continue;
                }

                if (j >= panelData.listPetEnemy.Count())
                {
                    buttons[j].gameObject.SetActive(false);
                }
                else
                {
                    try
                    {
                        buttons[j].gameObject.SetActive(true);

                        var petEnemy = panelData.listPetEnemy[j];

                        Image mainIcon = buttons[j].GetComponentInChildren<Image>();
                        if (mainIcon != null)
                        {
                            Sprite petSprite = Resources.Load<Sprite>("Image/IconsPet/" + petEnemy.id);
                            if (petSprite != null)
                            {
                                mainIcon.sprite = petSprite;
                            }
                            else
                            {
                                Debug.LogWarning($"[ChinhPhuc] Pet sprite not found: Image/IconsPet/{petEnemy.id}");
                            }
                        }

                        Transform imageTransform = buttons[j].transform.Find("Image");
                        if (imageTransform == null)
                        {
                            Debug.LogWarning($"[ChinhPhuc] 'Image' child not found in button {j}, panel {i}");
                            continue;
                        }

                        GameObject panelEnemy = imageTransform.gameObject;

                        Transform txtLvTransform = panelEnemy.transform.Find("txtLv");
                        if (txtLvTransform != null)
                        {
                            Text txtLv = txtLvTransform.GetComponent<Text>();
                            if (txtLv != null)
                            {
                                string petName = string.IsNullOrEmpty(petEnemy.name) ? "Pet" : petEnemy.name;
                                txtLv.text = petName + " Lv." + petEnemy.leverDisplay;
                            }
                        }

                        Transform txtYcTransform = panelEnemy.transform.Find("txtYc");
                        if (txtYcTransform != null)
                        {
                            Text txtYc = txtYcTransform.GetComponent<Text>();
                            if (txtYc != null)
                            {
                                txtYc.text = petEnemy.requestAttack.ToString();
                            }
                        }

                        int count = petEnemy.count;
                        int requestPass = petEnemy.requestPass;

                        Transform childImageTransform = buttons[j].transform.Find("Image");
                        Image childIcon = childImageTransform ? childImageTransform.GetComponent<Image>() : null;
                        Image targetIcon = childIcon != null ? childIcon : mainIcon;

                        bool makeGray = count < requestPass;
                        SetImageGrayscale(targetIcon, makeGray);

                        Transform txtWinTransform = panelEnemy.transform.Find("txtWin");
                        if (txtWinTransform != null)
                        {
                            Text txtWin = txtWinTransform.GetComponent<Text>();
                            if (txtWin != null)
                            {
                                if (count >= requestPass)
                                {
                                    txtWin.text = $"<color=yellow>{count}</color>/{requestPass}";
                                }
                                else
                                {
                                    txtWin.text = $"<color=red>{count}</color>/{requestPass}";
                                }
                            }
                        }

                        Transform txtCtTransform = panelEnemy.transform.Find("txtCt");
                        if (txtCtTransform != null)
                        {
                            Text txtCtReward = txtCtTransform.GetComponent<Text>();
                            if (txtCtReward != null)
                            {
                                txtCtReward.text = "+" + (petEnemy.requestAttack * 0.08).ToString();
                            }
                        }

                        string reA = (petEnemy.requestAttack * 0.08).ToString();
                        int petId = petEnemy.id;

                        int requiredAttack = petEnemy.requestAttack; // Lưu giá trị yêu cầu
                        buttons[j].onClick.RemoveAllListeners();
                        buttons[j].onClick.AddListener(() =>
                        {
                            // ✅ Kiểm tra chiến tích tại thời điểm click
                            int currentUserAttack = ManagerQuangTruong.Instance.txtCtint;

                            if (currentUserAttack < requiredAttack)
                            {
                                ToggleNotice();
                            }
                            else
                            {
                                OpenRoomWithPet(petId, reA);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[ChinhPhuc] Error processing button {j} in panel {i}: {ex.Message}\n{ex.StackTrace}");
                    }
                }
            }
        }

        Debug.Log("[ChinhPhuc] ✓✓ OnReceived completed successfully");
    }

    private void AddPetButtonAnimation(GameObject btnObj)
    {
        btnObj.transform.localScale = Vector3.zero;
        float randomDelay = UnityEngine.Random.Range(0f, 0.3f);

        LeanTween.scale(btnObj, Vector3.one, 0.4f)
            .setEaseOutBack()
            .setDelay(randomDelay);

    }


    void OpenRoomWithPet(int petId, string reA)
    {
        Debug.Log($"[ChinhPhuc] Opening Room panel with petId: {petId}, requestAttack: {reA}");

        PlayerPrefs.SetInt("SelectedPetId", petId);

        reA = reA.Replace(',', '.');
        float result = float.Parse(reA, CultureInfo.InvariantCulture);
        int ct = (int)Math.Round(result);
        PlayerPrefs.SetInt("requestAttack", ct);
        if (cachedPetData != null)
        {
            for (int groupIndex = 0; groupIndex < cachedPetData.Count; groupIndex++)
            {
                var group = cachedPetData[groupIndex];
                var foundPet = group.listPetEnemy.FirstOrDefault(p => p.id == petId);
                if (foundPet != null)
                {
                    PlayerPrefs.SetInt("EnemyPetLevel", foundPet.leverDisplay);
                    PlayerPrefs.SetInt("SelectedGroupIndex", groupIndex); // ✅ LƯU GROUP INDEX
                    Debug.Log($"[ChinhPhuc] Saved Group Index: {groupIndex}, Enemy Pet Level: {foundPet.leverDisplay}");
                    break;
                }
            }
        }
        PlayerPrefs.Save();

        ManagerRoom roomManager = FindObjectOfType<ManagerRoom>();
        if (roomManager != null)
        {
            roomManager.OpenRoomPanel();
        }
        else
        {
            Debug.LogError("[ChinhPhuc] ManagerRoom not found in scene!");
        }
    }

    private void AnimateNumberCount(Text textComponent, int targetValue)
    {
        if (textComponent == null) return;

        int currentValue = 0;
        int.TryParse(textComponent.text, out currentValue);

        LeanTween.value(textComponent.gameObject, currentValue, targetValue, 0.8f)
            .setOnUpdate((float val) =>
            {
                textComponent.text = Mathf.RoundToInt(val).ToString();
            })
            .setEaseOutQuad();
    }

    private void AnimateTextPulse(Text textComponent)
    {
        if (textComponent == null) return;

        Vector3 originalScale = textComponent.transform.localScale;

        LeanTween.scale(textComponent.gameObject, originalScale * 1.2f, 0.2f)
            .setEaseOutQuad()
            .setOnComplete(() =>
            {
                LeanTween.scale(textComponent.gameObject, originalScale, 0.2f)
                    .setEaseInQuad();
            });
    }

    void OnError(string error)
    {
        Debug.LogError("API Error: " + error);

        if (notice != null)
        {
            ShowNoticeWithAnimation("Có lỗi xảy ra! " + error);
        }
    }

    [Header("Transition")]
    public GameObject fadeOverlay; // Gán 1 Image đen fullscreen trong Inspector

    public void BackScene()
    {
        Debug.Log("[ChinhPhuc] Back button clicked - closing panel main");

        StartCoroutine(BackSceneWithFade());
    }

    private IEnumerator BackSceneWithFade()
    {
        // ✅ BƯỚC 1: Animate back button out
        if (backBtn != null && backBtn.activeSelf)
        {
            backBtn.SetActive(false);
        }




        // ✅ BƯỚC 4: Setup fade overlay (màn đen phủ lên)
        if (fadeOverlay != null)
        {
            fadeOverlay.SetActive(true);

            CanvasGroup overlayCanvas = fadeOverlay.GetComponent<CanvasGroup>();
            if (overlayCanvas == null)
            {
                overlayCanvas = fadeOverlay.AddComponent<CanvasGroup>();
            }
            overlayCanvas.alpha = 0f;

            // Fade to black (tối dần)
            LeanTween.alphaCanvas(overlayCanvas, 1f, 0.3f)
                .setEase(LeanTweenType.easeInQuad);

            yield return new WaitForSeconds(0.3f);
        }

        // ✅ BƯỚC 5: Tắt hết panels (khi màn hình đã đen hoàn toàn)
        if (panelMain != null)
        {
            panelMain.SetActive(false);
            CanvasGroup cg = panelMain.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f; // Reset alpha
        }

        foreach (GameObject panel in panels)
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        yield return new WaitForSeconds(0.1f);

        // ✅ BƯỚC 6: Fade from black (sáng dần trở lại)
        if (fadeOverlay != null)
        {
            CanvasGroup overlayCanvas = fadeOverlay.GetComponent<CanvasGroup>();

            LeanTween.alphaCanvas(overlayCanvas, 0f, 0.4f)
                .setEase(LeanTweenType.easeOutQuad)
                .setOnComplete(() =>
                {
                    fadeOverlay.SetActive(false);
                    Debug.Log("[ChinhPhuc] ✓ Back scene transition completed");
                });
        }
    }

    private IEnumerator AnimatePanelContent(GameObject panel)
    {
        Button[] panelButtons = panel.GetComponentsInChildren<Button>();

        for (int i = 0; i < panelButtons.Length; i++)
        {
            if (panelButtons[i].gameObject.activeSelf)
            {
                panelButtons[i].transform.localScale = Vector3.zero;

                LeanTween.scale(panelButtons[i].gameObject, Vector3.one, 0.3f)
                    .setEaseOutBack()
                    .setDelay(i * buttonPopDelay);

                if (i % 3 == 2)
                {
                    yield return new WaitForSeconds(0.05f);
                }
            }
        }
    }

    public void HideAllPanels()
    {
        if (backBtn != null)
        {
            backBtn.SetActive(false);
        }

        if (panelMain != null)
        {
            panelMain.SetActive(false);
        }

        foreach (GameObject panel in panels)
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }
    }

    void ToggleNotice()
    {
        if (notice != null)
        {
            if (!notice.activeSelf)
            {
                ShowNoticeWithAnimation("");
            }
            else
            {
                HideNoticeWithAnimation();
            }
        }
    }

    private void ShowNoticeWithAnimation(string message = "")
    {
        if (notice == null) return;

        Text noticeText = notice.GetComponentInChildren<Text>();
        if (noticeText != null && !string.IsNullOrEmpty(message))
        {
            noticeText.text = message;
        }

        // ✅ HIỆN NOTICE NGAY (KHÔNG SCALE ANIMATION)
        notice.SetActive(true);
        notice.transform.localScale = Vector3.one; // ✅ Set scale = 1 ngay lập tức

        // ✅ BỎ: LeanTween.scale animation

        // ✅ GIỮ LẠI: Shake animation nếu có lỗi (optional)
        if (message.Contains("lỗi") || message.Contains("error"))
        {
            LeanTween.rotateZ(notice, 5f, 0.1f)
                .setLoopPingPong(3)
                .setDelay(0.5f)
                .setOnComplete(() =>
                {
                    notice.transform.rotation = Quaternion.identity;
                });
        }
    }

    private void HideNoticeWithAnimation()
    {
        if (notice == null) return;

        // ✅ ẨN NOTICE NGAY (KHÔNG SCALE ANIMATION)
        notice.SetActive(false);

        // ✅ BỎ: LeanTween.scale animation
    }

    private void EnsureGrayMaterial()
    {
        if (grayscaleUIMaterial != null) return;

        if (_runtimeGrayMat == null)
        {
            var shader = Shader.Find("UI/Grayscale");
            if (shader != null)
            {
                _runtimeGrayMat = new Material(shader);
            }
            else
            {
                Debug.LogWarning("Shader 'UI/Grayscale' not found. Assign 'grayscaleUIMaterial' in Inspector.");
            }
        }
    }

    private void SetImageGrayscale(Image img, bool enable)
    {
        if (img == null) return;

        EnsureGrayMaterial();

        if (enable)
        {
            if (grayscaleUIMaterial != null)
                img.material = grayscaleUIMaterial;
            else if (_runtimeGrayMat != null)
                img.material = _runtimeGrayMat;

            img.color = Color.white;
        }
        else
        {
            img.material = null;
            img.color = Color.white;
        }
    }

    private void OnDestroy()
    {
        LeanTween.cancel(gameObject);

        if (LoadingPanel != null) LeanTween.cancel(LoadingPanel);
        if (panelMain != null) LeanTween.cancel(panelMain);
        if (backBtn != null) LeanTween.cancel(backBtn);
        if (notice != null) LeanTween.cancel(notice);

        foreach (var panel in panels)
        {
            if (panel != null) LeanTween.cancel(panel);
        }

        foreach (var button in buttons)
        {
            if (button != null) LeanTween.cancel(button.gameObject);
        }
    }
}