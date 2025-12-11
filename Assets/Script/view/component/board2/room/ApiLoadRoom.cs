using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Linq;

public class ApiLoadRoom : MonoBehaviour
{
    // API Endpoint
    private const string apiUrl = "https://pokiwar70-production.up.railway.app/api/v1/roomWait/join";

    public LoadDataCard loadDataCard;
    public LoadDataPet loadDataPet;
    public LoadRoom loadRoom;
    public Action OnComplete { get; internal set; }
    public int check = 1;
    public List<Button> imageButtons;
    public Button selectBtn;

    // public void Start()
    // {

    //     // Start the API call
    //     int nguoiChoi = PlayerPrefs.GetInt("nguoiChoi");
    //     int enemyP = PlayerPrefs.GetInt("enemyP");
    //     Debug.Log(enemyP);
    //     StartCoroutine(CallJoinRoomApi(nguoiChoi, enemyP));
    //     Debug.Log("call start lan update room");
    //     check = 0;
    // }

    public GameObject btnDown;
    public GameObject boardCard;
    private HashSet<Button> buttonsWithEvent = new HashSet<Button>();
    public void LoadBoardCard(Button button)
    {
        if (boardCard.activeSelf)
        {
            // StartCoroutine(Fade(boardCard, false));
            // StartCoroutine(Fade(btnDown, false));
            btnDown.SetActive(false);
            boardCard.SetActive(false);
        }
        else
        {
            selectBtn = button;
            boardCard.SetActive(true);
            // StartCoroutine(Fade(btnDown, true));
            // StartCoroutine(Fade(boardCard, true));
            btnDown.SetActive(true);
            boardCard.SetActive(true);
        }
    }

    void Update()
    {
        if (gameObject.activeSelf)
        {
            check = PlayerPrefs.GetInt("check");
            if (check == 1)
            {
                foreach (var button in imageButtons)
                {
                    if (!buttonsWithEvent.Contains(button)) // Kiểm tra xem nút đã có sự kiện chưa
                    {
                        Debug.Log("Gán sự kiện cho nút: " + button.name);
                        button.onClick.AddListener(() => LoadBoardCard(button));

                        // Thêm nút vào danh sách đã gán sự kiện
                        buttonsWithEvent.Add(button);
                    }
                    else
                    {
                        Debug.Log("Nút đã có sự kiện: " + button.name);
                    }
                }
                int nguoiChoi = PlayerPrefs.GetInt("nguoiChoi");
                int enemyP = PlayerPrefs.GetInt("enemyP");
                Debug.Log(enemyP);
                StartCoroutine(CallJoinRoomApi(nguoiChoi, enemyP));
                Debug.Log("call upadte lan update room");
                PlayerPrefs.SetInt("check", 0);
            }

        }
    }

    public IEnumerator CallJoinRoomApi(int userId, int enemyPetId)
    {
        // Create the request body
        string jsonBody = JsonUtility.ToJson(new JoinRoomRequest
        {
            idUser = userId,
            idEnemyPet = enemyPetId
        });
        Debug.Log(jsonBody);

        // Create the UnityWebRequest
        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // Send the request
            yield return request.SendWebRequest();

            // Handle the response
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("API Response: " + request.downloadHandler.text);

                // Deserialize the response JSON
                JoinRoomResponse response = JsonUtility.FromJson<JoinRoomResponse>(request.downloadHandler.text);
                Debug.Log("Message: " + response.message);
                Debug.Log("Room Name: " + response.data.name);
                //load room
                loadRoom.LoadRoomData(response.data);
                //load card
                loadDataCard.LoadCard(response.data.listChooseCard);
                //load list pet
                loadDataPet.LoadPet(response.data.listChoosePet);

            }
            else
            {
                Debug.LogError($"API Error: {request.error}");
            }
        }
    }
}

// Request Body Class
[System.Serializable]
public class JoinRoomRequest
{
    public int idUser;
    public int idEnemyPet;
}

// Response Class
[System.Serializable]
public class JoinRoomResponse
{
    public string timestamp;
    public int status;
    public bool success;
    public string message;
    public JoinRoomData data;
}

[System.Serializable]
public class JoinRoomData
{
    public int id;
    public string name;
    public string user;
    public int gold;
    public int money;
    public int idPet;
    public int energy;
    public int energyFull;
    public int lever;
    public int countPass;
    public int idPetUser;
    public string thumbnailPetUser;
    public int playerId;
    public ImageData[] imageUser;
    public string namePetEnemy;
    public ImageData[] imageEnemyPet;
    public EnemyPetRoom enemyPet;
    public ChooseCard[] listChooseCard;
    public ChoosePet[] listChoosePet;
}

[System.Serializable]
public class ImageData
{
    public int id;
    public string url;
}

[System.Serializable]
public class EnemyPetRoom
{
    public int id;
    public int idPet;
    public int idGroupPet;
    public int attack;
    public int mana;
    public int blood;
    public int lever;
    public bool checkNew;
    public string dameTypePet;
    public int requestPass;
    public int parentId;

}

[System.Serializable]
public class ChooseCard
{
    public int id;
    public int idUser;
    public int idCard;
    public int lever;
    public int count;
    public int value;
    public string createAt;
    public string name;
    public string thumbnail;
    public string imageCard;
}

[System.Serializable]
public class ChoosePet
{
    public int id;
    public int idAward;
    public int idUser;
    public int idPet;
    public string namePet;
    public string thumbnailPet;
    public int attack;
    public int mana;
    public int blood;
    public int lever;
    public string createAt;
    public string createBy;
    public string image;
}
