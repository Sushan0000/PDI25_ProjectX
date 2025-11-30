using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HealthPickup : MonoBehaviour, IInteractable
{
    [SerializeField, Min(0f)]
    private float healAmount = 25f;

    [SerializeField]
    private bool destroyOnUse = true;

    public void Interact(ControlScript player)
    {
        if (player == null)
            return;

        Health health = player.GetComponent<Health>();
        if (health == null || health.IsDead)
            return;

        float before = health.CurrentHealth;

        // Do nothing if already full HP.
        if (before >= health.MaxHealth)
            return;

        health.Heal(healAmount);

        // Consume pickup only if we actually healed.
        if (destroyOnUse && health.CurrentHealth > before)
            Destroy(gameObject);
    }
}