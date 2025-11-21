using UnityEngine;
using System;

public class TriggerInteract : MonoBehaviour
{
    public KeyCode interactKey = KeyCode.E;    // Tecla para interactuar
    private bool canInteract = false;          // ¿El jugador está dentro del área?
    
    // Evento estático para notificar cuando se presiona la tecla de interacción
    public static event Action OnInteractKeyPressed;
    
    // Propiedad pública para verificar si se puede interactuar
    public static bool CanInteract { get; private set; }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = true;
            CanInteract = true; // Actualizar el valor estático
            Debug.Log("Jugador dentro del área. Presiona E para interactuar.");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = false;
            CanInteract = false; // Actualizar el valor estático
            Debug.Log("Jugador salió del área.");
        }
    }

    void Update()
    {
        if (canInteract && Input.GetKeyDown(interactKey))
        {
            // Disparar el evento antes de interactuar
            OnInteractKeyPressed?.Invoke();
            
            Interact();
        }
    }

    // Acción al interactuar
    void Interact()
    {
        Debug.Log("Interacción realizada con el objeto.");
        // Aquí puedes agregar lo que quieras que pase

        // Llamar al método CompletarObjetivo1 del script Objetivos
        Objetivos objetivos = FindObjectOfType<Objetivos>();
        if (objetivos != null)
        {
            objetivos.CompletarObjetivo1();
        }
    }
}
