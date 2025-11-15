// Carga una escena al hacer clic en el objeto que tiene este script.
// Usa el nombre de escena asignado para realizar el cambio.
// Útil para botones de menú, transiciones y navegación entre pantallas.
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class CambiarEscenaAlClick : MonoBehaviour, IPointerClickHandler
{
    [Header("Nombre de la escena a cargar")]
    public string nombreEscena;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!string.IsNullOrEmpty(nombreEscena))
        {
            SceneManager.LoadScene(nombreEscena);
        }
        else
        {
            Debug.LogWarning("⚠️ No se ha asignado un nombre de escena en el script CambiarEscenaAlClick.");
        }
    }
}

