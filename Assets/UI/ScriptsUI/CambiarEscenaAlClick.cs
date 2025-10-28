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

