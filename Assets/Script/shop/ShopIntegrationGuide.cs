// // ===========================================
// // THÊM VÀO ManagerQuangTruong.cs
// // ===========================================

// // 1. THÊM VÀO HEADER CỦA CLASS (sau các Header hiện có):

// [Header("Shop Button")]
// public Button btnShop; // Button để mở cửa hàng

// // 2. THÊM VÀO METHOD Start() (sau phần Setup GiftBox):

// // Setup Shop button
// if (btnShop != null)
// {
//     btnShop.onClick.AddListener(OpenShop);
// }

// // 3. THÊM METHOD MỚI VÀO CUỐI CLASS:

// /// <summary>
// /// Mở cửa hàng
// /// </summary>
// public void OpenShop()
// {
//     if (ShopManager.Instance != null)
//     {
//         ShopManager.Instance.OpenShop();
//     }
//     else
//     {
//         Debug.LogError("[ManagerQuangTruong] ShopManager.Instance is null!");
//     }
// }

// // ===========================================
// // HƯỚNG DẪN SETUP TRONG UNITY
// // ===========================================

// /*
// 1. Tạo GameObject "ShopManager" trong scene QuangTruong
//    - Add component ShopManager.cs
   
// 2. Setup ShopManager trong Inspector:
   
//    a) Main Shop Panel:
//       - Kéo PanelShop vào field panelShop
//       - Kéo button đóng vào btnClose
   
//    b) Category Buttons:
//       - Kéo btnitem vào btnItem
//       - Kéo btnpet vào btnPet
//       - Kéo btnleft và btnright vào btnAvt
   
//    c) Lists:
//       - Kéo listavt vào field listavt
//       - Kéo listpet vào field listpet
//       - Kéo listitem vào field listitem
   
//    d) Avatar Board (2 slots):
//       - Trong listavt -> Board -> có 2 avt gameobjects
//       - Kéo từng Image component vào avtImages[0] và avtImages[1]
//       - Tương tự cho txtAtk, txtMana, txtHp, txtPrice
//       - Kéo Button component vào avtButtons[0] và avtButtons[1]
   
//    e) Pet Board (3 slots):
//       - Trong listpet -> Board -> có 3 pet gameobjects
//       - Setup tương tự Avatar board cho 3 slots
   
//    f) Item Board (15 slots):
//       - Trong listitem -> Board -> có 15 item gameobjects
//       - Setup tương tự cho 15 slots
   
//    g) Pagination:
//       - Kéo btnleft vào field btnLeft
//       - Kéo btnright vào field btnRight
//       - Nếu có Text hiển thị page (ví dụ "1/3"), kéo vào txtPageInfo
   
//    h) Currency Display:
//       - Kéo GYourGold -> txtMau vào field txtGold
//       - Kéo GYourRuby -> txtMau vào field txtRuby
   
//    i) Notice Panel:
//       - Kéo panel notice vào field panelNotice
//       - Kéo text notice vào txtNotice
//       - Kéo button đóng vào btnNoticeClose
//       - Kéo button đồng ý vào btnNoticeConfirm
   
//    j) Stone Sprites:
//       - Assign 7 sprites cho mỗi hệ (Fire, Water, Wind, Earth, Thunder)
//       - Mỗi array từ index 0-6 tương ứng level 5-7 của đá
//       - Ví dụ: stoneFire[0] = Fire Stone Level 5
//                 stoneFire[1] = Fire Stone Level 6
//                 stoneFire[2] = Fire Stone Level 7
//                 (cần update lại logic nếu cần level 1-7)

// 3. Thêm button mở shop vào ManagerQuangTruong:
//    - Tạo button "Cửa Hàng" trong UI
//    - Trong ManagerQuangTruong Inspector, kéo button vào field btnShop

// 4. Cấu trúc GameObjects (ví dụ):
   
//    PanelShop
//    ├── bgBorder
//    ├── btnClose
//    ├── btnitem (Button category)
//    ├── btnpet (Button category)  
//    ├── btnleft (Button category - đổi tên thành btnAvt nếu cần)
//    ├── btnright (Pagination)
//    ├── btnleft (Pagination)
//    ├── GYourGold
//    │   └── txtMau (Text for gold)
//    ├── GYourRuby
//    │   └── txtMau (Text for ruby)
//    ├── listavt
//    │   └── Board
//    │       └── listPanel
//    │           ├── avt[0]
//    │           │   ├── Image (imgavt)
//    │           │   ├── Text (atk, mana, hp, price)
//    │           │   └── Button
//    │           └── avt[1]
//    │               └── (tương tự)
//    ├── listpet
//    │   └── Board
//    │       └── listPanel
//    │           ├── pet[0]
//    │           ├── pet[1]
//    │           └── pet[2]
//    ├── listitem
//    │   └── Board
//    │       └── listPanel
//    │           ├── item[0]
//    │           ├── item[1]
//    │           ├── ...
//    │           └── item[14]
//    └── PanelNotice
//        ├── txtNotice
//        ├── btnClose
//        └── btnConfirm

// 5. Resources folder structure:
//    /Resources
//    ├── Image
//    │   ├── IconsPet
//    │   │   ├── 1.png
//    │   │   ├── 2.png
//    │   │   └── ...
//    │   ├── Avatars
//    │   │   ├── 1.png
//    │   │   ├── 2.png
//    │   │   └── ...
//    │   ├── Cards
//    │   │   └── [cardId].png
//    │   └── Shop
//    │       ├── energy.png
//    │       ├── wheel.png
//    │       ├── star_white.png
//    │       ├── star_blue.png
//    │       └── star_red.png

// 6. Trong APIConfig.cs, thêm:
   
//    public static string GET_SHOP_DATA(int userId)
//    {
//        return $"{BASE_URL}/shop/{userId}";
//    }
   
//    public static string PURCHASE_ITEM()
//    {
//        return $"{BASE_URL}/shop/purchase";
//    }

// 7. Test workflow:
//    - Click button "Cửa Hàng" -> Panel shop hiện
//    - Click btnitem -> Hiển thị list items
//    - Click btnpet -> Hiển thị list pets
//    - Click btnAvt -> Hiển thị list avatars
//    - Click item/pet/avatar -> Hiện notice confirm
//    - Click đồng ý -> Gọi API mua -> Update UI
//    - Click btnleft/btnright -> Chuyển trang
// */
