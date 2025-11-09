using UnityEngine;
using UnityEngine.EventSystems;

public class ActivarElementoAlClick : MonoBehaviour, IPointerClickHandler
{
    [Header("Objeto a activar")]
    [Tooltip("Arrastra aquí el objeto (imagen, texto, empty, etc.) que quieras activar al hacer clic.")]
    public GameObject objetoActivar;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (objetoActivar != null)
        {
            objetoActivar.SetActive(true);
            Debug.Log("Objeto activado: " + objetoActivar.name);
        }
        else
        {
            Debug.LogWarning("No hay ningún objeto asignado para activar en " + gameObject.name);
        }
    }
}


