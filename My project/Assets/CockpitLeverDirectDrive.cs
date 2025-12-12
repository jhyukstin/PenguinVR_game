using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRGrabInteractable))]
public class CockpitLeverDirectDrive : MonoBehaviour
{
    [Header("Refs")]
    public PlaneController plane;     // 비워도 됨(자동 찾기)
    public XRGrabInteractable grab;
    public Transform leverBone;       // Armature 안 lever bone

    [Header("Lever Base (reference)")]
    public Transform leverBase;       // 보통 이 오브젝트 or 레버 베이스
    public Vector3 localAxis = Vector3.forward; // 당김/밀기 축 (leverBase 로컬)

    [Header("Along Range (meters, local)")]
    public float minAlong = -0.12f;   // 당김(감속)
    public float maxAlong = 0.12f;    // 밀기(가속)

    [Header("Smoothing")]
    public float smoothing = 15f;

    [Header("Visual Rotation (bone)")]
    public Vector3 leverRotAxis = Vector3.right;
    public float minDeg = -30f;
    public float maxDeg = 30f;

    [Header("Lock Interactable Transform")]
    public bool lockThisTransform = true;

    Transform _lockParent;
    Vector3 _lockLocalPos;
    Quaternion _lockLocalRot;

    IXRSelectInteractor _interactor;
    Quaternion _boneNeutral;
    float _outT = 0.5f;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();

        if (!plane)
            plane = GetComponentInParent<PlaneController>() ?? FindObjectOfType<PlaneController>();

        if (!leverBase) leverBase = transform;

        _lockParent = transform.parent;
        _lockLocalPos = transform.localPosition;
        _lockLocalRot = transform.localRotation;

        if (leverBone) _boneNeutral = leverBone.localRotation;
    }

    void OnEnable()
    {
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void OnDisable()
    {
        grab.selectEntered.RemoveListener(OnGrab);
        grab.selectExited.RemoveListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        _interactor = args.interactorObject;
        if (leverBone) _boneNeutral = leverBone.localRotation;

        if (Time.frameCount % 10 == 0)
            Debug.Log($"[LeverDrive:{name}] GRAB -> Plane={(plane ? plane.name : "NULL")}");
    }

    void OnRelease(SelectExitEventArgs args)
    {
        _interactor = null;
        LockBack();

        if (Time.frameCount % 10 == 0)
            Debug.Log($"[LeverDrive:{name}] RELEASE -> Plane={(plane ? plane.name : "NULL")}");
    }

    void LateUpdate()
    {
        if (lockThisTransform) LockBack();
        if (!plane || !leverBone || _interactor == null) return;

        Transform attach = _interactor.GetAttachTransform(grab);
        if (!attach) return;

        Vector3 axis = localAxis.normalized;

        // leverBase 로컬에서 손 위치를 측정해서 축방향으로 얼마나 밀/당겼는지 계산
        Vector3 localHandPos = leverBase.InverseTransformPoint(attach.position);
        float along = Vector3.Dot(localHandPos, axis);

        float lo = Mathf.Min(minAlong, maxAlong);
        float hi = Mathf.Max(minAlong, maxAlong);
        along = Mathf.Clamp(along, lo, hi);

        float t = Mathf.InverseLerp(minAlong, maxAlong, along);

        _outT = Mathf.Lerp(_outT, t, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
        plane.SetThrottle01(_outT);

        float deg = Mathf.Lerp(minDeg, maxDeg, _outT);
        leverBone.localRotation = _boneNeutral * Quaternion.AngleAxis(deg, leverRotAxis.normalized);

        if (Time.frameCount % 30 == 0)
            Debug.Log($"[LeverDrive:{name}] -> Plane={plane.name} throttle01={_outT:F2} (along={along:F3})");
    }

    void LockBack()
    {
        if (_lockParent && transform.parent != _lockParent)
            transform.SetParent(_lockParent, false);

        transform.localPosition = _lockLocalPos;
        transform.localRotation = _lockLocalRot;
    }
}
