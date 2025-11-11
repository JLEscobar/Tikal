using UnityEngine;

public interface IDamageable 
{
    public void TakeDamage(int damageAmount);

    public void Die();

    bool IsDead { get; set; }

    int CurrentHealth { get; set; }

    int MaxHealth { get; set; }
}
