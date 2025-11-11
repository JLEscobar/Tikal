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

    private Transform target;
    #endregion

    #region State Machine Variables

    public EnemyStateMachine stateMachine { get; set; }

    public EnemyIdleState idleState { get; set; }

    public EnemyChasingState chasingState { get; set; }

    public EnemyAttackState attackState { get; set; }

    #endregion


    private void Awake()
    {
        //State Machine init
        stateMachine = new EnemyStateMachine();
        idleState = new EnemyIdleState(this, stateMachine);
        chasingState = new EnemyChasingState(this, stateMachine);
        attackState = new EnemyAttackState(this, stateMachine);
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
        /*switch (triggerType)
         {
             case AnimationTriggerType.EnemyDamaged:
                 // Handle enemy damaged animation trigger
                 break;
             case AnimationTriggerType.playFootstepSound:
                 // Handle footstep sound trigger
                 break;
             default:
                 break;
         }*/
        // Currently empty as per the original code

        stateMachine.CurrentState.AnimationTriggerEvent(triggerType);
    }
    public enum AnimationTriggerType     {
        EnemyDamaged,
        playFootstepSound
    }
    #endregion
}
