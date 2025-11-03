using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerTurnController : MonoBehaviour
{
    [SerializeField] private TurnSystem turnSystem;
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask targetMask = ~0;

    [Header("Input Settings")]
    [SerializeField] private bool attackOnClick = true;
    [SerializeField] private int defaultAbilityIndex = 0;

    private CharacterActor _current;
    private List<CharacterActor> _cachedOpponents = new();
    private int _cursor;
    private ITargetable _currentTarget;
    
    // NUEVO: Lista de actores del jugador para selección por índice
    private IReadOnlyList<CharacterActor> _playerActors; 

    void OnEnable()
    {
        if (turnSystem == null) turnSystem = FindFirstObjectByType<TurnSystem>();
        if (cam == null) cam = Camera.main;
        turnSystem.OnTurnStarted += HandleTurnStart;
        turnSystem.OnTurnEnded += HandleTurnEnd;
    }

    void OnDisable()
    {
        if (turnSystem == null) return;
        turnSystem.OnTurnStarted -= HandleTurnStart;
        turnSystem.OnTurnEnded -= HandleTurnEnd;
    }

    void Update()
    {
        if (PauseService.IsPaused) return;
        if (turnSystem.CurrentTeam != Team.Player) return;

        // Lógica de SELECCIÓN de personaje (activa siempre durante el turno del jugador)
        CheckCharacterSelectionInput();
        
        // Lógica de ACCIÓN (sólo si hay un personaje activo)
        if (_current == null) return; 

        // Resto del código de Update para habilidades y clicks (MANTENIDO)
        if (Input.GetKeyDown(KeyCode.Tab)) CycleTarget();
        if (Input.GetKeyDown(KeyCode.Alpha1)) TryUseAbility(0); // Usa la habilidad 0
        if (Input.GetKeyDown(KeyCode.Alpha2)) TryUseAbility(1); // Usa la habilidad 1
        
        // La tecla ENTER finaliza el turno del ACTOR actual.
        if (Input.GetKeyDown(KeyCode.Return)) turnSystem.EndTurn();
        
        if (Input.GetKeyDown(KeyCode.P)) TogglePause();

        if (Input.GetMouseButtonDown(0))
        {
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 200f, targetMask))
            {
                var t = hit.collider.GetComponentInParent<CharacterActor>();
                // Permitir seleccionar cualquier CharacterActor que no esté muerto
                if (t != null && !t.Health.IsDead)
                {
                    _currentTarget = t;
                    string targetType = t.Team == _current.Team ? "ally" : "enemy";
                    Debug.Log($"[v2] Selected {targetType}: {t.CharacterName}");

                    if (attackOnClick)
                    {
                        TryUseAbility(defaultAbilityIndex);
                    }
                }
                else if (t != null && t.Health.IsDead)
                {
                    Debug.Log($"[v2] Cannot select {t.CharacterName} - already dead");
                }
            }
        }
    }
    
    // NUEVO: Lógica de selección por teclado (1, 2, 3, 4)
    private void CheckCharacterSelectionInput()
    {
        int actorIndex = -1;

        if (Input.GetKeyDown(KeyCode.Alpha1)) actorIndex = 0; // Cualli
        else if (Input.GetKeyDown(KeyCode.Alpha2)) actorIndex = 1; // Ollin
        else if (Input.GetKeyDown(KeyCode.Alpha3)) actorIndex = 2; // Yaotl
        else if (Input.GetKeyDown(KeyCode.Alpha4)) actorIndex = 3; // Patlee
        
        if (actorIndex != -1)
        {
            SelectPlayerActor(actorIndex);
        }
    }

    // NUEVO: Lógica para seleccionar y activar el personaje usando el índice
    private void SelectPlayerActor(int index)
    {
        if (_playerActors == null || index < 0 || index >= _playerActors.Count)
        {
            MessagesSystem.Instance.ShowMessage("Número de personaje inválido o no existe.", Color.red);
            return;
        }

        var selectedActor = _playerActors[index];
        
        // El TurnSystem hace todas las validaciones y activa el personaje.
        if (turnSystem.SetCurrentActor(selectedActor))
        {
            _current = selectedActor;
            _currentTarget = null;
        }
    }

    private void TryUseAbility(int index)
    {
        // ... (El resto del código se mantiene igual, usando _current)
        if (_currentTarget == null)
        {
            Debug.Log("[v2] No target selected. Click on a character first.");
            return;
        }
        
        var ability = _current.GetAbilityByIndex(index);
        if (ability == null)
        {
            Debug.Log($"[v2] No ability at index {index}");
            return;
        }

        if (_current.TryUseAbility(index, _currentTarget))
        {
            Debug.Log($"[v2] {_current.CharacterName} used {ability.DisplayName} on {(_currentTarget as CharacterActor)?.CharacterName}");

            if (attackOnClick && _current.ActionPoints <= 0)
            {
                MessagesSystem.Instance.ShowMessage($"Turno de {_current.CharacterName} finalizado. Elige otro personaje (1-4).", Color.yellow);
                turnSystem.EndTurn(); // Finaliza el turno del ACTOR (lo setea a null en TurnSystem)
            }
        }
        else
        {
            // Diagnóstico de error (MANTENIDO)
        }
    }

    private void AutoEndTurn()
    {
        if (_current != null && turnSystem.CurrentTeam == Team.Player)
        {
            turnSystem.EndTurn();
        }
    }

    private void CycleTarget()
    {
        // ... (MANTENIDO)
        if (_cachedOpponents.Count == 0) return;
        _cursor = (_cursor + 1) % _cachedOpponents.Count;
        _currentTarget = _cachedOpponents[_cursor];
        Debug.Log($"[v2] Cycled to target: {(_currentTarget as CharacterActor)?.CharacterName}");
    }

    private void HandleTurnStart(Team team, CharacterActor actor)
    {
        if (team != Team.Player)
        {
            _current = actor;
            _cachedOpponents = turnSystem.GetOpponentsOf(Team.Player).Where(o => !o.Health.IsDead).ToList();
            _currentTarget = null;
            return;
        }
        
        // Si team == Team.Player y actor == null, es la fase de selección.
        _playerActors = turnSystem.PlayerTeamActors; 
        _current = actor; // Será null en fase de selección
        _cachedOpponents = turnSystem.GetOpponentsOf(Team.Player).Where(o => !o.Health.IsDead).ToList();
        _cursor = -1;
        _currentTarget = null;
        Debug.Log($"[v2] Player Turn Handler: Phase started. Active actor: {(_current == null ? "None" : _current.CharacterName)}");
    }

    private void HandleTurnEnd(Team team, CharacterActor actor)
    {
        // Sólo reseteamos el estado si no es el turno del jugador, o si el actor finalizado era el único activo.
        if (team != Team.Player)
        {
            _current = null;
            _cachedOpponents.Clear();
            _currentTarget = null;
            _cursor = -1;
        }
    }

    private void TogglePause()
    {
        if (PauseService.IsPaused) turnSystem.Resume();
        else turnSystem.Pause();
    }
    
    public void ForceEndTurn()
    {
        // ForceEndTurn del PlayerTurnController ahora usa EndTurn del TurnSystem
        // El TurnSystem decide si esto finaliza el actor o el equipo completo.
        if (turnSystem.CurrentTeam == Team.Player)
        {
            turnSystem.EndTurn();
            Debug.Log($"[v2] Turn force-ended by player.");
        }
    }
}