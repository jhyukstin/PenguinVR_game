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

    // ---- Speed model ----
    [Header("Speed")]
    public float baseSpeed = 25f;        // 기본 전진 속도 (m/s)
    public float minSpeed = 5f;         // 감속 하한
    public float maxSpeed = 80f;        // 가속 상한
    public float accelPerSec = 15f;      // B 누르는 동안 가속량 (m/s^2)
    public float decelPerSec = 20f;      // A 누르는 동안 감속량 (m/s^2)
    public float returnRate = 12f;      // 버튼을 놓았을 때 baseSpeed로 복귀 속도 (m/s^2)
    public float speedAlignRate = 6f;    // 현재 velocity를 기수 방향으로 맞추는 보정

    private float currentSpeed;

    // ---- Attitude (tilt) ----
    [Header("Tilt Torques")]
    public float pitchPower = 3500f;     // 피치
    public float rollPower = 3000f;     // 롤
    public float yawAssistWithSpeed = 0.0f; // 시험버전: Yaw 미사용(원하면 값 올리기)

    [Header("Damping / Comfort")]
    public float pitchDamp = 0.15f;
    public float rollDamp = 0.12f;
    public float yawDamp = 0.10f;
    public float autoLevel = 0.2f;       // 롤 자동 복원
    public float maxRollDegrees = 60f;   // 롤 제한

#if ENABLE_INPUT_SYSTEM
    // ---- Input Actions ----
    [Header("Input Actions (auto-create)")]
    public InputAction stickAction;   // 왼손 스틱 (Vector2)
    public InputAction accelAction;   // 오른손 B (buttonEast)
    public InputAction decelAction;   // 오른손 A (buttonSouth)
#endif

    [Header("Debug")]
    public bool enableKeyboardFallback = true; // 에디터용 백업 입력

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (centerOfMass)
            rb.centerOfMass = transform.InverseTransformPoint(centerOfMass.position);

        currentSpeed = Mathf.Clamp(baseSpeed, minSpeed, maxSpeed);
    }

#if ENABLE_INPUT_SYSTEM
    void OnEnable()
    {
        // 액션이 없거나 바인딩이 0개면 새로 만들어 바인딩 추가
        if (stickAction == null || stickAction.bindings.Count == 0)
        {
            stickAction = new InputAction("LeftStick", InputActionType.Value, expectedControlType: "Vector2");
            stickAction.AddBinding("<XRController>{LeftHand}/thumbstick");
            stickAction.AddBinding("<OculusTouchController>{LeftHand}/thumbstick");
        }
        if (accelAction == null || accelAction.bindings.Count == 0)
        {
            accelAction = new InputAction("RightB", InputActionType.Button);
            accelAction.AddBinding("<XRController>{RightHand}/buttonEast");          // B
            accelAction.AddBinding("<OculusTouchController>{RightHand}/buttonEast");
        }
        if (decelAction == null || decelAction.bindings.Count == 0)
        {
            decelAction = new InputAction("RightA", InputActionType.Button);
            decelAction.AddBinding("<XRController>{RightHand}/buttonSouth");         // A
            decelAction.AddBinding("<OculusTouchController>{RightHand}/buttonSouth");
        }

        stickAction.Enable();
        accelAction.Enable();
        decelAction.Enable();
    }

    void OnDisable()
    {
        // Enable/Disable 만 관리 (Dispose는 원할 때 직접)
        if (stickAction != null) stickAction.Disable();
        if (accelAction != null) accelAction.Disable();
        if (decelAction != null) decelAction.Disable();
    }
#endif

    // -------- INPUT CACHE (Update) --------
    Vector2 _stick; bool _accelHeld; bool _decelHeld;

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        _stick = Vector2.zero;
        _accelHeld = _decelHeld = false;

        if (stickAction != null && stickAction.enabled) _stick = stickAction.ReadValue<Vector2>();
        if (accelAction != null && accelAction.enabled) _accelHeld = accelAction.IsPressed();
        if (decelAction != null && decelAction.enabled) _decelHeld = decelAction.IsPressed();
#endif
        // 키보드 백업 (WASD / R/F)
        if (enableKeyboardFallback)
        {
            float sx = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            float sy = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
            if (Mathf.Abs(sx) > Mathf.Abs(_stick.x) || Mathf.Abs(sy) > Mathf.Abs(_stick.y))
                _stick = new Vector2(sx, sy);

            _accelHeld |= Input.GetKey(KeyCode.R);
            _decelHeld |= Input.GetKey(KeyCode.F);
        }
    }

    // -------- PHYSICS (FixedUpdate) --------
    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // 1) 속도 업데이트
        if (_accelHeld && !_decelHeld)
            currentSpeed = Mathf.Min(maxSpeed, currentSpeed + accelPerSec * dt);
        else if (_decelHeld && !_accelHeld)
            currentSpeed = Mathf.Max(minSpeed, currentSpeed - decelPerSec * dt);
        else
            currentSpeed = MoveToward(currentSpeed, baseSpeed, returnRate * dt);

        // 2) 스틱 → 기울기 입력
        float rollIn = Mathf.Clamp(_stick.x, -1f, 1f);   // 좌(-)/우(+)
        float pitchIn = Mathf.Clamp(-_stick.y, -1f, 1f);  // 앞(+Y)밀면 코내림 -> 피치는 음수

        // 3) 토크 계산/적용
        Vector3 angVel = rb.angularVelocity;
        Vector3 localAng = transform.InverseTransformDirection(angVel);

        Vector3 torque =
            (transform.right * (pitchIn * pitchPower)) +
            (transform.forward * (-rollIn * rollPower)) +
            (transform.up * (0f * (0f + rb.linearVelocity.magnitude * yawAssistWithSpeed)));

        // 감쇠
        torque += -transform.right * (localAng.x * pitchDamp * rb.mass);
        torque += -transform.forward * (localAng.z * rollDamp * rb.mass);
        torque += -transform.up * (localAng.y * yawDamp * rb.mass);

        // 롤 자동 레벨링
        Vector3 flatRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        float rollTilt = Vector3.SignedAngle(flatRight, transform.right, transform.forward) * Mathf.Deg2Rad;
        torque += -transform.forward * (rollTilt * autoLevel * rb.mass);

        rb.AddTorque(torque);

        // 롤 제한
        float rollDeg = Mathf.DeltaAngle(0f, transform.rotation.eulerAngles.z);
        if (Mathf.Abs(rollDeg) > maxRollDegrees)
        {
            float excess = Mathf.Sign(rollDeg) * (Mathf.Abs(rollDeg) - maxRollDegrees);
            rb.AddTorque(transform.forward * -excess * 20f);
        }

        // 4) 전진 속도 적용 (기수 방향 정렬)
        Vector3 targetVel = transform.forward * currentSpeed;
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVel, 1f - Mathf.Exp(-speedAlignRate * dt));
    }

    static float MoveToward(float v, float target, float maxDelta)
    {
        if (v < target) return Mathf.Min(target, v + maxDelta);
        if (v > target) return Mathf.Max(target, v - maxDelta);
        return target;
    }
}
