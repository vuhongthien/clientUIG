using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CardFight : MonoBehaviour
{
    [Header("Cài đặt sinh card")]
    public GameObject cardPrefab;       // Prefab của card (có Card.cs và Image)
    public Transform cardParent;        // Panel chứa các card
    public int cardCount = 3;           // Số lượng card cần tạo

    [Header("ID hình cho từng card (1 = HP, 2 = Mana, 3 = No, 4 = dameCard)")]
    private List<int> cardIDs = new List<int>(); // Danh sách ID hình

    [Header("Animation References")]
    public GameObject onAnimationCardObject; // ✅ Kéo thả ở đây!

    [Header("Cấp độ cho card ID 4 (DameCard)")]
    [Range(1, 10)]
    public int dameCardLevel =3;       // Một cấp độ chung cho tất cả DameCard

    [Header("UI References")]
    public Active active;           // Reference Active để dùng healdHP, healdMana,...
    public Board board;             // Reference Board để tắt/mở board

    private List<GameObject> spawnedCards = new List<GameObject>();

    public Animator cardAnimator;

    void Start()
    {
        active = FindFirstObjectByType<Active>();
        board = FindFirstObjectByType<Board>();
        GenerateCards();
    }

    // ✅ Sinh card và gán đúng ID/level
    public void GenerateCards()
    {
        // Xóa card cũ
        foreach (var c in spawnedCards)
            if (c != null) DestroyImmediate(c);
        spawnedCards.Clear();

        // Đảm bảo đủ ID
        while (cardIDs.Count < cardCount)
            cardIDs.Add(1);
        while (cardIDs.Count > cardCount)
            cardIDs.RemoveAt(cardIDs.Count - 1);

        // Tạo card mới
        for (int i = 0; i < cardCount; i++)
        {
            GameObject newCard = Instantiate(cardPrefab, cardParent);
            newCard.name = "Card_" + (i + 1);

            // Gán sprite theo ID
            Image img = newCard.GetComponent<Image>();
            if (img != null)
            {
                string spriteName = GetSpriteNameById(cardIDs[i]);
                Sprite sprite = Resources.Load<Sprite>("card/" + spriteName);
                if (sprite != null)
                    img.sprite = sprite;
                else
                    Debug.LogWarning("Không tìm thấy sprite: " + spriteName);
            }

            // Gán ID và level vào Card component
            Card cardComponent = newCard.GetComponent<Card>();
            if (cardComponent != null)
            {
                int level = (cardIDs[i] == 4) ? Mathf.Clamp(dameCardLevel, 1, 10) : 1;
                cardComponent.Initialize(cardIDs[i], level);
            }
            else
            {
                Debug.LogWarning("Card prefab thiếu component Card: " + newCard.name);
            }

            spawnedCards.Add(newCard);
        }
    }

    // ✅ Xử lý hiệu ứng khi ấn card
    public void HandleCardEffect(int cardID, int level)
    {
        if (active == null || board == null)
        {
            Debug.LogError("Active hoặc Board không tìm thấy!");
            return;
        }

        StartCoroutine(ProcessCardEffect(cardID, level));
    }

    // ✅ Process hiệu ứng card với level
    private IEnumerator ProcessCardEffect(int cardID, int level)
    {
        int value = 100;  // Giá trị mặc định cho các card khác

        if (cardID == 4)  // Nếu là card ID 4, tính value dựa trên level
        {
            value = level * 100;  // Level 1: 100, Level 2: 200, ...
            Debug.Log($"Card Dame Level {level}: Gây {value} sát thương");
        }

        // 1. TẮT BOARD
        board.HideAllItems();
        yield return new WaitForSeconds(0.3f);

        // 2. ÁP DỤNG HIỆU ỨNG THEO CARD
        switch (cardID)
        {
            case 1: // HP
                active.MauPlayer = Mathf.Min(active.MauPlayer + value, active.maxMau);
                active.valueCurrent = value;
                Debug.Log($"Card HP: +{value} → {active.MauPlayer}/{active.maxMau}");
                break;

            case 2: // Mana
                active.ManaPlayer = Mathf.Min(active.ManaPlayer + value, active.maxMana);
                active.valueCurrent = value;
                Debug.Log($"Card Mana: +{value} → {active.ManaPlayer}/{active.maxMana}");
                break;

            case 3: // No (Power)
                active.NoPlayer = Mathf.Min(active.NoPlayer + value, active.maxNo);
                active.valueCurrent = value;
                Debug.Log($"Card No: +{value} → {active.NoPlayer}/{active.maxNo}");
                break;

            case 4: // DameCard
                active.MauNPC = Mathf.Max(active.MauNPC - value, 0);
                active.valueCurrent = value;
                Debug.Log($"Card Dame: -{value} HP NPC → {active.MauNPC}/{active.maxMauNPC}");
                break;

            default:
                Debug.LogWarning($"Unknown cardID: {cardID}");
                yield break;
        }

        // 3. CẬP NHẬT SLIDER
        active.UpdateSlider();

        // 4. HIỆN HIỆU ỨNG - Truyền level vào
        yield return StartCoroutine(ShowCardEffect(cardID, level));

        // 5. MỞ LẠI BOARD
        board.ShowItems();
        yield return new WaitForSeconds(0.5f);

        // 6. Tạo card mới sau khi dùng xong
        GenerateCards();
    }

    public void ActivateOnAnimationCard(int level)
    {
        if (onAnimationCardObject != null)
        {
            onAnimationCardObject.SetActive(true);
            Debug.Log($"✅ CardFight: Đã kích hoạt onAnimationCard Level {level}");
        }
        else
        {
            Debug.LogWarning("onAnimationCardObject chưa được gán trong CardFight!");
        }
    }

    // ✅ Hiển thị hiệu ứng theo cardID và level
    private IEnumerator ShowCardEffect(int cardID, int level = 1)
    {
        switch (cardID)
        {
            case 1: // HP
                if (active.healdHP != null)
                {
                    active.healdHP.SetActive(true);
                    active.healdHP.GetComponent<Text>().text = "+ " + active.valueCurrent;
                    yield return active.effect.FadeAndMoveUp(active.healdHP);
                    active.healdHP.SetActive(false);
                }
                break;

            case 2: // Mana
                if (active.healdMana != null)
                {
                    active.healdMana.SetActive(true);
                    active.healdMana.GetComponent<Text>().text = "+ " + active.valueCurrent;
                    yield return active.effect.FadeAndMoveUp(active.healdMana);
                    active.healdMana.SetActive(false);
                }
                break;

            case 3: // No (Power)
                if (active.healdPower != null)
                {
                    active.healdPower.SetActive(true);
                    active.healdPower.GetComponent<Text>().text = "+ " + active.valueCurrent;
                    yield return active.effect.FadeAndMoveUp(active.healdPower);
                    active.healdPower.SetActive(false);
                }
                break;

            case 4: // DameCard - ✅ XỬ LÝ THEO LEVEL với Animator
                if (active.dameATKPrefad != null)
                {
                    // Hiển thị panel damage
                    active.dameATKPrefad.SetActive(true);

                    // Cập nhật text damage
                    Text dame = active.dameATKPrefad.transform.Find("txtdame")?.GetComponent<Text>();
                    if (dame != null)
                        dame.text = "- " + active.valueCurrent.ToString();

                    // ✅ Tìm Animator và xử lý animation theo level
                    Animator animator = active.dameATKPrefad.GetComponent<Animator>();
                    if (animator != null)
                    {
                        // Tắt tất cả gameobject lvX trước
                        for (int i = 1; i <= 10; i++)
                        {
                            Transform lvObj = active.dameATKPrefad.transform.Find("onCardDame_lv" + i);
                            if (lvObj != null)
                                lvObj.gameObject.SetActive(false);
                        }

                        // Bật gameobject tương ứng với level
                        Transform levelObj = active.dameATKPrefad.transform.Find("onCardDame_lv" + level);
                        if (levelObj != null)
                        {
                            levelObj.gameObject.SetActive(true);

                            // Chạy animation trigger tương ứng
                            animator.SetTrigger("lv" + level);
                            Debug.Log($"✅ Chạy animation dameCard level {level}");
                        }
                        else
                        {
                            Debug.LogWarning($"❌ Không tìm thấy onCardDame_lv{level} trong {active.dameATKPrefad.name}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("dameATKPrefad thiếu Animator component!");
                    }

                    // Đợi animation hoàn thành (có thể điều chỉnh theo độ dài animation)
                    yield return new WaitForSeconds(2f);

                    // Tắt panel
                    active.dameATKPrefad.SetActive(false);
                }
                else
                {
                    Debug.LogWarning("active.dameATKPrefad == null!");
                }
                break;
        }
    }

    // ✅ Lấy tên sprite theo ID
    private string GetSpriteNameById(int id)
    {
        switch (id)
        {
            case 1: return "mau";    // HP
            case 2: return "mana";   // Mana
            case 3: return "no";     // No
            case 4: return "dameCard";
            default: return "mana";
        }
    }

    // ✅ Đảm bảo giá trị hợp lệ trong Inspector

    public void playAnimationCard()
    {
        if (onAnimationCardObject == null || cardAnimator == null)
        {
            Debug.LogWarning("Thiếu onAnimationCardObject hoặc cardAnimator!");
            return;
        }

        // Bật object để chạy animation
        onAnimationCardObject.SetActive(true);

        // Gán level cho animator (ví dụ trigger theo level)
        cardAnimator.SetInteger("level", dameCardLevel);

        // Chạy coroutine để tắt sau khi animation kết thúc
        StartCoroutine(DisableOnAnimationAfterPlay());
    }

    private IEnumerator DisableOnAnimationAfterPlay()
    {
        // Đợi đúng độ dài animation (có thể chỉnh tuỳ clip)
        yield return new WaitForSeconds(2f); // thời gian = độ dài clip animator

        onAnimationCardObject.SetActive(false);
        Debug.Log("✅ Animation card đã chạy xong và được tắt.");
    }

    private void OnValidate()
    {
        while (cardIDs.Count < cardCount)
            cardIDs.Add(1);
        while (cardIDs.Count > cardCount)
            cardIDs.RemoveAt(cardIDs.Count - 1);

        dameCardLevel = Mathf.Clamp(dameCardLevel, 1, 10);
    }

    // ✅ Public method để gọi từ ngoài (ví dụ: khi bắt đầu turn mới)
    public void OnNewTurn()
    {
        GenerateCards();
    }
}