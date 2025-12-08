using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class VRStickInput : MonoBehaviour
{
    public XRGrabInteractable grab;
    public Transform neutralPose;
    public float maxPitchDeflection = 20f; // degrees forward/back
    public float maxRollDeflection = 25f; // degrees left/right
    public float axisLerp = 12f;

    public float Pitch { get; private set; } // +up / -down
    public float Roll { get; private set; } // +right wing down

    Quaternion _neutralRot;
    bool _held;

    void Awake()
    {
        if (!grab) grab = GetComponent<XRGrabInteractable>();
        _neutralRot = neutralPose ? neutralPose.rotation : transform.rotation;
        grab.selectEntered.AddListener(_ => _held = true);
        grab.selectExited.AddListener(_ => _held = false);
    }

    void Update()
    {
        Quaternion delta = Quaternion.Inverse(_neutralRot) * transform.rotation;
        delta.ToAngleAxis(out float angle, out Vector3 axis);
        var e = (Quaternion.AngleAxis(angle, axis)).eulerAngles;
        e.x = Mathf.DeltaAngle(0, e.x);
        e.z = Mathf.DeltaAngle(0, e.z);

        float rawPitch = Mathf.Clamp(-e.x / maxPitchDeflection, -1f, 1f);
        float rawRoll = Mathf.Clamp(-e.z / maxRollDeflection, -1f, 1f);
        if (!_held) { rawPitch = 0f; rawRoll = 0f; }

        float t = 1f - Mathf.Exp(-axisLerp * Time.deltaTime);
        Pitch = Mathf.Lerp(Pitch, rawPitch, t);
        Roll = Mathf.Lerp(Roll, rawRoll, t);
    }
}
