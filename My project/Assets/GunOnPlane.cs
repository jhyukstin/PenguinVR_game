using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class GunOnPlane : MonoBehaviour
{
    Rigidbody rb;
    UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        // Resting on plane = follow plane, no physics forces
        rb.isKinematic = true;

        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        // When grabbed, let physics help XR toolkit move it
        rb.isKinematic = false;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        // When released, snap back to plane and stick on it again
        rb.isKinematic = true;
        // Optional: reset local position/rotation so it goes back to its slot
        // transform.localPosition = originalLocalPos;
        // transform.localRotation = originalLocalRot;
    }
}
