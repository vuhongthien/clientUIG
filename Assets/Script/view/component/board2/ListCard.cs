using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class ListCard : MonoBehaviour
{
    public int slCard; // Số lượng Card cần hiển thị
    public Button cardPrefab; // Prefab của Card
    public float spacing = 800f;
    public GameObject onCard;
    

    // Danh sách các CardInfo làm dữ liệu nguồn
    public List<CardInfo> cardInfos = new List<CardInfo>();

    // Danh sách các Card hiện tại
    private List<Card> activeCards = new List<Card>();

    // Hàm để thiết lập cardInfos từ ngoài vào
    public void SetCardInfos(List<CardInfo> newCardInfos)
    {
        cardInfos = newCardInfos;
        InitializeCards();
    }

    void Start()
    {
        
        InitializeCards();
    }

    // Hàm khởi tạo Card
    void InitializeCards()
    {
        float startX = -(slCard - 1) * spacing / 2;

        for (int i = 0; i < slCard && i < cardInfos.Count; i++)
        {
            // Tạo một Button mới từ prefab 
            Button newCardButton = Instantiate(cardPrefab, transform);

            // Lấy component Card từ Button
            Card newCard = newCardButton.GetComponent<Card>();

            if (newCard != null)
            {
                // Gán giá trị từ CardInfo vào Card
                newCard.Initialize(cardInfos[i]);

                // Gọi hàm Setup để thiết lập sự kiện OnClick và lưu tham chiếu ListCard
                newCard.Setup(this);

                // Thêm vào danh sách activeCards
                activeCards.Add(newCard);
            }

            // Đặt vị trí cho Button mới
            RectTransform rectTransform = newCardButton.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(startX + i * spacing, 0);
            }

            newCardButton.gameObject.SetActive(true); // Kích hoạt Button
        }
    }


    // Hàm xóa Card
    public void RemoveCard(Card cardToRemove)
    {
 

        if (activeCards.Contains(cardToRemove))
        {
            activeCards.Remove(cardToRemove);
            RearrangeCards();
        }

    }


    // Hàm sắp xếp lại vị trí các Card
    void RearrangeCards()
    {
        float startX = -(activeCards.Count - 1) * spacing / 2;

        for (int i = 0; i < activeCards.Count; i++)
        {
            RectTransform rectTransform = activeCards[i].GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(startX + i * spacing, 0);
            }
        }
    }
}
