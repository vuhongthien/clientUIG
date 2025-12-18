using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingsManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject panelSettings;

    [Header("Buttons")]
    public Button btnOpenSettings;
    public Button btnCloseSettings;
    public Button btnLogout;

    [Header("Volume Sliders")]
    public Slider sliderMasterVolume;
    public Slider sliderBGMVolume;
    public Slider sliderSFXVolume;

    [Header("Volume Texts (Optional)")]
    public Text txtMasterVolume;
    public Text txtBGMVolume;
    public Text txtSFXVolume;

    [Header("Confirm Logout Panel (Optional)")]
    public GameObject panelConfirmLogout;
    public Button btnConfirmLogout;
    public Button btnCancelLogout;

    private AudioSettingsManager audioSettingsManager;

    private void Start()
    {
        // ✅ TÌM AudioSettingsManager TRONG SCENE
        audioSettingsManager = FindObjectOfType<AudioSettingsManager>();

        // Ẩn panels
        if (panelSettings != null)
            panelSettings.SetActive(false);

        if (panelConfirmLogout != null)
            panelConfirmLogout.SetActive(false);

        // Setup buttons
        if (btnOpenSettings != null)
            btnOpenSettings.onClick.AddListener(OpenSettings);

        if (btnCloseSettings != null)
            btnCloseSettings.onClick.AddListener(CloseSettings);

        if (btnLogout != null)
            btnLogout.onClick.AddListener(ShowLogoutConfirmation);

        // Setup sliders
        if (sliderMasterVolume != null)
            sliderMasterVolume.onValueChanged.AddListener(OnMasterVolumeChanged);

        if (sliderBGMVolume != null)
            sliderBGMVolume.onValueChanged.AddListener(OnBGMVolumeChanged);

        if (sliderSFXVolume != null)
            sliderSFXVolume.onValueChanged.AddListener(OnSFXVolumeChanged);

        // Setup confirm logout buttons
        if (btnConfirmLogout != null)
            btnConfirmLogout.onClick.AddListener(ConfirmLogout);

        if (btnCancelLogout != null)
            btnCancelLogout.onClick.AddListener(CancelLogout);

        // Load current values
        LoadCurrentVolumes();
    }

    /// <summary>
    /// Mở panel settings
    /// </summary>
    public void OpenSettings()
    {
        if (panelSettings != null)
        {
            panelSettings.SetActive(true);
            LoadCurrentVolumes();
        }
    }

    /// <summary>
    /// Đóng panel settings
    /// </summary>
    void CloseSettings()
    {
        if (panelSettings != null)
        {
            panelSettings.SetActive(false);
        }

        if (panelConfirmLogout != null && panelConfirmLogout.activeSelf)
        {
            panelConfirmLogout.SetActive(false);
        }
    }

    /// <summary>
    /// ✅ Load giá trị volume từ PlayerPrefs
    /// </summary>
    void LoadCurrentVolumes()
    {
        // ✅ LOAD TRỰC TIẾP TỪ PLAYERPREFS
        AudioSettings settings = AudioSettingsManager.GetSavedSettings();

        if (sliderMasterVolume != null)
        {
            sliderMasterVolume.SetValueWithoutNotify(settings.masterVolume);
            UpdateVolumeText(txtMasterVolume, settings.masterVolume);
        }

        if (sliderBGMVolume != null)
        {
            sliderBGMVolume.SetValueWithoutNotify(settings.bgmVolume);
            UpdateVolumeText(txtBGMVolume, settings.bgmVolume);
        }

        if (sliderSFXVolume != null)
        {
            sliderSFXVolume.SetValueWithoutNotify(settings.sfxVolume);
            UpdateVolumeText(txtSFXVolume, settings.sfxVolume);
        }
    }

    // ==================== VOLUME CALLBACKS ====================

    void OnMasterVolumeChanged(float value)
{
    if (audioSettingsManager != null)
    {
        audioSettingsManager.SetMasterVolume(value);
    }
    else
    {
        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();
        
        // ✅ TRIGGER EVENT TRỰC TIẾP NẾU KHÔNG CÓ MANAGER
        AudioEventManager.NotifyMasterVolumeChanged(value);
    }

    UpdateVolumeText(txtMasterVolume, value);
}

    void OnBGMVolumeChanged(float value)
    {
        if (audioSettingsManager != null)
        {
            audioSettingsManager.SetBGMVolume(value);
        }
        else
        {
            PlayerPrefs.SetFloat("BGMVolume", value);
            PlayerPrefs.Save();
        }

        UpdateVolumeText(txtBGMVolume, value);
    }

    void OnSFXVolumeChanged(float value)
{
    if (audioSettingsManager != null)
    {
        audioSettingsManager.SetSFXVolume(value);
    }
    else
    {
        PlayerPrefs.SetFloat("SFXVolume", value);
        PlayerPrefs.Save();
        
        // ✅ TRIGGER EVENT TRỰC TIẾP NẾU KHÔNG CÓ MANAGER
        AudioEventManager.NotifySFXVolumeChanged(value);
    }

    UpdateVolumeText(txtSFXVolume, value);
}

    /// <summary>
    /// Cập nhật text hiển thị volume (%)
    /// </summary>
    void UpdateVolumeText(Text textComponent, float value)
    {
        if (textComponent != null)
        {
            textComponent.text = Mathf.RoundToInt(value * 100) + "%";
        }
    }

    // ==================== LOGOUT ====================

    void ShowLogoutConfirmation()
    {
        if (panelConfirmLogout != null)
        {
            panelConfirmLogout.SetActive(true);
        }
        else
        {
            ConfirmLogout();
        }
    }

    void CancelLogout()
    {
        if (panelConfirmLogout != null)
        {
            panelConfirmLogout.SetActive(false);
        }
    }

    /// <summary>
    /// ✅ Xác nhận đăng xuất
    /// </summary>
    void ConfirmLogout()
    {
        Debug.Log("[Settings] Logging out...");

        ClearAllUserData();

        // ✅ VỀ SCENE LOGIN
        SceneManager.LoadScene("Login");
    }

    /// <summary>
    /// ✅ Xoá toàn bộ dữ liệu user (GIỮ LẠI AUDIO SETTINGS)
    /// </summary>
    void ClearAllUserData()
    {
        // ✅ LƯU LẠI AUDIO SETTINGS TRƯỚC KHI XOÁ
        float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.5f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);

        // ✅ XOÁ USER DATA
        PlayerPrefs.DeleteKey("userId");
        PlayerPrefs.DeleteKey("userName");
        PlayerPrefs.DeleteKey("userToken");
        PlayerPrefs.DeleteKey("userPetId");
        PlayerPrefs.DeleteKey("avtId");

        // ✅ XOÁ GAME DATA
        PlayerPrefs.DeleteKey("SelectedPetId");
        PlayerPrefs.DeleteKey("SelectedCards");
        PlayerPrefs.DeleteKey("requestAttack");
        PlayerPrefs.DeleteKey("requestPass");
        PlayerPrefs.DeleteKey("count");
        PlayerPrefs.DeleteKey("EnemyPetLevel");

        // ✅ XOÁ FLAGS
        PlayerPrefs.DeleteKey("ReturnToRoom");
        PlayerPrefs.DeleteKey("ReturnToChinhPhuc");
        PlayerPrefs.DeleteKey("ReturnToPanelIndex");
        PlayerPrefs.DeleteKey("ActivePanelIndex");
        PlayerPrefs.DeleteKey("ChinhPhucWasOpen");
        PlayerPrefs.DeleteKey("IsBossBattle");
        PlayerPrefs.DeleteKey("BossElementType");
        PlayerPrefs.DeleteKey("CurrentBossId");

        // ✅ XOÁ STAR DATA
        PlayerPrefs.DeleteKey("StarWhite");
        PlayerPrefs.DeleteKey("StarBlue");
        PlayerPrefs.DeleteKey("StarRed");

        // ✅ KHÔI PHỤC LẠI AUDIO SETTINGS
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("BGMVolume", bgmVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);

        PlayerPrefs.Save();

        Debug.Log("[Settings] ✓ User data cleared, audio settings preserved");
    }

    private void OnDestroy()
    {
        if (btnOpenSettings != null)
            btnOpenSettings.onClick.RemoveListener(OpenSettings);

        if (btnCloseSettings != null)
            btnCloseSettings.onClick.RemoveListener(CloseSettings);

        if (btnLogout != null)
            btnLogout.onClick.RemoveListener(ShowLogoutConfirmation);

        if (sliderMasterVolume != null)
            sliderMasterVolume.onValueChanged.RemoveListener(OnMasterVolumeChanged);

        if (sliderBGMVolume != null)
            sliderBGMVolume.onValueChanged.RemoveListener(OnBGMVolumeChanged);

        if (sliderSFXVolume != null)
            sliderSFXVolume.onValueChanged.RemoveListener(OnSFXVolumeChanged);

        if (btnConfirmLogout != null)
            btnConfirmLogout.onClick.RemoveListener(ConfirmLogout);

        if (btnCancelLogout != null)
            btnCancelLogout.onClick.RemoveListener(CancelLogout);
    }
}