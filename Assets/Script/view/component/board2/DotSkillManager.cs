using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DotSkillManager : MonoBehaviour
{
    public Transform parentPanel; // nơi chứa 7 nút (trong Canvas)
    public GameObject arrowPrefab; // prefab Image để hiển thị mũi tên
    public int arrowCount = 7;
    public int correctCount = 0;

    private List<Image> currentArrows = new List<Image>();
    private string[] directions = { "nutDown", "nutLeft", "nutRight", "nutUp" };
    private int currentIndex = 0;

    private Dictionary<string, Sprite> blueArrows = new Dictionary<string, Sprite>();
    private Dictionary<string, Sprite> purpleArrows = new Dictionary<string, Sprite>();

    void Start()
    {
        // Load sprites từ Resources
        foreach (string dir in directions)
        {
            blueArrows[dir] = Resources.Load<Sprite>($"DotSkillRepare/{dir}");
            purpleArrows[dir] = Resources.Load<Sprite>($"DotSkillComple/{dir}");
        }
    }

    public void GenerateArrows()
    {
        correctCount = 0;
        ClearOldArrows();
        currentArrows.Clear();
        currentIndex = 0;

        for (int i = 0; i < arrowCount; i++)
        {
            string randomDir = directions[Random.Range(0, directions.Length)];
            GameObject newArrow = Instantiate(arrowPrefab, parentPanel);
            Image img = newArrow.GetComponent<Image>();
            img.sprite = blueArrows[randomDir];
            img.name = randomDir; // để kiểm tra khi người chơi nhấn
            currentArrows.Add(img);
        }
    }

    void Update()
    {
        if (currentArrows.Count == 0) return;

        if (Input.anyKeyDown)
        {
            string keyPressed = GetDirectionFromInput();
            if (keyPressed != null)
            {
                CheckArrow(keyPressed);
            }
        }
    }

    void CheckArrow(string dir)
    {
        if (currentIndex >= currentArrows.Count) return;

        Image currentArrow = currentArrows[currentIndex];

        // Kiểm tra đúng
        if (currentArrow.name == dir)
        {
            currentArrow.sprite = purpleArrows[dir];
            currentIndex++;

            correctCount++;  // tăng số nút đúng
        }
        else
        {
            Debug.Log("Sai phím! Reset lại từ đầu.");

            // Reset toàn bộ
            ResetCombo();
            return;
        }

        // Hoàn thành
        if (currentIndex == currentArrows.Count)
        {
            Debug.Log("Hoàn thành combo!");
        }
    }

    void ResetCombo()
    {
        Debug.Log("Sai phím! Reset lại bộ 7 key cũ.");

        // Đưa tất cả mũi tên về trạng thái ban đầu (màu xanh)
        for (int i = 0; i < currentArrows.Count; i++)
        {
            Image arrow = currentArrows[i];
            string dir = arrow.name; // nutDown / nutLeft / nutRight / nutUp
            arrow.sprite = blueArrows[dir];
        }

        // Reset chỉ số của combo
        currentIndex = 0;
        correctCount = 0;
    }



    public void OnButtonPress(string dir)
    {
        CheckArrow(dir);
    }
    string GetDirectionFromInput()
    {
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) return "nutDown";
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) return "nutLeft";
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) return "nutRight";
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) return "nutUp";
        return null;
    }

    void ClearOldArrows()
    {
        foreach (Transform child in parentPanel)
            Destroy(child.gameObject);
    }
}
