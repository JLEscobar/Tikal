using UnityEngine;

public class EnemyPatrollingState : EnemyState
{
    private float waitTimer;
    private bool hasReachedPatrolPoint = false;
    public EnemyPatrollingState(Enemys enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
    }

    public override void EnterState()
    {
        waitTimer = 0f;
        hasReachedPatrolPoint = false;
        base.EnterState();
        enemy.canMove = true; // Asegurar que el enemigo pueda moverse
        Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: 🚶 PATROLLING - Entrando al estado de patrullaje (punto {enemy.CurrentPatrolIndex + 1}/{enemy.PatrolPoints.Length})");
        // Aqu� podr�as iniciar animaci�n de patrullaje
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
            Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: 🚶 PATROLLING - Target detectado a distancia: {distanceToTarget:F2}");
            
            // Si está en rango de ataque, atacar inmediatamente
            if (distanceToTarget <= enemy.AttackRange)
            {
                Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: 🚶 PATROLLING - ⚠️ Jugador en rango de ataque ({distanceToTarget:F2} <= {enemy.AttackRange}), cambiando a ATTACK");
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
                Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: 🚶 PATROLLING - Jugador en cono de visión (distancia: {distanceToTarget:F2}), cambiando a CHASING");
                stateMachine.ChangeState(enemy.chasingState);
                return;
            }
        }

        // PRIORIDAD 2: Si no hay target, patrullar
        // Verificar que haya puntos de patrulla
        if (enemy.PatrolPoints == null || enemy.PatrolPoints.Length == 0)
        {
            Debug.LogWarning($"[ENEMY_STATE] {enemy.gameObject.name}: 🚶 PATROLLING - No hay puntos de patrulla, cambiando a IDLE");
            stateMachine.ChangeState(enemy.idleState);
            return;
        }

        // Si ya llegó al punto de patrulla, esperar y luego cambiar a idle
        if (hasReachedPatrolPoint)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= enemy.IdleStateDuration)
            {
                Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: 🚶 PATROLLING - Tiempo de espera completado, cambiando a IDLE");
                stateMachine.ChangeState(enemy.idleState);
                return;
            }
            return; // Esperar sin moverse
        }

        // Avanza un paso hacia el siguiente punto de patrulla
        Transform patrolTarget = enemy.PatrolPoints[enemy.CurrentPatrolIndex];
        if (patrolTarget == null)
        {
            Debug.LogWarning($"[ENEMY_STATE] {enemy.gameObject.name}: 🚶 PATROLLING - Punto de patrulla {enemy.CurrentPatrolIndex} es null");
            return;
        }
        
        float distanceToPatrolPoint = Vector3.Distance(enemy.transform.position, patrolTarget.position);
        Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: 🚶 PATROLLING - Patrullando hacia punto {enemy.CurrentPatrolIndex + 1} (distancia: {distanceToPatrolPoint:F2})");
        Vector3 direction = (patrolTarget.position - enemy.transform.position).normalized;
        enemy.moveEnemy(direction);

        // Cuando llega al punto de patrulla, marcar que llegó y empezar a esperar
        if (distanceToPatrolPoint < 2f)
        {
            int oldIndex = enemy.CurrentPatrolIndex;
            enemy.CurrentPatrolIndex = (enemy.CurrentPatrolIndex + 1) % enemy.PatrolPoints.Length;
            hasReachedPatrolPoint = true;
            waitTimer = 0f;
            Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: 🚶 PATROLLING - Punto {oldIndex + 1} alcanzado, esperando {enemy.IdleStateDuration}s antes de cambiar a IDLE");
        }
    }

    public override void ExitState()
    {
        base.ExitState();
        // Detener animaci�n de patrullaje si aplica
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
