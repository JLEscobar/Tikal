using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("Sensibilidad del mouse (estándar industria: 2-5). Ajusta según preferencia.")]
    [Range(0.5f, 10f)]
    public float sensitivity = 2.0f;   // Sensibilidad estándar de la industria (sin Time.deltaTime)
    public Transform playerBody;       // El objeto que rota horizontalmente (normalmente el Player)

    float xRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; // Oculta y bloquea el cursor al centro
    }

    void Update()
    {
        // Leer movimiento del mouse (sin Time.deltaTime para sensibilidad estándar de la industria)
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        // Rotación vertical (cámara)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f); // Limita rotación arriba/abajo

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Rotación horizontal (cuerpo del jugador)
        playerBody.Rotate(Vector3.up * mouseX);
    }
}
