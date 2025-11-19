using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class GravityOnlyController : MonoBehaviour
{
    [Header("Gravity Settings")]
    public float gravity = -40f;            // Gravedad pesada
    public float fallMultiplier = 2.8f;     // Caída rápida y contundente

    // --- Jump Settings (opcional, comentado) ---
    // public float jumpHeight = 1.5f;

    private CharacterController controller;
    private Vector3 velocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // --- GRAVEDAD ---
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Lo mantiene pegado al piso sin flotación
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
