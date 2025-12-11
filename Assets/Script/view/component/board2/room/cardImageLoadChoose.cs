using UnityEngine;
using UnityEngine.UI;

public class CardImageLoadChoose : MonoBehaviour
{
    public Image imageComponent;
    private string lastName;

    void Start()
    {
        // Gán Image component nếu chưa được gán
        if (imageComponent == null)
        {
            imageComponent = gameObject.GetComponent<Image>();
        }

        // Lưu tên ban đầu
        lastName = gameObject.name;
    }

    void Update()
    {
        // Kiểm tra nếu tên đã thay đổi
        if (!gameObject.name.Equals(lastName))
        {
            UpdateImage();
            lastName = gameObject.name; // Cập nhật tên mới
        }
    }

    private void UpdateImage()
    {
        // Tải Sprite từ Resources
        Sprite loadedSprite = Resources.Load<Sprite>("card/" + gameObject.name);

        if (loadedSprite != null)
        {
            if (imageComponent != null)
            {
                imageComponent.sprite = loadedSprite; // Gán Sprite vào Image
            }
            else
            {
                Debug.LogWarning("Không tìm thấy component Image trên GameObject.");
            }
        }
        else
        {
            Debug.LogWarning("Không tìm thấy hình ảnh trong Resources.");
        }
    }
}
