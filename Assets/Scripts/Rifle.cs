using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Rifle : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Camera cam; // optional, will fall back to Camera.main

    [Header("Animations")]
    [SerializeField]
    private Animator animator;
    private readonly string firingBoolName = "isFiring";

    [SerializeField]
    private string upperBodyLayerName = "UpperBody";
    private int upperBodyLayerIndex;

    [SerializeField]
    private ParticleSystem muzzleFlash;

    [SerializeField]
    private GameObject hitEffectPrefab; // TODO: Object pooling

    [SerializeField]
    private LayerMask hitMask = ~0;

    [Header("Damage & Range")]
    [SerializeField]
    private float damage = 10f;

    [SerializeField]
    private float range = 100f;

    [Header("Fire Settings")]
    [Tooltip("Bullets per second when holding fire.")]
    [SerializeField]
    private float fireRate = 10f;

    [Tooltip("If true: hold to fire. If false: one shot per click.")]
    [SerializeField]
    private bool fullAuto = true;

    [Header("Ammo")]
    [Tooltip("Bullets per magazine.")]
    [SerializeField]
    private int magazineSize = 30;

    [Tooltip("Total bullets in reserve (not loaded).")]
    [SerializeField]
    private int reserveAmmo = 90;

    [SerializeField]
    private float reloadTime = 2f;

    // --- State ---
    private int ammoInMag;
    private bool isReloading;
    private float nextFireTime;
    private bool wasPressedLastFrame; // For semi-auto
    private Coroutine reloadCoroutine; // For interruptible reloads

    // --- UI / Events ---
    public event System.Action OnAmmoChanged;
    public event System.Action OnReloadStarted;
    public event System.Action OnReloadComplete;
    public event System.Action OnGunFired;

    // --- Public Read-only Info ---
    public int AmmoInMag => ammoInMag;
    public int ReserveAmmo => reserveAmmo;
    public bool IsReloading => isReloading;

    private void Awake()
    {
        if (!cam)
            cam = Camera.main;

        fireRate = Mathf.Max(0.01f, fireRate);
        range = Mathf.Max(0f, range);
        magazineSize = Mathf.Max(0, magazineSize);
        reserveAmmo = Mathf.Max(0, reserveAmmo);
        ammoInMag = magazineSize;

        if (animator != null)
            upperBodyLayerIndex = animator.GetLayerIndex(upperBodyLayerName);
    }

    private void Start()
    {
        OnAmmoChanged?.Invoke();
    }

    private void Update()
    {
        var mouse = Mouse.current;
        var keyboard = Keyboard.current;
        if (mouse == null || keyboard == null)
            return;

        if (isReloading)
            return;

        // --- Input Polling ---
        bool fireHeld = mouse.leftButton.isPressed;
        bool firePressedThisFrame = fireHeld && !wasPressedLastFrame;
        bool wantsToFire = fullAuto ? fireHeld : firePressedThisFrame;

        // visual firing state (looping shoot anim)
        bool canVisuallyFire = fireHeld && !isReloading && ammoInMag > 0;

        if (animator != null)
        {
            // 1) drive firing loop
            animator.SetBool(firingBoolName, canVisuallyFire);

            // 2) upper body layer active when aiming OR firing OR reloading
            if (upperBodyLayerIndex >= 0)
            {
                // read Aim bool that other script sets on right mouse
                bool isAiming = animator.GetBool("Aim");
                bool upperBodyActive = isAiming || canVisuallyFire || isReloading;
                float targetWeight = upperBodyActive ? 1f : 0f;
                float currentWeight = animator.GetLayerWeight(upperBodyLayerIndex);
                float newWeight = Mathf.MoveTowards(
                    currentWeight,
                    targetWeight,
                    Time.deltaTime * 10f
                );
                animator.SetLayerWeight(upperBodyLayerIndex, newWeight);
            }
        }

        // --- Shooting Logic ---
        if (wantsToFire && Time.time >= nextFireTime)
        {
            TryShoot();
        }

        // --- Reloading Logic ---
        if (keyboard.rKey.wasPressedThisFrame)
        {
            TryStartReload();
        }

        wasPressedLastFrame = fireHeld;
    }

    private void OnDisable()
    {
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }

        isReloading = false;
    }

    private Camera GetActiveCamera()
    {
        // Prefer explicitly assigned camera
        if (cam != null)
            return cam;

        // Fallback to currently active MainCamera
        return Camera.main;
    }

    private void TryShoot()
    {
        if (ammoInMag <= 0)
        {
            Debug.Log("Magazine empty.");
            TryStartReload(); // Auto-reload attempt
            return;
        }

        Camera activeCam = GetActiveCamera();
        if (!activeCam)
        {
            Debug.LogError($"{nameof(Rifle)} on {name} has no active camera for shooting.");
            return;
        }

        nextFireTime = Time.time + 1f / fireRate;
        ammoInMag = Mathf.Max(0, ammoInMag - 1);
        OnAmmoChanged?.Invoke();
        OnGunFired?.Invoke();

        if (muzzleFlash != null)
            muzzleFlash.Play();

        Ray ray = new Ray(activeCam.transform.position, activeCam.transform.forward);

        if (
            Physics.Raycast(ray, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore)
        )
        {
            Debug.Log($"Hit: {hit.transform.name}");

            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }

            if (hit.transform.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.ApplyDamage(damage);
            }
            else if (hit.transform.TryGetComponent<Health>(out var health))
            {
                // Backwards compatibility with Health-only targets
                health.ObjectHitDamage(damage);
            }
        }
    }

    private void TryStartReload()
    {
        if (isReloading || reloadCoroutine != null)
            return;

        if (ammoInMag >= magazineSize)
            return; // Already full

        if (reserveAmmo <= 0)
        {
            Debug.Log("No reserve ammo left.");
            return;
        }

        reloadCoroutine = StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        OnReloadStarted?.Invoke();
        Debug.Log("Reloading...");

        if (animator != null)
        {
            // stop firing loop
            animator.SetBool(firingBoolName, false);

            // ensure upper-body layer is fully active during reload
            if (upperBodyLayerIndex >= 0)
                animator.SetLayerWeight(upperBodyLayerIndex, 1f);

            // play reload animation
            animator.SetTrigger("Reload");
        }

        yield return new WaitForSeconds(reloadTime); // match this to your reload clip length

        int bulletsNeeded = magazineSize - ammoInMag;
        int bulletsToLoad = Mathf.Min(bulletsNeeded, reserveAmmo);

        ammoInMag += bulletsToLoad;
        reserveAmmo -= bulletsToLoad;

        isReloading = false;
        reloadCoroutine = null;
        OnReloadComplete?.Invoke();
        OnAmmoChanged?.Invoke();

        Debug.Log($"Reloaded. Mag: {ammoInMag}/{magazineSize}, Reserve: {reserveAmmo}");
    }

    // Call this from a pickup script when player collects ammo
    public void AddAmmo(int amount)
    {
        if (amount <= 0)
            return;

        reserveAmmo += amount;
        OnAmmoChanged?.Invoke();
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Rifle : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Camera cam; // optional, will fall back to Camera.main

    [Header("Animations")]
    [SerializeField]
    private Animator animator;
    private readonly string firingBoolName = "isFiring";

    [SerializeField]
    private string upperBodyLayerName = "UpperBody";
    private int upperBodyLayerIndex;

    [SerializeField]
    private ParticleSystem muzzleFlash;

    [SerializeField]
    private GameObject hitEffectPrefab; // TODO: Object pooling

    [SerializeField]
    private LayerMask hitMask = ~0;

    [Header("Damage & Range")]
    [SerializeField]
    private float damage = 10f;

    [SerializeField]
    private float range = 100f;

    [Header("Fire Settings")]
    [Tooltip("Bullets per second when holding fire.")]
    [SerializeField]
    private float fireRate = 10f;

    [Tooltip("If true: hold to fire. If false: one shot per click.")]
    [SerializeField]
    private bool fullAuto = true;

    [Header("Ammo")]
    [Tooltip("Bullets per magazine.")]
    [SerializeField]
    private int magazineSize = 30;

    [Tooltip("Total bullets in reserve (not loaded).")]
    [SerializeField]
    private int reserveAmmo = 90;

    [SerializeField]
    private float reloadTime = 2f;

    // --- State ---
    private int ammoInMag;
    private bool isReloading;
    private float nextFireTime;
    private bool wasPressedLastFrame; // For semi-auto
    private Coroutine reloadCoroutine; // For interruptible reloads

    // --- UI / Events ---
    public event System.Action OnAmmoChanged;
    public event System.Action OnReloadStarted;
    public event System.Action OnReloadComplete;
    public event System.Action OnGunFired;

    // --- Public Read-only Info ---
    public int AmmoInMag => ammoInMag;
    public int ReserveAmmo => reserveAmmo;
    public bool IsReloading => isReloading;

    private void Awake()
    {
        if (!cam)
            cam = Camera.main;

        fireRate = Mathf.Max(0.01f, fireRate);
        range = Mathf.Max(0f, range);
        magazineSize = Mathf.Max(0, magazineSize);
        reserveAmmo = Mathf.Max(0, reserveAmmo);
        ammoInMag = magazineSize;

        if (animator != null)
            upperBodyLayerIndex = animator.GetLayerIndex(upperBodyLayerName);
    }

    private void Start()
    {
        OnAmmoChanged?.Invoke();
    }

    private void Update()
    {
        var mouse = Mouse.current;
        var keyboard = Keyboard.current;
        if (mouse == null || keyboard == null)
            return;

        if (isReloading)
            return;

        // --- Input Polling ---
        bool fireHeld = mouse.leftButton.isPressed;
        bool firePressedThisFrame = fireHeld && !wasPressedLastFrame;
        bool wantsToFire = fullAuto ? fireHeld : firePressedThisFrame;

        // visual firing state (looping shoot anim)
        bool canVisuallyFire = fireHeld && !isReloading && ammoInMag > 0;

        if (animator != null)
        {
            // 1) drive firing loop
            animator.SetBool(firingBoolName, canVisuallyFire);

            // 2) upper body layer active when aiming OR firing OR reloading
            if (upperBodyLayerIndex >= 0)
            {
                // read Aim bool that other script sets on right mouse
                bool isAiming = animator.GetBool("Aim");
                bool upperBodyActive = isAiming || canVisuallyFire || isReloading;
                float targetWeight = upperBodyActive ? 1f : 0f;
                float currentWeight = animator.GetLayerWeight(upperBodyLayerIndex);
                float newWeight = Mathf.MoveTowards(
                    currentWeight,
                    targetWeight,
                    Time.deltaTime * 10f
                );
                animator.SetLayerWeight(upperBodyLayerIndex, newWeight);
            }
        }

        // --- Shooting Logic ---
        if (wantsToFire && Time.time >= nextFireTime)
        {
            TryShoot();
        }

        // --- Reloading Logic ---
        if (keyboard.rKey.wasPressedThisFrame)
        {
            TryStartReload();
        }

        wasPressedLastFrame = fireHeld;
    }

    private void OnDisable()
    {
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }

        isReloading = false;
    }

    private Camera GetActiveCamera()
    {
        // Prefer explicitly assigned camera
        if (cam != null)
            return cam;

        // Fallback to currently active MainCamera
        return Camera.main;
    }

    private void TryShoot()
    {
        if (ammoInMag <= 0)
        {
            Debug.Log("Magazine empty.");
            TryStartReload(); // Auto-reload attempt
            return;
        }

        Camera activeCam = GetActiveCamera();
        if (!activeCam)
        {
            Debug.LogError($"{nameof(Rifle)} on {name} has no active camera for shooting.");
            return;
        }

        nextFireTime = Time.time + 1f / fireRate;
        ammoInMag = Mathf.Max(0, ammoInMag - 1);
        OnAmmoChanged?.Invoke();
        OnGunFired?.Invoke();

        if (muzzleFlash != null)
            muzzleFlash.Play();

        Ray ray = new Ray(activeCam.transform.position, activeCam.transform.forward);

        if (
            Physics.Raycast(ray, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore)
        )
        {
            Debug.Log($"Hit: {hit.transform.name}");

            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }

            if (hit.transform.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.ApplyDamage(damage);
            }
            else if (hit.transform.TryGetComponent<Health>(out var health))
            {
                // Backwards compatibility with Health-only targets
                health.ObjectHitDamage(damage);
            }
        }
    }

    private void TryStartReload()
    {
        if (isReloading || reloadCoroutine != null)
            return;

        if (ammoInMag >= magazineSize)
            return; // Already full

        if (reserveAmmo <= 0)
        {
            Debug.Log("No reserve ammo left.");
            return;
        }

        reloadCoroutine = StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        OnReloadStarted?.Invoke();
        Debug.Log("Reloading...");

        if (animator != null)
        {
            // stop firing loop
            animator.SetBool(firingBoolName, false);

            // ensure upper-body layer is fully active during reload
            if (upperBodyLayerIndex >= 0)
                animator.SetLayerWeight(upperBodyLayerIndex, 1f);

            // play reload animation
            animator.SetTrigger("Reload");
        }

        yield return new WaitForSeconds(reloadTime); // match this to your reload clip length

        int bulletsNeeded = magazineSize - ammoInMag;
        int bulletsToLoad = Mathf.Min(bulletsNeeded, reserveAmmo);

        ammoInMag += bulletsToLoad;
        reserveAmmo -= bulletsToLoad;

        isReloading = false;
        reloadCoroutine = null;
        OnReloadComplete?.Invoke();
        OnAmmoChanged?.Invoke();

        Debug.Log($"Reloaded. Mag: {ammoInMag}/{magazineSize}, Reserve: {reserveAmmo}");
    }

    // Call this from a pickup script when player collects ammo
    public void AddAmmo(int amount)
    {
        if (amount <= 0)
            return;

        reserveAmmo += amount;
        OnAmmoChanged?.Invoke();
    }
}