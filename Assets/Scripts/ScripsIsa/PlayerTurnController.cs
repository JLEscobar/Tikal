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
        if (_current == null || turnSystem.CurrentTeam != Team.Player) return;

        // Keyboard shortcuts
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
                // Permitir seleccionar cualquier CharacterActor que no esté muerto
                if (t != null && !t.Health.IsDead)
                {
                    _currentTarget = t;
                    string targetType = t.Team == _current.Team ? "ally" : "enemy";
                    Debug.Log($"[v0] Selected {targetType}: {t.CharacterName}");

                    if (attackOnClick)
                    {
                        TryUseAbility(defaultAbilityIndex);
                    }
                }
                else if (t != null && t.Health.IsDead)
                {
                    Debug.Log($"[v0] Cannot select {t.CharacterName} - already dead");
                }
            }
        }
    }

    private void TryUseAbility(int index)
    {
        if (_currentTarget == null)
        {
            Debug.Log("[v0] No target selected. Click on a character first.");
            return;
        }

        var ability = _current.GetAbilityByIndex(index);
        if (ability == null)
        {
            Debug.Log($"[v0] No ability at index {index}");
            return;
        }

        if (_current.TryUseAbility(index, _currentTarget))
        {
            Debug.Log($"[v0] {_current.CharacterName} used {ability.DisplayName} on {(_currentTarget as CharacterActor)?.CharacterName}");

            if (attackOnClick && _current.ActionPoints <= 0)
            {
                Debug.Log("[v0] No action points left, ending turn automatically");
                Invoke(nameof(AutoEndTurn), 0.5f);
            }
        }
        else
        {
            // Diagnosticar por qué falló
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
                else if (ability is MeleeAttackAbility && targetActor != null && targetActor.Team == _current.Team)
                {
                    reason = "Cannot attack allies (use Heal ability instead)";
                }
                else if (ability is HealAbility && targetActor != null && targetActor.Team != _current.Team)
                {
                    reason = "Cannot heal enemies (use Attack ability instead)";
                }
                else
                {
                    reason = "Invalid target for this ability";
                }
            }

            Debug.Log($"[v0] Cannot use {ability.DisplayName}: {reason}");
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
        Debug.Log($"[v0] Cycled to target: {(_currentTarget as CharacterActor)?.CharacterName}");
    }

    private void HandleTurnStart(Team team, CharacterActor actor)
    {
        if (team != Team.Player) return;
        _current = actor;
        _cachedOpponents = turnSystem.GetOpponentsOf(Team.Player).Where(o => !o.Health.IsDead).ToList();
        _cursor = -1;
        _currentTarget = null;
        Debug.Log($"[v0] Player turn started: {actor.CharacterName}");
    }

    private void HandleTurnEnd(Team team, CharacterActor actor)
    {
        if (team != Team.Player) return;
        _current = null;
        _cachedOpponents.Clear();
        _currentTarget = null;
        _cursor = -1;
    }

    private void TogglePause()
    {
        if (PauseService.IsPaused) turnSystem.Resume();
        else turnSystem.Pause();
    }
    public void ForceEndTurn()
    {
        if (_current != null && turnSystem.CurrentTeam == Team.Player)
        {
            Debug.Log($"[v0] Turn force-ended by player ({_current.CharacterName})");
            turnSystem.EndTurn();
        }
        else
        {
            Debug.Log("[v0] Cannot force end turn: it's not the player's turn or no active character.");
        }
    }

}
