using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PanelCardUserController : MonoBehaviour
{
    public Image onImageCard; // Kéo OnImageCard vào đây trong Inspector

    private void Start()
    {
        if (onImageCard != null)
            onImageCard.gameObject.SetActive(false);
    }

    // Hiện ảnh tương ứng với sprite của card
    public void ShowOnImageCard(Sprite sprite)
    {
        if (onImageCard == null) return;

        onImageCard.sprite = sprite;

        StartCoroutine(ShowEffect());
    }

    private IEnumerator ShowEffect()
    {
        if (onImageCard == null) yield break;

        GameObject go = onImageCard.gameObject;

        go.SetActive(true);

        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();

        cg.alpha = 1f;

        // Đợi 2 giây
        yield return new WaitForSeconds(2f);

        // Fade out 1 giây
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        go.SetActive(false);
    }
}
