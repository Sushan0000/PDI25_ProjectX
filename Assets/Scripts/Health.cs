using System; // for Action
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField]
    private float objectHealth = 100f;

    [SerializeField]
    private bool destroyOnDeath = true;

    public float CurrentHealth => objectHealth;
    public bool IsDead => objectHealth <= 0f;

    // This is the event PlayerHealth will subscribe to
    public event Action OnDeath;

    public void ObjectHitDamage(float amount)
    {
        ApplyDamage(amount);
    }

    public void ApplyDamage(float amount)
    {
        if (amount <= 0f || IsDead)
            return;

        objectHealth -= amount;
        Debug.Log($"{name} took {amount} damage. Current HP: {objectHealth}");

        if (objectHealth <= 0f)
        {
            objectHealth = 0f;
            Die();
        }
    }

    private void Die()
    {
        // fire the event safely (if there are subscribers)
        OnDeath?.Invoke();

        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }
}
