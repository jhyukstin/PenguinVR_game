using UnityEngine;

[RequireComponent(typeof(Animator))]
public class VRHumanoidBinder : MonoBehaviour
{
    [Header("Targets from XR Rig")]
    public Transform headTarget;
    public Transform leftHandTarget;
    public Transform rightHandTarget;

    [Header("Offsets (tweak in Inspector)")]
    public Vector3 headPositionOffset;
    public Vector3 headRotationOffset;
    public Vector3 handPositionOffset;
    public Vector3 handRotationOffset;

    Animator animator;
    Transform headBone;
    Transform leftHandBone;
    Transform rightHandBone;

    void Awake()
    {
        animator = GetComponent<Animator>();

        if (animator != null)
        {
            headBone = animator.GetBoneTransform(HumanBodyBones.Head);
            leftHandBone = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            rightHandBone = animator.GetBoneTransform(HumanBodyBones.RightHand);
        }
    }

    void LateUpdate()
    {
        // HEAD
        if (headTarget != null && headBone != null)
        {
            headBone.position = headTarget.position + headTarget.TransformVector(headPositionOffset);
            headBone.rotation = headTarget.rotation * Quaternion.Euler(headRotationOffset);
        }

        // LEFT HAND
        if (leftHandTarget != null && leftHandBone != null)
        {
            leftHandBone.position = leftHandTarget.position + leftHandTarget.TransformVector(handPositionOffset);
            leftHandBone.rotation = leftHandTarget.rotation * Quaternion.Euler(handRotationOffset);
        }

        // RIGHT HAND
        if (rightHandTarget != null && rightHandBone != null)
        {
            rightHandBone.position = rightHandTarget.position + rightHandTarget.TransformVector(handPositionOffset);
            rightHandBone.rotation = rightHandTarget.rotation * Quaternion.Euler(handRotationOffset);
        }
    }
}
