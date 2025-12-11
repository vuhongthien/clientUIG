using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class SkillPetEspect : MonoBehaviour
{
    public GameObject openBoard;
    public GameObject closeCardEspect;
    public GameObject nutPrefab;
    public Transform parentTransform;
    public Slider timeCombo;
    public int nutCount = 7;
    public float spacing = 55.0f;
    public float scaleFactor = 50.0f;

    private int dem = 0; // Biến đếm
    private List<GameObject> nutObjects = new List<GameObject>(); // Danh sách các nut đã tạo
    private List<string> nutNames = new List<string>(); // Danh sách tên các nut
    private string[] keyBindings; // Mảng chứa các phím tương ứng với các nut
    private Sprite[] nutSpriteComplete; // Sprite từ thư mục DotSkillComple

    void Start()
    {
        // Tải tất cả các sprite trong thư mục DotSkillComple.
        nutSpriteComplete = Resources.LoadAll<Sprite>("DotSkillComple");
        if (nutSpriteComplete == null || nutSpriteComplete.Length < 0)
        {
            Debug.LogError("Không tìm thấy sprite nào trong thư mục 'DotSkillComple'.");
            return;
        }

        CreateNuts();
        // Bắt đầu chạy slider với hiệu ứng scrolling trong 3 giây.
        StartCoroutine(ScrollSlider(3.0f));
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            HandleKeyPress("nutUp_" + dem);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            HandleKeyPress("nutDown_" + dem);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            HandleKeyPress("nutLeft_" + dem);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            HandleKeyPress("nutRight_" + dem);
        }
    }

    void HandleKeyPress(string key)
    {
        // Kiểm tra xem phím nhấn có đúng theo thứ tự không.
        if (key == keyBindings[dem])
        {
            // Nhấn đúng, cập nhật hình ảnh nut tương ứng.

            UpdateNutSprite(dem);
            dem++;
             // Tăng biến đếm.
            Debug.Log($"Đúng! Đã thay đổi nut tại vị trí: {dem - 1}");
        }
        else
        {
            dem = 0; // Nhấn sai, reset dem về 0.
            Debug.Log("Sai! Dem đã được reset.");

        }

        // Hiển thị giá trị hiện tại của dem trong Debug.Log.
        Debug.Log($"Đang ở vị trí: {dem}, Phím nhấn: {key}");
    }

    void UpdateNutSprite(int index)
    {
        if (index < 0 || index >= nutObjects.Count)
        {
            Debug.LogError("Index vượt ngoài phạm vi!");
            return;
        }

        GameObject nut = nutObjects[index];
        SpriteRenderer spriteRenderer = nut.GetComponent<SpriteRenderer>();

        // Tên sprite cần tìm
        string targetSpriteName = nut.name.Contains("_" + index)
            ? nut.name.Replace("_" + index, "") // Nếu có hậu tố _index
            : nut.name;                        // Nếu không có hậu tố

        Debug.Log($"Tên sprite cần tìm: {targetSpriteName}");

        // Tìm sprite trong thư mục 'DotSkillComple' với tên đã chỉnh sửa (thêm _0)
        Sprite newSprite = System.Array.Find(nutSpriteComplete, sprite => sprite.name == targetSpriteName);

        // Kiểm tra nếu tìm thấy sprite
        if (newSprite != null)
        {
            spriteRenderer.sprite = newSprite;
            Debug.Log($"Nut {index} đã được cập nhật sprite mới: {newSprite.name}");
        }
        else
        {
            // Nếu không tìm thấy sprite, tìm sprite mới với tên targetSpriteName_0
            string newTargetSpriteName = targetSpriteName + "_0";
            Sprite newSpriteWithPrefix = System.Array.Find(nutSpriteComplete, sprite => sprite.name == newTargetSpriteName);

            if (newSpriteWithPrefix != null)
            {
                spriteRenderer.sprite = newSpriteWithPrefix;
                Debug.Log($"Nut {index} đã được cập nhật sprite mới với tiền tố _0: {newSpriteWithPrefix.name}");
            }
            else
            {
                // Nếu không tìm thấy sprite với tiền tố _0, dùng sprite mặc định.
                spriteRenderer.sprite = nutSpriteComplete[0]; // Sprite mặc định
                Debug.Log($"Không tìm thấy sprite {newTargetSpriteName}. Dùng sprite mặc định.");
            }
        }
    }

    void CreateNuts()
    {
        // Tải tất cả các sprite trong thư mục DotSkillRepare.
        Sprite[] nutSprites = Resources.LoadAll<Sprite>("DotSkillRepare");
        if (nutSprites == null || nutSprites.Length == 0)
        {
            Debug.LogError("Không tìm thấy sprite nào trong thư mục 'DotSkillRepare'.");
            return;
        }

        int nutCount = 7; // Số lượng nut.
        float spacing = 55.0f; // Khoảng cách giữa các nut.
        float totalWidth = (nutCount - 1) * spacing; // Tổng chiều rộng của cả hàng.
        float startX = -totalWidth / 2; // Tọa độ X bắt đầu để hàng nằm giữa.

        // Tạo danh sách tên nut.
        for (int i = 0; i < nutCount; i++)
        {
            // Tạo một bản sao của prefab.
            GameObject nut = Instantiate(nutPrefab, parentTransform);

            // Chọn ngẫu nhiên một sprite từ danh sách nutSprites.
            Sprite randomSprite = nutSprites[Random.Range(0, nutSprites.Length)];

            // Đặt tên của nut theo tên sprite.
            nut.name = randomSprite.name + "_" + i;

            // Lưu tên của nut vào danh sách
            nutNames.Add(nut.name);

            // Lưu nut vào danh sách nutObjects
            nutObjects.Add(nut);

            // Tính toán vị trí X cho từng nut.
            float xPosition = startX + i * spacing;

            // Đặt vị trí của nut.
            nut.transform.localPosition = new Vector3(xPosition, 0, 0);

            // Đảm bảo prefab có SpriteRenderer, nếu không thì thêm mới.
            SpriteRenderer spriteRenderer = nut.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = nut.AddComponent<SpriteRenderer>();
            }

            // Gán sprite ngẫu nhiên cho SpriteRenderer.
            spriteRenderer.sprite = randomSprite;

            // Đặt kích thước hiển thị của sprite thành 50x50.
            nut.transform.localScale = new Vector3(50.0f / spriteRenderer.sprite.bounds.size.x,
                                                   50.0f / spriteRenderer.sprite.bounds.size.y,
                                                   1.0f);

            // Log thông tin nut được tạo ra.
            Debug.Log($"Nut {i + 1} được tạo: Tên - {nut.name}, Vị trí - {nut.transform.localPosition}, Sprite - {randomSprite.name}");
        }

        // Gán phím cho các nut theo thứ tự tên
        keyBindings = new string[nutNames.Count];
        for (int i = 0; i < nutNames.Count; i++)
        {
            // Tạo các keyBinding theo tên của nut
            keyBindings[i] = nutNames[i] switch
            {
                var name when name.Contains("Left") => "nutLeft_" + i,
                var name when name.Contains("Right") => "nutRight_" + i,
                var name when name.Contains("Up") => "nutUp_" + i,
                var name when name.Contains("Down") => "nutDown_" + i,
                _ => throw new System.Exception("Tên nut không hợp lệ.")
            };
        }

        // Hiển thị danh sách keyBindings trong log để kiểm tra.
        string keyBindingsString = string.Join(", ", keyBindings);
        Debug.Log("Các phím đã gán: " + keyBindingsString);
    }

    // Coroutine để làm thanh slider chạy trong thời gian nhất định (duration).
    IEnumerator ScrollSlider(float duration)
    {
        timeCombo.value = 0;
        timeCombo.maxValue = 1;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            timeCombo.value = Mathf.Lerp(0, 1, elapsed / duration);
            yield return null;
        }
        timeCombo.value = 1;
        openBoard.SetActive(true);
        closeCardEspect.SetActive(false);

    }
}
