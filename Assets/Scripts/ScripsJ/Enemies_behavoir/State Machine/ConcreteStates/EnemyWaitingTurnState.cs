using UnityEngine;

public class EnemyWaitingTurnState : EnemyState
{
    public EnemyWaitingTurnState(Enemys enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
    }

    public override void EnterState()
    {
        base.EnterState();
        enemy.canMove = false;
    }

    public override void UpdateState()
    {
        base.UpdateState();
        // No hacer nada mientras espera su turno
        // La máquina de estados se activará cuando sea su turno
    }

    public override void ExitState()
    {
        base.ExitState();
        enemy.canMove = true;
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }
}

