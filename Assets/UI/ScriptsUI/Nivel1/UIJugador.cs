using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIJugador : MonoBehaviour
{
    [System.Serializable]
    public class DatosPersonaje
    {
        [Header("Datos lógicos")]
        public string nombre;
        public string rol;
        public Sprite imagen;
        public int numero;
        public int vidaActual;
        public int vidaMaxima;

        [Header("Referencias UI (principal o mini)")]
        public Image imagenUI;
        public TextMeshProUGUI nombreUI;
        public TextMeshProUGUI rolUI;
        public TextMeshProUGUI numeroUI;

        [Header("Vida UI")]
        public RectTransform barraVidaTransform;
        public RectTransform barraVidaRelleno;
        public TextMeshProUGUI textoVida;
    }

    [Header("PERSONAJE PRINCIPAL (arrastrar objetos grandes)")]
    public DatosPersonaje personajePrincipal;

    [Header("PERSONAJES INFERIORES (2, 3, 4)")]
    public DatosPersonaje[] personajesMini;

    private int indiceActual = 0;
    private int indiceAIntercambiar = -1;

    [Header("Ventana de confirmación")]
    public VentanaConfirmacion1 ventana;

    public void SeleccionarPersonaje(int indiceMini)
    {
        indiceAIntercambiar = indiceMini;

        string nombreActual = personajePrincipal.nombre;
        ventana.Mostrar("¿Confirmar acciones de " + nombreActual + "?", this);
    }

    public void ConfirmarCambio()
    {
        if (indiceAIntercambiar < 0) return;

        DatosPersonaje pPrincipal = personajePrincipal;
        DatosPersonaje pMini = personajesMini[indiceAIntercambiar];

        string tempNombre = pPrincipal.nombre;
        string tempRol = pPrincipal.rol;
        Sprite tempImg = pPrincipal.imagen;
        int tempNum = pPrincipal.numero;
        int tempVida = pPrincipal.vidaActual;
        int tempMax = pPrincipal.vidaMaxima;

        pPrincipal.nombre = pMini.nombre;
        pPrincipal.rol = pMini.rol;
        pPrincipal.imagen = pMini.imagen;
        pPrincipal.numero = pMini.numero;
        pPrincipal.vidaActual = pMini.vidaActual;
        pPrincipal.vidaMaxima = pMini.vidaMaxima;

        pMini.nombre = tempNombre;
        pMini.rol = tempRol;
        pMini.imagen = tempImg;
        pMini.numero = tempNum;
        pMini.vidaActual = tempVida;
        pMini.vidaMaxima = tempMax;

        ActualizarUI();
        indiceAIntercambiar = -1;
    }

    void Start()
    {
        ActualizarUI();
    }

    public void ActualizarUI()
    {
        // Principal
        ActualizarPersonajeUI(personajePrincipal);

        // Mini personajes
        foreach (var p in personajesMini)
        {
            ActualizarPersonajeUI(p);
        }
    }

    private void ActualizarPersonajeUI(DatosPersonaje p)
    {
        if (p.imagenUI) p.imagenUI.sprite = p.imagen;
        if (p.nombreUI) p.nombreUI.text = p.nombre;
        if (p.numeroUI) p.numeroUI.text = p.numero.ToString();
        if (p.rolUI) p.rolUI.text = p.rol;

        if (p.barraVidaRelleno && p.barraVidaTransform)
        {
            float porcentaje = (float)p.vidaActual / p.vidaMaxima;
            float ancho = p.barraVidaTransform.sizeDelta.x;

            p.barraVidaRelleno.sizeDelta = new Vector2(ancho * porcentaje, p.barraVidaRelleno.sizeDelta.y);
        }

        if (p.textoVida)
            p.textoVida.text = p.vidaActual + " / " + p.vidaMaxima;
    }
}
