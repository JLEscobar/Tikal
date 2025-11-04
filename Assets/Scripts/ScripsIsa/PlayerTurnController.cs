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

        CheckCharacterSelectionInput();
        
        if (_current == null) return; 

        if (Input.GetKeyDown(KeyCode.Tab)) CycleTarget();
        if (Input.GetKeyDown(KeyCode.Alpha1)) TryUseAbility(0); 
        if (Input.GetKeyDown(KeyCode.Alpha2)) TryUseAbility(1); 
        
        if (Input.GetKeyDown(KeyCode.Return)) turnSystem.EndTurn();
        
        if (Input.GetKeyDown(KeyCode.P)) TogglePause();

        if (Input.GetMouseButtonDown(0))
        {
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 200f, targetMask))
            {
                var t = hit.collider.GetComponentInParent<CharacterActor>();
                
                if (t != null && !t.Health.IsDead)
                {
                    _currentTarget = t;
                    string targetType = t.Team == _current.Team ? "ally" : "enemy";
                    Debug.Log($"[vFinal] Selected {targetType}: {t.CharacterName}");

                    if (attackOnClick)
                    {
                        TryUseAbility(defaultAbilityIndex);
                    }
                }
                else if (t != null && t.Health.IsDead)
                {
                    Debug.Log($"[vFinal] Cannot select {t.CharacterName} - already dead");
                }
            }
        }
    }
    
    private void CheckCharacterSelectionInput()
    {
        int actorIndex = -1;

        if (Input.GetKeyDown(KeyCode.Alpha1)) actorIndex = 0; 
        else if (Input.GetKeyDown(KeyCode.Alpha2)) actorIndex = 1; 
        else if (Input.GetKeyDown(KeyCode.Alpha3)) actorIndex = 2; 
        else if (Input.GetKeyDown(KeyCode.Alpha4)) actorIndex = 3; 
        
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

    private void TryUseAbility(int index)
    {
        if (_current.ActionPoints <= 0)
        {
            MessagesSystem.Instance.ShowMessage($"¡{_current.CharacterName} no tiene Puntos de Acción para usar habilidades!", Color.red);
            return;
        }
        
        var ability = _current.GetAbilityByIndex(index);
        if (ability == null)
        {
            Debug.Log($"[vFinal] No ability at index {index}");
            return;
        }

        if (_current.TryUseAbility(index, _currentTarget))
        {
            Debug.Log($"[vFinal] {_current.CharacterName} used {ability.DisplayName} on {(_currentTarget as CharacterActor)?.CharacterName}");

            if (attackOnClick && _current.ActionPoints <= 0)
            {
                MessagesSystem.Instance.ShowMessage($"Turno de {_current.CharacterName} finalizado. Elige otro personaje (1-4).", Color.yellow);
                turnSystem.EndTurn(); 
            }
        }
        else
        {
            var targetActor = _currentTarget as CharacterActor;
            string reason = "";

            if (_current.ActionPoints < ability.CostAP)
            {
                reason = $"Not enough AP (need {ability.CostAP}, have {_current.ActionPoints})";
            }
            else if (targetActor != null && targetActor.Health.IsDead)
            {
                reason = "Target is dead";
            }
            else
            {
                float distance = Vector3.Distance(_current.transform.position, _currentTarget.GetTransform().position);
                if (distance > ability.Range)
                {
                    reason = $"Out of range (distance: {distance:F1}, max: {ability.Range})";
                }
                else if (_currentTarget == null && !(ability is AreaAttackAbility))
                {
                     reason = $"Must select a target for '{ability.DisplayName}'"; // Mensaje si falta target para habilidad no AoE
                }
                else
                {
                    reason = "Invalid target for this ability";
                }
            }
             Debug.Log($"[vFinal] Cannot use {ability.DisplayName}: {reason}");
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
        if (_cachedOpponents.Count == 0) return;
        _cursor = (_cursor + 1) % _cachedOpponents.Count;
        _currentTarget = _cachedOpponents[_cursor];
        Debug.Log($"[vFinal] Cycled to target: {(_currentTarget as CharacterActor)?.CharacterName}");
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
        
        _playerActors = turnSystem.PlayerTeamActors; 
        _current = actor; 
        _cachedOpponents = turnSystem.GetOpponentsOf(Team.Player).Where(o => !o.Health.IsDead).ToList();
        _cursor = -1;
        _currentTarget = null;
        Debug.Log($"[vFinal] Player Turn Handler: Phase started. Active actor: {(_current == null ? "None" : _current.CharacterName)}");
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

    private void TogglePause()
    {
        if (PauseService.IsPaused) turnSystem.Resume();
        else turnSystem.Pause();
    }
    
    public void ForceEndTurn()
    {
        if (turnSystem.CurrentTeam == Team.Player)
        {
            turnSystem.EndTurn();
            Debug.Log($"[vFinal] Turn force-ended by player.");
        }
    }
}