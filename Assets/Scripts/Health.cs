using UnityEngine;

public class Health : MonoBehaviour
{
    public float objectHealth = 100f;

    public void ObjectHitDamage(float amount)
    {
        objectHealth -= amount;
        if (objectHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }
}
