using Unity.VisualScripting;
using UnityEngine;

public class SnowballDamage : MonoBehaviour
{
    public GameObject prefabToSpawn;

    public float destroyDelay = 0f;

    public int damage = 1;

    private void OnCollisionEnter(Collision collision)
    {
       
        // Enemy HP decrease
        EnemyInfo enemy = collision.gameObject.GetComponent<EnemyInfo>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }

        // Baby penguin damage
        BabyPenguinInfo babyPenguin = collision.gameObject.GetComponent<BabyPenguinInfo>();
        if (babyPenguin != null)
        {
            babyPenguin.Damage(damage);
        }

        //Spawn Particle Effect
        ContactPoint contact = collision.GetContact(0);
        Vector3 spawnPos = contact.point;
        Quaternion spawnRot = Quaternion.LookRotation(contact.normal);
        Instantiate(prefabToSpawn, spawnPos, spawnRot);

        Destroy(gameObject, destroyDelay);
    }
}
