using UnityEngine;

public class EnemyAttackState : EnemyState
{
    private int attackCooldownTurns = 0; // Cooldown en turnos (0 = puede atacar cada turno)
    private int turnsSinceLastAttack = 0;
    private bool hasAttackedThisTurn = false; // Para evitar atacar múltiples veces en el mismo turno
    
    public EnemyAttackState(Enemys enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
    }
    
    public override void EnterState()
    {
        base.EnterState();
        enemy.canMove = false; // Detener movimiento cuando está atacando
        hasAttackedThisTurn = false; // Resetear el flag al entrar al estado
        Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: 🗡️ ATTACK - Entrando al estado de ataque (movimiento detenido)");
    }
    
    /// <summary>
    /// Decrementa el cooldown del ataque (llamado al inicio de cada turno)
    /// </summary>
    public void DecrementCooldown()
    {
        if (turnsSinceLastAttack > 0)
        {
            turnsSinceLastAttack--;
            Debug.Log($"[ENEMY_ATTACK] {enemy.gameObject.name}: Cooldown decrementado: {turnsSinceLastAttack}/{attackCooldownTurns}");
        }
        hasAttackedThisTurn = false; // Resetear el flag al inicio del turno
    }
    
    /// <summary>
    /// Instancia efectos visuales de ataque en la posición del target
    /// </summary>
    private void InstantiateAttackVFX(ITargetable target, float yOffset = 1.0f)
    {
        // Obtener el CharacterActor del enemigo para acceder a los VFX
        CharacterActor enemyActor = enemy.GetComponent<CharacterActor>();
        if (enemyActor == null) return;
        
        // Intentar obtener el prefab de VFX (prioridad: defaultAbilityVFXPrefab)
        GameObject vfxPrefab = enemyActor.defaultAbilityVFXPrefab;
        
        // Si no hay VFX básico, intentar con el especial
        if (vfxPrefab == null)
        {
            vfxPrefab = enemyActor.specialAbilityVFXPrefab;
        }
        
        if (vfxPrefab != null)
        {
            Vector3 targetPosition = target.GetTransform().position;
            Vector3 spawnPosition = new Vector3(targetPosition.x, targetPosition.y + yOffset, targetPosition.z);
            
            GameObject vfxInstance = Object.Instantiate(vfxPrefab, spawnPosition, Quaternion.identity);
            
            // Buscar y reproducir el ParticleSystem
            ParticleSystem ps = vfxInstance.GetComponent<ParticleSystem>();
            if (ps == null)
            {
                ps = vfxInstance.GetComponentInChildren<ParticleSystem>(true);
            }
            
            if (ps != null)
            {
                ps.Play();
                Debug.Log($"[ENEMY_VFX] {enemy.gameObject.name}: Efecto visual de ataque instanciado en {target.GetTransform().name}");
            }
            else
            {
                Debug.LogWarning($"[ENEMY_VFX] {enemy.gameObject.name}: No se encontró ParticleSystem en el prefab {vfxPrefab.name}");
            }
            
            // Destruir el VFX después de 3 segundos
            Object.Destroy(vfxInstance, 3f);
        }
        else
        {
            Debug.LogWarning($"[ENEMY_VFX] {enemy.gameObject.name}: No hay prefab de VFX asignado en CharacterActor");
        }
    }
    
    /// <summary>
    /// Aplica daño al target y muestra efectos visuales
    /// </summary>
    private void PerformAttack(ITargetable target)
    {
        if (target == null || target.Health == null)
        {
            Debug.LogWarning($"[ENEMY_ATTACK] {enemy.gameObject.name}: Target inválido para ataque");
            return;
        }
        
        // Obtener el CharacterActor del enemigo para calcular el daño
        CharacterActor enemyActor = enemy.GetComponent<CharacterActor>();
        int damage = 10; // Daño por defecto
        
        if (enemyActor != null)
        {
            damage = Mathf.Max(1, enemyActor.AttackPower);
        }
        else
        {
            // Fallback: usar un daño fijo si no hay CharacterActor
            damage = 10;
            Debug.LogWarning($"[ENEMY_ATTACK] {enemy.gameObject.name}: No se encontró CharacterActor, usando daño por defecto: {damage}");
        }
        
        // Aplicar el daño
        target.Health.TakeDamage(damage);
        
        // Instanciar efectos visuales
        InstantiateAttackVFX(target, 1.0f);
        
        // Mostrar mensaje
        string targetName = target.GetTransform().name;
        Debug.Log($"[ENEMY_ATTACK] {enemy.gameObject.name}: ⚔️ Atacó a {targetName} por {damage} de daño!");
        
        // Mostrar mensaje en el sistema de mensajes si está disponible
        if (MessagesSystem.Instance != null)
        {
            MessagesSystem.Instance.ShowMessage($"{enemy.gameObject.name} atacó a {targetName} por {damage} de daño!", Color.red);
        }
    }

    public override void AnimationTriggerEvent(Enemys.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
    }


    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }

    public override void ExitState()
    {
        base.ExitState();
        enemy.canMove = true; // Permitir movimiento al salir del estado de ataque
        Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: 🗡️ ATTACK - Saliendo del estado de ataque (movimiento habilitado)");
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

        // Actualizar target PRIMERO para asegurar que tenemos el target más cercano y válido
        enemy.UpdateTarget();
        
        // Verificar que el target exista y sea válido después de actualizar
        if (enemy.Target == null)
        {
            Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: 🗡️ ATTACK - No hay target válido, cambiando a PATROLLING");
            enemy.SetTarget(null); // Asegurar que el target está limpio
            stateMachine.ChangeState(enemy.patrollingState);
            // Buscar nuevos targets inmediatamente
            enemy.UpdateTarget();
            enemy.CompleteTurnAction();
            return;
        }
        
        // Verificar que el target siga vivo
        CharacterActor targetActor = enemy.Target.GetComponent<CharacterActor>();
        if (targetActor == null || targetActor.Health == null || targetActor.Health.IsDead)
        {
            Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: 🗡️ ATTACK - Target está muerto o inválido, cambiando a PATROLLING");
            enemy.SetTarget(null); // Limpiar el target inválido
            stateMachine.ChangeState(enemy.patrollingState);
            // Buscar nuevos targets inmediatamente
            enemy.UpdateTarget();
            enemy.CompleteTurnAction();
            return;
        }

        float distanceToPlayer = Vector3.Distance(enemy.transform.position, enemy.Target.position);
        
        Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: 🗡️ ATTACK - Verificando distancia: {distanceToPlayer:F2} (AttackRange: {enemy.AttackRange})");

        // Si está en rango de ataque, atacar
        if (distanceToPlayer <= enemy.AttackRange)
        {
            if (turnsSinceLastAttack >= attackCooldownTurns && !hasAttackedThisTurn)
            {
                Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: 🗡️ ATTACK - Atacando al jugador! (distancia: {distanceToPlayer:F2})");
                // Aqu� ir�a la l�gica real de da�o
                // Obtener el target como ITargetable
                if (enemy.Target != null)
                {
                    ITargetable target = enemy.Target.GetComponent<ITargetable>();
                    if (target == null)
                    {
                        // Intentar obtener CharacterActor que implementa ITargetable
                        CharacterActor targetActorComponent = enemy.Target.GetComponent<CharacterActor>();
                        if (targetActorComponent != null)
                        {
                            target = targetActorComponent;
                        }
                    }
                    
                    if (target != null)
                    {
                        // Disparar animación de ataque
                        enemy.TriggerAttackAnimation();
                        
                        // Realizar el ataque con daño y efectos visuales
                        PerformAttack(target);
                    }
                    else
                    {
                        Debug.LogWarning($"[ENEMY_ATTACK] {enemy.gameObject.name}: No se pudo obtener ITargetable del target {enemy.Target.name}");
                    }
                }
                
                hasAttackedThisTurn = true;
                turnsSinceLastAttack = attackCooldownTurns; // Establecer cooldown después de atacar
                
                // Completar la acción del turno después de atacar
                enemy.CompleteTurnAction();
            }
            else if (hasAttackedThisTurn)
            {
                // Ya atacó este turno, solo esperar
                Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: 🗡️ ATTACK - Ya atacó este turno, completando turno");
                enemy.CompleteTurnAction();
            }
            else
            {
                // Está en cooldown, completar turno sin atacar
                Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: 🗡️ ATTACK - En cooldown ({turnsSinceLastAttack}/{attackCooldownTurns}), completando turno");
                enemy.CompleteTurnAction();
            }
        }
        else
        {
            Debug.Log($"[ENEMY_STATE] {enemy.gameObject.name}: 🗡️ ATTACK - Jugador fuera de rango ({distanceToPlayer:F2} > {enemy.AttackRange}), cambiando a CHASING");
            stateMachine.ChangeState(enemy.chasingState);
        }

        if (enemy.CurrentHealth <= enemy.RetreatHealthThreshold)
        {
            stateMachine.ChangeState(enemy.retreatState);
        }
    }
}
