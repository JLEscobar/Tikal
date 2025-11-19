// Gestiona los personajes y actualiza la UI principal y de los slots.
// Muestra popups al seleccionar un personaje por número y permite confirmar el intercambio.
// Realiza el swap y activa solo las habilidades del personaje principal.
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        Debug.Log($"[GESTOR_PERSONAJES] ConfirmarSwapDesdePopup llamado con slotIndex: {slotIndex}");
        
        if (slotIndex < 0 || slotIndex >= personajes.Count)
        {
            Debug.LogWarning($"[GESTOR_PERSONAJES] Índice inválido al confirmar swap: {slotIndex}");
            pendingSwapIndex = -1;
            return;
        }

        Debug.Log($"[GESTOR_PERSONAJES] Llamando a IntercambiarPrincipalCon con slotIndex: {slotIndex}, indicePrincipal: {indicePrincipal}");
        IntercambiarPrincipalCon(slotIndex);
        pendingSwapIndex = -1;
    }

    // ----------------------------------------------------------------------
    // INTERCAMBIO
    // ----------------------------------------------------------------------

    // Hace swap entre el personaje que está en la posición 'indicePrincipal' y el personaje en 'slotIndex'
    public void IntercambiarPrincipalCon(int slotIndex)
    {
        Debug.Log($"[GESTOR_PERSONAJES] IntercambiarPrincipalCon llamado: slotIndex={slotIndex}, indicePrincipal={indicePrincipal}");
        
        if (slotIndex < 0 || slotIndex >= personajes.Count)
        {
            Debug.LogWarning($"[GESTOR_PERSONAJES] slotIndex fuera de rango: {slotIndex}");
            return;
        }
        if (indicePrincipal < 0 || indicePrincipal >= personajes.Count)
        {
            Debug.LogWarning($"[GESTOR_PERSONAJES] indicePrincipal fuera de rango: {indicePrincipal}");
            return;
        }
        if (slotIndex == indicePrincipal)
        {
            Debug.Log($"[GESTOR_PERSONAJES] slotIndex == indicePrincipal ({slotIndex}), no se hace swap");
            return;
        }

        Debug.Log($"[GESTOR_PERSONAJES] Haciendo swap: {personajes[indicePrincipal].nombre} <-> {personajes[slotIndex].nombre}");
        
        // swap de PersonajeData en la lista
        PersonajeData tmp = personajes[indicePrincipal];
        personajes[indicePrincipal] = personajes[slotIndex];
        personajes[slotIndex] = tmp;

        Debug.Log($"[GESTOR_PERSONAJES] Swap completado. Personaje principal ahora: {personajes[indicePrincipal].nombre}");

        // Actualizar UI después del swap (esto activa las habilidades del personaje principal)
        ActualizarTodaLaUI();
        
        Debug.Log($"[GESTOR_PERSONAJES] UI actualizada. Llamando a SincronizarConTurnSystem...");
        
        // Sincronizar con TurnSystem DESPUÉS de actualizar la UI
        // Esto asegura que el movimiento se active correctamente después de que las habilidades se activen
        SincronizarConTurnSystem();
    }
    
    // Sincroniza la selección del personaje principal con el TurnSystem
    private void SincronizarConTurnSystem()
    {
        Debug.Log($"[GESTOR_PERSONAJES] SincronizarConTurnSystem llamado. indicePrincipal: {indicePrincipal}");
        
        if (indicePrincipal < 0 || indicePrincipal >= personajes.Count)
        {
            Debug.LogWarning($"[GESTOR_PERSONAJES] indicePrincipal fuera de rango: {indicePrincipal}");
            return;
        }
        
        var personajePrincipal = personajes[indicePrincipal];
        if (personajePrincipal == null)
        {
            Debug.LogWarning($"[GESTOR_PERSONAJES] personajePrincipal es null en índice {indicePrincipal}");
            return;
        }
        
        Debug.Log($"[GESTOR_PERSONAJES] Personaje principal: {personajePrincipal.nombre}");
        
        // Buscar el TurnSystem
        var turnSystem = FindFirstObjectByType<TurnSystem>();
        if (turnSystem == null)
        {
            Debug.LogWarning("[GESTOR_PERSONAJES] TurnSystem no encontrado. No se puede sincronizar la selección.");
            return;
        }
        
        // Buscar el CharacterActor correspondiente al personaje principal por nombre
        var playerActors = turnSystem.PlayerTeamActors;
        if (playerActors == null || playerActors.Count == 0)
        {
            Debug.LogWarning("[GESTOR_PERSONAJES] No hay actores del equipo del jugador disponibles.");
            return;
        }
        
        // Buscar el actor por nombre (soporta variaciones como Patlee/Patlaa)
        CharacterActor actorToSelect = null;
        string nombreBuscado = personajePrincipal.nombre;
        
        actorToSelect = playerActors.FirstOrDefault(a => 
            a != null && 
            a.CharacterName.Equals(nombreBuscado, System.StringComparison.OrdinalIgnoreCase));
        
        // Si no se encuentra con el nombre exacto, intentar variaciones para Patlee
        if (actorToSelect == null && (nombreBuscado.Contains("Patl", System.StringComparison.OrdinalIgnoreCase) || 
                                      nombreBuscado.Contains("Patlee", System.StringComparison.OrdinalIgnoreCase)))
        {
            actorToSelect = playerActors.FirstOrDefault(a => 
                a != null && 
                (a.CharacterName.Equals("Patlee", System.StringComparison.OrdinalIgnoreCase) ||
                 a.CharacterName.Equals("Patlaa", System.StringComparison.OrdinalIgnoreCase)));
        }
        
        if (actorToSelect != null)
        {
            // Verificar si el personaje ya está seleccionado
            bool yaEstaSeleccionado = (turnSystem.CurrentActor == actorToSelect);
            
            Debug.Log($"[GESTOR_PERSONAJES] Intentando sincronizar: {actorToSelect.CharacterName}. Ya seleccionado: {yaEstaSeleccionado}");
            
            // SIEMPRE forzar la selección del personaje, incluso si ya está seleccionado
            // Esto asegura que el movimiento se reactive y la cámara no cambie
            if (yaEstaSeleccionado)
            {
                Debug.Log($"[GESTOR_PERSONAJES] Personaje {actorToSelect.CharacterName} ya está seleccionado. Forzando reactivación del movimiento...");
                
                // Asegurar que el TurnSystem tenga el personaje correcto seleccionado
                // (por si acaso hay alguna discrepancia)
                if (turnSystem.CurrentActor != actorToSelect)
                {
                    Debug.LogWarning($"[GESTOR_PERSONAJES] ⚠ Discrepancia detectada! TurnSystem.CurrentActor != actorToSelect. Forzando selección...");
                    turnSystem.SetCurrentActor(actorToSelect);
                }
                
                // Asegurar que el TurnSystem esté en el equipo del jugador
                if (turnSystem.CurrentTeam != Team.Player)
                {
                    Debug.LogWarning($"[GESTOR_PERSONAJES] ⚠ TurnSystem.CurrentTeam es {turnSystem.CurrentTeam}, forzando a Team.Player...");
                    // No podemos cambiar directamente el CurrentTeam, pero podemos asegurarnos de que el actor esté seleccionado
                }
                
                var tacticalMovement = actorToSelect.GetComponent<TacticalMovementController>();
                if (tacticalMovement != null)
                {
                    // Terminar el movimiento actual primero para limpiar el estado
                    tacticalMovement.SetMovementPhase(false);
                    
                    // Esperar un frame para que se limpie completamente, luego reactivar
                    StartCoroutine(ReactivarMovimientoDespuesDeFrame(actorToSelect, tacticalMovement));
                }
                else
                {
                    // Si no hay TacticalMovementController, al menos llamar a BeginTurn
                    actorToSelect.BeginTurn();
                    Debug.Log($"[GESTOR_PERSONAJES] ✓ BeginTurn llamado para {actorToSelect.CharacterName} (sin TacticalMovementController).");
                }
            }
            else
            {
                // Si no está seleccionado, seleccionarlo normalmente
                // PERO primero asegurarse de que el TurnSystem esté en el equipo del jugador
                if (turnSystem.CurrentTeam != Team.Player)
                {
                    Debug.LogWarning($"[GESTOR_PERSONAJES] ⚠ TurnSystem.CurrentTeam es {turnSystem.CurrentTeam}, no Team.Player. No se puede seleccionar.");
                    return;
                }
                
                // Forzar la selección del personaje
                bool seleccionado = turnSystem.SetCurrentActor(actorToSelect);
                
                if (seleccionado)
                {
                    Debug.Log($"[GESTOR_PERSONAJES] ✓ Sincronizado con TurnSystem: {actorToSelect.CharacterName} seleccionado.");
                    
                    // Verificar que la selección fue exitosa
                    if (turnSystem.CurrentActor == actorToSelect)
                    {
                        Debug.Log($"[GESTOR_PERSONAJES] ✓ Verificación: TurnSystem.CurrentActor es {actorToSelect.CharacterName}");
                    }
                    else
                    {
                        Debug.LogError($"[GESTOR_PERSONAJES] ✗ ERROR: TurnSystem.CurrentActor NO es {actorToSelect.CharacterName} después de SetCurrentActor!");
                    }
                }
                else
                {
                    Debug.LogWarning($"[GESTOR_PERSONAJES] ✗ No se pudo seleccionar {actorToSelect.CharacterName} en TurnSystem.");
                }
            }
        }
        else
        {
            Debug.LogWarning($"[GESTOR_PERSONAJES] ✗ No se encontró CharacterActor para el personaje: {nombreBuscado}");
        }
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
    
    // Coroutine para reactivar el movimiento después de un frame
    private IEnumerator ReactivarMovimientoDespuesDeFrame(CharacterActor actor, TacticalMovementController movement)
    {
        yield return null; // Esperar un frame
        yield return null; // Esperar otro frame para asegurar que todo se limpió
        
        // Verificar que el TurnSystem todavía tenga este personaje seleccionado
        var turnSystem = FindFirstObjectByType<TurnSystem>();
        if (turnSystem != null && turnSystem.CurrentActor != actor)
        {
            Debug.LogWarning($"[GESTOR_PERSONAJES] ⚠ El TurnSystem cambió de personaje! Esperado: {actor.CharacterName}, Actual: {(turnSystem.CurrentActor == null ? "null" : turnSystem.CurrentActor.CharacterName)}. Forzando selección...");
            turnSystem.SetCurrentActor(actor);
            yield return null; // Esperar un frame después de forzar la selección
        }
        
        // Verificar que el movimiento esté desactivado antes de reactivarlo
        if (movement.IsMovementPhaseActive)
        {
            Debug.LogWarning($"[GESTOR_PERSONAJES] ⚠ El movimiento de {actor.CharacterName} todavía está activo después de desactivarlo. Forzando desactivación...");
            movement.SetMovementPhase(false);
            yield return null; // Esperar otro frame
        }
        
        // Reactivar el movimiento
        actor.BeginTurn();
        
        // Verificar que se activó correctamente
        yield return null; // Esperar un frame más para que se complete la activación
        
        // Verificar nuevamente que el TurnSystem todavía tenga este personaje seleccionado
        if (turnSystem != null && turnSystem.CurrentActor != actor)
        {
            Debug.LogError($"[GESTOR_PERSONAJES] ✗ ERROR CRÍTICO: El TurnSystem cambió de personaje después de reactivar el movimiento! Esperado: {actor.CharacterName}, Actual: {(turnSystem.CurrentActor == null ? "null" : turnSystem.CurrentActor.CharacterName)}");
            // Intentar forzar la selección nuevamente
            turnSystem.SetCurrentActor(actor);
            actor.BeginTurn();
        }
        
        if (movement.IsMovementPhaseActive)
        {
            Debug.Log($"[GESTOR_PERSONAJES] ✓ Movimiento reactivado correctamente para {actor.CharacterName} después del swap.");
        }
        else
        {
            Debug.LogError($"[GESTOR_PERSONAJES] ✗ ERROR: El movimiento de {actor.CharacterName} NO se activó después del swap!");
        }
    }
}
