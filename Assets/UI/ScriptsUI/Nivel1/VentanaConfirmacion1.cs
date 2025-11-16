using UnityEngine;
using TMPro;

public class VentanaConfirmacion1 : MonoBehaviour
{
    public TextMeshProUGUI textoPregunta;
    private UIJugador manejador;

    public void Mostrar(string texto, UIJugador refJugador)
    {
        manejador = refJugador;
        textoPregunta.text = texto;
        gameObject.SetActive(true);
    }

    public void Aceptar()
    {
        manejador.ConfirmarCambio();
        gameObject.SetActive(false);
    }

    public void Cancelar()
    {
        gameObject.SetActive(false);
    }
}
