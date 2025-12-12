using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class CockpitLeverDirectDrive : MonoBehaviour
{
    [Header("Refs")]
    public PlaneController plane;
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;
    public Transform leverBone;          // Armature 안 레버 Bone
    public Transform visualRootToLock;   // 보통 this.transform

    [Header("Pull/Push Mapping")]
    public Transform leverBase;          // 기준점(없으면 this.transform)
    public Vector3 localAxis = Vector3.forward; // 밀고/당기는 축(leverBase 로컬)
    public float minAlong = -0.06f;      // 당김(감속)
    public float maxAlong = 0.06f;      // 밀기(가속)
    public float smoothing = 15f;

    [Header("Lever Visual Rotation (optional)")]
    public Vector3 leverRotAxis = Vector3.right; // 레버가 도는 축(leverBone 로컬)
    public float minDeg = -30f;                  // 당김 각도
    public float maxDeg = 30f;                  // 밀기 각도

    Transform _lockParent;
    Vector3 _lockLocalPos;
    Quaternion _lockLocalRot;

    UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor _interactor;
    Quaternion _boneNeutral;
    float _outT;

    void Reset()
    {
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        visualRootToLock = transform;
        leverBase = transform;
    }

    void Awake()
    {
        if (!grab) grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (!visualRootToLock) visualRootToLock = transform;
        if (!leverBase) leverBase = transform;

        _lockParent = visualRootToLock.parent;
        _lockLocalPos = visualRootToLock.localPosition;
        _lockLocalRot = visualRootToLock.localRotation;

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
    }

    void OnRelease(SelectExitEventArgs args)
    {
        _interactor = null;
        LockBack();
        // 레버는 보통 "현재 스로틀 유지"가 자연스러움 → throttle01 유지
        // 원하면 중립 복귀하려면 여기서 SetThrottle01(0.5f) 같은 거 하면 됨
    }

    void LateUpdate()
    {
        LockBack();

        if (!plane || !leverBone || _interactor == null) return;

        var attach = _interactor.GetAttachTransform(grab);
        if (!attach) return;

        // leverBase 기준으로 손이 축 방향으로 얼마나 이동했는지(당김/밀기)
        Vector3 axis = localAxis.normalized;
        Vector3 localHandPos = leverBase.InverseTransformPoint(attach.position);
        float along = Vector3.Dot(localHandPos, axis);

        // clamp & normalize
        along = Mathf.Clamp(along, Mathf.Min(minAlong, maxAlong), Mathf.Max(minAlong, maxAlong));
        float t = Mathf.InverseLerp(minAlong, maxAlong, along); // 0..1

        _outT = Mathf.Lerp(_outT, t, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
        plane.SetThrottle01(_outT);

        // 레버 시각 회전(선택): throttle에 따라 bone 회전
        float deg = Mathf.Lerp(minDeg, maxDeg, _outT);
        leverBone.localRotation = _boneNeutral * Quaternion.AngleAxis(deg, leverRotAxis.normalized);
    }

    void LockBack()
    {
        if (!visualRootToLock) return;
        if (visualRootToLock.parent != _lockParent) visualRootToLock.SetParent(_lockParent, false);
        visualRootToLock.localPosition = _lockLocalPos;
        visualRootToLock.localRotation = _lockLocalRot;
    }
}
