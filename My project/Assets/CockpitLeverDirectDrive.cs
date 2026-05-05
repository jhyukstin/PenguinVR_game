using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRGrabInteractable))]
public class CockpitLeverDirectDrive : MonoBehaviour
{
    [Header("Refs")]
    public PlaneController plane;
    public XRGrabInteractable grab;
    public Transform leverBone;

    [Header("Lever Base (reference)")]
    public Transform leverBase;
    public Vector3 localAxis = Vector3.forward;

    [Header("Along Range (meters, local)")]
    public float minAlong = -0.12f;
    public float maxAlong = 0.12f;

    [Header("Smoothing")]
    public float smoothing = 15f;

    [Header("Visual Rotation (bone)")]
    public Vector3 leverRotAxis = Vector3.right;
    public float minDeg = -30f;
    public float maxDeg = 30f;

    [Header("SFX")]
    [SerializeField] private AudioSource accelerateAudioSource;
    [SerializeField] private AudioSource decelerateAudioSource;
    [SerializeField] private AudioClip accelerateSFX;
    [SerializeField] private AudioClip decelerateSFX;
    [SerializeField] private float sfxThreshold = 0.06f;
    [SerializeField] private float sfxCooldown = 0.2f;
    [SerializeField] private bool invertSFXDirection = false;

    [Header("Lock Interactable Transform")]
    public bool lockThisTransform = true;

    Transform _lockParent;
    Vector3 _lockLocalPos;
    Quaternion _lockLocalRot;

    IXRSelectInteractor _interactor;
    Quaternion _boneNeutral;
    float _outT = 0.5f;

    float _lastSfxTime = -999f;
    int _lastDirection = 0; // 1 = accelerate, -1 = decelerate

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();

        if (!plane)
            plane = GetComponentInParent<PlaneController>() ?? FindObjectOfType<PlaneController>();

        if (!leverBase)
            leverBase = transform;

        if (!accelerateAudioSource)
            accelerateAudioSource = GetComponent<AudioSource>();

        _lockParent = transform.parent;
        _lockLocalPos = transform.localPosition;
        _lockLocalRot = transform.localRotation;

        if (leverBone)
            _boneNeutral = leverBone.localRotation;
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

        if (leverBone)
            _boneNeutral = leverBone.localRotation;

        _lastDirection = 0;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        _interactor = null;
        _lastDirection = 0;
        StopThrottleSFX();
        LockBack();
    }

    void LateUpdate()
    {
        if (lockThisTransform)
            LockBack();

        if (!plane || !leverBone || _interactor == null)
            return;

        Transform attach = _interactor.GetAttachTransform(grab);
        if (!attach)
            return;

        Vector3 axis = localAxis.normalized;

        Vector3 localHandPos = leverBase.InverseTransformPoint(attach.position);
        float along = Vector3.Dot(localHandPos, axis);

        float lo = Mathf.Min(minAlong, maxAlong);
        float hi = Mathf.Max(minAlong, maxAlong);
        along = Mathf.Clamp(along, lo, hi);

        float t = Mathf.InverseLerp(minAlong, maxAlong, along);

        float previousT = _outT;

        _outT = Mathf.Lerp(
            _outT,
            t,
            1f - Mathf.Exp(-smoothing * Time.deltaTime)
        );

        plane.SetThrottle01(_outT);

        PlayThrottleSFX(previousT, _outT);

        float deg = Mathf.Lerp(minDeg, maxDeg, _outT);
        leverBone.localRotation = _boneNeutral * Quaternion.AngleAxis(deg, leverRotAxis.normalized);
    }

    void PlayThrottleSFX(float previousT, float currentT)
    {
        float delta = currentT - previousT;

        if (Mathf.Abs(delta) < sfxThreshold)
            return;

        if (Time.time - _lastSfxTime < sfxCooldown)
            return;

        int direction = delta > 0f ? 1 : -1;

        if (invertSFXDirection)
            direction *= -1;

        if (direction == _lastDirection)
            return;

        if (direction > 0)
        {
            if (decelerateAudioSource)
                decelerateAudioSource.Stop();

            if (accelerateAudioSource && accelerateSFX)
            {
                accelerateAudioSource.Stop();
                accelerateAudioSource.clip = accelerateSFX;
                accelerateAudioSource.Play();
            }
        }
        else
        {
            if (accelerateAudioSource)
                accelerateAudioSource.Stop();

            if (decelerateAudioSource && decelerateSFX)
            {
                decelerateAudioSource.Stop();
                decelerateAudioSource.clip = decelerateSFX;
                decelerateAudioSource.Play();
            }
        }

        _lastDirection = direction;
        _lastSfxTime = Time.time;
    }

    void StopThrottleSFX()
    {
        if (accelerateAudioSource)
            accelerateAudioSource.Stop();

        if (decelerateAudioSource)
            decelerateAudioSource.Stop();
    }

    void LockBack()
    {
        if (_lockParent && transform.parent != _lockParent)
            transform.SetParent(_lockParent, false);

        transform.localPosition = _lockLocalPos;
        transform.localRotation = _lockLocalRot;
    }
}