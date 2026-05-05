using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;
using System.Collections;

public class SplineEndSceneLoader : MonoBehaviour
{
    public SplineAnimate splineAnimate;

    [Header("Scene")]
    public string sceneName = "NextScene";
    public float delay = 10f;

    bool hasTriggered = false;

    void Update()
    {
        if (hasTriggered || splineAnimate == null)
            return;

        // spline 진행도 (0 ~ 1)
        float t = splineAnimate.NormalizedTime;

        if (t >= 1f)
        {
            hasTriggered = true;
            StartCoroutine(LoadSceneAfterDelay());
        }
    }

    IEnumerator LoadSceneAfterDelay()
    {
        yield return new WaitForSeconds(delay);

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"Scene '{sceneName}' not in Build Settings.");
            yield break;
        }

        SceneManager.LoadScene(sceneName);
    }
}