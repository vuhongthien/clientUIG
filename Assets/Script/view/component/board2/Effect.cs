using System.Collections;
using UnityEngine;

public class Effect : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public IEnumerator FadeAndMoveUp(GameObject target)
    {
        float duration = 0.5f;
        float timeElapsed = 0f;

        // Kích hoạt GameObject
        target.SetActive(true);

        // Lấy hoặc thêm CanvasGroup
        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = target.AddComponent<CanvasGroup>();
        }

        // Lấy RectTransform và vị trí ban đầu
        RectTransform rect = target.GetComponent<RectTransform>();
        Vector2 startPos = rect.anchoredPosition; // Vị trí hiện tại khi bắt đầu

        // Đặt alpha ban đầu
        canvasGroup.alpha = 2f;

        while (timeElapsed < duration)
        {
            float progress = timeElapsed / duration;

            // Di chuyển lên trục Y, giữ nguyên X
            rect.anchoredPosition = startPos + new Vector2(0, progress * 50f); // Bay lên 50 pixel

            // Giảm alpha để mờ dần
            canvasGroup.alpha = 1f - progress;

            timeElapsed += Time.deltaTime;
            yield return null;
        }

        // Đặt trạng thái cuối
        canvasGroup.alpha = 0f;
        rect.anchoredPosition = startPos; // Đặt lại vị trí ban đầu
        target.SetActive(false); // Ẩn GameObject
    }

    public IEnumerator FadeOut(GameObject item)
    {
        CanvasGroup canvasGroup = item.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = item.AddComponent<CanvasGroup>();
        }

        for (float alpha = 1f; alpha >= 0f; alpha -= Time.deltaTime / 0.5f) // 0.5 giây để mờ dần
        {
            canvasGroup.alpha = alpha;
            yield return null;
        }
    }

}
