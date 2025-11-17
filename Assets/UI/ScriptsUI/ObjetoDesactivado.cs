// Este script permite desactivar un objeto y todos sus hijos visualmente.
// Cuando está desactivado baja la opacidad a un 30% y bloquea toda interacción.
// Útil para mostrar elementos de UI “apagados” o temporalmente inutilizables.
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ObjetoDesactivado : MonoBehaviour
{
    [Header("Si está activado, el objeto queda a 50% y no interactúa")]
    public bool desactivar = false;

    private bool ultimoEstado = false;

    void Update()
    {
        // Solo actualiza cuando el checkbox cambia
        if (desactivar != ultimoEstado)
        {
            ultimoEstado = desactivar;
            AplicarEstado(desactivar);
        }
    }

    void AplicarEstado(bool estado)
    {
        float alpha = estado ? 0.3f : 1f;

        // Cambiar opacidad en todos los componentes gráficos del objeto
        var graficos = GetComponentsInChildren<Graphic>(true);
        foreach (var g in graficos)
        {
            Color c = g.color;
            c.a = alpha;
            g.color = c;

            // Si está desactivado → no interactúa
            g.raycastTarget = !estado;
        }

        // Desactivar interacción en botones
        var botones = GetComponentsInChildren<Button>(true);
        foreach (var b in botones)
            b.interactable = !estado;

        // Desactivar eventos UI tipo toggles, sliders, etc.
        var selectables = GetComponentsInChildren<Selectable>(true);
        foreach (var s in selectables)
            s.interactable = !estado;
    }
}
