using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using JetBrains.Annotations;
using System;
using UnityEngine.Networking;
using System.Linq;
using Unity.VisualScripting;

public class Active : MonoBehaviour
{
    [Header("UI Object")]
    public Board board;
    //slider player
    public Slider thanhMauSlider;
    public Slider thanhManaSlider;
    public Slider thanhNoSlider;
    //slider NPC
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
    public Animator anmtHealP;
    public Animator anmtHealE;

    public GameObject offBoard;
    public GameObject dameATKPrefad;
    public GameObject dameATKPrefadNPC;

    [Header("UI Information")]

    //trangthaiplayer
    public int inputKiem;
    public int outputKiem;
    public int finalDamageDisplay;
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
    private int sloMauAnDuoc;
    private int sloNoAnDuoc;
    private int sloHutAnDuoc;
    private int sloGiapAnDuoc;
    private int sloManaAnDuoc;
    private int sloKiemAnDuoc;
    public int MauPlayer;
    public int ManaPlayer;
    public int NoPlayer;
    //npc
    public int maxMauNPC;
    public int maxManaNPC;
    public int maxNoNPC;

    public int MauNPC;
    public int ManaNPC;
    public int NoNPC;

    //TextTrangThai
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

    //Gameplay
    public int valueCurrent = 0;
    public Text nangLuong;
    public Text leverPetUser;
    public Text leverEnemyPet;
    public Text namePetUser;
    public Text namePetEnemy;
    public Text dameTypePetUse;
    public Text dameTypePetEnemy;

    public List<CardInfo> cardInfos = new List<CardInfo>();
    public ListCard listCard;
    public Effect effect;
    public ApiLoadRoom apiLoadRoom;
    public GameObject onCard;

    [Header("Cấu hình hệ số")]
    public int GiapPlayer = 0;
    public int GiapNPC = 0;
    public float noGainPercent = 0.5f;
    public float giapPercentPerPiece = 0.4f;
    private int GiapPlayerActiveAtTurn = -1;   // Turn mà giáp Player bắt đầu có hiệu lực
    private int GiapNPCCreatedAtTurn = -1;
    private int GiapNPCActiveAtTurn = -1;
    public int shieldDurationTurns = 3;
    private int GiapPlayerCreatedAtTurn = -1;

    [Header("hệ số tấn công pet")]
    public int attackP;
    public int attackE;

    // ==================== TURN MANAGER INTEGRATED ====================
    [Header("Turn Manager Settings")]
    [SerializeField] private int totalEntities = 2; // Player (0) và NPC (1)
    [SerializeField] private float turnDuration = 14f;
    [SerializeField] private float turnTransitionDelay = 1f;

    // Turn State
    private int currentTurnIndex = 0; // 0 = Player, 1 = NPC
    private int turnNumber = 1;
    private bool isTurnInProgress = false;
    private float currentTurnTime;
    private Coroutine turnTimerCoroutine;

    // Public Properties
    public int CurrentTurnIndex => currentTurnIndex;
    public int TurnNumber => turnNumber;
    public bool IsTurnInProgress => isTurnInProgress;
    public bool IsPlayerTurn => currentTurnIndex == 0;
    public bool IsNPCTurn => currentTurnIndex == 1;
    public float CurrentTurnTime => currentTurnTime;

    // Events
    public event Action<int> OnTurnStart;
    public event Action<int> OnTurnEndInternal;
    public event Action<int, float> OnTurnTimeUpdate;
    public event Action<int> OnTurnTimeout;
    private int lastStealAmount = 0;
    private string lastStealType = "";
    [Header("Game Result")]

    public static Active Instance { get; private set; }

    // ==================== UNITY LIFECYCLE ====================

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

    void Start()
{
    apiLoadRoom = FindFirstObjectByType<ApiLoadRoom>();
    effect = FindFirstObjectByType<Effect>();

    int petUser = PlayerPrefs.GetInt("petUser");
    int petEnemy = PlayerPrefs.GetInt("petEnemy");

    // Đăng ký internal events
    OnTurnStart += HandleTurnStartInternal;
    OnTurnEndInternal += HandleTurnEndInternal;

    // ✅ KHÔNG CẦN GÌ THÊM - Chờ Board.OnBoardReady() gọi
}

    IEnumerator WaitAndStartGame()
    {
        yield return new WaitForSeconds(2f);
        StartGame();
    }

    void Update()
    {
        // Có thể thêm logic khác nếu cần
    }

    // ==================== TURN MANAGEMENT ====================

    public void StartGame()
    {
        if (isTurnInProgress)
        {
            return;
        }

        currentTurnIndex = 0; // Player đi trước
        turnNumber = 1;

        // ✅ RESET STATIC FLAGS CỦA CARD
        CardUI.ResetAllCardsForNewMatch();

        StartTurnInternal();
    }

    private void StartTurnInternal()
    {
        if (isTurnInProgress)
        {
            return;
        }

        isTurnInProgress = true;
        currentTurnTime = turnDuration;


        // Reset animation states
        if (IsPlayerTurn && playerPetAnimator != null)
            playerPetAnimator.SetInteger("key", 0);
        else if (IsNPCTurn && bossPetAnimator != null)
            bossPetAnimator.SetInteger("key", 0);

        OnTurnStart?.Invoke(currentTurnIndex);

    }


    private IEnumerator TurnTimerCoroutine()
    {
        while (currentTurnTime > 0 && isTurnInProgress)
        {
            // Update UI
            if (countdownText != null)
            {
                countdownText.text = Mathf.CeilToInt(currentTurnTime).ToString();
            }

            // Trigger event
            OnTurnTimeUpdate?.Invoke(currentTurnIndex, currentTurnTime);

            yield return new WaitForSeconds(1f);
            currentTurnTime--;
        }

        // Timeout
        if (isTurnInProgress)
        {

            if (countdownText != null)
                countdownText.text = "0";

            OnTurnTimeout?.Invoke(currentTurnIndex);
            yield return new WaitForSeconds(0.5f);
            EndCurrentTurn();
        }
    }

    public void EndCurrentTurn()
    {
        if (!isTurnInProgress)
        {
            return;
        }

        // Dừng timer
        if (turnTimerCoroutine != null)
        {
            StopCoroutine(turnTimerCoroutine);
            turnTimerCoroutine = null;
        }

        isTurnInProgress = false;


        // Trigger events
        OnTurnEndInternal?.Invoke(currentTurnIndex);
        OnTurnEnd?.Invoke(); // Legacy event for Board

        // Chuyển turn
        StartCoroutine(TransitionToNextTurn());
    }

    private IEnumerator TransitionToNextTurn()
    {
        yield return new WaitForSeconds(turnTransitionDelay);

        int previousIndex = currentTurnIndex;
        currentTurnIndex = (currentTurnIndex + 1) % totalEntities;

        if (currentTurnIndex == 0)
        {
            turnNumber++;
        }

        // ✅ THÊM: TRỪ NĂNG LƯỢNG KHI KẾT THÚC TURN PLAYER
        if (previousIndex == 0 && ManagerMatch.Instance != null)
        {
            int currentValue = int.Parse(ManagerMatch.Instance.txtNLUser.text);
            currentValue--;
            ManagerMatch.Instance.txtNLUser.text = currentValue.ToString();

            int userId = PlayerPrefs.GetInt("userId", 1);
            StartCoroutine(APIManager.Instance.GetRequest<UserDTO>(
                APIConfig.DOWN_ENERGY(userId),
                null,
                (error) => Debug.LogError($"[Active] Lỗi trừ năng lượng: {error}")
            ));

            // Kiểm tra năng lượng
            if (board != null && currentValue <= 0)
            {
                board.StartCoroutine(board.GetType()
                    .GetMethod("ShowEnergyWarningAndReturn",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .Invoke(board, new object[] { "Bạn đã hết năng lượng!", true }) as IEnumerator);
            }
        }

        StartTurnInternal();
    }

    // ==================== TURN CONTROL METHODS ====================

    public void PauseTurn()
    {
        if (turnTimerCoroutine != null)
        {
            StopCoroutine(turnTimerCoroutine);
            turnTimerCoroutine = null;
        }
    }

    public void ResumeTurn()
    {
        if (!isTurnInProgress) return;

        if (turnTimerCoroutine != null)
        {
            StopCoroutine(turnTimerCoroutine);
        }
        turnTimerCoroutine = StartCoroutine(TurnTimerCoroutine());
    }

    public void AddTime(float seconds)
    {
        currentTurnTime += seconds;
    }

    public void SetTime(float seconds)
    {
        currentTurnTime = seconds;
    }

    public bool CanEntityAct(int entityIndex)
    {
        return isTurnInProgress && currentTurnIndex == entityIndex;
    }

    private string GetEntityName(int index)
    {
        return index == 0 ? "Player" : "NPC";
    }

    // ==================== INTERNAL EVENT HANDLERS ====================

    private void HandleTurnStartInternal(int entityIndex)
    {
        // ✅ DEBUG: Log trạng thái giáp khi bắt đầu turn

        // Notify Board nếu cần
        if (board != null)
        {
            board.OnTurnStartNotify(entityIndex);
        }
    }



    // ==================== LEGACY METHODS (Backwards Compatibility) ====================

    // Giữ lại để không break code cũ, nhưng redirect sang methods mới
    public void StartCountdown()
    {
    }

    public void StartCountdownNext()
    {
    }

    public void StopCountdown()
    {
        PauseTurn();
    }

    // ==================== GAME LOGIC ====================

    public IEnumerator OutputsParam(string itemType)
    {
        // ========== VIÊN KIẾM (vang Dot) - Hiển thị damage ==========
        if (itemType.Equals("vang Dot") && IsNPCTurn)
        {
            dameATKPrefadNPC.SetActive(true);
            Text dame = dameATKPrefadNPC.transform.Find("txtdame").GetComponent<Text>();

            // ✅ Hiển thị damage gốc → final
            int shieldBlocked = outputKiem - finalDamageDisplay;
            if (shieldBlocked > 0)
            {
                dame.text = $"{finalDamageDisplay}";
            }
            else
            {
                dame.text = finalDamageDisplay.ToString();
            }

            yield return new WaitForSeconds(1f);
            dameATKPrefadNPC.SetActive(false);
            yield break;
        }

        if (itemType.Equals("vang Dot") && IsPlayerTurn)
        {
            dameATKPrefad.SetActive(true);
            Text dame = dameATKPrefad.transform.Find("txtdame").GetComponent<Text>();

            // ✅ Hiển thị damage gốc → final
            int shieldBlocked = outputKiem - finalDamageDisplay;
            if (shieldBlocked > 0)
            {
                dame.text = $"{finalDamageDisplay}";
            }
            else
            {
                dame.text = finalDamageDisplay.ToString();
            }

            yield return new WaitForSeconds(1f);
            dameATKPrefad.SetActive(false);
            yield break;
        }

        // ========== CÁC VIÊN HEAL - Hiển thị giá trị TỔNG ==========

        // Power (do Dot) - Hiển thị outputNo
        if (itemType.Equals("do Dot") && IsNPCTurn)
        {
            healdPowerNPC.SetActive(true);
            healdPowerNPC.gameObject.transform.GetComponent<Text>().text = "+ " + outputNo.ToString();
            yield return effect.FadeAndMoveUp(healdPowerNPC);
            yield break;
        }

        if (itemType.Equals("do Dot") && IsPlayerTurn)
        {
            healdPower.SetActive(true);
            healdPower.gameObject.transform.GetComponent<Text>().text = "+ " + outputNo.ToString();
            yield return effect.FadeAndMoveUp(healdPower);
            yield break;
        }

        // HP (xanh Dot) - Hiển thị outputMau
        if (itemType.Equals("xanh Dot") && IsNPCTurn)
        {
            healdHPNPC.SetActive(true);
            healdHPNPC.gameObject.transform.GetComponent<Text>().text = "+ " + outputMau.ToString();
            yield return effect.FadeAndMoveUp(healdHPNPC);
            yield break;
        }

        if (itemType.Equals("xanh Dot") && IsPlayerTurn)
        {
            healdHP.SetActive(true);
            healdHP.gameObject.transform.GetComponent<Text>().text = "+ " + outputMau.ToString();
            yield return effect.FadeAndMoveUp(healdHP);
            yield break;
        }

        // Mana (xanhduong Dot) - Hiển thị outputMana
        if (itemType.Equals("xanhduong Dot") && IsNPCTurn)
        {
            healdManaNPC.SetActive(true);
            healdManaNPC.gameObject.transform.GetComponent<Text>().text = "+ " + outputMana.ToString();
            yield return effect.FadeAndMoveUp(healdManaNPC);
            yield break;
        }

        if (itemType.Equals("xanhduong Dot") && IsPlayerTurn)
        {
            healdMana.SetActive(true);
            healdMana.gameObject.transform.GetComponent<Text>().text = "+ " + outputMana.ToString();
            yield return effect.FadeAndMoveUp(healdMana);
            yield break;
        }

        // DEF (tim Dot) - Hiển thị outputGiap
        if (itemType.Equals("tim Dot") && IsNPCTurn)
        {
            healdDEFNPC.SetActive(true);
            healdDEFNPC.gameObject.transform.GetComponent<Text>().text = "+ " + outputGiap.ToString();
            yield return effect.FadeAndMoveUp(healdDEFNPC);
            yield break;
        }

        if (itemType.Equals("tim Dot") && IsPlayerTurn)
        {
            healdDEF.SetActive(true);
            healdDEF.gameObject.transform.GetComponent<Text>().text = "+ " + outputGiap.ToString();
            yield return effect.FadeAndMoveUp(healdDEF);
            yield break;
        }

        // ========== STEAL (trang Dot) - Hiển thị hút tài nguyên ==========
        if (itemType.Equals("trang Dot"))
        {
            if (IsPlayerTurn)
            {
                // Player hút từ NPC
                if (lastStealType == "Mana")
                {
                    // Hiển thị NPC bị trừ Mana
                    healdManaNPC.SetActive(true);
                    healdManaNPC.gameObject.transform.GetComponent<Text>().text = "- " + lastStealAmount.ToString();

                    // Hiển thị Player được cộng Mana
                    healdMana.SetActive(true);
                    healdMana.gameObject.transform.GetComponent<Text>().text = "+ " + lastStealAmount.ToString();

                    // Chạy 2 hiệu ứng đồng thời
                    yield return StartCoroutine(RunBothEffects(healdManaNPC, healdMana));
                }
                else if (lastStealType == "No")
                {
                    // Hiển thị NPC bị trừ Nộ
                    healdPowerNPC.SetActive(true);
                    healdPowerNPC.gameObject.transform.GetComponent<Text>().text = "- " + lastStealAmount.ToString();

                    // Hiển thị Player được cộng Nộ
                    healdPower.SetActive(true);
                    healdPower.gameObject.transform.GetComponent<Text>().text = "+ " + lastStealAmount.ToString();

                    // Chạy 2 hiệu ứng đồng thời
                    yield return StartCoroutine(RunBothEffects(healdPowerNPC, healdPower));
                }
            }
            else if (IsNPCTurn)
            {
                // NPC hút từ Player
                if (lastStealType == "Mana")
                {
                    // Hiển thị Player bị trừ Mana
                    healdMana.SetActive(true);
                    healdMana.gameObject.transform.GetComponent<Text>().text = "- " + lastStealAmount.ToString();

                    // Hiển thị NPC được cộng Mana
                    healdManaNPC.SetActive(true);
                    healdManaNPC.gameObject.transform.GetComponent<Text>().text = "+ " + lastStealAmount.ToString();

                    // Chạy 2 hiệu ứng đồng thời
                    yield return StartCoroutine(RunBothEffects(healdMana, healdManaNPC));
                }
                else if (lastStealType == "No")
                {
                    // Hiển thị Player bị trừ Nộ
                    healdPower.SetActive(true);
                    healdPower.gameObject.transform.GetComponent<Text>().text = "- " + lastStealAmount.ToString();

                    // Hiển thị NPC được cộng Nộ
                    healdPowerNPC.SetActive(true);
                    healdPowerNPC.gameObject.transform.GetComponent<Text>().text = "+ " + lastStealAmount.ToString();

                    // Chạy 2 hiệu ứng đồng thời
                    yield return StartCoroutine(RunBothEffects(healdPower, healdPowerNPC));
                }
            }
            yield break;
        }
    }

    private IEnumerator RunBothEffects(GameObject effect1, GameObject effect2)
    {
        Coroutine coroutine1 = StartCoroutine(effect.FadeAndMoveUp(effect1));
        Coroutine coroutine2 = StartCoroutine(effect.FadeAndMoveUp(effect2));

        yield return coroutine1;
        yield return coroutine2;


    }

    public void CalculateOutputs(string tag, int count)
    {
        int currentAttack = IsPlayerTurn ? attackP : attackE;
        int currenthealth = IsPlayerTurn ? attackP : attackE;
        int currentmana = IsPlayerTurn ? maxMana : maxManaNPC;

        switch (tag)
        {
            case "vang Dot":
                int finalDamage;
                bool attackerNoFull = (IsPlayerTurn && NoPlayer >= 100) || (IsNPCTurn && NoNPC >= 100);

                if (attackerNoFull) // ========== ULTIMATE ATTACK ==========
                {
                    if (IsPlayerTurn)
                    {
                        playerPetAnimator.SetInteger("key", 2);
                        bossPetAnimator.SetInteger("key", 4);
                    }
                    else
                    {
                        bossPetAnimator.SetInteger("key", 2);
                        playerPetAnimator.SetInteger("key", 4);
                    }

                    outputKiem = Mathf.RoundToInt(currentAttack * 1.5f * count);

                    if (IsPlayerTurn)
                    {
                        // ✅ LOGIC MỚI: Giáp bị tiêu hao khi block
                        int shieldBlock = Mathf.Min(outputKiem, GiapNPC);
                        GiapNPC -= shieldBlock; // ⭐ GIẢM GIÁP
                        int baseDamage = outputKiem - shieldBlock;

                        // ✅ 2. ÁP DỤNG WEAK/STRONG
                        float damageMultiplier = 1f;

                        // Player có Weak → Tăng damage lên Boss
                        if (ManagerMatch.Instance.uPetsMatch.weaknessValue > 1f)
                        {
                            damageMultiplier = (float)ManagerMatch.Instance.uPetsMatch.weaknessValue;
                        }

                        // Boss có Weak → Giảm damage của Player (vì Boss kháng)
                        if (ManagerMatch.Instance.ePetsMatch.weaknessValue > 1f)
                        {
                            damageMultiplier /= (float)ManagerMatch.Instance.ePetsMatch.weaknessValue;
                        }

                        finalDamage = Mathf.RoundToInt(baseDamage * damageMultiplier);
                        finalDamageDisplay = finalDamage;

                        MauNPC -= finalDamage;
                        NoPlayer -= 100;
                        NoNPC = Mathf.Min(NoNPC + 30, 200);

                        if (ManagerMatch.Instance != null && ManagerMatch.Instance.IsBossBattle())
                        {
                            ManagerMatch.Instance.AddBossDamage(finalDamage);
                        }
                    }
                    else
                    {
                        // ✅ LOGIC MỚI: Giáp bị tiêu hao khi block
                        int shieldBlock = Mathf.Min(outputKiem, GiapPlayer);
                        GiapPlayer -= shieldBlock; // ⭐ GIẢM GIÁP
                        int baseDamage = outputKiem - shieldBlock;

                        float damageMultiplier = 1f;

                        // Boss có Weak → Tăng damage lên Player
                        if (ManagerMatch.Instance.ePetsMatch.weaknessValue > 1f)
                        {
                            damageMultiplier = (float)ManagerMatch.Instance.ePetsMatch.weaknessValue;
                        }

                        // Player có Weak → Giảm damage của Boss (vì Player kháng)
                        if (ManagerMatch.Instance.uPetsMatch.weaknessValue > 1f)
                        {
                            damageMultiplier /= (float)ManagerMatch.Instance.uPetsMatch.weaknessValue;
                        }

                        finalDamage = Mathf.RoundToInt(baseDamage * damageMultiplier);
                        finalDamageDisplay = finalDamage;

                        MauPlayer -= finalDamage;
                        NoNPC -= 100;
                        NoPlayer = Mathf.Min(NoPlayer + 30, 200);
                    }
                }
                else // ========== NORMAL ATTACK ==========
                {
                    if (IsPlayerTurn)
                    {
                        playerPetAnimator.SetInteger("key", 1);
                        bossPetAnimator.SetInteger("key", 4);
                    }
                    else
                    {
                        bossPetAnimator.SetInteger("key", 1);
                        playerPetAnimator.SetInteger("key", 4);
                    }

                    outputKiem = Mathf.RoundToInt(currentAttack * count);

                    if (IsPlayerTurn)
                    {
                        // ✅ 1. TÍNH DAMAGE GỐC (sau khi trừ giáp)
                        int shieldBlock = Mathf.Min(outputKiem, GiapNPC);
                        GiapNPC -= shieldBlock;
                        int baseDamage = outputKiem - shieldBlock;

                        // ✅ 2. ÁP DỤNG WEAK/STRONG
                        float damageMultiplier = 1f;

                        // Player có Weak → Tăng damage lên Boss
                        if (ManagerMatch.Instance.uPetsMatch.weaknessValue > 1f)
                        {
                            damageMultiplier = (float)ManagerMatch.Instance.uPetsMatch.weaknessValue;
                        }

                        // Boss có Weak → Giảm damage của Player (vì Boss kháng)
                        if (ManagerMatch.Instance.ePetsMatch.weaknessValue > 1f)
                        {
                            damageMultiplier /= (float)ManagerMatch.Instance.ePetsMatch.weaknessValue;
                        }

                        finalDamage = Mathf.RoundToInt(baseDamage * damageMultiplier);
                        finalDamageDisplay = finalDamage;


                        MauNPC -= finalDamage;
                        NoNPC = Mathf.Min(NoNPC + 15, 200);

                        if (ManagerMatch.Instance != null && ManagerMatch.Instance.IsBossBattle())
                        {
                            ManagerMatch.Instance.AddBossDamage(finalDamage);
                        }
                    }
                    else
                    {
                        // ✅ 1. TÍNH DAMAGE GỐC (sau khi trừ giáp)
                        int shieldBlock = Mathf.Min(outputKiem, GiapPlayer);
                        GiapPlayer -= shieldBlock;
                        int baseDamage = outputKiem - shieldBlock;

                        // ✅ 2. ÁP DỤNG WEAK/STRONG
                        float damageMultiplier = 1f;

                        // Boss có Weak → Tăng damage lên Player
                        if (ManagerMatch.Instance.ePetsMatch.weaknessValue > 1f)
                        {
                            damageMultiplier = (float)ManagerMatch.Instance.ePetsMatch.weaknessValue;
                        }

                        // Player có Weak → Giảm damage của Boss (vì Player kháng)
                        if (ManagerMatch.Instance.uPetsMatch.weaknessValue > 1f)
                        {
                            damageMultiplier /= (float)ManagerMatch.Instance.uPetsMatch.weaknessValue;
                        }

                        finalDamage = Mathf.RoundToInt(baseDamage * damageMultiplier);
                        finalDamageDisplay = finalDamage;


                        MauPlayer -= finalDamage;
                        NoPlayer = Mathf.Min(NoPlayer + 15, 200);
                    }
                }

                // ✅ CLAMP giáp về 0 nếu âm
                GiapPlayer = Mathf.Max(0, GiapPlayer);
                GiapNPC = Mathf.Max(0, GiapNPC);

                MauPlayer = Mathf.Clamp(MauPlayer, 0, maxMau);
                MauNPC = Mathf.Clamp(MauNPC, 0, maxMauNPC);
                valueCurrent = outputKiem;
                break;

            case "tim Dot":
                // ✅ TÍNH GIÁP DựA VÀO 5% MAX MÁU
                int shieldPerPiece = IsPlayerTurn
                    ? Mathf.RoundToInt(attackP * giapPercentPerPiece)
                    : Mathf.RoundToInt(attackE * giapPercentPerPiece);

                outputGiap = shieldPerPiece * count;

                if (IsPlayerTurn)
                {
                    playerPetAnimator.SetInteger("key", 3);
                    anmtHealP.gameObject.SetActive(true);
                    GiapPlayer += outputGiap;

                    // ✅ GHI NHẬN TURN ĂN GIÁP
                    GiapPlayerCreatedAtTurn = turnNumber;
                    // Giáp sẽ có hiệu lực từ turn kế tiếp (turn của đối thủ)
                    GiapPlayerActiveAtTurn = turnNumber;

                }
                else
                {
                    bossPetAnimator.SetInteger("key", 3);
                    anmtHealE.gameObject.SetActive(true);
                    GiapNPC += outputGiap;

                    // ✅ GHI NHẬN TURN ĂN GIÁP
                    GiapNPCCreatedAtTurn = turnNumber;
                    GiapPlayerActiveAtTurn = turnNumber;

                }
                valueCurrent = outputGiap;
                break;

            case "do Dot":
                outputNo = 20 * count;
                if (IsPlayerTurn)
                {
                    playerPetAnimator.SetInteger("key", 3);
                    anmtHealP.gameObject.SetActive(true);
                    NoPlayer += outputNo;
                }
                else
                {
                    bossPetAnimator.SetInteger("key", 3);
                    anmtHealE.gameObject.SetActive(true);
                    NoNPC += outputNo;
                }
                NoPlayer = Mathf.Clamp(NoPlayer, 0, 200);
                NoNPC = Mathf.Clamp(NoNPC, 0, 200);
                valueCurrent = outputNo;
                break;

            case "trang Dot":
                outputHut = Mathf.RoundToInt(currentAttack * 0.1f * count);

                // Random chọn hút Mana (1) hoặc Nộ (2)
                int stealType = UnityEngine.Random.Range(1, 3);

                if (IsPlayerTurn)
                {
                    playerPetAnimator.SetInteger("key", 3);
                    anmtHealP.gameObject.SetActive(true);
                    bossPetAnimator.SetInteger("key", 4);

                    if (stealType == 1) // Hút Nộ
                    {
                        int actualStealNo = Mathf.Min(outputHut, NoNPC);
                        NoNPC -= actualStealNo;
                        NoPlayer += actualStealNo;

                        lastStealAmount = actualStealNo;
                        lastStealType = "No";

                    }
                    else // Hút Mana
                    {
                        int actualStealMana = Mathf.Min(outputHut, ManaNPC);
                        ManaNPC -= actualStealMana;
                        ManaPlayer += actualStealMana;

                        lastStealAmount = actualStealMana;
                        lastStealType = "Mana";

                    }
                }
                else // NPC Turn
                {
                    bossPetAnimator.SetInteger("key", 3);
                    anmtHealE.gameObject.SetActive(true);
                    playerPetAnimator.SetInteger("key", 4);

                    if (stealType == 1) // Hút Nộ
                    {
                        int actualStealNo = Mathf.Min(outputHut, NoPlayer);
                        NoPlayer -= actualStealNo;
                        NoNPC += actualStealNo;

                        lastStealAmount = actualStealNo;
                        lastStealType = "No";

                    }
                    else // Hút Mana
                    {
                        int actualStealMana = Mathf.Min(outputHut, ManaPlayer);
                        ManaPlayer -= actualStealMana;
                        ManaNPC += actualStealMana;

                        lastStealAmount = actualStealMana;
                        lastStealType = "Mana";

                    }
                }

                // Clamp values
                NoPlayer = Mathf.Clamp(NoPlayer, 0, 200);
                NoNPC = Mathf.Clamp(NoNPC, 0, 200);
                ManaPlayer = Mathf.Clamp(ManaPlayer, 0, maxMana);
                ManaNPC = Mathf.Clamp(ManaNPC, 0, maxManaNPC);

                valueCurrent = outputHut;
                break;

            case "xanh Dot":
                outputMau = Mathf.RoundToInt(currenthealth * 0.2f * count);
                if (IsPlayerTurn)
                {
                    playerPetAnimator.SetInteger("key", 3);
                    anmtHealP.gameObject.SetActive(true);
                    MauPlayer += outputMau;
                }
                else
                {
                    bossPetAnimator.SetInteger("key", 3);
                    anmtHealE.gameObject.SetActive(true);
                    MauNPC += outputMau;
                }
                MauPlayer = Mathf.Clamp(MauPlayer, 0, maxMau);
                MauNPC = Mathf.Clamp(MauNPC, 0, maxMauNPC);
                valueCurrent = outputMau;
                break;

            case "xanhduong Dot":
                outputMana = Mathf.RoundToInt(currentmana * 0.05f * count);
                if (IsPlayerTurn)
                {
                    playerPetAnimator.SetInteger("key", 3);
                    anmtHealP.gameObject.SetActive(true);
                    ManaPlayer += outputMana;
                }
                else
                {
                    bossPetAnimator.SetInteger("key", 3);
                    anmtHealE.gameObject.SetActive(true);
                    ManaNPC += outputMana;
                }
                ManaPlayer = Mathf.Clamp(ManaPlayer, 0, maxMana);
                ManaNPC = Mathf.Clamp(ManaNPC, 0, maxManaNPC);
                valueCurrent = outputMana;
                break;
        }

        UpdateSlider();
    }

    /// <summary>
    /// ✅ Kiểm tra giáp có hiệu lực không
    /// Logic: Giáp có hiệu lực nếu:
    /// - Đã được tạo ở turn trước (activeAtTurn <= currentTurn)
    /// - Chưa hết hạn (currentTurn <= activeAtTurn, tức chỉ 1 turn của đối thủ)
    /// </summary>
    private bool IsShieldActive(bool checkPlayer)
    {
        bool notExpired = turnNumber <= GiapPlayerCreatedAtTurn + shieldDurationTurns;
        return (GiapPlayer > 0) && notExpired;
    }


    // ==================== XÓA GIÁP KHI HẾT HIỆU LỰC ====================

    /// <summary>
    /// ✅ Thêm vào HandleTurnEndInternal để xóa giáp hết hạn
    /// </summary>
    private void HandleTurnEndInternal(int entityIndex)
    {
        // Reset outputs
        resetOutput();
        if (turnNumber > GiapPlayerCreatedAtTurn + shieldDurationTurns)
        {
            GiapPlayer = 0;
        }
        // ✅ XÓA GIÁP HẾT HIỆU LỰC
        // Nếu kết thúc turn của Player, kiểm tra giáp NPC có hết hạn không
        if (entityIndex == 0) // Player turn ended
        {
            if (GiapNPCActiveAtTurn > 0 && turnNumber == GiapNPCActiveAtTurn)
            {
                GiapNPC = 0;
                GiapNPCCreatedAtTurn = -1;
                GiapNPCActiveAtTurn = -1;
            }
        }
        // Nếu kết thúc turn của NPC, kiểm tra giáp Player có hết hạn không
        else if (entityIndex == 1) // NPC turn ended
        {
            if (GiapPlayerActiveAtTurn > 0 && turnNumber == GiapPlayerActiveAtTurn)
            {
                GiapPlayer = 0;
                GiapPlayerCreatedAtTurn = -1;
                GiapPlayerActiveAtTurn = -1;
            }
        }

    }

    public IEnumerator waitsomeS()
    {
        yield return new WaitForSeconds(1f);
    }

    public void SummaryOutputs()
    {
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

    public void IncreaseNoPlayer(CardInfo cardInfo)
    {
        if (cardInfo.IdCard == 2)
        {
            NoPlayer += cardInfo.Value;
            NoPlayer = Mathf.Clamp(NoPlayer, 0, maxNo);
        }
        else if (cardInfo.IdCard == 3)
        {
            MauPlayer += cardInfo.Value;
            MauPlayer = Mathf.Clamp(MauPlayer, 0, maxMau);
        }
        else if (cardInfo.IdCard == 1)
        {
            ManaPlayer += cardInfo.Value;
            ManaPlayer = Mathf.Clamp(ManaPlayer, 0, maxMana);
        }
        else if (ManaPlayer >= cardInfo.ConditionUse && cardInfo.IdCard >= 4)
        {
            ManaPlayer -= cardInfo.ConditionUse;
            ManaPlayer = Mathf.Clamp(ManaPlayer, 0, maxMau);
            MauNPC -= cardInfo.Value;
        }
        else
        {
        }

        UpdateSlider();
    }

    public void UpdateSlider()
    {
        // Clamp values
        MauPlayer = Mathf.Max(0, MauPlayer);
        NoPlayer = Mathf.Max(0, NoPlayer);
        ManaPlayer = Mathf.Max(0, ManaPlayer);
        MauNPC = Mathf.Max(0, MauNPC);
        NoNPC = Mathf.Max(0, NoNPC);
        ManaNPC = Mathf.Max(0, ManaNPC);

        float smoothTime = 0.15f;

        // ✅ DÙNG LEANTWEEN (TỰ ĐỘNG CANCEL NẾU GỌI LẠI)
        if (thanhMauSlider != null)
        {
            LeanTween.cancel(thanhMauSlider.gameObject);
            LeanTween.value(thanhMauSlider.gameObject, thanhMauSlider.value, MauPlayer, smoothTime)
                .setOnUpdate((float val) => thanhMauSlider.value = val);
        }
        if (textMauPlayer != null)
            textMauPlayer.text = $"{MauPlayer}/{maxMau}";

        if (thanhNoSlider != null)
        {
            LeanTween.cancel(thanhNoSlider.gameObject);
            LeanTween.value(thanhNoSlider.gameObject, thanhNoSlider.value, NoPlayer, smoothTime)
                .setOnUpdate((float val) => thanhNoSlider.value = val);
        }
        if (textNoPlayer != null)
            textNoPlayer.text = $"{NoPlayer}/{maxNo}";

        if (thanhManaSlider != null)
        {
            LeanTween.cancel(thanhManaSlider.gameObject);
            LeanTween.value(thanhManaSlider.gameObject, thanhManaSlider.value, ManaPlayer, smoothTime)
                .setOnUpdate((float val) => thanhManaSlider.value = val);
        }
        if (textManaPlayer != null)
            textManaPlayer.text = $"{ManaPlayer}/{maxMana}";

        // Tương tự cho NPC...
        if (thanhMauNPC != null)
        {
            LeanTween.cancel(thanhMauNPC.gameObject);
            LeanTween.value(thanhMauNPC.gameObject, thanhMauNPC.value, MauNPC, smoothTime)
                .setOnUpdate((float val) => thanhMauNPC.value = val);
        }
        if (textMauNPC != null)
            textMauNPC.text = $"{MauNPC}/{maxMauNPC}";

        if (thanhNoNPC != null)
        {
            LeanTween.cancel(thanhNoNPC.gameObject);
            LeanTween.value(thanhNoNPC.gameObject, thanhNoNPC.value, NoNPC, smoothTime)
                .setOnUpdate((float val) => thanhNoNPC.value = val);
        }
        if (textNoNPC != null)
            textNoNPC.text = $"{NoNPC}/{maxNoNPC}";

        if (thanhManaNPC != null)
        {
            LeanTween.cancel(thanhManaNPC.gameObject);
            LeanTween.value(thanhManaNPC.gameObject, thanhManaNPC.value, ManaNPC, smoothTime)
                .setOnUpdate((float val) => thanhManaNPC.value = val);
        }
        if (textManaNPC != null)
            textManaNPC.text = $"{ManaNPC}/{maxManaNPC}";
    }


    private void OnDestroy()
    {
        if (turnTimerCoroutine != null)
        {
            StopCoroutine(turnTimerCoroutine);
        }

        OnTurnStart -= HandleTurnStartInternal;
        OnTurnEndInternal -= HandleTurnEndInternal;
    }

    public IEnumerator SetAnimationForItem(string itemType)
    {
        switch (itemType)
        {
            case "vang Dot":
                bool attackerNoFull = (IsPlayerTurn && NoPlayer >= 100) || (IsNPCTurn && NoNPC >= 100);
                if (attackerNoFull)
                {
                    if (IsPlayerTurn && playerPetAnimator != null)
                    {
                        playerPetAnimator.SetInteger("key", 2);
                        bossPetAnimator.SetInteger("key", 4);
                    } // Ultimate attack
                    else if (IsNPCTurn && bossPetAnimator != null)
                    {
                        bossPetAnimator.SetInteger("key", 2);
                        playerPetAnimator.SetInteger("key", 4);
                    }

                    yield return new WaitForSeconds(2f);
                }
                else
                {
                    if (IsPlayerTurn && playerPetAnimator != null)
                        playerPetAnimator.SetInteger("key", 1); // Normal attack
                    else if (IsNPCTurn && bossPetAnimator != null)
                        bossPetAnimator.SetInteger("key", 1);

                    yield return new WaitForSeconds(1f);
                }

                break;

            case "tim Dot":
            case "do Dot":
            case "xanh Dot":
            case "xanhduong Dot":
                // Tất cả heal items đều dùng animation key = 3
                if (IsPlayerTurn && playerPetAnimator != null)
                {
                    playerPetAnimator.SetInteger("key", 3);
                    anmtHealP.gameObject.SetActive(true);
                }

                else if (IsNPCTurn && bossPetAnimator != null)
                {
                    bossPetAnimator.SetInteger("key", 3);
                    anmtHealE.gameObject.SetActive(true);
                }

                break;

            case "trang Dot":
                // Steal animation
                if (IsPlayerTurn)
                {
                    if (playerPetAnimator != null)
                    {
                        playerPetAnimator.SetInteger("key", 3);
                        anmtHealP.gameObject.SetActive(true);
                    }

                    if (bossPetAnimator != null)
                        bossPetAnimator.SetInteger("key", 4); // Bị hút
                }
                else
                {
                    if (bossPetAnimator != null)
                    {
                        anmtHealE.gameObject.SetActive(true);
                        bossPetAnimator.SetInteger("key", 3);
                    }

                    if (playerPetAnimator != null)
                        playerPetAnimator.SetInteger("key", 4); // Bị hút
                }
                break;
        }
    }


    public void CalculateOutputsWithoutAnimation(string tag, int count)
    {
        int currentAttack = IsPlayerTurn ? attackP : attackE;
        int currenthealth = IsPlayerTurn ? attackP : attackE;
        int currentmana = IsPlayerTurn ? maxMana : maxManaNPC;

        switch (tag)
        {

            case "tim Dot":
                // ✅ TÍNH GIÁP DựA VÀO 5% MAX MÁU
                int shieldPerPieceNoAnim = IsPlayerTurn
                    ? Mathf.RoundToInt(attackP * giapPercentPerPiece)
                    : Mathf.RoundToInt(attackE * giapPercentPerPiece);

                outputGiap = shieldPerPieceNoAnim * count;

                if (IsPlayerTurn)
                {
                    GiapPlayer += outputGiap;
                    GiapPlayerCreatedAtTurn = turnNumber;
                    GiapPlayerActiveAtTurn = turnNumber;
                }
                else
                {
                    GiapNPC += outputGiap;
                    GiapNPCCreatedAtTurn = turnNumber;
                    GiapPlayerActiveAtTurn = turnNumber;
                }
                valueCurrent = outputGiap;
                break;

            case "do Dot":
                outputNo = 20 * count;
                if (IsPlayerTurn)
                {
                    // ❌ BỎ animation
                    // playerPetAnimator.SetInteger("key", 3);
                    NoPlayer += outputNo;
                }
                else
                {
                    // ❌ BỎ animation
                    // bossPetAnimator.SetInteger("key", 3);
                    NoNPC += outputNo;
                }
                NoPlayer = Mathf.Clamp(NoPlayer, 0, 200);
                NoNPC = Mathf.Clamp(NoNPC, 0, 200);
                valueCurrent = outputNo;
                break;

            case "trang Dot":
                outputHut = Mathf.RoundToInt(currentAttack * 0.1f * count);
                int stealType = UnityEngine.Random.Range(1, 3);

                if (IsPlayerTurn)
                {
                    // ❌ BỎ animation
                    // playerPetAnimator.SetInteger("key", 3);
                    // bossPetAnimator.SetInteger("key", 4);

                    if (stealType == 1)
                    {
                        int actualStealNo = Mathf.Min(outputHut, NoNPC);
                        NoNPC -= actualStealNo;
                        NoPlayer += actualStealNo;
                        lastStealAmount = actualStealNo;
                        lastStealType = "No";
                    }
                    else
                    {
                        int actualStealMana = Mathf.Min(outputHut, ManaNPC);
                        ManaNPC -= actualStealMana;
                        ManaPlayer += actualStealMana;
                        lastStealAmount = actualStealMana;
                        lastStealType = "Mana";
                    }
                }
                else
                {
                    // ❌ BỎ animation
                    // bossPetAnimator.SetInteger("key", 3);
                    // playerPetAnimator.SetInteger("key", 4);

                    if (stealType == 1)
                    {
                        int actualStealNo = Mathf.Min(outputHut, NoPlayer);
                        NoPlayer -= actualStealNo;
                        NoNPC += actualStealNo;
                        lastStealAmount = actualStealNo;
                        lastStealType = "No";
                    }
                    else
                    {
                        int actualStealMana = Mathf.Min(outputHut, ManaPlayer);
                        ManaPlayer -= actualStealMana;
                        ManaNPC += actualStealMana;
                        lastStealAmount = actualStealMana;
                        lastStealType = "Mana";
                    }
                }

                NoPlayer = Mathf.Clamp(NoPlayer, 0, 200);
                NoNPC = Mathf.Clamp(NoNPC, 0, 200);
                ManaPlayer = Mathf.Clamp(ManaPlayer, 0, maxMana);
                ManaNPC = Mathf.Clamp(ManaNPC, 0, maxManaNPC);
                valueCurrent = outputHut;
                break;

            case "xanh Dot":
                outputMau = Mathf.RoundToInt(currenthealth * 0.2f * count);
                if (IsPlayerTurn)
                {
                    // ❌ BỎ animation
                    // playerPetAnimator.SetInteger("key", 3);
                    MauPlayer += outputMau;
                }
                else
                {
                    // ❌ BỎ animation
                    // bossPetAnimator.SetInteger("key", 3);
                    MauNPC += outputMau;
                }
                MauPlayer = Mathf.Clamp(MauPlayer, 0, maxMau);
                MauNPC = Mathf.Clamp(MauNPC, 0, maxMauNPC);
                valueCurrent = outputMau;
                break;

            case "xanhduong Dot":
                outputMana = Mathf.RoundToInt(currentmana * 0.05f * count);
                if (IsPlayerTurn)
                {
                    // ❌ BỎ animation
                    // playerPetAnimator.SetInteger("key", 3);
                    ManaPlayer += outputMana;
                }
                else
                {
                    // ❌ BỎ animation
                    // bossPetAnimator.SetInteger("key", 3);
                    ManaNPC += outputMana;
                }
                ManaPlayer = Mathf.Clamp(ManaPlayer, 0, maxMana);
                ManaNPC = Mathf.Clamp(ManaNPC, 0, maxManaNPC);
                valueCurrent = outputMana;
                break;
        }

        UpdateSlider();
    }

    public void OnBoardReady()
{
    Debug.Log("[Active] Board ready, showing YOUR TURN first!");
    StartCoroutine(ShowYourTurnThenStartGame());
}

private IEnumerator ShowYourTurnThenStartGame()
{
    // ✅ 1. HIỂN THỊ "YOUR TURN"
    if (board != null && board.txtYourTurn != null)
    {
        board.txtYourTurn.SetActive(true);
        
        // Đảm bảo chữ luôn nằm trên cùng
        var turnCanvas = board.txtYourTurn.GetComponent<Canvas>();
        if (turnCanvas == null) turnCanvas = board.txtYourTurn.AddComponent<Canvas>();
        turnCanvas.overrideSorting = true;
        turnCanvas.sortingOrder = 99;
        
        // Chạy hiệu ứng chữ
        if (board.yourTurnEffect != null)
        {
            yield return StartCoroutine(board.yourTurnEffect.PlayEffect());
        }
        else
        {
            yield return new WaitForSeconds(1.5f); // Fallback nếu không có effect
        }
        
        board.txtYourTurn.SetActive(false);
    }
    
    yield return new WaitForSeconds(0.2f);
    
    // ✅ 2. HIỂN THỊ BOARD (FADE IN TẤT CẢ VIÊN)
    if (board != null && board.allDots != null)
    {
        for (int i = 0; i < board.width; i++)
        {
            for (int j = 0; j < board.height; j++)
            {
                if (board.allDots[i, j] != null)
                {
                    GameObject dot = board.allDots[i, j];
                    dot.SetActive(true);
                    
                    // Fade in effect
                    SpriteRenderer sr = dot.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        Color c = sr.color;
                        c.a = 0f;
                        sr.color = c;
                        LeanTween.alpha(dot, 1f, 0.3f)
                            .setDelay(i * 0.02f + j * 0.02f)
                            .setEaseOutQuad();
                    }
                    else
                    {
                        dot.SetActive(true);
                    }
                }
            }
        }
    }
    
    yield return new WaitForSeconds(0.5f);
    
    // ✅ 3. BẮT ĐẦU GAME
    Debug.Log("[Active] Starting game after YOUR TURN!");
    StartGame();
}
}