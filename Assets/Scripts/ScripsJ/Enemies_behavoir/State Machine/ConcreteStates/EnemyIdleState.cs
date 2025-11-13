using UnityEngine;

public class EnemyIdleState : EnemyState
{
    private float idleTimer;
    private float idleDuration = 2f; 
    public EnemyIdleState(Enemys enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
    }

    public override void AnimationTriggerEvent(Enemys.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
    }

    public override void EnterState()
    {
        idleTimer = 0f;
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
            stateMachine.ChangeState(enemy.chasingState);
            return;
        }

        idleTimer += Time.deltaTime;
        if (idleTimer >= idleDuration)
        {
            Transform idleTarget = enemy.IdlePoints[enemy.CurrentIdleIndex];
            Vector3 direction = (idleTarget.position - enemy.transform.position).normalized;
            enemy.moveEnemy(direction);

            if (Vector3.Distance(enemy.transform.position, idleTarget.position) < 0.5f)
            {
                enemy.CurrentIdleIndex = (enemy.CurrentIdleIndex + 1) % enemy.IdlePoints.Length;
                idleTimer = 0f;
            }
        }
    }
}
