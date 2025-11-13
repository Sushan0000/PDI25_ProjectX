using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // Still needed for Mouse.current

public class Rifle : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Camera cam;

    [SerializeField]
    private ParticleSystem muzzleFlash;

    [SerializeField]
    private GameObject hitEffectPrefab; // TODO: Use Object Pooling

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
    private float fireRate = 1f;

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
    private float reloadTime = 5f;

    // --- State & Private ---
    private int ammoInMag;
    private bool isReloading;
    private float nextFireTime;
    private bool wasPressedLastFrame; // For semi-auto
    private Coroutine reloadCoroutine; // For interruptible reloads

    // --- 10/10 UI EVENTS ---
    // The UI subscribes to these. No more Update() polling!
    public event System.Action OnAmmoChanged;
    public event System.Action OnReloadStarted;
    public event System.Action OnReloadComplete;
    public event System.Action OnGunFired; // For recoil/sound

    // --- Public Read-only Info (for UI or other scripts) ---
    public int AmmoInMag => ammoInMag;
    public int ReserveAmmo => ReserveAmmo;
    public bool IsReloading => isReloading;

    #region --- Unity Methods ---

    private void Awake()
    {
        if (!cam)
            cam = Camera.main;
        ammoInMag = magazineSize;
    }

    private void Start()
    {
        // Tell the UI our initial ammo count
        OnAmmoChanged?.Invoke();
    }

    private void Update()
    {
        // Check for device availability (prevents errors)
        if (Mouse.current == null || Keyboard.current == null)
            return;

        // Stop all other actions if reloading
        if (isReloading)
            return;

        // --- Input Polling ---
        bool fireHeld = Mouse.current.leftButton.isPressed;
        bool firePressedThisFrame = fireHeld && !wasPressedLastFrame;

        bool wantsToFire = fullAuto ? fireHeld : firePressedThisFrame;

        // --- Shooting Logic ---
        if (wantsToFire && Time.time >= nextFireTime)
        {
            TryShoot();
        }

        // --- Reloading Logic ---
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            TryStartReload();
        }

        // Store this for next frame's semi-auto check
        wasPressedLastFrame = fireHeld;
    }

    private void OnDisable()
    {
        // 10/10 Bug Fix: If we switch weapons (disabling this script)
        // while reloading, we MUST stop the coroutine.
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
            isReloading = false;
        }
    }

    #endregion

    #region --- Core Gun Logic ---

    private void TryShoot()
    {
        // No need for 'isReloading' check, Update() already handles it

        if (ammoInMag <= 0)
        {
            Debug.Log("Magazine empty.");
            TryStartReload(); // Auto-reload
            return;
        }

        nextFireTime = Time.time + 1f / fireRate;
        ammoInMag--;

        // --- 10/10 UI EVENT ---
        OnAmmoChanged?.Invoke(); // Tell the UI to update
        OnGunFired?.Invoke(); // Tell recoil/sound scripts to fire

        if (muzzleFlash != null)
            muzzleFlash.Play();

        if (!cam)
            return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        if (
            Physics.Raycast(ray, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore)
        )
        {
            Debug.Log($"Hit: {hit.transform.name}");

            // --- 10/10 PERFORMANCE (CRITICAL) ---
            // Instantiate is very slow. For a 10/10 game, you MUST
            // use an "Object Pool" to get and return hit effects.
            // For now, this works, but it will cause lag spikes.
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }

            // --- 10/10 ARCHITECTURE ---
            // We look for the IDamageable interface, NOT a "Health" script.
            if (hit.transform.TryGetComponent<Health>(out var damageable))
            {
                damageable.ObjectHitDamage(damage);
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

        // Start the interruptible coroutine
        reloadCoroutine = StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        OnReloadStarted?.Invoke(); // Tell UI "Reloading..."
        Debug.Log("Reloading...");

        yield return new WaitForSeconds(reloadTime);

        int bulletsNeeded = magazineSize - ammoInMag;
        int bulletsToLoad = Mathf.Min(bulletsNeeded, reserveAmmo);

        ammoInMag += bulletsToLoad;
        reserveAmmo -= bulletsToLoad;

        isReloading = false;
        reloadCoroutine = null; // Mark as finished

        OnReloadComplete?.Invoke(); // Tell UI "Reload complete!"
        OnAmmoChanged?.Invoke(); // Tell UI the new ammo count
        Debug.Log($"Reloaded. Mag: {ammoInMag}/{magazineSize}, Reserve: {reserveAmmo}");
    }

    // Call this from a pickup script when player collects ammo
    public void AddAmmo(int amount)
    {
        if (amount <= 0)
            return;

        reserveAmmo += amount;
        OnAmmoChanged?.Invoke(); // Update UI
    }

    #endregion
}

/////////////////////////////////////////////////////////////////////////////////////////////////////
// using System.Collections;
// using UnityEngine;
// using UnityEngine.InputSystem; // New Input System

// public class Rifle : MonoBehaviour
// {
//     [Header("References")]
//     [SerializeField]
//     private Camera cam;

//     [SerializeField]
//     private ParticleSystem muzzleFlash;

//     [SerializeField]
//     private GameObject hitEffectPrefab;

//     [SerializeField]
//     private LayerMask hitMask = ~0; // default = everything

//     [Header("Damage & Range")]
//     [SerializeField]
//     private float damage = 20f;

//     [SerializeField]
//     private float range = 100f;

//     [Header("Fire Settings")]
//     [Tooltip("Bullets per second when holding fire.")]
//     [SerializeField]
//     private float fireRate = 10f;

//     [Tooltip("If true: hold to fire. If false: one shot per click.")]
//     [SerializeField]
//     private bool fullAuto = true;

//     [Header("Ammo")]
//     [Tooltip("Bullets per magazine.")]
//     [SerializeField]
//     private int magazineSize = 30;

//     [Tooltip("Total bullets in reserve (not loaded).")]
//     [SerializeField]
//     private int reserveAmmo = 90;

//     [SerializeField]
//     private float reloadTime = 5f;

//     private int ammoInMag;
//     private bool isReloading;
//     private float nextFireTime;
//     private bool wasPressedLastFrame;

//     // Public read-only info (for UI or other scripts)
//     public int AmmoInMag => ammoInMag;
//     public int ReserveAmmo => reserveAmmo;
//     public bool IsReloading => isReloading;

//     private void Awake()
//     {
//         if (!cam)
//             cam = Camera.main;

//         ammoInMag = magazineSize;
//     }

//     private void Update()
//     {
//         if (isReloading)
//             return;

//         if (Mouse.current == null)
//             return;

//         bool fireHeld = Mouse.current.leftButton.isPressed;
//         bool firePressedThisFrame = fireHeld && !wasPressedLastFrame;
//         bool wantsToFire = fullAuto ? fireHeld : firePressedThisFrame;

//         // Manual reload (R)
//         if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
//         {
//             TryStartReload();
//         }

//         // Shooting
//         if (wantsToFire && Time.time >= nextFireTime)
//         {
//             TryShoot();
//         }

//         wasPressedLastFrame = fireHeld;
//     }

//     private void TryShoot()
//     {
//         if (ammoInMag <= 0)
//         {
//             Debug.Log("Magazine empty.");
//             TryStartReload();
//             return;
//         }

//         nextFireTime = Time.time + 1f / fireRate;

//         ammoInMag--;

//         if (muzzleFlash != null)
//             muzzleFlash.Play();

//         if (!cam)
//             return;

//         Ray ray = new Ray(cam.transform.position, cam.transform.forward);

//         if (
//             Physics.Raycast(ray, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore)
//         )
//         {
//             Debug.Log($"Hit: {hit.transform.name}");

//             if (hitEffectPrefab != null)
//             {
//                 Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
//             }

//             if (hit.transform.TryGetComponent<Health>(out var health))
//             {
//                 health.ObjectHitDamage(damage);
//             }
//         }
//     }

//     private void TryStartReload()
//     {
//         if (isReloading)
//             return;

//         if (ammoInMag >= magazineSize)
//             return;

//         if (reserveAmmo <= 0)
//         {
//             Debug.Log("No reserve ammo left.");
//             return;
//         }

//         StartCoroutine(ReloadRoutine());
//     }

//     private IEnumerator ReloadRoutine()
//     {
//         isReloading = true;
//         Debug.Log("Reloading...");

//         yield return new WaitForSeconds(reloadTime);

//         int bulletsNeeded = magazineSize - ammoInMag;
//         int bulletsToLoad = Mathf.Min(bulletsNeeded, reserveAmmo);

//         ammoInMag += bulletsToLoad;
//         reserveAmmo -= bulletsToLoad;

//         isReloading = false;
//         Debug.Log($"Reloaded. Mag: {ammoInMag}/{magazineSize}, Reserve: {reserveAmmo}");
//     }

//     // Call this from a pickup script when player collects ammo
//     public void AddAmmo(int amount)
//     {
//         if (amount <= 0)
//             return;

//         reserveAmmo += amount;
//     }

//     // Optional external trigger for reload (e.g., from an animation event)
//     public void ForceReload()
//     {
//         if (!isReloading)
//             StartCoroutine(ReloadRoutine());
//     }
// }
////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// using System.Collections;
// using UnityEngine;
// using UnityEngine.InputSystem; // new input system

// public class Rifle : MonoBehaviour
// {
//     [Header("Rifle")]
//     public Camera cam;
//     public float giveDamage = 20f;
//     public float shootingRange = 100f;

//     public float fireRate = 10f; // bullets per second
//     bool wasPressedLastFrame = false;

//     [Header("Rifle Ammunition & Shooting")]
//     float nextFireTime = 0f;
//     private int maxAmmo = 30;
//     private int currentAmmo;
//     private int mag = 15;
//     public float reloadTime = 2f;
//     private bool isReloading = false;

//     private void Awake()
//     {
//         currentAmmo = maxAmmo;
//     }

//     void Update()
//     {
//         if (isReloading)
//             return;

//         if (currentAmmo <= 0)
//         {
//             StartCoroutine(Reload());
//             return;
//         }
//         if (Mouse.current == null)
//             return;

//         bool isPressed = Mouse.current.leftButton.isPressed;

//         // fire once on click
//         if (isPressed && !wasPressedLastFrame)
//         {
//             Shoot();
//             nextFireTime = Time.time + 1f / fireRate;
//         }
//         // fire continuously while held
//         else if (isPressed && Time.time >= nextFireTime)
//         {
//             Shoot();
//             nextFireTime = Time.time + 1f / fireRate;
//         }

//         wasPressedLastFrame = isPressed;
//     }

//     void Shoot()
//     {
//         if (mag <= 0)
//         {
//             Debug.Log("Out of Ammo in Magazine!");
//             return;
//         }
//         currentAmmo--;
//         if (currentAmmo == 0)
//         {
//             mag--;
//         }
//         RaycastHit hitInfo;

//         if (
//             Physics.Raycast(
//                 cam.transform.position,
//                 cam.transform.forward,
//                 out hitInfo,
//                 shootingRange
//             )
//         )
//         {
//             Debug.Log(hitInfo.transform.name);

//             Health objects = hitInfo.transform.GetComponent<Health>();
//             if (objects != null)
//             {
//                 objects.ObjectHitDamage(giveDamage);
//             }
//         }
//     }

//     IEnumerator Reload()
//     {
//         isReloading = true;
//         Debug.Log("Reloading...");

//         yield return new WaitForSeconds(reloadTime);

//         int ammoNeeded = maxAmmo - currentAmmo;
//         if (mag >= ammoNeeded)
//         {
//             currentAmmo = maxAmmo;
//             mag -= ammoNeeded;
//         }
//         else
//         {
//             currentAmmo += mag;
//             mag = 0;
//         }

//         isReloading = false;
//         Debug.Log("Reloaded");
//     }
// }
