using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    [Header("Main Shop Panel")]
    public GameObject panelShop;
    public Button btnClose;

    [Header("Category Buttons")]
    public Button btnItem;
    public Button btnPet;
    public Button btnAvt;
    private Vector3 btnNormalScale = Vector3.one;
    private Vector3 btnSelectedScale = new Vector3(1.15f, 1.15f, 1f);

    [Header("Lists")]
    public GameObject listavt;
    public GameObject listpet;
    public GameObject listitem;

    [Header("Avatar Board - 2 slots")]
    public Image[] avtImages = new Image[2];
    public Text[] avtAtk = new Text[2];
    public Text[] avtMana = new Text[2];
    public Text[] avtHp = new Text[2];
    public Text[] avtPrice = new Text[2];
    public Button[] avtButtons = new Button[2];

    [Header("Pet Board - 3 slots")]
    public Image[] petImages = new Image[3];
    public Text[] petAtk = new Text[3];
    public Text[] petMana = new Text[3];
    public Text[] petHp = new Text[3];
    public Text[] petPrice = new Text[3];
    public Button[] petButtons = new Button[3];

    [Header("Item Board - 15 slots")]
    public Image[] itemImages = new Image[15];
    public Text[] itemCount = new Text[15];
    public Text[] itemPrice = new Text[15];
    public Button[] itemButtons = new Button[15];

    [Header("Pagination")]
    public Button btnLeft;
    public Button btnRight;
    public Text txtPageInfo; // "1/3"

    [Header("Currency Display")]
    public Text txtGold; // GYourGold -> txtMau
    public Text txtRuby; // GYourRuby -> txtMau

    [Header("Notice Panel")]
    public GameObject panelNotice;
    public Text txtNotice;
    public Button btnNoticeClose;
    public Button btnNoticeConfirm;

    [Header("Stone Sprites - 5 Hệ x 7 Levels")]
    public Sprite[] stoneFire = new Sprite[7];
    public Sprite[] stoneWater = new Sprite[7];
    public Sprite[] stoneWind = new Sprite[7];
    public Sprite[] stoneEarth = new Sprite[7];
    public Sprite[] stoneThunder = new Sprite[7];

    // Data
    private ShopDataResponse shopData;
    private List<ShopItemDTO> currentItems;
    private List<ShopPetDTO> currentPets;
    private List<ShopAvatarDTO> currentAvatars;

    // Pagination
    private int currentPage = 0;
    private int totalPages = 1;
    private string currentCategory = "AVATAR";

    // Pending purchase
    private object pendingPurchaseItem;
    private string pendingPurchaseType;
    private long pendingShopId;

    [Header("Item Sprites")]
    public Sprite spriteEnergy;
    public Sprite spriteWheel;
    public Sprite spriteStarWhite;
    public Sprite spriteStarBlue;
    public Sprite spriteStarRed;

    [Header("Animation Settings")]
    public float itemPopDelay = 0.05f;
    public float buttonHoverScale = 1.1f;

    [Header("Price Prefabs")]
    public GameObject prefabGoldPrice; // Gprice
    public GameObject prefabRubyPrice; // Rprice


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

    private void Start()
    {
        SetupButtons();
        HideAllLists();
        if (panelShop != null) panelShop.SetActive(false);
        if (panelNotice != null) panelNotice.SetActive(false);
    }

    void SetupButtons()
    {
        if (btnClose != null) btnClose.onClick.AddListener(CloseShop);
        if (btnItem != null) btnItem.onClick.AddListener(() => SwitchCategory("ITEM", resetPage: true));
        if (btnPet != null) btnPet.onClick.AddListener(() => SwitchCategory("PET", resetPage: true));
        if (btnAvt != null) btnAvt.onClick.AddListener(() => SwitchCategory("AVATAR", resetPage: true));
        if (btnLeft != null) btnLeft.onClick.AddListener(PreviousPage);
        if (btnRight != null) btnRight.onClick.AddListener(NextPage);
        if (btnNoticeClose != null) btnNoticeClose.onClick.AddListener(CloseNotice);
        if (btnNoticeConfirm != null) btnNoticeConfirm.onClick.AddListener(ConfirmPurchase);

        // Setup item buttons với hover animation
        for (int i = 0; i < itemButtons.Length; i++)
        {
            int index = i;
            if (itemButtons[i] != null)
            {
                itemButtons[i].onClick.AddListener(() => OnItemClicked(index));
                AddButtonHoverAnimation(itemButtons[i].gameObject);
            }
        }

        // Setup pet buttons với hover animation
        for (int i = 0; i < petButtons.Length; i++)
        {
            int index = i;
            if (petButtons[i] != null)
            {
                petButtons[i].onClick.AddListener(() => OnPetClicked(index));
                AddButtonHoverAnimation(petButtons[i].gameObject);
            }
        }

        // Setup avatar buttons với hover animation
        for (int i = 0; i < avtButtons.Length; i++)
        {
            int index = i;
            if (avtButtons[i] != null)
            {
                avtButtons[i].onClick.AddListener(() => OnAvatarClicked(index));
                AddButtonHoverAnimation(avtButtons[i].gameObject);
            }
        }

        // Add category button animations
        if (btnItem != null) AddCategoryButtonAnimation(btnItem.gameObject);
        if (btnPet != null) AddCategoryButtonAnimation(btnPet.gameObject);
        if (btnAvt != null) AddCategoryButtonAnimation(btnAvt.gameObject);
    }

    // ANIMATION: Thêm hover effect cho buttons
    void AddButtonHoverAnimation(GameObject buttonObj)
    {
        UnityEngine.EventSystems.EventTrigger trigger = buttonObj.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (trigger == null)
        {
            trigger = buttonObj.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        }

        // Hover enter
        UnityEngine.EventSystems.EventTrigger.Entry entryEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
        entryEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data) =>
        {
            LeanTween.cancel(buttonObj);
            LeanTween.scale(buttonObj, Vector3.one * buttonHoverScale, 0.2f).setEaseOutBack();
        });
        trigger.triggers.Add(entryEnter);

        // Hover exit
        UnityEngine.EventSystems.EventTrigger.Entry entryExit = new UnityEngine.EventSystems.EventTrigger.Entry();
        entryExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) =>
        {
            LeanTween.cancel(buttonObj);
            LeanTween.scale(buttonObj, Vector3.one, 0.2f).setEaseOutBack();
        });
        trigger.triggers.Add(entryExit);
    }

    // ANIMATION: Thêm pulse effect cho category buttons
    void AddCategoryButtonAnimation(GameObject buttonObj)
    {
        UnityEngine.EventSystems.EventTrigger trigger = buttonObj.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (trigger == null)
        {
            trigger = buttonObj.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        }

        UnityEngine.EventSystems.EventTrigger.Entry entry = new UnityEngine.EventSystems.EventTrigger.Entry();
        entry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        entry.callback.AddListener((data) =>
        {
            LeanTween.cancel(buttonObj);
            LeanTween.scale(buttonObj, buttonObj.transform.localScale * 1.05f, 0.15f)
                .setEaseInOutSine()
                .setLoopPingPong(1);
        });
        trigger.triggers.Add(entry);
    }

    // ANIMATION: Open shop với scale và fade
    public void OpenShop()
    {
        if (panelShop != null)
        {
            panelShop.SetActive(true);

            // Scale animation từ 0.8 lên 1.0
            panelShop.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            LeanTween.scale(panelShop, Vector3.one, 0.4f).setEaseOutBack();

            // Fade in nếu có CanvasGroup
            CanvasGroup canvasGroup = panelShop.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
                LeanTween.alphaCanvas(canvasGroup, 1f, 0.3f);
            }

            LoadShopData();
        }
    }

    // ANIMATION: Close shop với animation
    public void CloseShop()
    {
        if (panelShop != null)
        {
            // Scale down animation
            LeanTween.scale(panelShop, new Vector3(0.8f, 0.8f, 1f), 0.25f)
                .setEaseInBack()
                .setOnComplete(() => panelShop.SetActive(false));

            // Fade out
            CanvasGroup canvasGroup = panelShop.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                LeanTween.alphaCanvas(canvasGroup, 0f, 0.2f);
            }
        }
    }

    void LoadShopData()
    {
        int userId = PlayerPrefs.GetInt("userId", 0);
        if (userId == 0)
        {
            Debug.LogError("[ShopManager] User ID not found!");
            return;
        }

        ManagerGame.Instance.ShowLoading();
        StartCoroutine(LoadShopDataCoroutine(userId));
    }

    IEnumerator LoadShopDataCoroutine(int userId)
    {
        string url = APIConfig.GET_SHOP_DATA(userId);

        yield return APIManager.Instance.GetRequest<ShopDataResponse>(
            url,
            OnShopDataLoaded,
            OnShopDataError
        );
    }

    void OnShopDataLoaded(ShopDataResponse data)
{
    ManagerGame.Instance.HideLoading();
    shopData = data;

    Debug.Log($"[ShopManager] Shop data loaded - Items: {data.items?.Count}, Pets: {data.pets?.Count}, Avatars: {data.avatars?.Count}");

    // Update currency display
    UpdateCurrencyDisplay();

    // Giữ nguyên category và page hiện tại
    if (string.IsNullOrEmpty(currentCategory))
    {
        // Lần đầu mở shop
        SwitchCategory("AVATAR", resetPage: true);
    }
    else
    {
        // Sau khi mua → giữ nguyên page
        SwitchCategory(currentCategory, resetPage: false);
    }
}

    void OnShopDataError(string error)
    {
        ManagerGame.Instance.HideLoading();
        Debug.LogError($"[ShopManager] Error loading shop data: {error}");
        ShowNotice("Không thể tải dữ liệu cửa hàng. Vui lòng thử lại!");
    }

    void UpdateCurrencyDisplay()
    {
        int userId = PlayerPrefs.GetInt("userId", 0);
        StartCoroutine(UpdateCurrencyCoroutine(userId));
    }

    IEnumerator UpdateCurrencyCoroutine(int userId)
    {
        yield return APIManager.Instance.GetRequest<UserDTO>(
            APIConfig.GET_USER(userId),
            (user) =>
            {
                // ANIMATION: Number count up effect
                if (txtGold != null) AnimateNumberChange(txtGold, user.gold);
                if (txtRuby != null) AnimateNumberChange(txtRuby, user.ruby);
            },
            (error) => Debug.LogError($"[ShopManager] Currency update error: {error}")
        );
    }

    // ANIMATION: Animate số tiền thay đổi
    void AnimateNumberChange(Text textComponent, int targetValue)
    {
        if (textComponent == null) return;

        int currentValue = 0;
        int.TryParse(textComponent.text, out currentValue);

        LeanTween.value(textComponent.gameObject, currentValue, targetValue, 0.5f)
            .setOnUpdate((float val) =>
            {
                textComponent.text = Mathf.RoundToInt(val).ToString();
            })
            .setEaseOutQuad();
    }

    // ANIMATION: Switch category với fade và slide
    void SwitchCategory(string category, bool resetPage = true)
{
    currentCategory = category;
    
    if (resetPage)
    {
        currentPage = 0;
    }

    // Fade out current list
    GameObject currentList = GetCurrentListObject();
    if (currentList != null && currentList.activeSelf)
    {
        FadeOutList(currentList, () =>
        {
            HideAllLists();
            ResetButtonScales();
            ShowCategoryContent(category);
        });
    }
    else
    {
        HideAllLists();
        ResetButtonScales();
        ShowCategoryContent(category);
    }
}

    GameObject GetCurrentListObject()
    {
        switch (currentCategory)
        {
            case "ITEM": return listitem;
            case "PET": return listpet;
            case "AVATAR": return listavt;
            default: return null;
        }
    }

    void FadeOutList(GameObject listObj, System.Action onComplete)
    {
        CanvasGroup cg = listObj.GetComponent<CanvasGroup>();
        if (cg == null) cg = listObj.AddComponent<CanvasGroup>();

        LeanTween.alphaCanvas(cg, 0f, 0.2f)
            .setOnComplete(() => onComplete?.Invoke());
    }

    void ShowCategoryContent(string category)
    {
        GameObject targetList = null;
        Button targetButton = null;

        switch (category)
        {
            case "ITEM":
                targetList = listitem;
                targetButton = btnItem;
                currentItems = shopData?.items ?? new List<ShopItemDTO>();
                totalPages = Mathf.CeilToInt(currentItems.Count / 15f);
                break;

            case "PET":
                targetList = listpet;
                targetButton = btnPet;
                currentPets = shopData?.pets ?? new List<ShopPetDTO>();
                totalPages = Mathf.CeilToInt(currentPets.Count / 3f);
                break;

            case "AVATAR":
                targetList = listavt;
                targetButton = btnAvt;
                currentAvatars = shopData?.avatars ?? new List<ShopAvatarDTO>();
                totalPages = Mathf.CeilToInt(currentAvatars.Count / 2f);
                break;
        }

        // Animate button selection
        if (targetButton != null)
        {
            LeanTween.cancel(targetButton.gameObject);
            LeanTween.scale(targetButton.gameObject, btnSelectedScale, 0.3f).setEaseOutBack();
        }

        // Show and fade in list
        if (targetList != null)
        {
            targetList.SetActive(true);

            CanvasGroup cg = targetList.GetComponent<CanvasGroup>();
            if (cg == null) cg = targetList.AddComponent<CanvasGroup>();

            cg.alpha = 0;
            LeanTween.alphaCanvas(cg, 1f, 0.3f);

            // Display items với stagger animation
            StartCoroutine(DisplayCategoryWithAnimation(category));
        }

        UpdatePaginationUI();
    }

    // ANIMATION: Hiển thị items với stagger effect
    IEnumerator DisplayCategoryWithAnimation(string category)
    {
        switch (category)
        {
            case "ITEM":
                yield return StartCoroutine(DisplayItemsAnimated());
                break;
            case "PET":
                yield return StartCoroutine(DisplayPetsAnimated());
                break;
            case "AVATAR":
                yield return StartCoroutine(DisplayAvatarsAnimated());
                break;
        }
    }

    void HideAllLists()
    {
        if (listavt != null) listavt.SetActive(false);
        if (listpet != null) listpet.SetActive(false);
        if (listitem != null) listitem.SetActive(false);
    }

    void ResetButtonScales()
    {
        if (btnItem != null)
        {
            LeanTween.cancel(btnItem.gameObject);
            LeanTween.scale(btnItem.gameObject, btnNormalScale, 0.2f);
        }
        if (btnPet != null)
        {
            LeanTween.cancel(btnPet.gameObject);
            LeanTween.scale(btnPet.gameObject, btnNormalScale, 0.2f);
        }
        if (btnAvt != null)
        {
            LeanTween.cancel(btnAvt.gameObject);
            LeanTween.scale(btnAvt.gameObject, btnNormalScale, 0.2f);
        }
    }

    // ANIMATION: Display items với pop effect
    IEnumerator DisplayItemsAnimated()
    {
        int startIndex = currentPage * 15;

        for (int i = 0; i < 15; i++)
        {
            int dataIndex = startIndex + i;

            if (dataIndex < currentItems.Count)
            {
                ShopItemDTO item = currentItems[dataIndex];

                if (itemImages[i] != null)
                {
                    itemImages[i].sprite = GetItemSprite(item);
                    itemImages[i].gameObject.SetActive(true);

                    itemImages[i].transform.localScale = Vector3.zero;
                    LeanTween.scale(itemImages[i].gameObject, Vector3.one, 0.3f)
                        .setEaseOutBack()
                        .setDelay(i * itemPopDelay);
                }

                if (itemCount[i] != null)
                {
                    if (item.itemType == "CARD")
                    {
                        itemCount[i].text = item.name;
                    }
                    else
                    {
                        itemCount[i].text = "x" + item.value.ToString();
                    }
                }

                // SỬA: Không cần ref cachedObject nữa
                GameObject dummy = null;
                SetPriceWithPrefab(itemPrice[i], item.price, item.currencyType, ref dummy);

                if (itemButtons[i] != null)
                {
                    itemButtons[i].gameObject.SetActive(true);
                    itemButtons[i].interactable = true;
                }
            }
            else
            {
                if (itemImages[i] != null) itemImages[i].gameObject.SetActive(false);
                if (itemButtons[i] != null) itemButtons[i].gameObject.SetActive(false);
                if (itemPrice[i] != null && itemPrice[i].transform.parent != null)
                {
                    itemPrice[i].transform.parent.gameObject.SetActive(false);
                }
            }

            if (i % 5 == 4) yield return new WaitForSeconds(0.05f);
        }
    }

    void DisplayItems()
    {
        StartCoroutine(DisplayItemsAnimated());
    }

    Sprite GetItemSprite(ShopItemDTO item)
    {
        switch (item.itemType)
        {
            case "STONE":
                return GetStoneSpriteByTypeAndLevel(item.elementType, item.level);

            case "ENERGY":
                return spriteEnergy;

            case "WHEEL":
                return spriteWheel;

            case "STAR_WHITE":
                return spriteStarWhite;

            case "STAR_BLUE":
                return spriteStarBlue;

            case "STAR_RED":
                return spriteStarRed;

            case "CARD":
                return Resources.Load<Sprite>("Image/Card/" + "card" + item.cardId);

            default:
                return null;
        }
    }

    Sprite GetStoneSpriteByTypeAndLevel(string elementType, int level)
    {
        if (level < 1 || level > 7) return null;

        int index = level - 1;

        switch (elementType)
        {
            case "FIRE":
                return stoneFire != null && stoneFire.Length > index ? stoneFire[index] : null;
            case "WATER":
                return stoneWater != null && stoneWater.Length > index ? stoneWater[index] : null;
            case "WOOD":
                return stoneWind != null && stoneWind.Length > index ? stoneWind[index] : null;
            case "EARTH":
                return stoneEarth != null && stoneEarth.Length > index ? stoneEarth[index] : null;
            case "METAL":
                return stoneThunder != null && stoneThunder.Length > index ? stoneThunder[index] : null;
            default:
                return null;
        }
    }

    // ANIMATION: Display pets với slide in effect
    IEnumerator DisplayPetsAnimated()
    {
        int startIndex = currentPage * 3;

        for (int i = 0; i < 3; i++)
        {
            int dataIndex = startIndex + i;

            if (dataIndex < currentPets.Count)
            {
                ShopPetDTO pet = currentPets[dataIndex];

                if (petImages[i] != null)
                {
                    Sprite petSprite = Resources.Load<Sprite>("Image/IconsPet/" + pet.id);
                    if (petSprite != null) petImages[i].sprite = petSprite;
                    petImages[i].gameObject.SetActive(true);

                    RectTransform rt = petImages[i].GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        Vector3 originalPos = rt.anchoredPosition;
                        rt.anchoredPosition = new Vector3(originalPos.x - 100f, originalPos.y, 0);
                        LeanTween.moveLocal(rt.gameObject, originalPos, 0.4f)
                            .setEaseOutBack()
                            .setDelay(i * 0.1f);
                    }
                }

                if (petAtk[i] != null) petAtk[i].text = pet.attack.ToString();
                if (petMana[i] != null) petMana[i].text = pet.mana.ToString();
                if (petHp[i] != null) petHp[i].text = pet.hp.ToString();

                // SỬA: Không cần ref cachedObject nữa
                GameObject dummy = null;
                SetPriceWithPrefab(petPrice[i], pet.price, pet.currencyType, ref dummy, !pet.canPurchase);

                if (petButtons[i] != null)
                {
                    petButtons[i].gameObject.SetActive(true);
                    petButtons[i].interactable = pet.canPurchase;
                }
            }
            else
            {
                if (petImages[i] != null) petImages[i].gameObject.SetActive(false);
                if (petButtons[i] != null) petButtons[i].gameObject.SetActive(false);
                if (petPrice[i] != null && petPrice[i].transform.parent != null)
                {
                    petPrice[i].transform.parent.gameObject.SetActive(false);
                }
            }

            yield return new WaitForSeconds(0.05f);
        }
    }

    void DisplayPets()
    {
        StartCoroutine(DisplayPetsAnimated());
    }

    // ANIMATION: Display avatars với fade và scale
    IEnumerator DisplayAvatarsAnimated()
{
    int startIndex = currentPage * 2;

    for (int i = 0; i < 2; i++)
    {
        int dataIndex = startIndex + i;

        if (dataIndex < currentAvatars.Count)
        {
            ShopAvatarDTO avatar = currentAvatars[dataIndex];

            if (avtImages[i] != null)
            {
                Sprite avtSprite = Resources.Load<Sprite>("Image/Avt/" + avatar.id);
                if (avtSprite != null) avtImages[i].sprite = avtSprite;
                avtImages[i].gameObject.SetActive(true);

                // FLIP NGANG (scale X từ 0 lên 1)
                avtImages[i].transform.localScale = new Vector3(0f, 1f, 1f);
                
                LeanTween.scaleX(avtImages[i].gameObject, 1f, 0.4f)
                    .setEaseOutBack()
                    .setDelay(i * 0.15f);
            }

            if (avtAtk[i] != null) avtAtk[i].text = avatar.attack.ToString();
            if (avtMana[i] != null) avtMana[i].text = avatar.mana.ToString();
            if (avtHp[i] != null) avtHp[i].text = avatar.hp.ToString();

            GameObject dummy = null;
            SetPriceWithPrefab(avtPrice[i], avatar.price, avatar.currencyType, ref dummy, avatar.owned);

            if (avtButtons[i] != null)
            {
                avtButtons[i].gameObject.SetActive(true);
                avtButtons[i].interactable = !avatar.owned;
            }
        }
        else
        {
            if (avtImages[i] != null) avtImages[i].gameObject.SetActive(false);
            if (avtButtons[i] != null) avtButtons[i].gameObject.SetActive(false);
            if (avtPrice[i] != null && avtPrice[i].transform.parent != null)
            {
                avtPrice[i].transform.parent.gameObject.SetActive(false);
            }
        }

        yield return new WaitForSeconds(0.1f);
    }
}

    void DisplayAvatars()
    {
        StartCoroutine(DisplayAvatarsAnimated());
    }

    void UpdatePaginationUI()
    {
        if (txtPageInfo != null)
        {
            txtPageInfo.text = $"{currentPage + 1}/{totalPages}";
        }

        if (btnLeft != null)
        {
            btnLeft.interactable = currentPage > 0;
        }

        if (btnRight != null)
        {
            btnRight.interactable = currentPage < totalPages - 1;
        }
    }

    // ANIMATION: Page transition với slide effect
    void PreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            AnimatePageTransition(-1);
        }
    }

    void NextPage()
    {
        if (currentPage < totalPages - 1)
        {
            currentPage++;
            AnimatePageTransition(1);
        }
    }

    void AnimatePageTransition(int direction)
    {
        GameObject currentList = GetCurrentListObject();
        if (currentList == null) return;

        CanvasGroup cg = currentList.GetComponent<CanvasGroup>();
        if (cg == null) cg = currentList.AddComponent<CanvasGroup>();

        // Fade out
        LeanTween.alphaCanvas(cg, 0f, 0.15f)
            .setOnComplete(() =>
            {
                RefreshCurrentCategory();

                // Fade in
                cg.alpha = 0;
                LeanTween.alphaCanvas(cg, 1f, 0.2f);
            });
    }

    void RefreshCurrentCategory()
    {
        switch (currentCategory)
        {
            case "ITEM":
                DisplayItems();
                break;
            case "PET":
                DisplayPets();
                break;
            case "AVATAR":
                DisplayAvatars();
                break;
        }
        UpdatePaginationUI();
    }

    // Click handlers với animation feedback
    void OnItemClicked(int slotIndex)
    {
        Debug.Log($"[ShopManager] OnItemClicked: slotIndex={slotIndex}");

        int dataIndex = currentPage * 15 + slotIndex;
        if (dataIndex >= currentItems.Count)
        {
            Debug.LogWarning($"[ShopManager] dataIndex {dataIndex} >= currentItems.Count {currentItems.Count}");
            return;
        }

        ShopItemDTO item = currentItems[dataIndex];
        Debug.Log($"[ShopManager] Item clicked: {item.name}, shopId={item.id}");

        // Button click animation
        if (itemButtons[slotIndex] != null)
        {
            GameObject btnObj = itemButtons[slotIndex].gameObject;
            LeanTween.cancel(btnObj);
            LeanTween.scale(btnObj, Vector3.one * 0.9f, 0.1f)
                .setEaseInOutQuad()
                .setOnComplete(() =>
                {
                    LeanTween.scale(btnObj, Vector3.one, 0.1f);
                });
        }

        pendingShopId = item.id;
        ShowPurchaseConfirmation(item.name, item.price, item.currencyType);
    }

    void OnPetClicked(int slotIndex)
    {
        int dataIndex = currentPage * 3 + slotIndex;
        if (dataIndex >= currentPets.Count) return;

        ShopPetDTO pet = currentPets[dataIndex];
        if (!pet.canPurchase)
        {
            ShowNotice("Bạn đã mua đủ số lượng pet này trong ngày!");
            return;
        }

        // Button click animation
        if (petButtons[slotIndex] != null)
        {
            AnimateButtonClick(petButtons[slotIndex].gameObject);
        }

        pendingShopId = pet.shopId;
        ShowPurchaseConfirmation(pet.name, pet.price, pet.currencyType);
    }

    void OnAvatarClicked(int slotIndex)
    {
        Debug.Log($"[ShopManager] OnAvatarClicked: slotIndex={slotIndex}");

        int dataIndex = currentPage * 2 + slotIndex;
        if (dataIndex >= currentAvatars.Count)
        {
            Debug.LogWarning($"[ShopManager] Avatar dataIndex out of range");
            return;
        }

        ShopAvatarDTO avatar = currentAvatars[dataIndex];
        Debug.Log($"[ShopManager] Avatar: {avatar.name}, shopId={avatar.shopId}");

        if (avatar.owned)
        {
            ShowNotice("Bạn đã sở hữu avatar này!");
            return;
        }

        // Button click animation
        if (avtButtons[slotIndex] != null)
        {
            AnimateButtonClick(avtButtons[slotIndex].gameObject);
        }

        // SỬA: Dùng trực tiếp shopId từ avatar
        pendingShopId = avatar.shopId;
        ShowPurchaseConfirmation(avatar.name, avatar.price, avatar.currencyType);
    }

    // ANIMATION: Button click feedback
    void AnimateButtonClick(GameObject btnObj)
    {
        LeanTween.cancel(btnObj);
        LeanTween.scale(btnObj, Vector3.one * 0.9f, 0.1f)
            .setEaseInOutQuad()
            .setOnComplete(() =>
            {
                LeanTween.scale(btnObj, Vector3.one, 0.1f);
            });
    }

    // ANIMATION: Show notice panel với bounce
    void ShowPurchaseConfirmation(string itemName, int price, string currencyType)
    {
        Debug.Log($"[ShopManager] ShowPurchaseConfirmation: {itemName}, price={price}, pendingShopId={pendingShopId}");

        string currencyName = currencyType == "GOLD" ? "vàng" : "ruby";
        string message = $"Bạn có muốn mua {itemName} với giá {price} {currencyName}?";

        if (txtNotice != null) txtNotice.text = message;
        if (panelNotice != null)
        {
            panelNotice.SetActive(true);
            panelNotice.transform.localScale = Vector3.zero;
            LeanTween.scale(panelNotice, Vector3.one, 0.4f).setEaseOutBounce();
        }
        if (btnNoticeConfirm != null) btnNoticeConfirm.gameObject.SetActive(true);
    }

    void ShowNotice(string message)
    {
        if (txtNotice != null) txtNotice.text = message;
        if (panelNotice != null)
        {
            panelNotice.SetActive(true);

            // Shake animation for error notice
            LeanTween.cancel(panelNotice);
            panelNotice.transform.localScale = Vector3.one;
            LeanTween.rotateZ(panelNotice, 5f, 0.1f)
                .setLoopPingPong(3)
                .setOnComplete(() =>
                {
                    panelNotice.transform.rotation = Quaternion.identity;
                });
        }
        if (btnNoticeConfirm != null) btnNoticeConfirm.gameObject.SetActive(false);
    }

    // ANIMATION: Close notice với scale down
    void CloseNotice()
    {
        if (panelNotice != null)
        {
            LeanTween.scale(panelNotice, Vector3.zero, 0.25f)
                .setEaseInBack()
                .setOnComplete(() => panelNotice.SetActive(false));
        }
        pendingShopId = 0;
    }

    void ConfirmPurchase()
    {
        Debug.Log($"[ShopManager] ConfirmPurchase called, pendingShopId={pendingShopId}");

        if (pendingShopId == 0)
        {
            Debug.LogWarning("[ShopManager] pendingShopId is 0, closing notice");
            CloseNotice();
            return;
        }

        int userId = PlayerPrefs.GetInt("userId", 0);
        long shopIdToSend = pendingShopId;  // ← LƯU LẠI TRƯỚC

        ManagerGame.Instance.ShowLoading();
        CloseNotice();  // Ở đây pendingShopId sẽ = 0

        StartCoroutine(PurchaseItemCoroutine(userId, shopIdToSend));  // ← TRUYỀN VÀO
    }

    IEnumerator PurchaseItemCoroutine(int userId, long shopId)  // ← THÊM THAM SỐ
    {
        PurchaseRequest request = new PurchaseRequest
        {
            userId = userId,
            shopId = shopId  // ← DÙNG THAM SỐ
        };

        string url = APIConfig.PURCHASE_ITEM();

        yield return APIManager.Instance.PostRequest<PurchaseResponse>(
            url,
            request,
            OnPurchaseSuccess,
            OnPurchaseError
        );
    }

    void OnPurchaseSuccess(PurchaseResponse response)
    {
        ManagerGame.Instance.HideLoading();

        if (response.success)
        {
            ShowNotice(response.message);

            // Update currency với animation
            if (txtGold != null) AnimateNumberChange(txtGold, response.newGold);
            if (txtRuby != null) AnimateNumberChange(txtRuby, response.newRuby);

            // Success particle effect (nếu có)
            StartCoroutine(PlaySuccessEffect());

            // Refresh shop data
            LoadShopData();

            // Refresh user info
            if (ManagerQuangTruong.Instance != null)
            {
                ManagerQuangTruong.Instance.RefreshUserInfo(true);
            }
        }
        else
        {
            ShowNotice(response.message);
        }

        pendingShopId = 0;
    }

    void SetPriceWithPrefab(Text priceText, int price, string currencyType, ref GameObject cachedPriceObject, bool isOwned = false, string ownedText = "Đã sở hữu")
    {
        if (priceText == null) return;

        Transform parent = priceText.transform.parent; // Gprice cũ
        if (parent == null) return;

        // QUAN TRỌNG: Bật lại parent trước khi xử lý
        parent.gameObject.SetActive(true);

        // Tìm Image component trong parent (icon tiền tệ)
        Image priceIcon = parent.GetComponentInChildren<Image>();

        if (isOwned)
        {
            // Hiện text "Đã sở hữu"
            priceText.text = ownedText;
            priceText.color = Color.green;

            // Ẩn icon nếu có
            if (priceIcon != null)
            {
                priceIcon.enabled = false;
            }

            // Reset outline về mặc định (nếu có)
            UnityEngine.UI.Outline outline = priceText.GetComponent<UnityEngine.UI.Outline>();
            if (outline != null)
            {
                outline.effectColor = Color.black;
                outline.effectDistance = new Vector2(1, -1);
            }
        }
        else
        {
            // Chọn prefab tương ứng
            GameObject prefabToUse = (currencyType == "GOLD") ? prefabGoldPrice : prefabRubyPrice;

            if (prefabToUse != null)
            {
                // Lấy Image từ prefab
                Image prefabIcon = prefabToUse.GetComponentInChildren<Image>();
                if (prefabIcon != null && priceIcon != null)
                {
                    // Thay sprite cũ bằng sprite từ prefab
                    priceIcon.sprite = prefabIcon.sprite;
                    priceIcon.enabled = true; // BẬT LẠI icon
                }

                // Lấy Text từ prefab để copy màu/font/outline
                Text prefabText = prefabToUse.GetComponentInChildren<Text>();
                if (prefabText != null)
                {
                    // Set giá và copy style từ prefab
                    priceText.text = price.ToString();
                    priceText.color = prefabText.color;
                    priceText.font = prefabText.font;
                    priceText.fontSize = prefabText.fontSize;
                    priceText.fontStyle = prefabText.fontStyle;
                    priceText.alignment = prefabText.alignment;

                    // Copy Outline component từ prefab (nếu có)
                    UnityEngine.UI.Outline prefabOutline = prefabText.GetComponent<UnityEngine.UI.Outline>();
                    UnityEngine.UI.Outline currentOutline = priceText.GetComponent<UnityEngine.UI.Outline>();

                    if (prefabOutline != null)
                    {
                        // Nếu text cũ chưa có Outline, thêm vào
                        if (currentOutline == null)
                        {
                            currentOutline = priceText.gameObject.AddComponent<UnityEngine.UI.Outline>();
                        }

                        // Copy properties từ prefab outline
                        currentOutline.effectColor = prefabOutline.effectColor;
                        currentOutline.effectDistance = prefabOutline.effectDistance;
                        currentOutline.useGraphicAlpha = prefabOutline.useGraphicAlpha;
                        currentOutline.enabled = true;
                    }
                    else
                    {
                        // Nếu prefab không có outline, disable outline của text cũ (nếu có)
                        if (currentOutline != null)
                        {
                            currentOutline.enabled = false;
                        }
                    }
                }
                else
                {
                    // Nếu không có prefab text, chỉ set giá
                    priceText.text = price.ToString();
                    priceText.color = Color.white; // Đặt màu mặc định
                }
            }
        }
    }

    // ANIMATION: Success effect
    IEnumerator PlaySuccessEffect()
    {
        // Có thể thêm particle hoặc flash effect ở đây
        // Tạm thời làm flash màu xanh cho currency
        if (txtGold != null)
        {
            Color originalColor = txtGold.color;
            txtGold.color = Color.green;

            yield return new WaitForSeconds(0.3f);

            LeanTween.value(txtGold.gameObject, Color.green, originalColor, 0.5f)
                .setOnUpdate((Color val) => txtGold.color = val);
        }
    }

    void OnPurchaseError(string error)
    {
        ManagerGame.Instance.HideLoading();
        ShowNotice("Có lỗi xảy ra khi mua hàng. Vui lòng thử lại!");
        Debug.LogError($"[ShopManager] Purchase error: {error}");

        pendingShopId = 0;
    }

    // ANIMATION: Cleanup khi destroy
    void OnDestroy()
    {
        // Cancel tất cả LeanTween animations
        LeanTween.cancel(gameObject);

        if (panelShop != null) LeanTween.cancel(panelShop);
        if (panelNotice != null) LeanTween.cancel(panelNotice);

        // Cancel animations cho tất cả buttons
        foreach (var btn in itemButtons)
        {
            if (btn != null) LeanTween.cancel(btn.gameObject);
        }

        foreach (var btn in petButtons)
        {
            if (btn != null) LeanTween.cancel(btn.gameObject);
        }

        foreach (var btn in avtButtons)
        {
            if (btn != null) LeanTween.cancel(btn.gameObject);
        }
    }
}