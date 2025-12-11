using UnityEngine;

using System;
using System.Collections;
using UnityEngine.Networking;

public class APIManager : MonoBehaviour
{
    private static APIManager _instance;
    public static APIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject obj = new GameObject("APIManager");
                _instance = obj.AddComponent<APIManager>();
                DontDestroyOnLoad(obj);
            }
            return _instance;
        }
    }

    public IEnumerator GetRequest<T>(string url, Action<T> onSuccess, Action<string> onError)
    {
        Debug.Log($"[APIManager] → GET Request: {url}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            Debug.Log($"[APIManager] Response Code: {request.responseCode}");
            Debug.Log($"[APIManager] Result: {request.result}");

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string rawResponse = request.downloadHandler.text;
                    Debug.Log($"[APIManager] ✓ Raw Response: {rawResponse}");

                    // Kiểm tra response có rỗng không
                    if (string.IsNullOrEmpty(rawResponse))
                    {
                        string error = "Empty response from server";
                        Debug.LogError($"[APIManager] ❌ {error}");
                        onError?.Invoke(error);
                        yield break;
                    }

                    // Kiểm tra response có phải là array không
                    bool isArray = rawResponse.TrimStart().StartsWith("[");

                    string jsonResponse;
                    if (isArray)
                    {
                        // Nếu là array, wrap thành object
                        jsonResponse = "{\"data\":" + rawResponse + "}";
                        Debug.Log($"[APIManager] Array detected, wrapped: {jsonResponse}");
                    }
                    else if (rawResponse.TrimStart().StartsWith("{"))
                    {
                        // Nếu đã là object, kiểm tra có "data" field chưa
                        if (rawResponse.Contains("\"data\""))
                        {
                            // Đã có data field, dùng luôn
                            jsonResponse = rawResponse;
                            Debug.Log($"[APIManager] Object with 'data' field detected");
                        }
                        else
                        {
                            // Chưa có data field, wrap lại
                            jsonResponse = "{\"data\":" + rawResponse + "}";
                            Debug.Log($"[APIManager] Object without 'data' field, wrapped");
                        }
                    }
                    else
                    {
                        // Không phải JSON hợp lệ
                        string error = $"Invalid JSON format: {rawResponse}";
                        Debug.LogError($"[APIManager] ❌ {error}");
                        onError?.Invoke(error);
                        yield break;
                    }

                    Debug.Log($"[APIManager] Parsing: {jsonResponse}");

                    ResponseWrapper<T> wrappedResponse = JsonUtility.FromJson<ResponseWrapper<T>>(jsonResponse);

                    if (wrappedResponse == null)
                    {
                        string error = "Failed to parse ResponseWrapper";
                        Debug.LogError($"[APIManager] ❌ {error}");
                        onError?.Invoke(error);
                        yield break;
                    }

                    if (wrappedResponse.data == null)
                    {
                        string error = "Response data is null after parsing";
                        Debug.LogError($"[APIManager] ❌ {error}");
                        onError?.Invoke(error);
                        yield break;
                    }

                    Debug.Log($"[APIManager] ✓✓ Parse SUCCESS! Type: {typeof(T).Name}");
                    onSuccess?.Invoke(wrappedResponse.data);
                }
                catch (Exception e)
                {
                    string error = $"JSON Parse Error: {e.Message}\nStackTrace: {e.StackTrace}";
                    Debug.LogError($"[APIManager] ❌ Exception: {error}");
                    Debug.LogError($"[APIManager] Raw response was: {request.downloadHandler.text}");
                    onError?.Invoke(error);
                }
            }
            else
            {
                string error = $"Request Failed: {request.error} | Response Code: {request.responseCode}";
                string responseText = request.downloadHandler?.text;

                if (!string.IsNullOrEmpty(responseText))
                {
                    error += $"\nResponse: {responseText}";
                }

                Debug.LogError($"[APIManager] ❌ {error}");
                onError?.Invoke(error);
            }
        }
    }

    public IEnumerator PostRequestRaw(string url, object data, Action<string> onSuccess, Action<string> onError)
    {
        string jsonData = "";
        if (data != null)
        {
            jsonData = JsonUtility.ToJson(data);
        }

        Debug.Log($"[API POST RAW] URL: {url}");
        Debug.Log($"[API POST RAW] Data: {jsonData}");

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            if (!string.IsNullOrEmpty(jsonData))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            }

            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"[API POST RAW] Success: {responseText}");
                onSuccess?.Invoke(responseText);
            }
            else
            {
                string error = request.error;
                Debug.LogError($"[API POST RAW] Error: {error}");
                Debug.LogError($"[API POST RAW] Response: {request.downloadHandler.text}");
                onError?.Invoke(request.downloadHandler.text);

            }
        }
    }

    public IEnumerator PostRequest<T>(string url, object body, Action<T> onSuccess, Action<string> onError)
    {
        string jsonData = JsonUtility.ToJson(body);
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);

        // ✅ LOG REQUEST
        Debug.Log($"[APIManager] 📤 POST Request:");
        Debug.Log($"[APIManager]    URL: {url}");
        Debug.Log($"[APIManager]    Payload: {jsonData}");

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            // ✅ LOG RESPONSE
            Debug.Log($"[APIManager] 📥 POST Response:");
            Debug.Log($"[APIManager]    Status Code: {request.responseCode}");
            Debug.Log($"[APIManager]    Result: {request.result}");

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string rawResponse = request.downloadHandler.text;
                    Debug.Log($"[APIManager]    Raw Response: {rawResponse}");

                    // Handle string response directly
                    if (typeof(T) == typeof(string))
                    {
                        onSuccess?.Invoke((T)(object)rawResponse);
                        yield break;
                    }

                    // Wrap response for JSON parsing
                    string jsonResponse = "{\"data\":" + rawResponse + "}";
                    ResponseWrapper<T> wrappedResponse = JsonUtility.FromJson<ResponseWrapper<T>>(jsonResponse);

                    Debug.Log($"[APIManager] ✓ POST Success!");
                    onSuccess?.Invoke(wrappedResponse.data);
                }
                catch (Exception e)
                {
                    string error = $"JSON Parse Error: {e.Message}";
                    Debug.LogError($"[APIManager] ❌ {error}");
                    Debug.LogError($"[APIManager]    Raw response: {request.downloadHandler.text}");
                    onError?.Invoke(error);
                }
            }
            else
            {
                string error = $"POST Failed: {request.error}";
                string responseBody = request.downloadHandler?.text;

                if (!string.IsNullOrEmpty(responseBody))
                {
                    error += $"\nResponse Body: {responseBody}";
                }

                Debug.LogError($"[APIManager] ❌ {error}");
                onError?.Invoke(error);
            }
        }
    }

    public IEnumerator PostRequest_Generic<T>(string url, object data,
    Action<T> onSuccess, Action<string> onError)
    {
        string jsonData = JsonUtility.ToJson(data); // ← APIManager converts to JSON

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;

                // Handle string response type
                if (typeof(T) == typeof(string))
                {
                    onSuccess?.Invoke((T)(object)responseText);
                }
                else
                {
                    T response = JsonUtility.FromJson<T>(responseText);
                    onSuccess?.Invoke(response);
                }
            }
            else
            {
                string errorMsg = $"POST Failed: {request.error}";
                string errorBody = request.downloadHandler?.text;
                if (!string.IsNullOrEmpty(errorBody))
                {
                    errorMsg += $" | Response: {errorBody}";
                }
                onError?.Invoke(errorMsg);
            }
        }
    }


    [Serializable]
    private class ResponseWrapper<T>
    {
        public T data;
    }
}

