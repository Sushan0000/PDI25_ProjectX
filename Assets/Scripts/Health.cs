// Health.cs
using System;
using UnityEngine;

// Simple health component that implements IDamageable.
public class Health : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField, Min(0f)]
    private float objectHealth = 100f; // Current health

    [SerializeField]
    private bool destroyOnDeath = true; // Auto-destroy on death

    [SerializeField]
    private bool logDamageEvents = false; // Optional debug logging

    // Current health value.
    public float CurrentHealth => objectHealth;

    // True if this object is dead.
    public bool IsDead => objectHealth <= 0f;

    // Fired once when health reaches zero.
    public event Action OnDeath;

    // Convenience wrapper for ApplyDamage.
    public void ObjectHitDamage(float amount)
    {
        ApplyDamage(amount);    }

    // Core damage entry point used by external systems.
    public void ApplyDamage(float amount)
    {
        // Ignore invalid damage and hits on already-dead objects.
        if (amount <= 0f || IsDead)
            return;

        // Subtract damage and clamp to zero.
        objectHealth = Mathf.Max(0f, objectHealth - amount);

        if (logDamageEvents)
            Debug.Log($"{name} took {amount} damage. Current HP: {objectHealth}", this);

        // If health is zero, run death logic.
        if (objectHealth <= 0f)
            Die();
    }

    // Handle death: raise event and optionally destroy this object.
    private void Die()
    {
        // Notify listeners (player, AI, UI, etc.).
        OnDeath?.Invoke();

        // Remove this GameObject if configured.
        if (destroyOnDeath)
            Destroy(gameObject);
    }
}
