using UnityEngine;
using System.Linq;
using System.Collections.Generic;

[RequireComponent(typeof(Health))]
public class CharacterActor : MonoBehaviour, ITargetable
{
    [Header("Configuration")]
    [SerializeField] private CharacterStats stats;
    
    // DOBLE RANURA DE INYECCIÓN DE VFX
    [Header("VFX Overrides (Inyección)")]
    [Tooltip("Prefab de VFX de Habilidad 0/Básica (se usa si el SO está corrupto).")]
    public GameObject defaultAbilityVFXPrefab; 

    [Tooltip("Prefab de VFX de Habilidad 1/Especial (se usa si el SO está corrupto).")]
    public GameObject specialAbilityVFXPrefab; 

    [Header("GDD Combat Rules")]
    [SerializeField] private int baseAPPerTurn = 1;
    [SerializeField] private int maxAccumulatedAP = 3;

    [Header("Optional Movement Controller")]
    [SerializeField] private TacticalMovementController tacticalMovement;

    [Header("Runtime State")]
    [SerializeField] private int currentActionPoints;
    
    [Header("Status Effects")]
    [SerializeField] public List<StatusEffect> activeEffects = new List<StatusEffect>();

    [Header("Progression")]
    [SerializeField] private int level = 1;
    [SerializeField] private int currentXP = 0;
    private const int XP_TO_NEXT_LEVEL = 100; 

    // Cached components
    private Health _health;
    private CharacterMovement _movement;
    private CharacterController _controller; 

    // Properties
    public CharacterStats Stats => stats;
    public Team Team => stats != null ? stats.team : Team.Player;
    public IHealth Health => _health;
    public int ActionPoints => currentActionPoints;
    public int MaxActionPoints => stats != null ? stats.actionPointsPerTurn : 2;
    public string CharacterName => stats != null ? stats.characterName : gameObject.name;
    public int AttackPower => stats != null ? CalculateAttackPower() : 10;
    public float MovementRange => stats != null ? CalculateMovementRange() : 5f;

    public Transform GetTransform() => transform; // Implementación de ITargetable

    void Awake()
    {
        _health = GetComponent<Health>();
        _movement = GetComponent<CharacterMovement>();
        _controller = GetComponent<CharacterController>();

        if (stats != null)
        {
            _health.Initialize(stats.maxHealth);
            if (tacticalMovement != null)
            {
                tacticalMovement.SetCharacterStats(stats);
            }
        }
        _health.OnDied += OnDeath;
    }

    // ***************************************************
    // * MÉTODOS DE TURNO Y ACCIONES (FIX para CS1061) *
    // ***************************************************

    public void BeginTurn()
    {
        int apAfterRefill = currentActionPoints + baseAPPerTurn; 
        currentActionPoints = Mathf.Min(apAfterRefill, maxAccumulatedAP); 
        
        bool isKnockedOut = activeEffects.Any(e => e.Type == StatusEffectType.Noqueado);

        if (isKnockedOut)
        {
            MessagesSystem.Instance.ShowMessage($"{CharacterName} está Noqueado y no puede actuar.", Color.grey);
            currentActionPoints = 0; 
        }

        if (tacticalMovement != null)
        {
            tacticalMovement.SetMovementPhase(!isKnockedOut); 
            tacticalMovement.SetMovementRange(MovementRange);
        }
    }

    public void EndTurn()
    {
        if (tacticalMovement != null)
        {
            tacticalMovement.SetMovementPhase(false);
        }
    }
    
    // Llamado por TurnSystem para aplicar DoT (Damage over Time)
    public void ApplyTurnDamageEffects()
    {
        if (activeEffects.Any(e => e.Type == StatusEffectType.Quemado))
        {
            int burnDamage = Mathf.RoundToInt(Health.MaxHealth * 0.10f); // 10%
            if (burnDamage > 0)
            {
                Health.TakeDamage(burnDamage);
                MessagesSystem.Instance.ShowMessage($"{CharacterName} recibe {burnDamage} de daño por Quemado.", Color.red);
            }
        }

        if (activeEffects.Any(e => e.Type == StatusEffectType.Envenenado))
        {
            int poisonDamage = Mathf.RoundToInt(Health.MaxHealth * 0.03f); // 3%
            if (poisonDamage > 0)
            {
                Health.TakeDamage(poisonDamage);
                MessagesSystem.Instance.ShowMessage($"{CharacterName} recibe {poisonDamage} de daño por Envenenado.", Color.magenta);
            }
        }
    }
    
    public void ForceMovementPhaseActivation()
    {
        if (tacticalMovement != null)
        {
            tacticalMovement.SetMovementPhase(true);
            tacticalMovement.SetMovementRange(MovementRange);
        }
        else
        {
            Debug.LogWarning($"[vFinal] {CharacterName} no tiene asignado TacticalMovementController en el Inspector.");
        }
    }

    public void ConsumeActionPoints(int amount)
    {
        currentActionPoints = Mathf.Max(0, currentActionPoints - Mathf.Abs(amount));
        Debug.Log($"[AP Consumption] {CharacterName} Consumed {Mathf.Abs(amount)} AP. Remaining AP: {currentActionPoints}");
    }
    
    // Llamado por LineAttackAbility para el teletransporte de Ollin
    public void ForceTeleportToPosition(Vector3 position)
    {
        if (_controller == null)
        {
            transform.position = position; 
            Debug.LogWarning($"{CharacterName} teletransportado sin CharacterController. Usando transform.position.");
        }
        else
        {
            _controller.enabled = false;
            transform.position = position; 
            _controller.enabled = true;
        }
    }
    
    // Llamado por SimpleAIController y movimiento general
    public bool CanMoveTo(Vector3 position)
    {
        float distance = Vector3.Distance(transform.position, position);
        return distance <= MovementRange; 
    }

    // Llamado por SimpleAIController y movimiento general
    public void MoveTo(Vector3 position)
    {
        if (activeEffects.Any(e => e.Type == StatusEffectType.Noqueado)) return;
        if (_movement == null) return;

        if (CanMoveTo(position))
        {
            _movement.MoveToPosition(position);
        }
    }

    // ***************************************************
    // * MÉTODOS DE PROGRESIÓN (FIX para CS1061) *
    // ***************************************************
    
    // Llamado por ProgressionManager
    public void GrantExperience(int amount)
    {
        if (amount <= 0) return;
        currentXP += amount;
        
        CheckLevelUp();
    }

    private void CheckLevelUp()
    {
        while (currentXP >= XP_TO_NEXT_LEVEL)
        {
            currentXP -= XP_TO_NEXT_LEVEL;
            level++;
            MessagesSystem.Instance.ShowMessage($"{CharacterName} subió al Nivel {level}!", Color.yellow);
        }
    }

    // ***************************************************
    // * FIN MÉTODOS FIX *
    // ***************************************************

    public AbilityBase GetAbilityByIndex(int index)
    {
        if (stats == null || stats.abilities == null) return null;
        if (index < 0 || index >= stats.abilities.Length) return null;
        return stats.abilities[index];
    }
    
    public bool TryUseAbility(int abilityIndex, ITargetable target)
    {
        var ability = GetAbilityByIndex(abilityIndex);
        if (ability == null) return false;

        if (target == null && ability is AreaAttackAbility)
        {
             target = this; 
        }
        
        if (target == null) return false; 

        if (ability.currentCooldown > 0) return false;
        if (!ability.CanExecute(this, target)) return false;
        
        ability.Execute(this, target);
        
        if (ability.BaseCooldownTurns > 0)
        {
            ability.currentCooldown = ability.BaseCooldownTurns; 
        }
        return true;
    }

    public void ApplyStatusEffect(StatusEffectType type, int duration)
    {
        if (activeEffects == null) activeEffects = new List<StatusEffect>();

        activeEffects.RemoveAll(e => e.Type == type);
        
        if (duration > 0)
        {
            activeEffects.Add(new StatusEffect { Type = type, Duration = duration });
        }
        
        UpdateMovementSpeedBasedOnEffects();
    }
    
    public void UpdateMovementSpeedBasedOnEffects()
    {
        if (tacticalMovement != null)
        {
            tacticalMovement.SetMovementRange(MovementRange); 
        }
    }
    
    public void RemoveExpiredEffects()
    {
        if (activeEffects == null) return; 

        activeEffects.RemoveAll(e => e.Duration <= 0);
        
        UpdateMovementSpeedBasedOnEffects();
    }

    private int CalculateAttackPower()
    {
        float finalAP = stats.attackPower;
        if (activeEffects.Any(e => e.Type == StatusEffectType.Catalizador))
        {
            finalAP *= 1.15f; 
        }
        return Mathf.RoundToInt(finalAP);
    }
    
    private float CalculateMovementRange()
    {
        float finalRange = stats.movementRange;
        if (activeEffects.Any(e => e.Type == StatusEffectType.Ralentizado))
        {
            finalRange *= 0.80f; 
        }
        return finalRange;
    }
    
    private void OnDeath()
    {
        Debug.Log($"[v0] {CharacterName} has died!");
        gameObject.SetActive(false);
    }
}