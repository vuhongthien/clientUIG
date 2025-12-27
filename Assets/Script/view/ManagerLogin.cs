using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ManagerLogin : MonoBehaviour
{
    public InputField usernameInput; // Đổi từ Text sang InputField
    public InputField passwordInput;
    public Button loginBtn;
    public GameObject LoadingPanel;
    public Text errorText;

    private void Start()
    {
        loginBtn.onClick.AddListener(Login); // Gắn sự kiện click nút
    }

    private void Login()
    {
        StartCoroutine(LoginCoroutine());
    }

    IEnumerator LoginCoroutine()
{
    LoginRequest loginData = new LoginRequest
    {
        user = usernameInput.text,
        password = passwordInput.text,
        version = APIConfig.VERSION
    };

    string jsonData = JsonUtility.ToJson(loginData);
    using (UnityWebRequest request = new UnityWebRequest(APIConfig.POST_USER_LOGIN, "POST"))
    {
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        LoadingPanel.SetActive(true);
        errorText.gameObject.SetActive(false);

        yield return request.SendWebRequest();

        LoadingPanel.SetActive(false);

        if (request.result == UnityWebRequest.Result.Success)
        {
            UserDTO data = JsonUtility.FromJson<UserDTO>(request.downloadHandler.text);
            PlayerPrefs.SetInt("userId", data.id);
            PlayerPrefs.Save();
            SceneManager.LoadScene("QuangTruong");
        }
        else
        {
            string errorMessage = "Đã xảy ra lỗi. Vui lòng thử lại!";
            
            // Parse error từ response
            string responseText = request.downloadHandler.text;
            if (responseText.Contains("VERSION_EXPIRED"))
            {
                errorMessage = "Phiên bản đã hết hạn. Vui lòng cập nhật!";
            }
            else if (responseText.Contains("INVALID_CREDENTIALS"))
            {
                errorMessage = "Tài khoản hoặc mật khẩu không chính xác";
            }
            
            errorText.text = errorMessage;
            errorText.gameObject.SetActive(true);
        }
    }
}
}
[System.Serializable]
public class LoginRequest
{
    public string user;
    public string password;

    public string version;
}
