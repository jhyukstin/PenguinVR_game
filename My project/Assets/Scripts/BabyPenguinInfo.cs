using UnityEngine;

public class BabyPenguinInfo : MonoBehaviour
{
    public float penguinHP = 3f;
    public GameObject babyPenguinLoseScreen;

    private void Start()
    {
        babyPenguinLoseScreen.SetActive(false);
    }
    public void Damage(int damageAmount)
    {
        penguinHP -= damageAmount;

        if (penguinHP <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Baby Penguin Died Player Lost");
        babyPenguinLoseScreen.SetActive(true);
    }
}
