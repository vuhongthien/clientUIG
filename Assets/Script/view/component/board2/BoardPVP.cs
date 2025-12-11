using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using Photon.Pun;

public class BoardPVP : MonoBehaviourPun
{
    [Header("UI Elements")]
    public GameObject[] dots;
    public GameState currentState = GameState.wait;
    public GameObject destructionEntryPrefab;

    [Header("UI information")]
    public int width;
    public int height;
    public int offSet;
    private int destroyedCount = 0;
    public int currentTurn;
    public bool isPlayerTurn;
    private int lastTurn = -1;
    private bool isDestroyingMatches = false;
    public bool hasDestroyedThisTurn = false;

    [Header("UI Object")]
    private BackGroundTitle[,] allTiles;
    public GameObject[,] allDots;
    private FindMatchesPVP findMatches;
    private Dictionary<string, int> destroyedCountByTag = new Dictionary<string, int>();
    public ActivePVP active;
    private Coroutine stableBoardCheckCoroutine;
    private bool isBoardInitialized = false;

    public GameObject destructionCountPanel;
    private Dictionary<string, Sprite> itemIcons = new Dictionary<string, Sprite>();
    public Sprite[] pieces;
    public GameObject loading;
    public Api api;
    public NotifyWin notifyWin;
    public GameObject load;
    public bool enableAutoMove = false; // Tắt auto move trong PVP

    // ==================== COUNTDOWN SYNC VARIABLES ====================
    private double countdownStartTime = 0;
    private int countdownDuration = 0;
    private int countdownTurn = 0;
    private bool isCountdownRunning = false;
    private Coroutine countdownCoroutine;

    void Start()
    {
        Debug.Log($"[START] BoardPVP khởi tạo trên Client {PhotonNetwork.LocalPlayer.ActorNumber}, IsMaster={PhotonNetwork.IsMasterClient}");

        // ==================== KHỞI TẠO COMPONENT ====================
        findMatches = FindFirstObjectByType<FindMatchesPVP>();
        active = FindFirstObjectByType<ActivePVP>();

        if (findMatches == null)
            Debug.LogError("[START] Không tìm thấy FindMatchesPVP!");
        if (active == null)
            Debug.LogError("[START] Không tìm thấy ActivePVP!");

        // ==================== KIỂM TRA DOT PREFABS ====================
        for (int i = 0; i < dots.Length; i++)
        {
            if (dots[i] == null)
            {
                Debug.LogError($"[START] Mảng dots có giá trị null tại chỉ số {i}");
            }
            else if (dots[i].GetComponent<DotPVP>() == null)
            {
                Debug.LogError($"[START] Dot prefab tại chỉ số {i} thiếu thành phần DotPVP");
            }
        }

        // ==================== LẤY SPRITE TỪ DOT PREFABS ====================
        pieces = new Sprite[dots.Length];
        for (int i = 0; i < dots.Length; i++)
        {
            if (dots[i] == null)
                continue;

            SpriteRenderer spriteRenderer = dots[i].GetComponent<SpriteRenderer>();
            pieces[i] = spriteRenderer != null ? spriteRenderer.sprite : null;

            if (pieces[i] == null)
                Debug.LogWarning($"[START] Không tìm thấy SpriteRenderer trên GameObject: {dots[i].name}");
        }

        // ==================== KHỞI TẠO ITEM ICONS ====================
        itemIcons = new Dictionary<string, Sprite>
        {
            { "vang Dot", pieces[2] },
            { "xanhduong Dot", pieces[5] },
            { "do Dot", pieces[3] },
            { "xanh Dot", pieces[4] },
            { "trang Dot", pieces[1] },
            { "tim Dot", pieces[0] }
        };

        Debug.Log("[START] Khởi tạo itemIcons thành công");

        // ==================== ĐĂNG KÝ EVENT ====================
        if (active != null)
        {
            active.OnTurnEnd += HandleTurnEnd;
            currentTurn = 1;
            isPlayerTurn = PhotonNetwork.IsMasterClient; // Master đi trước
            Debug.Log("[START] Đăng ký OnTurnEnd event thành công");
        }

        // ==================== KHỞI TẠO ARRAYS ====================
        allTiles = new BackGroundTitle[width, height];
        allDots = new GameObject[width, height];

        Debug.Log($"[START] Khởi tạo array: {width}x{height}");

        // ==================== KIỂM TRA PHOTON NETWORK ====================
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogError("[START] Không trong Photon Room!");
            return;
        }

        Debug.Log($"[START] Trong Photon Room, IsMasterClient={PhotonNetwork.IsMasterClient}, PlayerCount={PhotonNetwork.PlayerList.Length}");

        // ==================== CẢ MASTER VÀ CLIENT ĐỀU CHỜ ====================
        StartCoroutine(InitializeBoardProcess());
    }

    // ==================== MASTER/CLIENT ĐỀU CHẠY COROUTINE NÀY ====================
    private IEnumerator InitializeBoardProcess()
    {
        // Chờ player join đủ
        yield return new WaitUntil(() => PhotonNetwork.PlayerList.Length >= 2 || !PhotonNetwork.IsMasterClient);

        Debug.Log($"[INIT BOARD] PlayerCount = {PhotonNetwork.PlayerList.Length}");
        yield return new WaitForSeconds(0.5f);

        if (PhotonNetwork.IsMasterClient)
        {
            // ✅ MASTER: TẠO BOARD LOCAL, GỬI CHO CLIENT
            Debug.Log("[INIT BOARD] Master bắt đầu tạo board");
            yield return StartCoroutine(setUp());

            // Chờ Client tạo board xong
            Debug.Log("[INIT BOARD] Master chờ Client ready...");
            yield return StartCoroutine(WaitForClientBoardReady());

            // Bắt đầu game
            Debug.Log("[INIT BOARD] Cả 2 player ready! Bắt đầu game");
            photonView.RPC("StartGameForAll", RpcTarget.AllBuffered);
        }
        else
        {
            // ✅ CLIENT: CHỜ NHẬN BOARD TỪ MASTER
            Debug.Log("[INIT BOARD] Client chờ nhận board từ Master...");
            yield return new WaitUntil(() => isBoardInitialized);
            Debug.Log("[INIT BOARD] Client nhận board xong!");

            // Thông báo cho Master
            photonView.RPC("ClientBoardReady", RpcTarget.MasterClient);
            Debug.Log("[INIT BOARD] Client gửi ready signal cho Master");
        }
    }

    // ==================== MASTER CHỜ CLIENT READY ====================
    private IEnumerator WaitForClientBoardReady()
    {
        float timeout = 30f; // Timeout 30 giây
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            // Kiểm tra nếu Master nhận được tín hiệu từ Client
            if (IsClientBoardReady)
            {
                Debug.Log("[WAIT CLIENT] Client đã sẵn sàng!");
                yield break;
            }

            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
            Debug.Log($"[WAIT CLIENT] Chờ Client... {elapsed:F1}s");
        }

        Debug.LogWarning($"[WAIT CLIENT] Timeout sau {timeout}s, tiếp tục...");
    }

    // ==================== FLAG KIỂM TRA CLIENT READY ====================
    private bool IsClientBoardReady = false;

    [PunRPC]
    private void ClientBoardReady()
    {
        Debug.Log("[RPC] Master nhận được ClientBoardReady từ Client");
        IsClientBoardReady = true;
    }

    // ==================== SIGNAL BẮT ĐẦU GAME ====================
    [PunRPC]
    private void StartGameForAll()
    {
        Debug.Log($"[START GAME] 🎮 Player {PhotonNetwork.LocalPlayer.ActorNumber} bắt đầu game");
        isBoardInitialized = true;
        currentState = GameState.move;

        // ✅ KHỞI TẠO TURN CHO TẤT CẢ CLIENT
        currentTurn = 1;
        isPlayerTurn = PhotonNetwork.IsMasterClient;

        // ✅ CHỈ MASTER MỚI ĐƯỢC START COUNTDOWN ĐẦU TIÊN
        if (PhotonNetwork.IsMasterClient && active != null)
        {
            Debug.Log("[START GAME] ⏰ Master khởi động countdown đầu tiên");
            StartCoroutine(DelayedStartCountdown());
        }
        else
        {
            Debug.Log("[START GAME] ⏳ Client chờ nhận countdown từ Master");
        }
    }

    private IEnumerator DelayedStartCountdown()
    {
        yield return new WaitForSeconds(1f);
        if (active != null)
        {
            active.StartCountdown();
        }
    }

    // ==================== HÀM LẤY BOARD DATA TỪ ALLOTS ====================
    private int[,] GetCurrentBoardData()
    {
        int[,] boardData = new int[width, height];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (allDots[i, j] != null)
                {
                    // Tìm index của dot này trong mảng dots
                    for (int k = 0; k < dots.Length; k++)
                    {
                        if (allDots[i, j].tag == dots[k].tag)
                        {
                            boardData[i, j] = k;
                            break;
                        }
                    }
                }
            }
        }
        return boardData;
    }

    void Update()
    {
        // Không dùng auto move trong PVP
    }

    // ==================== SETUP BOARD (CHỈ MASTER) ====================
    private IEnumerator setUp()
    {
        Debug.Log("[SETUP] Master bắt đầu tạo board...");

        if (loading != null)
        {
            loading.SetActive(true);
        }

        yield return new WaitForSeconds(2f);

        int[,] boardData = new int[width, height];

        Debug.Log("[SETUP] Bắt đầu tạo dots...");
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

                if (dots[dotToUse] == null)
                {
                    Debug.LogError($"[SETUP] Dot prefab null tại chỉ số {dotToUse}");
                    yield break;
                }

                boardData[i, j] = dotToUse;

                GameObject dot = Instantiate(dots[dotToUse], temPosition, Quaternion.identity);
                if (dot == null || dot.GetComponent<DotPVP>() == null)
                {
                    Debug.LogError($"[SETUP] Không thể tạo dot tại ({i},{j})");
                    yield break;
                }

                DotPVP dotComponent = dot.GetComponent<DotPVP>();
                dotComponent.row = j;
                dotComponent.column = i;
                dot.transform.parent = this.transform;
                dot.name = $"({i},{j})";
                allDots[i, j] = dot;
            }
        }

        Debug.Log("[SETUP] Tạo xong tất cả dots");

        // Fade out loading
        if (loading != null)
        {
            CanvasGroup canvasGroup = loading.gameObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = loading.gameObject.AddComponent<CanvasGroup>();
            }
            yield return StartCoroutine(FadeOut(canvasGroup, 0.5f));
        }

        isBoardInitialized = true;
        Debug.Log("[SETUP] Board local khởi tạo xong");

        yield return new WaitForSeconds(1.5f);

        // ✅ CHỈ GỬI CHO CLIENT KHÁC, KHÔNG GỬI CHO MASTER
        string boardString = CovertArray2DToString(boardData);
        Debug.Log($"[SETUP] Master gửi board cho Client (Others only)");
        photonView.RPC("SyncBoard", RpcTarget.Others, boardString, PhotonNetwork.Time);

        if (loading != null)
        {
            loading.SetActive(false);
        }

        Debug.Log("[SETUP] Hoàn tất!");
    }

    private string CovertArray2DToString(int[,] data)
    {
        if (data == null)
        {
            Debug.LogError("Dữ liệu bàn chơi null!");
            return "";
        }

        List<int> dumpData = new List<int>();
        foreach (var i in data)
        {
            dumpData.Add(i);
        }
        string result = string.Join(",", dumpData);
        Debug.Log($"Chuỗi hóa dữ liệu bàn chơi: {result}");
        return result;
    }

    private int[,] ConvertStringToArray(string str)
    {
        string[] data = str.Split(',');
        if (data.Length != width * height)
        {
            Debug.LogError($"Độ dài dữ liệu bàn chơi không hợp lệ: kỳ vọng {width * height}, nhận được {data.Length}");
            return new int[width, height];
        }
        int[,] _board = new int[width, height];
        int index = 0;
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (index < data.Length && int.TryParse(data[index], out int value) && value >= 0 && value < dots.Length)
                {
                    _board[i, j] = value;
                }
                else
                {
                    Debug.LogError($"Dữ liệu bàn chơi không hợp lệ tại chỉ số {index}: {data[index]}");
                    _board[i, j] = 0;
                }
                index++;
            }
        }
        return _board;
    }

    [PunRPC]
    private void SyncBoard(string serializedData, double timestamp)
    {
        Debug.Log($"[SYNC BOARD] Player {PhotonNetwork.LocalPlayer.ActorNumber} nhận SyncBoard");

        // ✅ MASTER KHÔNG BAO GIỜ NHẬN RPC NÀY (VÌ DÙNG RpcTarget.Others)
        // Kiểm tra timestamp
        if (PhotonNetwork.Time < timestamp)
        {
            Debug.LogWarning($"[SYNC BOARD] Bỏ qua RPC lỗi thời: timestamp={timestamp}");
            return;
        }

        // ✅ CLIENT: XÓA BOARD CŨ (NẾU CÓ)
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

        int[,] boardData = ConvertStringToArray(serializedData);
        if (boardData == null)
        {
            Debug.LogError("[SYNC BOARD] BoardData null sau ConvertStringToArray");
            return;
        }

        // ✅ TẠO BOARD MỚI
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                int dotIndex = boardData[i, j];

                if (dotIndex < 0 || dotIndex >= dots.Length)
                {
                    Debug.LogError($"[SYNC BOARD] Chỉ số dot không hợp lệ {dotIndex} tại ({i},{j})");
                    continue;
                }
                if (dots[dotIndex] == null)
                {
                    Debug.LogError($"[SYNC BOARD] Dot prefab null tại chỉ số {dotIndex}");
                    continue;
                }

                Vector2 tempPosition = new Vector2(i, j + offSet);
                GameObject dot = Instantiate(dots[dotIndex], tempPosition, Quaternion.identity);

                if (dot == null || dot.GetComponent<DotPVP>() == null)
                {
                    Debug.LogError($"[SYNC BOARD] Không thể tạo dot tại ({i},{j})");
                    continue;
                }

                DotPVP dotComponent = dot.GetComponent<DotPVP>();
                dotComponent.row = j;
                dotComponent.column = i;
                dot.transform.parent = this.transform;
                dot.name = $"({i},{j})";
                allDots[i, j] = dot;
            }
        }

        Debug.Log($"[SYNC BOARD] Client {PhotonNetwork.LocalPlayer.ActorNumber} tạo board xong ({width}x{height})");
        isBoardInitialized = true;
        currentState = GameState.move;
    }

    [PunRPC]
    private void SyncMove(int fromCol, int fromRow, int toCol, int toRow)
    {
        Debug.Log($"[SYNC MOVE] Từ ({fromCol},{fromRow}) đến ({toCol},{toRow})");

        // Kiểm tra bounds
        if (fromCol < 0 || fromCol >= width || fromRow < 0 || fromRow >= height ||
            toCol < 0 || toCol >= width || toRow < 0 || toRow >= height)
        {
            Debug.LogError($"Tọa độ di chuyển không hợp lệ: từ ({fromCol},{fromRow}) đến ({toCol},{toRow})");
            return;
        }

        GameObject dot1 = allDots[fromCol, fromRow];
        GameObject dot2 = allDots[toCol, toRow];

        if (dot1 == null || dot2 == null)
        {
            Debug.LogError($"Dot null tại từ ({fromCol},{fromRow}) hoặc đến ({toCol},{toRow})");
            return;
        }

        DotPVP dot1Component = dot1.GetComponent<DotPVP>();
        DotPVP dot2Component = dot2.GetComponent<DotPVP>();

        if (dot1Component == null || dot2Component == null)
        {
            Debug.LogError($"Thiếu thành phần DotPVP");
            return;
        }

        // Lưu vị trí cũ
        dot1Component.previousColumn = dot1Component.column;
        dot1Component.previousRow = dot1Component.row;
        dot2Component.previousColumn = dot2Component.column;
        dot2Component.previousRow = dot2Component.row;

        // Swap column/row
        int tempCol = dot1Component.column;
        int tempRow = dot1Component.row;

        dot1Component.column = dot2Component.column;
        dot1Component.row = dot2Component.row;

        dot2Component.column = tempCol;
        dot2Component.row = tempRow;

        // Swap trong allDots array
        allDots[dot1Component.column, dot1Component.row] = dot1;
        allDots[dot2Component.column, dot2Component.row] = dot2;

        Debug.Log($"[SYNC MOVE] Swap hoàn tất: dot1 -> ({dot1Component.column},{dot1Component.row}), dot2 -> ({dot2Component.column},{dot2Component.row})");
        dot1Component.otherDot = dot2;
        // Chỉ Master kiểm tra match sau swap
        if (PhotonNetwork.IsMasterClient)
        {

            StartCoroutine(dot1Component.CheckMoveCo());
        }
    }

    private IEnumerator VerifyBoard(int[,] expectedBoard)
    {
        yield return new WaitForSeconds(1.5f);

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (allDots[i, j] == null)
                {
                    Debug.LogError($"Đồng bộ bàn chơi thất bại tại vị trí ({i},{j}): DotPVP null");
                    TriggerResync(expectedBoard);
                    yield break;
                }
                DotPVP dotComponent = allDots[i, j].GetComponent<DotPVP>();
                if (dotComponent == null)
                {
                    Debug.LogError($"Đồng bộ bàn chơi thất bại tại vị trí ({i},{j}): Thiếu thành phần DotPVP");
                    TriggerResync(expectedBoard);
                    yield break;
                }

                string expectedTag = dots[expectedBoard[i, j]]?.tag;
                string actualTag = allDots[i, j].tag;
                if (expectedTag == null || actualTag != expectedTag)
                {
                    Debug.LogError($"Đồng bộ bàn chơi thất bại tại vị trí ({i},{j}): Kỳ vọng tag {expectedTag}, nhận được {actualTag}");
                    TriggerResync(expectedBoard);
                    yield break;
                }
            }
        }

        Debug.Log("Đồng bộ bàn chơi thành công.");
        currentState = GameState.move;
    }

    private void TriggerResync(int[,] boardData)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Kích hoạt đồng bộ lại bàn chơi...");
            photonView.RPC("SyncBoard", RpcTarget.AllBuffered, CovertArray2DToString(boardData), PhotonNetwork.Time);
        }
    }

    private bool MatchesAt(int column, int row, GameObject piece)
    {
        if (column > 1 && row > 1)
        {
            if (allDots[column - 1, row]?.tag == piece.tag && allDots[column - 2, row]?.tag == piece.tag)
            {
                return true;
            }
            if (allDots[column, row - 1]?.tag == piece.tag && allDots[column, row - 2]?.tag == piece.tag)
            {
                return true;
            }
        }
        else if (column <= 1 || row <= 1)
        {
            if (row > 1 && allDots[column, row - 1]?.tag == piece.tag && allDots[column, row - 2]?.tag == piece.tag)
            {
                return true;
            }
            if (column > 1 && allDots[column - 1, row]?.tag == piece.tag && allDots[column - 2, row]?.tag == piece.tag)
            {
                return true;
            }
        }
        return false;
    }

    private void DestroyMatchesAt(int column, int row)
    {
        isDestroyingMatches = true;
        if (isDestroyingMatches && active != null)
        {
            active.StopCountdown();
            if (allDots[column, row] != null && allDots[column, row].GetComponent<DotPVP>().isMathched)
            {
                string dotTag = allDots[column, row].tag.ToString();
                Debug.Log($"Phá dot tại ({column},{row}) với tag={dotTag}");
                if (destroyedCountByTag.ContainsKey(dotTag))
                {
                    destroyedCountByTag[dotTag]++;
                }
                else
                {
                    destroyedCountByTag[dotTag] = 1;
                }
                findMatches.currentMatches.Remove(allDots[column, row]);
                Destroy(allDots[column, row]);
                allDots[column, row] = null;
                destroyedCount++;
            }
        }
    }

    private void ResetDestroyedCounts()
    {
        destroyedCountByTag.Clear();
        Debug.Log("Đặt lại số lượng phá hủy.");
    }

    private void OnMouseDown()
    {
        ResetDestroyedCounts();
    }

    public IEnumerator WaitAndDestroyMatches()
    {
        currentState = GameState.wait;

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

        StartCoroutine(DecreaseRowCo());
        yield return new WaitForSeconds(1f);
    }

    public void DestroyMatches()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(WaitAndDestroyMatches());
            photonView.RPC("SyncDestroyMatches", RpcTarget.Others);
        }
    }

    [PunRPC]
    private void SyncDestroyMatches()
    {
        StartCoroutine(WaitAndDestroyMatches());
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
                    allDots[i, j].GetComponent<DotPVP>().row -= nullCount;
                    allDots[i, j] = null;
                }
            }
            nullCount = 0;
        }
        yield return new WaitForSeconds(.2f);
        StartCoroutine(FillBoardCo());
    }

    private void RefillBoard()
{
    if (!PhotonNetwork.IsMasterClient) return;

    List<(int x, int y, int dotIndex)> refillData = new List<(int, int, int)>();

    for (int i = 0; i < width; i++)
    {
        for (int j = 0; j < height; j++)
        {
            if (allDots[i, j] == null)
            {
                Vector2 tempPosition = new Vector2(i, j + offSet);
                int dotToUse = UnityEngine.Random.Range(0, dots.Length);
                GameObject piece = Instantiate(dots[dotToUse], tempPosition, Quaternion.identity);
                allDots[i, j] = piece;
                piece.GetComponent<DotPVP>().row = j;
                piece.GetComponent<DotPVP>().column = i;
                piece.transform.parent = this.transform;
                piece.name = "(" + i + "," + j + ")";
                refillData.Add((i, j, dotToUse));
            }
        }
    }

    if (refillData.Count > 0)
    {
        int[] xPositions = refillData.Select(d => d.x).ToArray();
        int[] yPositions = refillData.Select(d => d.y).ToArray();
        int[] dotIndices = refillData.Select(d => d.dotIndex).ToArray();
        
        // ✅ CHỈ GỬI CHO CLIENT KHÁC, KHÔNG GỬI CHO MASTER
        photonView.RPC("SyncRefillBoard", RpcTarget.Others, xPositions, yPositions, dotIndices, PhotonNetwork.Time);
        
        Debug.Log($"[REFILL] Master tạo {refillData.Count} dots và gửi cho Others");
    }
}

    [PunRPC]
private void SyncRefillBoard(int[] xPositions, int[] yPositions, int[] dotIndices, double timestamp)
{
    // ✅ MASTER KHÔNG BAO GIỜ VÀO ĐÂY (vì dùng RpcTarget.Others)
    if (PhotonNetwork.IsMasterClient)
    {
        Debug.LogWarning("[REFILL] ⚠️ Master không nên nhận RPC này!");
        return;
    }

    if (PhotonNetwork.Time < timestamp)
    {
        Debug.LogWarning($"[REFILL] Bỏ qua RPC lỗi thời: timestamp={timestamp}");
        return;
    }

    Debug.Log($"[REFILL] Client {PhotonNetwork.LocalPlayer.ActorNumber} nhận refill {xPositions.Length} dots");

    // Validation
    if (xPositions.Length != yPositions.Length || xPositions.Length != dotIndices.Length)
    {
        Debug.LogError($"[REFILL] Dữ liệu không hợp lệ!");
        return;
    }

    // ✅ CHỈ CLIENT TẠO DOTS
    for (int i = 0; i < xPositions.Length; i++)
    {
        int x = xPositions[i];
        int y = yPositions[i];
        int dotIndex = dotIndices[i];

        // Validation
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            Debug.LogError($"[REFILL] Tọa độ không hợp lệ: ({x},{y})");
            continue;
        }

        if (dotIndex < 0 || dotIndex >= dots.Length || dots[dotIndex] == null)
        {
            Debug.LogError($"[REFILL] Dot index không hợp lệ: {dotIndex}");
            continue;
        }

        // ✅ XÓA DOT CŨ (NẾU CÓ)
        if (allDots[x, y] != null)
        {
            Debug.Log($"[REFILL] Xóa dot cũ tại ({x},{y})");
            Destroy(allDots[x, y]);
            allDots[x, y] = null;
        }

        // ✅ TẠO DOT MỚI
        Vector2 tempPosition = new Vector2(x, y + offSet);
        GameObject piece = Instantiate(dots[dotIndex], tempPosition, Quaternion.identity);

        if (piece == null || piece.GetComponent<DotPVP>() == null)
        {
            Debug.LogError($"[REFILL] Không thể tạo dot tại ({x},{y})");
            continue;
        }

        DotPVP dotComp = piece.GetComponent<DotPVP>();
        dotComp.row = y;
        dotComp.column = x;
        piece.transform.parent = this.transform;
        piece.name = $"({x},{y})";
        allDots[x, y] = piece;
    }

    Debug.Log($"[REFILL] Client {PhotonNetwork.LocalPlayer.ActorNumber} refill hoàn tất");
}

    private bool MatchesOnBoard()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (allDots[i, j] != null && allDots[i, j].GetComponent<DotPVP>().isMathched)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private IEnumerator FillBoardCo()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            RefillBoard();
        }
        yield return new WaitForSeconds(.5f);

        while (MatchesOnBoard())
        {
            yield return new WaitForSeconds(.2f);
            DestroyMatches();
            yield return new WaitForSeconds(.4f);
        }

        yield return new WaitForSeconds(.4f);
        currentState = GameState.move;

        if (stableBoardCheckCoroutine != null)
        {
            StopCoroutine(stableBoardCheckCoroutine);
        }
        stableBoardCheckCoroutine = StartCoroutine(CheckForStableBoardAfterFill());
    }

    public IEnumerator CheckForStableBoardAfterFill()
    {
        float checkInterval = 0.1f;
        float maxWaitTime = 1f;
        float elapsedTime = 0f;

        int maxAttempts = 50;
        int attempts = 0;

        while (attempts < maxAttempts)
        {
            if (CheckBoardStable())
            {
                elapsedTime += checkInterval;
                if (elapsedTime >= maxWaitTime)
                {
                    Debug.Log("[BOARD] Board ổn định!");
                    break;
                }
            }
            else
            {
                elapsedTime = 0f;
            }

            yield return new WaitForSeconds(checkInterval);
            attempts++;
        }

        if (attempts >= maxAttempts)
        {
            Debug.LogWarning("[BOARD] Timeout khi chờ board ổn định, tiếp tục...");
        }

        displayDestroy();
        yield return HandleUI();
        if (PhotonNetwork.IsMasterClient && active != null)
        {
            Debug.Log("[STABLE BOARD] Master restart countdown sau khi board ổn định");
            yield return new WaitForSeconds(0.5f);
            active.StartCountdown();
        }
    }

    private void displayDestroy()
    {
        foreach (Transform child in destructionCountPanel.transform)
        {
            Destroy(child.gameObject);
        }

        List<string> customOrder = new List<string> { "xanh Dot", "xanhduong Dot", "do Dot", "tim Dot", "trang Dot", "vang Dot" };
        var sortedCounts = destroyedCountByTag
            .Where(kvp => kvp.Value > 0)
            .OrderBy(kvp => customOrder.IndexOf(kvp.Key) >= 0 ? customOrder.IndexOf(kvp.Key) : int.MaxValue)
            .ThenByDescending(kvp => kvp.Value)
            .ToList();

        foreach (var kvp in sortedCounts)
        {
            GameObject entry = Instantiate(destructionEntryPrefab, destructionCountPanel.transform);
            entry.name = kvp.Key;

            Image iconImage = entry.transform.Find("Icon").GetComponent<Image>();
            Text countText = entry.transform.Find("CountText").GetComponent<Text>();

            if (itemIcons.TryGetValue(kvp.Key, out Sprite iconSprite))
            {
                iconImage.sprite = iconSprite;
            }
            else
            {
                Debug.LogWarning($"Không tìm thấy icon cho {kvp.Key}");
            }
            countText.text = $"{kvp.Value}";

            CanvasGroup canvasGroup = entry.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = entry.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 1f;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            string[] tags = sortedCounts.Select(kvp => kvp.Key).ToArray();
            int[] counts = sortedCounts.Select(kvp => kvp.Value).ToArray();
            photonView.RPC("SyncDisplayAndFadeEffects", RpcTarget.All, tags, counts);
        }
    }

    [PunRPC]
    private void SyncDestroyedCounts(string[] tags, int[] counts)
    {
        destroyedCountByTag.Clear();
        for (int i = 0; i < tags.Length; i++)
        {
            destroyedCountByTag[tags[i]] = counts[i];
        }
        Debug.Log("Nhận số lượng phá hủy trên client.");
    }

    private IEnumerator HandleUI()
    {
        if (active == null || destructionCountPanel == null) yield break;

        photonView.RPC("SyncHideAllItems", RpcTarget.AllBuffered);
        yield return new WaitForSeconds(0.2f);

        List<(string itemType, int pieceCount)> itemsToProcess = new List<(string, int)>();
        List<string> customOrder = new List<string> { "xanh Dot", "xanhduong Dot", "do Dot", "tim Dot", "trang Dot", "vang Dot" };
        var sortedCounts = destroyedCountByTag
            .Where(kvp => kvp.Value > 0)
            .OrderBy(kvp => customOrder.IndexOf(kvp.Key) >= 0 ? customOrder.IndexOf(kvp.Key) : int.MaxValue)
            .ThenByDescending(kvp => kvp.Value)
            .ToList();

        foreach (var kvp in sortedCounts)
        {
            itemsToProcess.Add((kvp.Key, kvp.Value));
        }

        bool isMyTurn = (currentTurn % 2 == 1 && PhotonNetwork.IsMasterClient) ||
                        (currentTurn % 2 == 0 && !PhotonNetwork.IsMasterClient);
        Debug.Log($"[{PhotonNetwork.LocalPlayer.ActorNumber}] Là lượt của mình: {isMyTurn}, currentTurn={currentTurn}, Là MasterClient={PhotonNetwork.IsMasterClient}");

        if (isMyTurn)
        {
            Debug.Log($"[{PhotonNetwork.LocalPlayer.ActorNumber}] Xử lý lượt của mình, tính toán chỉ số.");
            foreach (var item in itemsToProcess)
            {
                Debug.Log($"Xử lý item: {item.itemType}, số lượng={item.pieceCount}");
                active.CalculateOutputs(item.itemType, item.pieceCount);
            }

            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log("Master gửi trạng thái sau khi tính toán.");
                active.UpdatePlayerStats();
            }
            else
            {
                Debug.Log($"Client {PhotonNetwork.LocalPlayer.ActorNumber} gửi trạng thái đến Master.");
                active.photonView.RPC(
                    "SyncStateFromClient",
                    RpcTarget.MasterClient,
                    active.MauPlayer, active.ManaPlayer, active.NoPlayer,
                    active.MauNPC, active.ManaNPC, active.NoNPC
                );
            }

            string[] tags = itemsToProcess.Select(item => item.itemType).ToArray();
            int[] counts = itemsToProcess.Select(item => item.pieceCount).ToArray();
            photonView.RPC("SyncDisplayAndFadeEffects", RpcTarget.AllBuffered, tags, counts);

            yield return new WaitForSeconds(itemsToProcess.Count * 1.5f + 0.5f);

            photonView.RPC("SyncClearUI", RpcTarget.AllBuffered);
            destroyedCountByTag.Clear();
            active.SummaryOutputs();
            active.resetOutput();

            yield return StartCoroutine(EndTurn());
        }
        else
        {
            Debug.Log($"[{PhotonNetwork.LocalPlayer.ActorNumber}] Đợi lượt của đối thủ hoàn thành...");
            yield return new WaitUntil(() => destructionCountPanel.transform.childCount == 0);
            destroyedCountByTag.Clear();
            active.resetOutput();
            active.UpdateSlider();
            yield return new WaitUntil(() => currentTurn > lastTurn);
        }
    }

    [PunRPC]
    private void SyncHideAllItems()
    {
        HideAllItems();
        Debug.Log($"Client {PhotonNetwork.LocalPlayer.ActorNumber} đồng bộ ẩn các item và hiển thị bảng phá hủy.");
    }

    [PunRPC]
    private void SyncDisplayAndFadeEffects(string[] tags, int[] counts)
    {
        if (tags.Length != counts.Length)
        {
            Debug.LogError($"Dữ liệu hiệu ứng không hợp lệ: độ dài tags ({tags.Length}) không khớp với độ dài counts ({counts.Length})");
            return;
        }

        Debug.Log($"Client {PhotonNetwork.LocalPlayer.ActorNumber} nhận SyncDisplayAndFadeEffects với {tags.Length} item.");

        foreach (Transform child in destructionCountPanel.transform)
        {
            Destroy(child.gameObject);
        }

        List<GameObject> entries = new List<GameObject>();
        for (int i = 0; i < tags.Length; i++)
        {
            GameObject entry = Instantiate(destructionEntryPrefab, destructionCountPanel.transform);
            entry.name = tags[i];

            Image iconImage = entry.transform.Find("Icon").GetComponent<Image>();
            Text countText = entry.transform.Find("CountText").GetComponent<Text>();

            if (itemIcons.TryGetValue(tags[i], out Sprite iconSprite))
            {
                iconImage.sprite = iconSprite;
            }
            else
            {
                Debug.LogWarning($"Không tìm thấy icon cho {tags[i]}");
            }
            countText.text = $"{counts[i]}";

            CanvasGroup canvasGroup = entry.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = entry.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 1f;

            entries.Add(entry);
        }

        StartCoroutine(ProcessFadeOutEffects(tags, entries));
    }

    private IEnumerator ProcessFadeOutEffects(string[] tags, List<GameObject> entries)
    {
        active.UpdateSlider();

        for (int i = 0; i < tags.Length; i++)
        {
            if (i >= entries.Count || entries[i] == null)
            {
                Debug.LogWarning($"Entry tại chỉ số {i} null hoặc thiếu.");
                continue;
            }

            GameObject entry = entries[i];
            string itemType = tags[i];

            CanvasGroup canvasGroup = entry.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                Debug.LogWarning($"Thiếu CanvasGroup trên entry: {entry.name}");
                continue;
            }

            Debug.Log($"Client {PhotonNetwork.LocalPlayer.ActorNumber} xử lý fade-out cho item: {itemType} vào {System.DateTime.Now:HH:mm:ss.fff}");

            yield return StartCoroutine(active.OutputsParam(itemType));

            yield return StartCoroutine(FadeOut(canvasGroup, 0.5f));
            if (entry != null)
            {
                Destroy(entry);
            }

            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log($"Client {PhotonNetwork.LocalPlayer.ActorNumber} hoàn thành hiệu ứng fade-out.");
    }

    [PunRPC]
    private void SyncDisplayEffects(string[] tags, int[] counts)
    {
        if (tags.Length != counts.Length)
        {
            Debug.LogError($"Dữ liệu hiệu ứng không hợp lệ: độ dài tags ({tags.Length}) không khớp với độ dài counts ({counts.Length})");
            return;
        }

        StartCoroutine(DisplayEffectsCoroutine(tags, counts));
    }

    private IEnumerator DisplayEffectsCoroutine(string[] tags, int[] counts)
    {
        active.UpdateSlider();

        for (int i = 0; i < tags.Length; i++)
        {
            yield return StartCoroutine(DisplayEffectForItem(tags[i], counts[i]));
            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator DisplayEffectForItem(string itemType, int pieceCount)
    {
        GameObject entry = Instantiate(destructionEntryPrefab, destructionCountPanel.transform);
        entry.name = itemType;

        Image iconImage = entry.transform.Find("Icon").GetComponent<Image>();
        Text countText = entry.transform.Find("CountText").GetComponent<Text>();

        if (itemIcons.TryGetValue(itemType, out Sprite iconSprite))
        {
            iconImage.sprite = iconSprite;
        }
        else
        {
            Debug.LogWarning($"Không tìm thấy icon cho {itemType}");
        }
        countText.text = $"{pieceCount}";

        CanvasGroup canvasGroup = entry.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = entry.AddComponent<CanvasGroup>();
        }

        Debug.Log($"Hiển thị hiệu ứng cho item: {itemType} với số lượng {pieceCount} vào {System.DateTime.Now:HH:mm:ss.fff}");

        yield return StartCoroutine(active.OutputsParam(itemType));

        yield return StartCoroutine(FadeOut(canvasGroup, 0.5f));
        if (entry != null)
        {
            Destroy(entry);
        }
    }

    [PunRPC]
    private void SyncClearUI()
    {
        if (destructionCountPanel != null)
        {
            foreach (Transform child in destructionCountPanel.transform)
            {
                Destroy(child.gameObject);
            }
            destructionCountPanel.SetActive(false);
        }
        ShowItems();
        active.UpdateSlider();
        currentState = GameState.move;
        Debug.Log($"Client {PhotonNetwork.LocalPlayer.ActorNumber} xóa giao diện và hiển thị bàn chơi.");
    }

    // ==================== TURN MANAGEMENT ====================
    private IEnumerator EndTurn()
    {
        Debug.Log($"[END TURN] Kết thúc lượt {currentTurn}");

        int nextTurn = currentTurn + 1;
        photonView.RPC("SyncTurn", RpcTarget.AllBuffered, nextTurn, PhotonNetwork.Time);

        // Chỉ Master mới được start countdown mới
        if (PhotonNetwork.IsMasterClient && active != null)
        {
            yield return new WaitForSeconds(0.5f);
            active.StartCountdown();
        }

        Debug.Log($"[END TURN] Bắt đầu lượt {nextTurn}");
        yield return null;
    }

    [PunRPC]
    private void SyncTurn(int newTurn, double networkTime)
    {
        Debug.Log($"[SYNC TURN] Client {PhotonNetwork.LocalPlayer.ActorNumber} nhận lượt {newTurn}");

        lastTurn = currentTurn;
        currentTurn = newTurn;
        isPlayerTurn = (currentTurn % 2 == 1) == PhotonNetwork.IsMasterClient;

        Debug.Log($"[SYNC TURN] Lượt hiện tại: {currentTurn}, isPlayerTurn: {isPlayerTurn}");

        // ✅ THÊM: Restart countdown sau khi sync turn
        if (active != null)
        {
            StartCoroutine(RestartCountdownAfterSync());
        }
    }

    // ✅ THÊM COROUTINE MỚI
    private IEnumerator RestartCountdownAfterSync()
    {
        yield return new WaitForSeconds(0.3f); // Chờ UI update xong

        // Chỉ Master mới được start countdown (nhất quán với logic hiện tại)
        if (PhotonNetwork.IsMasterClient && active != null)
        {
            Debug.Log($"[RESTART COUNTDOWN] Master khởi động countdown cho turn {currentTurn}");
            active.StartCountdown();
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
                if (allDots[i, j] == null)
                {
                    Debug.LogWarning($"[BOARD CHECK] Null dot tại ({i},{j})");
                    return false;
                }

                DotPVP dotComponent = allDots[i, j].GetComponent<DotPVP>();
                if (dotComponent == null)
                {
                    Debug.LogWarning($"[BOARD CHECK] Missing DotPVP tại ({i},{j})");
                    return false;
                }

                // Kiểm tra nếu dot được match
                if (dotComponent.isMathched)
                {
                    Debug.Log($"[BOARD CHECK] Còn match tại ({i},{j})");
                    return false;
                }
            }
        }
        return true;
    }

    public bool IsPlayerAllowedToMove()
    {
        if (!isBoardInitialized)
        {
            Debug.Log("[MOVE CHECK] Board chưa khởi tạo");
            return false;
        }
        if (currentState != GameState.move)
        {
            Debug.Log($"[MOVE CHECK] State không phải move: {currentState}");
            return false;
        }

        bool allowed = (currentTurn % 2 == 1 && PhotonNetwork.IsMasterClient) ||
                      (currentTurn % 2 == 0 && !PhotonNetwork.IsMasterClient);

        Debug.Log($"[MOVE CHECK] Player {PhotonNetwork.LocalPlayer.ActorNumber} - Turn {currentTurn} - Allowed: {allowed}");
        return allowed;
    }

    private IEnumerator FadeOut(CanvasGroup canvasGroup, float duration)
    {
        if (canvasGroup == null)
        {
            Debug.LogWarning("CanvasGroup null trong FadeOut.");
            yield break;
        }

        float elapsedTime = 0f;
        float startAlpha = canvasGroup.alpha;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / duration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.gameObject.SetActive(false);
    }

    public void HideAllItems()
    {
        foreach (GameObject item in allDots)
        {
            if (item != null) item.SetActive(false);
        }
        if (destructionCountPanel != null)
        {
            destructionCountPanel.SetActive(true);
        }
    }

    public IEnumerator HideAllItemsEnd()
    {
        foreach (GameObject item in allDots)
        {
            if (item != null)
            {
                yield return StartCoroutine(active.effect.FadeOut(item));
                item.SetActive(false);
            }
            yield return new WaitForSeconds(2f / allDots.Length);
        }
    }

    public void ShowItems()
    {
        if (destructionCountPanel != null)
        {
            destructionCountPanel.SetActive(false);
        }
        foreach (GameObject item in allDots)
        {
            if (item != null) item.SetActive(true);
        }
    }

    public void HandleTurnEnd()
    {
        Debug.Log($"[TURN END] Player {PhotonNetwork.LocalPlayer.ActorNumber} xử lý kết thúc lượt");

        if (PhotonNetwork.IsMasterClient)
        {
            EndTurn();
        }
    }

    public void ResetMoveCounters()
    {
        destroyedCountByTag.Clear();
        destroyedCount = 0;
        Debug.Log("Đặt lại bộ đếm di chuyển.");
        if (active != null)
        {
            active.resetOutput();
        }
    }

    /// <summary>
    /// ✅ RPC đồng bộ hoàn tác nước đi không hợp lệ
    /// Gửi đến TẤT CẢ client (bao gồm cả Master)
    /// </summary>
    [PunRPC]
    private void SyncUndoMove(int fromCol, int fromRow, int toCol, int toRow)
    {
        Debug.Log($"[UNDO] Player {PhotonNetwork.LocalPlayer.ActorNumber}: ({fromCol},{fromRow}) → ({toCol},{toRow})");

        // Validation
        if (fromCol < 0 || fromCol >= width || fromRow < 0 || fromRow >= height ||
            toCol < 0 || toCol >= width || toRow < 0 || toRow >= height)
        {
            Debug.LogError($"[UNDO] ❌ Tọa độ không hợp lệ!");
            return;
        }

        GameObject dot1 = allDots[fromCol, fromRow];
        GameObject dot2 = allDots[toCol, toRow];

        if (dot1 == null || dot2 == null)
        {
            Debug.LogError($"[UNDO] ❌ Dot null!");
            return;
        }

        DotPVP dot1Comp = dot1.GetComponent<DotPVP>();
        DotPVP dot2Comp = dot2.GetComponent<DotPVP>();

        if (dot1Comp == null || dot2Comp == null)
        {
            Debug.LogError($"[UNDO] ❌ Missing DotPVP component!");
            return;
        }

        // ✅ SWAP LẠI
        int tempCol = dot1Comp.column;
        int tempRow = dot1Comp.row;

        dot1Comp.column = toCol;
        dot1Comp.row = toRow;
        dot1Comp.previousColumn = toCol;
        dot1Comp.previousRow = toRow;

        dot2Comp.column = tempCol;
        dot2Comp.row = tempRow;
        dot2Comp.previousColumn = tempCol;
        dot2Comp.previousRow = tempRow;

        // Cập nhật array
        allDots[toCol, toRow] = dot1;
        allDots[tempCol, tempRow] = dot2;

        Debug.Log($"[UNDO] ✅ Hoàn tác thành công!");

        // Reset state
        currentState = GameState.move;
        hasDestroyedThisTurn = false;
    }
}
