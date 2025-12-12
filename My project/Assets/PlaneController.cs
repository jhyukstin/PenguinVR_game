using TMPro;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody))]
public class PlaneController : MonoBehaviour
{
    [Header("Refs")]
    public Transform centerOfMass;
    private Rigidbody rb;

    // -------- Speed model --------
    [Header("Speed")]
    public float baseSpeed = 25f;        // 기본 전진 속도 (m/s)
    public float minSpeed = 5f;          // 감속 하한
    public float maxSpeed = 80f;         // 가속 상한
    public float accelPerSec = 15f;      // B 누르는 동안 가속량 (m/s^2)
    public float decelPerSec = 20f;      // A 누르는 동안 감속량 (m/s^2)
    public float returnRate = 12f;       // 버튼 놓았을 때 baseSpeed로 복귀 속도 (m/s^2)
    public float speedAlignRate = 9f;    // 현재 velocity를 기수 방향으로 맞추는 보정
    public bool useSimpleLift = false;   // 간이 양력 사용 여부
    public float liftScale = 1.0f;       // 간이 양력 강도

    public float currentSpeed;

    // -------- Attitude (tilt) --------
    [Header("Tilt Torques")]
    public float pitchPower = 2200f;
    public float rollPower = 1800f;
    public float yawAssistWithSpeed = 0.0f;

    [Header("Damping / Comfort")]
    public float pitchDamp = 0.30f;
    public float rollDamp = 0.28f;
    public float yawDamp = 0.10f;
    public float autoLevel = 0.50f;
    public float maxRollDegrees = 45f;

    // -------- Stability Assist --------
    [Header("Stability Assist")]
    public float inputSmoothing = 10f;
    public float inputDeadzone = 0.10f;
    public float maxAngularVel = 6f;
    public float pitchLeveling = 0.7f;
    public float uprightStrength = 8f;
    public float uprightDamping = 3f;
    public float assistFadeWithInput = 0.6f;

    // -------- Cockpit override --------
    [Header("Cockpit Controls Override")]
    public bool useCockpitControls = true;     // ✅ Stick/Lever로 조종할거면 true
    [Range(0f, 1f)] public float throttle01 = 0.5f; // 0=minSpeed, 1=maxSpeed
    public float throttleResponse = 25f;       // 레버 목표속도에 따라가는 속도

    Vector2 _cockpitStick = Vector2.zero;

    public void SetCockpitStick(Vector2 v)
    {
        _cockpitStick = Vector2.ClampMagnitude(v, 1f);
    }

    public void SetThrottle01(float t)
    {
        throttle01 = Mathf.Clamp01(t);
    }

    // -------- Input System actions --------
#if ENABLE_INPUT_SYSTEM
    [Header("Input Actions (auto-create if empty)")]
    public InputAction stickAction;   // 왼손 스틱 (Vector2)
    public InputAction accelAction;   // 오른손 B
    public InputAction decelAction;   // 오른손 A
#endif

    // -------- Debug --------
    [Header("Debug")]
    public bool debugLogInputs = true;
    public bool debugOnScreen = true;

    Vector2 _stick, _stickRaw;
    bool _accelHeld, _decelHeld;
    float _debugNext;

    public TextMeshProUGUI speedUIText;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (centerOfMass)
            rb.centerOfMass = transform.InverseTransformPoint(centerOfMass.position);

        rb.maxAngularVelocity = maxAngularVel;
        currentSpeed = Mathf.Clamp(baseSpeed, minSpeed, maxSpeed);
    }

#if ENABLE_INPUT_SYSTEM
    void OnEnable()
    {
        // LEFT STICK
        if (stickAction == null || stickAction.bindings.Count == 0)
        {
            stickAction = new InputAction("LeftStick", InputActionType.Value, expectedControlType: "Vector2");
            stickAction.AddBinding("<XRController>{LeftHand}/primary2DAxis");
            stickAction.AddBinding("<OculusTouchController>{LeftHand}/primary2DAxis");
            stickAction.AddBinding("<XRController>{LeftHand}/thumbstick");
            stickAction.AddBinding("<OculusTouchController>{LeftHand}/thumbstick");
        }

        // RIGHT B
        if (accelAction == null || accelAction.bindings.Count == 0)
        {
            accelAction = new InputAction("RightB", InputActionType.Button);
            accelAction.AddBinding("<XRController>{RightHand}/secondaryButton");
            accelAction.AddBinding("<OculusTouchController>{RightHand}/secondaryButton");
            accelAction.AddBinding("<XRController>{RightHand}/buttonEast");
            accelAction.AddBinding("<OculusTouchController>{RightHand}/buttonEast");
        }

        // RIGHT A
        if (decelAction == null || decelAction.bindings.Count == 0)
        {
            decelAction = new InputAction("RightA", InputActionType.Button);
            decelAction.AddBinding("<XRController>{RightHand}/primaryButton");
            decelAction.AddBinding("<OculusTouchController>{RightHand}/primaryButton");
            decelAction.AddBinding("<XRController>{RightHand}/buttonSouth");
            decelAction.AddBinding("<OculusTouchController>{RightHand}/buttonSouth");
        }

        stickAction.Enable();
        accelAction.Enable();
        decelAction.Enable();
    }

    void OnDisable()
    {
        if (stickAction != null) stickAction.Disable();
        if (accelAction != null) accelAction.Disable();
        if (decelAction != null) decelAction.Disable();
    }
#endif

    void Update()
    {
        _stickRaw = Vector2.zero;
        _accelHeld = _decelHeld = false;

#if ENABLE_INPUT_SYSTEM
        if (stickAction != null && stickAction.enabled) _stickRaw = stickAction.ReadValue<Vector2>();
        if (accelAction != null && accelAction.enabled) _accelHeld = accelAction.IsPressed();
        if (decelAction != null && decelAction.enabled) _decelHeld = decelAction.IsPressed();

        // 키보드 백업(에디터 테스트)
        if (Keyboard.current != null)
        {
            var kb = Keyboard.current;
            float sx = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
            float sy = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
            if (Mathf.Abs(sx) > 0f || Mathf.Abs(sy) > 0f)
                _stickRaw = new Vector2(sx, sy);

            _accelHeld |= kb.rKey.isPressed;
            _decelHeld |= kb.fKey.isPressed;
        }
#endif

        _stickRaw = Vector2.ClampMagnitude(_stickRaw, 1f);

        // ✅ 콕핏 조종(Stick/Lever) 사용 시 입력 덮어쓰기
        if (useCockpitControls)
        {
            _stickRaw = _cockpitStick;
            _accelHeld = false;
            _decelHeld = false;
        }

        // 데드존 + 스무딩
        Vector2 dz = new Vector2(
            Mathf.Abs(_stickRaw.x) < inputDeadzone ? 0f : _stickRaw.x,
            Mathf.Abs(_stickRaw.y) < inputDeadzone ? 0f : _stickRaw.y
        );
        _stick = Vector2.Lerp(_stick, dz, 1f - Mathf.Exp(-inputSmoothing * Time.deltaTime));

        if (debugLogInputs && Time.time >= _debugNext)
        {
            _debugNext = Time.time + 0.2f;
            float v = rb ? rb.linearVelocity.magnitude : 0f;
            Debug.Log($"[Plane] stick=({_stick.x:F2},{_stick.y:F2})  throttle01={throttle01:F2} speed={currentSpeed:F1} vel={v:F1}");
        }

        if (speedUIText) speedUIText.text = currentSpeed.ToString("F0");
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // 1) 속도 업데이트
        if (useCockpitControls)
        {
            float targetSpeed = Mathf.Lerp(minSpeed, maxSpeed, throttle01);
            currentSpeed = MoveToward(currentSpeed, targetSpeed, throttleResponse * dt);
        }
        else
        {
            if (_accelHeld && !_decelHeld)
                currentSpeed = Mathf.Min(maxSpeed, currentSpeed + accelPerSec * dt);
            else if (_decelHeld && !_accelHeld)
                currentSpeed = Mathf.Max(minSpeed, currentSpeed - decelPerSec * dt);
            else
                currentSpeed = MoveToward(currentSpeed, baseSpeed, returnRate * dt);
        }

        // 2) 스틱 → 기울기 입력
        float speed = rb.linearVelocity.magnitude;
        float ctlScale = Mathf.Clamp01(speed / 20f);
        float rollIn = Mathf.Clamp(_stick.x, -1f, 1f);
        float pitchIn = Mathf.Clamp(-_stick.y, -1f, 1f);

        // 3) 토크 계산
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

        torque += -transform.right * (localAng.x * pitchDamp * rb.mass);
        torque += -transform.forward * (localAng.z * rollDamp * rb.mass);
        torque += -transform.up * (localAng.y * yawDamp * rb.mass);

        Vector3 flatRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        float rollTilt = Vector3.SignedAngle(flatRight, transform.right, transform.forward) * Mathf.Deg2Rad;
        torque += -transform.forward * (rollTilt * autoLevel * rb.mass);

        rb.AddTorque(torque, ForceMode.Force);

        // 4) Upright 보정
        float inputMag = Mathf.Clamp01(_stick.magnitude);
        float assistScale = 1f - inputMag * assistFadeWithInput;
        Vector3 upError = Vector3.Cross(transform.up, Vector3.up);
        Vector3 uprightTorque = upError * (uprightStrength * rb.mass) - rb.angularVelocity * uprightDamping;
        rb.AddTorque(uprightTorque * assistScale, ForceMode.Force);

        // 5) 롤 제한
        float rollDeg = Mathf.DeltaAngle(0f, transform.rotation.eulerAngles.z);
        if (Mathf.Abs(rollDeg) > maxRollDegrees)
        {
            float excess = Mathf.Sign(rollDeg) * (Mathf.Abs(rollDeg) - maxRollDegrees);
            rb.AddTorque(transform.forward * -excess * 20f, ForceMode.Force);
        }

        // 6) 전진 속도 적용
        Vector3 targetVel = transform.forward * currentSpeed;
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVel, 1f - Mathf.Exp(-speedAlignRate * dt));

        // 7) 간이 양력
        if (useSimpleLift)
        {
            float liftFactor = Mathf.Clamp01(rb.linearVelocity.magnitude / baseSpeed);
            Vector3 simpleLift = Vector3.up * (rb.mass * Physics.gravity.magnitude) * liftFactor * liftScale;
            rb.AddForce(simpleLift, ForceMode.Force);
        }
    }

    void OnGUI()
    {
        if (!debugOnScreen) return;
        var style = new GUIStyle(GUI.skin.label) { fontSize = 16 };
        GUILayout.BeginArea(new Rect(10, 10, 420, 110), GUI.skin.box);
        GUILayout.Label($"Cockpit:{useCockpitControls}", style);
        GUILayout.Label($"Stick X:{_stick.x:F2}  Y:{_stick.y:F2}", style);
        GUILayout.Label($"Throttle01:{throttle01:F2}  Speed:{currentSpeed:F1}", style);
        GUILayout.EndArea();
    }

    static float MoveToward(float v, float target, float maxDelta)
    {
        if (v < target) return Mathf.Min(target, v + maxDelta);
        if (v > target) return Mathf.Max(target, v - maxDelta);
        return target;
    }
}
