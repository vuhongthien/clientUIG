using UnityEngine;
using UnityEngine.UI;

public class CardImageLoader : MonoBehaviour
{
    public Image imageComponent; // Đổi từ RawImage sang Image
    private bool check = true;

    void Start()
    {
        // Có thể khởi tạo hoặc đảm bảo imageComponent được gán
        if (imageComponent == null)
        {
            imageComponent = gameObject.GetComponent<Image>();
        }
    }

    void Update()
    {
        if (check)
        {
            if (!gameObject.name.Equals("Image"))
            {
                // Tải Sprite thay vì Texture
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
                check = false;
            }
        }
    }
}
