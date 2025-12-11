using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ✅ SCRIPT GẮN VÀO MỖI ẢNH ĐÃ CHỌN TRONG DISPLAY PANEL
/// </summary>
public class SelectedCardUI : MonoBehaviour
{
    private CardSelectionData selectionData;
    private ToggleManager toggleManager;

    public void Setup(CardSelectionData data, ToggleManager manager)
    {
        selectionData = data;
        toggleManager = manager;

        // ✅ THÊM BUTTON ĐỂ XÓA
        Button button = GetComponent<Button>();
        if (button == null)
        {
            button = gameObject.AddComponent<Button>();
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (toggleManager == null || selectionData == null) return;

        Debug.Log($"[SelectedCardUI] Xóa thẻ {selectionData.cardData.name}");

        // ✅ XÓA KHỎI TOGGLE MANAGER
        toggleManager.RemoveSelectedCard(selectionData);

        // ✅ XÓA UI VỚI ANIMATION
        LeanTween.scale(gameObject, Vector3.zero, 0.2f)
            .setEaseInBack()
            .setOnComplete(() => 
            {
                if (gameObject != null)
                {
                    Destroy(gameObject);
                }
            });
    }
}