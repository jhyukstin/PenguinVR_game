using UnityEngine;

public class MinimapCameraFollow : MonoBehaviour
{
    public Transform target;      // Player or MinimapFollowTarget
    public float height = 50f;    // how high above target
    public bool rotateWithTarget = true;

    void LateUpdate()
    {
        if (!target) return;

        Vector3 pos = target.position;
        pos.y += height;
        transform.position = pos;

        // Look straight down
        float yRot = rotateWithTarget ? target.eulerAngles.y : 0f;
        transform.rotation = Quaternion.Euler(90f, yRot, 0f);
    }
}
