using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BulletShoot : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform shootingPoint;
    public float shootForce = 20f;
    public float rotationForce = 20f;

    [Header("Grab")]
    [SerializeField] private XRGrabInteractable grabInteractable;

    [Header("SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootSFX;
    [SerializeField] private AudioClip pickupSFX;

    private InputSystem_Actions input;
    private bool isHeld;

    void Awake()
    {
        input = new InputSystem_Actions();

        if (!grabInteractable)
            grabInteractable = GetComponent<XRGrabInteractable>();

        if (!audioSource)
            audioSource = GetComponent<AudioSource>();

        if (grabInteractable)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }
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

    private void OnDestroy()
    {
        if (grabInteractable)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isHeld = true;

        if (audioSource && pickupSFX)
        {
            audioSource.PlayOneShot(pickupSFX);
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isHeld = false;
    }

    private void OnShoot(InputAction.CallbackContext ctx)
    {
        if (!isHeld)
            return;

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