using UnityEngine;

public class EnemyAttackState : EnemyState
{
    private int attackCooldownTurns = 2;
    private int turnsSinceLastAttack = 0;
    public EnemyAttackState(Enemys enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
    }

    public override void AnimationTriggerEvent(Enemys.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
    }

    public override void EnterState()
    {
        base.EnterState();
    }

    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }

    public override void ExitState()
    {
        base.ExitState();
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }

    public override string ToString()
    {
        return base.ToString();
    }

    public override void UpdateState()
    {
        base.UpdateState();

        if (enemy.IsDead) return;

        float distanceToPlayer = Vector3.Distance(enemy.transform.position, enemy.Target.position);

        if (distanceToPlayer <= enemy.AttackRange)
        {
            if (turnsSinceLastAttack >= attackCooldownTurns)
            {
                Debug.Log("Enemy attacks player!");
                // Aquí iría la lógica real de daño
                turnsSinceLastAttack = 0;
            }
            else
            {
                turnsSinceLastAttack++;
                Debug.Log("Enemy waits for attack cooldown.");
            }
        }
        else
        {
            stateMachine.ChangeState(enemy.chasingState);
        }

        if (enemy.CurrentHealth <= enemy.RetreatHealthThreshold)
        {
            stateMachine.ChangeState(enemy.retreatState);
        }
    }
}
