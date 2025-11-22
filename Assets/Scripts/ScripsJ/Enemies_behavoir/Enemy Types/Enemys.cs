using UnityEngine;

public enum EnemyTurnStartState
{
    Idle,          // Estado de reposo al iniciar el turno
    Patrolling,    // Patrullando puntos de patrulla al iniciar el turno
    Auto           // Automático: decide basándose en si hay target (comportamiento por defecto)
}

public class Enemys : MonoBehaviour, IDamageable, IEnemyMovable
{
    #region Damage variables
    [field: SerializeField] public bool IsDead { get; set; }
    [field: SerializeField] public int CurrentHealth { get; set; }
    [field: SerializeField] public int MaxHealth { get; set; } = 100;

    #endregion

    #region Movement Variables
    public CharacterController controller { get; set; }
    private CharacterMovement characterMovement; // Referencia al componente CharacterMovement
    public bool canMove { get; set; }
    public bool facingPlayer { get; set; } = false;
    [field: SerializeField] public float moveSpeed { get; set; }

    [SerializeField] private Transform target;
    public Transform Target => target;
    
    /// <summary>
    /// Establece el target del enemigo
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            LastSeenPosition = target.position;
        }
    }

    private Vector3? lastSeenPosition;
    public Vector3? LastSeenPosition
    {
        get => lastSeenPosition;
        set => lastSeenPosition = value;
    }

    [SerializeField] private int maxSearchTurns = 1; 
    private int currentSearchTurns = 0;

    public int MaxSearchTurns => maxSearchTurns;
    public int CurrentSearchTurns
    {
        get => currentSearchTurns;
        set => currentSearchTurns = value;
    }

    // Rango de movimiento por turno (similar a los jugadores)
    private Vector3 startPositionOfTurn; // Posición inicial del turno actual
    private float movementRangePerTurn = 5f; // Rango de movimiento por turno (se obtiene de CharacterStats)
    [SerializeField] private bool useMovementRange = true; // Activar/desactivar límite de rango
    
    // Rango de movimiento permanente (opcional, para limitar el área total de movimiento)
    [SerializeField] private Transform movementReferencePoint; // Punto de referencia permanente (opcional)
    [SerializeField] private float maxMovementRange = 15f; // Distancia máxima permanente desde el punto de referencia (0 = desactivado)
    
    private Vector3 initialPosition; // Posición inicial guardada en Start()
    
    public float MovementRangePerTurn => movementRangePerTurn;
    public bool UseMovementRange => useMovementRange;
    public Vector3 StartPositionOfTurn => startPositionOfTurn;
    public Vector3 MovementReferencePosition => movementReferencePoint != null ? movementReferencePoint.position : initialPosition;
    #endregion

    #region State Machine Variables

    [Header("Turn Start State Configuration")]
    [Tooltip("Estado en el que el enemigo iniciará en su PRIMER turno (si no hay target en rango). Solo se aplica al primer turno del enemigo. Todos los enemigos inician en WaitingTurn al comenzar el juego.")]
    [SerializeField] private EnemyTurnStartState turnStartState = EnemyTurnStartState.Auto;

    public EnemyStateMachine stateMachine { get; set; }

    public EnemyIdleState idleState { get; set; }

    public EnemyChasingState chasingState { get; set; }

    public EnemyAttackState attackState { get; set; }
    public EnemyPatrollingState patrollingState { get; set; }
    public EnemyRetreatState retreatState { get; set; }
    public EnemyWaitingTurnState waitingTurnState { get; set; }

    #endregion

    #region Turn System Integration

    private TurnSystem turnSystem;
    private CharacterActor characterActor;
    private bool isMyTurn = false;
    private bool hasCompletedTurnAction = false;
    private float turnStartTime;
    private float lastActionTime;
    private bool isFirstTurn = true; // Bandera para rastrear si es el primer turno del enemigo
    private const float MAX_TURN_DURATION = 10f; // Máximo tiempo para completar un turno (segundos)
    private const float AUTO_COMPLETE_DELAY = 0.5f; // Tiempo después de una acción para completar automáticamente (segundos)

    public bool IsMyTurn => isMyTurn;
    public bool HasCompletedTurnAction => hasCompletedTurnAction;

    #endregion

    #region Stats Variables
    // Rango de visi�n
    [SerializeField] private float visionRange = 10f;
    public float VisionRange => visionRange;

    // Ángulo de visión (en grados, medido desde la dirección frontal)
    // Por ejemplo: 90 grados = 45° a cada lado, 180 grados = 90° a cada lado
    [SerializeField] private float visionAngle = 90f;
    public float VisionAngle => visionAngle;

    // Rango de ataque
    [SerializeField] private float attackRange = 2f;
    public float AttackRange => attackRange;

    // Umbral de salud para retirarse
    [SerializeField] private float retreatHealthThreshold = 30f;
    public float RetreatHealthThreshold => retreatHealthThreshold;

    // Puntos de patrullaje
    [SerializeField] private Transform[] patrolPoints;
    public Transform[] PatrolPoints => patrolPoints;

    // Puntos de espera (idle)
    [SerializeField] private Transform[] idlePoints;
    public Transform[] IdlePoints => idlePoints;

    // �ndices de patrullaje/idle
    private int currentPatrolIndex = 0;
    public int CurrentPatrolIndex
    {
        get => currentPatrolIndex;
        set => currentPatrolIndex = value;
    }

    private int currentIdleIndex = 0;
    public int CurrentIdleIndex
    {
        get => currentIdleIndex;
        set => currentIdleIndex = value;
    }

    #endregion


    private void Awake()
    {
        //State Machine init
        stateMachine = new EnemyStateMachine(this); // Pasar referencia del enemigo para logs
        idleState = new EnemyIdleState(this, stateMachine);
        chasingState = new EnemyChasingState(this, stateMachine);
        attackState = new EnemyAttackState(this, stateMachine);
        patrollingState = new EnemyPatrollingState(this, stateMachine);
        retreatState = new EnemyRetreatState(this, stateMachine);
        waitingTurnState = new EnemyWaitingTurnState(this, stateMachine);
        
        Debug.Log($"[ENEMY] {gameObject.name}: Máquina de estados inicializada con {gameObject.name}");
        
        // Turn System Integration
        turnSystem = FindFirstObjectByType<TurnSystem>();
        characterActor = GetComponent<CharacterActor>();
    }
    
    /// <summary>
    /// Obtiene el estado en el que el enemigo debe iniciar su turno según la configuración
    /// </summary>
    private EnemyState GetTurnStartState()
    {
        switch (turnStartState)
        {
            case EnemyTurnStartState.Idle:
                return idleState;
            case EnemyTurnStartState.Patrolling:
                return patrollingState;
            case EnemyTurnStartState.Auto:
            default:
                // Auto: decidir basándose en si hay target y puntos de patrulla
                if (Target != null)
                {
                    float distanceToTarget = Vector3.Distance(transform.position, Target.position);
                    if (distanceToTarget <= AttackRange)
                    {
                        return attackState;
                    }
                    else if (IsInVisionRange(Target))
                    {
                        return chasingState;
                    }
                }
                // Si no hay target o está fuera de rango, usar patrolling si hay puntos, sino idle
                if (PatrolPoints != null && PatrolPoints.Length > 0)
                {
                    return patrollingState;
                }
                return idleState;
        }
    }
    private void Start()
    {
        //state machine - SIEMPRE iniciar en WaitingTurn (necesario para el sistema de turnos)
        stateMachine.Initialize(waitingTurnState);
        Debug.Log($"[ENEMY] {gameObject.name}: Estado inicial: WaitingTurn, Estado al iniciar turno: {turnStartState}");

        //Movement
        controller = GetComponent<CharacterController>();
        characterMovement = GetComponent<CharacterMovement>(); // Intentar obtener CharacterMovement
        
        // IMPORTANTE: Deshabilitar CharacterMovement si existe para evitar conflictos
        // Enemys.cs maneja su propio movimiento y no debe interferir CharacterMovement
        if (characterMovement != null)
        {
            // Detener cualquier movimiento activo de CharacterMovement
            characterMovement.Stop();
            // Deshabilitar el componente para que no interfiera
            characterMovement.enabled = false;
            Debug.Log($"[ENEMY] {gameObject.name}: CharacterMovement deshabilitado para evitar conflictos con Enemys.cs");
        }
        
        // IMPORTANTE: Deshabilitar SimpleAIController si existe para evitar conflictos
        // SimpleAIController usa CharacterMovement que puede interferir con Enemys.cs
        SimpleAIController simpleAI = GetComponent<SimpleAIController>();
        if (simpleAI != null)
        {
            simpleAI.enabled = false;
            Debug.Log($"[ENEMY] {gameObject.name}: SimpleAIController deshabilitado para evitar conflictos con Enemys.cs");
        }
        
        // Asegurar que CharacterController esté habilitado
        if (controller != null)
        {
            controller.enabled = true;
            Debug.Log($"[ENEMY] {gameObject.name}: CharacterController habilitado");
        }
        else
        {
            Debug.LogWarning($"[ENEMY] {gameObject.name}: CharacterController no encontrado!");
        }
        
        // Configurar moveSpeed si hay CharacterActor
        if (characterActor != null && characterActor.Stats != null)
        {
            moveSpeed = characterActor.Stats.moveSpeed;
            Debug.Log($"[ENEMY] {gameObject.name}: MoveSpeed configurado a {moveSpeed} desde CharacterStats");
        }
        else if (moveSpeed <= 0)
        {
            moveSpeed = 5f; // Valor por defecto
            Debug.LogWarning($"[ENEMY] {gameObject.name}: MoveSpeed era 0, configurando a valor por defecto: {moveSpeed}");
        }

        // Guardar posición inicial para el rango de movimiento permanente
        initialPosition = transform.position;
        
        // Obtener movementRange del CharacterStats (similar a los jugadores)
        if (characterActor != null && characterActor.Stats != null)
        {
            movementRangePerTurn = characterActor.Stats.movementRange;
            Debug.Log($"[ENEMY] {gameObject.name}: Rango de movimiento por turno configurado desde CharacterStats: {movementRangePerTurn}");
        }
        else
        {
            movementRangePerTurn = 5f; // Valor por defecto
            Debug.LogWarning($"[ENEMY] {gameObject.name}: CharacterStats no encontrado, usando rango de movimiento por defecto: {movementRangePerTurn}");
        }
        
        if (movementReferencePoint == null)
        {
            Debug.Log($"[ENEMY] {gameObject.name}: Rango de movimiento permanente desactivado (maxMovementRange: {maxMovementRange})");
        }
        else
        {
            Debug.Log($"[ENEMY] {gameObject.name}: Rango de movimiento permanente configurado desde punto de referencia '{movementReferencePoint.name}'. Rango máximo: {maxMovementRange}");
        }
        
        target = GameObject.FindWithTag("Player").transform;

        // Asegurar que CharacterActor esté disponible
        if (characterActor == null)
        {
            characterActor = GetComponent<CharacterActor>();
        }

        // Sincronizar salud con CharacterActor si está disponible
        if (characterActor != null && characterActor.Health != null)
        {
            MaxHealth = characterActor.Health.MaxHealth;
            CurrentHealth = characterActor.Health.CurrentHealth;
            IsDead = characterActor.Health.IsDead;
        }
        else
        {
            // Si no hay CharacterActor, usar valores por defecto
            if (CurrentHealth <= 0)
            {
                CurrentHealth = MaxHealth;
            }
            IsDead = false;
        }

        // Suscribirse a eventos del TurnSystem
        if (turnSystem != null)
        {
            turnSystem.OnTurnStarted += HandleTurnStarted;
            turnSystem.OnTurnEnded += HandleTurnEnded;
        }

        // Registrar este enemigo en el TurnSystem
        RegisterWithTurnSystem();
    }

    private void RegisterWithTurnSystem()
    {
        // Intentar registrar el enemigo en el TurnSystem
        if (turnSystem == null)
        {
            turnSystem = FindFirstObjectByType<TurnSystem>();
        }

        if (turnSystem != null && characterActor != null)
        {
            turnSystem.AddEnemyActor(characterActor);
            Debug.Log($"[ENEMY] {gameObject.name} registrado en TurnSystem como {characterActor.CharacterName}");
        }
        else
        {
            // Si no se pudo registrar, intentar de nuevo en el siguiente frame
            if (characterActor == null)
            {
                Debug.LogWarning($"[ENEMY] {gameObject.name}: CharacterActor no encontrado. Intentando registrar más tarde...");
                Invoke(nameof(RegisterWithTurnSystem), 0.1f);
            }
            else if (turnSystem == null)
            {
                Debug.LogWarning($"[ENEMY] {gameObject.name}: TurnSystem no encontrado. Intentando registrar más tarde...");
                Invoke(nameof(RegisterWithTurnSystem), 0.1f);
            }
        }
    }

    private void OnDestroy()
    {
        // Desuscribirse de eventos
        if (turnSystem != null)
        {
            turnSystem.OnTurnStarted -= HandleTurnStarted;
            turnSystem.OnTurnEnded -= HandleTurnEnded;
        }
    }

    private void Update()
    {
        // Sincronizar salud con CharacterActor periódicamente
        if (characterActor != null && characterActor.Health != null)
        {
            CurrentHealth = characterActor.Health.CurrentHealth;
            MaxHealth = characterActor.Health.MaxHealth;
            IsDead = characterActor.Health.IsDead;
        }

        // Solo ejecutar la máquina de estados si es nuestro turno
        if (isMyTurn && !hasCompletedTurnAction)
        {
            // PRIORIDAD 1: Timeout automático para evitar turnos infinitos (máxima prioridad)
            if (Time.time - turnStartTime > MAX_TURN_DURATION)
            {
                Debug.LogWarning($"[ENEMY] {gameObject.name}: Turno excedió el tiempo máximo. Completando automáticamente.");
                CompleteTurnAction();
                return;
            }

            // PRIORIDAD 2: Completar automáticamente si ha pasado tiempo desde la última acción
            // Solo si NO se activó el timeout máximo (evitar llamadas duplicadas)
            // Esto ayuda a estados que no llaman explícitamente a CompleteTurnAction()
            if (Time.time - lastActionTime > AUTO_COMPLETE_DELAY && lastActionTime > 0 && Time.time - turnStartTime <= MAX_TURN_DURATION)
            {
                Debug.Log($"[ENEMY] {gameObject.name}: Completando turno automáticamente después de acción.");
                CompleteTurnAction();
                return;
            }

            stateMachine.currentState.UpdateState();
        }
    }

    private void FixedUpdate()
    {
        // Solo ejecutar física si es nuestro turno
        if (isMyTurn && !hasCompletedTurnAction)
        {
            stateMachine.currentState.PhysicsUpdate();
        }
    }
    #region Damage/Die
    public void TakeDamage(int damageAmount)
    {
        // Sincronizar con CharacterActor si está disponible
        if (characterActor != null && characterActor.Health != null)
        {
            // Usar el sistema de salud de CharacterActor
            characterActor.Health.TakeDamage(damageAmount);
            CurrentHealth = characterActor.Health.CurrentHealth;
            IsDead = characterActor.Health.IsDead;
        }
        else
        {
            // Fallback al sistema local de salud
            CurrentHealth -= damageAmount;
            if (CurrentHealth <= 0 && !IsDead)
            {
                Die();
            }
        }
    }
    
    public void Die()
    {
        if (IsDead) return;
        
        IsDead = true;
        Debug.Log($"[ENEMY] {gameObject.name} has died.");
        
        // Sincronizar con CharacterActor si está disponible
        if (characterActor != null && characterActor.Health != null)
        {
            // El CharacterActor ya debería estar muerto si recibió el daño a través de él
            // Pero por si acaso, sincronizamos
            IsDead = characterActor.Health.IsDead;
        }
    }

    #endregion

    #region movement/facing
    
    /// <summary>
    /// Verifica si una posición está dentro del rango de movimiento permitido
    /// </summary>
    /// <param name="position">Posición a verificar</param>
    /// <returns>True si la posición está dentro del rango</returns>
    public bool IsWithinMovementRange(Vector3 position)
    {
        if (!useMovementRange) return true; // Si el rango está desactivado, siempre permitir
        
        // Verificar solo el rango por turno (desde la posición inicial del turno)
        float distanceFromTurnStart = Vector3.Distance(position, startPositionOfTurn);
        // Reducir el rango ligeramente para evitar clipeo con el prefab visual del rango
        float controllerRadius = controller != null ? controller.radius : 0f;
        float maxDistanceThisTurn = movementRangePerTurn - controllerRadius - 0.1f; // Margen adicional para evitar clipeo
        
        if (distanceFromTurnStart > maxDistanceThisTurn)
        {
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Limita una dirección de movimiento para que no exceda el rango permitido
    /// </summary>
    /// <param name="direction">Dirección de movimiento deseada</param>
    /// <param name="moveDistance">Distancia del movimiento</param>
    /// <returns>Dirección ajustada que respeta el rango, o Vector3.zero si no puede moverse</returns>
    private Vector3 ClampMovementToRange(Vector3 direction, float moveDistance)
    {
        if (!useMovementRange) return direction; // Si el rango está desactivado, no limitar
        
        Vector3 currentPos = transform.position;
        Vector3 desiredPos = currentPos + direction * moveDistance;
        
        // Verificar solo el rango por turno (desde la posición inicial del turno)
        float distanceFromTurnStart = Vector3.Distance(desiredPos, startPositionOfTurn);
        // Reducir el rango ligeramente para evitar clipeo con el prefab visual del rango
        float controllerRadius = controller != null ? controller.radius : 0f;
        float maxDistanceThisTurn = movementRangePerTurn - controllerRadius - 0.1f; // Margen adicional para evitar clipeo
        
        if (distanceFromTurnStart > maxDistanceThisTurn)
        {
            // Calcular la dirección máxima permitida desde startPositionOfTurn
            Vector3 directionFromTurnStart = (desiredPos - startPositionOfTurn);
            if (directionFromTurnStart.magnitude < 0.001f)
            {
                // Si la dirección es muy pequeña, no mover
                return Vector3.zero;
            }
            directionFromTurnStart = directionFromTurnStart.normalized;
            Vector3 maxAllowedPosFromTurn = startPositionOfTurn + directionFromTurnStart * maxDistanceThisTurn;
            Vector3 limitedDirection = (maxAllowedPosFromTurn - currentPos);
            
            // Asegurar que la dirección limitada no sea mayor que el movimiento original
            // Esto previene teletransportes
            if (limitedDirection.magnitude > moveDistance * 1.2f)
            {
                // Si el movimiento limitado es demasiado grande, usar la dirección original pero limitada
                limitedDirection = direction.normalized * moveDistance;
            }
            else
            {
                limitedDirection = limitedDirection.normalized;
            }
            
            Debug.Log($"[ENEMY_MOVEMENT] {gameObject.name}: ⚠️ Movimiento limitado por rango del turno (distancia: {distanceFromTurnStart:F2} > {maxDistanceThisTurn:F2})");
            return limitedDirection;
        }
        
        // Si está dentro del rango, permitir el movimiento completo
        return direction;
    }
    
    public void moveEnemy(Vector3 direction)
    {
        if (direction == Vector3.zero)
        {
            Debug.LogWarning($"[ENEMY_MOVEMENT] {gameObject.name}: Intento de movimiento con dirección cero");
            return;
        }

        // Verificar que el enemigo pueda moverse
        if (!canMove)
        {
            Debug.LogWarning($"[ENEMY_MOVEMENT] {gameObject.name}: canMove es false, movimiento bloqueado. Estado: {stateMachine.currentState?.GetType().Name}");
            return;
        }

        // Verificar que moveSpeed sea válido
        if (moveSpeed <= 0)
        {
            Debug.LogWarning($"[ENEMY_MOVEMENT] {gameObject.name}: moveSpeed es {moveSpeed}, usando valor por defecto");
            moveSpeed = 5f;
        }

        // Registrar que se está realizando una acción
        if (isMyTurn)
        {
            lastActionTime = Time.time;
        }

        // Asegurar que CharacterController esté habilitado
        if (controller != null && !controller.enabled)
        {
            Debug.LogWarning($"[ENEMY_MOVEMENT] {gameObject.name}: CharacterController estaba deshabilitado, habilitándolo...");
            controller.enabled = true;
        }

        // Calcular movimiento con limitaciones de rango
        float moveDistance = moveSpeed * Time.deltaTime;
        Vector3 clampedDirection = ClampMovementToRange(direction, moveDistance);
        
        if (clampedDirection == Vector3.zero)
        {
            Debug.Log($"[ENEMY_MOVEMENT] {gameObject.name}: Movimiento bloqueado - fuera del rango permitido");
            return;
        }

        // Movimiento con CharacterController
        if (controller != null && controller.enabled)
        {
            Vector3 move = clampedDirection * moveSpeed * Time.deltaTime;
            
            // Verificación final: asegurar que el movimiento no cause que el enemigo se salga del rango
            Vector3 finalPosition = transform.position + move;
            if (useMovementRange && !IsWithinMovementRange(finalPosition))
            {
                // Si aún está fuera del rango, reducir el movimiento gradualmente
                float reductionFactor = 0.5f;
                move = move * reductionFactor;
                finalPosition = transform.position + move;
                
                // Si después de reducir sigue fuera, bloquear completamente
                if (!IsWithinMovementRange(finalPosition))
                {
                    Debug.Log($"[ENEMY_MOVEMENT] {gameObject.name}: Movimiento bloqueado - aún fuera del rango después de reducir");
                    return;
                }
            }
            
            // Asegurar que el movimiento no sea excesivamente grande (prevenir teletransporte)
            float maxAllowedMove = moveSpeed * Time.deltaTime * 1.2f;
            if (move.magnitude > maxAllowedMove)
            {
                Debug.LogWarning($"[ENEMY_MOVEMENT] {gameObject.name}: ⚠️ Movimiento demasiado grande ({move.magnitude:F3} > {maxAllowedMove:F3}), limitando...");
                move = move.normalized * maxAllowedMove;
            }
            
            controller.Move(move);
            
            Debug.Log($"[ENEMY_MOVEMENT] {gameObject.name}: Moviéndose en dirección {clampedDirection} a velocidad {moveSpeed} (distancia este frame: {move.magnitude:F3})");
            
            // Rotación
            if (target != null)
            {
                CheckFacing(clampedDirection);
            }
            else
            {
                Quaternion targetRotation = Quaternion.LookRotation(clampedDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
            }
            return;
        }

        // Último recurso: Modificar transform directamente (solo si no hay CharacterController)
        Debug.LogWarning($"[ENEMY_MOVEMENT] {gameObject.name}: Usando movimiento directo de transform (no recomendado). CharacterController no disponible.");
        
        Vector3 fallbackMove = clampedDirection * moveSpeed * Time.deltaTime;
        Vector3 fallbackNewPos = transform.position + fallbackMove;
        
        // Verificar rango antes de mover (solo rango por turno)
        if (useMovementRange && !IsWithinMovementRange(fallbackNewPos))
        {
            // Si está fuera del rango, reducir el movimiento
            fallbackMove = fallbackMove * 0.5f;
            fallbackNewPos = transform.position + fallbackMove;
            
            // Si después de reducir sigue fuera, bloquear
            if (!IsWithinMovementRange(fallbackNewPos))
            {
                Debug.Log($"[ENEMY_MOVEMENT] {gameObject.name}: Movimiento fallback bloqueado - fuera del rango");
                return;
            }
        }
        
        transform.position += fallbackMove;

        // Solo rotamos si hay target
        if (target != null)
        {
            CheckFacing(clampedDirection);
        }
        else
        {
            // Si no hay target, rotamos hacia la dirección de movimiento
            Quaternion targetRotation = Quaternion.LookRotation(clampedDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }
    }

    /// <summary>
    /// Mueve al enemigo una distancia fija hacia una dirección (para movimiento por turno)
    /// </summary>
    /// <param name="direction">Dirección normalizada hacia donde moverse</param>
    /// <param name="distance">Distancia a mover</param>
    public void MoveTowardsTarget(Vector3 direction, float distance)
    {
        if (direction == Vector3.zero || distance <= 0)
        {
            Debug.LogWarning($"[ENEMY_MOVEMENT] {gameObject.name}: MoveTowardsTarget llamado con dirección cero o distancia inválida");
            return;
        }

        if (!canMove)
        {
            Debug.LogWarning($"[ENEMY_MOVEMENT] {gameObject.name}: canMove es false, movimiento bloqueado");
            return;
        }

        // Asegurar que CharacterController esté habilitado
        if (controller != null && !controller.enabled)
        {
            Debug.LogWarning($"[ENEMY_MOVEMENT] {gameObject.name}: CharacterController estaba deshabilitado, habilitándolo...");
            controller.enabled = true;
        }

        // Normalizar la dirección
        direction = direction.normalized;
        
        // Calcular la posición deseada
        Vector3 desiredPosition = transform.position + direction * distance;
        
        // Verificar y limitar por rango de movimiento
        if (useMovementRange && !IsWithinMovementRange(desiredPosition))
        {
            // Calcular la distancia máxima permitida desde startPositionOfTurn
            float controllerRadius = controller != null ? controller.radius : 0f;
            float maxDistanceThisTurn = movementRangePerTurn - controllerRadius - 0.1f;
            
            Vector3 directionFromTurnStart = (desiredPosition - startPositionOfTurn);
            if (directionFromTurnStart.magnitude > 0.001f)
            {
                directionFromTurnStart = directionFromTurnStart.normalized;
                desiredPosition = startPositionOfTurn + directionFromTurnStart * maxDistanceThisTurn;
                distance = Vector3.Distance(transform.position, desiredPosition);
                
                Debug.Log($"[ENEMY_MOVEMENT] {gameObject.name}: Movimiento limitado por rango del turno, distancia ajustada a {distance:F2}");
            }
            else
            {
                Debug.Log($"[ENEMY_MOVEMENT] {gameObject.name}: No se puede mover - ya está en el límite del rango");
                return;
            }
        }
        
        // Calcular el movimiento final
        Vector3 move = direction * distance;
        
        // Mover con CharacterController
        if (controller != null && controller.enabled)
        {
            controller.Move(move);
            Debug.Log($"[ENEMY_MOVEMENT] {gameObject.name}: Movido {distance:F2} unidades hacia {direction}");
        }
        else
        {
            // Fallback: mover directamente
            transform.position += move;
            Debug.LogWarning($"[ENEMY_MOVEMENT] {gameObject.name}: Usando movimiento directo (CharacterController no disponible)");
        }
        
        // Rotar hacia la dirección de movimiento
        if (target != null)
        {
            CheckFacing(direction);
        }
        else
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = targetRotation;
        }
        
        // Registrar acción
        if (isMyTurn)
        {
            lastActionTime = Time.time;
        }
    }

    public void CheckFacing(Vector3 direction)
    {
        // Si no hay target, no intentamos rotar
        if (target == null)
        {
            Debug.LogWarning("CheckFacing llamado sin target en " + gameObject.name);
            return;
        }

        Vector3 lookDirection = target.position - transform.position;
        lookDirection.y = 0f;

        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }
    }
    #endregion

    #region Animations/triggers
    private void AnimationTriggerEvent(AnimationTriggerType triggerType)
    {
        //stateMachine.CurrentState.AnimationTriggerEvent(triggerType);
    }
    public enum AnimationTriggerType     {
        EnemyDamaged,
        playFootstepSound
    }
    #endregion

    /// <summary>
    /// Verifica si un target está dentro del cono de visión del enemigo
    /// (tanto en distancia como en ángulo)
    /// </summary>
    /// <param name="targetPosition">Posición del target a verificar</param>
    /// <returns>True si el target está dentro del cono de visión</returns>
    public bool IsInVisionRange(Vector3 targetPosition)
    {
        // Calcular la distancia al target
        float distance = Vector3.Distance(transform.position, targetPosition);
        
        // Verificar si está dentro del rango de distancia
        if (distance > visionRange)
        {
            return false;
        }

        // Calcular el ángulo entre la dirección frontal del enemigo y la dirección hacia el target
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        Vector3 forwardDirection = transform.forward;
        
        // Calcular el ángulo en grados
        float angle = Vector3.Angle(forwardDirection, directionToTarget);
        
        // Verificar si el ángulo está dentro del cono de visión (mitad del ángulo total a cada lado)
        float halfVisionAngle = visionAngle * 0.5f;
        bool isInAngle = angle <= halfVisionAngle;
        
        return isInAngle;
    }

    /// <summary>
    /// Verifica si un Transform está dentro del cono de visión del enemigo
    /// </summary>
    /// <param name="target">Transform del target a verificar</param>
    /// <returns>True si el target está dentro del cono de visión</returns>
    public bool IsInVisionRange(Transform target)
    {
        if (target == null) return false;
        return IsInVisionRange(target.position);
    }

    public void UpdateTarget()
    {
        // Buscar el jugador más cercano válido dentro del rango de visión
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Transform closestValidTarget = null;
        float closestDistance = Mathf.Infinity;
        
        Debug.Log($"[ENEMY_TARGET] {gameObject.name}: 🔍 Buscando targets... Encontrados {players.Length} objetos con tag 'Player'");

        // IMPORTANTE: Verificar y limpiar el target actual si es inválido ANTES de buscar nuevos targets
        bool currentTargetIsValid = false;
        float currentTargetDistance = Mathf.Infinity;
        
        if (target != null)
        {
            CharacterActor currentTargetActor = target.GetComponent<CharacterActor>();
            
            // Verificar si el target actual es válido (vivo y dentro del cono de visión)
            if (currentTargetActor != null && currentTargetActor.Health != null && !currentTargetActor.Health.IsDead)
            {
                // Verificar si el target actual está dentro del cono de visión
                if (IsInVisionRange(target))
                {
                    currentTargetDistance = Vector3.Distance(transform.position, target.position);
                    currentTargetIsValid = true;
                }
                else
                {
                    // El target actual está fuera del cono de visión
                    Debug.Log($"[ENEMY_TARGET] {gameObject.name}: ❌ Target actual {target.name} está fuera del cono de visión, limpiando target");
                    currentTargetIsValid = false;
                }
            }
            else
            {
                // El target actual está muerto o no tiene CharacterActor
                Debug.Log($"[ENEMY_TARGET] {gameObject.name}: ❌ Target actual {target.name} está muerto o inválido, limpiando target");
                currentTargetIsValid = false;
            }
            
            // Si el target actual es inválido, limpiarlo inmediatamente
            if (!currentTargetIsValid)
            {
                SetTarget(null);
                target = null; // Asegurar que se limpia
            }
        }

        // Buscar el jugador más cercano válido dentro del cono de visión
        int validTargetsFound = 0;
        foreach (GameObject player in players)
        {
            // Verificar que el jugador esté vivo
            CharacterActor playerActor = player.GetComponent<CharacterActor>();
            if (playerActor == null || playerActor.Health == null || playerActor.Health.IsDead)
            {
                Debug.Log($"[ENEMY_TARGET] {gameObject.name}: ⏭️ Saltando {player.name} - muerto o sin CharacterActor");
                continue; // Saltar jugadores muertos o sin CharacterActor
            }

            // Verificar si el jugador está dentro del cono de visión (distancia Y ángulo)
            if (IsInVisionRange(player.transform))
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                validTargetsFound++;
                Debug.Log($"[ENEMY_TARGET] {gameObject.name}: ✅ Target válido encontrado: {player.name} a distancia {distance:F2}");
                
                // Buscar el jugador más cercano dentro del cono de visión
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestValidTarget = player.transform;
                }
            }
            else
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                Debug.Log($"[ENEMY_TARGET] {gameObject.name}: ⏭️ {player.name} está fuera del cono de visión (distancia: {distance:F2}, visionRange: {visionRange})");
            }
        }
        
        Debug.Log($"[ENEMY_TARGET] {gameObject.name}: 📊 Resumen: {validTargetsFound} targets válidos encontrados, más cercano: {(closestValidTarget != null ? closestValidTarget.name : "ninguno")}");

        // Decidir si establecer o cambiar de target
        if (closestValidTarget != null)
        {
            // Si no tenemos target o el nuevo es diferente/más cercano, cambiar
            if (target == null)
            {
                SetTarget(closestValidTarget);
                Debug.Log($"[ENEMY_TARGET] {gameObject.name}: 🎯 Nuevo target establecido: {closestValidTarget.name} a distancia {closestDistance:F2}");
            }
            else if (target != closestValidTarget)
            {
                // El nuevo target es diferente al actual (más cercano o el actual era inválido)
                SetTarget(closestValidTarget);
                Debug.Log($"[ENEMY_TARGET] {gameObject.name}: 🔄 Cambiando a target: {closestValidTarget.name} (distancia: {closestDistance:F2})");
            }
            else if (target == closestValidTarget && currentTargetIsValid)
            {
                // El target actual sigue siendo válido y es el más cercano
                Debug.Log($"[ENEMY_TARGET] {gameObject.name}: ✅ Manteniendo target actual {target.name} (distancia: {currentTargetDistance:F2}) - es el más cercano");
            }
        }
        else
        {
            // No hay targets válidos en rango
            if (target != null)
            {
                Debug.Log($"[ENEMY_TARGET] {gameObject.name}: ⚠️ No hay targets válidos en rango, perdiendo target actual");
                SetTarget(null);
            }
        }
    }

    public void ExecuteTurn()
    {
        if (IsDead) return;
        stateMachine.currentState.UpdateState();
    }

    #region Turn System Handlers

    private void HandleTurnStarted(Team team, CharacterActor actor)
    {
        string currentStateName = stateMachine.currentState != null ? stateMachine.currentState.GetType().Name : "Null";
        
        // Verificar si es nuestro turno
        if (team == Team.Enemy && characterActor != null && actor == characterActor)
        {
            isMyTurn = true;
            hasCompletedTurnAction = false;
            turnStartTime = Time.time; // Registrar el tiempo de inicio del turno
            lastActionTime = 0f; // Resetear el tiempo de última acción
            
            // IMPORTANTE: Guardar la posición inicial del turno ANTES de cualquier movimiento
            // Esto resetea el rango de movimiento por turno, permitiendo que el enemigo se mueva normalmente
            // desde su posición actual en este nuevo turno
            startPositionOfTurn = transform.position;
            
            // Actualizar movementRangePerTurn desde CharacterStats (por si cambió)
            if (characterActor != null && characterActor.Stats != null)
            {
                movementRangePerTurn = characterActor.Stats.movementRange;
            }
            
            Debug.Log($"[ENEMY_TURN] {gameObject.name}: ⚔️ TURNO INICIADO - Estado actual: [{currentStateName}], Rango de movimiento: {movementRangePerTurn}, Posición inicial del turno: {startPositionOfTurn}");
            
            // Decrementar cooldown del ataque si está en estado de ataque
            if (stateMachine.currentState == attackState && attackState != null)
            {
                attackState.DecrementCooldown();
            }
            
            // SIEMPRE verificar el target al inicio del turno (prioridad sobre cualquier otro estado)
            UpdateTarget();
            
            // Si hay un target, verificar la distancia y cambiar de estado si es necesario
            if (Target != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, Target.position);
                Debug.Log($"[ENEMY_TURN] {gameObject.name}: Target encontrado a distancia: {distanceToTarget:F2} (AttackRange: {AttackRange}, VisionRange: {VisionRange})");
                
                // PRIORIDAD MÁXIMA: Si está en rango de ataque, cambiar a ATTACK sin importar el estado actual
                if (distanceToTarget <= AttackRange)
                {
                    if (stateMachine.currentState != attackState)
                    {
                        Debug.Log($"[ENEMY_TURN] {gameObject.name}: 🗡️ PRIORIDAD: Jugador en rango de ataque ({distanceToTarget:F2} <= {AttackRange}), cambiando a ATTACK desde [{currentStateName}]");
                        stateMachine.ChangeState(attackState);
                        // Ejecutar UpdateState inmediatamente para que ataque en este turno
                        if (stateMachine.currentState == attackState)
                        {
                            stateMachine.currentState.UpdateState();
                        }
                    }
                    else
                    {
                        // Ya está en ATTACK, ejecutar UpdateState para atacar
                        stateMachine.currentState.UpdateState();
                    }
                    return; // Salir después de atacar
                }
                // PRIORIDAD ALTA: Si está en cono de visión, cambiar a CHASING si no está ya en ATTACK o CHASING
                else if (IsInVisionRange(Target))
                {
                    if (stateMachine.currentState != chasingState && stateMachine.currentState != attackState)
                    {
                        Debug.Log($"[ENEMY_TURN] {gameObject.name}: 🏃 PRIORIDAD: Jugador en cono de visión (distancia: {distanceToTarget:F2}), cambiando a CHASING desde [{currentStateName}]");
                        stateMachine.ChangeState(chasingState);
                    }
                    // NO hacer return aquí - dejar que el estado se ejecute inmediatamente
                }
            }
            
            // Si estamos en waitingTurnState, decidir el estado inicial
            if (stateMachine.currentState == waitingTurnState)
            {
                // SOLO en el primer turno: usar el estado configurado si no hay target en rango
                if (isFirstTurn)
                {
                    // PRIORIDAD: Si hay target en rango, usar la lógica de combate (ya manejada arriba)
                    // Si no hay target o está fuera de rango, usar el estado configurado SOLO en el primer turno
                    if (Target == null || !IsInVisionRange(Target))
                    {
                        // No hay target válido, usar el estado configurado para el primer turno
                        EnemyState selectedState = GetTurnStartState();
                        stateMachine.ChangeState(selectedState);
                        
                        string stateName = selectedState.GetType().Name;
                        Debug.Log($"[ENEMY_TURN] {gameObject.name}: 🎯 PRIMER TURNO - Iniciando en estado configurado: {stateName} (TurnStartState config: {turnStartState})");
                    }
                    // Si hay target en rango, la lógica de arriba ya lo maneja (ATTACK o CHASING)
                    
                    // Marcar que ya no es el primer turno
                    isFirstTurn = false;
                }
                else
                {
                    // En turnos posteriores, usar la lógica automática normal
                    if (Target != null)
                    {
                        float distanceToTarget = Vector3.Distance(transform.position, Target.position);
                        
                        if (distanceToTarget <= AttackRange)
                        {
                            Debug.Log($"[ENEMY_TURN] {gameObject.name}: 🗡️ Cambiando a ATTACK (distancia: {distanceToTarget:F2} <= {AttackRange})");
                            stateMachine.ChangeState(attackState);
                        }
                        else if (IsInVisionRange(Target))
                        {
                            Debug.Log($"[ENEMY_TURN] {gameObject.name}: 🏃 Cambiando a CHASING (distancia: {distanceToTarget:F2}, en cono de visión)");
                            stateMachine.ChangeState(chasingState);
                        }
                        else
                        {
                            // Target fuera de rango, usar lógica automática
                            if (PatrolPoints != null && PatrolPoints.Length > 0)
                            {
                                Debug.Log($"[ENEMY_TURN] {gameObject.name}: 🚶 Cambiando a PATROLLING (target fuera de rango, tiene puntos de patrulla)");
                                stateMachine.ChangeState(patrollingState);
                            }
                            else
                            {
                                Debug.Log($"[ENEMY_TURN] {gameObject.name}: 😴 Cambiando a IDLE (target fuera de rango, sin puntos de patrulla)");
                                stateMachine.ChangeState(idleState);
                            }
                        }
                    }
                    else if (PatrolPoints != null && PatrolPoints.Length > 0)
                    {
                        Debug.Log($"[ENEMY_TURN] {gameObject.name}: 🚶 Cambiando a PATROLLING (sin target, tiene puntos de patrulla)");
                        stateMachine.ChangeState(patrollingState);
                    }
                    else
                    {
                        Debug.Log($"[ENEMY_TURN] {gameObject.name}: 😴 Cambiando a IDLE (sin target ni puntos de patrulla)");
                        stateMachine.ChangeState(idleState);
                    }
                }
            }
        }
        else if (team == Team.Enemy && characterActor != null && actor != characterActor)
        {
            // Es el turno de otro enemigo, asegurarse de estar en estado de espera
            Debug.Log($"[ENEMY_TURN] {gameObject.name}: ⏸️ Turno de otro enemigo ({actor?.CharacterName}). Cambiando a WAITING.");
            if (stateMachine.currentState != waitingTurnState)
            {
                stateMachine.ChangeState(waitingTurnState);
            }
            isMyTurn = false;
            hasCompletedTurnAction = false;
        }
        else if (team == Team.Player)
        {
            // Es turno del jugador, ir a estado de espera
            Debug.Log($"[ENEMY_TURN] {gameObject.name}: 👤 Turno del jugador. Cambiando a WAITING.");
            if (stateMachine.currentState != waitingTurnState)
            {
                stateMachine.ChangeState(waitingTurnState);
            }
            isMyTurn = false;
            hasCompletedTurnAction = false;
        }
    }

    private void HandleTurnEnded(Team team, CharacterActor actor)
    {
        string currentStateName = stateMachine.currentState != null ? stateMachine.currentState.GetType().Name : "Null";
        
        // Si terminó nuestro turno, marcar como completado
        if (team == Team.Enemy && characterActor != null && actor == characterActor)
        {
            hasCompletedTurnAction = true;
            isMyTurn = false;
            
            float turnDuration = Time.time - turnStartTime;
            Debug.Log($"[ENEMY_TURN] {gameObject.name}: ✅ TURNO FINALIZADO - Estado: [{currentStateName}], Duración: {turnDuration:F2}s");
            
            // Volver al estado de espera
            if (stateMachine.currentState != waitingTurnState)
            {
                Debug.Log($"[ENEMY_TURN] {gameObject.name}: 🔄 Volviendo a WAITING desde [{currentStateName}]");
                stateMachine.ChangeState(waitingTurnState);
            }
        }
    }

    /// <summary>
    /// Método para que los estados llamen cuando completan su acción de turno
    /// </summary>
    public void CompleteTurnAction()
    {
        if (isMyTurn && !hasCompletedTurnAction)
        {
            string currentStateName = stateMachine.currentState != null ? stateMachine.currentState.GetType().Name : "Null";
            hasCompletedTurnAction = true;
            isMyTurn = false;
            
            float turnDuration = Time.time - turnStartTime;
            Debug.Log($"[ENEMY_ACTION] {gameObject.name}: ✅ Acción completada - Estado: [{currentStateName}], Duración: {turnDuration:F2}s");
            
            // Volver al estado de espera antes de terminar el turno
            if (stateMachine.currentState != waitingTurnState)
            {
                Debug.Log($"[ENEMY_ACTION] {gameObject.name}: 🔄 Cambiando a WAITING desde [{currentStateName}]");
                stateMachine.ChangeState(waitingTurnState);
            }
            
            // Terminar el turno a través del TurnSystem
            if (turnSystem != null && characterActor != null)
            {
                Debug.Log($"[ENEMY_ACTION] {gameObject.name}:  Llamando a TurnSystem.EndTurn()");
                turnSystem.EndTurn();
            }
            else
            {
                Debug.LogWarning($"[ENEMY_ACTION] {gameObject.name}: ⚠️ No se pudo terminar el turno - TurnSystem o CharacterActor es null.");
            }
        }
        else if (!isMyTurn)
        {
            Debug.LogWarning($"[ENEMY_ACTION] {gameObject.name}: ⚠️ CompleteTurnAction() llamado pero no es mi turno (isMyTurn={isMyTurn})");
        }
        else if (hasCompletedTurnAction)
        {
            Debug.LogWarning($"[ENEMY_ACTION] {gameObject.name}: ⚠️ CompleteTurnAction() llamado pero la acción ya fue completada");
        }
    }

    #endregion

}
