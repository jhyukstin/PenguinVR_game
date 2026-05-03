using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class PlaneCrashSceneLoader : MonoBehaviour
{
    [Header("Crash Target")]
    [SerializeField] private string wallParentName = "Walls";
    [SerializeField] private string wallNamePrefix = "Ice Wall";

    [Header("Scene")]
    [SerializeField] private string sceneToLoad = "Cutscene";
    [SerializeField] private float loadDelay = 0f;

    [Header("Crash Behavior")]
    [SerializeField] private Collider crashCollider;
    [SerializeField] private bool stopPlaneOnCrash = true;
    [SerializeField] private bool disablePlaneControllerOnCrash = true;
    [SerializeField] private bool checkOverlapFallback = true;
    [SerializeField] private bool debugLog = true;

    private bool hasCrashed;
    private readonly Collider[] overlapResults = new Collider[32];

    private void Awake()
    {
        if (crashCollider == null)
        {
            crashCollider = GetComponent<Collider>();
        }

        if (debugLog)
        {
            Debug.Log($"PlaneCrashSceneLoader ready on {name}. Crash collider: {(crashCollider ? crashCollider.name : "None")}");
        }
    }

    private void FixedUpdate()
    {
        if (!checkOverlapFallback || hasCrashed || crashCollider == null)
        {
            return;
        }

        Vector3 center = crashCollider.bounds.center;
        Vector3 halfExtents = crashCollider.bounds.extents;
        Quaternion rotation = Quaternion.identity;

        if (crashCollider is BoxCollider boxCollider)
        {
            center = boxCollider.transform.TransformPoint(boxCollider.center);
            halfExtents = Vector3.Scale(boxCollider.size * 0.5f, Abs(boxCollider.transform.lossyScale));
            rotation = boxCollider.transform.rotation;
        }

        int hitCount = Physics.OverlapBoxNonAlloc(
            center,
            halfExtents,
            overlapResults,
            rotation,
            ~0,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = overlapResults[i];

            if (hit == null || hit == crashCollider || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            TryHandleCrash(hit, "overlap");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryHandleCrash(collision.collider, "collision");
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHandleCrash(other, "trigger");
    }

    private void TryHandleCrash(Collider other, string source)
    {
        if (hasCrashed || other == null)
        {
            return;
        }

        if (debugLog)
        {
            Debug.Log($"PlaneCrashSceneLoader saw {source} with {other.name}");
        }

        if (!IsIceWall(other.transform))
        {
            return;
        }

        hasCrashed = true;
        Debug.Log($"Plane crashed into {other.name}. Loading scene: {sceneToLoad}");

        if (disablePlaneControllerOnCrash && TryGetComponent(out PlaneController planeController))
        {
            planeController.enabled = false;
        }

        if (stopPlaneOnCrash && TryGetComponent(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        StartCoroutine(LoadSceneAfterDelay());
    }

    private bool IsIceWall(Transform hitTransform)
    {
        for (Transform current = hitTransform; current != null; current = current.parent)
        {
            if (current.name == wallParentName)
            {
                return true;
            }

            if (current.name.StartsWith(wallNamePrefix))
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerator LoadSceneAfterDelay()
    {
        if (loadDelay > 0f)
        {
            yield return new WaitForSeconds(loadDelay);
        }

        if (string.IsNullOrWhiteSpace(sceneToLoad))
        {
            Debug.LogWarning("PlaneCrashSceneLoader: No scene name set.");
            yield break;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneToLoad))
        {
            Debug.LogError($"PlaneCrashSceneLoader: Scene '{sceneToLoad}' is not in Build Settings yet.");
            yield break;
        }

        SceneManager.LoadScene(sceneToLoad);
    }

    private static Vector3 Abs(Vector3 value)
    {
        return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }
}
