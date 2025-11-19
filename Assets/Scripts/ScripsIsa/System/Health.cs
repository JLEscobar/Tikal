using System;
using UnityEngine;

[DisallowMultipleComponent]
public class Health : MonoBehaviour, IHealth
{
    public int maxHealth = 100;
    public int _currentHealth;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => _currentHealth;
    public bool IsDead => _currentHealth <= 0;

    public event Action<int, int> OnHealthChanged;
    public event Action OnDied;
    public event OnCharacterKilled OnKilledBy; // DECLARACIÓN DEL NUEVO EVENTO

    void Awake()
    {
        _currentHealth = maxHealth;
    }

    public void Initialize(int max)
    {
        maxHealth = Mathf.Max(1, max);
        _currentHealth = maxHealth;
        OnHealthChanged?.Invoke(_currentHealth, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        int actualDamage = Mathf.Abs(amount);
        _currentHealth = Mathf.Max(0, _currentHealth - actualDamage);

        OnHealthChanged?.Invoke(_currentHealth, maxHealth);

        if (_currentHealth == 0)
        {
            OnDied?.Invoke();
            // Nota: OnKilledBy se dispara desde el script de la habilidad (MeleeAttackAbility, etc.)
        }
    }

    public void Heal(int amount)
    {
        if (IsDead) return;

        int actualHeal = Mathf.Abs(amount);
        _currentHealth = Mathf.Min(maxHealth, _currentHealth + actualHeal);

        OnHealthChanged?.Invoke(_currentHealth, maxHealth);
    }
}