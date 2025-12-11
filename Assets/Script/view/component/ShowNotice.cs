using UnityEngine;
using UnityEngine.UI;

public class ShowNotice : MonoBehaviour
{
    public GameObject notice; // Kéo thả GameObject Notice vào đây trong Inspector
    public Button showButton; // Kéo thả Button vào đây trong Inspector
    public Button cancleNotice;
    void Start()
    {
        if (showButton != null)
        {
            showButton.onClick.AddListener(ToggleNotice);
            cancleNotice.onClick.AddListener(ToggleNotice);
        }
    }

    void ToggleNotice()
    {
        if (notice != null)
        {
            notice.SetActive(!notice.activeSelf); // Bật/tắt GameObject
        }
    }
}
