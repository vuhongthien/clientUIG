using UnityEngine;
using UnityEngine.UI;

public class SkillLg : MonoBehaviour
{
    public Slider slider;
    public Button button;

    private float durationSlider = 400f; // slider chạy chậm, đẹp
    private float durationLogic = 4f;    // thời gian tính LOGIC
    private float elapsedTime = 0f;
    private bool isSliding = false;

    private DotSkillManager skillManager;

    public GameObject timeSkillLegend;
    public GameObject arrowPanel;
    public GameObject GroupDot;
    public GameObject boardObj;



    void Start()
    {
        skillManager = FindObjectOfType<DotSkillManager>();
        slider.gameObject.SetActive(false);
        GroupDot.SetActive(false);
        arrowPanel.SetActive(false);
        button.onClick.AddListener(StartSliding);
    }

    void Update()
    {
        if (isSliding)
        {
            elapsedTime += Time.deltaTime;

            // slider chạy chậm
            float sliderProgress = Mathf.Lerp(0f, 100f, elapsedTime / durationSlider);
            slider.value = sliderProgress;

            // logic kết thúc sớm (4s)
            if (elapsedTime >= durationLogic)
            {
                isSliding = false;

                Debug.Log("Correct keys: " + skillManager.correctCount);

                // TẮT UI audition
                timeSkillLegend.SetActive(false);
                arrowPanel.SetActive(false);
                GroupDot.SetActive(false);
                boardObj.SetActive(true);

                // === GỌI PHÁ N DOT NGẪU NHIÊN TRÊN BẢNG ===
                if (skillManager != null)
                {
                    int count = skillManager.correctCount+3;
                    Board.Instance.DestroyRandomDots(count);
                }
            }

            // Slider full 100% thì tự dừng
            if (elapsedTime >= durationSlider)
            {
                elapsedTime = 0f;
                isSliding = false;
            }
        }
    }

    void StartSliding()
    {
        // Tạo combo mũi tên mới mỗi lần dùng skill
        if (skillManager != null)
        {
            skillManager.GenerateArrows();
        }

        slider.gameObject.SetActive(true);
        arrowPanel.SetActive(true);
        GroupDot.SetActive(true);
        boardObj.SetActive(false);

        isSliding = true;
        elapsedTime = 0f;
        slider.value = 0f;
    }
}
