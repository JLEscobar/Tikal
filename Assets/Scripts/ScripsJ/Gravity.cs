using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class GravityOnlyController : MonoBehaviour
{
    [Header("Gravity Settings")]
    public float gravity = -40f;            // Gravedad pesada
    public float fallMultiplier = 2.8f;     // Ca�da r�pida y contundente

    // --- Jump Settings (opcional, comentado) ---
    // public float jumpHeight = 1.5f;

    private CharacterController controller;
    private Vector3 velocity;
    private TacticalMovementController tacticalMovementController; // Referencia para verificar bloqueo de movimiento

    void Start()
    {
        controller = GetComponent<CharacterController>();
        tacticalMovementController = GetComponent<TacticalMovementController>();
    }

    void Update()
    {
        // Verificar si el movimiento está bloqueado (por animaciones de ataque)
        if (tacticalMovementController != null && tacticalMovementController.IsMovementBlocked)
        {
            // No aplicar gravedad si el movimiento está bloqueado
            return;
        }
        
        // Verificar si el CharacterController está deshabilitado
        if (!controller.enabled)
        {
            // No aplicar gravedad si el controller está deshabilitado
            return;
        }

        // --- GRAVEDAD ---
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Lo mantiene pegado al piso sin flotaci�n
        }
        else
        {
            velocity.y += gravity * fallMultiplier * Time.deltaTime;
        }

        // --- SALTO (opcional, comentado) ---
        /*
        if (controller.isGrounded && Input.GetButtonDown("Jump"))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        */

        controller.Move(velocity * Time.deltaTime);
    }
}
