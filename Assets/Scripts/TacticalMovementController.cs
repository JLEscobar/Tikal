using UnityEngine;

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

    [Header("Character Stats")]
    [SerializeField] private CharacterStats statsObject;
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int health = 100;
    [SerializeField] private int attackPower = 20;
    [SerializeField] private float moveSpeed = 4.0f;
    [SerializeField] private float movementRange = 5.0f;
    [SerializeField] private string characterName = "Default Name";

    public bool IsMovementPhaseActive => isMovementPhaseActive;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();

        if (statsObject != null)
            SetCharacterStats(statsObject);
    }

    void Update()
    {
        if (listenForSpace && Input.GetKeyDown(KeyCode.Space))
        {
            ToggleMovementPhase();
        }

        if (isMovementPhaseActive)
        {
            HandleMovement();
        }
    }

    public void SetCharacterStats(CharacterStats characterStats)
    {
        maxHealth = characterStats.maxHealth;
        health = characterStats.maxHealth;
        attackPower = characterStats.attackPower;
        moveSpeed = characterStats.moveSpeed;
        movementRange = characterStats.movementRange;
        characterName = characterStats.characterName;
    }

    public void ToggleMovementPhase()
    {
        if (isMovementPhaseActive) EndMovementPhase();
        else StartMovementPhase();
    }

    public void SetMovementPhase(bool active)
    {
        if (active && !isMovementPhaseActive) StartMovementPhase();
        else if (!active && isMovementPhaseActive) EndMovementPhase();
    }

    public void StartMovementPhase()
    {
        if (isMovementPhaseActive) return;

        GameManager.Instance.canvasWorldObject.gameObject.SetActive(false);
        GameManager.Instance.endTurnButton.gameObject.SetActive(true);

        isMovementPhaseActive = true;
        startPositionOfTurn = transform.position;

        if (aroDeLuzPrefab != null)
        {
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

        GameManager.Instance.canvasWorldObject.gameObject.SetActive(true);
        GameManager.Instance.endTurnButton.gameObject.SetActive(false);

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

        // Direcciones basadas en la cámara
        Vector3 camForward = GameManager.Instance.GetMainCamera().transform.forward;
        Vector3 camRight = GameManager.Instance.GetMainCamera().transform.right;

        // Eliminamos componente vertical para que no mire hacia arriba/abajo
        camForward.y = 0f;
        camRight.y = 0f;

        camForward.Normalize();
        camRight.Normalize();

        // Dirección de movimiento relativa a la cámara
        Vector3 moveDirectionCamera = (camForward * vertical + camRight * horizontal).normalized;

        if (moveDirectionCamera.magnitude >= 0.01f)
        {
            Vector3 nextPosition = transform.position + moveDirectionCamera * moveSpeed * Time.deltaTime;

            if (Vector3.Distance(startPositionOfTurn, nextPosition) <= movementRange - _controller.radius)
            {
                _controller.Move(moveDirectionCamera * moveSpeed * Time.deltaTime);
            }
        }
    }

    public int GetMaxHealth() { return maxHealth; }
    public int GetCurrentHealth() { return health; }
    public int GetAttackPower() { return attackPower; }
    public float GetMoveSpeed() { return moveSpeed; }
    public float GetMovementRange() { return movementRange; }
    public string GetCharacterName() { return characterName; }

}
