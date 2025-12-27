using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal.Internal;

public enum GameState
{
    wait, move
}

[Serializable]
public class SubmitDamageResponse
{
    public bool success;
    public string message;
    public int damageDealt;
}

[System.Serializable]
public class RewardStone
{
    public int level;      // 1-7
    public string element; // Fire, Water, Earth, Wind, Electric
    public int quantity;
}

[System.Serializable]
public class GameReward
{
    public List<RewardStone> stones = new List<RewardStone>();
    public bool receivedPet = false;
    public bool win = true;
    public string petElement;
    public int petId;
    public int requestAttack;
    public int bonusGold = 0;
}

public class Board : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject[] dots;
    public GameState currentState = GameState.move;
    public GameObject destructionEntryPrefab;

    [Header("UI information")]
    public int width;
    public int height;
    public int offSet;
    private int destroyedCount = 0;
    private bool isDestroyingMatches = false;
    public bool hasDestroyedThisTurn = false;

    [Header("UI Object")]
    private BackGroundTitle[,] allTiles;
    public GameObject[,] allDots;
    private FindMatches findMaches;
    private Dictionary<string, int> destroyedCountByTag = new Dictionary<string, int>();
    public Active active;
    private Coroutine stableBoardCheckCoroutine;

    public GameObject destructionCountPanel;
    private Dictionary<string, Sprite> itemIcons = new Dictionary<string, Sprite>();
    public Sprite[] pieces;
    public GameObject loading;
    public Api api;
    public NotifyWin notifyWin;
    public GameObject load;
    public bool enableAutoMove = true;
    public GameObject imgTurnE;
    public GameObject imgTurnP;
    public GameObject txtYourTurn;
    public YourTurnEffect yourTurnEffect;
    private bool isProcessingUI = false;
    private bool isAutoMoveInProgress = false;

    [Header("===== GAME RESULT SYSTEM =====")]
    [Header("Result UI References")]
    public GameObject panelResult;
    public GameObject resultBackground;
    public GameObject rewards;
    public GameObject anmtRW;
    public GameObject listReward;
    public GameObject itemRewardStone;
    public GameObject itemRewardPet;
    public GameObject itemRewardCT;

    public GameObject itemRewardGold;
    public GameObject itemRewardEXP;
    public Text txtResultTitle;
    public Button btnGet;

    [Header("Reward Settings - 5 Elements x 7 Levels")]
    [Tooltip("Fire Stones: Index 0-6 = Lv1-7")]
    public Sprite[] fireStones = new Sprite[7];
    [Tooltip("Water Stones: Index 0-6 = Lv1-7")]
    public Sprite[] waterStones = new Sprite[7];
    [Tooltip("Earth Stones: Index 0-6 = Lv1-7")]
    public Sprite[] earthStones = new Sprite[7];
    [Tooltip("Wind Stones: Index 0-6 = Lv1-7")]
    public Sprite[] windStones = new Sprite[7];
    [Tooltip("Electric Stones: Index 0-6 = Lv1-7")]
    public Sprite[] electricStones = new Sprite[7];

    [Header("Game Result Data")]
    private bool isGameOver = false;
    private string enemyPetElement;
    private int enemyPetId;
    private int currentCount;
    private int requestPass;
    private float lastAutoMoveTime = 0f;
    private const float AUTO_MOVE_COOLDOWN = 1.5f;
    private bool isBossBattle = false;
    [Header("Card Settings")]
    public GameObject cardPrefab;          // Prefab Card
    public Transform cardContainer;        // Transform "Board" chứa cards
    public int maxCardsInHand = 5;         // Số thẻ tối đa

    [Header("Card Display")]
    public float cardSpacing = 120f;       // Khoảng cách giữa các thẻ
    public float cardYPosition = 0f;       // Vị trí Y của thẻ

    [Header("Animation")]
    public float cardAnimDuration = 0.3f;

    private List<CardData> selectedCards = new List<CardData>();
    private List<GameObject> cardsInHand = new List<GameObject>();
    public CardData cardData;
    private int HOTTURN = 10;
    private int SUBHOTTURN = 5;
    [Header("Energy System")]
    [Tooltip("Panel thông báo hết năng lượng")]
    public GameObject energyWarningPanel;

    [Tooltip("Text hiển thị thông báo")]
    public Text energyWarningText;

    [Tooltip("Button OK để đóng thông báo")]
    public Button energyWarningButton;

    private bool hasShownEnergyWarning = false;
    private int lastCheckedEnergy = -1;
    [Header("Audio Settings")]
    private AudioSettingsManager audioSettingsManager;
    [Header("Background Settings")]
    public Image boardBackground;
    [Header("Board State")]
    private bool isBoardReady = false; // ✅ THÊM BIẾN NÀY

    public bool IsBoardReady => isBoardReady; // ✅ THÊM PROPERTY NÀY

    public static Board Instance { get; private set; }

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

    /// <summary>
    /// ✅ LOAD BACKGROUND THEO GROUP INDEX
    /// </summary>
    private void LoadGroupBackground()
    {
        if (boardBackground == null)
        {
            Debug.LogWarning("[Board] Board background Image not assigned!");
            return;
        }

        string bgPath;

        // ✅ BOSS THẾ GIỚI: Load background đặc biệt
        if (isBossBattle)
        {
            bgPath = "Image/groupBG/group5";
            Debug.Log("[Board] Loading Boss World background");
        }
        else
        {
            // PVP: Load theo group index
            int groupIndex = PlayerPrefs.GetInt("SelectedGroupIndex", 0);
            bgPath = $"Image/groupBG/group{groupIndex}";
            Debug.Log($"[Board] Loading PVP background for group {groupIndex}");
        }

        Sprite bgSprite = Resources.Load<Sprite>(bgPath);

        if (bgSprite != null)
        {
            boardBackground.sprite = bgSprite;
            Debug.Log($"[Board] ✓ Loaded background: {bgPath}");
        }
        else
        {
            Debug.LogWarning($"[Board] Background not found at: {bgPath}");
        }
    }

    void Start()
    {
        yourTurnEffect = txtYourTurn.GetComponent<YourTurnEffect>();
        findMaches = FindFirstObjectByType<FindMatches>();
        active = FindFirstObjectByType<Active>();

        LoadAudioSettings();
    }
    void LoadAudioSettings()
    {
        // Tìm AudioSettingsManager trong scene (nếu có)
        audioSettingsManager = FindObjectOfType<AudioSettingsManager>();

        // Load settings từ PlayerPrefs
        AudioSettings settings = AudioSettingsManager.GetSavedSettings();

        // Áp dụng cho AudioManager
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBGMVolume(settings.bgmVolume * settings.masterVolume);
            AudioManager.Instance.SetSFXVolume(settings.sfxVolume * settings.masterVolume);

            Debug.Log($"[Board] Audio settings loaded: Master={settings.masterVolume}, BGM={settings.bgmVolume}, SFX={settings.sfxVolume}");
        }
        else
        {
            Debug.LogWarning("[Board] AudioManager not found!");
        }
    }

    /// <summary>
    /// ✅ GỌI METHOD NÀY SAU KHI MANAGERMATCH ĐÃ LOAD XONG DATA
    /// </summary>
    public void InitializeCards()
    {
        LoadGroupBackground();
        // ✅ KIỂM TRA BOSS BATTLE
        isBossBattle = PlayerPrefs.GetString("IsBossBattle", "false") == "true";
        Debug.Log($"[Board] IsBossBattle: {isBossBattle}");

        // ✅ Load enemy pet info - ƯU TIÊN BOSS ELEMENT NẾU CÓ
        if (isBossBattle && PlayerPrefs.HasKey("BossElementType"))
        {
            enemyPetElement = PlayerPrefs.GetString("BossElementType", "Fire");
        }
        else
        {
            enemyPetElement = PlayerPrefs.GetString("BossElementType", "Fire");
        }
        // LoadCardsFromPlayerPrefs();
        enemyPetId = PlayerPrefs.GetInt("SelectedPetId", 1);
        currentCount = PlayerPrefs.GetInt("count", 0);
        requestPass = PlayerPrefs.GetInt("requestPass", 5);


        // Hide result panel initially
        if (panelResult != null)
        {
            panelResult.SetActive(false);
        }

        pieces = new Sprite[dots.Length];
        for (int i = 0; i < dots.Length; i++)
        {
            SpriteRenderer spriteRenderer = dots[i].GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                pieces[i] = spriteRenderer.sprite;
            }
            else
            {
                pieces[i] = null;
            }
        }

        itemIcons = new Dictionary<string, Sprite>
        {
            { "vang Dot", pieces[2] },
            { "xanhduong Dot", pieces[5] },
            { "do Dot", pieces[3]},
            { "xanh Dot", pieces[4] },
            { "trang Dot", pieces[1] },
            { "tim Dot", pieces[0] }
        };

        if (active != null)
        {
            active.OnTurnStart += HandleTurnStart;
            active.OnTurnEnd += HandleTurnEnd;
        }

        allTiles = new BackGroundTitle[width, height];
        allDots = new GameObject[width, height];
        StartCoroutine(setUp());

        if (cardContainer != null)
        {
            cardContainer.gameObject.SetActive(!isBossBattle);
        }

        // Load cards như bình thường (sẽ không hiện nếu container đã bị ẩn)
        if (!isBossBattle)
        {
            if (cardData != null)
            {
                CreateCardHT(cardData);
            }
            LoadCardsFromPlayerPrefs();
        }
    }

    void Update()
    {
        // ✅ THÊM CHECK isGameOver
        if (isGameOver) return;

        if (enableAutoMove &&
            active != null &&
            active.IsNPCTurn &&
            active.IsTurnInProgress &&
            !isAutoMoveInProgress &&
            !isProcessingUI &&
            !hasDestroyedThisTurn &&
            currentState == GameState.move &&
            Time.time - lastAutoMoveTime >= AUTO_MOVE_COOLDOWN)
        {
            isAutoMoveInProgress = true;
            lastAutoMoveTime = Time.time;
            StartCoroutine(AutoMoveCoroutine());
        }
    }

    // ==================== EVENT HANDLERS ====================

    public void OnTurnStartNotify(int entityIndex)
    {
        currentState = GameState.move;
        isAutoMoveInProgress = false;
        isProcessingUI = false;
        hasDestroyedThisTurn = false;
        lastAutoMoveTime = Time.time;
        StartCoroutine(UpdateTurnUI(entityIndex));
    }

    private void HandleTurnStart(int entityIndex)
    {
    }

    private void HandleTurnEnd()
    {
        ResetMoveCounters();
    }

    // ==================== UI UPDATE ====================

    private IEnumerator UpdateTurnUI(int entityIndex)
    {
        bool isPlayer = (entityIndex == 0);

        imgTurnP.SetActive(isPlayer);
        imgTurnE.SetActive(!isPlayer);

        if (isPlayer)
        {
            // ✅ TURN ĐẦU TIÊN: Không hiển thị YOUR TURN nữa (đã hiện lúc đầu)
            // ✅ CÁC TURN SAU: Vẫn hiển thị bình thường

            if (active != null && active.TurnNumber > 1) // Chỉ hiện từ turn 2 trở đi
            {
                if (active != null) active.PauseTurn();
                currentState = GameState.wait;
                HideAllItems();

                var turnCanvas = txtYourTurn.GetComponent<Canvas>();
                if (turnCanvas == null) turnCanvas = txtYourTurn.AddComponent<Canvas>();
                turnCanvas.overrideSorting = true;
                turnCanvas.sortingOrder = 99;

                txtYourTurn.SetActive(true);

                if (yourTurnEffect != null)
                    yield return StartCoroutine(yourTurnEffect.PlayEffect());

                txtYourTurn.SetActive(false);
                ShowItems();
            }

            currentState = GameState.move;
            if (active != null) active.ResumeTurn();
        }
        else
        {
            txtYourTurn.SetActive(false);
            if (active != null) active.ResumeTurn();
        }
    }

    // ==================== AI LOGIC ====================

    private IEnumerator AutoMoveCoroutine()
    {
        // ✅ Kiểm tra kỹ trước khi chạy
        if (active == null || !active.IsNPCTurn || isProcessingUI || hasDestroyedThisTurn)
        {
            isAutoMoveInProgress = false;
            yield break;
        }

        // ✅ Delay 1 giây trước khi tính toán (thay thế WaitAndAutoMove)
        yield return new WaitForSeconds(1f);

        // ✅ Kiểm tra lại sau delay
        if (active == null || !active.IsNPCTurn || isProcessingUI || hasDestroyedThisTurn)
        {
            isAutoMoveInProgress = false;
            yield break;
        }

        // === LOGIC CHỌN NƯỚC ĐI (giữ nguyên) ===
        List<(GameObject dot, int targetX, int targetY, float score, int vangDotDestroyed, int totalDestroyed, bool hasVangInCombo)> scoredMoves =
            new List<(GameObject, int, int, float, int, int, bool)>();

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (allDots[i, j] == null) continue;

                int[][] directions = new int[][]
                {
                new[] { -1, 0 }, new[] { 1, 0 }, new[] { 0, -1 }, new[] { 0, 1 }
                };

                foreach (var dir in directions)
                {
                    int newX = i + dir[0];
                    int newY = j + dir[1];
                    if (newX < 0 || newX >= width || newY < 0 || newY >= height) continue;

                    int chainLength;
                    bool isComplex;

                    if (CheckValidMove(i, j, newX, newY, out chainLength, out isComplex))
                    {
                        GameObject movedDot = allDots[i, j];
                        int vangDestroyed = CalculatePotentialVangDotDestruction(movedDot, newX, newY);
                        int comboDestroyed = SimulateVirtualCombo(movedDot, newX, newY, 3);
                        bool hasVangInCombo = SimulateHasVangDotInCombo(movedDot, newX, newY);

                        float score = CalculateMoveScore(movedDot, newX, newY, chainLength, isComplex, movedDot.tag);

                        if (vangDestroyed > 0)
                            score += 1000f + vangDestroyed * 200f;

                        if (hasVangInCombo)
                            score += 500f;

                        scoredMoves.Add((movedDot, newX, newY, score, vangDestroyed, comboDestroyed, hasVangInCombo));
                    }
                }
            }
        }

        if (scoredMoves.Count == 0)
        {
            isAutoMoveInProgress = false;
            yield break;
        }

        // === CHỌN NƯỚC ĐI TỐT NHẤT ===
        (GameObject dot, int targetX, int targetY, float score, int vangDotDestroyed, int totalDestroyed, bool hasVangInCombo) bestMove;

        var comboVangMoves = scoredMoves.Where(m => m.hasVangInCombo && m.totalDestroyed > 3).ToList();
        if (comboVangMoves.Count > 0)
        {
            bestMove = comboVangMoves.OrderByDescending(m => m.totalDestroyed).ThenByDescending(m => m.score).First();
        }
        else
        {
            var vangMoves = scoredMoves.Where(m => m.vangDotDestroyed > 0).ToList();
            if (vangMoves.Count > 0)
            {
                bestMove = vangMoves.OrderByDescending(m => m.vangDotDestroyed).ThenByDescending(m => m.score).First();
            }
            else
            {
                bestMove = scoredMoves.OrderByDescending(m => m.totalDestroyed).ThenByDescending(m => m.score).First();
            }
        }

        Dot Dot = bestMove.dot.GetComponent<Dot>();
        if (Dot == null)
        {
            isAutoMoveInProgress = false;
            yield break;
        }

        int deltaX = bestMove.targetX - Dot.column;
        int deltaY = bestMove.targetY - Dot.row;


        hasDestroyedThisTurn = true;

        PerformMove(Dot, deltaX, deltaY);

        yield return new WaitForSeconds(0.5f);

        DestroyMatches();

    }

    private bool SimulateHasVangDotInCombo(GameObject movedDot, int targetX, int targetY)
    {
        if (movedDot == null) return false;

        int originalX = movedDot.GetComponent<Dot>().column;
        int originalY = movedDot.GetComponent<Dot>().row;

        string[,] boardTags = new string[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                boardTags[x, y] = allDots[x, y] != null ? allDots[x, y].tag : null;
            }
        }

        (boardTags[originalX, originalY], boardTags[targetX, targetY]) = (boardTags[targetX, targetY], boardTags[originalX, originalY]);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                string tag = boardTags[x, y];
                if (tag == null) continue;

                int horiz = 1;
                int tx = x + 1;
                while (tx < width && boardTags[tx, y] == tag) { horiz++; tx++; }

                int vert = 1;
                int ty = y + 1;
                while (ty < height && boardTags[x, ty] == tag) { vert++; ty++; }

                if ((horiz >= 3 || vert >= 3) && tag == "vang Dot")
                    return true;
            }
        }

        return false;
    }

    private int SimulateVirtualCombo(GameObject movedDot, int targetX, int targetY, int maxDepth = 3)
    {
        if (movedDot == null) return 0;

        int totalDestroyed = 0;
        int originalX = movedDot.GetComponent<Dot>().column;
        int originalY = movedDot.GetComponent<Dot>().row;

        string[,] boardTags = new string[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                boardTags[x, y] = allDots[x, y] != null ? allDots[x, y].tag : null;
            }
        }

        (boardTags[originalX, originalY], boardTags[targetX, targetY]) = (boardTags[targetX, targetY], boardTags[originalX, originalY]);

        for (int depth = 0; depth < maxDepth; depth++)
        {
            bool[,] toDestroy = new bool[width, height];
            bool hasMatch = false;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    string tag = boardTags[x, y];
                    if (tag == null) continue;

                    int horiz = 1;
                    int tx = x + 1;
                    while (tx < width && boardTags[tx, y] == tag) { horiz++; tx++; }

                    int vert = 1;
                    int ty = y + 1;
                    while (ty < height && boardTags[x, ty] == tag) { vert++; ty++; }

                    if (horiz >= 3)
                    {
                        hasMatch = true;
                        for (int k = 0; k < horiz; k++) toDestroy[x + k, y] = true;
                    }

                    if (vert >= 3)
                    {
                        hasMatch = true;
                        for (int k = 0; k < vert; k++) toDestroy[x, y + k] = true;
                    }
                }
            }

            if (!hasMatch) break;

            int destroyedThisRound = 0;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (toDestroy[x, y])
                    {
                        boardTags[x, y] = null;
                        destroyedThisRound++;
                    }
                }
            }

            totalDestroyed += destroyedThisRound;

            for (int x = 0; x < width; x++)
            {
                int emptyBelow = 0;
                for (int y = 0; y < height; y++)
                {
                    if (boardTags[x, y] == null)
                    {
                        emptyBelow++;
                    }
                    else if (emptyBelow > 0)
                    {
                        boardTags[x, y - emptyBelow] = boardTags[x, y];
                        boardTags[x, y] = null;
                    }
                }
            }
        }

        return totalDestroyed;
    }

    private bool CheckValidMove(int x1, int y1, int x2, int y2, out int chainLength, out bool isComplexChain)
    {
        GameObject dot1 = allDots[x1, y1];
        GameObject dot2 = allDots[x2, y2];

        if (dot1 == null || dot2 == null)
        {
            chainLength = 0;
            isComplexChain = false;
            return false;
        }

        allDots[x1, y1] = dot2;
        allDots[x2, y2] = dot1;

        bool complex1, complex2;
        int chain1 = CheckChain(x1, y1, dot2, out complex1);
        int chain2 = CheckChain(x2, y2, dot1, out complex2);

        chainLength = Math.Max(chain1, chain2);
        isComplexChain = complex1 || complex2;

        allDots[x1, y1] = dot1;
        allDots[x2, y2] = dot2;

        return chainLength >= 3;
    }

    private int CheckChain(int col, int row, GameObject dot, out bool isComplexChain)
    {
        if (dot == null)
        {
            isComplexChain = false;
            return 0;
        }

        string tag = dot.tag;
        int horizontalCount = 1;
        int verticalCount = 1;

        for (int i = col - 1; i >= 0 && allDots[i, row]?.tag == tag; i--) horizontalCount++;
        for (int i = col + 1; i < width && allDots[i, row]?.tag == tag; i++) horizontalCount++;

        for (int j = row - 1; j >= 0 && allDots[col, j]?.tag == tag; j--) verticalCount++;
        for (int j = row + 1; j < height && allDots[col, j]?.tag == tag; j++) verticalCount++;

        isComplexChain = (horizontalCount >= 3 && verticalCount >= 3);

        return isComplexChain ? (horizontalCount + verticalCount - 1) : Math.Max(horizontalCount, verticalCount);
    }

    private void PerformMove(Dot dot, int deltaX, int deltaY)
    {
        int targetColumn = dot.column + deltaX;
        int targetRow = dot.row + deltaY;

        GameObject targetDot = allDots[targetColumn, targetRow];
        if (targetDot != null)
        {
            dot.previousColumn = dot.column;
            dot.previousRow = dot.row;

            targetDot.GetComponent<Dot>().column = dot.column;
            targetDot.GetComponent<Dot>().row = dot.row;

            dot.column = targetColumn;
            dot.row = targetRow;

            StartCoroutine(dot.CheckMoveCo());
        }
    }

    // ==================== BOARD SETUP ====================

    private IEnumerator setUp()
    {
        loading.SetActive(true);
        yield return new WaitForSeconds(2f);

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Vector2 temPosition = new Vector2(i, j + offSet);
                int dotToUse = UnityEngine.Random.Range(0, dots.Length);
                int maxIterations = 0;

                while (MatchesAt(i, j, dots[dotToUse]) && maxIterations < 100)
                {
                    dotToUse = UnityEngine.Random.Range(0, dots.Length);
                    maxIterations++;
                }

                maxIterations = 0;
                GameObject dot = Instantiate(dots[dotToUse], temPosition, Quaternion.identity);
                dot.GetComponent<Dot>().row = j;
                dot.GetComponent<Dot>().column = i;
                dot.transform.parent = this.transform;
                dot.name = "(" + i + "," + j + ")";
                allDots[i, j] = dot;

                // ✅ ẨN TẤT CẢ VIÊN NGAY SAU KHI TẠO
                dot.SetActive(false);
            }
        }

        CanvasGroup canvasGroup = loading.gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = loading.gameObject.AddComponent<CanvasGroup>();
        }
        yield return StartCoroutine(FadeOut(canvasGroup, 0.5f));

        loading.SetActive(false);
        isBoardReady = true;

        // ✅ GỌI OnBoardReady - NHƯNG CHƯA HIỆN BOARD
        if (active != null)
        {
            active.OnBoardReady();
        }
    }

    private bool MatchesAt(int column, int row, GameObject piece)
    {
        // ✅ CHECK NGANG: Cần có ít nhất 2 viên bên trái
        if (column > 1)
        {
            if (allDots[column - 1, row] != null && allDots[column - 2, row] != null)
            {
                if (allDots[column - 1, row].tag == piece.tag &&
                    allDots[column - 2, row].tag == piece.tag)
                {
                    return true;
                }
            }
        }

        // ✅ CHECK DỌC: Cần có ít nhất 2 viên bên dưới
        if (row > 1)
        {
            if (allDots[column, row - 1] != null && allDots[column, row - 2] != null)
            {
                if (allDots[column, row - 1].tag == piece.tag &&
                    allDots[column, row - 2].tag == piece.tag)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // ==================== MATCH DESTRUCTION ====================

    private void DestroyMatchesAt(int column, int row)
    {
        isDestroyingMatches = true;

        if (isDestroyingMatches)
        {
            if (active != null)
            {
                active.PauseTurn();

                if (allDots[column, row] != null && allDots[column, row].GetComponent<Dot>().isMathched)
                {
                    GameObject dotToDestroy = allDots[column, row];
                    Dot dotComponent = dotToDestroy.GetComponent<Dot>();
                    string dotTag = dotToDestroy.tag.ToString();

                    // ✅ PHÁT ÂM THANH
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayMatchSound(dotTag);
                    }

                    // ✅ CẬP NHẬT COUNTER VỚI MULTIPLIER
                    int countToAdd = dotComponent != null ? dotComponent.multiplier : 1;

                    if (destroyedCountByTag.ContainsKey(dotTag))
                    {
                        destroyedCountByTag[dotTag] += countToAdd;
                    }
                    else
                    {
                        destroyedCountByTag[dotTag] = countToAdd;
                    }

                    findMaches.currentMatches.Remove(dotToDestroy);
                    destroyedCount += countToAdd;

                    // ✅ HIỆU ỨNG PHÁ HUỶ
                    DotDestroyEffect.PlayEffect(dotToDestroy, () =>
                    {
                        if (dotToDestroy != null)
                        {
                            Destroy(dotToDestroy);
                        }
                    });

                    allDots[column, row] = null;
                }
            }
        }
    }

    private void ResetDestroyedCounts()
    {
        destroyedCountByTag.Clear();
    }

    private void OnMouseDown()
    {
        ResetDestroyedCounts();
    }

    public IEnumerator WaitAndDestroyMatches()
    {
        currentState = GameState.wait;

        // Phá tất cả viên match
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (allDots[i, j] != null)
                {
                    DestroyMatchesAt(i, j);
                }
            }
        }

        // ✅ ĐỢI ANIMATION FADE + SCALE HOÀN THÀNH (duration = 0.3s)
        yield return new WaitForSeconds(0.25f);

        StartCoroutine(DecreaseRowCo());
    }

    public void DestroyMatches()
    {
        StartCoroutine(WaitAndDestroyMatches());
    }

    public void DestroyRandomDots(int count)
    {
        if (count <= 0) return;
        StartCoroutine(DestroyRandomDotsCo(count));
    }

    private IEnumerator DestroyRandomDotsCo(int count)
    {
        // Không cho kéo trong lúc skill đang xử lý
        currentState = GameState.wait;

        // Gom tất cả vị trí đang có viên
        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (allDots[x, y] != null)
                {
                    candidates.Add(new Vector2Int(x, y));
                }
            }
        }

        // Không được phá nhiều hơn số ô đang có
        int destroyAmount = Mathf.Min(count, candidates.Count);

        for (int i = 0; i < destroyAmount; i++)
        {
            int index = UnityEngine.Random.Range(0, candidates.Count);
            Vector2Int pos = candidates[index];
            candidates.RemoveAt(index);

            GameObject dotGO = allDots[pos.x, pos.y];
            if (dotGO == null) continue;

            // Đánh dấu isMathched để tận dụng sẵn DestroyMatchesAt
            var dot = dotGO.GetComponent<Dot>();
            if (dot != null)
            {
                dot.isMathched = true;
            }
        }

        // Chờ 1 frame cho an toàn rồi dùng pipeline có sẵn
        yield return null;

        // Sử dụng lại luồng phá ô hiện có:
        // DestroyMatchesAt -> DecreaseRowCo -> FillBoardCo -> HandleUI
        DestroyMatches();
    }

    private IEnumerator DecreaseRowCo()
    {
        int nullCount = 0;
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (allDots[i, j] == null)
                {
                    nullCount++;
                }
                else if (nullCount > 0)
                {
                    allDots[i, j].GetComponent<Dot>().row -= nullCount;
                    allDots[i, j] = null;
                }
            }
            nullCount = 0;
        }
        yield return new WaitForSeconds(.1f);
        StartCoroutine(FillBoardCo());
    }

    private void RefillBoard()
    {
        int currentTurn = (active != null) ? active.TurnNumber : 0;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (allDots[i, j] == null)
                {
                    Vector2 tempPosition = new Vector2(i, j + offSet);

                    int dotToUse = UnityEngine.Random.Range(0, dots.Length);
                    int maxAttempts = 100;
                    int attempts = 0;

                    // ✅ GÁN VIÊN TẠM VÀO MẢNG ĐỂ MatchesAt() HOẠT ĐỘNG
                    GameObject tempPiece = dots[dotToUse];

                    while (MatchesAt(i, j, tempPiece) && attempts < maxAttempts)
                    {
                        dotToUse = UnityEngine.Random.Range(0, dots.Length);
                        tempPiece = dots[dotToUse];
                        attempts++;
                    }

                    // ✅ BÂY GIỜ MỚI SPAWN
                    GameObject piece = Instantiate(dots[dotToUse], tempPosition, Quaternion.identity);
                    allDots[i, j] = piece; // ✅ GÁN NGAY

                    Dot dotComponent = piece.GetComponent<Dot>();
                    dotComponent.row = j;
                    dotComponent.column = i;
                    piece.transform.parent = this.transform;
                    piece.name = "(" + i + "," + j + ")";

                    // ✅ LOGIC MULTIPLIER THEO TURN
                    if (currentTurn >= HOTTURN) // Turn 10+: Tỉ lệ đầy đủ
                    {
                        float roll = UnityEngine.Random.Range(0f, 100f);

                        if (roll < 10f)
                        {
                            dotComponent.multiplier = 4;
                            CreateMultiplierText(piece, 4);
                        }
                        else if (roll < 20f)
                        {
                            dotComponent.multiplier = 3;
                            CreateMultiplierText(piece, 3);
                        }
                        else if (roll < 40f)
                        {
                            dotComponent.multiplier = 2;
                            CreateMultiplierText(piece, 2);
                        }
                    }
                    else if (currentTurn >= SUBHOTTURN) // Turn 5-9: Chỉ có x2 với 20%
                    {
                        float roll = UnityEngine.Random.Range(0f, 100f);

                        if (roll < 20f)
                        {
                            dotComponent.multiplier = 2;
                            CreateMultiplierText(piece, 2);
                        }
                    }
                    // Turn 1-4: Không có multiplier
                }
            }
        }
    }
    /// <summary>
    /// Tạo text hiển thị multiplier trên viên
    /// </summary>
    /// <summary>
    /// Tạo text hiển thị multiplier trên viên
    /// </summary>
    private void CreateMultiplierText(GameObject dot, int multiplier)
    {
        // Tạo Canvas child
        GameObject canvasObj = new GameObject("MultiplierCanvas");
        canvasObj.transform.SetParent(dot.transform);
        canvasObj.transform.localPosition = Vector3.zero;
        canvasObj.transform.localScale = Vector3.one; // Giữ scale = 1 tại canvas level
        canvasObj.transform.localRotation = Quaternion.identity;

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingLayerName = "Default";
        canvas.sortingOrder = 2;
        canvas.renderMode = RenderMode.WorldSpace;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;

        canvasObj.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(100, 100);

        // Tạo Text
        GameObject textObj = new GameObject("MultiplierText");
        textObj.transform.SetParent(canvasObj.transform);
        textObj.transform.localRotation = Quaternion.identity;

        // ✅ SET SCALE = 0.01 như trong ảnh
        textObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        Text text = textObj.AddComponent<Text>();
        text.text = "x" + multiplier.ToString();

        // Load font
        Font customFont = Resources.Load<Font>("font/UTM Erie Black");
        if (customFont != null)
        {
            text.font = customFont;
        }
        else
        {
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        text.fontSize = 35;
        text.fontStyle = FontStyle.Bold;
        text.color = Color.yellow;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        if (text.material == null)
        {
            text.material = Resources.GetBuiltinResource<Material>("UI/Default.mat");
        }

        // Outline màu #FF00C2
        Outline outline = textObj.AddComponent<Outline>();
        Color outlineColor;
        if (ColorUtility.TryParseHtmlString("#FF00C2", out outlineColor))
        {
            outline.effectColor = outlineColor;
        }
        else
        {
            outline.effectColor = new Color(1f, 0f, 0.76f, 1f);
        }
        outline.effectDistance = new Vector2(2, -2);

        RectTransform textRect = textObj.GetComponent<RectTransform>();

        // ✅ SET WIDTH/HEIGHT = 52 như trong ảnh
        textRect.sizeDelta = new Vector2(52f, 52f);

        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);

        // ✅ POSITION: Góc dưới phải (tương tự ảnh: Pos X = 0.21, Pos Y = -0.24)
        textRect.localPosition = new Vector3(0.21f, -0.24f, 0f);

        // Lưu reference
        Dot dotComponent = dot.GetComponent<Dot>();
        dotComponent.multiplierText = text;

    }

    private bool MatchesOnBoard()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (allDots[i, j] != null)
                {
                    if (allDots[i, j].GetComponent<Dot>().isMathched)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private IEnumerator FillBoardCo()
    {
        RefillBoard();
        yield return new WaitForSeconds(.1f);

        // ✅ BẮT BUỘC: Tìm matches sau khi refill
        if (findMaches != null)
        {
            findMaches.FindAllMatches();
        }

        yield return new WaitForSeconds(.1f);

        // ✅ THÊM COUNTER để tránh infinite loop
        int safetyCounter = 0;
        int maxIterations = 10;

        while (MatchesOnBoard() && safetyCounter < maxIterations)
        {

            DestroyMatches();
            yield return new WaitForSeconds(.5f); // ✅ Đợi phá hủy xong

            // ✅ Tìm matches mới sau mỗi lần phá hủy
            if (findMaches != null)
            {
                findMaches.FindAllMatches();
            }

            safetyCounter++;
        }

        if (safetyCounter >= maxIterations)
        {
        }

        yield return new WaitForSeconds(.3f);

        if (stableBoardCheckCoroutine != null)
        {
            StopCoroutine(stableBoardCheckCoroutine);
        }
        stableBoardCheckCoroutine = StartCoroutine(CheckForStableBoardAfterFill());
    }

    public IEnumerator CheckForStableBoardAfterFill()
    {
        float checkInterval = 0.1f;
        float maxWaitTime = 0.5f;
        float elapsedTime = 0f;

        while (true)
        {
            if (CheckBoardStable())
            {
                elapsedTime += checkInterval;
                if (elapsedTime >= maxWaitTime)
                {
                    break;
                }
            }
            else
            {
                elapsedTime = 0f;
            }

            yield return new WaitForSeconds(checkInterval);
        }

        displayDestroy();

        // ✅ KIỂM TRA NƯỚC ĐI SAU KHI STABLE
        if (!HasValidMoves())
        {
            Debug.Log("[Board] Phát hiện không còn nước đi!");
            yield return StartCoroutine(ResetBoardWhenNoMoves());
        }

        yield return HandleUI();
    }

#if UNITY_EDITOR
    [ContextMenu("Check Valid Moves")]
    private void DebugCheckValidMoves()
    {
        if (!Application.isPlaying) return;

        bool hasMove = HasValidMoves();
        Debug.Log($"[Board] Có nước đi hợp lệ: {hasMove}");

        if (!hasMove)
        {
            StartCoroutine(ResetBoardWhenNoMoves());
        }
    }

    [ContextMenu("Force Reset Board")]
    private void DebugForceReset()
    {
        if (!Application.isPlaying) return;
        StartCoroutine(ResetBoardWhenNoMoves());
    }
#endif

    private void displayDestroy()
    {
        List<string> customOrder = new List<string> { "xanh Dot", "xanhduong Dot", "do Dot", "tim Dot", "trang Dot", "vang Dot" };

        foreach (Transform child in destructionCountPanel.transform)
        {
            Destroy(child.gameObject);
        }

        var sortedCounts = destroyedCountByTag
            .Where(kvp => kvp.Value > 0)
            .OrderBy(kvp => customOrder.IndexOf(kvp.Key) >= 0 ? customOrder.IndexOf(kvp.Key) : int.MaxValue)
            .ThenByDescending(kvp => kvp.Value)
            .ToList();

        foreach (var kvp in sortedCounts)
        {
            // ✅ LOG ĐỂ KIỂM TRA

            GameObject entry = Instantiate(destructionEntryPrefab, destructionCountPanel.transform);
            entry.name = kvp.Key;

            Image iconImage = entry.transform.Find("Icon").GetComponent<Image>();
            Text countText = entry.transform.Find("CountText").GetComponent<Text>();

            if (itemIcons.TryGetValue(kvp.Key, out Sprite iconSprite))
            {
                iconImage.sprite = iconSprite;
            }

            countText.text = $"{kvp.Value}";
        }
    }

    // ==================== HANDLE UI & EFFECTS ====================
    private IEnumerator HandleUI()
    {
        if (active == null)
        {
            yield break;
        }

        isProcessingUI = true;
        active.PauseTurn();
        if (!isGameOver)
        {
            HideAllItems();
        }

        // ===== 1) THU THẬP DỮ LIỆU =====
        List<Transform> itemsToProcess = new List<Transform>();
        for (int i = 0; i < destructionCountPanel.transform.childCount; i++)
        {
            Transform child = destructionCountPanel.transform.GetChild(i);
            if (child != null) itemsToProcess.Add(child);
        }

        var orderedItems = itemsToProcess
            .Where(t => t != null && t.TryGetComponent<RectTransform>(out _))
            .OrderBy(t => ((RectTransform)t).anchoredPosition.x)
            .ToList();

        // ===== 2) PHÂN LOẠI ITEMS =====
        Dictionary<string, int> itemCounts = new Dictionary<string, int>();
        foreach (Transform item in orderedItems)
        {
            if (item == null || item.gameObject == null) continue;

            string itemType = item.gameObject.name.Trim();
            int pieceCount = 0;
            bool isValid = int.TryParse(
                item.gameObject.transform.GetChild(1)?.GetComponent<Text>()?.text,
                out pieceCount
            );
            if (!isValid || pieceCount <= 0) continue;

            itemCounts[itemType] = pieceCount;
        }

        // ===== 3) TÍNH TOÁN TRƯỚC (KHÔNG ANIMATION) =====
        foreach (var kvp in itemCounts)
        {
            if (kvp.Key != "vang Dot")
            {
                active.CalculateOutputsWithoutAnimation(kvp.Key, kvp.Value);
            }
        }

        // ===== 4) HEAL ANIMATION BLOCK =====
        string[] healOrder = new string[] { "do Dot", "xanh Dot", "xanhduong Dot", "tim Dot", "trang Dot" };
        bool hasAnyHeal = healOrder.Any(h => itemCounts.ContainsKey(h));

        if (hasAnyHeal)
        {

            // 🎬 BẮT ĐẦU ANIMATION HEAL
            StartCoroutine(active.SetAnimationForItem("xanh Dot"));
            yield return new WaitForSeconds(0.15f); // Đợi animation khởi động

            // Hiển thị từng hiệu ứng heal
            foreach (string healType in healOrder)
            {
                if (itemCounts.ContainsKey(healType))
                {
                    yield return StartCoroutine(active.OutputsParam(healType));
                    active.UpdateSlider();
                    yield return new WaitForSeconds(0.1f); // Spacing giữa các heal
                }
            }

            // 🎬 KẾT THÚC ANIMATION HEAL
            yield return new WaitForSeconds(0.2f);
            if (active.IsPlayerTurn && active.playerPetAnimator != null)
                active.playerPetAnimator.SetInteger("key", 0);
            else if (active.IsNPCTurn && active.bossPetAnimator != null)
                active.bossPetAnimator.SetInteger("key", 0);

            yield return new WaitForSeconds(0.2f); // ⭐ Đợi animator về idle hoàn toàn
        }

        // ===== 5) DAMAGE (KIẾM VÀNG) BLOCK =====
        if (itemCounts.ContainsKey("vang Dot"))
        {
            int countKiem = itemCounts["vang Dot"];

            // ⏱️ DELAY QUAN TRỌNG: Đảm bảo animator đã reset xong
            yield return new WaitForSeconds(0.3f);

            // 🎬 TÍNH DAMAGE VỚI ANIMATION ATTACK
            active.CalculateOutputs("vang Dot", countKiem);

            // ⏱️ CHỜ ANIMATION ATTACK CHẠY XONG
            yield return new WaitForSeconds(0.5f); // ⭐ Tăng từ 0.15s lên 0.5s

            // Hiển thị số damage
            yield return StartCoroutine(active.OutputsParam("vang Dot"));

            // 🎬 RESET ANIMATION
            yield return new WaitForSeconds(0.2f);
            if (active.IsPlayerTurn && active.playerPetAnimator != null)
                active.playerPetAnimator.SetInteger("key", 0);
            else if (active.IsNPCTurn && active.bossPetAnimator != null)
                active.bossPetAnimator.SetInteger("key", 0);

            active.UpdateSlider();
            yield return new WaitForSeconds(0.2f);
        }

        // ===== 6) FADE OUT UI ITEMS =====
        float perItemDelay = 0.05f;
        float perItemFade = 0.2f;

        foreach (Transform item in orderedItems)
        {
            if (item == null || item.gameObject == null) continue;

            try
            {
                LeanTween.scale(item.gameObject, Vector3.one * 0.9f, 0.1f).setIgnoreTimeScale(true);
            }
            catch { }

            CanvasGroup cg = item.gameObject.GetComponent<CanvasGroup>();
            if (cg == null) cg = item.gameObject.AddComponent<CanvasGroup>();

            yield return StartCoroutine(FadeOut(cg, perItemFade));
            yield return new WaitForSeconds(perItemDelay);
        }

        foreach (Transform item in orderedItems)
        {
            if (item != null && item.gameObject != null)
                Destroy(item.gameObject);
        }

        // ===== 7) CLEANUP =====
        destroyedCountByTag.Clear();

        if (active != null)
        {
            active.SummaryOutputs();
            active.resetOutput();
        }

        // ===== 8) CHECK GAME OVER =====
        bool playerDead = active.MauPlayer <= 0;
        bool npcDead = active.MauNPC <= 0;
        bool gameOver = playerDead || npcDead;

        if (gameOver)
        {
            isProcessingUI = false;
            isAutoMoveInProgress = false;
            hasDestroyedThisTurn = false;
            currentState = GameState.move;

            // ✅ KIỂM TRA CẢ 2 CÙNG CHẾT (SYNC ERROR)
            if (playerDead && npcDead)
            {
                Debug.LogWarning("[Board] CẢ 2 CÙNG CHẾT - Server chưa đồng bộ!");
                yield return StartCoroutine(ShowSyncErrorWarning());
                yield break;
            }

            // Trường hợp bình thường: 1 bên thắng, 1 bên thua
            yield return StartCoroutine(ShowGameResultIntegrated(npcDead));
            yield break;
        }

        int nextIndex = active.IsPlayerTurn ? 1 : 0;
        float nextTurnDelay = (nextIndex == 0) ? 0.05f : 0.2f;
        yield return new WaitForSeconds(nextTurnDelay);

        isProcessingUI = false;
        isAutoMoveInProgress = false;
        hasDestroyedThisTurn = false;

        if (nextIndex == 1)
        {
            ShowItems();
            currentState = GameState.move;
        }
        else
        {
            HideAllItems();
            currentState = GameState.wait;
        }

        if (active != null)
        {
            active.EndCurrentTurn();
        }
    }

    /// <summary>
    /// ✅ HIỂN THỊ THÔNG BÁO LỖI ĐỒNG BỘ (CẢ 2 CÙNG CHẾT)
    /// </summary>
    private IEnumerator ShowSyncErrorWarning()
    {
        if (energyWarningPanel == null)
        {
            // Fallback: Về luôn nếu không có panel
            yield return new WaitForSeconds(1f);
            ReturnToQuangTruong();
            yield break;
        }

        // ✅ DỪNG HOÀN TOÀN GAME
        isGameOver = true;
        currentState = GameState.wait;
        enableAutoMove = false;

        if (active != null)
        {
            active.PauseTurn();
            active.StopAllCoroutines();
        }

        // ✅ HIỂN THỊ PANEL
        energyWarningPanel.SetActive(true);

        // ✅ SET TEXT
        if (energyWarningText != null)
        {
            energyWarningText.text = "Server chưa đồng bộ!\nVui lòng thử lại sau.";
        }

        // ✅ SETUP BUTTON
        if (energyWarningButton != null)
        {
            energyWarningButton.onClick.RemoveAllListeners();

            // Button về QuangTruong/Menu
            energyWarningButton.onClick.AddListener(() =>
            {
                energyWarningPanel.SetActive(false);
                ReturnToQuangTruongFromSync();
            });

            Text btnText = energyWarningButton.GetComponentInChildren<Text>();
            if (btnText != null)
            {
                btnText.text = "Quay về";
            }
        }

        // ✅ ANIMATION PANEL
        CanvasGroup cg = energyWarningPanel.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = energyWarningPanel.AddComponent<CanvasGroup>();
        }

        cg.alpha = 0f;
        LeanTween.alphaCanvas(cg, 1f, 0.3f).setIgnoreTimeScale(true);

    }

    /// <summary>
    /// ✅ TRỞ VỀ SAU KHI LỖI ĐỒNG BỘ
    /// </summary>
    private void ReturnToQuangTruongFromSync()
    {
        Time.timeScale = 1f;

        Debug.Log("[Board] Quay về do lỗi đồng bộ");

        // ✅ XÓA FLAGS NẾU CẦN
        if (isBossBattle)
        {
            PlayerPrefs.SetString("IsBossBattle", "false");
            PlayerPrefs.DeleteKey("BossElementType");
        }

        PlayerPrefs.Save();

        // ✅ VỀ SCENE TRƯỚC ĐÓ HOẶC QUẢNG TRƯỜNG
        if (ManagerGame.Instance != null)
        {
            ManagerGame.Instance.BackScene();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("QuangTruong");
        }
    }

    // ==================== INTEGRATED GAME RESULT SYSTEM ====================

    public IEnumerator ShowGameResultIntegrated(bool playerWon)
    {
        if (isGameOver)
        {
            yield break;
        }

        // ✅ DỪNG GAMEPLAY NGAY LẬP TỨC
        isGameOver = true;
        currentState = GameState.wait; // ✅ QUAN TRỌNG
        isProcessingUI = false;
        isAutoMoveInProgress = false;
        hasDestroyedThisTurn = false;
        enableAutoMove = false; // ✅ TẮT AI

        int turnCount = active != null ? active.TurnNumber : 0;

        // ✅ PAUSE TURN + DỪNG TIMER
        if (active != null)
        {
            active.PauseTurn();
            active.StopAllCoroutines(); // ✅ Dừng mọi coroutine trong Active
        }

        panelResult.SetActive(true);

        // 1. Fade out board
        if (this.gameObject != null)
            HideAllItemsEnd();

        yield return new WaitForSecondsRealtime(0.3f);

        // 2. Show result panel
        if (panelResult != null)
        {
            panelResult.SetActive(true);

            if (resultBackground != null)
            {
                CanvasGroup bgCanvas = resultBackground.GetComponent<CanvasGroup>();
                if (bgCanvas == null)
                {
                    bgCanvas = resultBackground.AddComponent<CanvasGroup>();
                }
                bgCanvas.alpha = 0f;
                LeanTween.alphaCanvas(bgCanvas, 1f, 0.5f).setIgnoreTimeScale(true);
            }
        }

        // 3. Pause turn
        if (active != null)
        {
            active.PauseTurn();
        }

        // 4. Show title
        if (txtResultTitle != null)
        {
            txtResultTitle.text = playerWon ? "Thắng" : "Thua";
        }

        yield return new WaitForSecondsRealtime(0.5f);

        // ✅ 5. BLIND BOX ANIMATION - CHUNG CHO CẢ BOSS VÀ PVP (chỉ khi thắng)
        if (playerWon && anmtRW != null && !isBossBattle) // ✅ PvP mới có blind box
        {
            int userId = PlayerPrefs.GetInt("userId", 1);
            var petData = new AddPetDto { petId = enemyPetId };

            string apiUrl = APIConfig.COUNT_PASS_TO_USER(userId);
            yield return APIManager.Instance.PostRequest<string>(
                apiUrl,
                petData,
                onSuccess: (resp) =>
                {
                },
                onError: (err) =>
                {
                }
            );

            currentCount++;
            PlayerPrefs.SetInt("count", currentCount);
            PlayerPrefs.Save();

            anmtRW.SetActive(true);
            Animator animator = anmtRW.GetComponent<Animator>();
            if (animator != null)
            {
                anmtRW.SetActive(true);

                float t = 0f, timeout = 3f;
                yield return new WaitUntil(() =>
                {
                    t += Time.unscaledDeltaTime;
                    var st = animator.GetCurrentAnimatorStateInfo(0);
                    return (!animator.IsInTransition(0) && st.normalizedTime >= 1f) || t >= timeout;
                });
            }

            anmtRW.SetActive(false);
        }

        // ✅ 6. CALCULATE REWARD - CHUNG CHO CẢ BOSS VÀ PVP
        GameReward reward = CalculateReward(playerWon, turnCount);

        // ✅ 7. GỬI DAMAGE LÊN SERVER (CHỈ BOSS)
        if (isBossBattle)
        {
            Debug.Log("[Board] GỬI DAMAGE LÊN SERVER");
            yield return StartCoroutine(SubmitBossDamage(playerWon, turnCount));
        }

        // ✅ 8. SHOW REWARDS CONTAINER
        if (rewards != null)
        {
            rewards.SetActive(true);
        }

        // ✅ 9. DISPLAY REWARDS - CHUNG CHO CẢ BOSS VÀ PVP
        yield return StartCoroutine(DisplayRewards(reward));

        // ✅ 10. SAVE TO SERVER - CHUNG CHO CẢ BOSS VÀ PVP
        yield return StartCoroutine(SaveRewardToServer(reward, playerWon));

        // ✅ 11. SETUP BUTTON THEO LOẠI BATTLE
        if (btnGet != null)
        {
            btnGet.onClick.RemoveAllListeners();

            if (isBossBattle)
            {
                // ✅ BOSS: Button về QuangTruong
                btnGet.onClick.AddListener(ReturnToQuangTruongFromBoss);

                Text btnText = btnGet.GetComponentInChildren<Text>();
                if (btnText != null)
                {
                    btnText.text = "Quay lại";
                }
            }
            else
            {
                // ✅ PVP: Button về scene trước
                btnGet.onClick.AddListener(ReturnToMenu);
            }

            btnGet.gameObject.SetActive(true);
        }

    }
    // ==================== BOSS DAMAGE SUBMISSION ====================

    private IEnumerator SubmitBossDamage(bool playerWon, int turnCount)
    {
        int userId = PlayerPrefs.GetInt("userId", 1);
        long bossScheduleId = PlayerPrefs.GetInt("CurrentBossId", 0);
        int totalDamage = 0;

        // Lấy damage từ ManagerMatch
        if (ManagerMatch.Instance != null)
        {
            totalDamage = ManagerMatch.Instance.GetTotalBossDamage();
        }


        // Tạo DTO
        BossBattleResultDTO resultData = new BossBattleResultDTO
        {
            userId = userId,
            bossScheduleId = bossScheduleId,
            damageDealt = totalDamage,
            victory = playerWon,
            turnCount = turnCount
        };

        // Gửi lên server
        yield return APIManager.Instance.PostRequestRaw(
            APIConfig.SUBMIT_BOSS_DAMAGE(userId, bossScheduleId),
            resultData,
            (response) =>
            {
            },
            (error) =>
            {
            }
        );
    }

    // ==================== PUBLIC RESULT METHODS ====================

    /// <summary>
    /// Button callback cho PvP - Về scene trước đó
    /// </summary>
    public void ReturnToMenu()
    {
        Time.timeScale = 1f;


        // ✅ GIỮ NGUYÊN FLAG để khôi phục state
        // KHÔNG xóa ReturnToRoom, ReturnToChinhPhuc, ReturnToPanelIndex

        if (ManagerGame.Instance != null)
        {
            ManagerGame.Instance.BackScene();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("QuangTruong");
        }
    }
    /// <summary>
    /// Button callback cho Boss Battle - Luôn về QuangTruong
    /// </summary>
    public void ReturnToQuangTruongFromBoss()
    {
        Time.timeScale = 1f;

        // ✅ XÓA FLAG BOSS
        PlayerPrefs.SetString("IsBossBattle", "false");
        PlayerPrefs.DeleteKey("BossElementType");

        // ✅ GIỮ FLAG ROOM để khôi phục state

        PlayerPrefs.Save();


        UnityEngine.SceneManagement.SceneManager.LoadScene("QuangTruong");
    }

    public void PlayAgain()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    // ==================== REWARD CALCULATION ====================

    private GameReward CalculateReward(bool playerWon, int turnCount)
    {
        GameReward reward = new GameReward();
        reward.petElement = enemyPetElement;
        reward.petId = enemyPetId;

        // ✅ NẾU THUA
        if (!playerWon)
        {
            reward.requestAttack = 50;
            reward.win = false;

            // THUA: nhận 1-2 đá level 1 (an ủi)
            int loseStones = UnityEngine.Random.Range(1, 3);
            reward.stones.Add(new RewardStone
            {
                level = 1,
                element = enemyPetElement,
                quantity = loseStones
            });

            return reward;
        }

        // ✅ NẾU THẮNG
        int requestAttack = PlayerPrefs.GetInt("requestAttack", 100);
        reward.requestAttack = requestAttack;
        reward.win = true;

        // ✅ THẮNG: 5% CƠ HỘI NHẬN GOLD BONUS (CHỈ VỚI PET LEVEL >= 60)
        int enemyPetLevel = PlayerPrefs.GetInt("EnemyPetLevel", 0); // ✅ THÊM: Lấy level enemy

        if (enemyPetLevel >= 60)
        {
            float luckyChance = UnityEngine.Random.Range(0f, 100f);
            if (luckyChance < 5f) // 5% chance
            {
                reward.bonusGold = UnityEngine.Random.Range(1000, 5001); // 1000-5000
            }
            else
            {
            }
        }
        else
        {
        }

        // ✅ BOSS BATTLE: Luôn cho đá theo hệ boss
        if (isBossBattle)
        {
            reward.receivedPet = false;

            int totalStones = CalculateTotalStones(turnCount);
            DistributeStones(reward, totalStones, turnCount);

            return reward;
        }

        // ✅ PVP: Logic như cũ (có thể nhận pet)
        if (currentCount == requestPass)
        {
            reward.receivedPet = true;
            return reward;
        }

        // PvP chưa đủ count → cho đá theo hệ enemy pet
        int pvpStones = CalculateTotalStones(turnCount);
        DistributeStones(reward, pvpStones, turnCount);

        return reward;
    }

    private int CalculateTotalStones(int turnCount)
    {
        // Công thức: base 8 + (turnCount / 3) - tối đa 15 đá
        int baseStones = 8;
        int bonusStones = Mathf.FloorToInt(turnCount / 3f);
        int total = baseStones + bonusStones;

        return Mathf.Clamp(total, 8, 15);
    }

    private void DistributeStones(GameReward reward, int totalStones, int turnCount)
    {
        Dictionary<(string element, int level), int> stoneCounts = new Dictionary<(string, int), int>();


        for (int i = 0; i < totalStones; i++)
        {
            int level = RollStoneLevel(turnCount);
            string element = enemyPetElement;

            var key = (element, level);
            if (stoneCounts.ContainsKey(key))
            {
                stoneCounts[key]++;
            }
            else
            {
                stoneCounts[key] = 1;
            }
        }

        foreach (var kvp in stoneCounts.OrderByDescending(x => x.Key.level).ThenBy(x => x.Key.element))
        {
            reward.stones.Add(new RewardStone
            {
                level = kvp.Key.level,
                element = kvp.Key.element,
                quantity = kvp.Value
            });
        }
    }

    private int RollStoneLevel(int turnCount)
    {
        float[] weights = new float[7]; // Level 1-7

        // Điều chỉnh weights theo yêu cầu: chỉ tập trung vào level 1-4
        if (turnCount <= 10) // ~5 turns
        {
            // Turn 5: Chủ yếu Lv1-2 (80% Lv1-2, 20% Lv3)
            weights[0] = 100f;  // Lv1 - 45%
            weights[1] = 80f;   // Lv2 - 35%  
            weights[2] = 10f;   // Lv3 - 20%
            weights[3] = 0f;    // Lv4 - 0%
            weights[4] = 0f;    // Lv5 - 0%
            weights[5] = 0f;    // Lv6 - 0%
            weights[6] = 0f;    // Lv7 - 0%
        }
        else if (turnCount <= 20) // ~15 turns
        {
            // Turn 15: Cân bằng Lv1-3
            weights[0] = 100f;  // Lv1 - 40%
            weights[1] = 85f;   // Lv2 - 35%
            weights[2] = 50f;   // Lv3 - 25%
            weights[3] = 0f;    // Lv4 - 0%
            weights[4] = 0f;    // Lv5 - 0%
            weights[5] = 0f;    // Lv6 - 0%
            weights[6] = 0f;    // Lv7 - 0%
        }
        else if (turnCount <= 30) // ~25 turns
        {
            // Turn 25: Nhiều Lv2-3
            weights[0] = 70f;   // Lv1 - 25%
            weights[1] = 100f;  // Lv2 - 40%
            weights[2] = 80f;   // Lv3 - 35%
            weights[3] = 10f;   // Lv4 - 0-5% (rất hiếm)
            weights[4] = 0f;    // Lv5 - 0%
            weights[5] = 0f;    // Lv6 - 0%
            weights[6] = 0f;    // Lv7 - 0%
        }
        else // 35+ turns
        {
            // Turn 35+: Nhiều Lv2-4
            weights[0] = 50f;   // Lv1 - 20%
            weights[1] = 100f;  // Lv2 - 40%
            weights[2] = 80f;   // Lv3 - 30%
            weights[3] = 40f;   // Lv4 - 10%
            weights[4] = 0f;    // Lv5 - 0%
            weights[5] = 0f;    // Lv6 - 0%
            weights[6] = 0f;    // Lv7 - 0%
        }

        float totalWeight = 0f;
        foreach (float w in weights)
        {
            totalWeight += w;
        }

        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float cumulativeWeight = 0f;

        for (int i = 0; i < 7; i++)
        {
            cumulativeWeight += weights[i];
            if (randomValue <= cumulativeWeight)
            {
                return i + 1;
            }
        }

        return 1; // Fallback
    }

    // ==================== DISPLAY REWARDS ====================

    private IEnumerator DisplayRewards(GameReward reward)
    {
        if (listReward == null)
        {
            yield break;
        }

        // Xóa các reward cũ
        foreach (Transform child in listReward.transform)
        {
            Destroy(child.gameObject);
        }

        yield return new WaitForSecondsRealtime(0.2f);

        // ✅ HIỂN THỊ BONUS GOLD (NẾU CÓ)
        if (reward.bonusGold > 0)
        {
            yield return StartCoroutine(DisplayBonusGold(reward.bonusGold));
        }

        // THÊM: Kiểm tra nếu có pet reward
        if (reward.receivedPet)
        {
            yield return StartCoroutine(DisplayPetReward(reward));
        }
        // THÊM: Kiểm tra nếu có stone rewards
        else if (reward.stones != null && reward.stones.Count > 0)
        {
            yield return StartCoroutine(DisplayStoneRewards(reward));
        }
        else
        {
        }
    }

    /// <summary>
    /// ✅ HIỂN THỊ BONUS GOLD (5% LUCKY)
    /// </summary>
    private IEnumerator DisplayBonusGold(int bonusGold)
    {
        if (itemRewardCT == null)
        {
            yield break;
        }

        // ✅ TẠO ICON GOLD
        GameObject goldReward = Instantiate(itemRewardGold, listReward.transform);
        goldReward.SetActive(true);
        Text goldCount = goldReward.transform.Find("txtCount")?.GetComponent<Text>();
        if (goldCount != null)
        {
            goldCount.text = $"+{bonusGold}";
        }

        goldReward.transform.localScale = Vector3.zero;

        // ✅ ANIMATION ĐẶC BIỆT (LUCKY!)
        LeanTween.scale(goldReward, Vector3.one * 1.2f, 0.6f)
            .setEaseOutElastic()
            .setIgnoreTimeScale(true)
            .setDelay(0.1f);

        // ✅ PULSE EFFECT (nhấp nháy)
        LeanTween.scale(goldReward, Vector3.one, 0.3f)
            .setDelay(0.7f)
            .setLoopPingPong(2)
            .setIgnoreTimeScale(true);

        yield return new WaitForSecondsRealtime(1.5f);
    }

    private IEnumerator DisplayPetReward(GameReward reward)
    {
        if (itemRewardPet == null)
        {
            yield break;
        }
        GameObject ctReward = Instantiate(itemRewardCT, listReward.transform);
        GameObject expReward = Instantiate(itemRewardEXP, listReward.transform);
        expReward.SetActive(true);
        Text counCtReward = ctReward.transform.Find("txtCount")?.GetComponent<Text>();
        Text counEXPReward = expReward.transform.Find("txtCount")?.GetComponent<Text>();
        if (counCtReward != null)
        {
            int requestAttack = PlayerPrefs.GetInt("requestAttack");
            counCtReward.text = "x" + requestAttack;
            int exp = requestAttack / 2;
            counEXPReward.text = "x" + exp;
        }
        LeanTween.scale(expReward, Vector3.one, 0.8f)
.setEaseOutElastic()
.setIgnoreTimeScale(true)
.setDelay(0.15f);
        LeanTween.scale(ctReward, Vector3.one, 0.8f)
.setEaseOutElastic()
.setIgnoreTimeScale(true)
.setDelay(0.15f);
        GameObject petReward = Instantiate(itemRewardPet, listReward.transform);
        Image petIcon = petReward.transform.Find("Image")?.GetComponent<Image>();

        if (petIcon != null)
        {
            string petPath = "Image/IconsPet/" + reward.petId.ToString();
            Sprite petSprite = Resources.Load<Sprite>(petPath);

            if (petSprite != null)
            {
                petIcon.sprite = petSprite;
            }
        }

        petReward.transform.localScale = Vector3.zero;



        LeanTween.scale(petReward, Vector3.one, 0.8f)
            .setEaseOutElastic()
            .setIgnoreTimeScale(true)
            .setDelay(0.3f);

        yield return new WaitForSecondsRealtime(1.2f);
    }

    private IEnumerator DisplayStoneRewards(GameReward reward)
    {
        if (itemRewardStone == null)
        {
            yield break;
        }

        var groupedStones = reward.stones
            .GroupBy(stone => new { stone.element, stone.level })
            .Select(group => new RewardStone
            {
                element = group.Key.element,
                level = group.Key.level,
                quantity = group.Sum(stone => stone.quantity)
            })
            .OrderByDescending(stone => stone.level)
            .ThenBy(stone => stone.element)
            .ToList();
        GameObject ctReward = Instantiate(itemRewardCT, listReward.transform);
        GameObject expReward = Instantiate(itemRewardEXP, listReward.transform);
        expReward.SetActive(true);
        Text counexpReward = expReward.transform.Find("txtCount")?.GetComponent<Text>();
        Text counCtReward = ctReward.transform.Find("txtCount")?.GetComponent<Text>();
        if (counCtReward != null)
        {
            int requestAttack = PlayerPrefs.GetInt("requestAttack");
            counCtReward.text = requestAttack.ToString();
            int exp = requestAttack / 2;
            counexpReward.text = "x" + exp.ToString();
        }
        LeanTween.scale(expReward, Vector3.one, 0.8f)
.setEaseOutElastic()
.setIgnoreTimeScale(true)
.setDelay(0.15f);
        LeanTween.scale(ctReward, Vector3.one, 0.8f)
.setEaseOutElastic()
.setIgnoreTimeScale(true)
.setDelay(0.15f);
        float delay = 0f;

        foreach (var stone in groupedStones)
        {
            GameObject stoneReward = Instantiate(itemRewardStone, listReward.transform);

            Image stoneIcon = stoneReward.transform.Find("Image")?.GetComponent<Image>();
            Text countText = stoneReward.transform.Find("txtCount")?.GetComponent<Text>();

            if (stoneIcon != null)
            {
                Sprite stoneSprite = GetStoneSprite(stone.element, stone.level);
                if (stoneSprite != null)
                {
                    stoneIcon.sprite = stoneSprite;
                }
            }

            if (countText != null)
            {
                // SỬA: Thêm $ cho string interpolation
                countText.text = $"{stone.quantity}";
            }

            stoneReward.transform.localScale = Vector3.zero;

            LeanTween.scale(stoneReward, Vector3.one, 0.4f)
                .setEaseOutBack()
                .setIgnoreTimeScale(true)
                .setDelay(delay);

            delay += 0.15f;
        }

        yield return new WaitForSecondsRealtime(delay + 0.5f);
    }

    private Sprite GetStoneSprite(string element, int level)
    {
        int index = level - 1;

        if (index < 0 || index >= 7)
            return null;

        switch (element.ToLower())
        {
            case "fire":
                return (fireStones != null && index < fireStones.Length) ? fireStones[index] : null;
            case "water":
                return (waterStones != null && index < waterStones.Length) ? waterStones[index] : null;
            case "earth":
                return (earthStones != null && index < earthStones.Length) ? earthStones[index] : null;
            case "wood":
                return (windStones != null && index < windStones.Length) ? windStones[index] : null;
            case "metal":
                return (electricStones != null && index < electricStones.Length) ? electricStones[index] : null;
            default:
                return null;
        }
    }

    // ==================== API CALLS ====================

    private IEnumerator SaveRewardToServer(GameReward reward, bool playerWon)
    {
        int userId = PlayerPrefs.GetInt("userId", 1);

        // ✅ TÍNH EXP Ở UNITY
        int expGain = 0;

        if (playerWon)
        {
            expGain = reward.requestAttack / 2;
        }
        else
        {
            expGain = 10;
        }


        // ✅ GỬI BONUS GOLD (NẾU CÓ)
        if (reward.bonusGold > 0)
        {
            var goldData = new AddGoldDto { goldAmount = reward.bonusGold };

            yield return APIManager.Instance.PostRequest<string>(
                APIConfig.ADD_GOLD(userId),
                goldData,
                onSuccess: (resp) =>
                {
                },
                onError: (err) =>
                {
                }
            );
        }

        // ✅ GỬI REQUEST LÊN SERVER (GIỮ NGUYÊN)
        if (reward.receivedPet)
        {
            // THẮNG + PET
            var petData = new AddPetDto
            {
                petId = reward.petId,
                requestAttack = reward.requestAttack,
                expGain = expGain
            };

            string apiUrl = APIConfig.ADD_PET_TO_USER(userId);
            yield return APIManager.Instance.PostRequest<string>(
                apiUrl,
                petData,
                onSuccess: (resp) =>
                {
                },
                onError: (err) =>
                {
                }
            );
        }
        else
        {
            // THẮNG/THUA + STONES

            // 1. Cộng CT
            yield return APIManager.Instance.GetRequest<UserDTO>(
                APIConfig.UP_CT(userId, reward.requestAttack),
                null,
                OnError
            );

            // 2. Cộng EXP
            var expData = new AddExpDto { expGain = expGain };

            yield return APIManager.Instance.PostRequest<string>(
                APIConfig.ADD_EXP(userId),
                expData,
                onSuccess: (resp) =>
                {
                },
                onError: (err) =>
                {
                }
            );

            // 3. Thêm Stones
            foreach (var stone in reward.stones)
            {
                var stoneData = new AddStoneDto
                {
                    element = stone.element,
                    level = stone.level,
                    quantity = stone.quantity
                };

                string apiUrl = APIConfig.ADD_STONE_TO_USER(userId);

                yield return APIManager.Instance.PostRequest<string>(
                    apiUrl,
                    stoneData,
                    onSuccess: (resp) =>
                    {
                    },
                    onError: (err) =>
                    {
                    }
                );

                yield return new WaitForSecondsRealtime(0.1f);
            }
        }
    }


    void OnError(string error)
    {
    }

    void OnSuccess(string message)
    {
    }

    // ==================== LEGACY RESULT (FALLBACK) ====================

    private IEnumerator ShowGameResult(bool playerWon)
    {
        // Try new integrated system first
        yield return StartCoroutine(ShowGameResultIntegrated(playerWon));
    }

    // ==================== HELPER METHODS ====================

    private bool IsHealItem(string itemType)
    {
        return itemType == "do Dot" || itemType == "xanh Dot" ||
               itemType == "xanhduong Dot" || itemType == "tim Dot" ||
               itemType == "trang Dot";
    }

    private IEnumerator FadeOutQuick(CanvasGroup canvasGroup, float duration)
    {
        if (canvasGroup == null)
            yield break;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            if (canvasGroup == null || canvasGroup.gameObject == null)
                yield break;

            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(1f - (elapsedTime / duration));
            yield return null;
        }

        if (canvasGroup != null && canvasGroup.gameObject != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.gameObject.SetActive(false);
        }
    }

    public int GetDestroyedCountInMove()
    {
        int result = destroyedCount;
        destroyedCount = 0;
        return result;
    }

    private bool CheckBoardStable()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (allDots[i, j] == null || allDots[i, j].GetComponent<Dot>().isMathched)
                {
                    return false;
                }
            }
        }
        return true;
    }

    public bool IsPlayerAllowedToMove()
    {
        // ✅ THÊM CHECK isGameOver
        if (isGameOver) return false;

        if (active == null || Active.Instance == null)
            return false;

        return Active.Instance.IsPlayerTurn &&
               Active.Instance.IsTurnInProgress &&
               Active.Instance.CurrentTurnTime > 0 &&
               !isProcessingUI &&
               !hasDestroyedThisTurn &&
               currentState == GameState.move;
    }

    private IEnumerator FadeOut(CanvasGroup canvasGroup, float duration)
    {
        if (canvasGroup == null)
            yield break;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            if (canvasGroup == null || canvasGroup.gameObject == null)
                yield break;

            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(1f - (elapsedTime / duration));
            yield return null;
        }

        if (canvasGroup != null && canvasGroup.gameObject != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.gameObject.SetActive(false);
        }
    }

    public void HideAllItems()
    {
        // ✅ KIỂM TRA allDots đã được tạo chưa
        if (allDots == null)
        {
            Debug.LogWarning("[Board] Board chưa sẵn sàng!");
            return; // Thoát ra, không làm gì cả
        }

        foreach (GameObject item in allDots)
        {
            if (item != null)
                item.SetActive(false);
        }

        if (destructionCountPanel != null)
            destructionCountPanel.SetActive(true);
    }

    public IEnumerator HideAllItemsEnd()
    {
        foreach (GameObject item in allDots)
        {
            if (item != null)
            {
                yield return StartCoroutine(active.effect.FadeOut(item));
                item.SetActive(false);
                yield return new WaitForSeconds(2 / allDots.Length);
            }
        }
    }

    public void ShowItems()
    {
        // ✅ KIỂM TRA allDots đã được tạo chưa
        if (allDots == null)
        {
            Debug.LogWarning("[Board] Board chưa sẵn sàng!");
            return;
        }

        destructionCountPanel.SetActive(false);

        foreach (GameObject item in allDots)
        {
            if (item != null)
                item.SetActive(true);
        }
    }

    public void ResetMoveCounters()
    {
        destroyedCountByTag.Clear();
        destroyedCount = 0;

        if (active != null)
        {
            active.resetOutput();
        }
    }

    public int GetCurrentTurn()
    {
        return active != null ? active.TurnNumber : 0;
    }

    public bool IsCurrentlyPlayerTurn()
    {
        return active != null && active.IsPlayerTurn;
    }

    // ==================== AI HELPER METHODS ====================

    private bool WillCreatePlayerVangDotOpportunity(int x1, int y1, int x2, int y2)
    {
        GameObject temp = allDots[x1, y1];
        allDots[x1, y1] = allDots[x2, y2];
        allDots[x2, y2] = temp;

        bool createsOpportunity = false;

        for (int i = Math.Max(0, x1 - 2); i <= Math.Min(width - 1, x1 + 2); i++)
        {
            for (int j = Math.Max(0, y1 - 2); j <= Math.Min(height - 1, y1 + 2); j++)
            {
                if (allDots[i, j] != null && allDots[i, j].tag == "vang Dot" &&
                    CheckPotentialMatch(i, j, "vang Dot"))
                {
                    createsOpportunity = true;
                    break;
                }
            }
        }

        temp = allDots[x1, y1];
        allDots[x1, y1] = allDots[x2, y2];
        allDots[x2, y2] = temp;

        return createsOpportunity;
    }

    private bool CheckPotentialMatch(int x, int y, string tag)
    {
        int horizontalCount = 1 + CountConsecutive(x, y, -1, 0, tag) + CountConsecutive(x, y, 1, 0, tag);
        int verticalCount = 1 + CountConsecutive(x, y, 0, -1, tag) + CountConsecutive(x, y, 0, 1, tag);

        return horizontalCount >= 3 || verticalCount >= 3;
    }

    private int CountConsecutive(int x, int y, int dx, int dy, string tag)
    {
        int count = 0;
        int i = x + dx;
        int j = y + dy;

        while (i >= 0 && i < width && j >= 0 && j < height)
        {
            if (allDots[i, j] != null && allDots[i, j].tag == tag)
            {
                count++;
                i += dx;
                j += dy;
            }
            else
            {
                break;
            }
        }
        return count;
    }

    private float CalculateMoveScore(GameObject dot, int targetX, int targetY,
        int chainLength, bool isComplexChain, string tag)
    {
        float score = 0f;

        Dictionary<string, float> tagScores = new Dictionary<string, float>
        {
            {"vang Dot", 100f},
            {"xanh Dot", 80f},
            {"trang Dot", 60f},
            {"xanhduong Dot", 40f},
            {"do Dot", 20f},
            {"tim Dot", 10f}
        };

        score += tagScores.TryGetValue(tag, out float tagScore) ? tagScore : 0f;

        if (isComplexChain)
        {
            score *= 2.5f;
        }

        score += chainLength * 10f;

        if (WillCreatePlayerVangDotOpportunity(dot.GetComponent<Dot>().column,
            dot.GetComponent<Dot>().row, targetX, targetY))
        {
            score -= 150f;
        }

        int vangDotDestroyed = CalculatePotentialVangDotDestruction(dot, targetX, targetY);
        score += vangDotDestroyed * 50f;

        return score;
    }

    private int CalculatePotentialVangDotDestruction(GameObject movedDot, int targetX, int targetY)
    {
        int originalX = movedDot.GetComponent<Dot>().column;
        int originalY = movedDot.GetComponent<Dot>().row;
        GameObject targetDot = allDots[targetX, targetY];

        allDots[originalX, originalY] = targetDot;
        allDots[targetX, targetY] = movedDot;

        HashSet<GameObject> virtualMatches = new HashSet<GameObject>();
        FindMatches.Instance.VirtualFindAllMatches(virtualMatches);

        int count = virtualMatches.Count(g => g != null && g.tag == "vang Dot");

        allDots[originalX, originalY] = movedDot;
        allDots[targetX, targetY] = targetDot;

        return count;
    }

    private void OnDestroy()
    {
        if (active != null)
        {
            active.OnTurnStart -= HandleTurnStart;
            active.OnTurnEnd -= HandleTurnEnd;
        }
    }

    /// <summary>
    /// ✅ LOAD CARDS TỪ PLAYERPREFS
    /// </summary>
    void LoadCardsFromPlayerPrefs()
    {
        if (PlayerPrefs.HasKey("SelectedCards"))
        {
            string json = PlayerPrefs.GetString("SelectedCards");
            CardListWrapper wrapper = JsonUtility.FromJson<CardListWrapper>(json);

            if (wrapper != null && wrapper.cards != null && wrapper.cards.Count > 0)
            {
                LoadSelectedCards(wrapper.cards);
            }
        }
    }

    void CreateCardHT(CardData cardData)
    {
        // ✅ KIỂM TRA NULL TRƯỚC KHI TẠO
        if (cardData == null)
        {
            return;
        }

        if (cardContainer == null)
        {
            return;
        }

        // Tìm object cardHT trong cardContainer
        Transform cardHT = cardContainer.Find("cardHT");
        if (cardHT == null)
        {
            return;
        }

        GameObject cardObj = cardHT.gameObject;
        cardObj.SetActive(true);
        cardObj.name = $"cardHT_{cardData.cardId}";

        // Load sprite
        Transform imgCardTransform = cardObj.transform.Find("imgtCard");
        if (imgCardTransform != null)
        {
            Image imgCard = imgCardTransform.GetComponent<Image>();
            if (imgCard != null)
            {
                string spritePath = $"Image/Card/HT{cardData.cardId}";
                Sprite cardSprite = Resources.Load<Sprite>(spritePath);

                if (cardSprite != null)
                {
                    imgCard.sprite = cardSprite;
                }
            }
        }

        // Gắn CardData
        CardUI cardUI = cardObj.GetComponent<CardUI>();
        if (cardUI == null)
        {
            cardUI = cardObj.AddComponent<CardUI>();
        }

        // ✅ SetCardData sẽ tự động xóa object nếu cardData null
        cardUI.SetCardData(cardData);

        // ✅ CHỈ THÊM VÀO LIST NẾU OBJECT VẪN TỒN TẠI
        if (cardObj != null)
        {
            cardsInHand.Add(cardObj);
        }

    }

    /// <summary>
    /// ✅ LOAD CARDS VÀ HIỂN THỊ
    /// </summary>
    public void LoadSelectedCards(List<CardData> cards)
    {
        // ✅ LỌC BỎ CARDS NULL TRƯỚC KHI XỬ LÝ
        var validCards = cards.Where(c => c != null).ToList();

        selectedCards = new List<CardData>(validCards);

        // Hiển thị thẻ
        DisplayCardsOnBoard();
    }

    /// <summary>
    /// ✅ HIỂN THỊ CARDS LÊN BOARD
    /// </summary>
    void DisplayCardsOnBoard()
    {
        // ✅ CHỈ XÓA CARDS THƯỜNG (giữ lại cardHT)
        ClearCards();

        int cardsToDisplay = Mathf.Min(selectedCards.Count, maxCardsInHand);

        for (int i = 0; i < cardsToDisplay; i++)
        {
            CreateCard(selectedCards[i], i, cardsToDisplay);
        }

    }

    /// <summary>
    /// ✅ TẠO MỘT CARD
    /// </summary>
    void CreateCard(CardData cardData, int index, int totalCards)
    {
        // ✅ KIỂM TRA NULL TRƯỚC KHI TẠO
        if (cardData == null)
        {
            return;
        }

        if (cardPrefab == null || cardContainer == null)
        {
            return;
        }

        GameObject cardObj = Instantiate(cardPrefab, cardContainer);
        cardObj.name = $"Card_{cardData.cardId}";

        // ✅ Load sprite
        Transform imgCardTransform = cardObj.transform.Find("imgtCard");
        if (imgCardTransform != null)
        {
            Image imgCard = imgCardTransform.GetComponent<Image>();
            if (imgCard != null)
            {
                Sprite cardSprite = Resources.Load<Sprite>($"Image/Card/card{cardData.cardId}");

                if (cardSprite != null)
                {
                    imgCard.sprite = cardSprite;
                }
                else
                {
                }
            }
        }

        // Gắn CardData
        CardUI cardUI = cardObj.GetComponent<CardUI>();
        if (cardUI == null)
        {
            cardUI = cardObj.AddComponent<CardUI>();
        }

        // ✅ SetCardData sẽ tự động xóa object nếu cardData null
        cardUI.SetCardData(cardData);

        // ✅ CHỈ THÊM VÀO LIST NẾU OBJECT VẪN TỒN TẠI
        if (cardObj != null)
        {
            cardsInHand.Add(cardObj);
        }
    }

    /// <summary>
    /// ✅ XÓA TẤT CẢ CARDS
    /// </summary>
    void ClearCards()
    {
        List<GameObject> cardsToRemove = new List<GameObject>();

        foreach (GameObject card in cardsInHand)
        {
            if (card != null)
            {
                // ✅ KIỂM TRA: Chỉ xóa card KHÔNG PHẢI cardHT
                if (card.name != null && !card.name.StartsWith("cardHT"))
                {
                    cardsToRemove.Add(card);
                }
            }
        }

        // Xóa các cards đã đánh dấu
        foreach (GameObject card in cardsToRemove)
        {
            cardsInHand.Remove(card);
            Destroy(card);
        }

    }

    /// <summary>
    /// ✅ SỬ DỤNG CARD (gọi từ CardUI)
    /// </summary>
    public void UseCard(GameObject cardObj)
    {
        if (!cardsInHand.Contains(cardObj))
        {
            return;
        }


        // Animation bay ra
        RectTransform rt = cardObj.GetComponent<RectTransform>();
        if (rt != null)
        {
            LeanTween.moveY(rt, rt.anchoredPosition.y + 300f, 0.3f)
                .setEaseInBack();
        }

        LeanTween.scale(cardObj, Vector3.zero, 0.3f)
            .setEaseInBack()
            .setOnComplete(() =>
            {
                cardsInHand.Remove(cardObj);
                Destroy(cardObj);

                // Sắp xếp lại
                RearrangeCards();
            });
    }

    /// <summary>
    /// ✅ SẮP XẾP LẠI CARDS
    /// </summary>
    void RearrangeCards()
    {
        if (cardsInHand.Count == 0) return;

        int totalCards = cardsInHand.Count;
        float totalWidth = (totalCards - 1) * cardSpacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < cardsInHand.Count; i++)
        {
            RectTransform rt = cardsInHand[i].GetComponent<RectTransform>();
            if (rt != null)
            {
                Vector2 targetPos = new Vector2(startX + i * cardSpacing, cardYPosition);

                LeanTween.move(rt, targetPos, 0.3f)
                    .setEaseOutQuad();
            }
        }

    }

    /// <summary>
    /// ✅ LẤY SỐ LƯỢNG CARDS
    /// </summary>
    public int GetCardCount()
    {
        return cardsInHand.Count;
    }

    public void DestroyConfiguredDots(int blue, int green, int red, int white, int yellow, int count)
    {
        if (count <= 0) return;
        StartCoroutine(DestroyConfiguredDotsCoroutine(blue, green, red, white, yellow, count));
    }

    public IEnumerator DestroyConfiguredDotsCoroutine(int blue, int green, int red, int white, int yellow, int count)
    {
        if (count <= 0) yield break;

        currentState = GameState.wait;

        // ✅ LỌC TAGS
        List<string> tagsToDestroy = new List<string>();
        if (blue == 1) tagsToDestroy.Add("xanhduong Dot");
        if (green == 1) tagsToDestroy.Add("xanh Dot");
        if (red == 1) tagsToDestroy.Add("do Dot");
        if (white == 1) tagsToDestroy.Add("trang Dot");
        if (yellow == 1) tagsToDestroy.Add("vang Dot");

        if (tagsToDestroy.Count == 0)
        {
            currentState = GameState.move;
            yield break;
        }

        // ✅ GOM VỊ TRÍ
        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (allDots[x, y] != null && tagsToDestroy.Contains(allDots[x, y].tag))
                {
                    candidates.Add(new Vector2Int(x, y));
                }
            }
        }

        if (candidates.Count == 0)
        {
            currentState = GameState.move;
            yield break;
        }

        int destroyAmount = Mathf.Min(count, candidates.Count);

        // ✅ ĐÁNH DẤU DOTS
        for (int i = 0; i < destroyAmount; i++)
        {
            int index = UnityEngine.Random.Range(0, candidates.Count);
            Vector2Int pos = candidates[index];
            candidates.RemoveAt(index);

            GameObject dotGO = allDots[pos.x, pos.y];
            if (dotGO == null) continue;

            var dot = dotGO.GetComponent<Dot>();
            if (dot != null)
            {
                dot.isMathched = true;
            }
        }

        yield return null;

        // ✅ PHÁ VÀ ĐỢI PIPELINE HOÀN THÀNH
        DestroyMatches();

        // ✅ ĐỢI CHO ĐẾN KHI BOARD TRỞ VỀ TRẠNG THÁI MOVE
        float timeout = 5f;
        float elapsed = 0f;
        while (currentState != GameState.move && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (currentState != GameState.move)
        {
            currentState = GameState.move;
        }

    }
    /// <summary>
    /// ✅ KIỂM TRA NĂNG LƯỢNG SAU MỖI TURN
    /// </summary>
    private bool CheckEnergyAndShowWarning()
    {
        if (ManagerMatch.Instance == null) return true;

        int currentEnergy = int.Parse(ManagerMatch.Instance.txtNLUser.text);


        // ✅ NẾU HẾT NĂNG LƯỢNG (0)
        if (currentEnergy <= 0)
        {
            StartCoroutine(ShowEnergyWarningAndReturn("Bạn đã hết năng lượng!", true));
            return false;
        }

        // ✅ NẾU NĂNG LƯỢNG DƯỚI 5 VÀ CHƯA CẢNH BÁO
        if (currentEnergy < 5 && currentEnergy != lastCheckedEnergy)
        {
            lastCheckedEnergy = currentEnergy;
            StartCoroutine(ShowEnergyWarningAndReturn($"Năng lượng sắp hết!\nCòn lại: {currentEnergy}", false));
            return true; // Vẫn cho phép chơi tiếp
        }

        return true;
    }
    /// <summary>
    /// ✅ HIỂN THỊ CẢNH BÁO NĂNG LƯỢNG
    /// </summary>
    private IEnumerator ShowEnergyWarningAndReturn(string message, bool forceReturn)
    {
        if (energyWarningPanel == null)
        {

            if (forceReturn)
            {
                // Nếu hết năng lượng mà không có panel → về luôn
                yield return new WaitForSeconds(1f);
                ReturnToQuangTruong();
            }
            yield break;
        }

        // ✅ PAUSE GAME
        if (active != null)
        {
            active.PauseTurn();
        }
        currentState = GameState.wait;

        // ✅ HIỂN THỊ PANEL
        energyWarningPanel.SetActive(true);

        // ✅ SET TEXT
        if (energyWarningText != null)
        {
            energyWarningText.text = message;
        }

        // ✅ SETUP BUTTON
        if (energyWarningButton != null)
        {
            energyWarningButton.onClick.RemoveAllListeners();

            if (forceReturn)
            {
                // HẾT NĂNG LƯỢNG: Button về QuangTruong
                energyWarningButton.onClick.AddListener(() =>
                {
                    energyWarningPanel.SetActive(false);
                    ReturnToQuangTruong();
                });

                Text btnText = energyWarningButton.GetComponentInChildren<Text>();
                if (btnText != null)
                {
                    btnText.text = "Về Quảng Trường";
                }
            }
            else
            {
                // CẢNH BÁO: Button đóng và tiếp tục
                energyWarningButton.onClick.AddListener(() =>
                {
                    energyWarningPanel.SetActive(false);

                    // Resume game
                    if (active != null)
                    {
                        active.ResumeTurn();
                    }
                    currentState = GameState.move;
                });

                Text btnText = energyWarningButton.GetComponentInChildren<Text>();
                if (btnText != null)
                {
                    btnText.text = "Tiếp tục";
                }
            }
        }

        // ✅ ANIMATION PANEL
        CanvasGroup cg = energyWarningPanel.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = energyWarningPanel.AddComponent<CanvasGroup>();
        }

        cg.alpha = 0f;
        LeanTween.alphaCanvas(cg, 1f, 0.3f).setIgnoreTimeScale(true);

        // ✅ NẾU HẾT NĂNG LƯỢNG: Tự động về sau 3 giây
        if (forceReturn)
        {
            yield return new WaitForSecondsRealtime(3f);

            if (energyWarningPanel.activeSelf)
            {
                energyWarningPanel.SetActive(false);
                ReturnToQuangTruong();
            }
        }
    }
    /// <summary>
    /// ✅ TRỞ VỀ QUẢNG TRƯỜNG KHI HẾT NĂNG LƯỢNG
    /// </summary>
    private void ReturnToQuangTruong()
    {
        Time.timeScale = 1f;


        // ✅ XÓA FLAGS NẾU CẦN
        if (isBossBattle)
        {
            PlayerPrefs.SetString("IsBossBattle", "false");
            PlayerPrefs.DeleteKey("BossElementType");
        }

        PlayerPrefs.Save();

        UnityEngine.SceneManagement.SceneManager.LoadScene("QuangTruong");
    }

    // ==================== CHECK VALID MOVES ====================

    /// <summary>
    /// Kiểm tra xem board có còn nước đi hợp lệ không
    /// </summary>
    public bool HasValidMoves()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (allDots[i, j] == null) continue;

                // Kiểm tra 4 hướng: trái, phải, lên, xuống
                int[][] directions = new int[][]
                {
                new[] { -1, 0 }, // trái
                new[] { 1, 0 },  // phải
                new[] { 0, -1 }, // xuống
                new[] { 0, 1 }   // lên
                };

                foreach (var dir in directions)
                {
                    int newX = i + dir[0];
                    int newY = j + dir[1];

                    if (newX < 0 || newX >= width || newY < 0 || newY >= height)
                        continue;

                    if (allDots[newX, newY] == null)
                        continue;

                    // Kiểm tra xem swap này có tạo match không
                    if (WouldCreateMatch(i, j, newX, newY))
                    {
                        return true; // Tìm thấy nước đi hợp lệ
                    }
                }
            }
        }

        return false; // Không còn nước đi
    }

    /// <summary>
    /// Kiểm tra xem việc swap 2 viên có tạo match không
    /// </summary>
    private bool WouldCreateMatch(int x1, int y1, int x2, int y2)
    {
        GameObject dot1 = allDots[x1, y1];
        GameObject dot2 = allDots[x2, y2];

        if (dot1 == null || dot2 == null) return false;

        // Swap tạm thời
        allDots[x1, y1] = dot2;
        allDots[x2, y2] = dot1;

        bool hasMatch = false;

        // Kiểm tra match tại vị trí x1, y1
        if (CheckMatchAt(x1, y1, dot2.tag))
        {
            hasMatch = true;
        }

        // Kiểm tra match tại vị trí x2, y2
        if (!hasMatch && CheckMatchAt(x2, y2, dot1.tag))
        {
            hasMatch = true;
        }

        // Hoàn tác swap
        allDots[x1, y1] = dot1;
        allDots[x2, y2] = dot2;

        return hasMatch;
    }

    /// <summary>
    /// Kiểm tra có match tại vị trí cụ thể không
    /// </summary>
    private bool CheckMatchAt(int col, int row, string tag)
    {
        // Kiểm tra ngang
        int horizontalCount = 1;

        // Đếm bên trái
        int left = col - 1;
        while (left >= 0 && allDots[left, row] != null && allDots[left, row].tag == tag)
        {
            horizontalCount++;
            left--;
        }

        // Đếm bên phải
        int right = col + 1;
        while (right < width && allDots[right, row] != null && allDots[right, row].tag == tag)
        {
            horizontalCount++;
            right++;
        }

        if (horizontalCount >= 3) return true;

        // Kiểm tra dọc
        int verticalCount = 1;

        // Đếm bên dưới
        int down = row - 1;
        while (down >= 0 && allDots[col, down] != null && allDots[col, down].tag == tag)
        {
            verticalCount++;
            down--;
        }

        // Đếm bên trên
        int up = row + 1;
        while (up < height && allDots[col, up] != null && allDots[col, up].tag == tag)
        {
            verticalCount++;
            up++;
        }

        if (verticalCount >= 3) return true;

        return false;
    }

    // ==================== RESET BOARD ====================

    /// <summary>
    /// Reset toàn bộ board khi không còn nước đi
    /// </summary>
    public IEnumerator ResetBoardWhenNoMoves()
    {
        Debug.Log("[Board] Không còn nước đi! Đang reset board...");

        // Thông báo cho người chơi
        if (txtYourTurn != null)
        {
            txtYourTurn.SetActive(true);
            Text txtComponent = txtYourTurn.GetComponent<Text>();
            if (txtComponent != null)
            {
                txtComponent.text = "SHUFFLING...";
            }
        }

        // Pause game
        currentState = GameState.wait;
        if (active != null)
        {
            active.PauseTurn();
        }

        // Fade out tất cả viên
        yield return StartCoroutine(FadeOutAllDots());

        // Xóa tất cả viên hiện tại
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (allDots[i, j] != null)
                {
                    Destroy(allDots[i, j]);
                    allDots[i, j] = null;
                }
            }
        }

        yield return new WaitForSeconds(0.3f);

        // Tạo lại board mới (đảm bảo có nước đi)
        yield return StartCoroutine(CreateNewValidBoard());

        // Ẩn thông báo
        if (txtYourTurn != null)
        {
            txtYourTurn.SetActive(false);
        }

        // Resume game
        currentState = GameState.move;
        if (active != null)
        {
            active.ResumeTurn();
        }

        Debug.Log("[Board] Reset board hoàn tất!");
    }

    /// <summary>
    /// Fade out tất cả viên
    /// </summary>
    private IEnumerator FadeOutAllDots()
    {
        List<GameObject> dotsToFade = new List<GameObject>();

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (allDots[i, j] != null)
                {
                    dotsToFade.Add(allDots[i, j]);
                }
            }
        }

        foreach (GameObject dot in dotsToFade)
        {
            if (dot != null)
            {
                LeanTween.scale(dot, Vector3.zero, 0.3f).setEaseInBack();
            }
        }

        yield return new WaitForSeconds(0.3f);
    }

    /// <summary>
    /// Tạo board mới đảm bảo có nước đi hợp lệ
    /// </summary>
    private IEnumerator CreateNewValidBoard()
    {
        int maxAttempts = 10; // Số lần thử tối đa
        int attempts = 0;

        while (attempts < maxAttempts)
        {
            // Tạo board mới
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    Vector2 tempPosition = new Vector2(i, j + offSet);
                    int dotToUse = UnityEngine.Random.Range(0, dots.Length);
                    int maxIterations = 0;

                    // Tránh tạo match ban đầu
                    while (MatchesAt(i, j, dots[dotToUse]) && maxIterations < 100)
                    {
                        dotToUse = UnityEngine.Random.Range(0, dots.Length);
                        maxIterations++;
                    }

                    GameObject dot = Instantiate(dots[dotToUse], tempPosition, Quaternion.identity);
                    dot.GetComponent<Dot>().row = j;
                    dot.GetComponent<Dot>().column = i;
                    dot.transform.parent = this.transform;
                    dot.name = "(" + i + "," + j + ")";
                    dot.transform.localScale = Vector3.zero;
                    allDots[i, j] = dot;

                    // Animation spawn
                    LeanTween.scale(dot, Vector3.one, 0.3f)
                        .setEaseOutBack()
                        .setDelay(i * 0.02f + j * 0.02f);
                }
            }

            yield return new WaitForSeconds(0.5f);

            // Kiểm tra xem board mới có nước đi không
            if (HasValidMoves())
            {
                Debug.Log($"[Board] Tạo board thành công sau {attempts + 1} lần thử");
                yield break; // Board hợp lệ, thoát
            }

            // Nếu không có nước đi, xóa và thử lại
            Debug.Log($"[Board] Board không có nước đi, thử lại... ({attempts + 1}/{maxAttempts})");

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (allDots[i, j] != null)
                    {
                        Destroy(allDots[i, j]);
                        allDots[i, j] = null;
                    }
                }
            }

            attempts++;
            yield return new WaitForSeconds(0.1f);
        }

        // Nếu sau maxAttempts vẫn không tạo được board hợp lệ
        Debug.LogWarning("[Board] Không thể tạo board hợp lệ sau nhiều lần thử!");
    }

}

[Serializable]
public class AddPetDto
{
    public int petId;
    public int requestAttack;
    public int expGain;
}

[Serializable]
public class AddStoneDto
{
    public string element;
    public int level;
    public int quantity;
}

[Serializable]
public class AddExpDto
{
    public int expGain;
}

[Serializable]
public class AddGoldDto
{
    public int goldAmount;
}