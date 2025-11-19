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
        Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: ⏸️ WAITING - Entrando al estado de espera de turno");
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
        Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: ⏸️ WAITING - Saliendo del estado de espera (turno iniciado)");
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }
}

