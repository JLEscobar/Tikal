using UnityEngine;

[DefaultExecutionOrder(-100)] // Se ejecuta antes que TacticalMovementController para establecer bloqueos a tiempo
[RequireComponent(typeof(Animator))]
public class CharacterAnimationHandler : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Si está vacío, se buscará automáticamente")]
    [SerializeField] private Animator animator;
    
    [Header("Audio")]
    [Tooltip("AudioSource para reproducir sonidos de ataques. Si está vacío, se buscará automáticamente")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Clip de audio que se reproduce cuando se ejecuta un ataque básico (Attack)")]
    [SerializeField] private AudioClip attackSound;
    [Tooltip("Clip de audio que se reproduce cuando se ejecuta un ataque especial (SAttack)")]
    [SerializeField] private AudioClip sAttackSound;

    [Header("Settings")]
    [Tooltip("Umbral mínimo de velocidad para considerar que el personaje está caminando")]
    [SerializeField] private float walkSpeedThreshold = 0.1f;
    
    [Tooltip("Umbral mínimo de input para considerar que el jugador está presionando teclas de movimiento")]
    [SerializeField] private float inputThreshold = 0.3f; // Más alto para evitar valores residuales de GetAxis

    [Tooltip("Si está habilitado, actualiza automáticamente las animaciones de Walk y Death en Update")]
    [SerializeField] private bool autoUpdateAnimations = true;
    
    [Tooltip("Si está habilitado, bloquea el movimiento durante las animaciones de ataque")]
    [SerializeField] private bool blockMovementDuringAttacks = true;

    // Referencias a componentes
    private CharacterActor characterActor;
    private Health health;
    private CharacterController characterController;
    private CharacterMovement characterMovement;
    private TacticalMovementController tacticalMovement2;

    

    // Variables para detectar uso de habilidades
    private int lastActionPoints = -1; // -1 indica que aún no se ha inicializado
    private int[] lastAbilityCooldowns; // Rastrea los cooldowns de todas las habilidades
    
    // Variables para controlar el movimiento durante animaciones
    private bool isInAttackAnimation = false;
    private bool wasCharacterControllerEnabled = true; // Para restaurar el estado del CharacterController
    private float attackAnimationStartTime = 0f; // Tiempo en que comenzó la animación de ataque
    [SerializeField] private const float MAX_ATTACK_ANIMATION_DURATION = 3.5f; // Duración máxima de seguridad (5 segundos)

    // Nombres de los parámetros del Animator (para evitar errores de tipeo)
    private const string PARAM_WALK = "Walk";
    private const string PARAM_ATTACK = "Attack";
    private const string PARAM_SATTACK = "SAttack";
    private const string PARAM_NEXUS = "Nexus";
    private const string PARAM_DEATH = "Death";

    private void Awake()
    {
        // Obtener referencias automáticamente
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        characterActor = GetComponent<CharacterActor>();
        health = GetComponent<Health>();
        characterController = GetComponent<CharacterController>();
        characterMovement = GetComponent<CharacterMovement>();
        tacticalMovement2 = GetComponent<TacticalMovementController>();
        
        // Buscar AudioSource si no está asignado
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            // Si no existe, crear uno
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }
        
        // Validar que el Animator existe
        if (animator == null)
        {
            Debug.LogError($"[CharacterAnimationHandler] {gameObject.name}: No se encontró componente Animator!");
        }
        
        // Validar que el CharacterController existe
        if (characterController == null)
        {
            Debug.LogError($"[CharacterAnimationHandler] {gameObject.name}: No se encontró componente CharacterController!");
        }
    }

    private void Start()
    {
        // Validar parámetros del Animator al inicio (solo en editor)
#if UNITY_EDITOR
        ValidateAnimatorParameters();
#endif
        
        // Inicializar el parámetro Death en false al inicio para evitar que quede en true por defecto
        if (animator != null && animator.isActiveAndEnabled)
        {
            SetDeath(false);
        }
        
        // Inicializar el rastreo de APs y cooldowns
        if (characterActor != null)
        {
            lastActionPoints = characterActor.ActionPoints;
            InitializeCooldownTracking();
        }
        
        // Suscribirse al evento de interacción de TriggerInteract
        TriggerInteract.OnInteractKeyPressed += OnInteractKeyPressed;
    }
    
    private void OnDestroy()
    {
        // Desuscribirse del evento al destruir el objeto
        TriggerInteract.OnInteractKeyPressed -= OnInteractKeyPressed;
    }
    
    /// <summary>
    /// Método llamado cuando se presiona la tecla de interacción (desde TriggerInteract)
    /// </summary>
    private void OnInteractKeyPressed()
    {
        // Solo disparar la animación Nexus si el personaje puede interactuar
        if (TriggerInteract.CanInteract)
        {
            TriggerNexus();
        }
    }
    
    /// <summary>
    /// Inicializa el sistema de rastreo de cooldowns de habilidades
    /// </summary>
    private void InitializeCooldownTracking()
    {
        if (characterActor == null || characterActor.Stats == null || characterActor.Stats.abilities == null)
        {
            lastAbilityCooldowns = new int[0];
            return;
        }
        
        int abilityCount = characterActor.Stats.abilities.Length;
        lastAbilityCooldowns = new int[abilityCount];
        
        // Guardar los cooldowns iniciales de todas las habilidades
        for (int i = 0; i < abilityCount; i++)
        {
            if (characterActor.Stats.abilities[i] != null)
            {
                lastAbilityCooldowns[i] = characterActor.Stats.abilities[i].currentCooldown;
            }
        }
    }

    private void Update()
    {
        if (!autoUpdateAnimations) return;

        // Verificar si estamos en una animación de ataque y bloquear movimiento si es necesario
        if (blockMovementDuringAttacks)
        {
            CheckAndBlockMovementDuringAttack();
        }

        // Actualizar animación de caminata
        UpdateWalkAnimation();

        // Actualizar animación de muerte
        UpdateDeathAnimation();
        
        // Detectar uso de habilidades monitoreando cambios en APs (solo Attack y SAttack)
        DetectAbilityUsage();
    }

    #region Métodos Públicos - Control de Animaciones

    /// <summary>
    /// Actualiza la velocidad de caminata (0 = idle, > 0 = caminar)
    /// Si el valor es 0 o muy pequeño, se establece como -0.1 para que la condición "Walk Less 0" del Animator funcione
    /// </summary>
    /// <param name="speed">Velocidad normalizada (0-1) o velocidad real</param>
    public void SetWalkSpeed(float speed)
    {
        if (animator != null && animator.isActiveAndEnabled)
        {
            // Normalizar el valor entre 0 y 1 para el parámetro
            float normalizedSpeed = Mathf.Clamp01(speed);
            
            // Si el valor es 0 o muy pequeño (menor o igual a 0.1), establecerlo como -0.1
            // Esto permite que la condición "Walk Less 0" del Animator funcione correctamente
            if (normalizedSpeed <= 0.1f)
            {
                normalizedSpeed = -0.1f;
            }
            
            float previousWalkValue = animator.GetFloat(PARAM_WALK);
            animator.SetFloat(PARAM_WALK, normalizedSpeed);
            
            // Solo loggear si el valor cambió significativamente para evitar spam
            if (Mathf.Abs(previousWalkValue - normalizedSpeed) > 0.05f)
            {
                Debug.Log($"[WALK_DEBUG] {gameObject.name}: SetWalkSpeed - Input:{speed:F2} -> Normalized:{normalizedSpeed:F2} (Previous:{previousWalkValue:F2})");
            }
        }
        else
        {
            Debug.LogWarning($"[WALK_DEBUG] {gameObject.name}: SetWalkSpeed falló - Animator null o deshabilitado (null:{animator == null} enabled:{animator != null && animator.isActiveAndEnabled})");
        }
    }

    /// <summary>
    /// Dispara la animación de ataque básico
    /// </summary>
    public void TriggerAttack()
    {
        if (animator != null && animator.isActiveAndEnabled)
        {
            animator.SetTrigger(PARAM_ATTACK);
            isInAttackAnimation = true;
            attackAnimationStartTime = Time.time;
            BlockMovement();
            ReproducirSonidoAtaque(attackSound);
            Debug.Log($"[CharacterAnimationHandler] {gameObject.name}: Trigger Attack disparado - Movimiento bloqueado");
        }
    }

    /// <summary>
    /// Dispara la animación de ataque especial
    /// </summary>
    public void TriggerSAttack()
    {
        if (animator != null && animator.isActiveAndEnabled)
        {
            animator.SetTrigger(PARAM_SATTACK);
            isInAttackAnimation = true;
            attackAnimationStartTime = Time.time;
            BlockMovement();
            ReproducirSonidoAtaque(sAttackSound);
            Debug.Log($"[CharacterAnimationHandler] {gameObject.name}: Trigger SAttack disparado - Movimiento bloqueado");
        }
    }

    /// <summary>
    /// Dispara la animación de Nexus
    /// </summary>
    public void TriggerNexus()
    {
        if (animator != null && animator.isActiveAndEnabled)
        {
            animator.SetTrigger(PARAM_NEXUS);
            isInAttackAnimation = true;
            attackAnimationStartTime = Time.time;
            BlockMovement();
            Debug.Log($"[CharacterAnimationHandler] {gameObject.name}: Trigger Nexus disparado - Movimiento bloqueado");
        }
    }

    /// <summary>
    /// Actualiza el estado de muerte (true = muerto, false = vivo)
    /// </summary>
    /// <param name="isDead">Si el personaje está muerto</param>
    public void SetDeath(bool isDead)
    {
        if (animator != null && animator.isActiveAndEnabled)
        {
            animator.SetBool(PARAM_DEATH, isDead);
        }
    }

    #endregion

    #region Métodos de Actualización Automática

    /// <summary>
    /// Actualiza la animación de caminata basada en el movimiento del personaje
    /// Detecta movimiento leyendo el input directamente o la velocidad del CharacterController
    /// </summary>
    private void UpdateWalkAnimation()
    {
        // Si el movimiento está bloqueado (durante animaciones de ataque), no caminar
        if (isInAttackAnimation)
        {
            Debug.Log($"[WALK_DEBUG] {gameObject.name}: UpdateWalkAnimation bloqueado - isInAttackAnimation = true");
            SetWalkSpeed(0f);
            return;
        }
        
        float currentSpeed = 0f;
        bool isMoving = false;
        
        // Método 1: Leer input directamente si TacticalMovementController está activo
        if (tacticalMovement2 != null)
        {
            // Verificar si la fase de movimiento está activa
            if (tacticalMovement2.IsMovementPhaseActive && !tacticalMovement2.IsMovementBlocked)
            {
                float horizontal = Input.GetAxis("Horizontal");
                float vertical = Input.GetAxis("Vertical");
                
                // Calcular la magnitud del input
                float inputMagnitude = Mathf.Sqrt(horizontal * horizontal + vertical * vertical);
                
                // Solo considerar movimiento si el input es significativo (mayor al threshold de input)
                // Usamos inputThreshold en lugar de walkSpeedThreshold para el input
                if (inputMagnitude > inputThreshold)
                {
                    isMoving = true;
                    // Obtener la velocidad máxima del personaje desde sus stats
                    float maxSpeed = 5f;
                    if (characterActor != null && characterActor.Stats != null)
                    {
                        maxSpeed = characterActor.Stats.moveSpeed;
                    }
                    // Normalizar basándose en el input (0-1)
                    // El input ya está normalizado (GetAxis retorna -1 a 1), así que la magnitud ya está normalizada
                    currentSpeed = Mathf.Clamp01(inputMagnitude);
                    Debug.Log($"[WALK_DEBUG] {gameObject.name}: Input detectado - H:{horizontal:F2} V:{vertical:F2} Mag:{inputMagnitude:F2} -> Speed:{currentSpeed:F2}");
                }
                else
                {
                    // Input muy pequeño o cero, asegurar que no hay movimiento
                    isMoving = false;
                    currentSpeed = 0f;
                }
            }
            else
            {
                // Fase de movimiento no activa o bloqueada, no caminar
                isMoving = false;
                currentSpeed = 0f;
                Debug.Log($"[WALK_DEBUG] {gameObject.name}: UpdateWalkAnimation bloqueado - IsMovementPhaseActive:{tacticalMovement2.IsMovementPhaseActive} IsMovementBlocked:{tacticalMovement2.IsMovementBlocked}");
            }
        }
        // Método 2: Usar CharacterController.velocity como fallback
        else if (characterController != null && characterController.enabled)
        {
            Vector3 velocity = characterController.velocity;
            velocity.y = 0f; // Ignorar movimiento vertical
            
            currentSpeed = velocity.magnitude;
            isMoving = currentSpeed > walkSpeedThreshold;
            
            if (isMoving)
            {
                // Obtener la velocidad máxima del personaje desde sus stats
                float maxSpeed = 5f;
                if (characterActor != null && characterActor.Stats != null)
                {
                    maxSpeed = characterActor.Stats.moveSpeed;
                }
                // Normalizar la velocidad
                currentSpeed = Mathf.Clamp01(currentSpeed / maxSpeed);
                Debug.Log($"[WALK_DEBUG] {gameObject.name}: Velocity detectado - Speed:{currentSpeed:F2} (vel:{velocity.magnitude:F2})");
            }
            else
            {
                // Velocidad muy baja, asegurar que no hay movimiento
                currentSpeed = 0f;
            }
        }
        else
        {
            // No hay CharacterController o está deshabilitado, no caminar
            isMoving = false;
            currentSpeed = 0f;
            Debug.Log($"[WALK_DEBUG] {gameObject.name}: No TacticalMovement2 ni CharacterController - isMoving=false");
        }
        
        // Aplicar la velocidad de caminata (siempre asegurar que sea 0 si no hay movimiento)
        Debug.Log($"[WALK_DEBUG] {gameObject.name}: Final - isMoving:{isMoving} currentSpeed:{currentSpeed:F2} -> SetWalkSpeed({(isMoving ? currentSpeed : 0f):F2})");
        SetWalkSpeed(isMoving ? currentSpeed : 0f);
    }

    /// <summary>
    /// Actualiza la animación de muerte basada en el estado de salud
    /// </summary>
    private void UpdateDeathAnimation()
    {
        if (health != null)
        {
            SetDeath(health.IsDead);
        }
    }

    /// <summary>
    /// Detecta cuando se usa una habilidad monitoreando cambios en los cooldowns
    /// Este método es más preciso que monitorear APs, ya que detecta directamente qué habilidad se usó
    /// NOTA: Solo detecta Attack y SAttack, NO Nexus (que es una interacción)
    /// </summary>
    private void DetectAbilityUsage()
    {
        if (characterActor == null) return;
        
        // Inicializar cooldowns si aún no se ha hecho
        if (lastAbilityCooldowns == null)
        {
            InitializeCooldownTracking();
            return;
        }
        
        // Verificar si los APs disminuyeron (confirmación de que se usó una habilidad)
        int currentAP = characterActor.ActionPoints;
        bool apDecreased = false;
        
        if (lastActionPoints == -1)
        {
            lastActionPoints = currentAP;
        }
        else if (currentAP < lastActionPoints)
        {
            apDecreased = true;
        }
        
        // Monitorear cambios en los cooldowns de las habilidades
        if (characterActor.Stats != null && characterActor.Stats.abilities != null)
        {
            for (int i = 0; i < characterActor.Stats.abilities.Length; i++)
            {
                var ability = characterActor.Stats.abilities[i];
                if (ability == null) continue;
                
                int currentCooldown = ability.currentCooldown;
                int lastCooldown = (i < lastAbilityCooldowns.Length) ? lastAbilityCooldowns[i] : 0;
                
                // Si el cooldown cambió de 0 a un valor mayor, la habilidad se acaba de usar
                if (lastCooldown == 0 && currentCooldown > 0)
                {
                    // Confirmar que los APs también disminuyeron (doble verificación)
                    if (apDecreased || currentAP < lastActionPoints)
                    {
                        // Esta es la habilidad que se usó
                        OnAbilityUsed(ability);
                        break; // Solo una habilidad se puede usar a la vez
                    }
                }
            }
            
            // Si no detectamos ninguna habilidad por cooldown pero los APs disminuyeron,
            // usar el método de fallback: buscar por costo de AP
            if (apDecreased)
            {
                int apConsumed = lastActionPoints - currentAP;
                bool abilityDetected = false;
                
                // Verificar si alguna habilidad ya fue detectada (por cooldown)
                for (int i = 0; i < characterActor.Stats.abilities.Length; i++)
                {
                    var ability = characterActor.Stats.abilities[i];
                    if (ability != null && ability.currentCooldown > 0)
                    {
                        abilityDetected = true;
                        break;
                    }
                }
                
                // Si no se detectó por cooldown, buscar por costo de AP
                if (!abilityDetected)
                {
                    for (int i = 0; i < characterActor.Stats.abilities.Length; i++)
                    {
                        var ability = characterActor.Stats.abilities[i];
                        if (ability != null && ability.CostAP == apConsumed)
                        {
                            // Esta es probablemente la habilidad que se usó
                            OnAbilityUsed(ability);
                            break;
                        }
                    }
                }
            }
            
            // Actualizar el rastreo de cooldowns
            for (int i = 0; i < characterActor.Stats.abilities.Length && i < lastAbilityCooldowns.Length; i++)
            {
                if (characterActor.Stats.abilities[i] != null)
                {
                    lastAbilityCooldowns[i] = characterActor.Stats.abilities[i].currentCooldown;
                }
            }
        }
        
        // Actualizar el último valor de APs
        lastActionPoints = currentAP;
    }
    
    /// <summary>
    /// Verifica si estamos en una animación de ataque y bloquea/desbloquea el movimiento según corresponda
    /// </summary>
    private void CheckAndBlockMovementDuringAttack()
    {
        if (animator == null || !animator.isActiveAndEnabled) return;
        
        // Obtener el estado actual del Animator
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        
        // Verificar si estamos en alguna animación de ataque usando múltiples métodos
        // Método 1: Verificar por nombre completo (incluyendo layer)
        bool currentlyInAttack = stateInfo.IsName("Base Layer.Attack") || 
                                  stateInfo.IsName("Base Layer.SAttack") || 
                                  stateInfo.IsName("Base Layer.Nexus") ||
                                  stateInfo.IsName("Attack") || 
                                  stateInfo.IsName("SAttack") || 
                                  stateInfo.IsName("Nexus");
        
        // Método 2: Verificar por hash si los nombres no funcionan
        if (!currentlyInAttack)
        {
            int currentStateHash = stateInfo.fullPathHash;
            int attackHash = Animator.StringToHash("Base Layer.Attack");
            int sAttackHash = Animator.StringToHash("Base Layer.SAttack");
            int nexusHash = Animator.StringToHash("Base Layer.Nexus");
            
            currentlyInAttack = (currentStateHash == attackHash || 
                                currentStateHash == sAttackHash || 
                                currentStateHash == nexusHash);
        }
        
        // Si estamos en una animación de ataque
        if (currentlyInAttack)
        {
            // Si aún no habíamos bloqueado el movimiento, bloquearlo ahora
            if (!isInAttackAnimation)
            {
                isInAttackAnimation = true;
                attackAnimationStartTime = Time.time;
                BlockMovement();
                string stateName = "Unknown";
                if (stateInfo.IsName("Attack") || stateInfo.IsName("Base Layer.Attack")) stateName = "Attack";
                else if (stateInfo.IsName("SAttack") || stateInfo.IsName("Base Layer.SAttack")) stateName = "SAttack";
                else if (stateInfo.IsName("Nexus") || stateInfo.IsName("Base Layer.Nexus")) stateName = "Nexus";
                Debug.Log($"[ATTACK_DEBUG] {gameObject.name}: Animación de ataque detectada - Movimiento bloqueado. Estado: {stateName} NormalizedTime: {stateInfo.normalizedTime:F2}");
            }
            
            // Verificar si la animación ha terminado (normalizedTime >= 1.0)
            if (stateInfo.normalizedTime >= 1.0f)
            {
                // La animación terminó, desbloquear movimiento
                isInAttackAnimation = false;
                attackAnimationStartTime = 0f;
                UnblockMovement();
                Debug.Log($"[ATTACK_DEBUG] {gameObject.name}: Animación de ataque terminada (normalizedTime >= 1.0) - Movimiento desbloqueado");
            }
            else
            {
                Debug.Log($"[ATTACK_DEBUG] {gameObject.name}: En animación de ataque - NormalizedTime: {stateInfo.normalizedTime:F2} (isInAttackAnimation: {isInAttackAnimation})");
            }
        }
        else
        {
            // No estamos en animación de ataque actualmente
            // Si isInAttackAnimation es true, podría ser que:
            // 1. La animación está en transición (esperar a que termine)
            // 2. La animación ya terminó pero aún no se detectó
            // 3. Hubo un error y la animación nunca se activó (timeout de seguridad)
            
            if (isInAttackAnimation)
            {
                float timeSinceStart = Time.time - attackAnimationStartTime;
                string currentStateName = stateInfo.IsName("Base Layer.Idle") ? "Idle" : 
                                         stateInfo.IsName("Base Layer.Walk") ? "Walk" : 
                                         stateInfo.IsName("Base Layer.Attack") ? "Attack" :
                                         stateInfo.IsName("Base Layer.SAttack") ? "SAttack" :
                                         stateInfo.IsName("Base Layer.Nexus") ? "Nexus" : "Unknown";
                Debug.Log($"[ATTACK_DEBUG] {gameObject.name}: NO en ataque pero isInAttackAnimation=true - Estado actual: {currentStateName} NormalizedTime: {stateInfo.normalizedTime:F2} Tiempo desde inicio: {timeSinceStart:F2}s");
                
                // Timeout de seguridad: si han pasado más de MAX_ATTACK_ANIMATION_DURATION segundos
                // desde que se bloqueó el movimiento, desbloquear automáticamente
                if (timeSinceStart > MAX_ATTACK_ANIMATION_DURATION)
                {
                    Debug.LogWarning($"[ATTACK_DEBUG] {gameObject.name}: Timeout de seguridad - Desbloqueando movimiento después de {MAX_ATTACK_ANIMATION_DURATION}s");
                    isInAttackAnimation = false;
                    attackAnimationStartTime = 0f;
                    UnblockMovement();
                }
                // Si no ha pasado el timeout, mantener bloqueado (la animación podría estar en transición)
            }
        }
    }
    
    /// <summary>
    /// Bloquea el movimiento del personaje durante las animaciones de ataque
    /// </summary>
    private void BlockMovement()
    {
        // Bloquear CharacterMovement si existe (usado por enemigos)
        if (characterMovement != null)
        {
            characterMovement.Stop();
        }
        
        // Para TacticalMovementController (jugadores), usar la flag IsMovementBlocked
        // Esta es la forma principal de bloquear el movimiento
        if (tacticalMovement2 != null)
        {
            bool wasBlocked = tacticalMovement2.IsMovementBlocked;
            tacticalMovement2.IsMovementBlocked = true;
            Debug.Log($"[BLOCK_DEBUG] {gameObject.name}: BlockMovement - IsMovementBlocked: {wasBlocked} -> true");
        }
        
        // SIEMPRE deshabilitar CharacterController durante animaciones de ataque
        // Esto previene cualquier movimiento, incluso si otros scripts intentan moverlo
        // Es una medida de seguridad adicional
        if (characterController != null)
        {
            // Solo guardar el estado si aún no estaba guardado y está habilitado
            if (characterController.enabled)
            {
                wasCharacterControllerEnabled = true;
            }
            // Deshabilitar el CharacterController para prevenir cualquier movimiento
            characterController.enabled = false;
        }
        
        // También forzar que la velocidad del CharacterController sea cero
        // Esto previene cualquier movimiento residual
        if (characterController != null && characterController.velocity.magnitude > 0.01f)
        {
            // El CharacterController ya está deshabilitado, pero por si acaso
            // intentamos detener cualquier movimiento residual
        }
    }
    
    /// <summary>
    /// Desbloquea el movimiento del personaje después de las animaciones de ataque
    /// </summary>
    private void UnblockMovement()
    {
        // Desbloquear TacticalMovementController
        if (tacticalMovement2 != null)
        {
            bool wasBlocked = tacticalMovement2.IsMovementBlocked;
            tacticalMovement2.IsMovementBlocked = false;
            Debug.Log($"[BLOCK_DEBUG] {gameObject.name}: UnblockMovement - IsMovementBlocked: {wasBlocked} -> false | IsMovementPhaseActive: {tacticalMovement2.IsMovementPhaseActive}");
        }
        
        // Rehabilitar el CharacterController si fue deshabilitado
        if (characterController != null && !characterController.enabled)
        {
            characterController.enabled = wasCharacterControllerEnabled;
            Debug.Log($"[CharacterAnimationHandler] {gameObject.name}: CharacterController rehabilitado (estado restaurado: {wasCharacterControllerEnabled})");
        }
    }
    
    #endregion

    #region Métodos de Integración con Habilidades

    /// <summary>
    /// Método para ser llamado cuando se usa una habilidad
    /// Decide automáticamente qué animación usar basado en el tipo de habilidad
    /// NOTA: Solo maneja Attack y SAttack, NO Nexus (que es una interacción)
    /// </summary>
    /// <param name="abilityIndex">Índice de la habilidad (0 = básica, 1 = especial)</param>
    public void OnAbilityUsed(int abilityIndex)
    {
        if (characterActor == null) return;
        
        var ability = characterActor.GetAbilityByIndex(abilityIndex);
        if (ability == null) return;
        
        // Decidir qué animación usar basado en el tipo de habilidad
        // Nexus no se maneja aquí porque es una interacción, no una habilidad
        if (ability is MeleeAttackAbility)
        {
            TriggerAttack();
        }
        else // Habilidades especiales (AreaAttackAbility, LineAttackAbility, etc.)
        {
            TriggerSAttack();
        }
    }
    
    /// <summary>
    /// Método para ser llamado cuando se usa una habilidad específica
    /// NOTA: Solo maneja Attack y SAttack, NO Nexus (que es una interacción)
    /// </summary>
    /// <param name="ability">La habilidad que se está usando</param>
    public void OnAbilityUsed(AbilityBase ability)
    {
        if (ability == null) return;
        
        // Decidir qué animación usar basado en el tipo de habilidad
        // Nexus no se maneja aquí porque es una interacción, no una habilidad
        if (ability is MeleeAttackAbility)
        {
            TriggerAttack();
        }
        else // Habilidades especiales
        {
            TriggerSAttack();
        }
    }

    #endregion

    #region Utilidades

    /// <summary>
    /// Reproduce un clip de audio de ataque si está disponible
    /// </summary>
    /// <param name="clip">Clip de audio a reproducir</param>
    private void ReproducirSonidoAtaque(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
            Debug.Log($"[CharacterAnimationHandler] {gameObject.name}: Reproduciendo sonido de ataque - {clip.name}");
        }
    }

    /// <summary>
    /// Resetea todos los triggers del Animator
    /// Útil después de ciertas animaciones o al cambiar de estado
    /// </summary>
    public void ResetTriggers()
    {
        if (animator != null && animator.isActiveAndEnabled)
        {
            animator.ResetTrigger(PARAM_ATTACK);
            animator.ResetTrigger(PARAM_SATTACK);
            animator.ResetTrigger(PARAM_NEXUS);
        }
    }

    /// <summary>
    /// Valida que todos los parámetros requeridos existan en el Animator Controller
    /// Solo funciona en el Editor
    /// </summary>
    [ContextMenu("Validate Animator Parameters")]
    private void ValidateAnimatorParameters()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning($"[CharacterAnimationHandler] {gameObject.name}: Animator o Controller no encontrado");
            return;
        }

        var parameters = animator.parameters;
        var paramNames = new System.Collections.Generic.HashSet<string>();
        foreach (var param in parameters)
        {
            paramNames.Add(param.name);
        }

        // Verificar que todos los parámetros requeridos existan
        string[] requiredParams = { PARAM_WALK, PARAM_ATTACK, PARAM_SATTACK, PARAM_NEXUS, PARAM_DEATH };
        bool allParamsExist = true;

        foreach (var paramName in requiredParams)
        {
            if (!paramNames.Contains(paramName))
            {
                Debug.LogWarning($"[CharacterAnimationHandler] {gameObject.name}: ⚠️ Parámetro '{paramName}' no encontrado en el Animator Controller!");
                allParamsExist = false;
            }
        }

        if (allParamsExist)
        {
            Debug.Log($"[CharacterAnimationHandler] {gameObject.name}: ✅ Todos los parámetros de animación están configurados correctamente");
        }
    }

    #endregion
}

