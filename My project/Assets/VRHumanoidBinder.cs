using UnityEngine;

[RequireComponent(typeof(Animator))]
public class VRHumanoidBinder : MonoBehaviour
{
    public Transform headTarget;
    public Transform leftHandTarget;
    public Transform rightHandTarget;

    public Vector3 handPositionOffset;
    public Vector3 handRotationOffset;


    Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (!animator) return;

        // LEFT HAND IK
        if (leftHandTarget)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1f);

            animator.SetIKPosition(AvatarIKGoal.LeftHand,
                leftHandTarget.position + leftHandTarget.TransformVector(handPositionOffset));

            animator.SetIKRotation(AvatarIKGoal.LeftHand,
                leftHandTarget.rotation * Quaternion.Euler(handRotationOffset));
        }

        // RIGHT HAND IK
        if (rightHandTarget)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1f);

            animator.SetIKPosition(AvatarIKGoal.RightHand,
                rightHandTarget.position + rightHandTarget.TransformVector(handPositionOffset));

            animator.SetIKRotation(AvatarIKGoal.RightHand,
                rightHandTarget.rotation * Quaternion.Euler(handRotationOffset));
        }
    }
}