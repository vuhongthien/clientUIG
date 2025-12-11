using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIEffect : MonoBehaviour
{
    public Text uiText; // Gắn đối tượng Text cần hiệu ứng
    public float displayDuration = 0.5f; // Thời gian hiển thị
    public float fadeDuration = 0.5f; // Thời gian mờ dần
    public float moveUpDistance = 50f; // Khoảng cách bay lên

    public void ShowWithEffect(string text)
    {
        // Gắn text mới
        uiText.text = text;

        // Kích hoạt và chạy hiệu ứng
        uiText.gameObject.SetActive(true);
        StartCoroutine(DisplayAndFade());
    }

    private IEnumerator DisplayAndFade()
    {
        // Chờ trong 0,5 giây
        yield return new WaitForSeconds(displayDuration);

        // Bắt đầu hiệu ứng mờ dần và bay lên
        Vector3 startPos = uiText.rectTransform.anchoredPosition;
        Vector3 targetPos = startPos + Vector3.up * moveUpDistance;
        Color startColor = uiText.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0);

        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;

            // Di chuyển text lên
            uiText.rectTransform.anchoredPosition = Vector3.Lerp(startPos, targetPos, elapsedTime / fadeDuration);

            // Giảm độ trong suốt
            uiText.color = Color.Lerp(startColor, targetColor, elapsedTime / fadeDuration);

            yield return null;
        }

        // Đảm bảo text bị ẩn và thiết lập lại vị trí ban đầu
        uiText.gameObject.SetActive(false);
        uiText.rectTransform.anchoredPosition = startPos;
        uiText.color = startColor;
    }
}
