using UnityEngine;

// Principio de Responsabilidad Única: Este script SOLO se encarga de leer el input del jugador.
public class PlayerInputHandler : MonoBehaviour
{
    // Propiedades públicas para que otros scripts puedan LEER los valores del input.
    // El "private set" asegura que solo este script pueda MODIFICARLOS.
    public Vector2 MoveInput { get; private set; }
    public bool SprintInput { get; private set; }

    void Update()
    {
        // Leemos los ejes de movimiento (WASD o joystick) y los guardamos.
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        MoveInput = new Vector2(horizontal, vertical);

        // Verificamos si la tecla de correr está presionada y guardamos el resultado.
        SprintInput = Input.GetKey(KeyCode.LeftShift);
    }
}