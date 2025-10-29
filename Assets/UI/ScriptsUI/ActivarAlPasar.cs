using UnityEngine;
using UnityEngine.EventSystems;

public class ActivarAlPasar : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Objeto que se activará al pasar el mouse")]
    public GameObject objetoAlPasar;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (objetoAlPasar != null)
            objetoAlPasar.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (objetoAlPasar != null)
            objetoAlPasar.SetActive(false);
    }
}

