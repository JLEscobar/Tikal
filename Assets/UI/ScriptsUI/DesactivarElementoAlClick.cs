using UnityEngine;
using UnityEngine.EventSystems;

public class DesactivarElementoAlClick : MonoBehaviour, IPointerClickHandler
{
    [Header("Objeto a desactivar")]
    [Tooltip("Arrastra aquí el objeto (imagen, texto, empty, etc.) que quieras desactivar al hacer clic.")]
    public GameObject objetoDesactivar;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (objetoDesactivar != null)
        {
            objetoDesactivar.SetActive(false);
            Debug.Log("Objeto desactivado: " + objetoDesactivar.name);
        }
        else
        {
            Debug.LogWarning("No hay ningún objeto asignado para desactivar en " + gameObject.name);
        }
    }
}
