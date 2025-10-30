using System;

public interface IHealth
{
    int MaxHealth { get; }
    int CurrentHealth { get; }
    bool IsDead { get; }

    event Action<int, int> OnHealthChanged;
    event Action OnDied;

    void TakeDamage(int amount);
    void Heal(int amount);
}
