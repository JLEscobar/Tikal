// Cambia el color de varios elementos UI/SVG y activa una imagen al cumplirse una condición.
// Guarda los colores originales y los restaura cuando la condición deja de cumplirse.
// Funciona también en el editor gracias a ExecuteAlways.
using UnityEngine;
using UnityEngine.UI;
#if UNITY_VECTOR_GRAPHICS
using Unity.VectorGraphics; // si usas SVGImage
#endif

[ExecuteAlways] // Esto hace que también funcione en modo Editor
public class CambiarColorYActivar : MonoBehaviour
{
    [Header("Activar para cambiar color y mostrar imagen 'Bien'")]
    public bool seCumplio = false;

    [Header("Elementos SVG o UI que cambiarán de color")]
    public Graphic[] elementos; // Puedes arrastrar imágenes SVG o UI (Image, SVGImage, etc.)

    [Header("Imagen que se activará (por ejemplo 'Bien')")]
    public GameObject imagenBien;

    [Header("Color cuando se cumple (ej: 59F7DD)")]
    public Color colorCumplido = new Color(0.349f, 0.969f, 0.867f); // #59F7DD

    private Color[] coloresOriginales;

    void Start()
    {
        // Guardar colores originales
        GuardarColoresOriginales();
        ActualizarEstado();
    }

    void OnValidate()
    {
        // Esto se ejecuta al cambiar el check desde el Inspector
        GuardarColoresOriginales();
        ActualizarEstado();
    }

    private void GuardarColoresOriginales()
    {
        if (elementos == null || elementos.Length == 0) return;

        coloresOriginales = new Color[elementos.Length];
        for (int i = 0; i < elementos.Length; i++)
        {
            if (elementos[i] != null)
                coloresOriginales[i] = elementos[i].color;
        }
    }

    private void ActualizarEstado()
    {
        if (elementos == null || elementos.Length == 0) return;

        for (int i = 0; i < elementos.Length; i++)
        {
            if (elementos[i] != null)
            {
                elementos[i].color = seCumplio ? colorCumplido : coloresOriginales[i];
            }
        }

        if (imagenBien != null)
            imagenBien.SetActive(seCumplio);
    }
}
