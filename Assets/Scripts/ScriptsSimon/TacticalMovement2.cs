using UnityEngine;
using System;
using System.Collections.Generic;

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
    }

    void Update()
    {
        // Solo escuchar Space si está habilitado Y si es el turno de este personaje
        if (listenForSpace && Input.GetKeyDown(KeyCode.Space))
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
        
        startPositionOfTurn = transform.position;
        totalDistanceMovedInTurn = 0f; 
        lastPosition = transform.position;
        
        Debug.Log($"[MOVEMENT] {gameObject.name}: Movement phase started. Speed: {moveSpeed}, Range: {movementRange}, Controller enabled: {(_controller != null ? _controller.enabled.ToString() : "null")}");
        
        if (aroDeLuzPrefab != null)
        {
            Vector3 spawnPosition = new Vector3(
            startPositionOfTurn.x,
            startPositionOfTurn.y + (_controller.height / 2f),
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