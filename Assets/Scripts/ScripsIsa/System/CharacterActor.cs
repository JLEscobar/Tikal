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

    [Tooltip("Overrides por ranura. Índice 0 = habilidad básica, 1 = especial, etc.")]
    [SerializeField] private GameObject[] abilitySlotVFXOverrides;

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
        
        // Buscar automáticamente TacticalMovementController si no está asignado
        if (tacticalMovement == null)
        {
            tacticalMovement = GetComponent<TacticalMovementController>();
        }

        if (stats != null)
        {
            _health.Initialize(stats.maxHealth);
            if (tacticalMovement != null)
            {
                tacticalMovement.SetCharacterStats(stats);
            }
            // Configurar velocidad de movimiento para CharacterMovement (usado por enemigos)
            if (_movement != null)
            {
                _movement.SetMoveSpeed(stats.moveSpeed);
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

        // Asegurar que tacticalMovement esté inicializado
        if (tacticalMovement == null)
        {
            tacticalMovement = GetComponent<TacticalMovementController>();
        }

        if (tacticalMovement != null)
        {
            tacticalMovement.SetMovementPhase(!isKnockedOut); 
            tacticalMovement.SetMovementRange(MovementRange);
            Debug.Log($"[MOVEMENT] {CharacterName}: Movement phase activated = {!isKnockedOut}");
        }
        else
        {
            Debug.LogWarning($"[MOVEMENT] {CharacterName}: No TacticalMovementController found. Movement will not work.");
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
        // Asegurar que tacticalMovement esté inicializado
        if (tacticalMovement == null)
        {
            tacticalMovement = GetComponent<TacticalMovementController>();
        }
        
        if (tacticalMovement != null)
        {
            tacticalMovement.SetMovementPhase(true);
            tacticalMovement.SetMovementRange(MovementRange);
            Debug.Log($"[MOVEMENT] {CharacterName}: Movement phase force activated");
        }
        else
        {
            Debug.LogWarning($"[MOVEMENT] {CharacterName}: No TacticalMovementController found. Cannot activate movement.");
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

    public int GetAbilityIndex(AbilityBase ability)
    {
        if (ability == null || stats == null || stats.abilities == null) return -1;
        for (int i = 0; i < stats.abilities.Length; i++)
        {
            if (stats.abilities[i] == ability)
            {
                return i;
            }
        }
        return -1;
    }

    public GameObject GetAbilityVFXOverride(AbilityBase ability)
    {
        int abilityIndex = GetAbilityIndex(ability);
        if (abilityIndex == -1)
        {
            Debug.LogWarning($"[VFX OVERRIDE] {CharacterName}: No se pudo encontrar el índice de la habilidad {ability?.DisplayName}");
            return null;
        }

        Debug.Log($"[VFX OVERRIDE] {CharacterName}: Buscando override para habilidad índice {abilityIndex} ({ability?.DisplayName})");

        if (abilitySlotVFXOverrides != null && abilityIndex < abilitySlotVFXOverrides.Length)
        {
            var specificOverride = abilitySlotVFXOverrides[abilityIndex];
            if (specificOverride != null)
            {
                Debug.Log($"[VFX OVERRIDE] {CharacterName}: Encontrado override específico en ranura {abilityIndex}: {specificOverride.name}");
                return specificOverride;
            }
            else
            {
                Debug.Log($"[VFX OVERRIDE] {CharacterName}: Ranura {abilityIndex} está vacía, buscando respaldo genérico...");
            }
        }
        else
        {
            Debug.Log($"[VFX OVERRIDE] {CharacterName}: Array de overrides no existe o índice {abilityIndex} fuera de rango. Buscando respaldo genérico...");
        }

        if (abilityIndex == 0 && defaultAbilityVFXPrefab != null)
        {
            Debug.Log($"[VFX OVERRIDE] {CharacterName}: Usando respaldo básico: {defaultAbilityVFXPrefab.name}");
            return defaultAbilityVFXPrefab;
        }

        if (abilityIndex > 0 && specialAbilityVFXPrefab != null)
        {
            Debug.Log($"[VFX OVERRIDE] {CharacterName}: Usando respaldo especial: {specialAbilityVFXPrefab.name}");
            return specialAbilityVFXPrefab;
        }

        Debug.LogWarning($"[VFX OVERRIDE] {CharacterName}: No se encontró ningún override para habilidad índice {abilityIndex}");
        return null;
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