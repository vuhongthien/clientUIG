using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;
using UnityEngine.Networking;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class ActivePVP : MonoBehaviourPunCallbacks, IOnEventCallback
{
    [Header("UI Object")]
    public BoardPVP board;
    public Slider thanhMauSlider;
    public Slider thanhManaSlider;
    public Slider thanhNoSlider;
    public Slider thanhMauNPC;
    public Slider thanhManaNPC;
    public Slider thanhNoNPC;
    public ImageLoader enemyPet;
    public ImageLoader playerPet;
    public ImageLoader typePetUser;
    public ImageLoader typePetEnemy;
    public event Action OnTurnEnd;
    public Animator playerPetAnimator;
    public Animator bossPetAnimator;
    public GameObject offBoard;
    public GameObject dameATKPrefad;
    public GameObject dameATKPrefadNPC;

    [Header("UI Information")]
    public int inputKiem;
    public int outputKiem;
    public int inputGiap;
    public int outputGiap;
    public int inputMau;
    public int outputMau;
    public int maxMau;
    public int inputHut;
    public int outputHut;
    public int inputNo;
    public int outputNo;
    public int maxNo;
    public int inputMana;
    public int outputMana;
    public int maxMana;
    public int MauBD;
    public int ManaBD;
    public int NoBD;
    private int sloMauAnDuoc;
    private int sloNoAnDuoc;
    private int sloHutAnDuoc;
    private int sloGiapAnDuoc;
    private int sloManaAnDuoc;
    private int sloKiemAnDuoc;
    public int MauPlayer;
    public int ManaPlayer;
    public int NoPlayer;
    public int maxMauNPC;
    public int maxManaNPC;
    public int maxNoNPC;
    public int MauBDNPC;
    public int ManaBDNPC;
    public int NoBDNPC;
    public int MauNPC;
    public int ManaNPC;
    public int NoNPC;
    public int thoigiandem;
    private bool isCountdownRunning = false;
    public int turn;
    public int useId;

    public int valueCurrent = 0;
    public Text nangLuong;
    public Text leverPetUser;
    public Text leverEnemyPet;
    public Text namePetUser;
    public Text namePetEnemy;
    public Text dameTypePetUse;
    public Text dameTypePetEnemy;
    public Text textMauPlayer;
    public Text textManaPlayer;
    public Text textNoPlayer;
    public Text textMauNPC;
    public Text textManaNPC;
    public Text textNoNPC;
    public Text countdownText;
    public GameObject healdMana;
    public GameObject healdDEF;
    public GameObject healdDEFNPC;
    public GameObject healdPower;
    public GameObject healdHP;
    public GameObject healdManaNPC;
    public GameObject healdPowerNPC;
    public GameObject healdHPNPC;
    public GameObject animationPet;
    public GameObject animationBoss;
    private Coroutine countdownCoroutine;

    public List<CardInfo> cardInfos = new List<CardInfo>();
    public ListCard listCard;
    public Effect effect;
    public ManagerMatchPVP api;
    public ApiLoadRoom apiLoadRoom;
    public GameObject onCard;
    public static bool isTimeOver = false;

    [Header("Turn Timer")]
    public int turnDuration = 14;

    public int attackP;
    public int attackE;

    public int GiapPlayer = 0;
    public int GiapNPC = 0;

    // Custom event codes
    private const byte STATS_UPDATE_EVENT = 1;
    private const byte ANIMATION_EVENT = 2;

    // ==================== ANIMATION CONSTANTS ====================
    private const float ATTACK_HIT_DELAY = 0.3f;        // Delay từ attack đến hit
    private const float ANIMATION_DURATION = 0.8f;      // Thời gian animation chạy
    private const float RESET_DELAY = 1.5f;             // Delay trước khi reset về idle
    private const float COMBO_INTERVAL = 0.5f;          // Khoảng cách giữa các hit
    private const float SKILL_CAST_DELAY = 0.4f;        // Delay khi cast skill
    private const float BUFF_DURATION = 1.0f;           // Thời gian hiệu ứng buff

    // ==================== COUNTDOWN SYNC VARIABLES ====================
    private double countdownStartTime = 0;
    private int countdownDuration = 0;
    private int countdownTurn = 0;

    void Start()
    {
        apiLoadRoom = FindFirstObjectByType<ApiLoadRoom>();
        string response = PlayerPrefs.GetString("startG");
        api = FindFirstObjectByType<ManagerMatchPVP>();
        effect = FindFirstObjectByType<Effect>();
        thoigiandem = turnDuration;

        // Đặt onCard ở trạng thái ẩn ban đầu
        if (onCard != null)
        {
            onCard.SetActive(false);
        }
        else
        {
            Debug.LogError("onCard GameObject chưa được gán trong Active.");
        }

        // Khởi tạo chỉ số
        inputGiap = 20;
        inputHut = 20;
        inputKiem = 100;
        inputMau = 150;
        inputNo = 20;
        inputMana = 20;

        // Tải dữ liệu pet
        int petUser = PlayerPrefs.GetInt("petUser");
        int petEnemy = PlayerPrefs.GetInt("petEnemy");

        api.LoadInfoMatch();

        // Đồng bộ hóa ListCard
        SyncListCardInfos();

        // Khởi tạo Custom Properties
        InitializePlayerStats();
        if (PhotonNetwork.IsMasterClient)
        {
            SyncInitialStatsForAll();
        }

        // Khởi tạo countdown text
        if (countdownText != null)
        {
            countdownText.text = turnDuration.ToString();
            countdownText.color = Color.white;
        }

        Debug.Log("[ACTIVE PVP] Khởi tạo countdown system thành công");
    }

    void OnEnable()
    {
        // Đăng ký Event callback
        PhotonNetwork.AddCallbackTarget(this);
    }

    void OnDisable()
    {
        // Hủy đăng ký Event callback
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    // ==================== CUSTOM PROPERTIES SYNC ====================
    private void InitializePlayerStats()
    {
        Hashtable props = new Hashtable
        {
            { "Mau", MauPlayer },
            { "Mana", ManaPlayer },
            { "No", NoPlayer },
            { "MaxMau", maxMau },
            { "MaxMana", maxMana },
            { "MaxNo", maxNo },
            { "Giap", GiapPlayer }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        Debug.Log($"[INIT] Player {PhotonNetwork.LocalPlayer.ActorNumber} khởi tạo stats: Máu={MauPlayer}, Mana={ManaPlayer}, Nộ={NoPlayer}");
    }

    public void SyncInitialStatsForAll()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Gửi chỉ số khởi tạo của MasterClient cho tất cả client
        Hashtable masterProps = new Hashtable
        {
            { "Mau", MauPlayer },
            { "Mana", ManaPlayer },
            { "No", NoPlayer },
            { "MaxMau", maxMau },
            { "MaxMana", maxMana },
            { "MaxNo", maxNo },
            { "Giap", GiapPlayer }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(masterProps);

        Debug.Log("[SYNC INIT] MasterClient đồng bộ chỉ số khởi tạo cho tất cả Client.");

        // Nếu có 2 người chơi, đợi một chút cho client vào phòng rồi yêu cầu nó gửi lại stats
        StartCoroutine(RequestClientStats());
    }

    private IEnumerator RequestClientStats()
    {
        yield return new WaitForSeconds(1f); // chờ client join đủ
        photonView.RPC("RequestClientStatsRPC", RpcTarget.Others);
    }

    [PunRPC]
    private void RequestClientStatsRPC()
    {
        // Client gửi lại Custom Properties của mình cho Master
        InitializePlayerStats();
        Debug.Log("[SYNC INIT] Client gửi chỉ số khởi tạo lại cho Master.");
    }

    public void UpdatePlayerStats()
    {
        Hashtable props = new Hashtable
        {
            { "Mau", MauPlayer },
            { "Mana", ManaPlayer },
            { "No", NoPlayer },
            { "Giap", GiapPlayer }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        Debug.Log($"[UPDATE] Player {PhotonNetwork.LocalPlayer.ActorNumber} cập nhật stats: Máu={MauPlayer}, Mana={ManaPlayer}, Nộ={NoPlayer}, Giáp={GiapPlayer}");
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        // Nếu là chính mình thì không cần xử lý
        if (targetPlayer == PhotonNetwork.LocalPlayer)
        {
            return;
        }

        Debug.Log($"[RECEIVE] Nhận cập nhật từ Player {targetPlayer.ActorNumber}");

        bool hasChanges = false;

        // Cập nhật stats của đối thủ (NPC trong góc nhìn local player)
        if (changedProps.ContainsKey("Mau"))
        {
            MauNPC = (int)changedProps["Mau"];
            Debug.Log($"  - Máu NPC: {MauNPC}");
            hasChanges = true;
        }
        if (changedProps.ContainsKey("Mana"))
        {
            ManaNPC = (int)changedProps["Mana"];
            Debug.Log($"  - Mana NPC: {ManaNPC}");
            hasChanges = true;
        }
        if (changedProps.ContainsKey("No"))
        {
            NoNPC = (int)changedProps["No"];
            Debug.Log($"  - Nộ NPC: {NoNPC}");
            hasChanges = true;
        }
        if (changedProps.ContainsKey("MaxMau"))
        {
            maxMauNPC = (int)changedProps["MaxMau"];
            Debug.Log($"  - MaxMáu NPC: {maxMauNPC}");
            hasChanges = true;
        }
        if (changedProps.ContainsKey("MaxMana"))
        {
            maxManaNPC = (int)changedProps["MaxMana"];
            Debug.Log($"  - MaxMana NPC: {maxManaNPC}");
            hasChanges = true;
        }
        if (changedProps.ContainsKey("MaxNo"))
        {
            maxNoNPC = (int)changedProps["MaxNo"];
            Debug.Log($"  - MaxNộ NPC: {maxNoNPC}");
            hasChanges = true;
        }
        if (changedProps.ContainsKey("Giap"))
        {
            GiapNPC = (int)changedProps["Giap"];
            Debug.Log($"  - Giáp NPC: {GiapNPC}");
            hasChanges = true;
        }

        // Chỉ update slider khi có thay đổi
        if (hasChanges)
        {
            UpdateSlider();
        }
    }

    // ==================== PHOTON EVENT ====================
    public void OnEvent(EventData photonEvent)
    {
        byte eventCode = photonEvent.Code;

        if (eventCode == STATS_UPDATE_EVENT)
        {
            object[] data = (object[])photonEvent.CustomData;
            int senderActorNumber = photonEvent.Sender;

            if (senderActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
                return;

            MauNPC = (int)data[0];
            ManaNPC = (int)data[1];
            NoNPC = (int)data[2];
            GiapNPC = (int)data[3];

            Debug.Log($"[EVENT] Nhận stats từ Player {senderActorNumber}: Máu={MauNPC}, Mana={ManaNPC}, Nộ={NoNPC}, Giáp={GiapNPC}");
            UpdateSlider();
        }

        if (eventCode == ANIMATION_EVENT)
        {
            object[] data = (object[])photonEvent.CustomData;
            int animKey = (int)data[0];
            bool isPlayer = (bool)data[1];

            if (photonEvent.Sender == PhotonNetwork.LocalPlayer.ActorNumber)
                return;

            // Remote player action = local enemy pet animation
            if (isPlayer)
            {
                bossPetAnimator?.SetInteger("key", animKey);
                Debug.Log($"[EVENT ANIM] Enemy pet animation: key={animKey}");
            }
            else
            {
                playerPetAnimator?.SetInteger("key", animKey);
                Debug.Log($"[EVENT ANIM] Player pet animation: key={animKey}");
            }
        }
    }

    private void BroadcastStatsViaEvent()
    {
        object[] content = new object[] { MauPlayer, ManaPlayer, NoPlayer, GiapPlayer };
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        PhotonNetwork.RaiseEvent(STATS_UPDATE_EVENT, content, raiseEventOptions, SendOptions.SendReliable);
        Debug.Log($"[EVENT SEND] Player {PhotonNetwork.LocalPlayer.ActorNumber} gửi stats qua Event");
    }

    // ==================== ANIMATION SYNC ====================
    public void PlayAnimationSynced(int animKey, bool isPlayer)
    {
        // Play local animation
        if (isPlayer)
        {
            if (playerPetAnimator != null)
            {
                playerPetAnimator.SetInteger("key", animKey);
                Debug.Log($"[ANIM LOCAL] Player pet: key={animKey}");
            }
        }
        else
        {
            if (bossPetAnimator != null)
            {
                bossPetAnimator.SetInteger("key", animKey);
                Debug.Log($"[ANIM LOCAL] Enemy pet: key={animKey}");
            }
        }

        // Broadcast animation to other players
        object[] data = new object[] { animKey, isPlayer };
        RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        PhotonNetwork.RaiseEvent(ANIMATION_EVENT, data, options, SendOptions.SendReliable);

        Debug.Log($"[ANIM SEND] Broadcast animation: key={animKey}, isPlayer={isPlayer}");
    }

    [PunRPC]
    private void SyncAnimation(int animKey, bool isPlayer)
    {
        if (isPlayer)
        {
            // Remote player = local enemy
            if (bossPetAnimator != null)
            {
                bossPetAnimator.SetInteger("key", animKey);
                Debug.Log($"[RPC ANIM] Enemy pet animation: key={animKey}");
            }
        }
        else
        {
            if (playerPetAnimator != null)
            {
                playerPetAnimator.SetInteger("key", animKey);
                Debug.Log($"[RPC ANIM] Player pet animation: key={animKey}");
            }
        }
    }

    public void ResetAnimationToIdle()
    {
        if (playerPetAnimator != null)
            playerPetAnimator.SetInteger("key", 0);
        if (bossPetAnimator != null)
            bossPetAnimator.SetInteger("key", 0);

        // Đồng bộ reset
        photonView.RPC("SyncResetAnimation", RpcTarget.Others);
        Debug.Log("[ANIM] Reset all animations to idle");
    }

    [PunRPC]
    private void SyncResetAnimation()
    {
        if (playerPetAnimator != null)
            playerPetAnimator.SetInteger("key", 0);
        if (bossPetAnimator != null)
            bossPetAnimator.SetInteger("key", 0);
        Debug.Log("[RPC] Reset animations to idle");
    }

    public IEnumerator WaitForAnimation(float duration = 1f)
    {
        yield return new WaitForSeconds(duration);
        ResetAnimationToIdle();
    }

    // ==================== COUNTDOWN SYSTEM ====================
    public void StartCountdown()
    {
        // ❌ XÓA KIỂM TRA NÀY
        // if (!PhotonNetwork.IsMasterClient) 
        // {
        //     Debug.LogWarning("[COUNTDOWN] Chỉ Master có thể bắt đầu countdown");
        //     return;
        // }

        StopCountdown();

        BoardPVP board = FindObjectOfType<BoardPVP>();
        if (board == null)
        {
            Debug.LogError("[COUNTDOWN] Không tìm thấy BoardPVP");
            return;
        }

        countdownTurn = board.currentTurn;
        countdownStartTime = PhotonNetwork.Time;
        countdownDuration = turnDuration;

        Debug.Log($"[COUNTDOWN] Player {PhotonNetwork.LocalPlayer.ActorNumber} bắt đầu: turn={countdownTurn}");

        // Bắt đầu countdown local
        countdownCoroutine = StartCoroutine(SyncedCountdownCoroutine());

        // ✅ CHỈ MASTER GỬI RPC
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("SyncCountdownStart", RpcTarget.Others, countdownStartTime, countdownDuration, countdownTurn);
        }
    }

    public void StartSyncedCountdown(double startTime, int duration, int turnNumber)
    {
        Debug.Log($"[SYNCED COUNTDOWN] Client nhận: start={startTime}, duration={duration}, turn={turnNumber}");

        StopCountdown();

        countdownStartTime = startTime;
        countdownDuration = duration;
        countdownTurn = turnNumber;

        // Bắt đầu countdown đồng bộ
        countdownCoroutine = StartCoroutine(SyncedCountdownCoroutine());
    }

    [PunRPC]
    private void SyncCountdownStart(double startTime, int duration, int turnNumber)
    {
        Debug.Log($"[RPC COUNTDOWN] Client {PhotonNetwork.LocalPlayer.ActorNumber} nhận countdown: turn={turnNumber}");

        // ✅ KIỂM TRA TURN ĐỂ TRÁNH COUNTDOWN CŨ
        BoardPVP board = FindObjectOfType<BoardPVP>();
        if (board != null && turnNumber < board.currentTurn)
        {
            Debug.LogWarning($"[RPC COUNTDOWN] Bỏ qua countdown cũ: nhận turn {turnNumber}, hiện tại là {board.currentTurn}");
            return;
        }

        // ✅ LUÔN START COUNTDOWN KHI NHẬN RPC
        StopCountdown(); // Dừng countdown cũ (nếu có)
        StartSyncedCountdown(startTime, duration, turnNumber);
    }

    private IEnumerator SyncedCountdownCoroutine()
    {
        isCountdownRunning = true;
        isTimeOver = false;

        Debug.Log($"[COUNTDOWN COROUTINE] Bắt đầu countdown cho turn {countdownTurn}");

        while (true)
        {
            // Tính thời gian còn lại dựa trên Photon Network Time
            double currentTime = PhotonNetwork.Time;
            double elapsed = currentTime - countdownStartTime;
            int timeRemaining = Mathf.Max(0, countdownDuration - (int)elapsed);

            // Cập nhật UI
            thoigiandem = timeRemaining;
            UpdateCountdownUI(timeRemaining);

            // Kiểm tra hết giờ
            if (timeRemaining <= 0)
            {
                Debug.Log($"[COUNTDOWN] Hết giờ cho turn {countdownTurn}");
                isTimeOver = true;
                HandleTimeUp();
                break;
            }

            yield return new WaitForSeconds(0.1f); // Update mỗi 0.1s
        }

        isCountdownRunning = false;
        countdownCoroutine = null;
    }

    private void UpdateCountdownUI(int timeRemaining)
    {
        if (countdownText != null)
        {
            countdownText.text = timeRemaining.ToString();

            // Hiệu ứng cảnh báo khi sắp hết giờ
            if (timeRemaining <= 5)
            {
                countdownText.color = Color.red;

                // Thêm hiệu ứng scale nhấp nháy
                if (timeRemaining <= 3)
                {
                    countdownText.transform.localScale = Vector3.one * (1f + Mathf.PingPong(Time.time * 5f, 0.3f));
                }
            }
            else
            {
                countdownText.color = Color.white;
                countdownText.transform.localScale = Vector3.one;
            }
        }
    }

    public void StopCountdown()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
        isCountdownRunning = false;
        isTimeOver = false;

        // Reset UI
        if (countdownText != null)
        {
            countdownText.color = Color.white;
            countdownText.transform.localScale = Vector3.one;
        }
    }

    private void HandleTimeUp()
    {
        Debug.Log($"[TIME UP] Xử lý hết giờ cho turn {countdownTurn}");

        BoardPVP board = FindObjectOfType<BoardPVP>();
        if (board != null)
        {
            // Chỉ Master xử lý logic hết giờ
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log("[TIME UP] Master xử lý hết giờ");
                board.HandleTurnEnd();
                board.ResetMoveCounters();
                StartCoroutine(ForceEndTurn());
            }
            else
            {
                Debug.Log("[TIME UP] Client chờ Master xử lý hết giờ");
            }
        }

        // Reset trạng thái
        thoigiandem = 0;
        UpdateCountdownUI(0);
    }

    private IEnumerator ForceEndTurn()
    {
        yield return new WaitForSeconds(0.5f);

        BoardPVP board = FindObjectOfType<BoardPVP>();
        if (board != null)
        {
            int nextTurn = board.currentTurn + 1;
            board.photonView.RPC("SyncTurn", RpcTarget.AllBuffered, nextTurn, PhotonNetwork.Time);

            yield return new WaitForSeconds(0.3f);
            StartCountdown(); // Restart countdown cho turn mới
        }
    }

    // ==================== TURN UI MANAGEMENT ====================
    public void UpdateTurnUI()
    {
        BoardPVP board = FindObjectOfType<BoardPVP>();
        if (board == null) return;

        bool isMyTurn = (board.currentTurn % 2 == 1 && PhotonNetwork.IsMasterClient) ||
                       (board.currentTurn % 2 == 0 && !PhotonNetwork.IsMasterClient);

        Debug.Log($"[TURN UI] Player {PhotonNetwork.LocalPlayer.ActorNumber} - Turn {board.currentTurn} - My Turn: {isMyTurn}");

        // Cập nhật UI để hiển thị lượt chơi
        if (countdownText != null)
        {
            if (isMyTurn)
            {
                countdownText.color = Color.green;
            }
            else
            {
                countdownText.color = Color.yellow;
            }
        }
    }

    // ==================== NETWORK CALLBACKS ====================
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[NETWORK] Player {otherPlayer.ActorNumber} đã rời phòng");
        StopCountdown();
    }

    public override void OnLeftRoom()
    {
        Debug.Log("[NETWORK] Đã rời phòng");
        StopCountdown();
    }

    // ==================== OUTPUT EFFECTS ====================
    public IEnumerator OutputsParam(string itemType)
    {
        BoardPVP board = FindObjectOfType<BoardPVP>();
        if (board == null)
        {
            Debug.LogError("[OUTPUT] Không tìm thấy BoardPVP!");
            yield break;
        }
        int currentTurn = board.currentTurn;
        bool isAttackerLocal = (PhotonNetwork.IsMasterClient && (currentTurn % 2 == 1)) ||
                               (!PhotonNetwork.IsMasterClient && (currentTurn % 2 == 0));

        if (itemType.Equals("vang Dot")) // ATTACK
        {
            if (isAttackerLocal)
            {
                // ✅ ANIMATION CHO PLAYER TẤN CÔNG
                if (NoPlayer >= 100)
                {
                    Debug.Log("[ATTACK] Ultimate Skill!");
                    yield return PlayUltimateSkill();

                    if (dameATKPrefadNPC != null)
                    {
                        yield return new WaitForSeconds(0.5f);
                        dameATKPrefadNPC.SetActive(true);
                        Text dame = dameATKPrefadNPC.transform.Find("txtdame").GetComponent<Text>();
                        dame.text = $"{valueCurrent} ULTIMATE!";
                        yield return new WaitForSeconds(1f);
                        dameATKPrefadNPC.SetActive(false);
                    }
                }
                else // NORMAL ATTACK
                {
                    Debug.Log("[ATTACK] Normal Attack");
                    PlayAnimationSynced(1, true); // ← Tấn công = player animation
                    StartCoroutine(DelayedAnimation(ATTACK_HIT_DELAY, 4, false)); // ← Enemy bị hit

                    yield return new WaitForSeconds(ATTACK_HIT_DELAY);
                    dameATKPrefadNPC.SetActive(true);
                    Text dame = dameATKPrefadNPC.transform.Find("txtdame").GetComponent<Text>();
                    dame.text = valueCurrent.ToString();

                    yield return new WaitForSeconds(ANIMATION_DURATION);
                    dameATKPrefadNPC.SetActive(false);

                    StartCoroutine(DelayedAnimation(0.2f, 0, true));
                    StartCoroutine(DelayedAnimation(0.2f, 0, false));
                }
            }
            else
            {
                // ✅ ANIMATION CHO PLAYER BỊ TẤN CÔNG (REMOTE)
                Debug.Log("[ATTACK] Remote player attacks - Playing hit animation");
                PlayAnimationSynced(4, true); // ← Player bị hit

                dameATKPrefad.SetActive(true);
                Text dame = dameATKPrefad.transform.Find("txtdame").GetComponent<Text>();
                dame.text = valueCurrent.ToString();

                yield return new WaitForSeconds(ANIMATION_DURATION);
                dameATKPrefad.SetActive(false);

                StartCoroutine(DelayedAnimation(0.2f, 0, true));
            }

            yield return new WaitForSeconds(0.3f);
        }
        else if (itemType.Equals("do Dot")) // RAGE/POWER
        {
            Debug.Log($"[BUFF] Power Buff - IsAttacker: {isAttackerLocal}");
            yield return PlayBuffAnimation(isAttackerLocal, "Power");

            if (isAttackerLocal)
            {
                // ✅ ANIMATION VÀ UI CHO PLAYER
                healdPower.SetActive(true);
                healdPower.gameObject.transform.GetComponent<Text>().text = "+ " + valueCurrent.ToString();
                yield return effect.FadeAndMoveUp(healdPower);
            }
            else
            {
                // ✅ ANIMATION VÀ UI CHO ENEMY (REMOTE PLAYER)
                healdPowerNPC.SetActive(true);
                healdPowerNPC.gameObject.transform.GetComponent<Text>().text = "+ " + valueCurrent.ToString();
                yield return effect.FadeAndMoveUp(healdPowerNPC);
            }

            StartCoroutine(DelayedAnimation(BUFF_DURATION, 0, isAttackerLocal));
            yield return new WaitForSeconds(0.3f);
        }

        else if (itemType.Equals("xanh Dot")) // HEAL HP
        {
            Debug.Log($"[HEAL] Big Heal - IsAttacker: {isAttackerLocal}");
            yield return PlayBuffAnimation(isAttackerLocal, "Heal");

            if (isAttackerLocal)
            {
                healdHP.SetActive(true);
                healdHP.gameObject.transform.GetComponent<Text>().text = "+ " + valueCurrent.ToString();
                yield return effect.FadeAndMoveUp(healdHP);
            }
            else
            {
                healdHPNPC.SetActive(true);
                healdHPNPC.gameObject.transform.GetComponent<Text>().text = "+ " + valueCurrent.ToString();
                yield return effect.FadeAndMoveUp(healdHPNPC);
            }

            StartCoroutine(DelayedAnimation(BUFF_DURATION, 0, isAttackerLocal));
            yield return new WaitForSeconds(0.3f);
        }

        else if (itemType.Equals("xanhduong Dot")) // MANA
        {
            Debug.Log($"[MANA] Restore Mana - IsAttacker: {isAttackerLocal}");
            PlayAnimationSynced(3, isAttackerLocal);

            if (isAttackerLocal)
            {
                healdMana.SetActive(true);
                healdMana.gameObject.transform.GetComponent<Text>().text = "+ " + valueCurrent.ToString();
                yield return effect.FadeAndMoveUp(healdMana);
            }
            else
            {
                healdManaNPC.SetActive(true);
                healdManaNPC.gameObject.transform.GetComponent<Text>().text = "+ " + valueCurrent.ToString();
                yield return effect.FadeAndMoveUp(healdManaNPC);
            }

            StartCoroutine(DelayedAnimation(BUFF_DURATION, 0, isAttackerLocal));
            yield return new WaitForSeconds(0.3f);
        }

        else if (itemType.Equals("tim Dot")) // DEFENSE/SHIELD
        {
            Debug.Log($"[SHIELD] Defense Buff - IsAttacker: {isAttackerLocal}");
            PlayAnimationSynced(3, isAttackerLocal);

            if (isAttackerLocal)
            {
                healdDEF.SetActive(true);
                healdDEF.gameObject.transform.GetComponent<Text>().text = "+ " + valueCurrent.ToString();
                yield return effect.FadeAndMoveUp(healdDEF);
            }
            else
            {
                healdDEFNPC.SetActive(true);
                healdDEFNPC.gameObject.transform.GetComponent<Text>().text = "+ " + valueCurrent.ToString();
                yield return effect.FadeAndMoveUp(healdDEFNPC);
            }

            StartCoroutine(DelayedAnimation(BUFF_DURATION, 0, isAttackerLocal));
            yield return new WaitForSeconds(0.3f);
        }

        else if (itemType.Equals("trang Dot")) // STEAL/DRAIN
        {
            Debug.Log($"[STEAL] Counter Attack - IsAttacker: {isAttackerLocal}");
            yield return PlayCounterAttack();

            if (isAttackerLocal)
            {
                healdHP.SetActive(true);
                healdHP.gameObject.transform.GetComponent<Text>().text = "+ " + valueCurrent.ToString();
                yield return effect.FadeAndMoveUp(healdHP);
            }
            else
            {
                healdHPNPC.SetActive(true);
                healdHPNPC.gameObject.transform.GetComponent<Text>().text = "+ " + valueCurrent.ToString();
                yield return effect.FadeAndMoveUp(healdHPNPC);
            }

            StartCoroutine(DelayedAnimation(0.5f, 0, isAttackerLocal));
            StartCoroutine(DelayedAnimation(0.5f, 0, !isAttackerLocal));
            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator DelayedAnimation(float delay, int animKey, bool isPlayer)
    {
        yield return new WaitForSeconds(delay);
        PlayAnimationSynced(animKey, isPlayer);
    }

    // ==================== CALCULATE OUTPUTS ====================
    public void CalculateOutputs(string tag, int count)
{
    Debug.Log($"[CALC] Tính toán: tag={tag}, count={count}");
    int currentAttack = attackP;
    int currenthealth = maxMau;
    int currentmana = maxMana;

    GiapPlayer = 0;

    switch (tag)
    {
        case "vang Dot": // Attack
            int finalDamage;
            bool attackerNoFull = (NoPlayer >= 100);

            if (attackerNoFull)
            {
                outputKiem = Mathf.RoundToInt(currentAttack * 1.5f * count);
                finalDamage = Mathf.Max(outputKiem - GiapNPC, 0);
                MauNPC -= finalDamage;
                NoPlayer -= 100;
                NoNPC = Mathf.Min(NoNPC + 30, maxNoNPC);
            }
            else
            {
                outputKiem = Mathf.RoundToInt(currentAttack * count);
                finalDamage = Mathf.Max(outputKiem - GiapNPC, 0);
                MauNPC -= finalDamage;
                NoNPC = Mathf.Min(NoNPC + 15, maxNoNPC);
            }

            MauPlayer = Mathf.Clamp(MauPlayer, 0, maxMau);
            MauNPC = Mathf.Clamp(MauNPC, 0, maxMauNPC);
            valueCurrent = outputKiem;
            break;

        case "tim Dot": // Defense/Shield
            outputGiap = Mathf.RoundToInt(currentAttack * 0.3f * count);
            GiapPlayer += outputGiap;
            valueCurrent = outputGiap;
            break;

        case "do Dot": // Rage/Power
            outputNo = 20 * count;
            NoPlayer += outputNo;
            NoPlayer = Mathf.Clamp(NoPlayer, 0, maxNo);
            valueCurrent = outputNo;
            break;

        case "trang Dot": // Steal/Drain
            outputHut = 20 * count;
            int stealType = UnityEngine.Random.Range(0, 3);
            switch (stealType)
            {
                case 0:
                    int actualStealHP = Mathf.Min(outputHut, MauNPC);
                    MauNPC -= actualStealHP;
                    MauPlayer += actualStealHP;
                    MauPlayer = Mathf.Clamp(MauPlayer, 0, maxMau);
                    break;
                case 1:
                    int actualStealNo = Mathf.Min(outputHut, NoNPC);
                    NoNPC -= actualStealNo;
                    NoPlayer += actualStealNo;
                    NoPlayer = Mathf.Clamp(NoPlayer, 0, maxNo);
                    break;
                case 2:
                    int actualStealMana = Mathf.Min(outputHut, ManaNPC);
                    ManaNPC -= actualStealMana;
                    ManaPlayer += actualStealMana;
                    ManaPlayer = Mathf.Clamp(ManaPlayer, 0, maxMana);
                    break;
            }
            valueCurrent = outputHut;
            break;

        case "xanh Dot": // Heal HP
            outputMau = Mathf.RoundToInt(currenthealth * 0.05f * count);
            MauPlayer += outputMau;
            MauPlayer = Mathf.Clamp(MauPlayer, 0, maxMau);
            valueCurrent = outputMau;
            break;

        case "xanhduong Dot": // Restore Mana
            outputMana = Mathf.RoundToInt(currentmana * 0.05f * count);
            ManaPlayer += outputMana;
            ManaPlayer = Mathf.Clamp(ManaPlayer, 0, maxMana);
            valueCurrent = outputMana;
            break;
    }

    // Clamp all stats
    MauPlayer = Mathf.Clamp(MauPlayer, 0, maxMau);
    ManaPlayer = Mathf.Clamp(ManaPlayer, 0, maxMana);
    NoPlayer = Mathf.Clamp(NoPlayer, 0, maxNo);
    MauNPC = Mathf.Clamp(MauNPC, 0, maxMauNPC);
    ManaNPC = Mathf.Clamp(ManaNPC, 0, maxManaNPC);
    NoNPC = Mathf.Clamp(NoNPC, 0, maxNoNPC);

    // ✅ QUAN TRỌNG: Đồng bộ NGAY LẬP TỨC qua RPC
    SyncStatsToAll();
}

// ✅ 2. THÊM PHƯƠNG THỨC MỚI - Đồng bộ stats (FIXED VERSION)
private void SyncStatsToAll()
{
    Debug.Log($"[SYNC STATS] Player {PhotonNetwork.LocalPlayer.ActorNumber} gửi stats");
    Debug.Log($"  → Máu Player={MauPlayer}, Enemy={MauNPC}");
    Debug.Log($"  → Mana Player={ManaPlayer}, Enemy={ManaNPC}");
    Debug.Log($"  → Nộ Player={NoPlayer}, Enemy={NoNPC}");
    
    // ✅ GỬI ĐÚNG: Gửi stats của CHÍNH MÌNH (Player) và stats của ĐỐI THỦ (NPC)
    photonView.RPC(
        "SyncPlayerStats",
        RpcTarget.Others,
        MauPlayer, ManaPlayer, NoPlayer, GiapPlayer,    // Stats của MÌNH
        MauNPC, ManaNPC, NoNPC, GiapNPC,                // Stats của ĐỐI THỦ (theo góc nhìn mình)
        PhotonNetwork.LocalPlayer.ActorNumber,
        PhotonNetwork.Time
    );
    
    // ✅ CẬP NHẬT UI LOCAL NGAY
    UpdateSlider();
}

// ✅ 3. RPC NHẬN STATS (LOGIC HOÀN TOÀN MỚI - ĐÚNG 100%)
[PunRPC]
private void SyncPlayerStats(
    int remoteMauPlayer, int remoteManaPlayer, int remoteNoPlayer, int remoteGiapPlayer,
    int remoteMauNPC, int remoteManaNPC, int remoteNoNPC, int remoteGiapNPC,
    int senderActorNumber,
    double timestamp)
{
    // ✅ BẢO VỆ: Không xử lý message từ chính mình
    if (senderActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
    {
        Debug.LogWarning("[SYNC STATS] Bỏ qua message từ chính mình");
        return;
    }

    // Kiểm tra timestamp
    if (PhotonNetwork.Time < timestamp - 0.5)
    {
        Debug.LogWarning($"[SYNC STATS] Bỏ qua dữ liệu cũ: {timestamp}");
        return;
    }
    
    Debug.Log($"[SYNC STATS] ========================================");
    Debug.Log($"[SYNC STATS] Player {PhotonNetwork.LocalPlayer.ActorNumber} nhận từ Player {senderActorNumber}");
    
    // ✅ HIỂU ĐÚNG LOGIC:
    // Remote player gửi:
    //   - remoteMauPlayer = Máu của REMOTE (người gửi)
    //   - remoteMauNPC = Máu của địch thủ REMOTE nhìn thấy (chính là TA!)
    //
    // Vậy khi TA nhận:
    //   - remoteMauPlayer = Máu của địch (remote player) = MauNPC của ta
    //   - remoteMauNPC = Máu của ta (vì ta là NPC trong góc nhìn remote) = MauPlayer của ta
    
    Debug.Log($"[SYNC STATS] Nhận được:");
    Debug.Log($"  → remoteMauPlayer={remoteMauPlayer} (enemy của ta)");
    Debug.Log($"  → remoteMauNPC={remoteMauNPC} (chính ta)");
    
    // ✅ CẬP NHẬT ĐÚNG:
    // Enemy của ta = Player của remote
    MauNPC = remoteMauPlayer;
    ManaNPC = remoteManaPlayer;
    NoNPC = remoteNoPlayer;
    GiapNPC = remoteGiapPlayer;
    
    // Ta = NPC của remote (vì ta là địch của họ)
    MauPlayer = remoteMauNPC;
    ManaPlayer = remoteManaNPC;
    NoPlayer = remoteNoNPC;
    GiapPlayer = remoteGiapNPC;
    
    Debug.Log($"[SYNC STATS] Sau khi cập nhật:");
    Debug.Log($"  → Player: Máu={MauPlayer}, Mana={ManaPlayer}, Nộ={NoPlayer}, Giáp={GiapPlayer}");
    Debug.Log($"  → Enemy: Máu={MauNPC}, Mana={ManaNPC}, Nộ={NoNPC}, Giáp={GiapNPC}");
    Debug.Log($"[SYNC STATS] ========================================");

    // ✅ CẬP NHẬT UI
    UpdateSlider();
}

    // ==================== UPDATE SLIDER ====================
    public void UpdateSlider()
    {
        Debug.Log($"[UI] Player: Máu={MauPlayer}/{maxMau}, Mana={ManaPlayer}/{maxMana}, Nộ={NoPlayer}/{maxNo} | NPC: Máu={MauNPC}/{maxMauNPC}, Mana={ManaNPC}/{maxManaNPC}, Nộ={NoNPC}/{maxNoNPC}");

        // ===== PLAYER SLIDERS =====
        if (thanhMauSlider != null)
        {
            float playerMaxMau = maxMau > 0 ? maxMau : 1;
            thanhMauSlider.value = Mathf.Clamp01((float)MauPlayer / playerMaxMau);
            Debug.Log($"[SLIDER] Player Máu Slider: {MauPlayer}/{maxMau} = {thanhMauSlider.value}");
        }
        if (textMauPlayer != null)
        {
            textMauPlayer.text = $"{MauPlayer}/{maxMau}";
        }

        if (thanhNoSlider != null)
        {
            float playerMaxNo = maxNo > 0 ? maxNo : 1;
            thanhNoSlider.value = Mathf.Clamp01((float)NoPlayer / playerMaxNo);
            Debug.Log($"[SLIDER] Player Nộ Slider: {NoPlayer}/{maxNo} = {thanhNoSlider.value}");
        }
        if (textNoPlayer != null)
        {
            textNoPlayer.text = $"{NoPlayer}/{maxNo}";
        }

        if (thanhManaSlider != null)
        {
            float playerMaxMana = maxMana > 0 ? maxMana : 1;
            thanhManaSlider.value = Mathf.Clamp01((float)ManaPlayer / playerMaxMana);
            Debug.Log($"[SLIDER] Player Mana Slider: {ManaPlayer}/{maxMana} = {thanhManaSlider.value}");
        }
        if (textManaPlayer != null)
        {
            textManaPlayer.text = $"{ManaPlayer}/{maxMana}";
        }

        // ===== NPC SLIDERS =====
        if (thanhMauNPC != null)
        {
            if (maxMauNPC <= 0)
            {
                Debug.LogWarning($"[SLIDER] maxMauNPC không hợp lệ: {maxMauNPC}. Dùng giá trị mặc định 150.");
                maxMauNPC = 150;
            }
            float npcValue = Mathf.Clamp01((float)MauNPC / maxMauNPC);
            thanhMauNPC.value = npcValue;
            Debug.Log($"[SLIDER] NPC Máu Slider: {MauNPC}/{maxMauNPC} = {npcValue}");
        }
        if (textMauNPC != null)
        {
            textMauNPC.text = $"{MauNPC}/{maxMauNPC}";
        }

        if (thanhNoNPC != null)
        {
            if (maxNoNPC <= 0)
            {
                Debug.LogWarning($"[SLIDER] maxNoNPC không hợp lệ: {maxNoNPC}. Dùng giá trị mặc định 100.");
                maxNoNPC = 100;
            }
            float npcValue = Mathf.Clamp01((float)NoNPC / maxNoNPC);
            thanhNoNPC.value = npcValue;
            Debug.Log($"[SLIDER] NPC Nộ Slider: {NoNPC}/{maxNoNPC} = {npcValue}");
        }
        if (textNoNPC != null)
        {
            textNoNPC.text = $"{NoNPC}/{maxNoNPC}";
        }

        if (thanhManaNPC != null)
        {
            if (maxManaNPC <= 0)
            {
                Debug.LogWarning($"[SLIDER] maxManaNPC không hợp lệ: {maxManaNPC}. Dùng giá trị mặc định 100.");
                maxManaNPC = 100;
            }
            float npcValue = Mathf.Clamp01((float)ManaNPC / maxManaNPC);
            thanhManaNPC.value = npcValue;
            Debug.Log($"[SLIDER] NPC Mana Slider: {ManaNPC}/{maxManaNPC} = {npcValue}");
        }
        if (textManaNPC != null)
        {
            textManaNPC.text = $"{ManaNPC}/{maxManaNPC}";
        }
    }

    // ==================== SUMMARY & RESET ====================
    public void SummaryOutputs()
    {
        Debug.Log($"[SUMMARY] Player:\n +Máu:{sloMauAnDuoc}-->{outputMau}->{MauPlayer}/{maxMau}");
        Debug.Log($"[SUMMARY] Player:\n +Mana:{sloManaAnDuoc}-->{outputMana}->{ManaPlayer}/{maxMana}");
        Debug.Log($"[SUMMARY] Player:\n +Nộ:{sloNoAnDuoc}-->{outputNo}->{NoPlayer}/{maxNo}");
        Debug.Log($"[SUMMARY] Player:\n +Kiếm:{sloKiemAnDuoc}->{outputKiem}");
        Debug.Log($"[SUMMARY] Player:\n +Giáp:{sloGiapAnDuoc}->{outputGiap}");
        Debug.Log($"[SUMMARY] Player:\n +Hút:{sloHutAnDuoc}->{outputHut}");
        Debug.Log($"[SUMMARY] NPC:\n Máu:{MauNPC}/{maxMauNPC}");
        Debug.Log($"[SUMMARY] NPC:\n Mana:{ManaNPC}/{maxManaNPC}");
        Debug.Log($"[SUMMARY] NPC:\n Nộ:{NoNPC}/{maxNoNPC}");
    }

    public void resetOutput()
    {
        sloMauAnDuoc = 0;
        sloManaAnDuoc = 0;
        sloNoAnDuoc = 0;
        sloKiemAnDuoc = 0;
        sloGiapAnDuoc = 0;
        sloHutAnDuoc = 0;
        outputKiem = 0;
        outputMana = 0;
        outputNo = 0;
        outputMau = 0;
        outputHut = 0;
    }

    // ==================== CARD HANDLING ====================
    public void IncreaseNoPlayer(CardInfo cardInfo)
    {
        Debug.Log($"[CARD] Sử dụng thẻ ID={cardInfo.IdCard}, Value={cardInfo.Value}");

        if (cardInfo.IdCard == 2) // Rage card
        {
            // Play buff animation
            PlayAnimationSynced(3, true);

            NoPlayer += cardInfo.Value;
            NoPlayer = Mathf.Clamp(NoPlayer, 0, maxNo);
            Debug.Log($"[CARD] Tăng Nộ: +{cardInfo.Value} -> {NoPlayer}/{maxNo}");

            // Show effect
            StartCoroutine(ShowCardEffect(healdPower, $"+ {cardInfo.Value}", true));

            // Reset animation after buff duration
            StartCoroutine(DelayedAnimation(BUFF_DURATION, 0, true));
        }
        else if (cardInfo.IdCard == 3) // Health card
        {
            // Play heal animation
            PlayAnimationSynced(3, true);

            MauPlayer += cardInfo.Value;
            MauPlayer = Mathf.Clamp(MauPlayer, 0, maxMau);
            Debug.Log($"[CARD] Tăng Máu: +{cardInfo.Value} -> {MauPlayer}/{maxMau}");

            // Show effect
            StartCoroutine(ShowCardEffect(healdHP, $"+ {cardInfo.Value}", true));

            // Reset animation
            StartCoroutine(DelayedAnimation(BUFF_DURATION, 0, true));
        }
        else if (cardInfo.IdCard == 1) // Mana card
        {
            // Play mana restore animation
            PlayAnimationSynced(3, true);

            ManaPlayer += cardInfo.Value;
            ManaPlayer = Mathf.Clamp(ManaPlayer, 0, maxMana);
            Debug.Log($"[CARD] Tăng Mana: +{cardInfo.Value} -> {ManaPlayer}/{maxMana}");

            // Show effect
            StartCoroutine(ShowCardEffect(healdMana, $"+ {cardInfo.Value}", true));

            // Reset animation
            StartCoroutine(DelayedAnimation(BUFF_DURATION, 0, true));
        }
        else if (ManaPlayer >= cardInfo.ConditionUse && cardInfo.IdCard >= 4) // Skill card
        {
            ManaPlayer -= cardInfo.ConditionUse;
            ManaPlayer = Mathf.Clamp(ManaPlayer, 0, maxMana);
            MauNPC -= cardInfo.Value;
            MauNPC = Mathf.Clamp(MauNPC, 0, maxMauNPC);

            // Skill attack sequence
            PlayAnimationSynced(2, true);
            StartCoroutine(DelayedAnimation(SKILL_CAST_DELAY, 4, false));
            StartCoroutine(ShowCardDamageSequence(cardInfo.Value));
            StartCoroutine(DelayedAnimation(RESET_DELAY, 0, true));
            StartCoroutine(DelayedAnimation(RESET_DELAY, 0, false));

            Debug.Log($"[CARD] Skill: Tiêu {cardInfo.ConditionUse} Mana, gây {cardInfo.Value} sát thương");
        }
        else
        {
            Debug.LogWarning($"[CARD] Không xác định hoặc không đủ điều kiện: {cardInfo.IdCard}");
            return;
        }

        UpdatePlayerStats();
        UpdateSlider();

        photonView.RPC("SyncUseCard", RpcTarget.Others, cardInfo.IdCard, cardInfo.IdCardUser,
                       cardInfo.Value, cardInfo.ConditionUse, cardInfo.Lever);
    }

    [PunRPC]
    private void SyncUseCard(int cardId, string cardUserId, int value, int conditionUse, int lever)
    {
        Debug.Log($"[RPC] Nhận thông tin sử dụng thẻ từ remote: ID={cardId}, Value={value}");
    }

    // ==================== CARD UI SYNC ====================
    [PunRPC]
    private void SyncShowCardUI(string cardSpriteName, int cardId)
    {
        if (onCard == null)
        {
            Debug.LogError("onCard GameObject chưa được gán trong Active.");
            return;
        }

        Debug.Log($"[RPC] Client {PhotonNetwork.LocalPlayer.ActorNumber} nhận SyncShowCardUI: ID={cardId}, Sprite={cardSpriteName}");
        onCard.SetActive(true);

        Image cardImage = onCard.GetComponent<Image>();
        if (cardImage == null)
        {
            Debug.LogError("Không tìm thấy Image component trên onCard.");
            onCard.SetActive(false);
            return;
        }

        Sprite cardSprite = Resources.Load<Sprite>("Cards/" + cardSpriteName);
        if (cardSprite != null)
        {
            cardImage.sprite = cardSprite;
            Debug.Log($"[RPC] Tải sprite: {cardSpriteName}");
        }
        else
        {
            Debug.LogWarning($"Sprite {cardSpriteName} không tìm thấy. Dùng mặc định.");
            cardSprite = Resources.Load<Sprite>("Cards/DefaultCard");
            if (cardSprite != null)
            {
                cardImage.sprite = cardSprite;
            }
            else
            {
                Debug.LogError("Sprite mặc định không tìm thấy.");
            }
        }
    }

    [PunRPC]
    private void SyncHideCardUI()
    {
        if (onCard != null)
        {
            onCard.SetActive(false);
            Debug.Log($"[RPC] Client {PhotonNetwork.LocalPlayer.ActorNumber} ẩn onCard.");
        }
    }

    [PunRPC]
    private void SyncHideBoard()
    {
        BoardPVP board = FindFirstObjectByType<BoardPVP>();
        if (board != null)
        {
            board.HideAllItems();
            Debug.Log($"[RPC] Client {PhotonNetwork.LocalPlayer.ActorNumber} ẩn board.");
        }
    }

    [PunRPC]
    private void SyncShowBoard()
    {
        BoardPVP board = FindFirstObjectByType<BoardPVP>();
        if (board != null)
        {
            board.ShowItems();
            Debug.Log($"[RPC] Client {PhotonNetwork.LocalPlayer.ActorNumber} hiển thị board.");
        }
    }

    // ==================== LIST CARD SYNC ====================
    private void SyncListCardInfos()
    {
        ListCard listCard = FindFirstObjectByType<ListCard>();
        if (listCard != null && PhotonNetwork.IsMasterClient)
        {
            string serializedCardInfos = SerializeCardInfos(listCard.cardInfos);
            photonView.RPC("SyncCardInfos", RpcTarget.AllBuffered, serializedCardInfos);
        }
    }

    private string SerializeCardInfos(List<CardInfo> cardInfos)
    {
        List<string> cardData = new List<string>();
        foreach (var card in cardInfos)
        {
            cardData.Add($"{card.IdCard},{card.IdCardUser},{card.CardDetail},{card.Value},{card.Url},{card.Lever},{card.ConditionUse}");
        }
        return string.Join("|", cardData);
    }

    [PunRPC]
    private void SyncCardInfos(string serializedData)
    {
        if (string.IsNullOrEmpty(serializedData)) return;

        string[] cards = serializedData.Split('|');
        cardInfos.Clear();

        foreach (string cardData in cards)
        {
            string[] parts = cardData.Split(',');
            if (parts.Length >= 7)
            {
                CardInfo card = new CardInfo(
                    int.Parse(parts[0]),  // IdCard
                    parts[1],             // IdCardUser
                    parts[2],             // CardDetail
                    int.Parse(parts[3]),  // Value
                    null,                 // Url (sprite)
                    int.Parse(parts[5]),  // Lever
                    int.Parse(parts[6])   // ConditionUse
                );
                cardInfos.Add(card);
            }
        }
        Debug.Log($"[SYNC] Đồng bộ {cardInfos.Count} thẻ bài.");
    }

    public void SetTurnDuration(int seconds)
    {
        turnDuration = Mathf.Max(1, seconds);
    }

    // ==================== LEGACY RPC (BACKUP) ====================
    [PunRPC]
    private void SyncFinalStats(
        int masterMau, int masterMana, int masterNo,
        int clientMau, int clientMana, int clientNo,
        double timestamp)
    {
        Debug.Log($"[LEGACY RPC] SyncFinalStats timestamp={timestamp}");

        if (PhotonNetwork.IsMasterClient)
        {
            MauPlayer = masterMau;
            ManaPlayer = masterMana;
            NoPlayer = masterNo;
            MauNPC = clientMau;
            ManaNPC = clientMana;
            NoNPC = clientNo;
        }
        else
        {
            MauPlayer = clientMau;
            ManaPlayer = clientMana;
            NoPlayer = clientNo;
            MauNPC = masterMau;
            ManaNPC = masterMana;
            NoNPC = masterNo;
        }

        UpdateSlider();
    }

    public void BroadcastStats()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log($"[LEGACY] Master broadcast stats");
        photonView.RPC(
            "SyncFinalStats",
            RpcTarget.All,
            MauPlayer, ManaPlayer, NoPlayer,
            MauNPC, ManaNPC, NoNPC,
            PhotonNetwork.Time
        );
    }

    [PunRPC]
    private void RequestStateSync()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("[LEGACY] Master nhận yêu cầu đồng bộ từ Client");
            BroadcastStats();
        }
    }

    [PunRPC]
    private void SyncStateFromClient(int clientMauPlayer, int clientManaPlayer, int clientNoPlayer, int clientMauNPC, int clientManaNPC, int clientNoNPC)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log($"[LEGACY] Master nhận state từ Client");
        MauPlayer = clientMauNPC;
        ManaPlayer = clientManaNPC;
        NoPlayer = clientNoNPC;
        MauNPC = clientMauPlayer;
        ManaNPC = clientManaPlayer;
        NoNPC = clientNoPlayer;

        BroadcastStats();
    }

    // ==================== CARD EFFECT HELPERS ====================
    private IEnumerator ShowCardEffect(GameObject effectObject, string text, bool resetAnimation)
    {
        if (effectObject != null && effect != null)
        {
            effectObject.SetActive(true);
            effectObject.GetComponent<Text>().text = text;
            yield return effect.FadeAndMoveUp(effectObject);
        }
    }

    private IEnumerator ShowCardDamageSequence(int damage)
    {
        yield return new WaitForSeconds(SKILL_CAST_DELAY);

        if (dameATKPrefadNPC != null)
        {
            dameATKPrefadNPC.SetActive(true);
            Text damageText = dameATKPrefadNPC.transform.Find("txtdame").GetComponent<Text>();
            if (damageText != null)
            {
                damageText.text = damage.ToString();
            }
            yield return new WaitForSeconds(ANIMATION_DURATION);
            dameATKPrefadNPC.SetActive(false);
        }
    }

    // ==================== DEATH & VICTORY ANIMATIONS ====================
    public IEnumerator PlayDeathSequence(bool isPlayerDead)
    {
        if (isPlayerDead)
        {
            PlayAnimationSynced(5, true);
            yield return new WaitForSeconds(1.5f);
            StartCoroutine(DelayedAnimation(0.5f, 6, false));
            yield return new WaitForSeconds(2f);
        }
        else
        {
            PlayAnimationSynced(5, false);
            yield return new WaitForSeconds(1.5f);
            StartCoroutine(DelayedAnimation(0.5f, 6, true));
            yield return new WaitForSeconds(2f);
        }

        Debug.Log("[GAME END] Death sequence completed");
    }

    // ==================== COMBO ATTACK SYSTEM ====================
    public IEnumerator PlayComboAttack(int hitCount)
    {
        Debug.Log($"[COMBO] Starting {hitCount}-hit combo");

        for (int i = 0; i < hitCount; i++)
        {
            PlayAnimationSynced(1, true);
            StartCoroutine(DelayedAnimation(ATTACK_HIT_DELAY, 4, false));
            yield return new WaitForSeconds(COMBO_INTERVAL);

            if (i < hitCount - 1)
            {
                StartCoroutine(DelayedAnimation(0.1f, 0, false));
            }
        }

        StartCoroutine(DelayedAnimation(0.3f, 0, true));
        StartCoroutine(DelayedAnimation(0.3f, 0, false));

        Debug.Log("[COMBO] Combo completed");
    }

    // ==================== BUFF/DEBUFF SYSTEM ====================
    public IEnumerator PlayBuffAnimation(bool isPlayer, string buffType)
    {
        Debug.Log($"[BUFF] Applying {buffType} to {(isPlayer ? "Player" : "Enemy")}");

        PlayAnimationSynced(3, isPlayer);
        yield return new WaitForSeconds(BUFF_DURATION);
        StartCoroutine(DelayedAnimation(0.2f, 0, isPlayer));
    }

    // ==================== SYNCHRONIZED ANIMATION SYSTEM ====================
    public IEnumerator PlaySyncedAttackDefend()
    {
        Debug.Log("[SYNC] Starting synchronized attack-defend sequence");

        PlayAnimationSynced(0, true);
        PlayAnimationSynced(0, false);
        yield return new WaitForSeconds(0.3f);

        PlayAnimationSynced(1, true);
        StartCoroutine(DelayedAnimation(ATTACK_HIT_DELAY - 0.1f, 3, false));
        StartCoroutine(DelayedAnimation(ATTACK_HIT_DELAY + 0.2f, 4, false));

        yield return new WaitForSeconds(ANIMATION_DURATION);

        StartCoroutine(DelayedAnimation(0.2f, 0, true));
        StartCoroutine(DelayedAnimation(0.2f, 0, false));
    }

    // ==================== ULTIMATE SKILL SEQUENCE ====================
    public IEnumerator PlayUltimateSkill()
    {
        Debug.Log("[ULTIMATE] Starting ultimate skill sequence");

        PlayAnimationSynced(3, true);
        yield return new WaitForSeconds(0.8f);

        PlayAnimationSynced(2, true);
        StartCoroutine(DelayedAnimation(0.3f, 4, false));
        StartCoroutine(DelayedAnimation(0.6f, 4, false));
        StartCoroutine(DelayedAnimation(0.9f, 4, false));

        yield return new WaitForSeconds(1.5f);

        StartCoroutine(DelayedAnimation(0.2f, 0, true));
        StartCoroutine(DelayedAnimation(0.2f, 0, false));

        Debug.Log("[ULTIMATE] Ultimate skill completed");
    }

    // ==================== COUNTER ATTACK SYSTEM ====================
    public IEnumerator PlayCounterAttack()
    {
        Debug.Log("[COUNTER] Starting counter attack sequence");

        PlayAnimationSynced(1, false);
        StartCoroutine(DelayedAnimation(ATTACK_HIT_DELAY, 4, true));

        yield return new WaitForSeconds(ANIMATION_DURATION);

        PlayAnimationSynced(2, true);
        StartCoroutine(DelayedAnimation(ATTACK_HIT_DELAY, 4, false));

        yield return new WaitForSeconds(ANIMATION_DURATION);

        StartCoroutine(DelayedAnimation(0.2f, 0, true));
        StartCoroutine(DelayedAnimation(0.2f, 0, false));

        Debug.Log("[COUNTER] Counter attack completed");
    }
}

// ==================== ANIMATION KEY REFERENCE ====================
/*
 * Animation Keys được sử dụng trong Animator Controller:
 * 
 * key = 0: Idle (trạng thái nghỉ)
 * key = 1: Normal Attack (đánh thường)
 * key = 2: Ultimate Attack (chiêu thức đặc biệt khi Nộ đầy)
 * key = 3: Buff/Heal/Shield (hồi máu, tăng giáp, buff)
 * key = 4: Hit/Hurt (nhận sát thương)
 * key = 5: Death (chết)
 * key = 6: Victory (thắng trận)
 * 
 * Lưu ý: Bạn cần đảm bảo Animator Controller có các state tương ứng
 * với các key này để animation hoạt động đúng.
 */