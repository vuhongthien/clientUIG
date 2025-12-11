using System.Collections;
using UnityEngine;

public class ManagerLuyenThe : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject LoadingPanel;
    private void Start()
    {
        StartCoroutine(LoadSceneAfterDelay());
    }

    private IEnumerator LoadSceneAfterDelay()
    {
        ManagerGame.Instance.LoadingPanel = LoadingPanel;
        ManagerGame.Instance.ShowLoading();
        yield return new WaitForSeconds(2f);
        ManagerGame.Instance.HideLoading();
    }

        public void BackScene()
    {
        ManagerGame.Instance.BackScene();
    }
}
