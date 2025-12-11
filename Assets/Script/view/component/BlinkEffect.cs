using System.Collections;
using UnityEngine;

public class BlinkEffect : MonoBehaviour
{
    public float fadeDuration = 0.5f;
    public float waitTime = 0.5f;
    private CanvasGroup canvasGroup;
    private Coroutine blinkCoroutine; // Lưu trữ coroutine

    void OnEnable() // Kích hoạt khi object được bật
    {
        // Đảm bảo CanvasGroup tồn tại
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Khởi động coroutine khi object active
        blinkCoroutine = StartCoroutine(BlinkEffectt());
    }

    void OnDisable() // Vô hiệu hóa khi object tắt
    {
        // Dừng coroutine nếu đang chạy
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
    }

    IEnumerator BlinkEffectt()
    {
        while (true)
        {
            yield return StartCoroutine(Fade(0)); // Ẩn dần
            yield return new WaitForSeconds(waitTime);
            yield return StartCoroutine(Fade(1)); // Hiện dần
            yield return new WaitForSeconds(waitTime);
        }
    }

    IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsedTime = 0;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }
}