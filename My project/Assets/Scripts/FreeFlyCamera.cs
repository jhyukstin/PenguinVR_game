using UnityEngine;

/// <summary>
/// FreeFlyCamera
/// - WASD : 좌/우/앞/뒤
/// - Q/E  : 아래/위
/// - Mouse : 시점 회전 (Pitch/Yaw)
/// - Shift : 스프린트(속도 증가)
/// - Esc  : 커서 해제, 클릭하면 다시 잠금
/// - 옵션: 마우스 클릭을 눌렀을 때만 회전하도록 토글 가능
/// </summary>
[AddComponentMenu("Camera/Free Fly Camera")]
public class FreeFlyCamera : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;            // 기본 이동 속도
    public float sprintMultiplier = 2.5f;    // Shift 누를 때 곱해지는 값
    public bool useAcceleration = true;      // 가속/감속 사용 여부
    public float acceleration = 10f;         // 가속 정도 (높을수록 즉시 속도)
    public float deceleration = 10f;         // 감속 정도

    [Header("Vertical")]
    public KeyCode ascendKey = KeyCode.E;    // 위로
    public KeyCode descendKey = KeyCode.Q;   // 아래로

    [Header("Mouse Look")]
    public float mouseSensitivity = 6.0f;    // 마우스 민감도
    public bool invertY = false;             // Y축 반전
    public bool requireRightMouseToLook = false; // 오른쪽 버튼 누르고 있을 때만 회전
    public float smoothing = 0.0f;           // 회전 스무딩 (0 = 즉시)

    [Header("Limits")]
    public float minPitch = -89f;
    public float maxPitch = 89f;

    [Header("Cursor")]
    public bool lockCursorOnStart = true;

    // 내부 상태
    Vector2 currentRotation;    // (yaw, pitch)
    Vector2 rotationVelocity;   // 스무딩용
    Vector3 currentVelocity;    // 이동 스무딩용 (가속 사용 시)
    Camera cam;

    void Start()
    {
        cam = GetComponentInChildren<Camera>();
        Vector3 e = transform.eulerAngles;
        currentRotation.x = e.y; // yaw
        currentRotation.y = e.x; // pitch

        if (lockCursorOnStart)
            LockCursor(true);
    }

    void Update()
    {
        HandleCursorLockToggle();
        HandleMouseLook();
        HandleMovement();
    }

    void HandleCursorLockToggle()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            LockCursor(false);
        if (Input.GetMouseButtonDown(0) && !Cursor.lockState.Equals(CursorLockMode.Locked))
            LockCursor(true);
    }

    void LockCursor(bool locked)
    {
        if (locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void HandleMouseLook()
    {
        if (requireRightMouseToLook && !Input.GetMouseButton(1))
            return;

        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");

        float invert = invertY ? 1f : -1f;

        currentRotation.x += mx * mouseSensitivity;
        currentRotation.y += my * mouseSensitivity * invert;
        currentRotation.y = Mathf.Clamp(currentRotation.y, minPitch, maxPitch);

        if (smoothing > 0f)
        {
            // 스무딩 적용 (간단한 Lerp 기반)
            Vector3 targetEuler = new Vector3(currentRotation.y, currentRotation.x, 0f);
            Vector3 newEuler = Vector3.Lerp(transform.eulerAngles, targetEuler, Time.deltaTime * (1f / Mathf.Max(0.0001f, smoothing)));
            transform.eulerAngles = newEuler;
        }
        else
        {
            transform.eulerAngles = new Vector3(currentRotation.y, currentRotation.x, 0f);
        }
    }

    void HandleMovement()
    {
        // 입력
        float inputX = Input.GetAxisRaw("Horizontal"); // A/D
        float inputZ = Input.GetAxisRaw("Vertical");   // W/S

        float up = 0f;
        if (Input.GetKey(ascendKey)) up += 1f;
        if (Input.GetKey(descendKey)) up -= 1f;

        // 로컬 이동 방향 (카메라 기준)
        Vector3 localDirection = new Vector3(inputX, up, inputZ).normalized;

        // 속도
        float targetSpeed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            targetSpeed *= sprintMultiplier;

        Vector3 targetVelocity = transform.TransformDirection(localDirection) * targetSpeed;

        if (useAcceleration)
        {
            // 가속/감속 처리
            // 가속 시에는 targetVelocity로 선형 보간(가속), 감속 시 deceleration 사용
            if (localDirection.sqrMagnitude > 0.001f)
            {
                currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, Time.deltaTime * acceleration);
            }
            else
            {
                currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, Time.deltaTime * deceleration);
            }

            transform.position += currentVelocity * Time.deltaTime;
        }
        else
        {
            transform.position += targetVelocity * Time.deltaTime;
        }
    }

#if UNITY_EDITOR
    // 인스펙터에서 값 변경 시 에디터 플레이 중 초기 회전 동기화
    void OnValidate()
    {
        minPitch = Mathf.Clamp(minPitch, -89f, 89f);
        maxPitch = Mathf.Clamp(maxPitch, -89f, 89f);
        if (maxPitch < minPitch) maxPitch = minPitch;
    }
#endif
}
