using UnityEngine;
using System.Linq;
using System.Collections;
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
    private TurnSystem _turnSystem;
    
    // NOTA: La lógica de restauración de APs para jugadores está ahora en TacticalMovement2.cs y PlayerTurnController.cs
    // Las flags de APs se eliminaron porque la restauración se maneja externamente

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
        
        // Buscar TurnSystem para suscribirse a eventos
        _turnSystem = FindFirstObjectByType<TurnSystem>();
    }
    
    void Start()
    {
        // Suscribirse a eventos del TurnSystem para detectar cambios de turno global
        if (_turnSystem != null)
        {
            _turnSystem.OnTurnStarted += HandleTurnStarted;
            _turnSystem.OnTurnEnded += HandleTurnEnded;
        }
    }
    
    void OnDestroy()
    {
        // Desuscribirse de eventos
        if (_turnSystem != null)
        {
            _turnSystem.OnTurnStarted -= HandleTurnStarted;
            _turnSystem.OnTurnEnded -= HandleTurnEnded;
        }
    }
    
    private void HandleTurnStarted(Team team, CharacterActor actor)
    {
        // La lógica de restauración de APs para jugadores está ahora en TacticalMovement2.cs y PlayerTurnController.cs
        // Este método se mantiene para compatibilidad pero ya no restaura APs
    }
    
    private void HandleTurnEnded(Team team, CharacterActor actor)
    {
        // La lógica de restauración de APs para jugadores está ahora en TacticalMovement2.cs y PlayerTurnController.cs
        // Este método se mantiene para compatibilidad
    }

    // ***************************************************
    // * MÉTODOS DE TURNO Y ACCIONES (FIX para CS1061) *
    // ***************************************************

    public void BeginTurn()
    {
        // La lógica de restauración de APs para jugadores está ahora en TacticalMovement2.cs y PlayerTurnController.cs
        // Este método solo maneja el estado del turno (movimiento, efectos, etc.)
        
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
        // Las flags de AP se resetean automáticamente cuando cambia el turno global en HandleTurnEnded()
        // No necesitamos resetear nada aquí
        
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

    /// <summary>
    /// Establece los Action Points directamente (usado por TacticalMovement2 y PlayerTurnController)
    /// </summary>
    public void SetActionPoints(int newAP)
    {
        // Asegurar que no sea negativo
        currentActionPoints = Mathf.Max(0, newAP);
        Debug.Log($"[AP] {CharacterName}: AP establecidos directamente a {currentActionPoints}");
    }
    
    public void ConsumeActionPoints(int amount)
    {
        int apBefore = currentActionPoints;
        int amountToConsume = Mathf.Abs(amount);
        currentActionPoints = Mathf.Max(0, currentActionPoints - amountToConsume);
        Debug.Log($"[AP Consumption] {CharacterName}: Consumió {amountToConsume} AP. AP: {apBefore} -> {currentActionPoints}");
        
        // Verificación de seguridad: si los AP no disminuyeron, hay un problema
        if (amountToConsume > 0 && apBefore == currentActionPoints)
        {
            Debug.LogError($"[AP Consumption] ⚠️ ERROR: {CharacterName} intentó consumir {amountToConsume} AP pero los AP no disminuyeron! (AP antes: {apBefore}, AP después: {currentActionPoints})");
        }
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
        if (ability == null)
        {
            Debug.LogWarning($"[TryUseAbility] {CharacterName}: Habilidad con índice {abilityIndex} no encontrada.");
            return false;
        }

        if (target == null && ability is AreaAttackAbility)
        {
             target = this; 
        }
        
        if (target == null)
        {
            Debug.LogWarning($"[TryUseAbility] {CharacterName}: Target es null para {ability.DisplayName}.");
            return false; 
        }

        // Verificar AP ANTES de ejecutar (doble verificación de seguridad)
        int apBefore = currentActionPoints;
        if (apBefore < ability.CostAP)
        {
            Debug.LogWarning($"[TryUseAbility] {CharacterName}: AP insuficientes para {ability.DisplayName}. Tiene: {apBefore}, Necesita: {ability.CostAP}");
            return false;
        }

        if (ability.currentCooldown > 0)
        {
            Debug.Log($"[TryUseAbility] {CharacterName}: {ability.DisplayName} está en cooldown ({ability.currentCooldown} turnos).");
            return false;
        }
        
        if (!ability.CanExecute(this, target))
        {
            Debug.Log($"[TryUseAbility] {CharacterName}: {ability.DisplayName} no puede ejecutarse (CanExecute retornó false).");
            return false;
        }
        
        // Ejecutar la habilidad (esto debería consumir AP dentro de Execute)
        ability.Execute(this, target);
        
        // Verificar que los AP se consumieron correctamente
        int apAfter = currentActionPoints;
        if (ability.CostAP > 0 && apBefore == apAfter)
        {
            Debug.LogError($"[TryUseAbility] ⚠️ ERROR CRÍTICO: {CharacterName} usó {ability.DisplayName} (CostAP: {ability.CostAP}) pero los AP NO disminuyeron! AP antes: {apBefore}, AP después: {apAfter}");
        }
        else if (ability.CostAP > 0)
        {
            Debug.Log($"[TryUseAbility] {CharacterName}: {ability.DisplayName} ejecutada correctamente. AP: {apBefore} -> {apAfter} (consumió {apBefore - apAfter})");
        }
        
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
    
    [Header("Death Settings")]
    [SerializeField] private float deathAnimationDelay = 4.0f; // Tiempo en segundos antes de desactivar el GameObject
    
    private void OnDeath()
    {
        Debug.Log($"[v0] {CharacterName} has died!");
        // Iniciar corrutina para desactivar el GameObject después del delay
        StartCoroutine(DeactivateAfterDeathAnimation());
    }
    
    /// <summary>
    /// Corrutina que espera a que la animación de muerte se reproduzca antes de desactivar el GameObject
    /// </summary>
    private IEnumerator DeactivateAfterDeathAnimation()
    {
        // Esperar el tiempo necesario para que la animación de muerte se reproduzca
        yield return new WaitForSeconds(deathAnimationDelay);
        
        // Desactivar el GameObject después del delay
        if (gameObject != null)
        {
            gameObject.SetActive(false);
            Debug.Log($"[v0] {CharacterName} GameObject desactivado después de {deathAnimationDelay} segundos");
        }
    }
}