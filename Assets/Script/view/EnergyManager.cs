using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnergyManager : MonoBehaviour
{
    public static EnergyManager Instance { get; private set; }

    [Header("UI References - Updated per scene")]
    private Text txtEnergy;
    private Text txtCountdown;
    private Image imgEnergyBar;

    [Header("Server Data")]
    private int currentEnergy;
    private int maxEnergy;
    private DateTime nextRegenTime;
    private const float REGEN_INTERVAL_MINUTES = 8f;

    [Header("Client-side State")]
    private bool isRegenerating = false;
    private Coroutine regenCoroutine;
    private DateTime lastServerSync;

    // ✅ THROTTLE SETTINGS - Ngăn spam API
    private const float MIN_SYNC_INTERVAL_SECONDS = 5f; // Tối thiểu 5 giây giữa các lần gọi API
    private const float AUTO_SYNC_INTERVAL_SECONDS = 60f; // Tự động sync mỗi 60 giây (nếu cần)
    private bool isSyncing = false; // Flag để tránh gọi API đồng thời

    // ✅ SMART SYNC - Chỉ sync khi cần thiết
    private bool needsServerSync = false; // Đánh dấu cần sync với server
    private Coroutine autoSyncCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Khởi động auto-sync timer
        StartAutoSyncTimer();
    }

    // ════════════════════════════════════════════════════════
    // UI REGISTRATION
    // ════════════════════════════════════════════════════════

    public void RegisterUI(Text energyText, Text countdownText, Image energyBar)
    {
        txtEnergy = energyText;
        txtCountdown = countdownText;
        imgEnergyBar = energyBar;

        UpdateUI();

        // ✅ CHỈ sync nếu chưa có data hoặc đã lâu không sync
        if (currentEnergy == 0 || (DateTime.Now - lastServerSync).TotalSeconds > AUTO_SYNC_INTERVAL_SECONDS)
        {
            RefreshEnergyFromServer();
        }
    }

    public void UnregisterUI()
    {
        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
            regenCoroutine = null;
        }

        txtEnergy = null;
        txtCountdown = null;
        imgEnergyBar = null;

    }

    // ════════════════════════════════════════════════════════
    // ✅ AUTO SYNC TIMER - Chỉ sync định kỳ khi cần
    // ════════════════════════════════════════════════════════

    private void StartAutoSyncTimer()
    {
        if (autoSyncCoroutine != null)
        {
            StopCoroutine(autoSyncCoroutine);
        }

        autoSyncCoroutine = StartCoroutine(AutoSyncLoop());
    }

    private IEnumerator AutoSyncLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(AUTO_SYNC_INTERVAL_SECONDS);

            // THÊM CHECK NÀY:
            if (currentEnergy > maxEnergy)
            {
                needsServerSync = false;
                continue; // Bỏ qua sync
            }
            if ((currentEnergy < maxEnergy || needsServerSync) && !isSyncing)
            {
                RefreshEnergyFromServer();
                needsServerSync = false;
            }
        }
    }

    // ════════════════════════════════════════════════════════
    // ✅ SERVER SYNC - THROTTLED
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// ✅ GỌI API với throttle - Tránh spam
    /// </summary>
    public void RefreshEnergyFromServer()
    {
        // ✅ THROTTLE 1: Đang sync thì bỏ qua
        if (isSyncing)
        {
            return;
        }

        // ✅ THROTTLE 2: Sync quá gần thì bỏ qua
        float timeSinceLastSync = (float)(DateTime.Now - lastServerSync).TotalSeconds;
        if (timeSinceLastSync < MIN_SYNC_INTERVAL_SECONDS)
        {
            return;
        }

        StartCoroutine(RefreshEnergyCoroutine());
    }

    /// <summary>
    /// ✅ FORCE sync - Bỏ qua throttle (dùng khi thực sự cần thiết)
    /// </summary>
    public void ForceRefreshEnergyFromServer()
    {
        if (isSyncing)
        {
            return;
        }

        StartCoroutine(RefreshEnergyCoroutine());
    }

    private IEnumerator RefreshEnergyCoroutine()
    {
        isSyncing = true; // ✅ Đánh dấu đang sync

        int userId = PlayerPrefs.GetInt("userId", 0);
        if (userId == 0)
        {
            isSyncing = false;
            yield break;
        }

        string url = APIConfig.GET_ENERGY(userId);

        yield return APIManager.Instance.GetRequest<EnergyInfoDTO>(
            url,
            OnEnergyReceivedFromServer,
            (error) =>
            {
                OnEnergyError(error);
                isSyncing = false; // ✅ Reset flag khi lỗi
            }
        );

        isSyncing = false; // ✅ Reset flag khi xong
    }

    private void OnEnergyReceivedFromServer(EnergyInfoDTO data)
    {
        currentEnergy = data.currentEnergy;
        maxEnergy = data.maxEnergy;
        lastServerSync = DateTime.Now;


        // Tính thời điểm hồi năng lượng tiếp theo
        if (currentEnergy < maxEnergy && data.secondsUntilNextRegen > 0)
        {
            nextRegenTime = DateTime.Now.AddSeconds(data.secondsUntilNextRegen);
            StartClientSideRegeneration();
        }
        else if (currentEnergy >= maxEnergy)
        {
            StopClientSideRegeneration();
        }

        UpdateUI();
    }

    private void OnEnergyError(string error)
    {
        Debug.LogError($"[EnergyManager] ❌ API Error: {error}");
    }

    // ════════════════════════════════════════════════════════
    // CLIENT-SIDE REGENERATION - KHÔNG GỌI API
    // ════════════════════════════════════════════════════════

    private void StartClientSideRegeneration()
    {
        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
        }

        isRegenerating = true;
        regenCoroutine = StartCoroutine(ClientSideRegenLoop());

    }

    private void StopClientSideRegeneration()
    {
        isRegenerating = false;

        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
            regenCoroutine = null;
        }

    }

    private IEnumerator ClientSideRegenLoop()
    {
        while (isRegenerating)
        {
            DateTime now = DateTime.Now;

            // Kiểm tra xem đã đến thời điểm hồi năng lượng chưa
            if (now >= nextRegenTime && currentEnergy < maxEnergy)
            {
                // Hồi 1 năng lượng
                currentEnergy++;

                // ✅ Đánh dấu cần sync với server (để confirm)
                needsServerSync = true;

                // Tính thời điểm hồi tiếp theo
                if (currentEnergy < maxEnergy)
                {
                    nextRegenTime = now.AddMinutes(REGEN_INTERVAL_MINUTES);
                }
                else
                {
                    // Đã full năng lượng
                    StopClientSideRegeneration();
                }

                UpdateUI();
            }

            UpdateCountdownUI();
            yield return new WaitForSeconds(1f);
        }
    }

    // ════════════════════════════════════════════════════════
    // UI UPDATE
    // ════════════════════════════════════════════════════════

    private void UpdateUI()
    {
        if (txtEnergy != null)
        {
            txtEnergy.text = $"{currentEnergy}/{maxEnergy}";
        }

        if (imgEnergyBar != null)
        {
            imgEnergyBar.fillAmount = maxEnergy > 0 ? (float)currentEnergy / maxEnergy : 0f;
        }

        UpdateCountdownUI();
    }

    private void UpdateCountdownUI()
    {
        if (txtCountdown == null) return;

        if (currentEnergy >= maxEnergy)
        {
            txtCountdown.text = "FULL";
            return;
        }

        TimeSpan remaining = nextRegenTime - DateTime.Now;

        if (remaining.TotalSeconds < 0)
        {
            remaining = TimeSpan.Zero;
        }

        int minutes = (int)remaining.TotalMinutes;
        int seconds = remaining.Seconds;

        txtCountdown.text = $"{minutes:D2}:{seconds:D2}";
    }

    // ════════════════════════════════════════════════════════
    // ✅ CONSUME ENERGY - OPTIMISTIC UPDATE + BACKGROUND SYNC
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// ✅ Tiêu năng lượng - Optimistic update (trừ ngay) + background sync
    /// </summary>
    public bool ConsumeEnergy(int amount, Action onSuccess = null, Action onFailed = null)
    {
        if (currentEnergy < amount)
        {
            onFailed?.Invoke();
            return false;
        }

        // ✅ OPTIMISTIC UPDATE: Trừ ngay ở client
        currentEnergy -= amount;

        UpdateUI();

        // Bắt đầu regeneration nếu chưa full
        if (!isRegenerating && currentEnergy < maxEnergy)
        {
            nextRegenTime = DateTime.Now.AddMinutes(REGEN_INTERVAL_MINUTES);
            StartClientSideRegeneration();
        }

        // ✅ BACKGROUND SYNC: Không block UI
        StartCoroutine(SyncConsumeEnergyWithServer(amount, onSuccess, onFailed));

        return true;
    }

    private IEnumerator SyncConsumeEnergyWithServer(int amount, Action onSuccess, Action onFailed)
    {
        int userId = PlayerPrefs.GetInt("userId", 0);
        string url = APIConfig.CONSUME_ENERGY(userId, amount);


        yield return APIManager.Instance.PostRequest_Generic<ConsumeEnergyResponse>(
            url,
            null,
            (response) =>
            {
                lastServerSync = DateTime.Now;
                onSuccess?.Invoke();
            },
            (error) =>
            {

                // ✅ ROLLBACK nếu server reject
                currentEnergy += amount;
                UpdateUI();
                onFailed?.Invoke();
            }
        );
    }

    // ════════════════════════════════════════════════════════
    // ✅ APP LIFECYCLE - CHỈ SYNC KHI CẦN
    // ════════════════════════════════════════════════════════

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            if (currentEnergy > maxEnergy)
            {
                return;
            }
            // ✅ CHỈ sync nếu đã lâu không sync (> 30s)
            float timeSinceLastSync = (float)(DateTime.Now - lastServerSync).TotalSeconds;
            if (timeSinceLastSync > 30f)
            {
                RefreshEnergyFromServer();
            }
            else
            {
            }
        }
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (!isPaused)
        {
            if (currentEnergy > maxEnergy)
            {
                return;
            }
            // ✅ CHỈ sync nếu đã lâu không sync (> 30s)
            float timeSinceLastSync = (float)(DateTime.Now - lastServerSync).TotalSeconds;
            if (timeSinceLastSync > 30f)
            {
                RefreshEnergyFromServer();
            }
            else
            {
            }
        }
    }

    // ════════════════════════════════════════════════════════
    // PUBLIC GETTERS
    // ════════════════════════════════════════════════════════

    public int GetCurrentEnergy() => currentEnergy;
    public int GetMaxEnergy() => maxEnergy;
    public bool IsRegenerating() => isRegenerating;
    public bool IsSyncing() => isSyncing; // ✅ Kiểm tra đang sync hay không

    public TimeSpan GetTimeUntilNextRegen()
    {
        if (currentEnergy >= maxEnergy)
            return TimeSpan.Zero;

        return nextRegenTime - DateTime.Now;
    }

    // ✅ Lấy thời gian sync cuối
    public DateTime GetLastServerSync() => lastServerSync;

    // ✅ Kiểm tra có cần sync không
    public bool NeedsServerSync() => needsServerSync;

    // ════════════════════════════════════════════════════════
    // CLEANUP
    // ════════════════════════════════════════════════════════

    private void OnDestroy()
    {
        StopClientSideRegeneration();

        if (autoSyncCoroutine != null)
        {
            StopCoroutine(autoSyncCoroutine);
            autoSyncCoroutine = null;
        }

        if (gameObject != null)
        {
            LeanTween.cancel(gameObject);
        }
    }
}