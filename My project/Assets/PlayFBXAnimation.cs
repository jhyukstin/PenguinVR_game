using UnityEngine;

public class PlayFBXAnimationClip : MonoBehaviour
{
    public AnimationClip animationClip;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();

        if (!animator)
            Debug.LogError("Animator not found!");

        if (!animationClip)
            Debug.LogError("No animation clip assigned!");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            animator.Play(animationClip.name);
        }
    }
}
