using UnityEngine;

public class EnemyRetreatState : EnemyState
{

    public EnemyRetreatState(Enemys enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
    }

    public override void EnterState()
    {
        base.EnterState();

    }

    public override void UpdateState()
    {
        base.UpdateState();
        // Lógica de retirada pendiente
    }

    public override void ExitState()
    {
        base.ExitState();
        // Detener animación de retirada si aplica
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }

    public override void AnimationTriggerEvent(Enemys.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);

        if (enemy.IsDead) return;

        float distanceToPlayer = Vector3.Distance(enemy.transform.position, enemy.Target.position);

        if (distanceToPlayer <= enemy.AttackRange)
        {
            stateMachine.ChangeState(enemy.attackState);
            return;
        }

        // Alejarse del jugador
        Vector3 direction = (enemy.transform.position - enemy.Target.position).normalized;
        enemy.moveEnemy(direction);

        if (distanceToPlayer > enemy.VisionRange * 2f)
        {
            stateMachine.ChangeState(enemy.patrollingState);
        }

        if (enemy.CurrentHealth > enemy.RetreatHealthThreshold)
        {
            stateMachine.ChangeState(enemy.idleState);
        }
    }
}
