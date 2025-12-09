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
    public float minSpeed = 5f;         // 감속 하한
    public float maxSpeed = 80f;        // 가속 상한
    public float accelPerSec = 15f;      // B 누르는 동안 가속량 (m/s^2)
    public float decelPerSec = 20f;      // A 누르는 동안 감속량 (m/s^2)
    public float returnRate = 12f;      // 버튼 놓았을 때 baseSpeed로 복귀 속도 (m/s^2)
    public float speedAlignRate = 9f;    // 현재 velocity를 기수 방향으로 맞추는 보정(감각 부드럽게)
    public bool useSimpleLift = false;  // 간이 양력 사용 여부
    public float liftScale = 1.0f;   // 간이 양력 강도(1.0부터 시작)

    private float currentSpeed;

    // -------- Attitude (tilt) --------
    [Header("Tilt Torques")]
    public float pitchPower = 2200f;     // 피치 토크
    public float rollPower = 1800f;     // 롤 토크
    public float yawAssistWithSpeed = 0.0f; // 시험버전: Yaw는 0 (원하면 올리기)

    [Header("Damping / Comfort")]
    public float pitchDamp = 0.30f;
    public float rollDamp = 0.28f;
    public float yawDamp = 0.10f;
    public float autoLevel = 0.50f;      // 롤 자동 복원
    public float maxRollDegrees = 45f;   // 롤 제한

    // -------- Stability Assist --------
    [Header("Stability Assist")]
    public float inputSmoothing = 10f;   // 입력 스무딩
    public float inputDeadzone = 0.10f; // 작은 입력 무시
    public float maxAngularVel = 6f;    // 회전 속도 상한
    public float pitchLeveling = 0.7f;  // 피치 수평 보정 강도
    public float uprightStrength = 8f;   // transform.up을 세계 Up으로 돌리는 힘
    public float uprightDamping = 3f;   // 그때의 감쇠
    public float assistFadeWithInput = 0.6f; // 스틱을 많이 움직일수록 보조 감소(0~1)

    // -------- Input System actions --------
#if ENABLE_INPUT_SYSTEM
    [Header("Input Actions (auto-create if empty)")]
    public InputAction stickAction;   // 왼손 스틱 (Vector2)
    public InputAction accelAction;   // 오른손 B (buttonEast)
    public InputAction decelAction;   // 오른손 A (buttonSouth)
#endif

    // -------- Debug --------
    [Header("Debug")]
    public bool debugLogInputs = true;
    public bool debugOnScreen = true;

    // runtime
    Vector2 _stick, _stickRaw;
    bool _accelHeld, _decelHeld;
    float _debugNext;

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
        // LEFT STICK (Vector2)
        if (stickAction == null || stickAction.bindings.Count == 0)
        {
            stickAction = new InputAction("LeftStick", InputActionType.Value, expectedControlType: "Vector2");
            // 제너릭(권장)
            stickAction.AddBinding("<XRController>{LeftHand}/primary2DAxis");
            // Oculus 계열(예비)
            stickAction.AddBinding("<OculusTouchController>{LeftHand}/primary2DAxis");
            // 레거시(예비)
            stickAction.AddBinding("<XRController>{LeftHand}/thumbstick");
            stickAction.AddBinding("<OculusTouchController>{LeftHand}/thumbstick");
        }

        // RIGHT B (secondaryButton)
        if (accelAction == null || accelAction.bindings.Count == 0)
        {
            accelAction = new InputAction("RightB", InputActionType.Button);
            // 제너릭(권장)
            accelAction.AddBinding("<XRController>{RightHand}/secondaryButton");
            // Oculus 계열(예비)
            accelAction.AddBinding("<OculusTouchController>{RightHand}/secondaryButton");
            // 레거시(예비)
            accelAction.AddBinding("<XRController>{RightHand}/buttonEast");
            accelAction.AddBinding("<OculusTouchController>{RightHand}/buttonEast");
        }

        // RIGHT A (primaryButton)
        if (decelAction == null || decelAction.bindings.Count == 0)
        {
            decelAction = new InputAction("RightA", InputActionType.Button);
            // 제너릭(권장)
            decelAction.AddBinding("<XRController>{RightHand}/primaryButton");
            // Oculus 계열(예비)
            decelAction.AddBinding("<OculusTouchController>{RightHand}/primaryButton");
            // 레거시(예비)
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
        // ---- 원시 입력 읽기 ----
        _stickRaw = Vector2.zero;
        _accelHeld = _decelHeld = false;

#if ENABLE_INPUT_SYSTEM
        if (stickAction != null && stickAction.enabled) _stickRaw = stickAction.ReadValue<Vector2>();
        if (accelAction != null && accelAction.enabled) _accelHeld = accelAction.IsPressed();
        if (decelAction != null && decelAction.enabled) _decelHeld = decelAction.IsPressed();

        // (선택) 키보드 백업도 새 Input System으로 처리 (에디터 테스트용)
        if (Keyboard.current != null)
        {
            var kb = Keyboard.current;
            float sx = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
            float sy = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
            if (Mathf.Abs(sx) > 0f || Mathf.Abs(sy) > 0f)
                _stickRaw = new Vector2(sx, sy);

            _accelHeld |= kb.rKey.isPressed; // R = 가속
            _decelHeld |= kb.fKey.isPressed; // F = 감속
        }
#endif
        _stickRaw = Vector2.ClampMagnitude(_stickRaw, 1f);

        // ---- 데드존 + 입력 스무딩 ----
        Vector2 dz = new Vector2(
            Mathf.Abs(_stickRaw.x) < inputDeadzone ? 0f : _stickRaw.x,
            Mathf.Abs(_stickRaw.y) < inputDeadzone ? 0f : _stickRaw.y
        );
        _stick = Vector2.Lerp(_stick, dz, 1f - Mathf.Exp(-inputSmoothing * Time.deltaTime));

        // ---- 디버그 로그 (스팸 방지 5fps) ----
        if (debugLogInputs && Time.time >= _debugNext)
        {
            _debugNext = Time.time + 0.2f;
            float v = rb ? rb.linearVelocity.magnitude : 0f;
            Debug.Log($"[Plane] stick=({_stick.x:F2},{_stick.y:F2})  accel={_accelHeld} decel={_decelHeld}  speed={currentSpeed:F1}  vel={v:F1}");
        }
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // 1) 속도 업데이트: B(가속) / A(감속) / 기본속도 복귀
        if (_accelHeld && !_decelHeld)
            currentSpeed = Mathf.Min(maxSpeed, currentSpeed + accelPerSec * dt);
        else if (_decelHeld && !_accelHeld)
            currentSpeed = Mathf.Max(minSpeed, currentSpeed - decelPerSec * dt);
        else
            currentSpeed = MoveToward(currentSpeed, baseSpeed, returnRate * dt);

        // 2) 스틱 → 기울기 입력
        float speed = rb.linearVelocity.magnitude;
        float ctlScale = Mathf.Clamp01(speed / 20f); // 저속에서 과조작 방지
        float rollIn = Mathf.Clamp(_stick.x, -1f, 1f);
        float pitchIn = Mathf.Clamp(-_stick.y, -1f, 1f); // 앞 밀면 코내림 → 음수

        // 3) 토크 계산
        Vector3 angVel = rb.angularVelocity;
        Vector3 localAng = transform.InverseTransformDirection(angVel);

        // 피치 수평 보정(너무 들거나 숙이면 천천히 수평으로)
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

        // 댐핑
        torque += -transform.right * (localAng.x * pitchDamp * rb.mass);
        torque += -transform.forward * (localAng.z * rollDamp * rb.mass);
        torque += -transform.up * (localAng.y * yawDamp * rb.mass);

        // 롤 자동 레벨링(수평 기준)
        Vector3 flatRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        float rollTilt = Vector3.SignedAngle(flatRight, transform.right, transform.forward) * Mathf.Deg2Rad;
        torque += -transform.forward * (rollTilt * autoLevel * rb.mass);

        rb.AddTorque(torque, ForceMode.Force);

        // 4) Upright 보정: transform.up을 세계 Up으로 맞춤 (입력 많을수록 보조 축소)
        float inputMag = Mathf.Clamp01(new Vector2(_stick.x, _stick.y).magnitude);
        float assistScale = 1f - inputMag * assistFadeWithInput;
        Vector3 upError = Vector3.Cross(transform.up, Vector3.up); // 회전해야 할 축
        Vector3 uprightTorque = upError * (uprightStrength * rb.mass) - rb.angularVelocity * uprightDamping;
        rb.AddTorque(uprightTorque * assistScale, ForceMode.Force);

        // 5) 롤 각도 제한(멀미 방지)
        float rollDeg = Mathf.DeltaAngle(0f, transform.rotation.eulerAngles.z);
        if (Mathf.Abs(rollDeg) > maxRollDegrees)
        {
            float excess = Mathf.Sign(rollDeg) * (Mathf.Abs(rollDeg) - maxRollDegrees);
            rb.AddTorque(transform.forward * -excess * 20f, ForceMode.Force);
        }

        // 6) 전진 속도 적용 (기수 방향으로 정렬)
        Vector3 targetVel = transform.forward * currentSpeed;
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVel, 1f - Mathf.Exp(-speedAlignRate * dt));

        // 7) (옵션) 간이 양력: 속도 비례로 중력 상쇄 보조
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
        GUILayout.BeginArea(new Rect(10, 10, 360, 110), GUI.skin.box);
        GUILayout.Label($"Stick X:{_stick.x:F2}  Y:{_stick.y:F2}", style);
        GUILayout.Label($"Accel(B):{_accelHeld}  Decel(A):{_decelHeld}", style);
        GUILayout.Label($"Speed:{currentSpeed:F1}  Vel:{rb.linearVelocity.magnitude:F1}", style);
        GUILayout.EndArea();
    }

    static float MoveToward(float v, float target, float maxDelta)
    {
        if (v < target) return Mathf.Min(target, v + maxDelta);
        if (v > target) return Mathf.Max(target, v - maxDelta);
        return target;
    }
}
