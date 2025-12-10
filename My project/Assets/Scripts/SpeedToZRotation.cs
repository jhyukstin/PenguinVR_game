using UnityEngine;

public class SpeedToZRotation : MonoBehaviour
{
    [Header("Plane Controller Reference")]
    public PlaneController plane;       // currentSpeed 읽어올 대상

    [Header("Rotation Settings")]
    public float minZ = -75f;
    public float maxZ = 75f;
    public float smooth = 5f;

    void Update()
    {
        if (plane == null) return;

        // 1) 현재 속도 비율 계산 (0 ~ 1)
        float t = Mathf.InverseLerp(plane.minSpeed, plane.maxSpeed, plane.currentSpeed);

        // 2) 속도를 -75 ~ 75 도 사이로 매핑
        float targetZ = Mathf.Lerp(minZ, maxZ, t);
        targetZ = -targetZ;

        // 3) 로컬 회전 적용 (부드럽게)
        Vector3 rot = transform.localEulerAngles;
        rot.z = Mathf.LerpAngle(rot.z, targetZ, Time.deltaTime * smooth);
        transform.localEulerAngles = rot;
    }
}