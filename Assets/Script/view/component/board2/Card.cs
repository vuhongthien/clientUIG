using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    public int idCard;
    public string idCardUser;
    public string cardDetail;
    public int value;
    public int lever;
    public int conditionUse;
    private Active active;
    private ListCard listCard;
    private CardFight cardFight;
    private Button btn;

    Board board;

    // UI Elements
    private string url;
    public Image cardImage;  // Use RawImage instead of Image

    void Start()
    {
         board = FindFirstObjectByType<Board>();
        active = FindFirstObjectByType<Active>();
        cardFight = FindFirstObjectByType<CardFight>();

        btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(OnClickCard);
        }
        else
        {
            Debug.LogWarning("Card " + gameObject.name + " không có Button component!");
        }
    }

    public void Initialize(int id, int level = 1)
    {
        this.lever = Mathf.Clamp(level, 1, 10);
    }

    // Thiết lập giá trị card từ CardInfo
    public void Initialize(CardInfo cardInfo)
    {
        idCard = cardInfo.IdCard;
        idCardUser = cardInfo.IdCardUser;
        cardDetail = cardInfo.CardDetail;
        value = cardInfo.Value;
        lever = cardInfo.Lever;
        conditionUse = cardInfo.ConditionUse;

        // Cập nhật giao diện hiển thị
        Text cardText = GetComponentInChildren<Text>();
        if (cardText != null)
        {
            cardText.text = $"{idCard}\n{cardDetail}\nBuff: {value}";
        }

        cardImage = GetComponentInChildren<Image>();
        if (cardImage != null)
        {
            cardImage.name = "card" + idCard;
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

        }
    }

    public void OnClickCard()
    {
        
        if (board == null ||
            board.currentState != GameState.move ||
            !board.IsPlayerAllowedToMove() ||
            active == null ||
            board.hasDestroyedThisTurn)
        {
            Debug.Log("Không thể ấn card: " + gameObject.name);
            return;
        }

        Debug.Log($"Player ấn card ID={idCard} ({GetCardTypeName()}) Level={lever}");

        // ✅ LẤY onAnimationCard từ CardFight và kích hoạt
        if (idCard == 4 && cardFight != null)
        {
            // Gọi method từ CardFight để kích hoạt onAnimationCard
            cardFight.ActivateOnAnimationCard(lever);
            cardFight.playAnimationCard();
        }

        cardFight?.HandleCardEffect(idCard, lever);
        Destroy(gameObject);
    }

    private string GetCardTypeName()
    {
        switch (idCard)
        {
            case 1: return "HP";
            case 2: return "Mana";
            case 3: return "No";
            case 4: return $"dameCard (Level {lever})";
            default: return "Unknown";
        }
    }


    // Hàm thiết lập để nhận tham chiếu đến ListCard
    public void Setup(ListCard listCardReference)
    {
        listCard = listCardReference;

        // Gán sự kiện OnClick để gọi hàm OnClickCard khi nhấn vào
        GetComponent<Button>().onClick.AddListener(OnClickCard);
    }

    // Coroutine để hiển thị items sau khi trì hoãn
    private IEnumerator ShowItemsAfterDelay(float delay)
    {
        Debug.Log("Đang hiển thị onCard...");

        board.HideAllItems();
        listCard.onCard.SetActive(true);

        // Chờ đợi trong khoảng thời gian được chỉ định
        yield return new WaitForSeconds(delay);

        // Sau khi trì hoãn, ẩn onCard và hiển thị items
        Debug.Log("Ẩn onCard và hiển thị items.");
        listCard.onCard.SetActive(false);
        board.ShowItems();
    }

    // Xử lý sự kiện khi nhấn vào Card
    // void OnClickCard()
    // {
    //     if (active != null)
    //     {
    //         active.StopCountdown();
    //         Debug.Log("Đã dừng đếm ngược.");
    //         active.onCard.GetComponent<Image>().sprite = gameObject.GetComponent<Image>().sprite;
    //         CardInfo cardInfo = new CardInfo(idCard, idCardUser, cardDetail, value, null, lever, conditionUse);

    //         if (active.ManaPlayer < conditionUse)
    //         {
    //             Debug.LogWarning("Không đủ Mana để sử dụng thẻ này!");
    //             return; // Thoát ra nếu không thỏa điều kiện
    //         }

    //         StartCoroutine(HandleCardActionsWithDelay(2f, cardInfo));
    //     }
    // }

    // Coroutine để xử lý toàn bộ hành động liên quan đến thẻ
    // Coroutine để xử lý toàn bộ hành động liên quan đến thẻ
    private IEnumerator HandleCardActionsWithDelay(float delay, CardInfo cardInfo)
    {
        // Xóa button nếu là card buff (idCard < 4)
        if (idCard < 4)
        {
            var button = gameObject.GetComponent<Button>();
            if (button != null)
            {
                Destroy(button);
            }
        }

        // Hiển thị animation
        yield return StartCoroutine(ShowItemsAfterDelay(delay));

        // Áp dụng hiệu ứng card
        if (active != null)
        {
            active.IncreaseNoPlayer(cardInfo);
        }

        if (idCard < 4)
        {
            // ✅ CARD BUFF: Ẩn card và tiếp tục turn hiện tại
            gameObject.SetActive(false);

            if (active != null)
            {
                active.ResumeTurn(); // Tiếp tục đếm giờ turn
                Debug.Log($"[CARD] Buff card {idCard} used. Turn continues.");
            }

            // Đặt lại trạng thái board
            if (board != null)
            {
                board.currentState = GameState.move;
            }
        }
        else
        {
            // ✅ CARD KỸ NĂNG: Kết thúc turn ngay lập tức
            if (active != null)
            {
                Debug.Log($"[CARD] Skill card {idCard} used. Ending turn {active.TurnNumber}.");
                active.EndCurrentTurn(); // Kết thúc turn → chuyển sang NPC
            }

            // Board state sẽ được reset tự động bởi Active's OnTurnStart event
        }

        Debug.Log($"[CARD] Card {idCard} processing completed.");
    }


}