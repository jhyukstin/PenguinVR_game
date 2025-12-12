using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChangeOnAwake : MonoBehaviour
{
    [SerializeField] private string sceneToLoad;

    void Awake()
    {
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogWarning("SceneChangeOnAwake: No scene name set!");
        }
    }
}