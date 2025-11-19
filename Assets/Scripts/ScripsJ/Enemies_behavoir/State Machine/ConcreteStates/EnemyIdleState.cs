using UnityEngine;

public class EnemyIdleState : EnemyState
{
    private int idleTurnsWaited;
    private int idleTurnsToWait = 2;
    public EnemyIdleState(Enemys enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
    }

    public override void AnimationTriggerEvent(Enemys.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
    }

    public override void EnterState()
    {
        idleTurnsWaited = 0;
        base.EnterState();
        enemy.canMove = true; // Asegurar que el enemigo pueda moverse
        Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: 😴 IDLE - Entrando al estado de reposo");
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

        // PRIORIDAD 1: SIEMPRE buscar targets en cada turno (especialmente si no hay target actual)
        // Esto asegura que si un target muere, el enemigo encuentre nuevos targets
        enemy.UpdateTarget();
        
        if (enemy.Target != null)
        {
            float distanceToTarget = Vector3.Distance(enemy.transform.position, enemy.Target.position);
            Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: 😴 IDLE - Target detectado a distancia: {distanceToTarget:F2}");
            
            // Si está en rango de ataque, atacar inmediatamente
            if (distanceToTarget <= enemy.AttackRange)
            {
                Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: 😴 IDLE - ⚠️ Jugador en rango de ataque ({distanceToTarget:F2} <= {enemy.AttackRange}), cambiando a ATTACK");
                stateMachine.ChangeState(enemy.attackState);
                // Ejecutar UpdateState inmediatamente para que ataque en este turno
                if (stateMachine.currentState == enemy.attackState)
                {
                    stateMachine.currentState.UpdateState();
                }
                return;
            }
            // Si está en rango de visión (cono de visión), perseguir
            else if (enemy.IsInVisionRange(enemy.Target))
            {
                Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: 😴 IDLE - Jugador en cono de visión (distancia: {distanceToTarget:F2}), cambiando a CHASING");
                stateMachine.ChangeState(enemy.chasingState);
                return;
            }
        }

        // PRIORIDAD 2: Si no hay target, comportamiento idle
        // Verificar que haya puntos idle
        if (enemy.IdlePoints == null || enemy.IdlePoints.Length == 0)
        {
            // Si no hay puntos idle, solo esperar
            Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: 😴 IDLE - No hay puntos idle, esperando...");
            return;
        }

        // Espera unos turnos antes de moverse
        if (idleTurnsWaited < idleTurnsToWait)
        {
            idleTurnsWaited++;
            Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: 😴 IDLE - Esperando ({idleTurnsWaited}/{idleTurnsToWait})");
            return;
        }

        // Moverse un paso hacia el siguiente punto idle
        Transform idleTarget = enemy.IdlePoints[enemy.CurrentIdleIndex];
        if (idleTarget == null)
        {
            Debug.LogWarning($"[ENEMY_STATE] {enemy.gameObject.name}: 😴 IDLE - Punto idle {enemy.CurrentIdleIndex} es null");
            return;
        }
        
        Vector3 direction = (idleTarget.position - enemy.transform.position).normalized;
        enemy.moveEnemy(direction);

        if (Vector3.Distance(enemy.transform.position, idleTarget.position) < 0.5f)
        {
            enemy.CurrentIdleIndex = (enemy.CurrentIdleIndex + 1) % enemy.IdlePoints.Length;
            idleTurnsWaited = 0;
        }
    }
}
