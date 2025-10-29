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

    public bool TryUseAbility(int abilityIndex, ITargetable target)
    {
        var ability = GetAbilityByIndex(abilityIndex);
        if (ability == null) return false;
        if (!ability.CanExecute(this, target)) return false;

        ability.Execute(this, target);
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

    private void OnDeath()
    {
        Debug.Log($"[v0] {CharacterName} has died!");
        gameObject.SetActive(false);
    }
}
