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
        enemy.canMove = true; // Asegurar que el enemigo pueda moverse
        enemy.SetWalkAnimation(true); // Activar animación de caminar
        Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: 🏃 CHASING - Entrando al estado de persecución");
    }

    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }

    public override void ExitState()
    {
        base.ExitState();
        enemy.SetWalkAnimation(false); // Detener animación de caminar
        Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: 🏃 CHASING - Saliendo del estado de persecución");
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
            
            Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: 🏃 CHASING - Target a distancia: {distanceToPlayer:F2} (AttackRange: {enemy.AttackRange}, VisionRange: {enemy.VisionRange})");

            // PRIORIDAD 1: Verificar si está en rango de ataque ANTES de moverse
            if (distanceToPlayer <= enemy.AttackRange)
            {
                Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: 🏃 CHASING - ⚠️ Jugador en rango de ataque ({distanceToPlayer:F2} <= {enemy.AttackRange}), DETENIÉNDOSE y cambiando a ATTACK");
                // NO moverse, solo cambiar de estado inmediatamente
                stateMachine.ChangeState(enemy.attackState);
                // Ejecutar UpdateState inmediatamente para que ataque en este turno
                if (stateMachine.currentState == enemy.attackState)
                {
                    stateMachine.currentState.UpdateState();
                }
                return;
            }

            // PRIORIDAD 2: Si está fuera de rango de ataque pero dentro del cono de visión, perseguir
            if (enemy.IsInVisionRange(enemy.Target))
            {
                Vector3 direction = (enemy.Target.position - enemy.transform.position).normalized;
                float distanceNeeded = distanceToPlayer - enemy.AttackRange;
                
                // Calcular la distancia a mover este turno (usar un porcentaje del rango de movimiento)
                float moveDistanceThisTurn = enemy.MovementRangePerTurn * 0.8f; // Mover 80% del rango por turno
                
                // Si la distancia necesaria es menor que el movimiento disponible, mover solo lo necesario
                if (distanceNeeded < moveDistanceThisTurn)
                {
                    moveDistanceThisTurn = distanceNeeded;
                }
                
                Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: 🏃 CHASING - Persiguiendo jugador (distancia: {distanceToPlayer:F2}, faltan {distanceNeeded:F2} para atacar, moviendo {moveDistanceThisTurn:F2})");
                
                // Mover directamente hacia el objetivo una distancia fija
                enemy.MoveTowardsTarget(direction, moveDistanceThisTurn);
                
                // IMPORTANTE: Verificar que el target siga siendo válido después del movimiento
                // (podría haberse vuelto inválido durante el movimiento, por ejemplo, si se murió)
                enemy.UpdateTarget();
                
                if (enemy.Target != null)
                {
                    // Verificar si después del movimiento está en rango de ataque
                    float newDistanceToPlayer = Vector3.Distance(enemy.transform.position, enemy.Target.position);
                    if (newDistanceToPlayer <= enemy.AttackRange)
                    {
                        Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: 🏃 CHASING - ⚠️ Después del movimiento, jugador en rango de ataque ({newDistanceToPlayer:F2} <= {enemy.AttackRange}), cambiando a ATTACK");
                        stateMachine.ChangeState(enemy.attackState);
                        // Ejecutar UpdateState inmediatamente para que ataque en este turno
                        if (stateMachine.currentState == enemy.attackState)
                        {
                            stateMachine.currentState.UpdateState();
                        }
                        return;
                    }
                    
                    // Si no está en rango después de moverse, completar el turno
                    enemy.CompleteTurnAction();
                    return;
                }
                else
                {
                    // El target se volvió inválido durante el movimiento
                    Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: 🏃 CHASING - Target se volvió inválido después del movimiento, cambiando a PATROLLING");
                    stateMachine.ChangeState(enemy.patrollingState);
                    enemy.CompleteTurnAction();
                    return;
                }
            }
            else
            {
                // Target está fuera del cono de visión
                Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: 🏃 CHASING - Target fuera del cono de visión (distancia: {distanceToPlayer:F2}), perdiendo target");
                enemy.SetTarget(null);
                // Completar el turno ya que no hay nada que hacer
                enemy.CompleteTurnAction();
                return;
            }

            enemy.CurrentSearchTurns = 0; // reinicia b�squeda
        }
        else if (enemy.LastSeenPosition.HasValue)
        {
            float distanceToLastSeen = Vector3.Distance(enemy.transform.position, enemy.LastSeenPosition.Value);
            Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: 🏃 CHASING - Moviéndose hacia última posición vista (distancia: {distanceToLastSeen:F2})");
            Vector3 direction = (enemy.LastSeenPosition.Value - enemy.transform.position).normalized;
            enemy.moveEnemy(direction);
            
            // Completar el turno después de moverse
            enemy.CompleteTurnAction();

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
