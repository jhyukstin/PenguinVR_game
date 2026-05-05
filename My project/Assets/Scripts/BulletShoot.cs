using UnityEngine;
using UnityEngine.InputSystem;

public class BulletShoot : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform shootingPoint;
    public float shootForce = 20f;
    public float rotationForce = 20f;

    [Header("SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootSFX;

    private InputSystem_Actions input;

    void Awake()
    {
        input = new InputSystem_Actions();
    }

    void OnEnable()
    {
        input.Player.Attack.performed += OnShoot;
        input.Player.Enable();
    }

    void OnDisable()
    {
        input.Player.Attack.performed -= OnShoot;
        input.Player.Disable();
    }

    private void OnShoot(InputAction.CallbackContext ctx)
    {
        Shoot();
    }

    void Shoot()
    {
        GameObject projectile = Instantiate(
            bulletPrefab,
            shootingPoint.position,
            shootingPoint.rotation
        );

        Rigidbody rb = projectile.GetComponent<Rigidbody>();

        if (rb)
        {
            rb.linearVelocity = shootingPoint.forward * shootForce;
            rb.angularVelocity = Random.insideUnitSphere * rotationForce;
        }

        if (audioSource && shootSFX)
        {
            audioSource.PlayOneShot(shootSFX);
        }
    }
}