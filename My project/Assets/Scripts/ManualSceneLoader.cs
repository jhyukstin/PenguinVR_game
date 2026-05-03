using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ManualSceneLoader : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string sceneToLoad;

    [Header("Auto Load")]
    [SerializeField] private GameObject failUI;
    [SerializeField] private bool loadAfterFailUIAppears = true;
    [SerializeField] private float delayAfterFailUIAppears = 5f;

    private bool hasStartedLoading;

    private void Update()
    {
        if (!loadAfterFailUIAppears || hasStartedLoading || failUI == null)
        {
            return;
        }

        if (failUI.activeInHierarchy)
        {
            LoadSceneAfterDelay(delayAfterFailUIAppears);
        }
    }

    public void LoadSceneNow()
    {
        if (hasStartedLoading)
        {
            return;
        }

        hasStartedLoading = true;
        LoadAssignedScene();
    }

    public void LoadSceneAfterDelay(float delay)
    {
        if (hasStartedLoading)
        {
            return;
        }

        hasStartedLoading = true;
        StartCoroutine(LoadSceneRoutine(delay));
    }

    private IEnumerator LoadSceneRoutine(float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        LoadAssignedScene();
    }

    private void LoadAssignedScene()
    {
        if (string.IsNullOrWhiteSpace(sceneToLoad))
        {
            Debug.LogWarning("ManualSceneLoader: No scene name assigned.");
            hasStartedLoading = false;
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneToLoad))
        {
            Debug.LogError($"ManualSceneLoader: Scene '{sceneToLoad}' is not in Build Settings.");
            hasStartedLoading = false;
            return;
        }

        SceneManager.LoadScene(sceneToLoad);
    }
}
