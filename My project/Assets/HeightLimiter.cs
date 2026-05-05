using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HeightLimiter : MonoBehaviour
{
    public float maxHeight = 135f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        Vector3 pos = rb.position;

        if (pos.y > maxHeight)
        {
            pos.y = maxHeight;
            rb.position = pos;

            Vector3 vel = rb.linearVelocity;

            // 위로 올라가는 속도 제거
            if (vel.y > 0f)
                vel.y = 0f;

            rb.linearVelocity = vel;
        }
    }
}