using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ThrottleLeverObjectXR_ParentLock : MonoBehaviour
{
    [Header("Refs")]
    public PlaneController plane;
    public Transform leverBone;       // LeverObject/Armature 안 Lever 담당 Bone
    public Transform grabProxy;       // 보통 자기 자신
    public XRGrabInteractable grab;

    [Header("Rotation Axis (local)")]
    public Vector3 localAxis = Vector3.right; // 레버 회전축
    public float minDeg = -30f;               // 당김(감속)
    public float maxDeg = 30f;               // 밀기(가속)

    [Header("Tuning")]
    public float smoothing = 15f;
    public bool clampVisualAngle = true;

    [Header("Parent Lock")]
    public bool forceParentLockEveryFrame = true;

    Transform _originalParent;
    Vector3 _originalLocalPos;
    Quaternion _originalLocalRot;

    Quaternion _boneNeutral;
    Quaternion _proxyNeutral;
    float _outT;

    UnityEngine.Events.UnityAction<SelectEnterEventArgs> _onEnter;
    UnityEngine.Events.UnityAction<SelectExitEventArgs> _onExit;

    void Reset()
    {
        grabProxy = transform;
        grab = GetComponent<XRGrabInteractable>();
    }

    void Awake()
    {
        if (!grabProxy) grabProxy = transform;
        if (!grab) grab = GetComponent<XRGrabInteractable>();

        _originalParent = transform.parent;
        _originalLocalPos = transform.localPosition;
        _originalLocalRot = transform.localRotation;

        CacheNeutral();
    }

    void OnEnable()
    {
        if (!grab) return;

        _onEnter = (args) => { CacheNeutral(); };
        _onExit = (args) => { RestoreParentAndPose(); };

        grab.selectEntered.AddListener(_onEnter);
        grab.selectExited.AddListener(_onExit);
    }

    void OnDisable()
    {
        if (!grab) return;

        if (_onEnter != null) grab.selectEntered.RemoveListener(_onEnter);
        if (_onExit != null) grab.selectExited.RemoveListener(_onExit);
    }

    void CacheNeutral()
    {
        if (leverBone) _boneNeutral = leverBone.localRotation;
        if (grabProxy) _proxyNeutral = grabProxy.localRotation;
    }

    void RestoreParentAndPose()
    {
        if (_originalParent == null) return;

        transform.SetParent(_originalParent, false);
        transform.localPosition = _originalLocalPos;
        transform.localRotation = _originalLocalRot;

        CacheNeutral();
    }

    void LateUpdate()
    {
        if (!plane || !leverBone || !grabProxy) return;

        if (forceParentLockEveryFrame && transform.parent != _originalParent)
        {
            if (grab == null || !grab.isSelected)
                RestoreParentAndPose();
        }

        Quaternion rel = Quaternion.Inverse(_proxyNeutral) * grabProxy.localRotation;

        Vector3 e = rel.eulerAngles;
        Vector3 a = localAxis.normalized;

        float ax = Mathf.Abs(a.x), ay = Mathf.Abs(a.y), az = Mathf.Abs(a.z);
        float rawDeg;
        if (ax >= ay && ax >= az) rawDeg = Signed180(e.x) * Mathf.Sign(a.x);
        else if (ay >= ax && ay >= az) rawDeg = Signed180(e.y) * Mathf.Sign(a.y);
        else rawDeg = Signed180(e.z) * Mathf.Sign(a.z);

        float clamped = rawDeg;
        if (clampVisualAngle)
        {
            float lo = Mathf.Min(minDeg, maxDeg);
            float hi = Mathf.Max(minDeg, maxDeg);
            clamped = Mathf.Clamp(rawDeg, lo, hi);
        }

        leverBone.localRotation = _boneNeutral * Quaternion.AngleAxis(clamped, a);

        float t = Mathf.InverseLerp(minDeg, maxDeg, clamped);
        _outT = Mathf.Lerp(_outT, t, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
        plane.SetThrottle01(_outT);
    }

    static float Signed180(float deg)
    {
        deg %= 360f;
        if (deg > 180f) deg -= 360f;
        return deg;
    }
}
