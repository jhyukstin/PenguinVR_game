using System.Collections;
using UnityEngine;

public class StartAnimationAtFrame : MonoBehaviour
{
   [SerializeField] private Animator animator;
    [SerializeField] private AnimationClip clip;
    [SerializeField] private string stateName = "mixamo.com";
    [SerializeField] private int startFrame = 120;

    private IEnumerator OnEnable()
    {
        yield return null; // 활성화 후 1프레임 대기

        animator.Rebind();
        animator.Update(0f);

        float time = startFrame / clip.frameRate;
        float normalizedTime = time / clip.length;

        animator.Play(stateName, 0, normalizedTime);
        animator.Update(0f); // 바로 적용
    }
}