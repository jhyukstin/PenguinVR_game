using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRGrabInteractable))]
public class CockpitStickDirectDrive : MonoBehaviour
{
    [Header("Refs")]
    public PlaneController plane;     // 비워도 됨(자동으로 찾음)
    public XRGrabInteractable grab;
    public Transform stickBone;       // Armature 안 stick bone

    [Header("Stick Limits (degrees)")]
    public float maxPitchDeg = 25f;
    public float maxRollDeg = 25f;
    public float smoothing = 20f;

    [Header("Position Driven Pitch")]
    public bool useControllerPositionForPitch = true;
    public float pitchTravelMeters = 0.12f;
    public bool invertPitchPosition;

    [Header("Lock Interactable Transform")]
    public bool lockThisTransform = true;

    Transform _lockParent;
    Vector3 _lockLocalPos;
    Quaternion _lockLocalRot;

    IXRSelectInteractor _interactor;
    Quaternion _boneNeutral;
    Vector2 _out;
    float _grabStartLocalZ;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();

        if (!plane)
            plane = GetComponentInParent<PlaneController>() ?? FindObjectOfType<PlaneController>();

        _lockParent = transform.parent;
        _lockLocalPos = transform.localPosition;
        _lockLocalRot = transform.localRotation;

        if (stickBone) _boneNeutral = stickBone.localRotation;
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
        if (stickBone) _boneNeutral = stickBone.localRotation;
        _grabStartLocalZ = GetInteractorLocalPosition().z;

        if (Time.frameCount % 10 == 0)
            Debug.Log($"[StickDrive:{name}] GRAB -> Plane={(plane ? plane.name : "NULL")}");
    }

    void OnRelease(SelectExitEventArgs args)
    {
        _interactor = null;
        _out = Vector2.zero;

        if (plane) plane.SetCockpitStick(Vector2.zero);
        if (stickBone) stickBone.localRotation = _boneNeutral;

        LockBack();

        if (Time.frameCount % 10 == 0)
            Debug.Log($"[StickDrive:{name}] RELEASE -> Plane={(plane ? plane.name : "NULL")}");
    }

    void LateUpdate()
    {
        if (lockThisTransform) LockBack();
        if (!plane || !stickBone || _interactor == null) return;

        Transform attach = _interactor.GetAttachTransform(grab);
        if (!attach) return;

        // attach.forward 방향을 로컬로 변환해서 pitch/roll 계산(안정형)
        Vector3 localHandDir = transform.InverseTransformDirection(attach.forward);

        float pitchN;
        if (useControllerPositionForPitch)
        {
            float localZDelta = GetInteractorLocalPosition().z - _grabStartLocalZ;
            pitchN = Mathf.Clamp(localZDelta / Mathf.Max(0.001f, pitchTravelMeters), -1f, 1f);

            if (invertPitchPosition)
                pitchN = -pitchN;
        }
        else
        {
            float wristPitch = Mathf.Atan2(localHandDir.y, localHandDir.z) * Mathf.Rad2Deg;
            pitchN = Mathf.Clamp(wristPitch / Mathf.Max(0.001f, maxPitchDeg), -1f, 1f);
        }

        float roll = Mathf.Atan2(localHandDir.x, localHandDir.z) * Mathf.Rad2Deg;

        roll = Mathf.Clamp(roll, -maxRollDeg, maxRollDeg);

        float pitch = pitchN * maxPitchDeg;
        stickBone.localRotation = _boneNeutral * Quaternion.Euler(pitch, 0f, -roll);

        float rollN = roll / Mathf.Max(0.001f, maxRollDeg);

        Vector2 target = new Vector2(rollN, -pitchN);
        _out = Vector2.Lerp(_out, target, 1f - Mathf.Exp(-smoothing * Time.deltaTime));

        plane.SetCockpitStick(_out);

        if (Time.frameCount % 30 == 0)
            Debug.Log($"[StickDrive:{name}] -> Plane={plane.name} out={_out}");
    }

    Vector3 GetInteractorLocalPosition()
    {
        if (_interactor == null || _interactor.transform == null)
            return Vector3.zero;

        return transform.InverseTransformPoint(_interactor.transform.position);
    }

    void LockBack()
    {
        if (_lockParent && transform.parent != _lockParent)
            transform.SetParent(_lockParent, false);

        transform.localPosition = _lockLocalPos;
        transform.localRotation = _lockLocalRot;
    }
}
