// Controla un botón SVG cambiando colores según su estado: normal, hover, clic o deshabilitado.
// Responde a eventos del mouse con transiciones visuales para marco, interior y texto.
// Permite activar/desactivar el botón y actualizar su apariencia automáticamente.
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.VectorGraphics;
using TMPro;

public class BotonInteractivo : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Referencias")]
    public SVGImage ImagenMarco;
    public SVGImage ImagenInterior;
    public TextMeshProUGUI Texto;

    [Header("Estado")]
    public bool estaDeshabilitado = false;

    [Header("Colores - Normal / Hover / Click")]
    public Color colorMarcoNormal = new Color32(0x59, 0xF7, 0xDD, 0xFF); // Azul base
    public Color colorInteriorNormal = new Color32(0x59, 0xF7, 0xDD, 0x80); // Semi transparente
    public Color colorTextoNormal = Color.white;

    public Color colorMarcoHover = new Color32(0x59, 0xF7, 0xDD, 0xFF);
    public Color colorInteriorHover = new Color32(0x59, 0xF7, 0xDD, 0xB0);
    public Color colorTextoHover = Color.white;

    public Color colorMarcoClick = new Color32(0x59, 0xF7, 0xDD, 0xFF);
    public Color colorInteriorClick = new Color32(0x59, 0xF7, 0xDD, 0xC0);
    public Color colorTextoClick = Color.white;

    [Header("Colores deshabilitado")]
    public Color colorMarcoDeshabilitado = new Color32(0x59, 0xF7, 0xDD, 0x40);  // 25% visible
    public Color colorInteriorDeshabilitado = new Color32(0x59, 0xF7, 0xDD, 0x15); // 8% visible
    public Color colorTextoDeshabilitado = new Color32(0x59, 0xF7, 0xDD, 0x60); // 38% visible

    private bool presionado = false;

    void Start()
    {
        AplicarEstadoActual();
    }

    void OnValidate()
    {
        // Actualiza al instante si se cambia desde el editor
        AplicarEstadoActual();
    }

    public void SetDeshabilitado(bool valor)
    {
        estaDeshabilitado = valor;
        AplicarEstadoActual();
    }

    private void AplicarEstadoActual()
    {
        if (estaDeshabilitado)
            AplicarColoresDeshabilitado();
        else
            AplicarColoresNormales();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (estaDeshabilitado) return;
        if (!presionado)
            AplicarColoresHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (estaDeshabilitado) return;
        if (!presionado)
            AplicarColoresNormales();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (estaDeshabilitado) return;
        presionado = true;
        AplicarColoresClick();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (estaDeshabilitado) return;
        presionado = false;
        AplicarColoresHover();
    }

    // ---- Métodos para cambiar colores ----
    void AplicarColoresNormales()
    {
        if (ImagenMarco) ImagenMarco.color = colorMarcoNormal;
        if (ImagenInterior) ImagenInterior.color = colorInteriorNormal;
        if (Texto) Texto.color = colorTextoNormal;
    }

    void AplicarColoresHover()
    {
        if (ImagenMarco) ImagenMarco.color = colorMarcoHover;
        if (ImagenInterior) ImagenInterior.color = colorInteriorHover;
        if (Texto) Texto.color = colorTextoHover;
    }

    void AplicarColoresClick()
    {
        if (ImagenMarco) ImagenMarco.color = colorMarcoClick;
        if (ImagenInterior) ImagenInterior.color = colorInteriorClick;
        if (Texto) Texto.color = colorTextoClick;
    }

    void AplicarColoresDeshabilitado()
    {
        if (ImagenMarco) ImagenMarco.color = colorMarcoDeshabilitado;
        if (ImagenInterior) ImagenInterior.color = colorInteriorDeshabilitado;
        if (Texto) Texto.color = colorTextoDeshabilitado;
    }
}
