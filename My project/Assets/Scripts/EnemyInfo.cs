using UnityEngine;

public class EnemyInfo : MonoBehaviour
{
    public float enemyHP = 20f;
    public float enemySpeed = 1.0f;
    public GameObject WinScreen;

    private void Start()
    {
        WinScreen.SetActive(false);
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
        Debug.Log("Player Wins");
        WinScreen.SetActive(true);
    }
}
