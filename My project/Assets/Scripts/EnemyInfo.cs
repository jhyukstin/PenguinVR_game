using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class EnemyInfo : MonoBehaviour
{
    public float enemyHP = 20f;
    public float enemySpeed = 1.0f;

    [SerializeField] private string winSceneName = "WinCutScene";
    public bool win;

    [Header("SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hitSFX;
    [SerializeField] private AudioClip deathSFX;

    private void Start()
    {
        win = false;

        if (!audioSource)
            audioSource = GetComponent<AudioSource>();
    }

    public void TakeDamage(int damageAmount)
    {
        enemyHP -= damageAmount;

        if (audioSource && hitSFX)
        {
            audioSource.PlayOneShot(hitSFX);
        }
        else
        {
            Debug.LogWarning("EnemyInfo: Missing audioSource or hitSFX.");
        }

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

        StartCoroutine(PlayDeathThenLoadScene());
    }

    IEnumerator PlayDeathThenLoadScene()
    {
        if (audioSource && deathSFX)
        {
            audioSource.PlayOneShot(deathSFX);
            yield return new WaitForSeconds(deathSFX.length);
        }

        if (string.IsNullOrWhiteSpace(winSceneName))
        {
            Debug.LogWarning("EnemyInfo: No win scene name assigned.");
            yield break;
        }

        if (!Application.CanStreamedLevelBeLoaded(winSceneName))
        {
            Debug.LogError($"EnemyInfo: Scene '{winSceneName}' is not in Build Settings.");
            yield break;
        }

        SceneManager.LoadScene(winSceneName);
    }
}