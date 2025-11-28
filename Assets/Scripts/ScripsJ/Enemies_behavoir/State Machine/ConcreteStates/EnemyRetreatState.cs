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

        if (enemy.IsDead) return;

        float distanceToPlayer = Vector3.Distance(enemy.transform.position, enemy.Target.position);

        // Alejarse del jugador un paso
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

    public override void ExitState()
    {
        base.ExitState();
        // Detener animaci�n de retirada si aplica
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }

    public override void AnimationTriggerEvent(Enemys.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);

    }
}
