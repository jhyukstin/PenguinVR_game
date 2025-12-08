using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlaneController : MonoBehaviour
{
    [Header("Refs")]
    public VRStickInput stick;
    public VRThrottle throttle;
    public Transform thrustPoint;
    public Transform liftPoint;
    public Transform centerOfMass;

    Rigidbody rb;

    [Header("Aero")]
    public float wingArea = 16f;
    public float liftCoeff = 0.8f;
    public float dragCoeff = 0.04f;
    public float inducedDrag = 0.02f;
    public float airDensity = 1.225f;

    [Header("Engine")]
    public float maxThrust = 8000f;
    public AnimationCurve throttleResponse = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Controls (torque)")]
    public float pitchPower = 3500f;
    public float rollPower = 3000f;
    public float yawPower = 1200f;
    public float yawAssistWithSpeed = 0.002f; // increases yaw authority with speed

    [Header("Stability/Comfort")]
    public float autoLevel = 0.2f;  // roll leveling
    public float pitchDamp = 0.15f;
    public float rollDamp = 0.12f;
    public float yawDamp = 0.10f;
    public float maxRollDegrees = 45f; // cap for comfort

    // --- ADD: Keyboard control (optional) ---
    [Header("Keyboard (optional)")]
    public bool enableKeyboardControl = true;
    public float kbPitchSpeed = 1.5f;
    public float kbRollSpeed = 1.5f;
    public float kbYawSpeed = 1.5f;
    public float kbThrottleStep = 0.35f;  // per second
    [Range(0f, 1f)] public float kbThrottle = 0f; // 0..1

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (centerOfMass)
            rb.centerOfMass = transform.InverseTransformPoint(centerOfMass.position);
    }

    void FixedUpdate()
    {
        Vector3 vel = rb.linearVelocity;
        float speed = vel.magnitude;

        // local velocity
        Vector3 localVel = transform.InverseTransformDirection(vel);
        float forwardSpeed = Mathf.Max(0f, localVel.z);

        // angle of attack (simplified)
        float aoa = Mathf.Atan2(localVel.y, Mathf.Max(0.001f, forwardSpeed));

        // lift
        float q = 0.5f * airDensity * speed * speed;
        float CL = liftCoeff + Mathf.Clamp(aoa, -0.35f, 0.35f);
        Vector3 lift = Vector3.up * (CL * wingArea * q);
        if (liftPoint) rb.AddForceAtPosition(lift, liftPoint.position);
        else rb.AddForce(lift);

        // drag
        float Cd = dragCoeff + inducedDrag * CL * CL;
        if (speed > 0.001f)
        {
            Vector3 drag = -vel.normalized * (Cd * wingArea * q);
            rb.AddForce(drag);
        }

        // thrust
        float engine = throttle ? throttleResponse.Evaluate(throttle.Value) : 0f;

        // --- ADD: Keyboard throttle (R/F) ---
        if (enableKeyboardControl)
        {
            if (Input.GetKey(KeyCode.R)) kbThrottle = Mathf.Clamp01(kbThrottle + kbThrottleStep * Time.fixedDeltaTime);
            if (Input.GetKey(KeyCode.F)) kbThrottle = Mathf.Clamp01(kbThrottle - kbThrottleStep * Time.fixedDeltaTime);

            float engineKb = throttleResponse.Evaluate(kbThrottle);
            if (engineKb > engine) engine = engineKb;
        }

        Vector3 thrust = transform.forward * (engine * maxThrust);
        rb.AddForceAtPosition(thrust, thrustPoint ? thrustPoint.position : transform.position);

        // control torques
        float pitchIn = stick ? stick.Pitch : 0f;
        float rollIn = stick ? stick.Roll : 0f;
        float yawIn = 0f; // optional: wire a thumbstick X value into this (see step 6)

        // --- ADD: Keyboard pitch/roll/yaw overrides ---
        if (enableKeyboardControl)
        {
            float p = 0f; // W/S = pitch up/down
            if (Input.GetKey(KeyCode.W)) p += 1f;
            if (Input.GetKey(KeyCode.S)) p -= 1f;

            float r = 0f; // A/D = roll left/right
            if (Input.GetKey(KeyCode.D)) r += 1f;
            if (Input.GetKey(KeyCode.A)) r -= 1f;

            float y = 0f; // Q/E = yaw left/right
            if (Input.GetKey(KeyCode.E)) y += 1f;
            if (Input.GetKey(KeyCode.Q)) y -= 1f;

            // Prefer keyboard input when its magnitude is larger
            if (Mathf.Abs(p) > Mathf.Abs(pitchIn)) pitchIn = p;
            if (Mathf.Abs(r) > Mathf.Abs(rollIn)) rollIn = r;
            yawIn = y;
        }

        // DEBUG CODE:
        if (Time.frameCount % 60 == 0) // Log every 60 frames to avoid spam
        {
            float throttleValue = throttle ? throttle.Value : 0f;
            Debug.Log($"[PlaneController] Pitch: {pitchIn:F2}, Roll: {rollIn:F2}, Throttle: {throttleValue:F2} (kb:{kbThrottle:F2})");
        }

        Vector3 angVel = rb.angularVelocity;
        Vector3 localAng = transform.InverseTransformDirection(angVel);

        Vector3 torque =
            (transform.right * (pitchIn * pitchPower)) +
            (transform.forward * (-rollIn * rollPower)) +
            (transform.up * (yawIn * (yawPower + speed * yawAssistWithSpeed)));

        // damping
        torque += -transform.right * (localAng.x * pitchDamp * rb.mass);
        torque += -transform.forward * (localAng.z * rollDamp * rb.mass);
        torque += -transform.up * (localAng.y * yawDamp * rb.mass);

        // gentle auto-level (roll only)
        Vector3 flatRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        float rollTilt = Vector3.SignedAngle(flatRight, transform.right, transform.forward) * Mathf.Deg2Rad;
        torque += -transform.forward * (rollTilt * autoLevel * rb.mass);

        rb.AddTorque(torque);

        // roll clamp for comfort
        Vector3 euler = transform.rotation.eulerAngles;
        float roll = Mathf.DeltaAngle(0, euler.z);
        float maxR = maxRollDegrees;
        if (Mathf.Abs(roll) > maxR)
        {
            float excess = Mathf.Sign(roll) * (Mathf.Abs(roll) - maxR);
            rb.AddTorque(transform.forward * -excess * 20f); // push back toward cap
        }
    }
}
