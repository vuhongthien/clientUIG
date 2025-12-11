using UnityEngine;
using System.Collections;

public class BoardController : MonoBehaviour
{
    public GameObject boardPet;
    public GameObject boardUpdate;
    public float slideDuration = 0.5f;
    public Vector3 hiddenPosition = new Vector3(0, 1000, 0); // Vị trí ngoài màn hình
    public Vector3 visiblePosition = new Vector3(0, 0, 0);   // Vị trí trên màn hình
    public GameObject btnDown;
    public GameObject boardCard;

    public void LoadBoardCard()
    {
        if (boardCard.activeSelf)
        {
            // StartCoroutine(Fade(boardCard, false));
            // StartCoroutine(Fade(btnDown, false));
            btnDown.SetActive(false);
            boardCard.SetActive(false);
        }
        else
        {
            boardCard.SetActive(true);
            // StartCoroutine(Fade(btnDown, true));
            // StartCoroutine(Fade(boardCard, true));
            btnDown.SetActive(true);
            boardCard.SetActive(true);
        }
    }

    
    public void LoadBoardUpdate()
    {
        if (boardUpdate.activeSelf)
        {
            StartCoroutine(SlideOut(boardUpdate));
        }
        else
        {
            boardUpdate.SetActive(true);
            StartCoroutine(SlideIn(boardUpdate));
        }
        
    }

    public void LoadBoard()
    {
        if (boardPet.activeSelf)
        {
            StartCoroutine(SlideOut(boardPet));
        }
        else
        {
            boardPet.SetActive(true);
            StartCoroutine(SlideIn(boardPet));
        }
    }

    public void CloseBoard()
    {
        
        if (boardCard.activeSelf)
        {
            // StartCoroutine(Fade(boardCard, false));
            // StartCoroutine(Fade(btnDown, false));
            btnDown.SetActive(false);
            boardCard.SetActive(false);
        }
        if (boardUpdate != null && boardUpdate.activeSelf)
        {
            StartCoroutine(SlideOut(boardUpdate));
        }
    }

        public void CloseUpdateBoard()
    {
        
        if (boardUpdate.activeSelf)
        {
            StartCoroutine(SlideOut(boardUpdate));
        }
    }

    private IEnumerator SlideIn(GameObject board)
    {
        float elapsed = 0;
        Vector3 startPos = hiddenPosition;
        Vector3 endPos = visiblePosition;
        RectTransform rectTransform = board.GetComponent<RectTransform>();

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            rectTransform.localPosition = Vector3.Lerp(startPos, endPos, elapsed / slideDuration);
            yield return null;
        }

        rectTransform.localPosition = endPos;
    }

    private IEnumerator SlideOut(GameObject board)
    {
        float elapsed = 0;
        Vector3 startPos = visiblePosition;
        Vector3 endPos = hiddenPosition;
        RectTransform rectTransform = board.GetComponent<RectTransform>();

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            rectTransform.localPosition = Vector3.Lerp(startPos, endPos, elapsed / slideDuration);
            yield return null;
        }

        rectTransform.localPosition = endPos;
        board.SetActive(false);
    }

    // public float fadeDuration = 2;

    // private CanvasGroup GetCanvasGroup(GameObject board)
    // {
    //     CanvasGroup canvasGroup = board.GetComponent<CanvasGroup>();
    //     if (canvasGroup == null)
    //     {
    //         canvasGroup = board.AddComponent<CanvasGroup>();
    //     }
    //     return canvasGroup;
    // }

    // public IEnumerator Fade(GameObject board, bool fadeIn)
    // {
    //     CanvasGroup canvasGroup = GetCanvasGroup(board);

    //     float elapsed = 0f;
    //     float startAlpha = fadeIn ? 0f : 1f;
    //     float endAlpha = fadeIn ? 1f : 0f;

    //     if (fadeIn)
    //     {
    //         board.SetActive(true); // Hiển thị đối tượng trước khi bắt đầu fade in
    //     }

    //     while (elapsed < fadeDuration)
    //     {
    //         elapsed += Time.deltaTime;
    //         canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / fadeDuration);
    //         yield return null;
    //     }

    //     canvasGroup.alpha = endAlpha;

    //     if (!fadeIn)
    //     {
    //         board.SetActive(false); // Ẩn đối tượng sau khi hoàn tất fade out
    //     }
    // }
}
