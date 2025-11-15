// Permite activar o desactivar un objeto UI haciendo clic o presionando una tecla.
// Alterna automáticamente la visibilidad del objeto asignado.
// Útil para mostrar/ocultar menús, paneles o elementos interactivos.
using UnityEngine;
using UnityEngine.EventSystems;

public class ActivarDesactivarUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Objeto que se activará o desactivará")]
    public GameObject objeto;

    [Header("Tecla para activar/desactivar")]
    public KeyCode teclaActivar = KeyCode.M; // cámbiala en el inspector si quieres

    void Update()
    {
        // Si se presiona la tecla, alternar visibilidad
        if (Input.GetKeyDown(teclaActivar))
        {
            AlternarObjeto();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Si se hace clic, alternar visibilidad
        AlternarObjeto();
    }

    private void AlternarObjeto()
    {
        if (objeto == null)
        {
            Debug.LogWarning("No hay objeto asignado para activar/desactivar en " + gameObject.name);
            return;
        }

        bool nuevoEstado = !objeto.activeSelf;
        objeto.SetActive(nuevoEstado);
        Debug.Log($"[{gameObject.name}] {(nuevoEstado ? "Activó" : "Desactivó")} {objeto.name}");
    }
}
