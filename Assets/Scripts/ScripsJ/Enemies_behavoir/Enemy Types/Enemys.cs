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

    #endregion

    #region Stats Variables
    // Rango de visión
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

    // Índices de patrullaje/idle
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
    }
    private void Start()
    {
        //state machine 
        stateMachine.Initialize(idleState);

        //Movement
        controller = GetComponent<CharacterController>();
        target = GameObject.FindWithTag("Player").transform;

        //Health
        CurrentHealth = MaxHealth;
        IsDead = false;
    }

    private void Update()
    {
        stateMachine.currentState.UpdateState();
    }

    private void FixedUpdate()
    {
        stateMachine.currentState.PhysicsUpdate();
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
        controller.Move(direction * moveSpeed * Time.deltaTime);
        CheckFacing(direction);
    }

    public void CheckFacing(Vector3 direction)
    {
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


}
