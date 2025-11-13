using UnityEngine;

public class EnemyChasingState : EnemyState
{
    public EnemyChasingState(Enemys enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
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

        enemy.UpdateTarget();

        if (enemy.Target != null)
        {
            float distanceToPlayer = Vector3.Distance(enemy.transform.position, enemy.Target.position);

            if (distanceToPlayer <= enemy.AttackRange)
            {
                stateMachine.ChangeState(enemy.attackState);
                return;
            }

            Vector3 direction = (enemy.Target.position - enemy.transform.position).normalized;
            enemy.moveEnemy(direction);

            enemy.CurrentSearchTurns = 0; // reinicia búsqueda
        }
        else if (enemy.LastSeenPosition.HasValue)
        {
            Vector3 direction = (enemy.LastSeenPosition.Value - enemy.transform.position).normalized;
            enemy.moveEnemy(direction);

            if (Vector3.Distance(enemy.transform.position, enemy.LastSeenPosition.Value) < 0.5f)
            {
                enemy.CurrentSearchTurns++;
                if (enemy.CurrentSearchTurns >= enemy.MaxSearchTurns)
                {
                    enemy.LastSeenPosition = null;
                    enemy.CurrentSearchTurns = 0;
                    stateMachine.ChangeState(enemy.patrollingState);
                }
            }
        }
        else
        {
            stateMachine.ChangeState(enemy.patrollingState);
        }

        if (enemy.CurrentHealth <= enemy.RetreatHealthThreshold)
        {
            stateMachine.ChangeState(enemy.retreatState);
        }
    }
}
