using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ✅ SCRIPT CHO CARDS TRONG MEMBER PREFAB (CÓ THỂ UNSELECT)
/// </summary>
public class MemberCardUI : MonoBehaviour
{
    private CardData cardData;
    private ToggleManager toggleManager;
    private long memberUserId;

    public void Setup(CardData data, ToggleManager manager, long userId)
    {
        cardData = data;
        toggleManager = manager;
        memberUserId = userId;

        // ✅ CHỈ CHO PHÉP CLICK NẾU LÀ CHÍNH USER NÀY
        int currentUserId = PlayerPrefs.GetInt("userId", 0);
        
        if (memberUserId == currentUserId)
        {
            // ✅ THÊM BUTTON ĐỂ XÓA
            Button button = GetComponent<Button>();
            if (button == null)
            {
                button = gameObject.AddComponent<Button>();
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
            
            Debug.Log($"[MemberCardUI] ✓ Setup clickable card: {cardData.name}");
        }
    }

    private void OnClick()
    {
        if (toggleManager == null || cardData == null) return;

        Debug.Log($"[MemberCardUI] Removing card from member: {cardData.name}");

        // ✅ TÌM VÀ XÓA CARD TRONG SELECTED LIST
        var selectedCards = toggleManager.GetSelectedCards();
        
        bool found = false;
        for (int i = 0; i < selectedCards.Count; i++)
        {
            if (selectedCards[i].cardId == cardData.cardId)
            {
                // ✅ GỌI REMOVE TRONG TOGGLE MANAGER
                // Cần thêm method mới trong ToggleManager
                toggleManager.RemoveSelectedCardByIndex(i);
                found = true;
                break;
            }
        }

        if (!found)
        {
            Debug.LogWarning($"[MemberCardUI] Card not found in selected list: {cardData.name}");
        }
    }
}