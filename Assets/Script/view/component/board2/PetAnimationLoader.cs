using UnityEngine;

public class PetAnimationLoader : MonoBehaviour
{
    public Animator animator;
    private bool check =true;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if(check){

        
        if (!gameObject.name.Equals("animationUPet") && !gameObject.name.Equals("animationEPet"))
        {
            AnimationClip[] clips = LoadAnimationsByPetName(gameObject.name);
            Debug.Log("---------"+ clips.Length);
            // Thay thế Animation Clips trong Animator
            if (clips != null && animator != null)
            {
                Debug.Log("--thay----"+ gameObject.name);
                ReplaceAnimations(clips);
                
            }
            check = false;
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
