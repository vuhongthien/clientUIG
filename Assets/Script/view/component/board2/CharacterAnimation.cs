using UnityEngine;

public class CharacterAnimation : MonoBehaviour
{
    private Animator animator;
    public Animator p;
    public Animator e;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {

    }


    public void ReturnToIdle()
    {
        animator.SetInteger("key", 0);

    }
    public void DisableHealAnimation()
    {
        p.gameObject.SetActive(false);
        e.gameObject.SetActive(false);
    }
}
