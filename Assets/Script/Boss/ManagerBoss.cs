using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ManagerBoss : MonoBehaviour
{
    [Header("UI References")]
    public Button btnClose;
    public GameObject panelBoss;
    public GameObject panelBossTG; // Panel chứa Boss1-6
    public GameObject panelNotice; // Panel thông báo chung cho tất cả boss
    public Button btnXepHang;
    public GameObject panelXepHang;


    [Header("Status Display")]
    public Text txtStatusOutside; // Text status ở button bên ngoài
    public GameObject statusObject; // Object chứa text status (để show/hide)
    public GameObject anmtObject; // Animation object (để show/hide)

    private List<WorldBossDTO> bossList = new List<WorldBossDTO>();
    private List<BossItem> bossItems = new List<BossItem>();


    void Start()
    {
        Debug.Log("[ManagerBoss] Start called");

        // ✅ LƯU SCENE HIỆN TẠI VÀO PLAYERPREFS
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        PlayerPrefs.SetString("PreviousScene", currentScene);
        PlayerPrefs.Save();

        Debug.Log($"[ManagerBoss] Current scene saved: {currentScene}");

        if (btnClose != null)
        {
            btnClose.onClick.AddListener(ClosePanel);
        }

        // ← Thêm phần này
        if (btnXepHang != null)
        {
            btnXepHang.onClick.AddListener(OpenPanelXepHang);
        }

        // Ẩn panel xếp hạng ban đầu
        if (panelXepHang != null)
        {
            panelXepHang.SetActive(false);
        }

        FindExistingBossItems();
        LoadBossList();
        StartCoroutine(UpdateCountdownLoop());
    }

    public void OpenPanelXepHang()
    {
        if (panelXepHang != null)
        {
            panelXepHang.SetActive(true);
            Debug.Log("[ManagerBoss] Panel xếp hạng opened");
        }
        else
        {
            Debug.LogWarning("[ManagerBoss] panelXepHang is NULL!");
        }
    }

    void FindExistingBossItems()
    {
        bossItems.Clear();

        if (panelBossTG == null)
        {
            Debug.LogError("[ManagerBoss] panelBossTG is NULL! Cannot find boss items.");
            return;
        }

        Debug.Log("[ManagerBoss] Finding existing boss items...");

        // Tìm tất cả các GameObject Boss1, Boss2, ... Boss6 (hoặc Boss7 nếu có)
        for (int i = 1; i <= 7; i++)
        {
            string bossName = "Boss" + i;
            Transform bossTransform = panelBossTG.transform.Find(bossName);

            if (bossTransform != null)
            {
                Debug.Log($"[ManagerBoss] Found {bossName} transform");

                // LẤY component BossItem có sẵn (KHÔNG tạo mới!)
                BossItem bossItem = bossTransform.GetComponent<BossItem>();

                if (bossItem != null)
                {
                    // GÁN PANELNOTICE CHO BOSSITEM
                    if (panelNotice != null)
                    {
                        bossItem.panelNotice = panelNotice;
                        Debug.Log($"[ManagerBoss] ✓ Assigned PanelNotice to {bossName}");
                    }
                    else
                    {
                        Debug.LogWarning("[ManagerBoss] PanelNotice is NULL! Please assign it in Inspector.");
                    }

                    bossItems.Add(bossItem);
                    Debug.Log($"[ManagerBoss] ✓ Added existing BossItem from {bossName}");
                }
                else
                {
                    Debug.LogWarning($"[ManagerBoss] ✗ {bossName} doesn't have BossItem component! Please add it in Inspector.");

                    // CHỈ thêm component nếu THẬT SỰ không có
                    // Nhưng cách này không tốt vì sẽ thiếu references
                    bossItem = bossTransform.gameObject.AddComponent<BossItem>();

                    // GÁN PANELNOTICE
                    if (panelNotice != null)
                    {
                        bossItem.panelNotice = panelNotice;
                    }

                    bossItems.Add(bossItem);

                    Debug.LogWarning($"[ManagerBoss] Added BossItem component to {bossName} at runtime (not recommended)");
                }
            }
            else
            {
                Debug.LogWarning($"[ManagerBoss] Cannot find {bossName} under panelBossTG");
            }
        }

        Debug.Log($"[ManagerBoss] Total boss items found: {bossItems.Count}");
    }

    void LoadBossList()
    {
        int userId = PlayerPrefs.GetInt("userId", 0);

        if (userId == 0)
        {
            Debug.LogError("[ManagerBoss] User ID not found!");
            return;
        }

        Debug.Log($"[ManagerBoss] Loading boss list for user {userId}");

        ManagerGame.Instance.ShowLoading();
        StartCoroutine(APIManager.Instance.GetRequest<List<WorldBossDTO>>(
            APIConfig.GET_ALL_WORLD_BOSSES(userId),
            OnBossListReceived,
            OnError
        ));
    }

    void OnBossListReceived(List<WorldBossDTO> bosses)
    {
        ManagerGame.Instance.HideLoading();

        if (bosses == null || bosses.Count == 0)
        {
            Debug.Log("[ManagerBoss] No bosses available from API");
            UpdateOutsideStatus(null);
            return;
        }

        bossList = bosses;
        Debug.Log($"[ManagerBoss] Loaded {bossList.Count} bosses from API");

        DisplayBosses();
        UpdateOutsideStatus(bosses);
    }

    void DisplayBosses()
    {
        Debug.Log($"[ManagerBoss] DisplayBosses: {bossItems.Count} items, {bossList.Count} boss data");

        // Gán data cho từng boss item có sẵn (không sort vì API đã sort)
        for (int i = 0; i < bossItems.Count && i < bossList.Count; i++)
        {
            if (bossItems[i] != null)
            {
                Debug.Log($"[ManagerBoss] Setting up boss item {i}: {bossList[i].bossName}");

                bossItems[i].gameObject.SetActive(true);
                bossItems[i].SetupBoss(bossList[i]);
            }
            else
            {
                Debug.LogWarning($"[ManagerBoss] Boss item {i} is NULL!");
            }
        }

        // Ẩn các boss items thừa (nếu số boss < số items)
        for (int i = bossList.Count; i < bossItems.Count; i++)
        {
            if (bossItems[i] != null)
            {
                Debug.Log($"[ManagerBoss] Hiding unused boss item {i}");
                bossItems[i].gameObject.SetActive(false);
            }
        }
    }

    IEnumerator UpdateCountdownLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            foreach (var bossItem in bossItems)
            {
                if (bossItem != null && bossItem.gameObject.activeSelf)
                {
                    bossItem.UpdateCountdown();
                }
            }

            // Cập nhật status bên ngoài
            UpdateOutsideStatus(bossList);
        }
    }

    void UpdateOutsideStatus(List<WorldBossDTO> bosses)
    {
        if (bosses == null || bosses.Count == 0)
        {
            // Không có boss nào -> ẩn status và animation
            HideStatusAndAnimation();
            return;
        }

        // Tìm boss đang ACTIVE
        WorldBossDTO activeBoss = null;
        DateTime now = DateTime.Now;

        foreach (var boss in bosses)
        {
            try
            {
                DateTime startTime = DateTime.Parse(boss.startTime);
                DateTime endTime = DateTime.Parse(boss.endTime);

                if (now >= startTime && now <= endTime)
                {
                    activeBoss = boss;
                    break; // Tìm thấy boss đang diễn ra
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ManagerBoss] Error parsing time for boss {boss.bossName}: {e.Message}");
            }
        }

        // Cập nhật UI
        if (activeBoss != null)
        {
            // Có boss đang diễn ra -> hiện status và animation
            ShowStatusAndAnimation();

            if (txtStatusOutside != null)
            {
                txtStatusOutside.text = "đang diễn ra!";
            }

            Debug.Log($"[ManagerBoss] Active boss: {activeBoss.bossName}");
        }
        else
        {
            // Không có boss nào đang diễn ra -> ẩn status và animation
            HideStatusAndAnimation();

            Debug.Log("[ManagerBoss] No active boss");
        }
    }

    void ShowStatusAndAnimation()
    {
        if (statusObject != null && !statusObject.activeSelf)
        {
            statusObject.SetActive(true);
            Debug.Log("[ManagerBoss] Status object shown");
        }

        if (anmtObject != null && !anmtObject.activeSelf)
        {
            anmtObject.SetActive(true);
            Debug.Log("[ManagerBoss] Animation object shown");
        }
    }

    void HideStatusAndAnimation()
    {
        if (statusObject != null && statusObject.activeSelf)
        {
            statusObject.SetActive(false);
            Debug.Log("[ManagerBoss] Status object hidden");
        }

        if (anmtObject != null && anmtObject.activeSelf)
        {
            anmtObject.SetActive(false);
            Debug.Log("[ManagerBoss] Animation object hidden");
        }
    }

    public void ClosePanel()
    {
        if (panelBoss != null)
        {
            panelBoss.SetActive(false);
        }
    }

    public void RefreshBossList()
    {
        LoadBossList();
    }

    void OnError(string error)
    {
        ManagerGame.Instance.HideLoading();
        Debug.LogError("[ManagerBoss] Boss API Error: " + error);
        UpdateOutsideStatus(null);
    }

    void OnDestroy()
    {
        StopAllCoroutines();
    }
}