using UnityEngine;

public class AnimationLoad : MonoBehaviour
{
public Animator animator;
    private bool check =true;
private string previousName = "";
    void Start()
    {
        animator = GetComponent<Animator>();
    }

void Update()
    {
        // Kiểm tra nếu tên gameObject thay đổi và không nằm trong danh sách bị loại trừ
        if (!gameObject.name.Equals(previousName) && 
            !gameObject.name.Equals("animationUPet") && 
            !gameObject.name.Equals("animationEPet"))
        {
            // Cập nhật tên đã lưu
            previousName = gameObject.name;

            // Tải Animation Clips theo tên gameObject mới
            AnimationClip[] clips = LoadAnimationsByPetName(gameObject.name);
            Debug.Log("Số lượng Animation Clips: " + clips.Length);

            // Thay thế Animation Clips trong Animator
            if (clips != null && animator != null)
            {
                Debug.Log("Thay đổi Animation Clips cho: " + gameObject.name);
                ReplaceAnimations(clips);
            }
        }
    }
    AnimationClip[] LoadAnimationsByPetName(string petName)
    {
        // Load tất cả Animation Clips từ thư mục tương ứng với tên pet
        return Resources.LoadAll<AnimationClip>($"Pets/{petName}");
    }

    void ReplaceAnimations(AnimationClip[] newClips)
    {
        // Tạo một AnimatorOverrideController để thay thế Animation Clips
        RuntimeAnimatorController originalController = animator.runtimeAnimatorController;
        AnimatorOverrideController overrideController = new AnimatorOverrideController(originalController);

        // Duyệt qua tất cả các Animation Clips và thay thế
        foreach (AnimationClip newClip in newClips)
        {
            foreach (var pair in overrideController.animationClips)
            {
                if (pair.name == newClip.name) // So sánh theo tên
                {
                    overrideController[pair] = newClip; // Gán Animation Clip mới
                }
            }
        }

        // Gán AnimatorOverrideController mới cho Animator
        animator.runtimeAnimatorController = overrideController;
    }
}
