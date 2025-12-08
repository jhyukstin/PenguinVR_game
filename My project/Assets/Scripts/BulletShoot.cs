using UnityEngine;
using UnityEngine.InputSystem;

public class BulletShoot : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform shootingPoint;
    public float shootForce = 20f;
    public float rotationForce = 20f;

    private InputSystem_Actions input;

    void Awake()
    {
        input = new InputSystem_Actions();
    }

    void OnEnable()
    {
        input.Player.ShootTest.performed += OnShoot;
        input.Player.Enable();
    }

    void OnDisable()
    {
        input.Player.ShootTest.performed -= OnShoot;
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
        rb.linearVelocity = shootingPoint.forward * shootForce;
        rb.angularVelocity = Random.insideUnitSphere * rotationForce;
    }
}
