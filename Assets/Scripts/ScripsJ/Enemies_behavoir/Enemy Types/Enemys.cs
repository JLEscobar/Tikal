using UnityEngine;

public class Enemys : MonoBehaviour, IDamageable, IEnemyMovable
{
    #region Damage variables
    [field: SerializeField] public bool IsDead { get; set; }
    [field: SerializeField] public int CurrentHealth { get; set; }
    [field: SerializeField] public int MaxHealth { get; set; } = 100;

    #endregion

    #region Movement Variables
    public CharacterController controller { get; set; }
    public bool canMove { get; set; }
    public bool facingPlayer { get; set; } = false;
    [field: SerializeField] public float moveSpeed { get; set; }

    [SerializeField] private Transform target;
    public Transform Target => target;

    private Vector3? lastSeenPosition;
    public Vector3? LastSeenPosition
    {
        get => lastSeenPosition;
        set => lastSeenPosition = value;
    }

    [SerializeField] private int maxSearchTurns = 1; 
    private int currentSearchTurns = 0;

    public int MaxSearchTurns => maxSearchTurns;
    public int CurrentSearchTurns
    {
        get => currentSearchTurns;
        set => currentSearchTurns = value;
    }
    #endregion

    #region State Machine Variables

    public EnemyStateMachine stateMachine { get; set; }

    public EnemyIdleState idleState { get; set; }

    public EnemyChasingState chasingState { get; set; }

    public EnemyAttackState attackState { get; set; }
    public EnemyPatrollingState patrollingState { get; set; }
    public EnemyRetreatState retreatState { get; set; }
    public EnemyWaitingTurnState waitingTurnState { get; set; }

    #endregion

    #region Turn System Integration

    private TurnSystem turnSystem;
    private CharacterActor characterActor;
    private bool isMyTurn = false;
    private bool hasCompletedTurnAction = false;

    public bool IsMyTurn => isMyTurn;
    public bool HasCompletedTurnAction => hasCompletedTurnAction;

    #endregion

    #region Stats Variables
    // Rango de visi�n
    [SerializeField] private float visionRange = 10f;
    public float VisionRange => visionRange;

    // Rango de ataque
    [SerializeField] private float attackRange = 2f;
    public float AttackRange => attackRange;

    // Umbral de salud para retirarse
    [SerializeField] private float retreatHealthThreshold = 30f;
    public float RetreatHealthThreshold => retreatHealthThreshold;

    // Puntos de patrullaje
    [SerializeField] private Transform[] patrolPoints;
    public Transform[] PatrolPoints => patrolPoints;

    // Puntos de espera (idle)
    [SerializeField] private Transform[] idlePoints;
    public Transform[] IdlePoints => idlePoints;

    // �ndices de patrullaje/idle
    private int currentPatrolIndex = 0;
    public int CurrentPatrolIndex
    {
        get => currentPatrolIndex;
        set => currentPatrolIndex = value;
    }

    private int currentIdleIndex = 0;
    public int CurrentIdleIndex
    {
        get => currentIdleIndex;
        set => currentIdleIndex = value;
    }

    #endregion


    private void Awake()
    {
        //State Machine init
        stateMachine = new EnemyStateMachine();
        idleState = new EnemyIdleState(this, stateMachine);
        chasingState = new EnemyChasingState(this, stateMachine);
        attackState = new EnemyAttackState(this, stateMachine);
        patrollingState = new EnemyPatrollingState(this, stateMachine);
        retreatState = new EnemyRetreatState(this, stateMachine);
        waitingTurnState = new EnemyWaitingTurnState(this, stateMachine);
        
        // Turn System Integration
        turnSystem = FindFirstObjectByType<TurnSystem>();
        characterActor = GetComponent<CharacterActor>();
    }
    private void Start()
    {
        //state machine - Iniciar en estado de espera
        stateMachine.Initialize(waitingTurnState);

        //Movement
        controller = GetComponent<CharacterController>();
        target = GameObject.FindWithTag("Player").transform;

        //Health
        CurrentHealth = MaxHealth;
        IsDead = false;

        // Suscribirse a eventos del TurnSystem
        if (turnSystem != null)
        {
            turnSystem.OnTurnStarted += HandleTurnStarted;
            turnSystem.OnTurnEnded += HandleTurnEnded;
        }
    }

    private void OnDestroy()
    {
        // Desuscribirse de eventos
        if (turnSystem != null)
        {
            turnSystem.OnTurnStarted -= HandleTurnStarted;
            turnSystem.OnTurnEnded -= HandleTurnEnded;
        }
    }

    private void Update()
    {
        // Solo ejecutar la máquina de estados si es nuestro turno
        if (isMyTurn && !hasCompletedTurnAction)
        {
            stateMachine.currentState.UpdateState();
        }
    }

    private void FixedUpdate()
    {
        // Solo ejecutar física si es nuestro turno
        if (isMyTurn && !hasCompletedTurnAction)
        {
            stateMachine.currentState.PhysicsUpdate();
        }
    }
    #region Damage/Die
    public void TakeDamage(int damageAmount)
    {
        CurrentHealth -= damageAmount;

        if (CurrentHealth <= 0 && !IsDead)
        {
            Die();
        }

    }
    public void Die()
    {
        Debug.Log("Enemy has died.");
    }

    #endregion

    #region movement/facing
    public void moveEnemy(Vector3 direction)
    {
        if (direction == Vector3.zero) return;

        // Movimiento b�sico (puedes adaptarlo a tu sistema de grid o NavMesh)
        transform.position += direction * moveSpeed * Time.deltaTime;

        // Solo rotamos si hay target
        if (target != null)
        {
            CheckFacing(direction);
        }
        else
        {
            // Si no hay target, rotamos hacia la direcci�n de movimiento
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }
    }

    public void CheckFacing(Vector3 direction)
    {
        // Si no hay target, no intentamos rotar
        if (target == null)
        {
            Debug.LogWarning("CheckFacing llamado sin target en " + gameObject.name);
            return;
        }

        Vector3 lookDirection = target.position - transform.position;
        lookDirection.y = 0f;

        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }
    }
    #endregion

    #region Animations/triggers
    private void AnimationTriggerEvent(AnimationTriggerType triggerType)
    {
        //stateMachine.CurrentState.AnimationTriggerEvent(triggerType);
    }
    public enum AnimationTriggerType     {
        EnemyDamaged,
        playFootstepSound
    }
    #endregion

    public void UpdateTarget()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Transform closest = null;
        float minDistance = Mathf.Infinity;

        foreach (GameObject player in players)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);

            if (distance < minDistance && distance <= VisionRange)
            {
                minDistance = distance;
                closest = player.transform;
            }
        }

        target = closest;

        if (target != null)
        {
            LastSeenPosition = target.position;
        }
    }

    public void ExecuteTurn()
    {
        if (IsDead) return;
        stateMachine.currentState.UpdateState();
    }

    #region Turn System Handlers

    private void HandleTurnStarted(Team team, CharacterActor actor)
    {
        // Verificar si es nuestro turno
        if (team == Team.Enemy && characterActor != null && actor == characterActor)
        {
            isMyTurn = true;
            hasCompletedTurnAction = false;
            
            Debug.Log($"[ENEMY_STATE_MACHINE] {gameObject.name}: My turn started. Activating state machine.");
            
            // Cambiar de estado de espera al estado apropiado según la situación
            if (stateMachine.currentState == waitingTurnState)
            {
                // Decidir qué estado usar basado en la situación
                UpdateTarget();
                
                if (Target != null)
                {
                    float distanceToTarget = Vector3.Distance(transform.position, Target.position);
                    
                    if (distanceToTarget <= AttackRange)
                    {
                        stateMachine.ChangeState(attackState);
                    }
                    else if (distanceToTarget <= VisionRange)
                    {
                        stateMachine.ChangeState(chasingState);
                    }
                    else
                    {
                        stateMachine.ChangeState(idleState);
                    }
                }
                else if (PatrolPoints != null && PatrolPoints.Length > 0)
                {
                    stateMachine.ChangeState(patrollingState);
                }
                else
                {
                    stateMachine.ChangeState(idleState);
                }
            }
        }
        else if (team == Team.Enemy && characterActor != null && actor != characterActor)
        {
            // Es el turno de otro enemigo, asegurarse de estar en estado de espera
            if (stateMachine.currentState != waitingTurnState)
            {
                stateMachine.ChangeState(waitingTurnState);
            }
            isMyTurn = false;
            hasCompletedTurnAction = false;
        }
        else if (team == Team.Player)
        {
            // Es turno del jugador, ir a estado de espera
            if (stateMachine.currentState != waitingTurnState)
            {
                stateMachine.ChangeState(waitingTurnState);
            }
            isMyTurn = false;
            hasCompletedTurnAction = false;
        }
    }

    private void HandleTurnEnded(Team team, CharacterActor actor)
    {
        // Si terminó nuestro turno, marcar como completado
        if (team == Team.Enemy && characterActor != null && actor == characterActor)
        {
            hasCompletedTurnAction = true;
            isMyTurn = false;
            
            Debug.Log($"[ENEMY_STATE_MACHINE] {gameObject.name}: My turn ended. Returning to waiting state.");
            
            // Volver al estado de espera
            if (stateMachine.currentState != waitingTurnState)
            {
                stateMachine.ChangeState(waitingTurnState);
            }
        }
    }

    /// <summary>
    /// Método para que los estados llamen cuando completan su acción de turno
    /// </summary>
    public void CompleteTurnAction()
    {
        if (isMyTurn && !hasCompletedTurnAction)
        {
            hasCompletedTurnAction = true;
            Debug.Log($"[ENEMY_STATE_MACHINE] {gameObject.name}: Turn action completed. Ending turn.");
            
            // Terminar el turno a través del TurnSystem
            if (turnSystem != null && characterActor != null)
            {
                turnSystem.EndTurn();
            }
        }
    }

    #endregion

}
