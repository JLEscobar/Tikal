using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[ExecuteAlways]
public class GestorPersonajesUI : MonoBehaviour
{
    [Header("Lista de personajes")]
    public List<PersonajeData> personajes = new List<PersonajeData>();

    [Header("UI Principal")]
    public Image imgPrincipal_Retrato;
    public TextMeshProUGUI textoPrincipal_Nombre;
    public TextMeshProUGUI textoPrincipal_Rol;
    public TextMeshProUGUI textoPrincipal_Numero;
    public RectTransform contenedorBarraPrincipal;
    public Vector2 tamanioBarraPrincipal = new Vector2(300, 20);

    [Header("UI Slots Pequeños")]
    public List<Image> imgSlot_Retrato = new List<Image>();
    public List<TextMeshProUGUI> textoSlot_Nombre = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> textoSlot_Numero = new List<TextMeshProUGUI>();
    public List<RectTransform> contenedorBarraSlot = new List<RectTransform>();
    public Vector2 tamanioBarraSlot = new Vector2(140, 12);

    [Header("Popup Global (Opcional)")]
    public GameObject popupGlobal;
    public TextMeshProUGUI popupGlobal_Texto;
    public Button popupGlobal_BotonAceptar;
    public Button popupGlobal_BotonCancelar;

    [Header("Popups por slot (Opcional)")]
    public List<GameObject> popupPorSlot = new List<GameObject>();
    public List<TextMeshProUGUI> popupPorSlot_Texto = new List<TextMeshProUGUI>();
    public List<Button> popupPorSlot_BotonAceptar = new List<Button>();
    public List<Button> popupPorSlot_BotonCancelar = new List<Button>();

    [Header("Configuración")]
    public int indicePrincipal = 0;

    private GameObject barraInstanciadaEnPrincipal;
    private List<GameObject> barrasInstanciadasSlots = new List<GameObject>();

    private int pendingSwapIndex = -1; // Slot que pidió cambio

    // ----------------------------------------------------------------------

    void Start()
    {
        InicializarPopups();
        ActualizarTodaLaUI();
    }

    void Update()
    {
        for (int i = 0; i < personajes.Count && i < 9; i++)
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
                MostrarPopupParaSlot(i);
    }

    // ----------------------------------------------------------------------
    // POPUPS
    // ----------------------------------------------------------------------

    private void InicializarPopups()
    {
        // GLOBAL
        if (popupGlobal != null)
        {
            if (popupGlobal_BotonAceptar != null)
            {
                popupGlobal_BotonAceptar.onClick.RemoveAllListeners();
                popupGlobal_BotonAceptar.onClick.AddListener(() =>
                {
                    ConfirmarSwapDesdePopup(pendingSwapIndex);
                    popupGlobal.SetActive(false);
                });
            }

            if (popupGlobal_BotonCancelar != null)
            {
                popupGlobal_BotonCancelar.onClick.RemoveAllListeners();
                popupGlobal_BotonCancelar.onClick.AddListener(() =>
                {
                    popupGlobal.SetActive(false);
                    pendingSwapIndex = -1;
                });
            }
        }

        // POR SLOT
        for (int i = 0; i < popupPorSlot.Count; i++)
        {
            int index = i;

            if (popupPorSlot_BotonAceptar.Count > i && popupPorSlot_BotonAceptar[i] != null)
            {
                popupPorSlot_BotonAceptar[i].onClick.RemoveAllListeners();
                popupPorSlot_BotonAceptar[i].onClick.AddListener(() =>
                {
                    ConfirmarSwapDesdePopup(index);
                    popupPorSlot[index].SetActive(false);
                });
            }

            if (popupPorSlot_BotonCancelar.Count > i && popupPorSlot_BotonCancelar[i] != null)
            {
                popupPorSlot_BotonCancelar[i].onClick.RemoveAllListeners();
                popupPorSlot_BotonCancelar[i].onClick.AddListener(() =>
                {
                    popupPorSlot[index].SetActive(false);
                    pendingSwapIndex = -1;
                });
            }
        }
    }

    public void MostrarPopupParaSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= personajes.Count) return;
        if (slotIndex == indicePrincipal) return;

        pendingSwapIndex = slotIndex;

        string nombrePrincipal = personajes[indicePrincipal].nombre;
        string nombreNuevo = personajes[slotIndex].nombre;
        string mensaje = $"¿Reemplazar {nombrePrincipal} por {nombreNuevo}?";

        // POPUP POR SLOT
        if (slotIndex < popupPorSlot.Count && popupPorSlot[slotIndex] != null)
        {
            popupPorSlot[slotIndex].SetActive(true);

            if (slotIndex < popupPorSlot_Texto.Count && popupPorSlot_Texto[slotIndex] != null)
                popupPorSlot_Texto[slotIndex].text = mensaje;

            return;
        }

        // POPUP GLOBAL
        if (popupGlobal != null)
        {
            popupGlobal.SetActive(true);
            if (popupGlobal_Texto != null) popupGlobal_Texto.text = mensaje;
            return;
        }

        Debug.LogWarning("No hay popup asignado.");
    }

    public void ConfirmarSwapDesdePopup(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= personajes.Count) return;

        IntercambiarPrincipalCon(slotIndex);
        pendingSwapIndex = -1;
    }

    // ----------------------------------------------------------------------
    // LÓGICA DE INTERCAMBIO
    // ----------------------------------------------------------------------

    public void IntercambiarPrincipalCon(int slotIndex)
    {
        var tmp = personajes[indicePrincipal];
        personajes[indicePrincipal] = personajes[slotIndex];
        personajes[slotIndex] = tmp;

        ActualizarTodaLaUI();
    }

    // ----------------------------------------------------------------------
    // UI PRINCIPAL + SLOTS
    // ----------------------------------------------------------------------

    public void ActualizarTodaLaUI()
    {
        // Limpiar barras previas
        if (barraInstanciadaEnPrincipal != null) DestroyImmediate(barraInstanciadaEnPrincipal);
        foreach (var b in barrasInstanciadasSlots) if (b != null) DestroyImmediate(b);
        barrasInstanciadasSlots.Clear();

        ActualizarUIPrincipal();
        ActualizarUISlots();
        ActivarHabilidades();
    }

    void ActualizarUIPrincipal()
    {
        var p = personajes[indicePrincipal];

        if (imgPrincipal_Retrato) imgPrincipal_Retrato.sprite = p.retrato;
        if (textoPrincipal_Nombre) textoPrincipal_Nombre.text = p.nombre;
        if (textoPrincipal_Rol) textoPrincipal_Rol.text = p.rol;
        if (textoPrincipal_Numero) textoPrincipal_Numero.text = p.numero.ToString();

        if (contenedorBarraPrincipal && p.prefabBarraVida)
        {
            barraInstanciadaEnPrincipal = Instantiate(p.prefabBarraVida, contenedorBarraPrincipal);
            ConfigurarBarra(barraInstanciadaEnPrincipal, tamanioBarraPrincipal, p);
        }
    }

    void ActualizarUISlots()
    {
        // 2) Actualizar slots pequeños (asumimos que personajes.Count coincida con cantidad de slots)
        for (int i = 0; i < personajes.Count; i++)
        {
            var p = personajes[i];

            // 🔥 OCULTAR EL SLOT DEL PERSONAJE PRINCIPAL 🔥
            if (i == indicePrincipal)
            {
                if (imgSlot_Retrato.Count > i && imgSlot_Retrato[i] != null)
                    imgSlot_Retrato[i].transform.parent.gameObject.SetActive(false);

                // Añadimos un null a la lista para conservar el orden
                barrasInstanciadasSlots.Add(null);
                continue; // IMPORTANTE: evitar que siga actualizando este slot
            }
            else
            {
                if (imgSlot_Retrato.Count > i && imgSlot_Retrato[i] != null)
                    imgSlot_Retrato[i].transform.parent.gameObject.SetActive(true);
            }


            // --- DATOS DE UI DEL SLOT ---
            if (i < imgSlot_Retrato.Count && imgSlot_Retrato[i] != null)
                imgSlot_Retrato[i].sprite = p.retrato;

            if (i < textoSlot_Nombre.Count && textoSlot_Nombre[i] != null)
                textoSlot_Nombre[i].text = p.nombre;

            if (i < textoSlot_Numero.Count && textoSlot_Numero[i] != null)
                textoSlot_Numero[i].text = p.numero.ToString();


            // --- BARRA DE VIDA DEL SLOT ---
            if (i < contenedorBarraSlot.Count && contenedorBarraSlot[i] != null && p.prefabBarraVida != null)
            {
                GameObject barra = Instantiate(p.prefabBarraVida, contenedorBarraSlot[i]);
                barrasInstanciadasSlots.Add(barra);

                // Configurar tamaño y valores
                var rt = barra.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0, 0.5f);
                    rt.anchorMax = new Vector2(0, 0.5f);
                    rt.pivot = new Vector2(0, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = tamanioBarraSlot;
                }

                var barraVida = barra.GetComponent("BarraDeVidaSVG");
                if (barraVida != null)
                {
                    var vidaActualField = barraVida.GetType().GetField("vidaActual");
                    var vidaMaximaField = barraVida.GetType().GetField("vidaMaxima");

                    if (vidaActualField != null) vidaActualField.SetValue(barraVida, p.vidaActual);
                    if (vidaMaximaField != null) vidaMaximaField.SetValue(barraVida, p.vidaMaxima);
                }
            }
            else
            {
                barrasInstanciadasSlots.Add(null);
            }
        }

    }

    void ConfigurarBarra(GameObject barra, Vector2 size, PersonajeData p)
    {
        var rt = barra.GetComponent<RectTransform>();
        if (rt)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
        }

        var comp = barra.GetComponent("BarraDeVidaSVG");
        if (comp != null)
        {
            comp.GetType().GetField("vidaActual")?.SetValue(comp, p.vidaActual);
            comp.GetType().GetField("vidaMaxima")?.SetValue(comp, p.vidaMaxima);
        }
    }

    void ActivarHabilidades()
    {
        for (int i = 0; i < personajes.Count; i++)
            if (personajes[i].habilidadesGO != null)
                personajes[i].habilidadesGO.SetActive(i == indicePrincipal);
    }
}
