using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class ManagerDauTruong : MonoBehaviourPunCallbacks
{
    public GameObject LoadingPanel;
    public Text txtVang;
    public Text txtCt;
    public Text txtNl; // Giữ lại để fallback
    
    public string txtLvUser;
    public string txtName;
    public string petId;
    public static ManagerDauTruong Instance;

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
        StartCoroutine(LoadSceneAfterDelay());
    }

    private IEnumerator LoadSceneAfterDelay()
    {
        int userId = PlayerPrefs.GetInt("userId", 0);
        Debug.Log("userId: " + userId);
        
        // ✅ SỬA: Gọi energy API trước
        // yield return StartCoroutine(LoadEnergyInfo(userId));
        
        yield return APIManager.Instance.GetRequest<UserDTO>(
            APIConfig.GET_USER(userId), 
            OnUserReceived, 
            OnError
        );
        
        yield return StartCoroutine(Login(userId));
    }

    // ✅ THÊM: Load energy info
    // private IEnumerator LoadEnergyInfo(int userId)
    // {
    //     string url = APIConfig.GET_ENERGY(userId);
        
    //     var request = APIManager.Instance.GetRequest<EnergyInfoDTO>(
    //         url,
    //         (energyInfo) => {
    //             if (energyManager != null)
    //             {
    //                 energyManager.InitializeEnergy(
    //                     energyInfo.currentEnergy,
    //                     energyInfo.maxEnergy,
    //                     energyInfo.secondsUntilNextRegen
    //                 );
    //             }
    //             else if (txtNl != null)
    //             {
    //                 // Fallback
    //                 txtNl.text = $"{energyInfo.currentEnergy}/{energyInfo.maxEnergy}";
    //             }
                
    //             Debug.Log($"✓ Energy loaded: {energyInfo.currentEnergy}/{energyInfo.maxEnergy}");
    //         },
    //         (error) => {
    //             Debug.LogError($"Lỗi load energy: {error}");
    //         }
    //     );
        
    //     yield return request;
    // }

    void OnUserReceived(UserDTO user)
    {
        // Vẫn update UI khác
        txtVang.text = user.gold.ToString();
        txtCt.text = user.requestAttack.ToString();
        txtLvUser = "Lv" + user.lever.ToString();
        txtName = user.name;
        petId = user.petId.ToString();
        Debug.Log("petId: " + petId);
    }

    public IEnumerator Login(int userId)
    {
        Debug.Log(transform.name + ": Login " + userId);
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.LocalPlayer.NickName = userId.ToString();

        PhotonNetwork.ConnectUsingSettings();

        while (!PhotonNetwork.IsConnected)
        {
            yield return null;
        }

        bool isConnected = false;
        PhotonNetwork.AddCallbackTarget(new ConnectionCallback(() => isConnected = true));
        yield return new WaitUntil(() => isConnected);
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("OnConnectedToMaster");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("OnJoinedLobby");
    }

    public void BackScene()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
            Debug.Log("Disconnect");
        }

        ManagerGame.Instance.LoadScene("QuangTruong");
    }

    void OnError(string error)
    {
        Debug.LogError("API Error: " + error);
    }
}

public class ConnectionCallback : MonoBehaviourPunCallbacks
{
    private System.Action onConnectedCallback;

    public ConnectionCallback(System.Action callback)
    {
        onConnectedCallback = callback;
    }

    public override void OnConnectedToMaster()
    {
        onConnectedCallback?.Invoke();
    }

    private void OnDestroy()
{
    // Hủy tất cả LeanTween tweens
    LeanTween.cancelAll();
    
    // Hoặc chỉ hủy tweens của GameObject này
    // LeanTween.cancel(gameObject);
}
}