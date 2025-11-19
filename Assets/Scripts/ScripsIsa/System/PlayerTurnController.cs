using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerTurnController : MonoBehaviour
{
    [SerializeField] private TurnSystem turnSystem;
    [SerializeField] private Camera cam;
    
    [SerializeField] private LayerMask targetMask = ~0; 

    [Header("Input Settings")]
    [Tooltip("DEBE ESTAR MARCADO. Clic en enemigo = Ataque Básico (Q).")]
    [SerializeField] private bool attackOnClick = true; 
    [SerializeField] private int defaultAbilityIndex = 0; // Índice 0: Ataque Básico (Q o Clic)
    [SerializeField] private int specialAbilityIndex = 1; // Índice 1: Habilidad Especial (E)

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

        // Manejar Space y Enter ANTES de verificar _current, para poder terminar la fase incluso sin personaje seleccionado
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("[PLAYER_INPUT] Space pressed - Forcing end of player phase");
            turnSystem.EndTurn(true);
            return;
        }
        
        if (Input.GetKeyDown(KeyCode.Return))
        {
            Debug.Log("[PLAYER_INPUT] Enter pressed - Forcing end of player phase");
            turnSystem.EndTurn(true);
            return;
        }

        CheckCharacterSelectionInput();
        
        if (_current == null) return; 

        if (Input.GetKeyDown(KeyCode.Tab)) CycleTarget();
        
        // 1. Tecla Q (Ataque Básico - Alternativa)
        if (Input.GetKeyDown(KeyCode.Q)) TryUseAbilityKey(defaultAbilityIndex);
        
        // 2. Tecla E (Habilidad Especial)
        if (Input.GetKeyDown(KeyCode.E)) TryUseAbilityKey(specialAbilityIndex); 
        
        if (Input.GetKeyDown(KeyCode.P)) TogglePause();


        // Lógica de detección por Clic (Mouse)
        if (Input.GetMouseButtonDown(0)) 
        {
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(ray, out var hit, 200f, targetMask))
            {
                var t = hit.collider.GetComponentInParent<CharacterActor>();
                var eo = hit.collider.GetComponentInParent<ExplosiveObject>(); 
                
                if (t != null || eo != null)
                {
                    if (t != null)
                    {
                        _currentTarget = t;
                    }
                    else if (eo != null)
                    {
                        _currentTarget = eo;
                    }
                    
                    if (attackOnClick && _current != null && _current.ActionPoints >= 1)
                    {
                        TryUseAbilityKey(defaultAbilityIndex);
                    }
                }
                else
                {
                    _currentTarget = null;
                }
            }
        }
    }
    
    private void TryUseAbilityKey(int index)
    {
        if (_current == null) 
        {
             MessagesSystem.Instance.ShowMessage("Primero selecciona un personaje (1-4).", Color.red);
             return;
        }

        if (_current.ActionPoints < 1)
        {
             MessagesSystem.Instance.ShowMessage("¡No quedan Puntos de Acción para la acción!", Color.red);
             return;
        }

        var ability = _current.GetAbilityByIndex(index);
        if (ability == null) return;

        ITargetable targetToUse = ResolveTargetForAbility(ability, _currentTarget);
        if (targetToUse == null)
        {
            bool needsFriendly = AbilityNeedsFriendlyTarget(ability);
            string reason = needsFriendly 
                ? "No hay aliados disponibles para recibir la habilidad."
                : "No hay enemigos en rango para la habilidad.";

            Debug.Log($"[PLAYER_INPUT] Target inválido para {ability.DisplayName}. {reason}");
            MessagesSystem.Instance.ShowMessage(reason, needsFriendly ? Color.cyan : Color.red);
            return;
        }

        _currentTarget = targetToUse;

        if (_current.TryUseAbility(index, targetToUse))
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
            // La habilidad falló (por rango o cooldown)
            string reason = GetAbilityFailureReason(ability, _current, targetToUse);
            Debug.Log($"[vAP_FIX_FINAL] Cannot use {ability.DisplayName}: {reason}");
            
            // Silenciar el error de rango del ATAQUE BÁSICO para no interferir
            if (reason.Contains("OUT OF RANGE") && index == defaultAbilityIndex)
            {
                 // Silencioso. Permite que el jugador presione E después.
            }
            else
            {
                MessagesSystem.Instance.ShowMessage($"No se pudo usar {ability.DisplayName}.", Color.red);
            }
        }
    }
    
    // NUEVO MÉTODO: Encuentra el enemigo más cercano dentro del rango de la habilidad.
    private CharacterActor FindClosestValidTarget(CharacterActor user, AbilityBase referenceAbility)
    {
        if (user == null || referenceAbility == null || turnSystem == null) return null;

        CharacterActor closestTarget = null;
        float closestDistance = float.MaxValue;
        float maxRange = referenceAbility.Range + 0.1f;

        var opponents = turnSystem.GetOpponentsOf(user.Team)
            .Where(o => !o.Health.IsDead)
            .Cast<CharacterActor>();

        foreach (var opponent in opponents)
        {
            float distance = Vector3.Distance(user.transform.position, opponent.transform.position);

            if (distance <= maxRange && distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = opponent;
            }
        }

        return closestTarget;
    }

    private CharacterActor FindBestAllyTarget(CharacterActor user)
    {
        if (user == null || turnSystem == null || turnSystem.PlayerTeamActors == null) return null;

        var allies = turnSystem.PlayerTeamActors
            .Where(a => a != null && !a.Health.IsDead && a.Team == user.Team)
            .Cast<CharacterActor>()
            .ToList();

        if (allies.Count == 0) return null;

        var injured = allies
            .Where(a => a.Health.CurrentHealth < a.Health.MaxHealth)
            .OrderBy(a => (float)a.Health.CurrentHealth / a.Health.MaxHealth)
            .ToList();

        if (injured.Count > 0) return injured.First();

        return allies.Contains(user) ? user : allies.First();
    }

    private bool AbilityNeedsFriendlyTarget(AbilityBase ability)
    {
        return ability is HealAbility || ability is SupportAbility;
    }

    private ITargetable ResolveTargetForAbility(AbilityBase ability, ITargetable desiredTarget)
    {
        if (_current == null || ability == null) return null;

        // AoE siempre se centra en el usuario
        if (ability is AreaAttackAbility)
        {
            return _current;
        }

        bool requiresFriendly = AbilityNeedsFriendlyTarget(ability);

        if (desiredTarget != null && IsTargetValidForAbility(ability, _current, desiredTarget))
        {
            return desiredTarget;
        }

        if (requiresFriendly)
        {
            var ally = FindBestAllyTarget(_current);
            return ally;
        }

        return FindClosestValidTarget(_current, ability);
    }

    private bool IsTargetValidForAbility(AbilityBase ability, CharacterActor user, ITargetable candidate)
    {
        if (user == null || candidate == null) return false;

        if (candidate.Health != null && candidate.Health.IsDead) return false;

        if (ability is AreaAttackAbility)
        {
            return candidate == user;
        }

        bool requiresFriendly = AbilityNeedsFriendlyTarget(ability);

        if (requiresFriendly)
        {
            return candidate.Team == user.Team;
        }

        return candidate.Team != user.Team;
    }
    
    // ... (El resto de métodos se mantienen sin cambios) ...
    
    private string GetAbilityFailureReason(AbilityBase ability, CharacterActor user, ITargetable target)
    {
        if (target == null) return "Internal error: Target is null after validation."; 
        
        float distance = Vector3.Distance(user.transform.position, target.GetTransform().position);
        
        if (ability.currentCooldown > 0) return $"In Cooldown ({ability.currentCooldown} turns)";
        if (distance > ability.Range + 0.1f) return $"OUT OF RANGE! Distance: {distance:F1}m, Max: {ability.Range}m.";
        if (user.ActionPoints < ability.CostAP) return $"Not enough AP (need {ability.CostAP}, have {user.ActionPoints})";
        if (target is CharacterActor targetActor && targetActor.Health.IsDead) return "Target is dead";
        
        return "Invalid target/ability check failed";
    }

    private void CheckCharacterSelectionInput()
    {
        int actorIndex = -1;

        if (Input.GetKeyDown(KeyCode.Alpha1)) actorIndex = 0;
        else if (Input.GetKeyDown(KeyCode.Alpha2)) actorIndex = 1;
        else if (Input.GetKeyDown(KeyCode.Alpha3)) actorIndex = 2;
        else if (Input.GetKeyDown(KeyCode.Alpha4)) actorIndex = 3;
        
        if (actorIndex != -1) SelectPlayerActor(actorIndex);
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

    private void CycleTarget()
    {
        if (_cachedOpponents.Count == 0) return;
        
        _cachedOpponents = turnSystem.GetOpponentsOf(Team.Player).Where(o => !o.Health.IsDead).ToList();

        if (_cachedOpponents.Count > 0)
        {
            _cursor = (_cursor + 1) % _cachedOpponents.Count;
            _currentTarget = _cachedOpponents[_cursor];
            Debug.Log($"[vAP_FIX_FINAL] Cycled to target: {(_currentTarget as CharacterActor)?.CharacterName}");
        }
        else
        {
            _currentTarget = null;
            MessagesSystem.Instance.ShowMessage("No quedan enemigos vivos.", Color.green);
        }
    }

    private void TogglePause() => PauseService.TogglePause();

    // MANEJADORES DE EVENTOS DEL TURNSYSTEM
    private void HandleTurnStart(Team team, CharacterActor actor)
    {
        if (team == Team.Player)
        {
            _playerActors = turnSystem.PlayerTeamActors; 
            _current = actor; 
            _cachedOpponents = turnSystem.GetOpponentsOf(Team.Player).Where(o => !o.Health.IsDead).ToList();
            _cursor = -1;
            _currentTarget = null;
            Debug.Log($"[vAP_FIX_FINAL] Player Turn Handler: Phase started. Active actor: {(_current == null ? "None" : _current.CharacterName)}");
        }
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
}