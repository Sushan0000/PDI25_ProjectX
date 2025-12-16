using System;
using UnityEngine;

// Simple health component that implements IDamageable.
public class Health : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField, Min(0f)]
    private float maxHealth = 100f;

    [SerializeField, Min(0f)]
    private float objectHealth = 100f;

    [SerializeField]
    private bool destroyOnDeath = true;

    [SerializeField]
    private bool logDamageEvents = false;

    public float CurrentHealth => objectHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => objectHealth <= 0f;

    public event Action OnDeath;

    private void Awake()
    {
        if (maxHealth <= 0f)
            maxHealth = objectHealth > 0f ? objectHealth : 100f;

        objectHealth = Mathf.Clamp(objectHealth, 0f, maxHealth);
    }

    public void ObjectHitDamage(float amount)
    {
        ApplyDamage(amount);
    }

    public void ApplyDamage(float amount)
    {
        if (amount <= 0f || IsDead)
            return;

        objectHealth = Mathf.Max(0f, objectHealth - amount);

        if (logDamageEvents)
            Debug.Log($"{name} took {amount} damage. Current HP: {objectHealth}", this);

        if (objectHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || IsDead)
            return;

        float before = objectHealth;
        objectHealth = Mathf.Min(maxHealth, objectHealth + amount);

        if (logDamageEvents)
            Debug.Log($"{name} healed {objectHealth - before}. Current HP: {objectHealth}", this);
    }

    private void Die()
    {
        OnDeath?.Invoke();

        if (destroyOnDeath)
            Destroy(gameObject);
    }
}
