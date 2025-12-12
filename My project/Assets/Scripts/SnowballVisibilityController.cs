using UnityEngine;

public class SnowballVisibilityController : MonoBehaviour
{
    public float minVisibleDistance = 0.25f; // 25 cm from the camera

    private Transform playerCamera;
    private Renderer[] renderers;

    void Start()
    {
        playerCamera = Camera.main.transform;

        // Disable all renderers initially
        renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
            r.enabled = false;
    }

    void Update()
    {
        // Check distance from camera
        float distance = Vector3.Distance(transform.position, playerCamera.position);

        // Enable rendering once it's far enough
        if (distance > minVisibleDistance)
        {
            foreach (Renderer r in renderers)
                r.enabled = true;

            // Optional: remove script after making visible
            Destroy(this);
        }
    }
}
