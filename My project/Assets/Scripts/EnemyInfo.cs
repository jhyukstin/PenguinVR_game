using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyInfo : MonoBehaviour
{
    public float enemyHP = 20f;
    public float enemySpeed = 1.0f;

    [SerializeField] private string winSceneName = "WinCutScene";
    public bool win;

    private void Start()
    {
        win = false;
    }
    public void TakeDamage(int damageAmount)
    {
        enemyHP -= damageAmount;

        if (enemyHP <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (win)
            return;

        win = true;
        Debug.Log("Player Wins");

        if (string.IsNullOrWhiteSpace(winSceneName))
        {
            Debug.LogWarning("EnemyInfo: No win scene name assigned.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(winSceneName))
        {
            Debug.LogError($"EnemyInfo: Scene '{winSceneName}' is not in Build Settings.");
            return;
        }

        SceneManager.LoadScene(winSceneName);
    }
}
