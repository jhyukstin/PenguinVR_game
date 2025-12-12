using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class CockpitStickDirectDrive : MonoBehaviour
{
    [Header("Refs")]
    public PlaneController plane;
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;
    public Transform stickBone;          // Armature 안 스틱 Bone
    public Transform visualRootToLock;   // 보통 this.transform (Interactable 고정용)

    [Header("Stick Limits (degrees)")]
    public float maxPitchDeg = 25f;  // 앞/뒤
    public float maxRollDeg = 25f;  // 좌/우
    public float smoothing = 20f;

    // 고정(콕핏 장치가 움직이지 않게)
    Vector3 _lockLocalPos;
    Quaternion _lockLocalRot;
    Transform _lockParent;

    // 입력 계산용
    UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor _interactor;
    Quaternion _boneNeutral;
    Vector2 _out;

    void Reset()
    {
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        visualRootToLock = transform;
    }

    void Awake()
    {
        if (!grab) grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (!visualRootToLock) visualRootToLock = transform;

        _lockParent = visualRootToLock.parent;
        _lockLocalPos = visualRootToLock.localPosition;
        _lockLocalRot = visualRootToLock.localRotation;

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
    }

    void OnRelease(SelectExitEventArgs args)
    {
        _interactor = null;
        _out = Vector2.zero;
        if (plane) plane.SetCockpitStick(Vector2.zero);

        // 스틱 시각 중립 복귀(원하면)
        if (stickBone) stickBone.localRotation = _boneNeutral;

        // 장치 고정 복구
        LockBack();
    }

    void LateUpdate()
    {
        // 항상 장치가 자리에서 안 움직이게 고정
        LockBack();

        if (!plane || !stickBone || _interactor == null) return;

        // Direct Grab이면 interactor의 attachTransform이 실제 "손잡이" 근처에 있음
        var attach = _interactor.GetAttachTransform(grab);
        if (!attach) return;

        // 손(attach)의 local 방향으로 입력 계산:
        // StickInteractable 기준에서 손이 어디로 기울어졌는지로 pitch/roll 뽑기
        Vector3 localHandDir = transform.InverseTransformDirection(attach.forward);
        // forward 기준으로 pitch/roll 만들기(간단/안정)
        float pitch = Mathf.Atan2(-localHandDir.y, localHandDir.z) * Mathf.Rad2Deg; // 위/아래
        float roll = Mathf.Atan2(localHandDir.x, localHandDir.z) * Mathf.Rad2Deg; // 좌/우

        pitch = Mathf.Clamp(pitch, -maxPitchDeg, maxPitchDeg);
        roll = Mathf.Clamp(roll, -maxRollDeg, maxRollDeg);

        // Bone 회전(시각)
        stickBone.localRotation = _boneNeutral * Quaternion.Euler(pitch, 0f, -roll);

        // Plane 입력 (-1~1)
        float pitchN = pitch / maxPitchDeg;
        float rollN = roll / maxRollDeg;

        Vector2 target = new Vector2(rollN, -pitchN); // PlaneController 규칙 맞춤
        _out = Vector2.Lerp(_out, target, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
        plane.SetCockpitStick(_out);
    }

    void LockBack()
    {
        if (!visualRootToLock) return;
        if (visualRootToLock.parent != _lockParent) visualRootToLock.SetParent(_lockParent, false);
        visualRootToLock.localPosition = _lockLocalPos;
        visualRootToLock.localRotation = _lockLocalRot;
    }
}
