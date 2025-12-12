using TMPro;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlaneController : MonoBehaviour
{
    [Header("Refs")]
    public Transform centerOfMass;
    private Rigidbody rb;

    [Header("UI (optional)")]
    public TextMeshProUGUI speedUIText;

    // -------- Speed model --------
    [Header("Speed")]
    public float baseSpeed = 25f;
    public float minSpeed = 5f;
    public float maxSpeed = 80f;

    public float accelPerSec = 15f;
    public float decelPerSec = 20f;
    public float returnRate = 12f;

    public float speedAlignRate = 9f;
    public bool useSimpleLift = false;
    public float liftScale = 1.0f;

    [Header("Cockpit Controls Override")]
    public bool useCockpitControls = true;
    [Range(0f, 1f)] public float throttle01 = 0.5f; // 0=minSpeed, 1=maxSpeed
    public float throttleResponse = 25f;

    // -------- Attitude (tilt) --------
    [Header("Tilt Torques")]
    public float pitchPower = 2200f;
    public float rollPower = 1800f;

    [Header("Damping / Comfort")]
    public float pitchDamp = 0.30f;
    public float rollDamp = 0.28f;
    public float yawDamp = 0.10f;
    public float autoLevel = 0.50f;
    public float maxRollDegrees = 45f;

    [Header("Stability Assist")]
    public float inputSmoothing = 10f;
    public float inputDeadzone = 0.10f;
    public float maxAngularVel = 6f;
    public float pitchLeveling = 0.7f;
    public float uprightStrength = 8f;
    public float uprightDamping = 3f;
    public float assistFadeWithInput = 0.6f;

    [Header("Debug")]
    public bool debugLog = true;
    public bool debugOnScreen = true;

    public float currentSpeed;

    // runtime inputs
    private Vector2 _stickRaw;
    private Vector2 _stickSmoothed;

    // cockpit inputs (from stick/lever scripts)
    private Vector2 _cockpitStick;

    public void SetCockpitStick(Vector2 v)
    {
        _cockpitStick = Vector2.ClampMagnitude(v, 1f);
    }

    public void SetThrottle01(float t)
    {
        throttle01 = Mathf.Clamp01(t);
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (centerOfMass)
            rb.centerOfMass = transform.InverseTransformPoint(centerOfMass.position);

        rb.maxAngularVelocity = maxAngularVel;

        currentSpeed = Mathf.Clamp(baseSpeed, minSpeed, maxSpeed);
        _stickRaw = Vector2.zero;
        _stickSmoothed = Vector2.zero;
        _cockpitStick = Vector2.zero;
    }

    void Update()
    {
        // ✅ cockpit 입력 덮어쓰기
        if (useCockpitControls)
        {
            _stickRaw = _cockpitStick;
        }
        else
        {
            // (여기엔 컨트롤러/키보드 입력을 넣어도 되지만)
            _stickRaw = Vector2.zero;
        }

        // deadzone
        Vector2 dz = new Vector2(
            Mathf.Abs(_stickRaw.x) < inputDeadzone ? 0f : _stickRaw.x,
            Mathf.Abs(_stickRaw.y) < inputDeadzone ? 0f : _stickRaw.y
        );

        // smoothing
        _stickSmoothed = Vector2.Lerp(_stickSmoothed, dz, 1f - Mathf.Exp(-inputSmoothing * Time.deltaTime));

        if (speedUIText) speedUIText.text = currentSpeed.ToString("F0");
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // 1) Speed update
        if (useCockpitControls)
        {
            float targetSpeed = Mathf.Lerp(minSpeed, maxSpeed, throttle01);
            currentSpeed = MoveToward(currentSpeed, targetSpeed, throttleResponse * dt);
        }
        else
        {
            // fallback: baseSpeed 유지
            currentSpeed = MoveToward(currentSpeed, baseSpeed, returnRate * dt);
        }

        // 2) Stick -> pitch/roll inputs
        float speed = rb.linearVelocity.magnitude;
        float ctlScale = Mathf.Clamp01(speed / 20f);

        float rollIn = Mathf.Clamp(_stickSmoothed.x, -1f, 1f);
        float pitchIn = Mathf.Clamp(-_stickSmoothed.y, -1f, 1f);

        // 3) Torque
        Vector3 angVel = rb.angularVelocity;
        Vector3 localAng = transform.InverseTransformDirection(angVel);

        float noseUpDeg = Vector3.SignedAngle(
            Vector3.ProjectOnPlane(transform.forward, Vector3.up),
            transform.forward,
            transform.right
        );
        float noseUpRad = Mathf.Deg2Rad * Mathf.Clamp(noseUpDeg, -45f, 45f);
        Vector3 pitchLevelTorque = transform.right * (-noseUpRad * pitchLeveling * rb.mass);

        Vector3 torque =
            (transform.right * (pitchIn * pitchPower * ctlScale)) +
            (transform.forward * (-rollIn * rollPower * ctlScale)) +
            pitchLevelTorque;

        // damping
        torque += -transform.right * (localAng.x * pitchDamp * rb.mass);
        torque += -transform.forward * (localAng.z * rollDamp * rb.mass);
        torque += -transform.up * (localAng.y * yawDamp * rb.mass);

        // auto level roll
        Vector3 flatRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        float rollTilt = Vector3.SignedAngle(flatRight, transform.right, transform.forward) * Mathf.Deg2Rad;
        torque += -transform.forward * (rollTilt * autoLevel * rb.mass);

        rb.AddTorque(torque, ForceMode.Force);

        // upright assist (fade with input)
        float inputMag = Mathf.Clamp01(_stickSmoothed.magnitude);
        float assistScale = 1f - inputMag * assistFadeWithInput;

        Vector3 upError = Vector3.Cross(transform.up, Vector3.up);
        Vector3 uprightTorque = upError * (uprightStrength * rb.mass) - rb.angularVelocity * uprightDamping;
        rb.AddTorque(uprightTorque * assistScale, ForceMode.Force);

        // roll limit
        float rollDeg = Mathf.DeltaAngle(0f, transform.rotation.eulerAngles.z);
        if (Mathf.Abs(rollDeg) > maxRollDegrees)
        {
            float excess = Mathf.Sign(rollDeg) * (Mathf.Abs(rollDeg) - maxRollDegrees);
            rb.AddTorque(transform.forward * -excess * 20f, ForceMode.Force);
        }

        // 4) Forward velocity
        Vector3 targetVel = transform.forward * currentSpeed;
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVel, 1f - Mathf.Exp(-speedAlignRate * dt));

        // 5) Simple lift (optional)
        if (useSimpleLift)
        {
            float liftFactor = Mathf.Clamp01(rb.linearVelocity.magnitude / Mathf.Max(0.01f, baseSpeed));
            Vector3 simpleLift = Vector3.up * (rb.mass * Physics.gravity.magnitude) * liftFactor * liftScale;
            rb.AddForce(simpleLift, ForceMode.Force);
        }

        // Debug
        if (debugLog && Time.frameCount % 30 == 0)
        {
            Debug.Log($"[PlaneController:{name}] stick={_cockpitStick} throttle01={throttle01:F2} speed={currentSpeed:F1} rbVel={rb.linearVelocity.magnitude:F1}");
        }
    }

    void OnGUI()
    {
        if (!debugOnScreen) return;

        var style = new GUIStyle(GUI.skin.label) { fontSize = 16 };
        GUILayout.BeginArea(new Rect(10, 10, 520, 120), GUI.skin.box);
        GUILayout.Label($"PlaneController: {name}", style);
        GUILayout.Label($"useCockpitControls: {useCockpitControls}", style);
        GUILayout.Label($"cockpitStick: {_cockpitStick.x:F2}, {_cockpitStick.y:F2}", style);
        GUILayout.Label($"throttle01: {throttle01:F2}  currentSpeed: {currentSpeed:F1}  rbVel: {rb.linearVelocity.magnitude:F1}", style);
        GUILayout.EndArea();
    }

    static float MoveToward(float v, float target, float maxDelta)
    {
        if (v < target) return Mathf.Min(target, v + maxDelta);
        if (v > target) return Mathf.Max(target, v - maxDelta);
        return target;
    }
}
