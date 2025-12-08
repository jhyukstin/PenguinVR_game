using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class VRThrottle : MonoBehaviour
{
    public XRGrabInteractable grab;
    public Transform startPoint; // value = 0
    public Transform endPoint;   // value = 1
    public float lerpSpeed = 15f;

    public float Value { get; private set; }

    void Awake()
    {
        if (!grab) grab = GetComponent<XRGrabInteractable>();
    }

    // ---- XRI event handlers ----
    void OnHoverEntered(HoverEnterEventArgs args)
        => Debug.Log($"[Throttle] Hover enter by {args.interactorObject?.transform?.name}");

    void OnHoverExited(HoverExitEventArgs args)
        => Debug.Log($"[Throttle] Hover exit by {args.interactorObject?.transform?.name}");

    void OnSelectEntered(SelectEnterEventArgs args)
        => Debug.Log($"[Throttle] GRABBED by {args.interactorObject?.transform?.name}");

    void OnSelectExited(SelectExitEventArgs args)
        => Debug.Log($"[Throttle] RELEASED by {args.interactorObject?.transform?.name}");

    void OnActivated(ActivateEventArgs args)
        => Debug.Log($"[Throttle] Activated (trigger) by {args.interactorObject?.transform?.name}");

    void OnDeactivated(DeactivateEventArgs args)
        => Debug.Log($"[Throttle] Deactivated (trigger) by {args.interactorObject?.transform?.name}");

    void Update()
    {
        Vector3 a = startPoint.position;
        Vector3 b = endPoint.position;
        Vector3 p = transform.position;

        float len = Vector3.Distance(a, b);
        if (len < 0.0001f) return;

        float t = Vector3.Dot(p - a, (b - a).normalized) / len;
        t = Mathf.Clamp01(t);

        float k = 1f - Mathf.Exp(-lerpSpeed * Time.deltaTime);
        Value = Mathf.Lerp(Value, t, k);

        // snap visual to rail
        transform.position = Vector3.Lerp(transform.position, Vector3.Lerp(a, b, Value), k);
    }
}
