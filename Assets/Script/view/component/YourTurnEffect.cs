using UnityEngine;
using System.Collections;

public class YourTurnEffect : MonoBehaviour
{
    [Header("Effect Settings")]
    [SerializeField] private float fadeInDuration = 0.1f;    // Giảm thời gian fade in
    [SerializeField] private float fadeOutDuration = 0.1f;   // Giảm thời gian fade out
    [SerializeField] private float peakDuration = 0.1f;      // Thời gian hiển thị đỉnh điểm
    [SerializeField] private float scaleAmount = 3f;       // Tăng tỷ lệ phóng to

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        
    }

    public IEnumerator PlayEffect()
    {
        // Reset trạng thái
        canvasGroup.alpha = 0f;
        rectTransform.localScale = Vector3.one;
        gameObject.SetActive(true);

        // Fade in nhanh
        float timer = 0f;
        while (timer < fadeInDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(0f, 0.5f, timer / fadeInDuration);
            rectTransform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * scaleAmount, timer / fadeInDuration);
            timer += Time.deltaTime;
            yield return null;
        }

        // Giữ ở trạng thái đỉnh trong thời gian ngắn
        canvasGroup.alpha = 1f;
        rectTransform.localScale = Vector3.one * scaleAmount;
        yield return new WaitForSeconds(peakDuration);

        // Fade out nhanh
        timer = 0f;
        while (timer < fadeOutDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(0.5f, 0f, timer / fadeOutDuration);
            rectTransform.localScale = Vector3.Lerp(Vector3.one * scaleAmount, Vector3.one, timer / fadeOutDuration);
            timer += Time.deltaTime;
            yield return null;
        }

        // Reset về trạng thái ban đầu
        canvasGroup.alpha = 0f;
        rectTransform.localScale = Vector3.one;
        gameObject.SetActive(false);
    }
}