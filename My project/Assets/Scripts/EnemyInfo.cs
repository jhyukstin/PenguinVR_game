using UnityEngine;

public class EnemyInfo : MonoBehaviour
{
    public float enemyHP = 20f;
    public float enemySpeed = 1.0f;
    public GameObject WinScreen;
    public bool win;

    private void Start()
    {
        win = false;

        if (WinScreen)
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
        if (win)
            return;

        win = true;
        Debug.Log("Player Wins");

        if (WinScreen)
            WinScreen.SetActive(true);
    }
}
