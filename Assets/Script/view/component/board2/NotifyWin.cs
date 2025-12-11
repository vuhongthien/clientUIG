using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class NotifyWin : MonoBehaviour
{
    public GameObject openThongBao;
    public GameObject tbPrefab;
    public GameObject listA;
    public GameObject itemA;
    public GameObject nameA;
    public GameObject imgA;
    public GameObject imgB;
    public Api api;
    public GameObject offBoardParent; 
        public GameObject resultBaner; 
        public GameObject enemyPet;       

    // public Board board; 
    void Start(){
        api = FindFirstObjectByType<Api>();
    }

    public void endBoard(int userId)
    {

        if (openThongBao != null)
        {
            resultBaner.SetActive(true);
            enemyPet.SetActive(false);
            GameObject thongbao = Instantiate(tbPrefab, openThongBao.transform);
            GameObject listAward = Instantiate(listA, thongbao.transform);
            thongbao.SetActive(true);
            StartCoroutine(winGame(userId,listAward));
            


        }

    }


    public IEnumerator winGame(int useId, GameObject listAward)
{
    Debug.Log("quà khi thắng----------->dang chạy" + api.keyRandom);

    yield return api.PostRequest(api.keyRandom, api.idGroupPetEnemy, api.idPetEnemy, useId);
    
    if (api.typeAward)
    {
        ResponseDataPet r = api.responseDataPet as ResponseDataPet;
        GameObject itemAward = Instantiate(itemA, listAward.transform);
        GameObject name = Instantiate(nameA, itemAward.transform);
        name.gameObject.GetComponent<TextMeshProUGUI>().text = "Pet " + r.namePet +" LV "+r.lever;
        RawImage img = Instantiate(imgB.gameObject.GetComponent<RawImage>(), itemAward.transform);
        yield return StartCoroutine(LoadImageFromUrl(r.image.FirstOrDefault().url, img));
    }
    else
    {
        List<ResponseDataAward> r = api.responseDataAward as List<ResponseDataAward>;

        foreach (var stone in r)
        {
            GameObject itemAward = Instantiate(itemA, listAward.transform);
            // Instantiate name object
            GameObject name = Instantiate(nameA, itemAward.transform);
            name.gameObject.GetComponent<TextMeshProUGUI>().text = stone.upgradeStone.name + " : " + stone.count;
            name.gameObject.GetComponent<TextMeshProUGUI>().name = stone.upgradeStone.name + " : " + stone.count;

            // Instantiate image object
            RawImage img = Instantiate(imgA.gameObject.GetComponent<RawImage>(), itemAward.transform);
            img.name = stone.count.ToString();
            Debug.Log($"-------------------->hình: {stone.upgradeStone.image.FirstOrDefault().url}, Count: {stone.count}");

            // // Load image from URL
            yield return StartCoroutine(LoadImageFromUrl(stone.upgradeStone.image.FirstOrDefault().url, img));

            Debug.Log($"-------------------->Award Stone ID: {stone.id}, Count: {stone.count}");
        }
    }
}

private IEnumerator LoadImageFromUrl(string url, RawImage rawImage)
{
    using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
    {
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            rawImage.texture = texture; // Set the downloaded texture
        }
        else
        {
            Debug.LogError($"Failed to load image from URL: {url}. Error: {request.error}");
        }
    }
}

}
