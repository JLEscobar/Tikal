using UnityEngine;
using UnityEngine.UI;

public class BotonPersonaje : MonoBehaviour
{
    public int indiceMini;
    public UIJugador manejador;

    public void Seleccionar()
    {
        manejador.SeleccionarPersonaje(indiceMini);
    }
}
