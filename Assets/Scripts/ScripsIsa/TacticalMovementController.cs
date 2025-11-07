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
    
    // VARIABLES CLAVE PARA EL RASTREO DE DISTANCIA
    private float totalDistanceMovedInTurn = 0f; 
    private Vector3 lastPosition;
    
    // Variables de runtime para las stats (ahora declaradas aquí)
    private float moveSpeed; 
    private float movementRange;
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

        if (statsObject != null)
            SetCharacterStats(statsObject);
    }

    void Start()
    {
        lastPosition = transform.position; 
    }

    void Update()
    {
        if (listenForSpace && Input.GetKeyDown(KeyCode.Space))
        {
            ToggleMovementPhase(); // MÉTODO REQUERIDO
        }

        if (isMovementPhaseActive)
        {
            HandleMovement();
        }
    }

    public void SetCharacterStats(CharacterStats characterStats)
    {
        // ASIGNACIÓN CLAVE
        this.movementRange = characterStats.movementRange; 
        this.moveSpeed = characterStats.moveSpeed; 

        maxHealth = characterStats.maxHealth;
        health = characterStats.maxHealth;
        attackPower = characterStats.attackPower;
        characterName = characterStats.characterName;
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
        
        if (aroDeLuzPrefab != null)
        {
            // ... (Lógica de instanciación del aro de luz) ...
            Vector3 spawnPosition = startPositionOfTurn;

            aroDeLuzInstance = Instantiate(aroDeLuzPrefab, spawnPosition, Quaternion.identity);

            // Keep the ring slightly above the ground
            aroDeLuzInstance.transform.position = new Vector3(
                aroDeLuzInstance.transform.position.x,
                0.01f,
                aroDeLuzInstance.transform.position.z
            );

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

    private void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // ... (Cálculo de la dirección de movimiento basado en la cámara) ...
        Vector3 camForward = GameManager.Instance.GetMainCamera().transform.forward;
        Vector3 camRight = GameManager.Instance.GetMainCamera().transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();
        Vector3 moveDirectionCamera = (camForward * vertical + camRight * horizontal).normalized;


        if (moveDirectionCamera.magnitude >= 0.01f)
        {
            Vector3 move = moveDirectionCamera * moveSpeed * Time.deltaTime; 
            Vector3 nextPosition = transform.position + move;
            
            if (Vector3.Distance(startPositionOfTurn, nextPosition) <= movementRange - _controller.radius)
            {
                // Antes de movernos, registramos la distancia recorrida
                float distanceIncrement = Vector3.Distance(transform.position, lastPosition);
                totalDistanceMovedInTurn += distanceIncrement;
                
                _controller.Move(move);
                
                // Actualizamos la última posición
                lastPosition = transform.position;
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