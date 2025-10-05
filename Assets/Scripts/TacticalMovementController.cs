using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class TacticalMovementController : MonoBehaviour
{
    [Header("Datos del Personaje")]
    [SerializeField] private Classes movementData;

    [Header("Efectos Visuales")]
    [SerializeField] private GameObject aroDeLuzPrefab;

    // Componentes y estado interno
    private CharacterController _controller;
    private Vector3 startPositionOfTurn;
    private GameObject aroDeLuzInstance;
    private bool isMovementPhaseActive = false;
    private float moveSpeed = 4.0f;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isMovementPhaseActive)
            {
                EndMovementPhase();
            }
            else
            {
                StartMovementPhase();
            }
        }

        if (isMovementPhaseActive)
        {
            HandleMovement();
        }
    }

    public void StartMovementPhase()
    {
        isMovementPhaseActive = true;
        startPositionOfTurn = transform.position;

        if (aroDeLuzPrefab != null && movementData != null)
        {
            // Lo instanciamos en la posición del personaje
            Vector3 spawnPosition = startPositionOfTurn;

            // Instanciar aro
            aroDeLuzInstance = Instantiate(aroDeLuzPrefab, spawnPosition, Quaternion.identity);

            // --- FORZARLO A PEGARSE AL PISO ---
            aroDeLuzInstance.transform.position = new Vector3(
                aroDeLuzInstance.transform.position.x,
                0.01f, // siempre un poquito arriba del suelo
                aroDeLuzInstance.transform.position.z
            );

            // --- AJUSTE VISUAL DEL ARO ---
            float visualRadius = movementData.movementRange - _controller.radius;
            float diameter = visualRadius * 2;
            aroDeLuzInstance.transform.localScale = new Vector3(diameter, 0.01f, diameter);
        }
    }

    public void EndMovementPhase()
    {
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
        Vector3 moveDirection = new Vector3(horizontal, 0, vertical).normalized;

        if (moveDirection.magnitude >= 0.1f)
        {
            Vector3 nextPosition = transform.position + moveDirection * moveSpeed * Time.deltaTime;

            // --- CÁLCULO DE PRECISIÓN FINAL ---
            if (Vector3.Distance(startPositionOfTurn, nextPosition) <= movementData.movementRange - _controller.radius)
            {
                _controller.Move(moveDirection * moveSpeed * Time.deltaTime);
            }
        }
    }
}
