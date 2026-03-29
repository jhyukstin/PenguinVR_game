using UnityEngine;

public class SimpleVRArmFollow : MonoBehaviour
{
    [Header("Left Arm")]
    public Transform leftPivotProxy;   // 씬에 따로 만든 어깨 기준점
    public Transform leftBone;
    public Transform leftController;

    [Header("Right Arm")]
    public Transform rightPivotProxy;
    public Transform rightBone;
    public Transform rightController;

    [Header("Offsets (Euler)")]
    public Vector3 leftPivotOffsetEuler;
    public Vector3 leftBoneOffsetEuler;
    public Vector3 rightPivotOffsetEuler;
    public Vector3 rightBoneOffsetEuler;

    [Header("Smoothing")]
    public float pivotRotationSmooth = 10f;
    public float boneRotationSmooth = 15f;
    public float targetPositionSmooth = 12f;

    [Header("Arm Length Limit")]
    public float maxArmLength = 0.6f;

    Vector3 smoothedLeftTargetPos;
    Vector3 smoothedRightTargetPos;
    bool leftInit;
    bool rightInit;

    void OnEnable()
    {
        if (leftController)
        {
            smoothedLeftTargetPos = leftController.position;
            leftInit = true;
        }

        if (rightController)
        {
            smoothedRightTargetPos = rightController.position;
            rightInit = true;
        }
    }

    void LateUpdate()
    {
        FollowArm(
            leftPivotProxy,
            leftBone,
            leftController,
            ref smoothedLeftTargetPos,
            ref leftInit,
            leftPivotOffsetEuler,
            leftBoneOffsetEuler
        );

        FollowArm(
            rightPivotProxy,
            rightBone,
            rightController,
            ref smoothedRightTargetPos,
            ref rightInit,
            rightPivotOffsetEuler,
            rightBoneOffsetEuler
        );
    }

    void FollowArm(
        Transform pivotProxy,
        Transform bone,
        Transform controller,
        ref Vector3 smoothedTargetPos,
        ref bool initialized,
        Vector3 pivotOffsetEuler,
        Vector3 boneOffsetEuler)
    {
        if (!pivotProxy || !bone || !controller) return;

        if (!initialized)
        {
            smoothedTargetPos = controller.position;
            initialized = true;
        }

        float posT = 1f - Mathf.Exp(-targetPositionSmooth * Time.deltaTime);
        smoothedTargetPos = Vector3.Lerp(smoothedTargetPos, controller.position, posT);

        Vector3 toTarget = smoothedTargetPos - pivotProxy.position;

        if (toTarget.sqrMagnitude > 0.000001f)
        {
            if (toTarget.magnitude > maxArmLength)
            {
                toTarget = toTarget.normalized * maxArmLength;
            }

            Quaternion targetPivotRot =
                Quaternion.LookRotation(toTarget.normalized, pivotProxy.up) *
                Quaternion.Euler(pivotOffsetEuler);

            float rotT = 1f - Mathf.Exp(-pivotRotationSmooth * Time.deltaTime);
            pivotProxy.rotation = Quaternion.Slerp(pivotProxy.rotation, targetPivotRot, rotT);
        }

        Quaternion targetBoneRot =
            controller.rotation * Quaternion.Euler(boneOffsetEuler);

        float boneT = 1f - Mathf.Exp(-boneRotationSmooth * Time.deltaTime);
        bone.rotation = Quaternion.Slerp(bone.rotation, targetBoneRot, boneT);
    }
}