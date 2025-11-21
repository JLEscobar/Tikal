using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerTurnController : MonoBehaviour
{
    [SerializeField] private TurnSystem turnSystem;
    [SerializeField] private Camera cam;
    
    [SerializeField] private LayerMask targetMask = ~0; 

    [Header("Input Settings")]
    // attackOnClick removido - El ataque básico ahora solo se ejecuta con Q 
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
        if (Input.GetKeyDown(KeyCode.T))
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
        if (Input.GetKeyDown(KeyCode.Space)) TryUseAbilityKey(specialAbilityIndex); 


        // Lógica de detección por Clic (Mouse) - Solo selecciona objetivo, NO ataca automáticamente
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
                    
                    // Ataque automático desactivado - Usa Q para atacar después de seleccionar objetivo
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
             Debug.LogWarning("[ABILITY] TryUseAbilityKey: _current es null. Selecciona un personaje primero.");
             return;
        }

        var ability = _current.GetAbilityByIndex(index);
        if (ability == null)
        {
            Debug.LogError($"[ABILITY] {_current.CharacterName}: No se encontró habilidad en índice {index}");
            return;
        }
        
        Debug.Log($"[ABILITY] {_current.CharacterName} intenta usar habilidad índice {index}: {ability.DisplayName}");
        
        // Verificar que el jugador tenga AP suficiente para esta habilidad específica
        if (_current.ActionPoints < ability.CostAP)
        {
             MessagesSystem.Instance.ShowMessage($"¡No tienes suficientes AP! Necesitas {ability.CostAP} AP para usar {ability.DisplayName}.", Color.red);
             Debug.Log($"[AP_CHECK] {_current.CharacterName}: AP insuficientes. Tiene: {_current.ActionPoints}, Necesita: {ability.CostAP}");
             return;
        }
        
        ITargetable targetToUse = _currentTarget;
        
        // ***** INICIO DE LA CORRECCIÓN CLAVE *****
        // 1. Si la habilidad es AoE (Cualli), el target es el propio usuario.
        if (ability is AreaAttackAbility)
        {
            targetToUse = _current;
        }
        // 2. Si la habilidad NO es AoE y no tenemos target (Space/Q presionada sin clic):
        else if (targetToUse == null)
        {
             Debug.Log($"[ABILITY] {_current.CharacterName}: No hay target seleccionado para {ability.DisplayName}. Buscando automáticamente...");
             
             // Si el jugador presionó Space (Habilidad Especial) o Q (Ataque Básico), buscamos el target más cercano
             if (index == specialAbilityIndex || index == defaultAbilityIndex)
             {
                 // Detectar si es habilidad de soporte (curación/buff) para buscar aliados en lugar de enemigos
                 bool isSupportAbility = ability is SupportAbility || ability is HealAbility;
                 Debug.Log($"[ABILITY] {_current.CharacterName}: Buscando target para {ability.DisplayName} (isSupport: {isSupportAbility}, rango: {ability.Range}m)");
                 
                 targetToUse = FindClosestValidTarget(_current, ability, isSupportAbility);
                 
                 // Si aun así no encontramos target, fallamos
                 if (targetToUse == null)
                 {
                    string targetType = isSupportAbility ? "aliados" : "enemigos";
                    string reason = $"No se encontraron {targetType} dentro del rango.";
                    Debug.LogWarning($"[ABILITY] {_current.CharacterName}: No se puede usar {ability.DisplayName} - {reason}");
                    MessagesSystem.Instance.ShowMessage($"No hay {targetType} en rango para {ability.DisplayName} (Rango: {ability.Range}m).", Color.red);
                    return;
                 }
                 
                 // Si encontramos un target, actualizamos _currentTarget para que se mantenga seleccionado
                 if (targetToUse is CharacterActor foundTarget)
                 {
                     _currentTarget = foundTarget;
                     Debug.Log($"[TARGET_SELECT] {_current.CharacterName}: Target automático seleccionado: {foundTarget.CharacterName}");
                 }
             }
             else 
             {
                 // Si no es especial ni básico, fallamos pidiendo target
                 string reason = $"Must select a target for '{ability.DisplayName}'";
                 Debug.LogWarning($"[ABILITY] {_current.CharacterName}: {reason}");
                 MessagesSystem.Instance.ShowMessage($"Selecciona un objetivo para usar {ability.DisplayName}.", Color.yellow);
                 return;
             }
        }
        else
        {
            // Si hay un target seleccionado, verificar que sea válido para la habilidad
            if (ability is SupportAbility || ability is HealAbility)
            {
                // Para habilidades de soporte, verificar que el target sea aliado
                if (targetToUse is CharacterActor selectedTarget && selectedTarget.Team != _current.Team)
                {
                    Debug.LogWarning($"[TARGET_CHECK] {_current.CharacterName} intentó usar {ability.DisplayName} en {selectedTarget.CharacterName} pero es enemigo. Buscando aliado automáticamente...");
                    targetToUse = FindClosestValidTarget(_current, ability, true);
                    if (targetToUse != null && targetToUse is CharacterActor foundAlly)
                    {
                        _currentTarget = foundAlly;
                        Debug.Log($"[TARGET_FIX] Target corregido a aliado: {foundAlly.CharacterName}");
                    }
                }
            }
        }
        // ***** FIN DE LA CORRECCIÓN CLAVE *****


        // Log detallado antes de intentar usar la habilidad
        Debug.Log($"[ABILITY_DEBUG] {_current.CharacterName} intentando usar {ability.DisplayName} (índice {index}):");
        Debug.Log($"[ABILITY_DEBUG] - Target: {(targetToUse == null ? "null" : targetToUse.GetTransform().name)}");
        Debug.Log($"[ABILITY_DEBUG] - AP actuales: {_current.ActionPoints}, Costo: {ability.CostAP}");
        Debug.Log($"[ABILITY_DEBUG] - Cooldown: {ability.currentCooldown}");
        if (targetToUse != null)
        {
            float distance = Vector3.Distance(_current.transform.position, targetToUse.GetTransform().position);
            Debug.Log($"[ABILITY_DEBUG] - Distancia al target: {distance:F2}m, Rango máximo: {ability.Range}m");
        }
        
        if (_current.TryUseAbility(index, targetToUse))
        {
            Debug.Log($"[vAP_FIX_FINAL] {_current.CharacterName} used {ability.DisplayName} successfully. Remaining AP: {_current.ActionPoints}");
            
            // Verificar si el jugador todavía tiene AP después de usar la habilidad
            if (_current.ActionPoints <= 0)
            {
                // No quedan AP, informar al usuario pero NO terminar el turno automáticamente
                // El usuario debe presionar "Terminar turno" explícitamente para cambiar de player
                MessagesSystem.Instance.ShowMessage($"{_current.CharacterName} se quedó sin AP. Presiona 'Terminar turno' para cambiar de personaje.", Color.yellow);
            }
            else
            {
                // Todavía tiene AP, puede usar otra habilidad
                MessagesSystem.Instance.ShowMessage($"{_current.CharacterName} usó {ability.DisplayName}. AP restantes: {_current.ActionPoints}", Color.cyan);
            }
        }
        else
        {
            // La habilidad falló (por rango o cooldown)
            string reason = GetAbilityFailureReason(ability, _current, targetToUse);
            Debug.LogWarning($"[ABILITY_FAIL] {_current.CharacterName} no pudo usar {ability.DisplayName}: {reason}");
            Debug.LogWarning($"[ABILITY_FAIL] - Detalles: AP={_current.ActionPoints}/{ability.CostAP}, Cooldown={ability.currentCooldown}, Target={(targetToUse == null ? "null" : targetToUse.GetTransform().name)}");
            
            // Mostrar mensaje de error más descriptivo
            if (reason.Contains("OUT OF RANGE") && index == defaultAbilityIndex)
            {
                 // Silencioso. Permite que el jugador presione E después.
            }
            else
            {
                MessagesSystem.Instance.ShowMessage($"No se pudo usar {ability.DisplayName}: {reason}", Color.red);
            }
        }
    }
    
    // MÉTODO: Encuentra el target más cercano (aliado o enemigo) dentro del rango de la habilidad específica.
    private CharacterActor FindClosestValidTarget(CharacterActor user, AbilityBase abilityToUse, bool findAllies = false)
    {
        if (abilityToUse == null)
        {
            Debug.LogError($"[TARGET_FIND] {user?.CharacterName}: abilityToUse es null!");
            return null;
        }

        CharacterActor closestTarget = null;
        float closestDistance = float.MaxValue;
        float maxRange = abilityToUse.Range + 0.1f;

        Debug.Log($"[TARGET_FIND] {user.CharacterName}: Buscando target para {abilityToUse.DisplayName} (findAllies: {findAllies}, rango: {abilityToUse.Range}m)");

        // Si es habilidad de soporte, buscar aliados; si no, buscar enemigos
        IEnumerable<CharacterActor> candidates;
        if (findAllies)
        {
            // Obtener aliados del mismo equipo
            if (user.Team == Team.Player)
            {
                candidates = turnSystem.PlayerTeamActors
                    .Where(a => a != null && !a.Health.IsDead && a != user);
            }
            else
            {
                // Para enemigos, necesitamos obtener sus aliados (otros enemigos)
                candidates = turnSystem.GetOpponentsOf(Team.Player)
                    .Where(a => a != null && !a.Health.IsDead && a != user && a.Team == user.Team);
            }
        }
        else
        {
            candidates = turnSystem.GetOpponentsOf(user.Team)
                .Where(o => !o.Health.IsDead)
                .Cast<CharacterActor>();
        }

        int candidateCount = candidates.Count();
        Debug.Log($"[TARGET_FIND] {user.CharacterName}: Encontrados {candidateCount} candidatos potenciales");

        foreach (var candidate in candidates)
        {
            if (candidate == null) continue;
            
            float distance = Vector3.Distance(user.transform.position, candidate.transform.position);
            Debug.Log($"[TARGET_FIND] {user.CharacterName}: Candidato {candidate.CharacterName} a {distance:F2}m (rango máximo: {maxRange:F2}m)");

            if (distance <= maxRange && distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = candidate;
                Debug.Log($"[TARGET_FIND] {user.CharacterName}: Nuevo target más cercano: {candidate.CharacterName} a {distance:F2}m");
            }
        }

        if (closestTarget != null)
        {
            string targetType = findAllies ? "aliado" : "enemigo";
            Debug.Log($"[TARGET_FIND] ✓ {user.CharacterName}: Encontrado {targetType} más cercano para {abilityToUse.DisplayName}: {closestTarget.CharacterName} a {closestDistance:F2}m (Rango máximo: {abilityToUse.Range}m)");
        }
        else
        {
            string targetType = findAllies ? "aliados" : "enemigos";
            Debug.LogWarning($"[TARGET_FIND] ✗ {user.CharacterName}: No se encontraron {targetType} en rango para {abilityToUse.DisplayName} (Rango máximo: {abilityToUse.Range}m, candidatos revisados: {candidateCount})");
        }

        return closestTarget;
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
            
            // Actualizar la cámara al personaje seleccionado manualmente
            if (selectedActor != null && GameManager.Instance != null)
            {
                try
                {
                    GameManager.Instance.UpdateCameraToActor(selectedActor);
                    Debug.Log($"[CAMERA] Cámara actualizada a {selectedActor.CharacterName} por selección manual");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[CAMERA] Error al actualizar cámara a {selectedActor.CharacterName}: {e.Message}");
                }
            }
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

    // Flags para manejar la restauración de APs globales
    private static bool hasRestoredAPThisGlobalTurn = false;
    private static Team lastAPGlobalTurnTeam = Team.Enemy;
    
    // MANEJADORES DE EVENTOS DEL TURNSYSTEM
    private void HandleTurnStart(Team team, CharacterActor actor)
    {
        if (team == Team.Player)
        {
            _playerActors = turnSystem.PlayerTeamActors; 
            
            // IMPORTANTE: Solo actualizar _current si el actor no es null
            // Si actor es null, significa que es el inicio de fase y NO debemos cambiar _current
            // Esto previene que la cámara se actualice automáticamente a Cualli después de acciones
            if (actor != null)
            {
                _current = actor;
            }
            // Si actor es null, mantener el _current actual (el personaje que el usuario seleccionó)
            
            _cachedOpponents = turnSystem.GetOpponentsOf(Team.Player).Where(o => !o.Health.IsDead).ToList();
            _cursor = -1;
            // NO limpiar _currentTarget aquí para mantener la selección del usuario
            // _currentTarget = null; // COMENTADO: Mantener el target seleccionado
            
            Debug.Log($"[vAP_FIX_FINAL] Player Turn Handler: Phase started. Active actor: {(_current == null ? "None" : _current.CharacterName)}, Event actor: {(actor == null ? "None" : actor.CharacterName)}");
            
            // LÓGICA DE RESTAURACIÓN DE APs Y POSICIÓN INICIAL GLOBALES
            // Si cambió el turno global (de Enemy a Player), resetear las flags
            if (team != lastAPGlobalTurnTeam)
            {
                hasRestoredAPThisGlobalTurn = false;
                lastAPGlobalTurnTeam = team;
                Debug.Log($"[AP] PlayerTurnController: Nuevo turno global de jugadores detectado. Restaurando APs y estableciendo posición inicial para todos los jugadores.");
                
                // Restaurar APs para todos los jugadores vivos
                RestoreAPsForAllPlayers();
                
                // Establecer startPositionOfTurn para todos los jugadores vivos (una vez por turno global)
                SetStartPositionForAllPlayers();
            }
        }
    }
    
    /// <summary>
    /// Restaura los Action Points para todos los jugadores vivos (suma directa, sin límite)
    /// </summary>
    private void RestoreAPsForAllPlayers()
    {
        if (_playerActors == null) return;
        
        foreach (var player in _playerActors)
        {
            if (player == null || player.Health.IsDead) continue;
            
            // Obtener baseAPPerTurn desde CharacterStats
            int baseAPPerTurn = 1; // Valor por defecto
            if (player.Stats != null)
            {
                baseAPPerTurn = player.Stats.actionPointsPerTurn;
            }
            
            // Suma directa de APs (sin límite de maxAccumulatedAP)
            int currentAP = player.ActionPoints;
            int newAP = currentAP + baseAPPerTurn;
            
            // Verificar si está noqueado
            bool isKnockedOut = player.activeEffects.Any(e => e.Type == StatusEffectType.Noqueado);
            if (isKnockedOut)
            {
                newAP = 0;
            }
            
            // Asegurar que no sea negativo
            newAP = Mathf.Max(0, newAP);
            
            // Establecer los nuevos APs
            player.SetActionPoints(newAP);
            
            Debug.Log($"[AP] PlayerTurnController: {player.CharacterName} - AP restaurados directamente. AP: {currentAP} -> {newAP} (baseAPPerTurn: {baseAPPerTurn})");
        }
        
        hasRestoredAPThisGlobalTurn = true;
    }
    
    /// <summary>
    /// Establece la posición inicial del turno (startPositionOfTurn) para todos los jugadores vivos
    /// </summary>
    private void SetStartPositionForAllPlayers()
    {
        if (_playerActors == null) return;
        
        foreach (var player in _playerActors)
        {
            if (player == null || player.Health.IsDead) continue;
            
            // Obtener el componente TacticalMovementController del jugador
            TacticalMovementController movementController = player.GetComponent<TacticalMovementController>();
            if (movementController != null)
            {
                // Establecer la posición inicial del turno para este jugador
                movementController.SetStartPositionOfTurn(player.transform.position);
                Debug.Log($"[MOVEMENT] PlayerTurnController: {player.CharacterName} - startPositionOfTurn establecido globalmente: {player.transform.position}");
            }
            else
            {
                Debug.LogWarning($"[MOVEMENT] PlayerTurnController: {player.CharacterName} no tiene TacticalMovementController.");
            }
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
            
            // Si cambió el turno a Enemy, resetear las flags de AP
            if (team == Team.Enemy)
            {
                hasRestoredAPThisGlobalTurn = false;
                lastAPGlobalTurnTeam = Team.Enemy;
                Debug.Log($"[AP] PlayerTurnController: Turno cambió a Enemy. Reset de flags de AP.");
            }
        }
    }

    public void TurnButton() => turnSystem.EndTurn(true);

} 