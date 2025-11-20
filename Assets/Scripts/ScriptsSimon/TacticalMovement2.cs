using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(CharacterController))]
public class TacticalMovementController : MonoBehaviour
{
    [Header("VFX")]
    [SerializeField] private GameObject aroDeLuzPrefab;

    [Header("Input (for single-character testing)")]
    [Tooltip("If true, this script will listen to Space to toggle movement phase by itself.")]
    [SerializeField] private bool listenForSpace = false;

    private CharacterController _controller;
    private Vector3 startPositionOfTurn; 
    private GameObject aroDeLuzInstance;
    private bool isMovementPhaseActive = false;
    private CharacterActor _characterActor;
    private TurnSystem _turnSystem;
    
    // VARIABLES CLAVE PARA EL RASTREO DE DISTANCIA
    private float totalDistanceMovedInTurn = 0f; 
    private Vector3 lastPosition;
    
    // Flag para rastrear si ya se estableció startPositionOfTurn en el turno global actual
    private static bool hasSetStartPositionThisGlobalTurn = false;
    private static Team lastGlobalTurnTeam = Team.Enemy; // Inicializar con Enemy para que el primer turno de Player lo detecte
    private bool hasUsedTurnThisGlobalTurn = false; // Flag para rastrear si este jugador ya usó su turno en el turno global actual
    private bool hasStartPositionSet = false; // Flag de instancia para verificar si este jugador ya tiene startPositionOfTurn establecido
    
    // NOTA: La lógica de restauración de APs está ahora en PlayerTurnController.cs
    
    // Variables de runtime para las stats (ahora declaradas aquí)
    private float moveSpeed = 5f; // Valor por defecto
    private float movementRange = 5f; // Valor por defecto
    // --- FIN DE DECLARACIÓN DE RUNTIME ---


    [Header("Character Stats")]
    [SerializeField] private CharacterStats statsObject; 
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int health = 100;
    [SerializeField] private int attackPower = 20;
    [SerializeField] private string characterName = "Default Name";


    public bool IsMovementPhaseActive => isMovementPhaseActive;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _characterActor = GetComponent<CharacterActor>();
        _turnSystem = FindFirstObjectByType<TurnSystem>();

        if (statsObject != null)
            SetCharacterStats(statsObject);
    }

    void Start()
    {
        lastPosition = transform.position;
        startPositionOfTurn = transform.position; // Inicializar con la posición actual
        
        // Suscribirse a eventos del TurnSystem para detectar cambios de turno global
        if (_turnSystem != null)
        {
            _turnSystem.OnTurnStarted += HandleTurnStarted;
            _turnSystem.OnTurnEnded += HandleTurnEnded;
        }
    }
    
    void OnDestroy()
    {
        // Desuscribirse de eventos
        if (_turnSystem != null)
        {
            _turnSystem.OnTurnStarted -= HandleTurnStarted;
            _turnSystem.OnTurnEnded -= HandleTurnEnded;
        }
    }
    
    private void HandleTurnStarted(Team team, CharacterActor actor)
    {
        // Si cambió el equipo (de Enemy a Player o viceversa), resetear las flags
        if (team != lastGlobalTurnTeam)
        {
            hasSetStartPositionThisGlobalTurn = false;
            lastGlobalTurnTeam = team;
            // Resetear la flag de instancia para todos los jugadores cuando cambia el turno global
            hasUsedTurnThisGlobalTurn = false;
            hasStartPositionSet = false; // Resetear la flag de posición inicial
            Debug.Log($"[MOVEMENT] {gameObject.name}: Nuevo turno global detectado ({team}). Reset de flags de posición inicial.");
        }
        
        // NOTA: La posición inicial se establece globalmente desde PlayerTurnController.cs
        // cuando comienza el turno global de jugadores, no individualmente aquí
    }
    
    
    private void HandleTurnEnded(Team team, CharacterActor actor)
    {
        // Si este personaje terminó su turno en la fase de jugadores
        if (team == Team.Player && actor == _characterActor)
        {
            // No necesitamos marcar hasUsedTurnThisGlobalTurn aquí porque cada jugador
            // establece su propia startPositionOfTurn cuando comienza su turno
            Debug.Log($"[MOVEMENT] {gameObject.name}: Turno completado.");
        }
        
        // Si cambió el equipo (de Player a Enemy), resetear las flags estáticas
        if (team == Team.Player && _turnSystem != null && _turnSystem.CurrentTeam == Team.Enemy)
        {
            hasUsedTurnThisGlobalTurn = false;
            hasSetStartPositionThisGlobalTurn = false;
            Debug.Log($"[MOVEMENT] {gameObject.name}: Fase de jugadores terminada. Reset de flags estáticas.");
        }
    }

    void Update()
    {
        // Solo escuchar Space si está habilitado Y si es el turno de este personaje
        if (listenForSpace && Input.GetKeyDown(KeyCode.K))
        {
            // Verificar que es el turno de este personaje antes de toggle
            if (_turnSystem != null && _characterActor != null)
            {
                if (_turnSystem.CurrentActor == _characterActor && _turnSystem.CurrentTeam == Team.Player)
                {
                    ToggleMovementPhase();
                }
            }
            else if (listenForSpace)
            {
                // Si no hay turnSystem, permitir toggle (para testing)
                ToggleMovementPhase();
            }
        }

        if (isMovementPhaseActive)
        {
            HandleMovement();
        }
    }

    public void SetCharacterStats(CharacterStats characterStats)
    {
        if (characterStats == null)
        {
            Debug.LogError($"[MOVEMENT] {gameObject.name}: CharacterStats is null!");
            return;
        }

        // ASIGNACIÓN CLAVE
        this.movementRange = characterStats.movementRange; 
        this.moveSpeed = characterStats.moveSpeed; 

        maxHealth = characterStats.maxHealth;
        health = characterStats.maxHealth;
        attackPower = characterStats.attackPower;
        characterName = characterStats.characterName;
        
        Debug.Log($"[MOVEMENT] {gameObject.name}: Stats set. Speed: {moveSpeed}, Range: {movementRange}");
    }

    public void SetMovementRange(float newRange)
    {
        // Se llama desde CharacterActor para aplicar la penalización de Ralentizado
        this.movementRange = newRange; 
        // Opcional: Re-escalar el aroDeLuzInstance aquí si es visible y activo
    }
    
    /// <summary>
    /// Establece la posición inicial del turno (llamado desde PlayerTurnController una vez por turno global)
    /// </summary>
    public void SetStartPositionOfTurn(Vector3 position)
    {
        startPositionOfTurn = position;
        hasStartPositionSet = true;
        Debug.Log($"[MOVEMENT] {gameObject.name}: startPositionOfTurn establecido globalmente: {startPositionOfTurn}");
    }

    // MÉTODO REQUERIDO POR EL ERROR CS0103
    public void ToggleMovementPhase()
    {
        if (isMovementPhaseActive) EndMovementPhase();
        else StartMovementPhase();
    }

    // MÉTODO REQUERIDO POR EL ERROR CS1061 EN CharacterActor.cs
    public void SetMovementPhase(bool active)
    {
        if (active && !isMovementPhaseActive) StartMovementPhase();
        else if (!active && isMovementPhaseActive) EndMovementPhase();
    }


    public void StartMovementPhase()
    {
        if (isMovementPhaseActive) return;

        // Verificar que CharacterController esté habilitado
        if (_controller != null && !_controller.enabled)
        {
            Debug.LogWarning($"[MOVEMENT] {gameObject.name}: CharacterController is disabled! Enabling it...");
            _controller.enabled = true;
        }

        // Asumiendo que GameManager.Instance y sus componentes existen:
        if (GameManager.Instance != null && GameManager.Instance.canvasWorldObject != null)
        {
            GameManager.Instance.canvasWorldObject.gameObject.SetActive(false);
            GameManager.Instance.endTurnButton.gameObject.SetActive(true);
        }

        isMovementPhaseActive = true;
        
        // NOTA: startPositionOfTurn ya debería estar establecido globalmente por PlayerTurnController
        // Si no está establecido (por alguna razón), establecerlo como fallback
        if (!hasStartPositionSet)
        {
            startPositionOfTurn = transform.position;
            hasStartPositionSet = true;
            Debug.Log($"[MOVEMENT] {gameObject.name}: ⚠️ startPositionOfTurn establecido como fallback: {startPositionOfTurn}");
        }
        else
        {
            Debug.Log($"[MOVEMENT] {gameObject.name}: ✅ Usando startPositionOfTurn establecido globalmente: {startPositionOfTurn}");
        }
        
        totalDistanceMovedInTurn = 0f; 
        lastPosition = transform.position;
        
        Debug.Log($"[MOVEMENT] {gameObject.name}: Movement phase started. Speed: {moveSpeed}, Range: {movementRange}, Controller enabled: {(_controller != null ? _controller.enabled.ToString() : "null")}");
        
        if (aroDeLuzPrefab != null)
        {
            // Calcular la posición base del personaje (suelo) en lugar del centro
            // La base del CharacterController está en: position + center - (height/2 en Y)
            float baseY = startPositionOfTurn.y + _controller.center.y - (_controller.height / 2f);
            
            // Opcional: Hacer un raycast hacia abajo para encontrar el suelo real
            Vector3 rayStart = new Vector3(startPositionOfTurn.x, startPositionOfTurn.y + _controller.center.y, startPositionOfTurn.z);
            RaycastHit hit;
            float finalY = baseY;
            
            if (Physics.Raycast(rayStart, Vector3.down, out hit, _controller.height + 1f))
            {
                // Si encontramos el suelo, usar esa posición
                finalY = hit.point.y;
            }
            
            Vector3 spawnPosition = new Vector3(
                startPositionOfTurn.x,
                finalY + 0.1f, // Pequeño offset para que la esfera no se hunda en el suelo
                startPositionOfTurn.z
            );

        aroDeLuzInstance = Instantiate(aroDeLuzPrefab, spawnPosition, Quaternion.identity);

        // ya NO forzamos Y = 0.01
        // la posicion se mantiene a la altura correcta del personaje

        float visualRadius = movementRange - _controller.radius;
        float diameter = visualRadius * 2f;
        aroDeLuzInstance.transform.localScale = new Vector3(diameter, diameter, diameter);

        }
    }

    public void EndMovementPhase()
    {
        if (!isMovementPhaseActive) return;

        if (GameManager.Instance != null && GameManager.Instance.canvasWorldObject != null)
        {
            GameManager.Instance.canvasWorldObject.gameObject.SetActive(true);
            GameManager.Instance.endTurnButton.gameObject.SetActive(false);
        }

        isMovementPhaseActive = false;
        if (aroDeLuzInstance != null)
        {
            Destroy(aroDeLuzInstance);
        }
    }

    private float _lastInputLogTime = 0f;
    private const float INPUT_LOG_INTERVAL = 2f; // Log cada 2 segundos

    private void HandleMovement()
    {
        // Verificar que es el turno de este personaje
        if (_turnSystem != null && _characterActor != null)
        {
            if (_turnSystem.CurrentActor != _characterActor || _turnSystem.CurrentTeam != Team.Player)
            {
                // No es el turno de este personaje, no permitir movimiento
                return;
            }
        }

        if (_controller == null)
        {
            Debug.LogError($"[MOVEMENT] {gameObject.name}: CharacterController is null!");
            return;
        }

        if (moveSpeed <= 0)
        {
            Debug.LogWarning($"[MOVEMENT] {gameObject.name}: MoveSpeed is {moveSpeed}. Movement will not work.");
            return;
        }

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Log de input periódicamente para debug
        if (Time.time - _lastInputLogTime > INPUT_LOG_INTERVAL)
        {
            Debug.Log($"[MOVEMENT] {gameObject.name}: Input detected - H: {horizontal:F2}, V: {vertical:F2}");
            _lastInputLogTime = Time.time;
        }

        // Verificar que GameManager y la cámara existan
        if (GameManager.Instance == null)
        {
            Debug.LogError($"[MOVEMENT] {gameObject.name}: GameManager.Instance is null!");
            return;
        }

        Camera mainCam = GameManager.Instance.GetMainCamera();
        if (mainCam == null)
        {
            Debug.LogError($"[MOVEMENT] {gameObject.name}: Main camera is null!");
            return;
        }

        // ... (Cálculo de la dirección de movimiento basado en la cámara) ...
        Vector3 camForward = mainCam.transform.forward;
        Vector3 camRight = mainCam.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();
        Vector3 moveDirectionCamera = (camForward * vertical + camRight * horizontal).normalized;

        if (moveDirectionCamera.magnitude >= 0.01f)
        {
            Vector3 move = moveDirectionCamera * moveSpeed * Time.deltaTime; 
            Vector3 nextPosition = transform.position + move;
            
            float distanceFromStart = Vector3.Distance(startPositionOfTurn, nextPosition);
            float maxDistance = movementRange - _controller.radius;
            
            if (distanceFromStart <= maxDistance)
            {
                // Antes de movernos, registramos la distancia recorrida
                float distanceIncrement = Vector3.Distance(transform.position, lastPosition);
                totalDistanceMovedInTurn += distanceIncrement;
                
                _controller.Move(move);
                
                // Log cuando se mueve
                if (Time.time - _lastInputLogTime > INPUT_LOG_INTERVAL)
                {
                    Debug.Log($"[MOVEMENT] {gameObject.name}: Moving! Direction: {moveDirectionCamera}, Move: {move}, Distance from start: {distanceFromStart:F2}/{maxDistance:F2}");
                }
                
                // Actualizamos la última posición
                lastPosition = transform.position;
            }
            else
            {
                // Log cuando se intenta mover pero está fuera de rango
                if (Time.time - _lastInputLogTime > INPUT_LOG_INTERVAL)
                {
                    Debug.LogWarning($"[MOVEMENT] {gameObject.name}: Movement blocked! Distance from start: {distanceFromStart:F2} exceeds max: {maxDistance:F2}");
                }
            }
        }
    }

    public float GetDistanceMovedThisTurn(Vector3 currentPosition)
    {
        // Devuelve el total acumulado de la distancia BLITZ (Usado por Yaotl)
        return totalDistanceMovedInTurn; 
    }
    
    // ... (El resto de getters se mantienen) ...
    public int GetMaxHealth() { return maxHealth; }
    public int GetCurrentHealth() { return health; }
    public int GetAttackPower() { return attackPower; }
    public float GetMoveSpeed() { return moveSpeed; }
    public float GetMovementRange() { return movementRange; }
    public string GetCharacterName() { return characterName; }
}