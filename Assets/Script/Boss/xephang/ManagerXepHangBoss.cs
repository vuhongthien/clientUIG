using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Poki.Assets.Script.Boss.xephang
{
    public class ManagerXepHangBoss : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject panelXepHang;
        public Button btnClose;
        public Button btnNhanQua;

        [Header("Top Players Panel")]
        public Transform panelTop;
        public GameObject topItemPrefab;

        [Header("Your Result Panel")]
        public GameObject panelYourResult;
        public Image imgPet;
        public Image imgBoss;
        public Text txtName;
        public Text txtDame;
        public Text txtYourTop;

        [Header("Boss Info")]
        public Text txtBossName;

        [Header("Notice Panel")]
        public GameObject panelNotice;
        public Button btnGet;
        public Text txtMessage;

        private BossRankingResponseDTO currentRanking;
        private int currentUserId;
        private long currentBossScheduleId;

        void Start()
        {
            currentUserId = PlayerPrefs.GetInt("userId", 0);

            if (btnClose != null)
            {
                btnClose.onClick.AddListener(ClosePanel);
            }

            if (btnNhanQua != null)
            {
                btnNhanQua.onClick.AddListener(ClaimReward);
            }

            if (btnGet != null)
            {
                btnGet.onClick.AddListener(CloseNotice);
            }

            if (panelXepHang != null)
            {
                panelXepHang.SetActive(false);
            }

            if (panelNotice != null)
            {
                panelNotice.SetActive(false);
            }

            if (panelXepHang != null)
            {
                panelXepHang.SetActive(true);
                LoadRankingData();
            }
        }

        public void OpenPanel()
        {
            if (panelXepHang != null)
            {
                panelXepHang.SetActive(true);
                LoadRankingData();
            }
        }

        public void ClosePanel()
        {
            if (panelXepHang != null)
            {
                panelXepHang.SetActive(false);
            }
        }

        void LoadRankingData()
        {
            if (currentUserId == 0)
            {
                Debug.LogError("[ManagerXepHangBoss] User ID not found!");
                ShowNotice("Không tìm thấy thông tin người chơi!");
                return;
            }

            Debug.Log($"[ManagerXepHangBoss] Loading ranking for user {currentUserId}");

            ManagerGame.Instance.ShowLoading();
            StartCoroutine(APIManager.Instance.GetRequest<BossRankingResponseDTO>(
                APIConfig.GET_BOSS_RANKING(currentUserId),
                OnRankingReceived,
                OnError
            ));
        }

        void HideAllPanels()
        {
            HideTopPlayers();
            HideCurrentPlayer();

            if (txtBossName != null)
            {
                txtBossName.text = "Không có dữ liệu";
                txtBossName.gameObject.SetActive(true);
            }
        }

        void OnRankingReceived(BossRankingResponseDTO ranking)
        {
            ManagerGame.Instance.HideLoading();

            if (ranking == null)
            {
                Debug.LogError("[ManagerXepHangBoss] Ranking data is null!");
                ShowNotice("Không có dữ liệu xếp hạng!");
                HideAllPanels();
                return;
            }

            currentRanking = ranking;
            currentBossScheduleId = ranking.bossScheduleId;

            Debug.Log($"[ManagerXepHangBoss] Received ranking for boss: {ranking.bossName}");
            Debug.Log($"[ManagerXepHangBoss] Top players count: {ranking.topPlayers?.Count ?? 0}");
            Debug.Log($"[ManagerXepHangBoss] Current player exists: {ranking.currentPlayer != null}");

            if (txtBossName != null)
            {
                txtBossName.text = ranking.bossName;
                txtBossName.gameObject.SetActive(true);
            }

            if (ranking.topPlayers != null && ranking.topPlayers.Count > 0)
            {
                DisplayTopPlayers(ranking.topPlayers);
            }
            else
            {
                HideTopPlayers();
            }

            if (ranking.currentPlayer != null)
            {
                DisplayCurrentPlayer(ranking.currentPlayer);
            }
            else
            {
                HideCurrentPlayer();
            }
        }

        void HideCurrentPlayer()
        {
            if (panelYourResult != null)
            {
                panelYourResult.SetActive(false);
                Debug.Log("[ManagerXepHangBoss] Current player panel hidden");
            }

            if (btnNhanQua != null)
            {
                btnNhanQua.gameObject.SetActive(false);
            }
        }

        void HideTopPlayers()
        {
            if (panelTop != null)
            {
                foreach (Transform child in panelTop)
                {
                    Destroy(child.gameObject);
                }
            }

            Debug.Log("[ManagerXepHangBoss] Top players panel hidden");
        }

        void DisplayTopPlayers(List<BossRankingPlayerDTO> topPlayers)
        {
            if (panelTop != null)
            {
                foreach (Transform child in panelTop)
                {
                    Destroy(child.gameObject);
                }

                panelTop.gameObject.SetActive(true);
            }

            if (topPlayers == null || topPlayers.Count == 0)
            {
                Debug.Log("[ManagerXepHangBoss] No top players to display");
                HideTopPlayers();
                return;
            }

            foreach (var player in topPlayers)
            {
                if (topItemPrefab != null && panelTop != null)
                {
                    GameObject itemObj = Instantiate(topItemPrefab, panelTop);
                    TopPlayerItem item = itemObj.GetComponent<TopPlayerItem>();

                    if (item != null)
                    {
                        item.SetupTopPlayer(player);
                    }
                    else
                    {
                        SetupTopItemManually(itemObj, player);
                    }
                }
            }

            Debug.Log($"[ManagerXepHangBoss] Displayed {topPlayers.Count} top players");
        }

        void SetupTopItemManually(GameObject itemObj, BossRankingPlayerDTO player)
        {
            Text txtTop = itemObj.transform.Find("txtTop")?.GetComponent<Text>();
            Image imgPet = itemObj.transform.Find("imgPet")?.GetComponent<Image>();
            Image imgBg = itemObj.transform.Find("imgbg")?.GetComponent<Image>();
            Text txtName = itemObj.transform.Find("txtName")?.GetComponent<Text>();
            Text txtDame = itemObj.transform.Find("txtDame")?.GetComponent<Text>();

            if (txtTop != null)
            {
                txtTop.text = "Top "+player.rank.ToString();
            }

            if (txtName != null)
            {
                txtName.text = player.userName;
            }

            if (txtDame != null)
            {
                txtDame.text = player.totalDamage.ToString();
            }

            if (imgPet != null && player.petId > 0)
            {
                LoadPetAvatar(imgPet, player.petId);
            }
        }

        void DisplayCurrentPlayer(BossRankingPlayerDTO currentPlayer)
        {
            if (panelYourResult == null)
            {
                Debug.LogWarning("[ManagerXepHangBoss] PanelYourResult is null!");
                return;
            }

            if (currentPlayer == null)
            {
                Debug.Log("[ManagerXepHangBoss] Current player is null");
                HideCurrentPlayer();
                return;
            }

            panelYourResult.SetActive(true);

            if (txtName != null)
            {
                txtName.text = currentPlayer.userName;
                txtName.gameObject.SetActive(true);
            }

            if (txtDame != null)
            {
                txtDame.text = currentPlayer.totalDamage > 0 ? currentPlayer.totalDamage.ToString() : "0";
                txtDame.gameObject.SetActive(true);
            }

            if (txtYourTop != null)
            {
                txtYourTop.text = currentPlayer.rank > 0 ? "Top: "+currentPlayer.rank.ToString() : "--";
                txtYourTop.gameObject.SetActive(true);
            }

            if (imgPet != null)
            {
                if (currentPlayer.petId > 0)
                {
                    LoadPetAvatar(imgPet, currentPlayer.petId);
                    imgPet.gameObject.SetActive(true);
                }
                else
                {
                    imgPet.gameObject.SetActive(false);
                }
            }

            if (imgBoss != null)
            {
                if (currentPlayer.bossId > 0)
                {
                    LoadBossAvatar(imgBoss, currentPlayer.bossId);
                    imgBoss.gameObject.SetActive(true);
                }
                else
                {
                    imgBoss.gameObject.SetActive(false);
                }
            }

            if (btnNhanQua != null)
            {
                bool showButton = currentPlayer.canClaimReward && !currentPlayer.rewardClaimed;
                btnNhanQua.gameObject.SetActive(showButton);

                Text btnText = btnNhanQua.GetComponentInChildren<Text>();
                if (btnText != null)
                {
                    btnText.text = currentPlayer.rewardClaimed ? "Đã nhận quà" : "Nhận quà";
                }
            }

            Debug.Log($"[ManagerXepHangBoss] Current player displayed - name: {currentPlayer.userName}, rank: {currentPlayer.rank}, damage: {currentPlayer.totalDamage}, bossId: {currentPlayer.bossId}");
        }

        void ClaimReward()
        {
            if (currentRanking == null || currentRanking.currentPlayer == null)
            {
                Debug.LogWarning("[ManagerXepHangBoss] No ranking data to claim reward");
                ShowNotice("Không có dữ liệu để nhận quà!");
                return;
            }

            if (!currentRanking.currentPlayer.canClaimReward)
            {
                ShowNotice("Bạn không đủ điều kiện nhận quà!");
                return;
            }

            if (currentRanking.currentPlayer.rewardClaimed)
            {
                ShowNotice("Bạn đã nhận quà rồi!");
                return;
            }

            Debug.Log($"[ManagerXepHangBoss] Claiming reward for boss schedule: {currentBossScheduleId}");

            StartCoroutine(APIManager.Instance.PostRequest<ClaimRewardResponseDTO>(
                APIConfig.CLAIM_BOSS_REWARD(currentUserId, (int)currentBossScheduleId),
                null,
                OnRewardClaimed,
                OnError
            ));
        }

        void OnRewardClaimed(ClaimRewardResponseDTO response)
        {

            if (response == null)
            {
                Debug.LogError("[ManagerXepHangBoss] Response is null!");
                ShowNotice("Có lỗi xảy ra!");
                return;
            }

            Debug.Log($"[ManagerXepHangBoss] Claim reward response: success={response.success}, message={response.message}");

            if (response.success)
            {
                // Cập nhật UI ngay lập tức
                if (currentRanking != null && currentRanking.currentPlayer != null)
                {
                    currentRanking.currentPlayer.rewardClaimed = true;
                    DisplayCurrentPlayer(currentRanking.currentPlayer);
                }

                if (btnNhanQua != null)
                {
                    btnNhanQua.gameObject.SetActive(false);
                }

                // Hiển thị notice
                ShowNotice(response.message);

                // TỰ REFRESH USER INFO NGAY TRONG CLASS NÀY
                StartCoroutine(RefreshUserInfoSilently());
            }
            else
            {
                ShowNotice(response.message);
            }
        }

        // ===== TỰ REFRESH USER INFO =====
        IEnumerator RefreshUserInfoSilently()
        {
            // Đợi 1 frame để UI render xong
            yield return null;

            Debug.Log("[ManagerXepHangBoss] Refreshing user info silently...");

            yield return APIManager.Instance.GetRequest<UserDTO>(
                APIConfig.GET_USER(currentUserId),
                OnUserInfoRefreshed,
                OnRefreshError
            );
        }

        void OnUserInfoRefreshed(UserDTO user)
        {
            Debug.Log("[ManagerXepHangBoss] User info refreshed successfully");

            // Cập nhật UI của ManagerQuangTruong nếu tồn tại
            if (ManagerQuangTruong.Instance != null)
            {
                ManagerQuangTruong.Instance.txtNl.text = user.energy + "/" + user.energyFull;
                ManagerQuangTruong.Instance.txtVang.text = user.gold.ToString();
                ManagerQuangTruong.Instance.txtCt.text = user.requestAttack.ToString();
                ManagerQuangTruong.Instance.UpdateWheelFlag(user.wheel);
                ManagerQuangTruong.Instance.UpdateStarUI(user.starWhite, user.starBlue, user.starRed);
            }
            else
            {
                Debug.LogWarning("[ManagerXepHangBoss] ManagerQuangTruong.Instance is null - cannot update UI");
            }
        }

        void OnRefreshError(string error)
        {
            Debug.LogError($"[ManagerXepHangBoss] Refresh Error: {error}");
            // Không hiện thông báo lỗi để tránh làm phiền user
        }

        void LoadPetAvatar(Image imgPet, long petId)
        {
            if (imgPet == null)
            {
                Debug.LogWarning("[ManagerXepHangBoss] imgPet is null!");
                return;
            }

            string spritePath = $"Image/IconsPet/{petId}";
            Sprite petSprite = Resources.Load<Sprite>(spritePath);

            if (petSprite != null)
            {
                imgPet.sprite = petSprite;
                Debug.Log($"[ManagerXepHangBoss] Loaded pet avatar: {spritePath}");
            }
            else
            {
                Debug.LogWarning($"[ManagerXepHangBoss] Cannot find pet sprite at: {spritePath}");
                Sprite defaultSprite = Resources.Load<Sprite>("Image/IconsPet/default");
                if (defaultSprite != null)
                {
                    imgPet.sprite = defaultSprite;
                }
            }
        }

        void LoadBossAvatar(Image imgBoss, long bossId)
        {
            if (imgBoss == null)
            {
                Debug.LogWarning("[ManagerXepHangBoss] imgBoss is null!");
                return;
            }

            string spritePath = $"Image/IconsPet/{bossId}";
            Sprite bossSprite = Resources.Load<Sprite>(spritePath);

            if (bossSprite != null)
            {
                imgBoss.sprite = bossSprite;
                Debug.Log($"[ManagerXepHangBoss] Loaded boss avatar: {spritePath}");
            }
            else
            {
                Debug.LogWarning($"[ManagerXepHangBoss] Cannot find boss sprite at: {spritePath}");
                Sprite defaultSprite = Resources.Load<Sprite>("Image/IconsPet/default");
                if (defaultSprite != null)
                {
                    imgBoss.sprite = defaultSprite;
                }
            }
        }

        void ShowNotice(string message)
        {
            if (panelNotice == null)
            {
                Debug.LogWarning("[ManagerXepHangBoss] PanelNotice is null!");
                return;
            }

            if (txtMessage == null)
            {
                txtMessage = panelNotice.transform.Find("message")?.GetComponent<Text>();
            }

            if (txtMessage != null)
            {
                txtMessage.text = message;
            }

            panelNotice.SetActive(true);

            Debug.Log($"[ManagerXepHangBoss] Notice shown: {message}");
        }

        void CloseNotice()
        {
            if (panelNotice != null)
            {
                panelNotice.SetActive(false);
            }
        }

        void OnError(string error)
        {
            ManagerGame.Instance.HideLoading();
            Debug.LogError($"[ManagerXepHangBoss] API Error: {error}");
            ShowNotice("Có lỗi xảy ra, vui lòng thử lại!");
        }
    }

    [Serializable]
    public class BossRankingResponseDTO
    {
        public long bossScheduleId;
        public string bossName;
        public List<BossRankingPlayerDTO> topPlayers;
        public BossRankingPlayerDTO currentPlayer;
    }

    [Serializable]
    public class BossRankingPlayerDTO
    {
        public long userId;
        public string userName;
        public long userPetId;
        public long petId;
        public long bossId;
        public int totalDamage;
        public int rank;
        public bool canClaimReward;
        public bool rewardClaimed;
    }

    [Serializable]
    public class ClaimRewardResponseDTO
    {
        public bool success;
        public string message;
        public RewardDetailDTO reward;
    }

    [Serializable]
    public class RewardDetailDTO
    {
        public int rank;
        public int wheel;
        public int gold;
        public int energy;
    }
}