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

    public void ObjectHitDamage(float amount)
    {
        ApplyDamage(amount);
    }

    public void ApplyDamage(float amount)
    {
        if (amount <= 0f || IsDead)
            return;

        objectHealth -= amount;
        if (objectHealth <= 0f)
        {
            objectHealth = 0f;
            Die();
        }
    }

    private void Die()
    {
        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }
}
