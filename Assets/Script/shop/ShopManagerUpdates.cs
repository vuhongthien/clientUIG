// // =====================================================
// // CẬP NHẬT CHO ShopManager.cs
// // =====================================================

// // Thay đổi các method click handlers để lưu shopId thay vì itemId:

// // 1. THAY ĐỔI PRIVATE VARIABLES (dòng ~70)
// // CŨ:
// // private object pendingPurchaseItem;
// // private string pendingPurchaseType;

// // MỚI:
// private long pendingShopId; // Lưu shop.id để mua


// // 2. THAY ĐỔI OnItemClicked (dòng ~460)
// void OnItemClicked(int slotIndex)
// {
//     int dataIndex = currentPage * 15 + slotIndex;
//     if (dataIndex >= currentItems.Count) return;

//     ShopItemDTO item = currentItems[dataIndex];
    
//     // Items có thể mua nhiều lần, không check owned
    
//     pendingShopId = item.id; // ⚠️ Lưu shop.id
//     ShowPurchaseConfirmation(item.name, item.price, item.currencyType);
// }


// // 3. THAY ĐỔI OnPetClicked (dòng ~475)
// void OnPetClicked(int slotIndex)
// {
//     int dataIndex = currentPage * 3 + slotIndex;
//     if (dataIndex >= currentPets.Count) return;

//     ShopPetDTO pet = currentPets[dataIndex];
//     if (!pet.canPurchase)
//     {
//         ShowNotice("Bạn đã sở hữu pet này!");
//         return;
//     }

//     // ⚠️ Cần tìm shopId từ pet.id
//     // Cách 1: Lưu shopId trong DTO (khuyến nghị)
//     // Cách 2: Tìm trong shopData gốc
//     ShopItemDTO shopItem = FindShopItemByTypeAndId("PET", pet.id);
//     if (shopItem != null)
//     {
//         pendingShopId = shopItem.id;
//         ShowPurchaseConfirmation(pet.name, pet.price, pet.currencyType);
//     }
// }


// // 4. THAY ĐỔI OnAvatarClicked (dòng ~490)
// void OnAvatarClicked(int slotIndex)
// {
//     int dataIndex = currentPage * 2 + slotIndex;
//     if (dataIndex >= currentAvatars.Count) return;

//     ShopAvatarDTO avatar = currentAvatars[dataIndex];
//     if (avatar.owned)
//     {
//         ShowNotice("Bạn đã sở hữu avatar này!");
//         return;
//     }

//     // ⚠️ Cần tìm shopId từ avatar.id
//     ShopItemDTO shopItem = FindShopItemByTypeAndId("AVATAR", avatar.id);
//     if (shopItem != null)
//     {
//         pendingShopId = shopItem.id;
//         ShowPurchaseConfirmation(avatar.name, avatar.price, avatar.currencyType);
//     }
// }


// // 5. THÊM METHOD HELPER (thêm vào cuối class)
// /// <summary>
// /// Tìm shop item từ shopData gốc
// /// Vì Pet/Avatar DTO không có shopId, cần tìm ngược lại
// /// </summary>
// ShopItemDTO FindShopItemByTypeAndId(string itemType, long itemId)
// {
//     if (shopData == null || shopData.items == null) return null;
    
//     foreach (var item in shopData.items)
//     {
//         if (item.itemType == itemType && 
//             ((itemType == "PET" && item.id == itemId) ||
//              (itemType == "AVATAR" && item.id == itemId)))
//         {
//             return item;
//         }
//     }
    
//     return null;
// }


// // 6. THAY ĐỔI CloseNotice (dòng ~520)
// void CloseNotice()
// {
//     if (panelNotice != null) panelNotice.SetActive(false);
//     pendingShopId = 0; // Reset
// }


// // 7. THAY ĐỔI ConfirmPurchase (dòng ~525)
// void ConfirmPurchase()
// {
//     if (pendingShopId == 0)
//     {
//         CloseNotice();
//         return;
//     }

//     int userId = PlayerPrefs.GetInt("userId", 0);
//     ManagerGame.Instance.ShowLoading();
//     CloseNotice();

//     StartCoroutine(PurchaseItemCoroutine(userId));
// }


// // 8. THAY ĐỔI PurchaseItemCoroutine (dòng ~535)
// IEnumerator PurchaseItemCoroutine(int userId)
// {
//     PurchaseRequest request = new PurchaseRequest
//     {
//         userId = userId,
//         shopId = pendingShopId // ⚠️ Gửi shopId
//     };

//     string url = APIConfig.PURCHASE_ITEM();
    
//     yield return APIManager.Instance.PostRequest<PurchaseResponse>(
//         url,
//         request,
//         OnPurchaseSuccess,
//         OnPurchaseError
//     );
// }


// // 9. THAY ĐỔI OnPurchaseSuccess (dòng ~550)
// void OnPurchaseSuccess(PurchaseResponse response)
// {
//     ManagerGame.Instance.HideLoading();
    
//     if (response.success)
//     {
//         ShowNotice(response.message);
        
//         // Update currency
//         if (txtGold != null) txtGold.text = response.newGold.ToString();
//         if (txtRuby != null) txtRuby.text = response.newRuby.ToString();
        
//         // Refresh shop data
//         LoadShopData();
        
//         // Refresh user info
//         if (ManagerQuangTruong.Instance != null)
//         {
//             ManagerQuangTruong.Instance.RefreshUserInfo(true);
//         }
//     }
//     else
//     {
//         ShowNotice(response.message);
//     }

//     pendingShopId = 0; // Reset
// }


// // 10. THAY ĐỔI OnPurchaseError (dòng ~575)
// void OnPurchaseError(string error)
// {
//     ManagerGame.Instance.HideLoading();
//     ShowNotice("Có lỗi xảy ra khi mua hàng. Vui lòng thử lại!");
//     Debug.LogError($"[ShopManager] Purchase error: {error}");
    
//     pendingShopId = 0; // Reset
// }


// // =====================================================
// // GIẢI PHÁP TỐT HƠN: Thêm shopId vào Pet/Avatar DTO
// // =====================================================

// // Nếu bạn muốn giải pháp sạch hơn, hãy:
// // 1. Thêm field shopId vào ShopPetDTO và ShopAvatarDTO (cả C# và Java)
// // 2. Backend populate shopId khi convert
// // 3. Unity dùng trực tiếp pet.shopId / avatar.shopId

// // Ví dụ trong ShopService.java - method convertToPetDTO:
// private ShopPetDTO convertToPetDTO(Shop shop, Long userId) {
//     ShopPetDTO dto = new ShopPetDTO();
//     dto.setShopId(shop.getId()); // ⚠️ THÊM DÒNG NÀY
//     dto.setId(shop.getItemId());
//     // ... rest of code
// }

// // Và trong ShopDTOs (C# và Java):
// public class ShopPetDTO {
//     public long shopId; // ⚠️ THÊM FIELD NÀY
//     public long id;
//     // ... rest
// }

// // Khi đó OnPetClicked sẽ đơn giản hơn:
// void OnPetClicked(int slotIndex) {
//     // ...
//     pendingShopId = pet.shopId; // Dùng trực tiếp
//     ShowPurchaseConfirmation(pet.name, pet.price, pet.currencyType);
// }
