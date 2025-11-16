using UnityEngine;
using System.Linq;
using System.Collections.Generic;

[RequireComponent(typeof(Health))]
public class CharacterActor : MonoBehaviour, ITargetable
{
    [Header("Configuration")]
    [SerializeField] private CharacterStats stats;

    [Header("GDD Combat Rules")]
    [SerializeField] private int baseAPPerTurn = 1;
    [SerializeField] private int maxAccumulatedAP = 3;

    [Header("Optional Movement Controller")]
    [SerializeField] private TacticalMovementController tacticalMovement;

    [Header("Runtime State")]
    [SerializeField] private int currentActionPoints;
    
    [Header("Status Effects")]
    [SerializeField] public List<StatusEffect> activeEffects = new List<StatusEffect>();

    // Campos de progresión
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
    public string CharacterName => stats != null ? stats.characterName : name;
    
    public int AttackPower
    {
        get
        {
            int baseAttack = stats != null ? stats.attackPower : 10;
            
            if (activeEffects.Any(e => e.Type == StatusEffectType.Catalizador))
            {
                int buffAmount = Mathf.RoundToInt(baseAttack * 0.15f);
                return baseAttack + buffAmount; 
            }

            return baseAttack;
        }
    }
    
    public float MovementRange
    {
        get
        {
            float baseRange = stats != null ? stats.movementRange : 5f;
            
            if (activeEffects.Any(e => e.Type == StatusEffectType.Ralentizado))
            {
                return baseRange * 0.8f; 
            }

            return baseRange;
        }
    }

    public int Level => level;
    public int CurrentXP => currentXP;

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

    void OnDestroy()
    {
        _health.OnDied -= OnDeath;
    }

    public Transform GetTransform() => transform; // Reimplementación de ITargetable

    // CORRECCIÓN 1: Lógica de AP Acumulativo
    public void BeginTurn()
    {
        // Sumar AP base al remanente, con límite de 3.
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
        }
        
        // Tick de Cooldowns
        if (stats != null && stats.abilities != null)
        {
            foreach (var ability in stats.abilities)
            {
                if (ability.currentCooldown > 0)
                {
                    ability.currentCooldown--;
                }
            }
        }
    }

    // CORRECCIÓN 2: EndTurn NO debe resetear el AP. Solo detiene el movimiento.
    public void EndTurn()
    {
        if (tacticalMovement != null)
        {
            tacticalMovement.SetMovementPhase(false);
        }
        // Nota: El AP se mantiene para la lógica de acumulación y chequeo de PlayerTurnController.
    }
    
    public void ForceMovementPhaseActivation()
    {
        if (tacticalMovement != null)
        {
            tacticalMovement.SetMovementPhase(true);
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

    // ... (El resto de métodos se mantiene, incluyendo TryUseAbility, etc.) ...
    
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

    public void ApplyStatusEffect(StatusEffectType type, int duration)
    {
        if (activeEffects == null) activeEffects = new List<StatusEffect>();

        activeEffects.RemoveAll(e => e.Type == type);
        
        if (duration > 0)
        {
            activeEffects.Add(new StatusEffect { Type = type, Duration = duration });
            MessagesSystem.Instance.ShowMessage($"{CharacterName} ahora tiene {type} por {duration} turnos.", Color.yellow);
        }
        
        UpdateMovementSpeedBasedOnEffects();
    }
    
    public void UpdateMovementSpeedBasedOnEffects()
    {
        if (tacticalMovement != null)
        {
            Debug.Log($"[Estado] {CharacterName} Movement Range actualizado a: {MovementRange:F2}");
        }
    }
    
    public void RemoveExpiredEffects()
    {
        if (activeEffects == null) return; 

        activeEffects.RemoveAll(e => e.Duration <= 0);
        
        UpdateMovementSpeedBasedOnEffects();
    }
    
    public void ApplyTurnDamageEffects()
    {
        if (activeEffects.Any(e => e.Type == StatusEffectType.Quemado))
        {
            int burnDamage = Mathf.RoundToInt(_health.MaxHealth * 0.10f);
            if (burnDamage > 0)
            {
                _health.TakeDamage(burnDamage);
                MessagesSystem.Instance.ShowMessage($"{CharacterName} recibe {burnDamage} de daño por Quemado.", Color.red);
            }
        }

        if (activeEffects.Any(e => e.Type == StatusEffectType.Envenenado))
        {
            int poisonDamage = Mathf.RoundToInt(_health.MaxHealth * 0.03f);
            if (poisonDamage > 0)
            {
                _health.TakeDamage(poisonDamage);
                MessagesSystem.Instance.ShowMessage($"{CharacterName} recibe {poisonDamage} de daño por Envenenado.", Color.magenta);
            }
        }
    }


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

        if (ability.currentCooldown > 0)
        {
             MessagesSystem.Instance.ShowMessage($"{CharacterName}: {ability.DisplayName} está en Cooldown ({ability.currentCooldown} turnos).", Color.red);
             return false;
        }

        if (!ability.CanExecute(this, target)) return false;
        
        if (activeEffects.Any(e => e.Type == StatusEffectType.Noqueado))
        {
             MessagesSystem.Instance.ShowMessage($"{CharacterName} no puede usar habilidades, está Noqueado.", Color.grey);
             return false;
        }

        ability.Execute(this, target);
        
        if (ability.BaseCooldownTurns > 0)
        {
            ability.currentCooldown = ability.BaseCooldownTurns; 
        }

        return true;
    }

    public bool CanMoveTo(Vector3 position)
    {
        float distance = Vector3.Distance(transform.position, position);
        return distance <= MovementRange; 
    }

    public void MoveTo(Vector3 position)
    {
        if (activeEffects.Any(e => e.Type == StatusEffectType.Noqueado))
        {
             MessagesSystem.Instance.ShowMessage($"{CharacterName} no puede moverse, está Noqueado.", Color.grey);
             return;
        }
        
        if (_movement == null)
        {
            Debug.LogError($"[v0] {CharacterName} has no CharacterMovement component!");
            return;
        }

        if (!CanMoveTo(position))
        {
            return;
        }

        _movement.MoveToPosition(position);
    }
    
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

    private void OnDeath()
    {
        Debug.Log($"[v0] {CharacterName} has died!");
        gameObject.SetActive(false);
    }
}