// using System.Collections;
// using Photon.Pun;
// using TMPro;
// using UnityEngine;


// public class PhotonGetUserName : MonoBehaviourPunCallbacks
// {
//     public static PhotonGetUserName Instance;
//     private void Awake()
//     {
//         if (Instance == null)
//         {
//             Instance = this;
//             // DontDestroyOnLoad(gameObject);
//         }
//         else
//         {
//             Destroy(gameObject);
//         }
//     }
// public IEnumerator Login(string userName)
// {
//     Debug.Log(transform.name + ": Login " + userName);
//     PhotonNetwork.AutomaticallySyncScene = true;
//     PhotonNetwork.LocalPlayer.NickName = userName;

//     // Bắt đầu kết nối Photon
//     PhotonNetwork.ConnectUsingSettings();

//     // Chờ đến khi kết nối thành công (hoặc có lỗi)
//     while (!PhotonNetwork.IsConnected)
//     {
//         yield return null; // Chờ mỗi frame
//     }

//     // Hoặc chờ sự kiện OnConnectedToMaster
//     bool isConnected = false;
//     PhotonNetwork.AddCallbackTarget(new ConnectionCallback(() => isConnected = true));
//     yield return new WaitUntil(() => isConnected);
// }

//     public override void OnConnectedToMaster()
//     {
//         Debug.Log("OnConnectedToMaster");
//         PhotonNetwork.JoinLobby();
//     }

//     public override void OnJoinedLobby()
//     {
//         Debug.Log("OnJoinedLobby");
//     }
// }
// public class ConnectionCallback : MonoBehaviourPunCallbacks
// {
//     private System.Action onConnectedCallback;

//     public ConnectionCallback(System.Action callback)
//     {
//         onConnectedCallback = callback;
//     }

//     public override void OnConnectedToMaster()
//     {
//         onConnectedCallback?.Invoke();
//     }
// }