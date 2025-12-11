using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using PhotonPlayer = Photon.Realtime.Player;

public class ManagerPVP : MonoBehaviourPunCallbacks
{
    [Header("Panel Controls")]
    public GameObject panelPet;
    public GameObject panelCard;
    public GameObject btnClosePet;
    public GameObject btnCloseCard;
    public Button btnOpenPet;
    public Button btnOpenCard;
    private bool isRotatingPet = false;
    private bool isRotatingCard = false;
    public float rotationSpeed = 200f;

    [Header("Loading")]
    public GameObject loading;

    [Header("Pet Selection")]
    public GameObject petUIPrefab;
    public Transform petListContainer;

    [Header("Pet Display")]
    public GameObject panelUser1;

    [Header("Lobby References")]
    public Transform[] lobbyPlayer; // Drag lobby player containers từ scene

    [Header("Debug Info")]
    public Text debugText; // Optional: để hiển thị debug info

    private string selectedPetId;
    private Image petImg;
    private bool isSceneLoaded = false; // Flag để đảm bảo chỉ load 1 lần
    private bool isPetsDataLoaded = false; // Flag để track trạng thái data
    private bool isUpdatingPetSelection = false; // Flag để ngăn duplicate pet updates
    private bool isLocalPlayerReady = false; // Track ready state của local player
    
    [Header("Photon")]
    public PhotonView photonView;

    // Event để notify các systems khác về việc thay đổi pet
    public System.Action<string> OnPetSelectionUpdated;

    void Awake()
    {
        photonView = GetComponent<PhotonView>();
    }
    
    void Start()
    {
        if (!isSceneLoaded)
        {
            isSceneLoaded = true;
            StartCoroutine(LoadSceneAfterDelay());
            ManagerGame.Instance.HideLoading();

            UpdateDebugInfo("Manager started - Loading scene data");
        }
        else
        {
            UpdateDebugInfo("Manager already initialized");
        }
    }

    #region Debug Methods
    private void UpdateDebugInfo(string message)
    {
        string info = $"[{Time.time:F1}] {message}";
        Debug.Log(info);

        if (debugText != null)
        {
            debugText.text += info + "\n";
            string[] lines = debugText.text.Split('\n');
            if (lines.Length > 10)
            {
                debugText.text = string.Join("\n", lines, lines.Length - 10, 10);
            }
        }
    }
    #endregion

    #region Initialization and Loading
    private IEnumerator LoadSceneAfterDelay()
    {
        if (isPetsDataLoaded)
        {
            UpdateDebugInfo("Pets data already loaded - skipping API call");
            yield break;
        }

        UpdateDebugInfo("Starting initial data load...");

        ManagerGame.Instance.LoadingPanel = loading;
        ManagerGame.Instance.ShowLoading();

        int userId = PlayerPrefs.GetInt("userId", 1);
        UpdateDebugInfo($"Loading pets for user {userId}");

        yield return APIManager.Instance.GetRequest<List<PetUserDTO>>(APIConfig.GET_ALL_PET_USERS(userId), OnPetsReceived, OnError);
    }

    void OnPetsReceived(List<PetUserDTO> pets)
    {
        if (isPetsDataLoaded)
        {
            UpdateDebugInfo("Pets data already processed - ignoring duplicate call");
            return;
        }

        isPetsDataLoaded = true; // Set flag để không load lại
        UpdateDebugInfo($"Received {pets.Count} pets from API - First time load");

        // Clear existing pets
        foreach (Transform child in petListContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var pet in pets)
        {
            GameObject petUIObject = Instantiate(petUIPrefab, petListContainer);
            Image petIcon = petUIObject.transform.Find("imgtPet")?.GetComponent<Image>();
            Image imgHe = petUIObject.transform.Find("imgHe")?.GetComponent<Image>();
            Text txtLv = petUIObject.transform.Find("txtLv")?.GetComponent<Text>();

            string petID = pet.petId.ToString();
            Sprite petSprite = Resources.Load<Sprite>("Image/IconsPet/" + petID);

            if (imgHe != null)
                imgHe.sprite = Resources.Load<Sprite>("Image/Attribute/" + pet.elementType);

            petUIObject.name = petID;

            if (petIcon != null)
                petIcon.sprite = petSprite;

            if (txtLv != null)
                txtLv.text = "Lv" + pet.level;

            Button petButton = petUIObject.GetComponent<Button>();
            if (petButton != null)
            {
                string capturedPetId = petID;
                petButton.onClick.AddListener(() => OnPetClicked(capturedPetId));
                UpdateDebugInfo($"Added click listener for pet {capturedPetId}");
            }

            // Highlight pet đã chọn từ PlayerPrefs
            int savedPetId = PlayerPrefs.GetInt("SelectedPetId", 1);
            if (petID == savedPetId.ToString())
            {
                SetPetHighlight(petUIObject, true);
                selectedPetId = petID;
                UpdateDebugInfo($"Auto-selected pet {petID} from PlayerPrefs");
            }
        }

        // Load initial pet display sau khi có data
        if (!string.IsNullOrEmpty(selectedPetId))
        {
            StartCoroutine(WaitAndUpdateLocalPetDisplay());
        }

        // Check initial ready state và update UI accordingly
        UpdateSelectionLockState();

        ManagerGame.Instance.HideLoading();
        UpdateDebugInfo("Initial pets data load completed");
    }

    void OnError(string error)
    {
        UpdateDebugInfo($"API Error: {error}");
        ManagerGame.Instance.HideLoading();
    }
    #endregion

    #region Pet Selection - Locked When Ready
    void OnPetClicked(string petId)
    {
        // Kiểm tra ready state - nếu ready thì không cho phép thay đổi
        if (isLocalPlayerReady)
        {
            UpdateDebugInfo($"PET CLICK BLOCKED - Player is ready: {petId}");
            ShowLockedMessage("Không thể thay đổi pet khi đã sẵn sàng!");
            return;
        }

        if (isUpdatingPetSelection)
        {
            UpdateDebugInfo($"PET CLICK IGNORED - Already updating: {petId}");
            return;
        }

        if (selectedPetId == petId)
        {
            UpdateDebugInfo($"PET ALREADY SELECTED: {petId}");
            return;
        }

        isUpdatingPetSelection = true;
        UpdateDebugInfo($"=== PET CLICKED: {petId} ===");

        try
        {
            // 1. Highlight pet được chọn
            UpdatePetHighlights(petId);

            // 2. Save selection
            selectedPetId = petId;
            PlayerPrefs.SetInt("SelectedPetId", int.Parse(petId));
            PlayerPrefs.SetInt("userPetId", int.Parse(petId));
            PlayerPrefs.Save();
            UpdateDebugInfo($"Saved selection: {petId}");

            // 3. Update local player pet UI ngay lập tức
            UpdateLocalPlayerPetDisplay(petId);

            // 4. Sync qua Photon
            StartCoroutine(DelayedSyncPetSelection(petId));

            // 5. Set cho ManagerDauTruong
            if (ManagerDauTruong.Instance != null)
            {
                ManagerDauTruong.Instance.petId = petId;
                UpdateDebugInfo("Updated ManagerDauTruong");
            }

            // 6. Notify other systems about pet change
            OnPetSelectionUpdated?.Invoke(petId);

            UpdateDebugInfo($"Successfully processed pet selection: {petId}");
        }
        catch (System.Exception e)
        {
            UpdateDebugInfo($"ERROR in OnPetClicked: {e.Message}");
        }
        finally
        {
            StartCoroutine(ResetUpdateFlag());
        }
    }

    private void ShowLockedMessage(string message)
    {
        // Hiển thị thông báo khi bị khóa (có thể dùng Toast, Dialog, etc.)
        UpdateDebugInfo($"LOCKED MESSAGE: {message}");
        
        // Có thể implement UI popup hoặc toast message ở đây
        // Ví dụ: ToastManager.Instance.ShowToast(message);
    }

    private IEnumerator ResetUpdateFlag()
    {
        yield return new WaitForSeconds(0.5f);
        isUpdatingPetSelection = false;
    }

    private IEnumerator DelayedSyncPetSelection(string petId)
    {
        yield return new WaitForSeconds(0.1f); // Delay nhỏ để tránh spam
        SyncPetSelection(petId);
    }

    /// <summary>
    /// Update pet display cho local player
    /// </summary>
    private void UpdateLocalPlayerPetDisplay(string petId)
    {
        UpdateDebugInfo($"UpdateLocalPlayerPetDisplay: {petId}");

        // Tìm và update local player trong lobby
        if (lobbyPlayer != null)
        {
            for (int i = 0; i < lobbyPlayer.Length; i++)
            {
                if (lobbyPlayer[i].childCount > 0)
                {
                    PlayerMatch playerMatch = lobbyPlayer[i].GetComponentInChildren<PlayerMatch>();
                    if (playerMatch != null && playerMatch.PlayerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
                    {
                        UpdateDebugInfo($"Found local PlayerMatch, updating pet");
                        playerMatch.SetSelectedPet(int.Parse(petId));
                        break;
                    }
                }
            }
        }

        // Cũng update petImg reference nếu có
        if (petImg != null)
        {
            SetPetSprite(petImg, int.Parse(petId));
        }
        else
        {
            StartCoroutine(FindAndSetPetImg(petId));
        }
    }

    /// <summary>
    /// Tìm petImg và set sprite
    /// </summary>
    private IEnumerator FindAndSetPetImg(string petId)
    {
        int attempts = 0;
        const int maxAttempts = 10;

        while (petImg == null && attempts < maxAttempts)
        {
            attempts++;
            yield return new WaitForSeconds(0.2f);

            // Tìm petImg trong panelUser1
            if (panelUser1 != null)
            {
                Image[] images = panelUser1.GetComponentsInChildren<Image>(true);
                foreach (var img in images)
                {
                    if (img.name.ToLower().Contains("pet"))
                    {
                        petImg = img;
                        break;
                    }
                }
            }

            if (petImg != null)
            {
                SetPetSprite(petImg, int.Parse(petId));
                UpdateDebugInfo("Found and set petImg after delay");
                yield break;
            }
        }

        if (attempts >= maxAttempts)
        {
            UpdateDebugInfo("Could not find petImg after max attempts");
        }
    }

    /// <summary>
    /// Set sprite cho Image component
    /// </summary>
    private bool SetPetSprite(Image targetImage, int petId)
    {
        if (targetImage == null)
        {
            return false;
        }

        Sprite petSprite = Resources.Load<Sprite>("Image/IconsPet/" + petId);
        if (petSprite == null)
        {
            return false;
        }

        targetImage.sprite = petSprite;

        if (targetImage.canvas != null)
        {
            Canvas.ForceUpdateCanvases();
        }

        UpdateDebugInfo($"Set sprite on image {targetImage.name}");
        return true;
    }

    private IEnumerator WaitAndUpdateLocalPetDisplay()
    {
        yield return new WaitForSeconds(0.5f); // Đợi UI được khởi tạo
        if (!string.IsNullOrEmpty(selectedPetId))
        {
            UpdateLocalPlayerPetDisplay(selectedPetId);
        }
    }

    void UpdatePetHighlights(string selectedPetId)
    {
        foreach (Transform child in petListContainer)
        {
            bool isSelected = child.name == selectedPetId;
            SetPetHighlight(child.gameObject, isSelected);
        }
    }

    void SetPetHighlight(GameObject petUIObject, bool highlight)
    {
        Image bgImage = petUIObject.GetComponent<Image>();
        if (bgImage != null)
        {
            bgImage.color = highlight ? Color.yellow : Color.white;
        }

        Outline outline = petUIObject.GetComponent<Outline>();
        if (highlight && outline == null)
        {
            outline = petUIObject.AddComponent<Outline>();
            outline.effectColor = Color.green;
            outline.effectDistance = new Vector2(2, 2);
        }
        else if (!highlight && outline != null)
        {
            Destroy(outline);
        }
    }
    #endregion

    #region Ready State Management - Lock When Ready
    /// <summary>
    /// Method để handle khi ready state thay đổi và khóa pet/card selection
    /// </summary>
    public void OnReadyStateChanged(bool isReady)
    {
        isLocalPlayerReady = isReady;
        UpdateDebugInfo($"Ready state changed: {isReady} - Updating selection lock state");
        
        // Update lock state cho pet/card selection
        UpdateSelectionLockState();
        
        // Visual feedback
        ShowReadyStateVisual(isReady);
    }

    /// <summary>
    /// Update trạng thái khóa/mở khóa pet và card selection
    /// </summary>
    private void UpdateSelectionLockState()
    {
        // Lock/Unlock pet selection buttons
        if (petListContainer != null)
        {
            Button[] petButtons = petListContainer.GetComponentsInChildren<Button>();
            foreach (var btn in petButtons)
            {
                btn.interactable = !isLocalPlayerReady;
                
                // Visual feedback cho locked buttons
                var img = btn.GetComponent<Image>();
                if (img != null)
                {
                    img.color = isLocalPlayerReady ? Color.gray : Color.white;
                }
            }
        }

        // Lock/Unlock pet panel button
        if (btnOpenPet != null)
        {
            btnOpenPet.interactable = !isLocalPlayerReady;
        }

        // Lock/Unlock card panel button  
        if (btnOpenCard != null)
        {
            btnOpenCard.interactable = !isLocalPlayerReady;
        }

        // Lock/Unlock card selection buttons
        if (panelCard != null)
        {
            Button[] cardButtons = panelCard.GetComponentsInChildren<Button>();
            foreach (var btn in cardButtons)
            {
                // Không lock close button
                if (btn.gameObject != btnCloseCard)
                {
                    btn.interactable = !isLocalPlayerReady;
                }
            }
        }

        UpdateDebugInfo($"Selection lock state updated - Locked: {isLocalPlayerReady}");
    }

    private void ShowReadyStateVisual(bool isReady)
    {
        if (isReady)
        {
            UpdateDebugInfo("Visual: Player is ready - Pet/Card selection LOCKED");
            
            // Có thể thêm visual effects như overlay, dimming, etc.
            ShowLockedOverlay(true);
        }
        else
        {
            UpdateDebugInfo("Visual: Player is not ready - Pet/Card selection UNLOCKED");
            
            ShowLockedOverlay(false);
        }
    }

    /// <summary>
    /// Hiển thị overlay khi locked
    /// </summary>
    private void ShowLockedOverlay(bool show)
    {
        // Có thể implement overlay UI để hiển thị trạng thái locked
        // Ví dụ: dim panels, show lock icons, etc.
        
        if (show)
        {
            // Làm mờ pet panel
            if (panelPet != null)
            {
                var canvasGroup = panelPet.GetComponent<CanvasGroup>() ?? panelPet.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 0.5f;
                canvasGroup.interactable = false;
            }
            
            // Làm mờ card panel
            if (panelCard != null)
            {
                var canvasGroup = panelCard.GetComponent<CanvasGroup>() ?? panelCard.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 0.5f;
                canvasGroup.interactable = false;
            }
        }
        else
        {
            // Khôi phục pet panel
            if (panelPet != null)
            {
                var canvasGroup = panelPet.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1.0f;
                    canvasGroup.interactable = true;
                }
            }
            
            // Khôi phục card panel
            if (panelCard != null)
            {
                var canvasGroup = panelCard.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1.0f;
                    canvasGroup.interactable = true;
                }
            }
        }
    }
    #endregion

    #region Photon Synchronization
    void SyncPetSelection(string petId)
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.LocalPlayer != null)
        {
            int userId = PlayerPrefs.GetInt("userId", 1);

            // Set player custom properties
            var playerProps = new ExitGames.Client.Photon.Hashtable
            {
                ["petId"] = int.Parse(petId),
                ["userId"] = userId
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);

            // Set room properties for cross-reference
            string roomPetKey = $"player_{userId}_petId";
            var roomProps = new ExitGames.Client.Photon.Hashtable
            {
                [roomPetKey] = int.Parse(petId)
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

            UpdateDebugInfo($"Synced via Photon: User {userId}, Pet {petId}");
        }
    }

    public override void OnPlayerEnteredRoom(PhotonPlayer newPlayer)
    {
        UpdateDebugInfo($"Player entered room: {newPlayer.NickName}");
        
        // Send current pet selection to new player qua RPC
        if (!string.IsNullOrEmpty(selectedPetId))
        {
            StartCoroutine(DelayedSendPetToNewPlayer(newPlayer));
        }
        
        // Sync lại tất cả current room properties cho new player
        StartCoroutine(SyncAllPetSelectionsToNewPlayer(newPlayer));
    } 

    private IEnumerator DelayedSendPetToNewPlayer(PhotonPlayer newPlayer)
    {
        // Đợi một chút để đảm bảo new player đã setup xong
        yield return new WaitForSeconds(0.5f);
        
        if (!string.IsNullOrEmpty(selectedPetId))
        {
            int userId = PlayerPrefs.GetInt("userId", 1);
            int petId = int.Parse(selectedPetId);
            
            // Send pet selection qua RPC đến new player
            photonView.RPC("OnPetSelectionChanged", newPlayer, userId, petId);
            
            UpdateDebugInfo($"Sent current pet selection to new player: User {userId}, Pet {petId}");
        }
    }

    // RPC method để receive pet selection changes
    [PunRPC]
    void OnPetSelectionChanged(int userId, int petId)
    {
        UpdateDebugInfo($"Received pet selection via RPC: User {userId}, Pet {petId}");
        
        // Update room properties với pet selection nhận được
        string roomPetKey = $"player_{userId}_petId";
        var roomProps = new ExitGames.Client.Photon.Hashtable
        {
            [roomPetKey] = petId
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
        
        // Tìm và update UI của player tương ứng
        if (lobbyPlayer != null)
        {
            // Tìm player theo userId trong room properties hoặc player list
            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (player.CustomProperties.ContainsKey("userId") && 
                    (int)player.CustomProperties["userId"] == userId)
                {
                    UpdatePlayerPetUI(player, petId);
                    break;
                }
            }
        }
    }

    private IEnumerator SyncAllPetSelectionsToNewPlayer(PhotonPlayer newPlayer)
    {
        yield return new WaitForSeconds(2f); // Đợi new player setup xong
        
        // Send tất cả pet selections hiện tại
        foreach (var prop in PhotonNetwork.CurrentRoom.CustomProperties)
        {
            string key = prop.Key.ToString();
            if (key.StartsWith("player_") && key.EndsWith("_petId"))
            {
                string userIdStr = key.Substring(7, key.Length - 13);
                if (int.TryParse(userIdStr, out int userId) && int.TryParse(prop.Value.ToString(), out int petId))
                {
                    photonView.RPC("OnPetSelectionChanged", newPlayer, userId, petId);
                    UpdateDebugInfo($"Sent pet selection to new player: User {userId} = Pet {petId}");
                }
            }
        }
    }

    public override void OnPlayerPropertiesUpdate(PhotonPlayer targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        // Check ready state changes
        if (changedProps.ContainsKey("isReady") && targetPlayer == PhotonNetwork.LocalPlayer)
        {
            bool isReady = (bool)changedProps["isReady"];
            OnReadyStateChanged(isReady);
        }

        // Chỉ xử lý khi player khác thay đổi pet (không phải local player)
        if (targetPlayer == PhotonNetwork.LocalPlayer)
        {
            UpdateDebugInfo($"Local player property update processed");
            return;
        }

        // Xử lý khi player khác thay đổi pet selection
        if (changedProps.ContainsKey("petId"))
        {
            int petId = (int)changedProps["petId"];
            UpdateDebugInfo($"Player {targetPlayer.NickName} changed pet to {petId}");

            // Update UI của player đó
            UpdatePlayerPetUI(targetPlayer, petId);

            // Sync room properties
            if (changedProps.ContainsKey("userId"))
            {
                int userId = (int)changedProps["userId"];
                SetPlayerPetInRoom(userId, petId);
            }
        }
    }

    /// <summary>
    /// Update pet UI cho player cụ thể
    /// </summary>
    private void UpdatePlayerPetUI(PhotonPlayer player, int petId)
    {
        UpdateDebugInfo($"UpdatePlayerPetUI: Player {player.NickName}, petId: {petId}");

        // Tìm UI player tương ứng và cập nhật
        if (lobbyPlayer != null)
        {
            for (int i = 0; i < lobbyPlayer.Length; i++)
            {
                if (lobbyPlayer[i].childCount > 0)
                {
                    PlayerMatch playerMatch = lobbyPlayer[i].GetComponentInChildren<PlayerMatch>();
                    if (playerMatch != null && playerMatch.PlayerActorNumber == player.ActorNumber)
                    {
                        UpdateDebugInfo($"Found PlayerMatch for player {player.ActorNumber}");
                        playerMatch.SetSelectedPet(petId);
                        break;
                    }
                }
            }
        }
    }

    private void SetPlayerPetInRoom(int userId, int petId)
    {
        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        string roomPetKey = $"player_{userId}_petId";
        var roomProps = new ExitGames.Client.Photon.Hashtable
        {
            [roomPetKey] = petId
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
        UpdateDebugInfo($"Set room property: {roomPetKey} = {petId}");
    }
    #endregion

    #region UI Panel Controls - Locked When Ready
    public void ShowPetPanel()
    {
        // Kiểm tra ready state trước khi mở panel
        if (isLocalPlayerReady)
        {
            UpdateDebugInfo("Pet panel blocked - Player is ready");
            ShowLockedMessage("Không thể thay đổi pet khi đã sẵn sàng!");
            return;
        }

        panelPet.SetActive(true);
        isRotatingPet = true;
        UpdateDebugInfo("Pet panel opened");
    }

    public void HidePetPanel()
    {
        panelPet.SetActive(false);
        isRotatingPet = false;
    }

    public void ShowCardPanel()
    {
        // Kiểm tra ready state trước khi mở panel
        if (isLocalPlayerReady)
        {
            UpdateDebugInfo("Card panel blocked - Player is ready");
            ShowLockedMessage("Không thể thay đổi thẻ khi đã sẵn sàng!");
            return;
        }

        panelCard.SetActive(true);
        isRotatingCard = true;
        UpdateDebugInfo("Card panel opened");
    }

    public void HideCardPanel()
    {
        panelCard.SetActive(false);
        isRotatingCard = false;
    }

    private void Update()
    {
        if (isRotatingPet)
        {
            btnClosePet.transform.Rotate(rotationSpeed * Time.deltaTime, 0f, 0f);
        }

        if (isRotatingCard)
        {
            btnCloseCard.transform.Rotate(rotationSpeed * Time.deltaTime, 0f, 0f);
        }
    }
    #endregion

    #region Public Getters
    public string GetSelectedPetId()
    {
        return selectedPetId;
    }

    public bool IsLocalPlayerReady()
    {
        return isLocalPlayerReady;
    }
    #endregion
}