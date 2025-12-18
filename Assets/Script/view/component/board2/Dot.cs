using Photon.Pun;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dot script hợp nhất hỗ trợ cả PVP (Photon) và Single Player
/// </summary>
public class Dot : MonoBehaviourPun
{
    [Header("Game Mode")]
    [Tooltip("Bật để sử dụng chế độ PVP với Photon. Tắt để chơi đơn.")]
    public bool usePVPMode = false;

    [Header("Dot Properties")]
    public int column;
    public int row;
    public int previousColumn;
    public int previousRow;
    public int targetX;
    public int targetY;
    public bool isMathched = false;

    [Header("Swipe Settings")]
    public float swipeResit = 1f;
    public float swipeAngle = 0;

    // Private variables
    private Board board;
    private BoardPVP boardPVP;
    private FindMatches findMatches;
    private FindMatchesPVP findMatchesPVP;
    private Active active;

    public GameObject otherDot;
    public Vector2 firstTouchPosition;
    public Vector2 finalTouchPosition;
    private Vector2 tempPosition;

    // Click system (Single Player only)
    private static Dot firstSelectedDot = null;
    private Color originalColor;
    private bool isSelected = false;
    public int multiplier = 1; // Số lượng viên (1, 2, hoặc 3)
    public Text multiplierText;

    void Start()
    {
        originalColor = GetComponent<SpriteRenderer>().color;

        if (usePVPMode)
        {
            InitializePVP();
        }
        else
        {
            InitializeSinglePlayer();
        }
    }

    void InitializePVP()
    {
        boardPVP = FindFirstObjectByType<BoardPVP>();
        findMatchesPVP = FindFirstObjectByType<FindMatchesPVP>();

        if (boardPVP == null)
        {
        }
    }

    void InitializeSinglePlayer()
    {
        board = FindFirstObjectByType<Board>();
        findMatches = FindFirstObjectByType<FindMatches>();
        active = FindFirstObjectByType<Active>();

        if (board == null)
        {
        }
    }

    void Update()
    {
        UpdatePositionAndMatches();

        if (!usePVPMode)
        {
            HandleContinuousHighlight();
        }
    }

    void UpdatePositionAndMatches()
    {
        targetX = column;
        targetY = row;

        // Di chuyển ngang
        if (Mathf.Abs(targetX - transform.position.x) > .1f)
        {
            tempPosition = new Vector2(targetX, transform.position.y);
            transform.position = Vector2.Lerp(transform.position, tempPosition, 0.2f);
            UpdateBoardReference();
            FindAllMatches();
        }
        else
        {
            tempPosition = new Vector2(targetX, transform.position.y);
            transform.position = tempPosition;
        }

        // Di chuyển dọc
        if (Mathf.Abs(targetY - transform.position.y) > .1f)
        {
            tempPosition = new Vector2(transform.position.x, targetY);
            transform.position = Vector2.Lerp(transform.position, tempPosition, 0.2f);
            UpdateBoardReference();
            FindAllMatches();
        }
        else
        {
            tempPosition = new Vector2(transform.position.x, targetY);
            transform.position = tempPosition;
        }
    }

    void UpdateBoardReference()
    {
        if (usePVPMode)
        {
            if (boardPVP != null && boardPVP.allDots[column, row] != this.gameObject)
            {
                boardPVP.allDots[column, row] = this.gameObject;
            }
        }
        else
        {
            if (board != null && board.allDots[column, row] != this.gameObject)
            {
                board.allDots[column, row] = this.gameObject;
            }
        }
    }

    void FindAllMatches()
    {
        if (usePVPMode)
        {
            if (findMatchesPVP != null)
                findMatchesPVP.FindAllMatches();
        }
        else
        {
            if (findMatches != null)
                findMatches.FindAllMatches();
        }
    }

    private void OnMouseDown()
    {
        if (CanInteract())
        {
            firstTouchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            AudioManager.Instance.PlaySwordClickSound();
        }
    }

    private void OnMouseUp()
    {
        if (CanInteract())
        {
            finalTouchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            CalculateAngle();
        }
    }

    bool CanInteract()
    {
        if (usePVPMode)
        {
            return boardPVP != null &&
                   boardPVP.currentState == GameState.move &&
                   boardPVP.IsPlayerAllowedToMove() &&
                   !boardPVP.hasDestroyedThisTurn;
        }
        else
        {
            // ✅ Sử dụng Active.Instance thay vì Active.isTimeOver
            return board != null &&
                   active != null &&
                   Active.Instance != null &&
                   Active.Instance.IsTurnInProgress && // Kiểm tra turn đang chạy
                   Active.Instance.CurrentTurnTime > 0 && // Kiểm tra còn thời gian
                   board.currentState == GameState.move &&
                   board.IsPlayerAllowedToMove() &&
                   !board.hasDestroyedThisTurn;
        }
    }

    public void CalculateAngle()
    {
        if (IsValidSwipe())
        {
            swipeAngle = Mathf.Atan2(finalTouchPosition.y - firstTouchPosition.y,
                                   finalTouchPosition.x - firstTouchPosition.x) * 180 / Mathf.PI;
            MovePieces();

            if (!usePVPMode && board != null)
            {
                board.currentState = GameState.wait;
            }
        }
        else
        {
            if (usePVPMode)
            {
                if (boardPVP != null)
                    boardPVP.currentState = GameState.move;
            }
            else
            {
                if (board != null)
                    board.currentState = GameState.move;
                HandleClick(); // Chỉ có click system trong Single Player
            }
        }
    }

    bool IsValidSwipe()
    {
        return Mathf.Abs(finalTouchPosition.y - firstTouchPosition.y) > swipeResit ||
               Mathf.Abs(finalTouchPosition.x - firstTouchPosition.x) > swipeResit;
    }

    // === SINGLE PLAYER CLICK SYSTEM ===
    void HandleClick()
    {
        if (firstSelectedDot == null)
        {
            SelectFirstDot();
        }
        else
        {
            HandleSecondClick();
        }
    }

    void SelectFirstDot()
    {
        firstSelectedDot = this;
        Highlight(true);
    }

    void HandleSecondClick()
    {
        if (firstSelectedDot == this)
        {
            DeselectDot();
        }
        else if (IsAdjacent(firstSelectedDot))
        {
            SwapAndCheckMatches();
        }
        else
        {
            ResetSelection();
        }
    }

    void DeselectDot()
    {
        firstSelectedDot.Highlight(false);
        firstSelectedDot = null;
    }

    void SwapAndCheckMatches()
    {
        StartCoroutine(SwapAndCheck(firstSelectedDot));
        firstSelectedDot.Highlight(false);
        firstSelectedDot = null;
    }

    void ResetSelection()
    {
        firstSelectedDot.Highlight(false);
        firstSelectedDot = null;
    }

    bool IsAdjacent(Dot other)
    {
        return (Mathf.Abs(column - other.column) == 1 && row == other.row) ||
               (Mathf.Abs(row - other.row) == 1 && column == other.column);
    }

    IEnumerator SwapAndCheck(Dot otherDot)
    {
        SwapPieces(otherDot);
        board.currentState = GameState.wait;

        yield return new WaitForSeconds(0.5f);

        findMatches.FindAllMatches();
        if (!isMathched && !otherDot.isMathched)
        {
            SwapPieces(otherDot);
            yield return new WaitForSeconds(0.5f);
            board.currentState = GameState.move;
        }
        else
        {
            board.DestroyMatches();
        }
    }

    void SwapPieces(Dot otherDot)
    {
        board.allDots[column, row] = otherDot.gameObject;
        board.allDots[otherDot.column, otherDot.row] = this.gameObject;

        int tempCol = column;
        int tempRow = row;
        column = otherDot.column;
        row = otherDot.row;
        otherDot.column = tempCol;
        otherDot.row = tempRow;
    }

    void Highlight(bool highlight)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        sr.color = highlight ? new Color(1f, 1f, 0.5f, 1f) : originalColor;
        isSelected = highlight;
    }

    void HandleContinuousHighlight()
    {
        if (isSelected && firstSelectedDot != this)
        {
            Highlight(false);
        }
    }

    // === MOVE PIECES ===
    public void MovePieces()
    {
        if (usePVPMode)
        {
            MovePiecesPVP();
        }
        else
        {
            MovePiecesSinglePlayer();
        }
    }

    void MovePiecesPVP()
    {
        if (boardPVP == null || boardPVP.hasDestroyedThisTurn) return;

        int toCol = column;
        int toRow = row;

        if (swipeAngle > -45 && swipeAngle <= 45 && column < boardPVP.width - 1)
        {
            toCol = column + 1;
            toRow = row;
        }
        else if (swipeAngle > 45 && swipeAngle <= 135 && row < boardPVP.height - 1)
        {
            toCol = column;
            toRow = row + 1;
        }
        else if ((swipeAngle > 135 || swipeAngle <= -135) && column > 0)
        {
            toCol = column - 1;
            toRow = row;
        }
        else if (swipeAngle < -45 && swipeAngle >= -135 && row > 0)
        {
            toCol = column;
            toRow = row - 1;
        }
        else
        {
            boardPVP.currentState = GameState.move;
            return;
        }

        // Đồng bộ di chuyển qua Photon
        boardPVP.photonView.RPC("SyncMove", RpcTarget.All, column, row, toCol, toRow);
    }

    void MovePiecesSinglePlayer()
    {
        if (board == null || active == null) return;

        if (board.hasDestroyedThisTurn || active.CurrentTurnTime <= 1)
        {
            board.currentState = GameState.move;
            return;
        }

        if (swipeAngle > -45 && swipeAngle <= 45 && column < board.width - 1)
        {
            otherDot = board.allDots[column + 1, row];
            previousColumn = column;
            previousRow = row;
            otherDot.GetComponent<Dot>().column -= 1;
            column += 1;
        }
        else if (swipeAngle > 45 && swipeAngle <= 135 && row < board.height - 1)
        {
            otherDot = board.allDots[column, row + 1];
            previousColumn = column;
            previousRow = row;
            otherDot.GetComponent<Dot>().row -= 1;
            row += 1;
        }
        else if ((swipeAngle > 135 || swipeAngle <= -135) && column > 0)
        {
            otherDot = board.allDots[column - 1, row];
            previousColumn = column;
            previousRow = row;
            otherDot.GetComponent<Dot>().column += 1;
            column -= 1;
        }
        else if (swipeAngle < -45 && swipeAngle >= -135 && row > 0)
        {
            otherDot = board.allDots[column, row - 1];
            previousColumn = column;
            previousRow = row;
            otherDot.GetComponent<Dot>().row += 1;
            row -= 1;
        }
        else
        {
            board.currentState = GameState.move;
            return;
        }

        StartCoroutine(CheckMoveCo());
    }

    // === CHECK MOVE COROUTINE ===
    public IEnumerator CheckMoveCo()
    {
        yield return new WaitForSeconds(usePVPMode ? 0.5f : 0.2f);

        if (otherDot != null)
        {
            Dot otherDotComponent = otherDot.GetComponent<Dot>();

            if (!isMathched && !otherDotComponent.isMathched)
            {
                // Hoàn tác di chuyển
                otherDotComponent.row = row;
                otherDotComponent.column = column;
                row = previousRow;
                column = previousColumn;

                if (usePVPMode)
                {
                    if (boardPVP != null)
                    {
                        boardPVP.allDots[column, row] = this.gameObject;
                        boardPVP.allDots[otherDotComponent.column, otherDotComponent.row] = otherDot;
                        boardPVP.currentState = GameState.move;
                    }
                }
                else
                {
                    if (board != null)
                    {
                        board.currentState = GameState.move;
                    }
                }
            }
            else
            {
                // Phá hủy matches
                if (usePVPMode)
                {
                    if (boardPVP != null)
                        boardPVP.DestroyMatches();
                }
                else
                {
                    if (board != null)
                        board.DestroyMatches();
                }
            }

            otherDot = null;
        }
        else
        {
            if (usePVPMode)
            {
                if (boardPVP != null)
                    boardPVP.currentState = GameState.move;
            }
            else
            {
                if (board != null)
                    board.currentState = GameState.move;
            }
        }
    }
}