using UnityEngine;
using UnityEngine.EventSystems;

public class ActivarElementoConTecla : MonoBehaviour, IPointerClickHandler
{
    [Header("Objeto a activar")]
    [Tooltip("Arrastra aquí el objeto (imagen, texto, empty, etc.) que quieras activar.")]
    public GameObject objetoActivar;

    [Header("Tecla para activar")]
    [Tooltip("Tecla que activará el objeto al ser presionada.")]
    public KeyCode teclaActivar = KeyCode.E;  // Puedes cambiarla en el Inspector

    [Header("Opción para desactivar si ya está activo")]
    public bool alternar = false; // Si true, presionar la tecla hará ON/OFF

    void Update()
    {
        // Detecta la tecla presionada
        if (Input.GetKeyDown(teclaActivar))
        {
            Activar();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Detecta clic en el objeto (requiere EventSystem + componente con raycast)
        Activar();
    }

    private void Activar()
    {
        if (objetoActivar == null)
        {
            Debug.LogWarning("No hay ningún objeto asignado para activar en " + gameObject.name);
            return;
        }

        if (alternar)
        {
            bool nuevoEstado = !objetoActivar.activeSelf;
            objetoActivar.SetActive(nuevoEstado);
            Debug.Log($"Objeto {(nuevoEstado ? "activado" : "desactivado")}: {objetoActivar.name}");
        }
        else
        {
            objetoActivar.SetActive(true);
            Debug.Log("Objeto activado: " + objetoActivar.name);
        }
    }
}

