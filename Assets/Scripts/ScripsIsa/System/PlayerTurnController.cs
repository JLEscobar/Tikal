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
    
    private IReadOnlyList<CharacterActor> _playerActors; 

    void OnEnable()
    {
        if (turnSystem == null) turnSystem = FindFirstObjectByType<TurnSystem>();
        if (cam == null) cam = Camera.main;
        
        // CONEXIÓN CORRECTA DE EVENTOS (CS0103)
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

        CheckCharacterSelectionInput(); // MÉTODO ACCESIBLE
        
        if (_current == null) return; 

        if (Input.GetKeyDown(KeyCode.Tab)) CycleTarget(); // MÉTODO ACCESIBLE
        
        // Teclas Q y E (Habilidades 0 y 1)
        if (Input.GetKeyDown(KeyCode.Q)) 
        {
            if (_current.ActionPoints >= 1) TryUseAbility(0); 
            else MessagesSystem.Instance.ShowMessage("¡No quedan Puntos de Acción!", Color.red);
        }
        
        if (Input.GetKeyDown(KeyCode.E)) 
        {
            if (_current.ActionPoints >= 1) TryUseAbility(1);
            else MessagesSystem.Instance.ShowMessage("¡No quedan Puntos de Acción!", Color.red);
        }
        
        if (Input.GetKeyDown(KeyCode.Return)) turnSystem.EndTurn();
        
        if (Input.GetKeyDown(KeyCode.P)) TogglePause(); // MÉTODO ACCESIBLE

        // Lógica de detección por Clic (Mouse)
        if (Input.GetMouseButtonDown(0))
        {
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(ray, out var hit, 200f, targetMask))
            {
                var t = hit.collider.GetComponentInParent<CharacterActor>();
                var eo = hit.collider.GetComponentInParent<ExplosiveObject>(); 
                
                // Prioridad 1: Objetos Explosivos (para detonar)
                if (eo != null && _current != null && _current.ActionPoints >= 1)
                {
                    _currentTarget = eo; 
                    TryUseAbility(defaultAbilityIndex);
                }
                // Prioridad 2: Personajes (para seleccionar y atacar)
                else if (t != null && !t.Health.IsDead)
                {
                    _currentTarget = t;
                    if (attackOnClick && _current != null && _current.ActionPoints >= 1) TryUseAbility(defaultAbilityIndex);
                }
                // Si no es un personaje ni un explosivo, limpiamos el target.
                else
                {
                    _currentTarget = null;
                }
            }
        }
    }
    
    // ********************************************
    // * MÉTODOS AUXILIARES Y MANEJADORES DE EVENTOS *
    // * (Ahora accesibles por el contexto de la clase) *
    // ********************************************

    private void TryUseAbility(int index)
    {
        if (_current.ActionPoints < 1) return; 

        var ability = _current.GetAbilityByIndex(index);
        if (ability == null) return;
        
        if (_current.TryUseAbility(index, _currentTarget))
        {
            Debug.Log($"[vAP_FIX_FINAL] {_current.CharacterName} used {ability.DisplayName} successfully.");

            if (ability.CostAP > 0)
            {
                MessagesSystem.Instance.ShowMessage($"Turno de {_current.CharacterName} finalizado. Elige otro personaje (1-4).", Color.yellow);
                turnSystem.EndTurn(); 
            }
        }
        else
        {
            var targetActor = _currentTarget as CharacterActor;
            string reason = "";
            float distance = (_currentTarget != null) ? Vector3.Distance(_current.transform.position, _currentTarget.GetTransform().position) : 9999f;
            
            // Diagnóstico de error
            if (distance > ability.Range + 0.1f)
            {
                reason = $"OUT OF RANGE! Distance: {distance:F1}m, Max: {ability.Range}m.";
            }
            else if (_current.ActionPoints < ability.CostAP)
            {
                reason = $"Not enough AP (need {ability.CostAP}, have {_current.ActionPoints})";
            }
            else if (targetActor != null && targetActor.Health.IsDead)
            {
                reason = "Target is dead";
            }
            else if (_currentTarget == null && !(ability is AreaAttackAbility))
            {
                 reason = $"Must select a target for '{ability.DisplayName}'"; 
            }
            else
            {
                reason = "Invalid target/ability check failed";
            }
             Debug.Log($"[vAP_FIX_FINAL] Cannot use {ability.DisplayName}: {reason}");
        }
    }
    
    private void CheckCharacterSelectionInput()
    {
        int actorIndex = -1;

        if (Input.GetKeyDown(KeyCode.Alpha1)) actorIndex = 0; // Tecla 1 (Selección)
        else if (Input.GetKeyDown(KeyCode.Alpha2)) actorIndex = 1; // Tecla 2 (Selección)
        else if (Input.GetKeyDown(KeyCode.Alpha3)) actorIndex = 2; // Tecla 3 (Selección)
        else if (Input.GetKeyDown(KeyCode.Alpha4)) actorIndex = 3; // Tecla 4 (Selección)
        
        if (actorIndex != -1)
        {
            SelectPlayerActor(actorIndex);
        }
    }

    private void SelectPlayerActor(int index)
    {
        if (_playerActors == null || index < 0 || index >= _playerActors.Count)
        {
            MessagesSystem.Instance.ShowMessage("Número de personaje inválido o no existe.", Color.red);
            return;
        }

        var selectedActor = _playerActors[index];
        
        if (turnSystem.SetCurrentActor(selectedActor))
        {
            _current = selectedActor;
            _currentTarget = null;
        }
    }

    private void AutoEndTurn()
    {
        if (_current != null && turnSystem.CurrentTeam == Team.Player)
            turnSystem.EndTurn();
    }

    private void CycleTarget()
    {
        if (_cachedOpponents.Count == 0) return;
        _cursor = (_cursor + 1) % _cachedOpponents.Count;
        _currentTarget = _cachedOpponents[_cursor];
        Debug.Log($"[vAP_FIX_FINAL] Cycled to target: {(_currentTarget as CharacterActor)?.CharacterName}");
    }

    private void TogglePause()
    {
        if (PauseService.IsPaused) turnSystem.Resume();
        else turnSystem.Pause();
    }

    // MANEJADORES DE EVENTOS DEL TURNSYSTEM (CS0103 en OnEnable)
    private void HandleTurnStart(Team team, CharacterActor actor)
    {
        if (team != Team.Player)
        {
            _current = actor;
            _cachedOpponents = turnSystem.GetOpponentsOf(Team.Player).Where(o => !o.Health.IsDead).ToList();
            _currentTarget = null;
            return;
        }
        
        _playerActors = turnSystem.PlayerTeamActors; 
        _current = actor; 
        _cachedOpponents = turnSystem.GetOpponentsOf(Team.Player).Where(o => !o.Health.IsDead).ToList();
        _cursor = -1;
        _currentTarget = null;
        Debug.Log($"[vAP_FIX_FINAL] Player Turn Handler: Phase started. Active actor: {(_current == null ? "None" : _current.CharacterName)}");
    }

    private void HandleTurnEnd(Team team, CharacterActor actor)
    {
        if (team != Team.Player)
        {
            _current = null;
            _cachedOpponents.Clear();
            _currentTarget = null; 
            _cursor = -1;
        }
    }
    
    public void ForceEndTurn()
    {
        if (turnSystem.CurrentTeam == Team.Player)
        {
            turnSystem.EndTurn();
            Debug.Log($"[vAP_FIX_FINAL] Turn force-ended by player.");
        }
    }
}