using UnityEngine;

public class AmmoPickup : MonoBehaviour, IInteractable
{
    [Header("Ammo")]
    [SerializeField]
    private int ammoAmount = 30;

    [Header("References")]
    [Tooltip(
        "Optional. Assign the player's Rifle here to be 100% sure this pickup feeds the correct gun."
    )]
    [SerializeField]
    private Rifle rifleOverride;

    [Header("Feedback")]
    [SerializeField]
    private AudioClip pickupClip;

    [SerializeField]
    private GameObject pickupVfx;

    [SerializeField]
    private bool destroyOnPickup = true;

    public void Interact(ControlScript player)
    {
        // 1) Decide which Rifle instance to use
        Rifle rifle = rifleOverride;

        if (!rifle && player != null)
        {
            // Try to find rifle on the player hierarchy
            rifle = player.GetComponentInChildren<Rifle>();
        }

        if (!rifle)
        {
            // Fallback: first Rifle in the scene (same as your UI uses)
            rifle = Object.FindFirstObjectByType<Rifle>();
        }

        if (!rifle)
        {
            Debug.LogWarning("AmmoPickup: no Rifle found to give ammo to.", this);
            return;
        }

        // 2) Actually add ammo
        int before = rifle.ReserveAmmo;
        rifle.AddAmmo(ammoAmount);
        int after = rifle.ReserveAmmo;

        Debug.Log(
            $"AmmoPickup: added {ammoAmount} ammo. Reserve {before} -> {after} on {rifle.name}"
        );

        // 3) Feedback
        if (pickupClip != null)
            AudioSource.PlayClipAtPoint(pickupClip, transform.position);

        if (pickupVfx != null)
            Instantiate(pickupVfx, transform.position, Quaternion.identity);

        if (destroyOnPickup)
            Destroy(gameObject);
    }
}