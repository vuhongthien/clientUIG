using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ManagerGame : MonoBehaviour
{
    public static ManagerGame Instance;
    public GameObject LoadingPanel;
    public static Stack<string> sceneHistory = new Stack<string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        StartCoroutine(LoadSceneAfterDelay());
    }

    private IEnumerator LoadSceneAfterDelay()
    {
        ShowLoading();
        yield return new WaitForSeconds(2f);
        HideLoading();
    }


    public void LoadScene(string nameScene)
{
    int userId = PlayerPrefs.GetInt("userId", 0);
    PlayerPrefs.SetInt("userId", userId);
    
    LeanTween.cancelAll();
    LeanTween.reset();
    
    // LƯU TRẠNG THÁI TRƯỚC KHI CHUYỂN SCENE
    string currentScene = SceneManager.GetActiveScene().name;
    
    // ✅ CHỈ PUSH scene nếu KHÔNG phải Match (để khi back về thì vẫn còn history)
    if (nameScene != "Match" && (sceneHistory.Count == 0 || sceneHistory.Peek() != currentScene))
    {
        sceneHistory.Push(currentScene);
    }
    
    // ✅ LƯU TRẠNG THÁI PANEL NẾU ĐANG Ở QUANGTRUONG
    if (currentScene == "QuangTruong")
    {
        // Lưu trạng thái Room Panel đang mở
        PlayerPrefs.SetInt("RoomPanelWasOpen", 1);
        PlayerPrefs.Save();
    }

    SceneManager.LoadScene(nameScene);
}

    public void BackScene()
    {
        LeanTween.cancelAll();
    LeanTween.reset();
        if (sceneHistory.Count > 0)
        {
            string previousScene = sceneHistory.Pop();
            SceneManager.LoadScene(previousScene);
        }
        else
        {
            Debug.LogWarning("Không có scene trước đó để quay lại!");
        }
    }

    public void RefreshCurrentUserInfo()
    {
        // Kiểm tra scene hiện tại và gọi refresh tương ứng
        if (ManagerQuangTruong.Instance != null)
        {
            ManagerQuangTruong.Instance.RefreshUserInfo();
        }
        // Có thể thêm cho các scene khác nếu cần
    }


    public void ShowLoading()
    {
        LoadingPanel.SetActive(true);
    }

    public void HideLoading()
    {
        LoadingPanel.SetActive(false);
    }
}
