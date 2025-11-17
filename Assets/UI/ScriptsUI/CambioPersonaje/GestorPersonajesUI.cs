// Gestiona los personajes y actualiza la UI principal y de los slots.
// Muestra popups al seleccionar un personaje por número y permite confirmar el intercambio.
// Realiza el swap y activa solo las habilidades del personaje principal.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GestorPersonajesUI : MonoBehaviour
{
    [Header("Lista de personajes")]
    public List<PersonajeData> personajes = new List<PersonajeData>();

    [Header("UI Principal")]
    public Image imgPrincipal_Retrato;
    public TextMeshProUGUI textoPrincipal_Nombre;
    public TextMeshProUGUI textoPrincipal_Rol;
    public TextMeshProUGUI textoPrincipal_Numero;

    [Header("UI Slots Pequeños")]
    public List<Image> imgSlot_Retrato = new List<Image>();
    public List<TextMeshProUGUI> textoSlot_Nombre = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> textoSlot_Numero = new List<TextMeshProUGUI>();

    [Header("Popup Global (Opcional)")]
    public GameObject popupGlobal;
    public TextMeshProUGUI popupGlobal_Texto;
    public Button popupGlobal_BotonAceptar;
    public Button popupGlobal_BotonCancelar;

    [Header("Popups por slot (Opcional)")]
    // Estos popups funcionan por índice en la lista 'personajes'
    public List<GameObject> popupPorSlot = new List<GameObject>();
    public List<TextMeshProUGUI> popupPorSlot_Texto = new List<TextMeshProUGUI>();
    public List<Button> popupPorSlot_BotonAceptar = new List<Button>();
    public List<Button> popupPorSlot_BotonCancelar = new List<Button>();

    [Header("Configuración")]
    [Tooltip("Índice en la lista que corresponde a la posición principal (ej: si tu UI tiene la posición principal fija en la lista).")]
    public int indicePrincipal = 0;

    private int pendingSwapIndex = -1;

    // ----------------------------------------------------------------------

    void Start()
    {
        InicializarPopups();
        ActualizarTodaLaUI();
    }

    void Update()
    {
        // Escucha teclas 1..9 (cada tecla corresponde a un número de personaje, NO al índice)
        for (int i = 0; i < 9; i++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
            {
                int numeroBuscado = i + 1;
                MostrarPopupPorNumero(numeroBuscado);
            }
        }
    }

    // ----------------------------------------------------------------------
    // BÚSQUEDA POR NÚMERO Y POPUPS
    // ----------------------------------------------------------------------

    // Busca en la lista el índice cuyo personaje tenga PersonajeData.numero == numero
    // Retorna -1 si no existe
    private int EncontrarIndicePorNumero(int numero)
    {
        int found = -1;
        for (int i = 0; i < personajes.Count; i++)
        {
            if (personajes[i] != null && personajes[i].numero == numero)
            {
                found = i;
                break;
            }
        }
        return found;
    }

    // Llamar cuando el usuario presiona la tecla del número (1,2,3...)
    public void MostrarPopupPorNumero(int numero)
    {
        int slotIndex = EncontrarIndicePorNumero(numero);
        if (slotIndex == -1)
        {
            Debug.LogWarning($"No se encontró personaje con numero {numero}. Revisa PersonajeData.numero en el Inspector.");
            return;
        }

        // Si el personaje encontrado ya está en la posición principal, igual mostramos popup si quieres
        MostrarPopupParaSlot(slotIndex);
    }

    // Mostrar popup para un índice de la lista (misma lógica previa)
    public void MostrarPopupParaSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= personajes.Count) return;

        // Si el slotIndex es el mismo que está en principal -> no hace swap (pero puedes querer aún preguntar)
        if (slotIndex == indicePrincipal)
        {
            Debug.Log("El personaje seleccionado ya está en la posición principal.");
            return;
        }

        pendingSwapIndex = slotIndex;

        string nombrePrincipal = personajes[indicePrincipal].nombre;
        string nombreNuevo = personajes[slotIndex].nombre;
        string mensaje = $"¿Confirmas las acciones de {nombrePrincipal}?";

        // POPUP POR SLOT (si existe)
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

        Debug.LogWarning("No hay popup asignado (ni global ni por slot).");
    }

    // ----------------------------------------------------------------------
    // INICIALIZACIÓN POPUPS
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
                    if (popupGlobal != null) popupGlobal.SetActive(false);
                });
            }

            if (popupGlobal_BotonCancelar != null)
            {
                popupGlobal_BotonCancelar.onClick.RemoveAllListeners();
                popupGlobal_BotonCancelar.onClick.AddListener(() =>
                {
                    if (popupGlobal != null) popupGlobal.SetActive(false);
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
                    if (popupPorSlot[index] != null) popupPorSlot[index].SetActive(false);
                });
            }

            if (popupPorSlot_BotonCancelar.Count > i && popupPorSlot_BotonCancelar[i] != null)
            {
                popupPorSlot_BotonCancelar[i].onClick.RemoveAllListeners();
                popupPorSlot_BotonCancelar[i].onClick.AddListener(() =>
                {
                    if (popupPorSlot[index] != null) popupPorSlot[index].SetActive(false);
                    pendingSwapIndex = -1;
                });
            }
        }
    }

    // ----------------------------------------------------------------------
    // CONFIRMAR SWAP
    // ----------------------------------------------------------------------

    public void ConfirmarSwapDesdePopup(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= personajes.Count)
        {
            Debug.LogWarning("Índice inválido al confirmar swap.");
            pendingSwapIndex = -1;
            return;
        }

        IntercambiarPrincipalCon(slotIndex);
        pendingSwapIndex = -1;
    }

    // ----------------------------------------------------------------------
    // INTERCAMBIO
    // ----------------------------------------------------------------------

    // Hace swap entre el personaje que está en la posición 'indicePrincipal' y el personaje en 'slotIndex'
    public void IntercambiarPrincipalCon(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= personajes.Count) return;
        if (indicePrincipal < 0 || indicePrincipal >= personajes.Count) return;
        if (slotIndex == indicePrincipal) return;

        // swap de PersonajeData en la lista
        PersonajeData tmp = personajes[indicePrincipal];
        personajes[indicePrincipal] = personajes[slotIndex];
        personajes[slotIndex] = tmp;

        // Actualizar UI después del swap
        ActualizarTodaLaUI();
    }

    // ----------------------------------------------------------------------
    // UI
    // ----------------------------------------------------------------------

    public void ActualizarTodaLaUI()
    {
        ActualizarUIPrincipal();
        ActualizarUISlots();
        ActivarHabilidades();
    }

    void ActualizarUIPrincipal()
    {
        if (indicePrincipal < 0 || indicePrincipal >= personajes.Count) return;
        var p = personajes[indicePrincipal];

        if (imgPrincipal_Retrato) imgPrincipal_Retrato.sprite = p.retrato;
        if (textoPrincipal_Nombre) textoPrincipal_Nombre.text = p.nombre;
        if (textoPrincipal_Rol) textoPrincipal_Rol.text = p.rol;
        if (textoPrincipal_Numero) textoPrincipal_Numero.text = p.numero.ToString();
    }

    void ActualizarUISlots()
    {
        for (int i = 0; i < personajes.Count; i++)
        {
            var p = personajes[i];
            bool esPrincipal = (i == indicePrincipal);

            // Solo ocultar el retrato del slot que corresponde con la posición principal (sin desactivar padres)
            if (i < imgSlot_Retrato.Count && imgSlot_Retrato[i] != null)
                imgSlot_Retrato[i].gameObject.SetActive(!esPrincipal);

            if (esPrincipal) continue;

            if (i < imgSlot_Retrato.Count && imgSlot_Retrato[i] != null)
                imgSlot_Retrato[i].sprite = p.retrato;

            if (i < textoSlot_Nombre.Count && textoSlot_Nombre[i] != null)
                textoSlot_Nombre[i].text = p.nombre;

            if (i < textoSlot_Numero.Count && textoSlot_Numero[i] != null)
                textoSlot_Numero[i].text = p.numero.ToString();
        }
    }

    void ActivarHabilidades()
    {
        for (int i = 0; i < personajes.Count; i++)
            if (personajes[i] != null && personajes[i].habilidadesGO != null)
                personajes[i].habilidadesGO.SetActive(i == indicePrincipal);
    }
}
