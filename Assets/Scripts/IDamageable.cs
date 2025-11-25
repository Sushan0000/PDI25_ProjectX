// IDamageable.cs

/// <summary>Interface for objects that can take damage.</summary>
public interface IDamageable
{
    /// <summary>Apply damage to this object.</summary>
    /// <param name="amount">Damage amount in hit points.</param>
    void ApplyDamage(float amount);
}
