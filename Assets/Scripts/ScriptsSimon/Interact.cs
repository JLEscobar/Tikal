using UnityEngine;

public class TriggerInteract : MonoBehaviour
{
    public KeyCode interactKey = KeyCode.E;    // Tecla para interactuar
    private bool canInteract = false;          // ¿El jugador está dentro del área?
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = true;
            Debug.Log("Jugador dentro del área. Presiona E para interactuar.");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = false;
            Debug.Log("Jugador salió del área.");
        }
    }

    void Update()
    {
        if (canInteract && Input.GetKeyDown(interactKey))
        {
            Interact();
        }
    }

    // Acción al interactuar
    void Interact()
    {
        Debug.Log("Interacción realizada con el objeto.");
        // Aquí puedes agregar lo que quieras que pase
    }
}
