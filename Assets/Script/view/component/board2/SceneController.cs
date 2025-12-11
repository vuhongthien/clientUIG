using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
public class SceneController : MonoBehaviour
{
    public LoadRoom loadRoom;
    public Api api;
    public ApiLoadRoom apiLoadRoom;

public void LoadSceneByNameStart(string sceneName)
    {
        api = FindFirstObjectByType<Api>();

    List<long> cardNumbers = apiLoadRoom.imageButtons
        .Select(card => ExtractNumberFromName(card.name)) // Extract numbers
        .Where(number => number > 0)
        .ToList();

        string listCardUserIdJson = "[" + string.Join(",", cardNumbers) + "]";

        // Bắt đầu coroutine xử lý API và load scene
        StartCoroutine(LoadSceneAfterApi(sceneName, listCardUserIdJson));
    }
    private long ExtractNumberFromName(string name)
    {
        // Use LINQ to extract only digits and convert to a number
        string numberString = new string(name.Where(char.IsDigit).ToArray());
        return long.TryParse(numberString, out long result) ? result : 0;
    }

    private IEnumerator LoadSceneAfterApi(string sceneName, string listCardUserIdJson)
    {
        // Gọi API và chờ hoàn tất
        yield return api.StartMatch(loadRoom.nguoiChoi, loadRoom.petUser, loadRoom.petEnemy, listCardUserIdJson);

        // Ẩn giao diện hiện tại nếu cần
        HideNewScene("Island");

        // Load scene mới sau khi API hoàn tất
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
    }


    public void UnloadNewScene()
    {
        // Kiểm tra nếu scene mới đã được tải
        if (SceneManager.GetSceneByName("StartMatch").isLoaded)
        {
            PlayerPrefs.SetInt("check", 1);
            PlayerPrefs.SetInt("checkG", 1);
            SceneManager.UnloadSceneAsync("StartMatch");
            ShowNewScene("Island");
        }
    }

    public void HideNewScene(string newSceneName)
    {
        Scene scene = SceneManager.GetSceneByName(newSceneName);
        if (scene.isLoaded)
        {
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                obj.SetActive(false);
            }
        }
    }

    public void ShowNewScene(string newSceneName)
    {
        Scene scene = SceneManager.GetSceneByName(newSceneName);
        if (scene.isLoaded)
        {
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                obj.SetActive(true);
            }
        }
    }


    public void LoadSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // Hàm chuyển scene theo chỉ số
    public void LoadSceneByIndex(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }

    // Hàm thoát ứng dụng
    public void QuitGame()
    {
        // Chỉ hoạt động khi build game, không hoạt động trong editor
        Application.Quit();
        Debug.Log("Game is exiting..."); // Để kiểm tra khi chạy trong editor
    }
    public void ChangeSceneDauTruong(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
