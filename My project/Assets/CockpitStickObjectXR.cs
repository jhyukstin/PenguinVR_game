using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class CockpitStickObjectXR_ParentLock : MonoBehaviour
{
    [Header("Refs")]
    public PlaneController plane;
    public Transform stickBone;       // StickObject/Armature 안 Stick 담당 Bone
    public Transform grabProxy;       // 보통 자기 자신
    public XRGrabInteractable grab;

    [Header("Mapping")]
    public float maxPitchDeg = 25f;
    public float maxRollDeg = 25f;
    public float smoothing = 20f;
    public bool clampVisualAngles = true;

    [Header("Parent Lock")]
    public bool forceParentLockEveryFrame = true; // 루트로 튀면 즉시 복귀

    // saved pose
    Transform _originalParent;
    Vector3 _originalLocalPos;
    Quaternion _originalLocalRot;

    Quaternion _boneNeutral;
    Quaternion _proxyNeutral;
    Vector2 _out;

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

        _onEnter = (args) =>
        {
            CacheNeutral();
        };

        _onExit = (args) =>
        {
            Release();
            RestoreParentAndPose();
        };

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
        if (stickBone) _boneNeutral = stickBone.localRotation;
        if (grabProxy) _proxyNeutral = grabProxy.localRotation;
    }

    void Release()
    {
        _out = Vector2.zero;
        if (plane) plane.SetCockpitStick(Vector2.zero);
    }

    void RestoreParentAndPose()
    {
        if (_originalParent == null) return;

        // 강제로 원래 부모로 되돌리기
        transform.SetParent(_originalParent, false);
        transform.localPosition = _originalLocalPos;
        transform.localRotation = _originalLocalRot;

        // 다음 grab 대비해서 neutral 재캐시
        CacheNeutral();
    }

    void LateUpdate()
    {
        if (!plane || !stickBone || !grabProxy) return;

        // (안전장치) 부모가 씬 루트로 튀면 즉시 복구
        if (forceParentLockEveryFrame && transform.parent != _originalParent)
        {
            // 잡고 있는 동안에는 XRI가 잠깐 부모를 바꿀 수 있어서,
            // "선택 중이 아닐 때만" 되돌리는 게 안전함
            if (grab == null || !grab.isSelected)
                RestoreParentAndPose();
        }

        // 조이스틱 입력 처리
        Quaternion rel = Quaternion.Inverse(_proxyNeutral) * grabProxy.localRotation;
        Quaternion desiredBone = _boneNeutral * rel;

        if (clampVisualAngles)
        {
            Vector3 e = (Quaternion.Inverse(_boneNeutral) * desiredBone).eulerAngles;
            float pitch = Mathf.Clamp(Signed180(e.x), -maxPitchDeg, maxPitchDeg);
            float roll = Mathf.Clamp(Signed180(e.z), -maxRollDeg, maxRollDeg);
            desiredBone = _boneNeutral * Quaternion.Euler(pitch, 0f, roll);
        }

        stickBone.localRotation = desiredBone;

        Vector3 ang = (Quaternion.Inverse(_boneNeutral) * stickBone.localRotation).eulerAngles;
        float pitchDeg = Signed180(ang.x);
        float rollDeg = Signed180(ang.z);

        float pitchN = Mathf.Clamp(pitchDeg / maxPitchDeg, -1f, 1f);
        float rollN = Mathf.Clamp(rollDeg / maxRollDeg, -1f, 1f);

        Vector2 target = new Vector2(rollN, -pitchN);
        _out = Vector2.Lerp(_out, target, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
        plane.SetCockpitStick(_out);
    }

    static float Signed180(float deg)
    {
        deg %= 360f;
        if (deg > 180f) deg -= 360f;
        return deg;
    }
}
