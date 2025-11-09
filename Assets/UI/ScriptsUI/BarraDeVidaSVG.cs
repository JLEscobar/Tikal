using UnityEngine;
using Unity.VectorGraphics;
using TMPro;

[ExecuteAlways] // Se actualiza también en el editor sin darle Play
public class BarraDeVidaSVG : MonoBehaviour
{
    [Header("Referencias")]
    public SVGImage imagenFondo;        // SVG del fondo
    public SVGImage imagenRelleno;      // SVG del relleno (la barra)
    public TextMeshProUGUI textoVida;   // Texto que muestra los valores

    [Header("Configuración de vida")]
    [Range(0, 300)] public float vidaActual = 300;
    public float vidaMaxima = 300;
    [Range(0f, 1f)] public float umbralVidaBaja = 0.25f; // 25% o menos

    [Header("Colores del fondo")]
    public Color colorFondoNormal = new Color(0.1f, 0.1f, 0.1f, 1f);     // Gris oscuro
    public Color colorFondoBajo = new Color(0.4f, 0.15f, 0.05f, 1f);      // Marrón-naranja suave

    [Header("Colores del relleno")]
    public Color colorRellenoNormal = new Color(0.35f, 0.97f, 0.86f, 1f); // Azulito (#59F7DD)
    public Color colorRellenoBajo = new Color(1f, 0.45f, 0.27f, 1f);      // Naranjita (#FF733E)

    [Header("Colores del texto")]
    public Color colorTextoNormal = Color.white;
    public Color colorTextoBajo = new Color(1f, 0.8f, 0.7f, 1f);

    private float anchoInicial;
    private RectTransform rellenoRect;
    private SVGImage svgRelleno;

    void Start()
    {
        if (imagenRelleno != null)
        {
            rellenoRect = imagenRelleno.GetComponent<RectTransform>();
            svgRelleno = imagenRelleno;
            anchoInicial = rellenoRect.sizeDelta.x;
        }

        ActualizarBarra();
    }

    void Update()
    {
        ActualizarBarra();
    }

    void ActualizarBarra()
    {
        if (rellenoRect == null || svgRelleno == null) return;

        float porcentaje = Mathf.Clamp01(vidaActual / vidaMaxima);

        // Cambiar tamaño del relleno
        rellenoRect.sizeDelta = new Vector2(anchoInicial * porcentaje, rellenoRect.sizeDelta.y);

        // Asegurar que el relleno decrezca desde la izquierda
        rellenoRect.pivot = new Vector2(0f, 0.5f);
        rellenoRect.anchorMin = new Vector2(0f, 0.5f);
        rellenoRect.anchorMax = new Vector2(0f, 0.5f);
        rellenoRect.anchoredPosition = new Vector2(0f, rellenoRect.anchoredPosition.y);

        // Cambiar colores según el porcentaje
        bool vidaBaja = porcentaje <= umbralVidaBaja;

        // Relleno
        svgRelleno.color = vidaBaja ? colorRellenoBajo : colorRellenoNormal;

        // Fondo
        if (imagenFondo != null)
            imagenFondo.color = vidaBaja ? colorFondoBajo : colorFondoNormal;

        // Texto
        if (textoVida != null)
        {
            textoVida.text = Mathf.RoundToInt(vidaActual) + " / " + Mathf.RoundToInt(vidaMaxima);
            textoVida.color = vidaBaja ? colorTextoBajo : colorTextoNormal;
        }
    }
}
