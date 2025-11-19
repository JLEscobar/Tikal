using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float sensitivity = 300f;   // Sensibilidad del mouse
    public Transform playerBody;       // El objeto que rota horizontalmente (normalmente el Player)

    float xRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; // Oculta y bloquea el cursor al centro
    }

    void Update()
    {
        // Leer movimiento del mouse
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;

        // Rotación vertical (cámara)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f); // Limita rotación arriba/abajo

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Rotación horizontal (cuerpo del jugador)
        playerBody.Rotate(Vector3.up * mouseX);
    }
}
