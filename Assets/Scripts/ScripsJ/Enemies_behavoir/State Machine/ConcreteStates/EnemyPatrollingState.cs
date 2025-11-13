using UnityEngine;

public class EnemyPatrollingState : EnemyState
{
    private float waitTimer;
    private float waitDuration = 1.5f;
    public EnemyPatrollingState(Enemys enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
    }

    public override void EnterState()
    {
        waitTimer = 0f;
        base.EnterState();
        // Aquí podrías iniciar animación de patrullaje
    }

    public override void UpdateState()
    {
        base.UpdateState();
        // Lógica de patrullaje pendiente
    }

    public override void ExitState()
    {
        base.ExitState();
        // Detener animación de patrullaje si aplica
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }

    public override void AnimationTriggerEvent(Enemys.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);

        if (enemy.IsDead) return;

        enemy.UpdateTarget();
        if (enemy.Target != null)
        {
            stateMachine.ChangeState(enemy.chasingState);
            return;
        }

        Transform patrolTarget = enemy.PatrolPoints[enemy.CurrentPatrolIndex];
        Vector3 direction = (patrolTarget.position - enemy.transform.position).normalized;
        enemy.moveEnemy(direction);

        if (Vector3.Distance(enemy.transform.position, patrolTarget.position) < 0.5f)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitDuration)
            {
                enemy.CurrentPatrolIndex = (enemy.CurrentPatrolIndex + 1) % enemy.PatrolPoints.Length;
                waitTimer = 0f;
            }
        }
    }
}
