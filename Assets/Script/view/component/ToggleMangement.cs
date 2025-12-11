using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleManager : MonoBehaviour
{
    [Header("Toggle Settings")]
    public GameObject listToggle;
    public GameObject togglePrefab;
    
    [Header("Display Settings")]
    public GameObject selectedImagePrefab;
    public Transform displayPanel;
    public int maxSelected = 3;
    
    [Header("Colors")]
    public Color selectedColor;
    public Color defaultColor;

    // ✅ THAY ĐỔI: Lưu danh sách các lần chọn (cho phép trùng cardId)
    private List<CardSelectionData> selectedCardsList = new List<CardSelectionData>();
    
    // ✅ Dictionary để track số lượng đã chọn của mỗi cardId
    private Dictionary<int, int> selectedCountByCardId = new Dictionary<int, int>();
    
    // ✅ Dictionary để lưu reference đến các Toggle theo cardId
    private Dictionary<int, Toggle> togglesByCardId = new Dictionary<int, Toggle>();
    
    // ✅ Dictionary để lưu các GameObject đã chọn theo selectionId
    private Dictionary<string, GameObject> selectedImagesBySelectionId = new Dictionary<string, GameObject>();

    // ✅ THÊM: HashSet để track các thẻ ATTACK đã được chọn
    private HashSet<int> selectedAttackCardIds = new HashSet<int>();

    // Flag để biết khi nào đang restore
    private bool isRestoring = false;

    private void Start()
    {
    }

    /// <summary>
    /// ✅ Gán sự kiện cho toggle mới tạo
    /// </summary>
    public void RegisterToggle(Toggle toggle)
    {
        if (toggle == null) return;

        CardToggleData cardData = toggle.GetComponent<CardToggleData>();
        if (cardData != null && cardData.cardData != null)
        {
            int cardId = (int)cardData.cardData.cardId;
            
            // Lưu reference
            if (!togglesByCardId.ContainsKey(cardId))
            {
                togglesByCardId[cardId] = toggle;
            }
            
            // Khởi tạo count
            if (!selectedCountByCardId.ContainsKey(cardId))
            {
                selectedCountByCardId[cardId] = 0;
            }
        }
        
        toggle.onValueChanged.AddListener(delegate { OnToggleChanged(toggle); });
        
        // ✅ NẾU TOGGLE ĐÃ ĐƯỢC SET isOn = true, TRIGGER NGAY
        if (toggle.isOn)
        {
            OnToggleChanged(toggle);
        }
        
        UpdateToggleColor(toggle);
        UpdateToggleInteractable(toggle);
        
        Debug.Log($"[ToggleManager] Registered new toggle: {toggle.name} (isOn: {toggle.isOn})");
    }

    /// <summary>
    /// ✅ Restore toggle mà KHÔNG trigger OnToggleChanged
    /// </summary>
    public void RestoreToggle(Toggle toggle, bool isOn)
    {
        if (toggle == null) return;
        
        isRestoring = true;
        
        toggle.isOn = isOn;
        
        if (isOn)
        {
            AddSelectedImage(toggle);
        }
        
        UpdateToggleColor(toggle);
        UpdateToggleInteractable(toggle);
        
        isRestoring = false;
        
        Debug.Log($"[ToggleManager] Restored toggle: {toggle.name} = {isOn}");
    }

    /// <summary>
    /// ✅ KIỂM TRA XEM THẺ CÓ PHẢI LÀ ATTACK KHÔNG
    /// </summary>
    private bool IsAttackCard(CardData card)
    {
        return card.elementTypeCard != null && card.elementTypeCard.ToUpper() == "ATTACK";
    }

    /// <summary>
    /// ✅ Xử lý khi toggle thay đổi - CHO PHÉP CHỌN NHIỀU LẦN
    /// </summary>
    private void OnToggleChanged(Toggle changedToggle)
    {
        if (isRestoring)
        {
            Debug.Log($"[ToggleManager] Skip OnToggleChanged (restoring): {changedToggle.name}");
            return;
        }

        CardToggleData cardToggleData = changedToggle.GetComponent<CardToggleData>();
        if (cardToggleData == null || cardToggleData.cardData == null)
        {
            Debug.LogWarning("[ToggleManager] CardToggleData not found!");
            return;
        }

        CardData card = cardToggleData.cardData;
        int cardId = (int)card.cardId;
        bool isAttack = IsAttackCard(card);

        if (changedToggle.isOn)
        {
            // ✅ KIỂM TRA ĐÃ ĐẠT MAX CHƯA
            if (selectedCardsList.Count >= maxSelected)
            {
                changedToggle.isOn = false;
                Debug.LogWarning($"[ToggleManager] Đã chọn đủ {maxSelected} thẻ!");
                return;
            }

            // ✅ KIỂM TRA THẺ ATTACK ĐÃ ĐƯỢC CHỌN CHƯA
            if (isAttack)
            {
                if (selectedAttackCardIds.Contains(cardId))
                {
                    changedToggle.isOn = false;
                    Debug.LogWarning($"[ToggleManager] Thẻ ATTACK {card.name} đã được chọn rồi!");
                    return;
                }
            }
            // ✅ KIỂM TRA COUNT - CHỈ ÁP DỤNG CHO THẺ KHÔNG PHẢI ATTACK
            else
            {
                int currentSelected = selectedCountByCardId.ContainsKey(cardId) ? selectedCountByCardId[cardId] : 0;
                
                if (currentSelected >= card.count)
                {
                    changedToggle.isOn = false;
                    Debug.LogWarning($"[ToggleManager] Không thể chọn thêm {card.name}. Đã chọn {currentSelected}/{card.count}");
                    return;
                }
            }

            // ✅ THÊM VÀO DANH SÁCH ĐÃ CHỌN (KHÔNG GỌI API)
            ProcessCardSelection(card, cardId, isAttack, changedToggle);
        }

        // ✅ Reset toggle về false sau khi xử lý
        changedToggle.isOn = false;
        UpdateToggleColor(changedToggle);
    }

    /// <summary>
    /// ✅ XỬ LÝ CHỌN CARD (KHÔNG GỌI API - CHỈ LƯU STATE)
    /// </summary>
    private void ProcessCardSelection(CardData card, int cardId, bool isAttack, Toggle toggle)
    {
        // ✅ THÊM VÀO DANH SÁCH ĐÃ CHỌN
        CardSelectionData selectionData = new CardSelectionData
        {
            cardData = card,
            selectionId = System.Guid.NewGuid().ToString(),
            toggle = toggle
        };
        
        selectedCardsList.Add(selectionData);
        
        // ✅ XỬ LÝ THEO LOẠI THẺ
        if (isAttack)
        {
            // ✅ THÊM VÀO DANH SÁCH ATTACK ĐÃ CHỌN
            selectedAttackCardIds.Add(cardId);
            Debug.Log($"[ToggleManager] ✓ Chọn thẻ ATTACK {card.name} - Tổng: {selectedCardsList.Count}/{maxSelected}");
        }
        else
        {
            // ✅ TĂNG COUNT CHO THẺ THƯỜNG/BUFF
            if (!selectedCountByCardId.ContainsKey(cardId))
            {
                selectedCountByCardId[cardId] = 0;
            }
            selectedCountByCardId[cardId]++;
            Debug.Log($"[ToggleManager] ✓ Chọn {card.name} ({selectedCountByCardId[cardId]}/{card.count}) - Tổng: {selectedCardsList.Count}/{maxSelected}");
        }

        // ✅ HIỂN THỊ THẺ ĐÃ CHỌN
        AddSelectedImage(selectionData);

        // ✅ CẬP NHẬT UI
        if (isAttack)
        {
            // ✅ VÔ HIỆU HÓA TOGGLE SAU KHI CHỌN
            UpdateToggleInteractable(toggle);
        }
        else
        {
            // ✅ CẬP NHẬT TEXT COUNT
            UpdateToggleCountText(cardId);
        }
    }

    /// <summary>
    /// ✅ THÊM ẢNH VÀO DISPLAY PANEL - HỖ TRỢ CẢ TOGGLE VÀ SELECTIONDATA
    /// </summary>
    private void AddSelectedImage(Toggle toggle)
    {
        CardToggleData cardToggleData = toggle.GetComponent<CardToggleData>();
        if (cardToggleData == null || cardToggleData.cardData == null) return;

        CardSelectionData selectionData = new CardSelectionData
        {
            cardData = cardToggleData.cardData,
            selectionId = System.Guid.NewGuid().ToString(),
            toggle = toggle
        };

        AddSelectedImage(selectionData);
    }

    private void AddSelectedImage(CardSelectionData selectionData)
    {
        if (selectedImagesBySelectionId.ContainsKey(selectionData.selectionId))
        {
            Debug.LogWarning($"[ToggleManager] Selection đã tồn tại: {selectionData.selectionId}");
            return;
        }

        Image[] images = selectionData.toggle.GetComponentsInChildren<Image>();  
        Image img = images.Length > 1 ? images[1] : null;
        
        if (img == null || img.sprite == null) return;

        GameObject newImageObj = Instantiate(selectedImagePrefab, displayPanel);
        Image newImage = newImageObj.GetComponent<Image>();

        if (newImage != null)
        {
            newImage.sprite = img.sprite;
        }

        // ✅ GẮN SCRIPT ĐỂ XỬ LÝ CLICK
        SelectedCardUI selectedCardUI = newImageObj.GetComponent<SelectedCardUI>();
        if (selectedCardUI == null)
        {
            selectedCardUI = newImageObj.AddComponent<SelectedCardUI>();
        }
        selectedCardUI.Setup(selectionData, this);

        // ✅ CHỈ ANIMATE KHI KHÔNG PHẢI RESTORE
        if (!isRestoring)
        {
            newImageObj.transform.localScale = Vector3.zero;
            LeanTween.scale(newImageObj, Vector3.one, 0.3f).setEaseOutBack();
        }
        else
        {
            newImageObj.transform.localScale = Vector3.one;
        }

        selectedImagesBySelectionId.Add(selectionData.selectionId, newImageObj);
        
        Debug.Log($"[ToggleManager] Thêm ảnh đã chọn: {selectionData.cardData.name} ({selectedCardsList.Count}/{maxSelected})");
    }

    /// <summary>
    /// ✅ XÓA THẺ ĐÃ CHỌN (khi user click vào ảnh trong displayPanel)
    /// </summary>
    public void RemoveSelectedCard(CardSelectionData selectionData)
    {
        if (selectionData == null) return;

        int cardId = (int)selectionData.cardData.cardId;
        bool isAttack = IsAttackCard(selectionData.cardData);

        // ✅ XÓA KHỎI DANH SÁCH
        selectedCardsList.Remove(selectionData);

        // ✅ XỬ LÝ THEO LOẠI THẺ
        if (isAttack)
        {
            // ✅ XÓA KHỎI DANH SÁCH ATTACK ĐÃ CHỌN
            selectedAttackCardIds.Remove(cardId);
            Debug.Log($"[ToggleManager] ✓ Xóa thẻ ATTACK {selectionData.cardData.name} - Tổng: {selectedCardsList.Count}/{maxSelected}");
            
            // ✅ BẬT LẠI TOGGLE
            UpdateToggleInteractable(selectionData.toggle);
        }
        else
        {
            // ✅ GIẢM COUNT CHO THẺ THƯỜNG
            if (selectedCountByCardId.ContainsKey(cardId))
            {
                selectedCountByCardId[cardId]--;
                
                if (selectedCountByCardId[cardId] < 0)
                {
                    selectedCountByCardId[cardId] = 0;
                }
            }
            Debug.Log($"[ToggleManager] ✓ Xóa {selectionData.cardData.name} ({selectedCountByCardId[cardId]}/{selectionData.cardData.count}) - Tổng: {selectedCardsList.Count}/{maxSelected}");
            
            // ✅ CẬP NHẬT TEXT COUNT
            UpdateToggleCountText(cardId);
        }

        // ✅ XÓA KHỎI DICTIONARY
        if (selectedImagesBySelectionId.ContainsKey(selectionData.selectionId))
        {
            selectedImagesBySelectionId.Remove(selectionData.selectionId);
        }
    }

    /// <summary>
    /// ✅ CẬP NHẬT TRẠNG THÁI INTERACTABLE CỦA TOGGLE
    /// </summary>
    private void UpdateToggleInteractable(Toggle toggle)
    {
        CardToggleData cardData = toggle.GetComponent<CardToggleData>();
        if (cardData == null || cardData.cardData == null) return;

        bool isAttack = IsAttackCard(cardData.cardData);
        
        if (isAttack)
        {
            int cardId = (int)cardData.cardData.cardId;
            bool isSelected = selectedAttackCardIds.Contains(cardId);
            
            // ✅ VÔ HIỆU HÓA NẾU ĐÃ CHỌN
            toggle.interactable = !isSelected;
            
            // ✅ THAY ĐỔI MÀU ĐỂ CHỈ RA TRẠNG THÁI
            if (!isSelected)
            {
                // Reset màu bình thường
                ColorBlock colors = toggle.colors;
                colors.disabledColor = Color.gray;
                toggle.colors = colors;
            }
        }
        else
        {
            // Thẻ thường luôn interactable
            toggle.interactable = true;
        }
    }

    /// <summary>
    /// ✅ CẬP NHẬT TEXT COUNT TRÊN TOGGLE
    /// </summary>
    private void UpdateToggleCountText(int cardId)
    {
        if (!togglesByCardId.ContainsKey(cardId)) return;

        Toggle toggle = togglesByCardId[cardId];
        CardToggleData cardData = toggle.GetComponent<CardToggleData>();
        
        if (cardData == null || cardData.cardData == null) return;

        // ✅ KIỂM TRA NẾU LÀ THẺ ATTACK THÌ KHÔNG CẬP NHẬT
        if (IsAttackCard(cardData.cardData))
        {
            // ✅ ẨN TEXT COUNT CHO THẺ ATTACK
            Text[] texts = toggle.GetComponentsInChildren<Text>();
            foreach (Text txt in texts)
            {
                if (txt.name.Contains("Count"))
                {
                    txt.text = "";
                    break;
                }
            }
            return;
        }

        int selectedCount = selectedCountByCardId.ContainsKey(cardId) ? selectedCountByCardId[cardId] : 0;
        int remainingCount = cardData.cardData.count - selectedCount;

        // ✅ TÌM TEXT COUNT VÀ CẬP NHẬT
        Text[] allTexts = toggle.GetComponentsInChildren<Text>();
        foreach (Text txt in allTexts)
        {
            if (txt.name.Contains("Count"))
            {
                if (remainingCount > 0)
                {
                    txt.text = $"x{remainingCount}";
                    txt.color = Color.white;
                }
                else
                {
                    txt.text = "x0";
                    txt.color = Color.red;
                }
                break;
            }
        }
    }

    /// <summary>
    /// ✅ Cập nhật màu toggle
    /// </summary>
    private void UpdateToggleColor(Toggle toggle)
    {
        ColorBlock colors = toggle.colors;
        colors.normalColor = toggle.isOn ? selectedColor : defaultColor;
        colors.selectedColor = toggle.isOn ? selectedColor : defaultColor;
        toggle.colors = colors;
    }

    /// <summary>
    /// ✅ Lấy danh sách CardData đã chọn
    /// </summary>
    public List<CardData> GetSelectedCards()
    {
        List<CardData> cards = new List<CardData>();
        
        foreach (var selection in selectedCardsList)
        {
            cards.Add(selection.cardData);
        }

        Debug.Log($"[ToggleManager] GetSelectedCards: {cards.Count} thẻ");
        
        return cards;
    }

    /// <summary>
    /// ✅ Lấy số lượng đã chọn
    /// </summary>
    public int GetSelectedCount()
    {
        return selectedCardsList.Count;
    }

    /// <summary>
    /// ✅ Reset tất cả toggle
    /// </summary>
    public void ResetAllToggles()
    {
        if (listToggle == null) return;
        
        Toggle[] toggles = listToggle.GetComponentsInChildren<Toggle>();
        
        foreach (var toggle in toggles)
        {
            toggle.isOn = false;
            toggle.interactable = true; // ✅ Bật lại tất cả
        }
        
        foreach (var imageObj in selectedImagesBySelectionId.Values)
        {
            if (imageObj != null)
            {
                Destroy(imageObj);
            }
        }
        
        selectedCardsList.Clear();
        selectedCountByCardId.Clear();
        selectedImagesBySelectionId.Clear();
        selectedAttackCardIds.Clear(); // ✅ Xóa danh sách attack đã chọn
        
        Debug.Log("[ToggleManager] Reset tất cả toggle");
    }

    /// <summary>
    /// ✅ Xóa tất cả toggle (dùng trước khi load cards mới)
    /// </summary>
    public void ClearAllToggles()
    {
        if (listToggle == null) return;
        
        foreach (Transform child in listToggle.transform)
        {
            Destroy(child.gameObject);
        }
        
        foreach (var imageObj in selectedImagesBySelectionId.Values)
        {
            if (imageObj != null)
            {
                Destroy(imageObj);
            }
        }
        
        selectedCardsList.Clear();
        selectedCountByCardId.Clear();
        togglesByCardId.Clear();
        selectedImagesBySelectionId.Clear();
        selectedAttackCardIds.Clear(); // ✅ Xóa danh sách attack đã chọn
        
        Debug.Log("[ToggleManager] Đã xóa tất cả toggle và selectedImages");
    }
}

/// <summary>
/// ✅ CLASS ĐỂ LƯU THÔNG TIN MỖI LẦN CHỌN THẺ
/// </summary>
[System.Serializable]
public class CardSelectionData
{
    public CardData cardData;
    public string selectionId; // ID duy nhất cho mỗi lần chọn
    public Toggle toggle; // Reference đến toggle
}

/// <summary>
/// Component lưu CardData vào Toggle
/// </summary>
public class CardToggleData : MonoBehaviour
{
    public CardData cardData;
}