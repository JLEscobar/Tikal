using System;

// Delegado para el evento de personaje eliminado (User es el atacante, Target es el muerto)
public delegate void OnCharacterKilled(CharacterActor user, CharacterActor target); 

public interface IHealth
{
    int MaxHealth { get; }
    int CurrentHealth { get; }
    bool IsDead { get; }

    event Action<int, int> OnHealthChanged;
    event Action OnDied;
    event OnCharacterKilled OnKilledBy; // NUEVO: Evento para cuando el personaje muere por un ataque

    void TakeDamage(int amount);
    void Heal(int amount);
}