using UnityEngine;

[RequireComponent(typeof(Health))]
public class CharacterActor : MonoBehaviour, ITargetable
{
    [Header("Configuration")]
    [SerializeField] private CharacterStats stats;

    [Header("Optional Movement Controller")]
    [SerializeField] private TacticalMovementController tacticalMovement;

    [Header("Runtime State")]
    [SerializeField] private int currentActionPoints;
    
    // Campos de progresión
    [Header("Progression")]
    [SerializeField] private int level = 1;
    [SerializeField] private int currentXP = 0;
    private const int XP_TO_NEXT_LEVEL = 100; 

    // Cached components
    private Health _health;
    private CharacterMovement _movement;

    // Properties
    public CharacterStats Stats => stats;
    public Team Team => stats != null ? stats.team : Team.Player;
    public IHealth Health => _health;
    public int ActionPoints => currentActionPoints;
    public int MaxActionPoints => stats != null ? stats.actionPointsPerTurn : 2;
    public string CharacterName => stats != null ? stats.characterName : name;
    public int AttackPower => stats != null ? stats.attackPower : 10;
    public float MovementRange => stats != null ? stats.movementRange : 5f;
    
    // Propiedades de progresión
    public int Level => level;
    public int CurrentXP => currentXP;

    void Awake()
    {
        _health = GetComponent<Health>();
        _movement = GetComponent<CharacterMovement>();

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

    public Transform GetTransform() => transform;

    public void BeginTurn()
    {
        currentActionPoints = MaxActionPoints;

        if (tacticalMovement != null)
        {
            tacticalMovement.SetMovementPhase(true);
        }
    }

    public void EndTurn()
    {
        currentActionPoints = 0;

        if (tacticalMovement != null)
        {
            tacticalMovement.SetMovementPhase(false);
        }
    }
    
    // MÉTODO DE CORRECCIÓN DE NULLREF
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
    }

    public AbilityBase GetAbilityByIndex(int index)
    {
        if (stats == null || stats.abilities == null) return null;
        if (index < 0 || index >= stats.abilities.Length) return null;
        return stats.abilities[index];
    }

    // MÉTODO MODIFICADO: Inicia el Cooldown
    public bool TryUseAbility(int abilityIndex, ITargetable target)
    {
        var ability = GetAbilityByIndex(abilityIndex);
        if (ability == null) return false;

        // Lógica para permitir habilidades AoE/Self-Cast
        if (target == null && ability is AreaAttackAbility)
        {
             target = this; 
        }
        
        if (target == null) return false; 

        if (!ability.CanExecute(this, target)) return false;

        ability.Execute(this, target);
        
        // NUEVO: Iniciar el Cooldown después de la ejecución exitosa
        if (ability.BaseCooldownTurns > 0)
        {
            ability.currentCooldown = ability.BaseCooldownTurns; 
            Debug.Log($"[CD] {CharacterName}: {ability.DisplayName} en cooldown por {ability.currentCooldown} turnos.");
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
        Debug.Log($"[v0] {CharacterName} MoveTo called. Target: {position}");

        if (_movement == null)
        {
            Debug.LogError($"[v0] {CharacterName} has no CharacterMovement component!");
            return;
        }

        if (!CanMoveTo(position))
        {
            float distance = Vector3.Distance(transform.position, position);
            Debug.LogWarning($"[v0] {CharacterName} cannot move to {position}. Distance: {distance:F2}, Max Range: {MovementRange:F2}");
            return;
        }

        Debug.Log($"[v0] {CharacterName} starting movement to {position}");
        _movement.MoveToPosition(position);
    }
    
    public void GrantExperience(int amount)
    {
        if (amount <= 0) return;
        currentXP += amount;
        
        Debug.Log($"[vFinal] {CharacterName} gained {amount} XP. Total: {currentXP}");

        CheckLevelUp();
    }

    private void CheckLevelUp()
    {
        while (currentXP >= XP_TO_NEXT_LEVEL)
        {
            currentXP -= XP_TO_NEXT_LEVEL;
            level++;
            
            MessagesSystem.Instance.ShowMessage($"{CharacterName} subió al Nivel {level}!", Color.yellow);
            Debug.Log($"[vFinal] {CharacterName} leveled up to Level {level}!");
        }
    }

    private void OnDeath()
    {
        Debug.Log($"[v0] {CharacterName} has died!");
        gameObject.SetActive(false);
    }
}