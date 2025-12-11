using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LoadDataCard : MonoBehaviour
{
    public GameObject boardCardPanel;
    public GameObject itemPrefab;
    public Texture2D fallbackTexture;
    public ApiLoadRoom apiLoadRoom;

    void Start(){

    }

    public void LoadCard(ChooseCard[] listChooseCard)
    {
        if (boardCardPanel == null)
        {
            Debug.LogError("BoardCardPanel is not assigned.");
            return;
        }

        Transform boardTransform = boardCardPanel.transform.Find("Board");
        if (boardTransform == null)
        {
            Debug.LogError("Board child not found under BoardCardPanel.");
            return;
        }

        // Destroy existing children safely
        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in boardTransform)
        {
            childrenToDestroy.Add(child.gameObject);
        }
        foreach (var child in childrenToDestroy)
        {
            Destroy(child);
        }

        // Load new cards
        foreach (var card in listChooseCard)
{
    // Instantiate item prefab
    GameObject itemCard = Instantiate(itemPrefab, boardTransform);

    // Tìm các thành phần UI
    Image iconImage = itemCard.transform.Find("Image").GetComponent<Image>();
    TextMeshProUGUI countText = itemCard.transform.Find("SL").GetComponent<TextMeshProUGUI>();
    Button btn = iconImage.GetComponent<Button>();

    // Cập nhật số lượng hiển thị
    countText.text = "x" + card.count.ToString();

    // Kiểm tra và tải ảnh nếu URL không trống
    if (!string.IsNullOrEmpty(card.thumbnail))
    {
        Debug.Log($"Loading image from URL: {card.thumbnail}");

        // Đặt tên cho Image
        string iconName = "card" + card.idCard;
        iconImage.name = iconName;

        // Sử dụng closure để tránh vấn đề phạm vi biến (closure issue)
        btn.onClick.AddListener(() => OnImageButtonClick(iconName));
    }
    else
    {
        Debug.LogWarning($"Card {card.idCard} does not have a valid thumbnail URL.");
    }
}

    }

    void OnImageButtonClick(string name){
        Debug.Log(name);
        apiLoadRoom.selectBtn.name = name;
        apiLoadRoom.btnDown.SetActive(false);
        apiLoadRoom.boardCard.SetActive(false);
    }

}
