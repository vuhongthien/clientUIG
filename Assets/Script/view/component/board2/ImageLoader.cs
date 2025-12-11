using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ImageLoader : MonoBehaviour
{
    public RawImage rawImage; // Tham chiếu tới RawImage UI
    public float jumpHeight = 1f; // Chiều cao nhảy ngắn hơn (giảm giá trị này)
    public float jumpSpeed = 0.2f; // Tốc độ nhảy chậm hơn (giảm giá trị này)

    private Vector3 initialPosition; // Vị trí ban đầu của hình ảnh
    private bool isImageLoaded = false; // Kiểm tra xem ảnh đã tải xong chưa

    private void Start()
    {
        if (rawImage != null)
        {
            initialPosition = rawImage.rectTransform.localPosition; // Lưu vị trí ban đầu
        }
    }

    private void Update()
    {
        if (isImageLoaded)
        {
            // Tạo hiệu ứng nhảy theo chiều dọc, chỉ nhảy lên với tốc độ và chiều cao nhảy ngắn hơn
            float yOffset = Mathf.Abs(Mathf.Sin(Time.time * jumpSpeed / 2)) * jumpHeight;
            rawImage.rectTransform.localPosition = initialPosition + new Vector3(0, yOffset, 0);
        }
    }

    public void StartLoadingImage(string url)
    {
        if (rawImage != null)
        {
            StartCoroutine(LoadImageFromURL(url));
        }
        else
        {
            Debug.LogError("RawImage reference is not assigned.");
        }
    }

public RawImage LoadingImage(string url, RawImage r)
{
    if (r != null)
    {
        StartCoroutine(LoadImageFromURL(url, r));
        return r;
    }
    else
    {
        Debug.LogError("RawImage reference is not assigned.");
        return null; // Handle the null case by returning null
    }
}


    public IEnumerator LoadImageFromURL(string url,RawImage r)
    {
        Debug.Log("Loading image from URL: " + url);

        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                r.texture = texture;

                // Đánh dấu rằng hình ảnh đã tải xong
                isImageLoaded = true;
            }
            else
            {
                Debug.LogError("Error loading image: " + www.error);
            }
        }
    }

    public IEnumerator LoadImageFromURL(string url)
    {
        Debug.Log("Loading image from URL: " + url);

        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                rawImage.texture = texture;

                // Đánh dấu rằng hình ảnh đã tải xong
                isImageLoaded = true;
            }
            else
            {
                Debug.LogError("Error loading image: " + www.error);
            }
        }
    }
}
