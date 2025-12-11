using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
public class Api : MonoBehaviour
{
    public const string HOST = "https://pokiwar70-production.up.railway.app/api/v1";
    public const string UPDATEENERGY = "/userPlayer/updateEnergy";
    public const string START = "/match/start";
    public const string RANDOMKEY = "/award/gen?idUser=";

    public const string GIVEAWARD = "/award/give";

    private Active active;
    public string keyRandom;
    public int idGroupPetEnemy;
    public int idPetEnemy;
    public bool typeAward;
    public ResponseDataPet responseDataPet;
    public List<ResponseDataAward> responseDataAward;
    public PetAnimationLoader petAnimationLoader;


    void Start()
    {
        active = FindFirstObjectByType<Active>();
        petAnimationLoader = FindFirstObjectByType<PetAnimationLoader>();
    }

    
    // api random key
    public IEnumerator GetRequest(int idUser)
    
    {
        string url = HOST + RANDOMKEY + idUser;
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            // Gửi yêu cầu GET
            yield return webRequest.SendWebRequest();

            // Kiểm tra kết quả phản hồi
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Response: " + webRequest.downloadHandler.text);
                APIResponseRandomKey response = JsonUtility.FromJson<APIResponseRandomKey>(webRequest.downloadHandler.text);
                
                keyRandom = response.data;
            }
            else
            {
                Debug.LogError("Error: " + webRequest.error);
            }
        }
    }

    public IEnumerator PostRequest(string codeV, int idGroupPetV, int idPetV, int idUserV)
    {

        string url = HOST + GIVEAWARD;
        string jsonBody = $"{{\"code\": \"{codeV}\", \"idGroupPet\": {idGroupPetV}, \"idPet\": {idPetV}, \"idUser\": {idUserV}}}";
        Debug.Log(jsonBody);
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.SetRequestHeader("Content-Type", "application/json");
        request.downloadHandler = new DownloadHandlerBuffer();
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("-----------------------cahy api thang");

            var response = JsonUtility.FromJson<ApiResponseAward<ResponseDataPet>>(request.downloadHandler.text);
            if (!response.message.Equals("Nhận đá"))
            {


                responseDataPet = response.data;
                typeAward = true;
                Debug.Log("-----------------------pet"+response.message);
            }
            else
            {
                Debug.Log("-----------------------đá"+response.message);
                typeAward = false;
                var stoneResponse = JsonUtility.FromJson<ApiResponseAward<List<ResponseDataAward>>>(request.downloadHandler.text);
                    responseDataAward = stoneResponse.data;
                
            }
        }
        else
        {
            Debug.LogError("Error: " + request.error);
        }
    }



    // goi api update nangluong
    public IEnumerator UpdateEnergyAPI(int idUser, int type)
    {
        string url = HOST + UPDATEENERGY;
        string jsonBody = "{\"idUser\":" + idUser + ", \"type\":" + type + "}";

        UnityWebRequest request = UnityWebRequest.PostWwwForm(url, jsonBody);
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonBody));
        request.SetRequestHeader("Content-Type", "application/json");

        // Gửi yêu cầu và chờ phản hồi
        yield return request.SendWebRequest();

        // Kiểm tra lỗi
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("API Call Failed: " + request.error);
        }
        else
        {
            // Xử lý phản hồi
            ApiResponseEnegy response = JsonUtility.FromJson<ApiResponseEnegy>(request.downloadHandler.text);
            if (response == null)
            {
                Debug.LogError("Failed to deserialize jsonResponse into ApiResponse.");

            }
            active.nangLuong.text = response.data.ToString();
            Debug.Log("API Response: " + request.downloadHandler.text);
        }
    }

    //api bắt đầu
    public IEnumerator StartMatch(int idUser, int idPetUser, int idEnemyPet, string listCardUserId)
    {
        string url = HOST + START;

        string jsonBody = "{\"idUser\":" + idUser +
                          ", \"idEnemyPet\":" + idEnemyPet +
                          ", \"idPetUser\":" + idPetUser +
                          ", \"listCardUserId\":" + listCardUserId + "}";

        Debug.Log(jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // Gửi yêu cầu
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("API Response: " + request.downloadHandler.text);
                // HandleApiResponse(request.downloadHandler.text);
                PlayerPrefs.SetString("startG", request.downloadHandler.text);

            }
            else
            {
                Debug.LogError("Error: " + request.error);
            }
        }
    }
    // xử lý data tả về
    public void HandleApiResponse(string jsonResponse)
    {
        if (string.IsNullOrEmpty(jsonResponse))
        {
            Debug.LogError("jsonResponse is null or empty.");
            return;
        }

        ApiResponse response = JsonUtility.FromJson<ApiResponse>(jsonResponse);
        if (response == null)
        {
            Debug.LogError("Failed to deserialize jsonResponse into ApiResponse.");
            return;
        }

        if (response.data == null || response.data.enemyPet == null)
        {
            Debug.LogError("response.data or response.data.enemyPet is null.");
            return;
        }

        // Gán dữ liệu cơ bản từ API
        idGroupPetEnemy = response.data.enemyPet.idGroupPet;
        // active.useId = response.data.id;
        idPetEnemy = response.data.enemyPet.idPet;
        // Dữ liệu người chơi


        // Dữ liệu địch


        // Bắt đầu tải tất cả hình ảnh
        StartCoroutine(LoadAllImages(response));
    }
    // xử lý hình
    IEnumerator LoadAllImages(ApiResponse response)
    {
        List<string> imageUrls = new List<string>();

        // Collect all image URLs
        if (response.data.imagePet != null)
            imageUrls.AddRange(response.data.imagePet.Select(img => img.url));

        if (response.data.imageEnemyPet != null)
            imageUrls.AddRange(response.data.imageEnemyPet.Select(img => img.url));

        if (response.data.imageTypePet != null)
            imageUrls.Add(response.data.imageTypePet.FirstOrDefault()?.url);

        if (response.data.imageTypeEnemyPet != null)
            imageUrls.Add(response.data.imageTypeEnemyPet.FirstOrDefault()?.url);

        List<Texture2D> textures = new List<Texture2D>();

        // Load all images concurrently
        foreach (string url in imageUrls)
        {
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                textures.Add(DownloadHandlerTexture.GetContent(request));
            }
            else
            {
                Debug.LogError("Failed to load image: " + url + " Error: " + request.error);
            }
        }

        // Once all images are loaded, update the UI
        if (textures.Count == imageUrls.Count)
        {
            // Assign textures to UI components
            active.animationPet.SetActive(true);
            // active.playerPetAnimator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>($"Animations/act{idPetUser}/amtDung");
            active.animationBoss.SetActive(true);
            // active.BossPetAnimator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>($"Animations/act{idPetEnemy}/amtDungpetb");
            // active.enemyPet.StartLoadingImage(response.data.imageEnemyPet.FirstOrDefault().url);
            active.maxNo = 200;
            active.ManaPlayer = 0;
            active.NoPlayer = 0;
            active.maxNoNPC = 200;
            active.ManaNPC = 0;
            active.NoNPC = 0;
            active.typePetUser.StartLoadingImage(response.data.imageTypePet.FirstOrDefault().url);
            active.typePetEnemy.StartLoadingImage(response.data.imageTypeEnemyPet.FirstOrDefault().url);
            active.nangLuong.text = response.data.energy.ToString();
            active.leverPetUser.text = "LV " + response.data.lever.ToString();
            active.leverEnemyPet.text = "LV " + response.data.enemyPet.lever.ToString();
            active.typePetUser.StartLoadingImage(response.data.imageTypePet.FirstOrDefault().url);
            active.typePetEnemy.StartLoadingImage(response.data.imageTypeEnemyPet.FirstOrDefault().url);
            active.namePetUser.text = response.data.user;
            active.namePetEnemy.text = response.data.namePetEnemy;
            active.dameTypePetUse.text = "+ " + response.data.dameTypePet;
            active.dameTypePetEnemy.text = "+ " + response.data.enemyPet.dameTypePet;
            active.animationBoss.name = response.data.enemyPet.parentId.ToString();
            active.animationPet.name = response.data.idPet.ToString();
            active.UpdateSlider();
            for (int i = 0; i < response.data.listCard.Length; i++)
            {
                CardDetail card = response.data.listCard[i];
                active.cardInfos.Add(new CardInfo(
                    card.idCard,
                    card.id.ToString(),
                    "noname",
                    card.value,
                    card.imageCard.FirstOrDefault().url,
                    card.lever,
                    card.conditionUse
                ));
            }
            active.listCard.SetCardInfos(active.cardInfos);
            StartCoroutine(GetRequest(response.data.id));
            Debug.Log("All images loaded and UI updated.");
        }
        else
        {
            Debug.LogError("Some images failed to load.");
        }

    }



}

[System.Serializable]
public class ResponseDataPet
{
    public int id;
    public int idAward;
    public int idUser;
    public int idPet;
    public string namePet;
    public int attack;
    public int mana;
    public int blood;
    public int lever;
    public string createAt;
    public string createBy;
    public bool checkNew;
    public ImageAPI[] image;
}

[System.Serializable]
public class ApiResponseAward<T>
{
    public string timestamp;
    public int status;
    public bool success;
    public string message;
    public T data;
}

[System.Serializable]
public class ResponseDataAward
{
    public int id;
    public int count;
    public int idGroupPet;
    public int idUpgradeStone;
    public int idPet;
    public UpgradeStone upgradeStone;
    public object pet;
}

[System.Serializable]
public class UpgradeStone
{
    public int id;
    public int idTypePet;
    public string name;
    public string description;
    public int lever;
    public ImageAPI[] image;
}

[System.Serializable]
public class APIResponseRandomKey
{
    public string timestamp;
    public int status;
    public bool success;
    public string message;
    public string data;
}


[System.Serializable]
public class ApiResponse
{
    public string timestamp;
    public int status;
    public bool success;
    public string message;
    public Data data;
}

[System.Serializable]
public class Data
{
    public int id;
    public string name;
    public string password;
    public string user;
    public string namePetEnemy;
    public string dameTypePet;
    public int energy;
    public int lever;
    public int attack;
    public int mana;
    public int blood;
    public int idPet;
    public string namePet;
    public ImageAPI[] imageTypePet;
    public ImageAPI[] imageTypeEnemyPet;
    public ImageAPI[] imagePet;
    public ImageAPI[] imageUser;
    public CardDetail[] listCard;
    public ImageAPI[] imageEnemyPet;
    public EnemyPet enemyPet;
}

[System.Serializable]
public class CardDetail
{
    public int id;
    public int idUser;
    public int idCard;
    public int lever;
    public int conditionUse;
    public int value;
    public int count;
    public ImageAPI[] imageCard;
}

[System.Serializable]
public class ImageAPI
{
    public int id;
    public string url;
}

[System.Serializable]
public class EnemyPet
{
    public int id;
    public int idPet;
    public int idGroupPet;
    public int parentId;
    public int attack;
    public int mana;
    public int blood;
    public int lever;
    public bool checkNew;
    public string dameTypePet;
}

[System.Serializable]
public class ApiResponseEnegy
{
    public string timestamp;
    public int status;
    public bool success;
    public string message;
    public int data;
}
