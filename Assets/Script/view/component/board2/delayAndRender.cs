using System.Collections;
using UnityEngine;

public class delayAndRender : MonoBehaviour
{
    public GameObject offBoardParent; // Gán trong Inspector
    public GameObject onListDot;      // Gán trong Inspector

    public void CheckForStableBoardAfterFill()
    {
        StartCoroutine(DelayedRendering());
    }

    private IEnumerator DelayedRendering()
    {
        Debug.Log("delayRender is called.");
        offBoardParent.SetActive(false); // Tắt bảng
        onListDot.SetActive(true);

        Debug.Log("Waiting for 2 seconds...");
        yield return new WaitForSeconds(2f);

        Debug.Log("delayRender2 is called.");
        offBoardParent.SetActive(true); // Bật bảng
        onListDot.SetActive(false);
    }
}