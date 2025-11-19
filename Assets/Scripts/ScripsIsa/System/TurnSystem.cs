using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TurnSystem : MonoBehaviour
{
    [Header("Teams")]
    [SerializeField] private List<CharacterActor> playerTeam = new();
    [SerializeField] private List<CharacterActor> enemyTeam = new();

    [Header("Settings")]
    [SerializeField] private bool autoStartOnPlay = true;

    [Header("Player Phase Options")]
    [SerializeField] private bool autoSelectInitialPlayer = true;
    [SerializeField][Min(0)] private int initialPlayerIndex = 0;

    public event Action<Team, CharacterActor> OnTurnStarted;
    public event Action<Team, CharacterActor> OnTurnEnded;
    public event Action<Team> OnBattleEnded;

    private int _playerIndex; 
    private int _enemyIndex;
    private Team _currentTeam;
    private CharacterActor _current;
    private bool _started;
    private bool _initialSelectionDone;
    private readonly HashSet<CharacterActor> _playersActivatedThisPhase = new();

    void Start()
    {
        if (autoStartOnPlay) StartBattle();
    }

    public void StartBattle()
    {
        if (_started) return;
        CleanDeadFromLists();
        _started = true;
        _initialSelectionDone = false;
        _playerIndex = _enemyIndex = 0;
        Debug.Log($"[vDoT_FINAL] Battle started! Player count: {playerTeam.Count}, Enemy count: {enemyTeam.Count}");
        
        // Log de enemigos para debug
        foreach(var enemy in enemyTeam)
        {
            if (enemy != null)
            {
                Debug.Log($"[TURN_SYSTEM] Enemy in list: {enemy.CharacterName}");
            }
        }
        
        // Preparar la fase de selección del jugador
        _currentTeam = Team.Player;
        _playersActivatedThisPhase.Clear();
        
        // Usar coroutine para asegurar que todos los sistemas estén inicializados
        StartCoroutine(InitializePlayerSelection());
    }
    
    private IEnumerator InitializePlayerSelection()
    {
        // Esperar varios frames para asegurar que todos los OnEnable/Start se hayan ejecutado
        yield return null;
        yield return null;
        yield return null; // Esperar un frame adicional para asegurar que PlayerTurnController esté completamente inicializado
        
        // Verificar que PlayerTurnController esté suscrito
        var playerController = FindFirstObjectByType<PlayerTurnController>();
        if (playerController == null)
        {
            Debug.LogWarning("[TURN_SYSTEM] PlayerTurnController not found! Waiting another frame...");
            yield return null;
        }
        
        // Intentar selección automática primero
        if (autoSelectInitialPlayer)
        {
            var candidate = GetInitialPlayerActor();
            if (candidate != null)
            {
                Debug.Log($"[TURN_SYSTEM] Attempting to auto-select: {candidate.CharacterName}");
                
                // Verificar condiciones antes de seleccionar
                if (_currentTeam != Team.Player)
                {
                    Debug.LogWarning($"[TURN_SYSTEM] _currentTeam is {_currentTeam}, not Team.Player. Setting to Player...");
                    _currentTeam = Team.Player;
                }
                
                if (SetCurrentActor(candidate))
                {
                    _initialSelectionDone = true;
                    Debug.Log($"[TURN_SYSTEM] ✓ Auto-selected initial player: {candidate.CharacterName}. Current actor is now: {(_current != null ? _current.CharacterName : "null")}");
                    
                    // Esperar un frame más para asegurar que el evento se procese
                    yield return null;
                    
                    // Verificar que el actor sigue siendo el correcto
                    if (_current == candidate)
                    {
                        Debug.Log($"[TURN_SYSTEM] ✓ Verification: Current actor is still {_current.CharacterName}");
                    }
                    else
                    {
                        Debug.LogWarning($"[TURN_SYSTEM] ⚠ Warning: Current actor changed! Expected {candidate.CharacterName}, got {(_current == null ? "null" : _current.CharacterName)}");
                    }
                    
                    yield break; // Salir temprano si la selección automática fue exitosa
                }
                else
                {
                    Debug.LogWarning($"[TURN_SYSTEM] ✗ Failed to auto-select {candidate.CharacterName}. SetCurrentActor returned false.");
                }
            }
            else
            {
                Debug.LogWarning("[TURN_SYSTEM] ✗ No candidate found for auto-selection.");
            }
        }
        
        // Si no hay selección automática o falló, iniciar fase de selección normal
        Debug.Log("[TURN_SYSTEM] Starting normal player selection phase (no auto-selection).");
        StartPlayerSelectionPhase(true);
    }

    public void Pause() => PauseService.SetPaused(true);
    public void Resume() => PauseService.SetPaused(false);

    public void EndTurn(bool forcePlayerPhaseEnd = false)
    {
        if (!_started) return;

        Debug.Log($"[TURN_SYSTEM] EndTurn called. forcePlayerPhaseEnd: {forcePlayerPhaseEnd}, CurrentTeam: {_currentTeam}, CurrentActor: {(_current != null ? _current.CharacterName : "null")}");

        // 1. Finalizar turno del actor actual (si existe)
        if (_current != null && _current.gameObject != null)
        {
            TickCooldowns(_current); 
            TickStatusEffects(_current); 
            _current.ApplyTurnDamageEffects(); // Llama a la lógica de Quemado/Envenenado para el actor actual
            
            _current.EndTurn();
            OnTurnEnded?.Invoke(_currentTeam, _current);
            Debug.Log($"[vDoT_FINAL] Turn ended for {_current.CharacterName}.");
        }
        else if (_current != null && _current.gameObject == null)
        {
             CleanDeadFromLists();
        }

        if (CheckBattleEnd())
        {
            return;
        }

        if (_currentTeam == Team.Player)
        {
            // Si estamos forzando el fin de la fase del jugador, no importa si hay más jugadores con AP
            if (forcePlayerPhaseEnd)
            {
                Debug.Log("[TURN_SYSTEM] Force ending player phase. Switching to Enemy team.");
                _current = null;
                
                // Tick a todo el equipo enemigo 
                foreach(var enemy in enemyTeam.Where(e => e != null)) 
                {
                    TickCooldowns(enemy); 
                    TickStatusEffects(enemy); 
                    enemy.ApplyTurnDamageEffects();
                }
                
                _enemyIndex = 0;
                _currentTeam = Team.Enemy;
                NextTurn();
                return;
            }

            // Si no estamos forzando, verificar si hay más jugadores con AP
            _current = null;
            
            // Tick a todo el equipo enemigo 
            foreach(var enemy in enemyTeam.Where(e => e != null)) 
            {
                TickCooldowns(enemy); 
                TickStatusEffects(enemy); 
                enemy.ApplyTurnDamageEffects();
            }
            
            bool playersHaveActions = playerTeam.Any(a => a != null && !a.Health.IsDead && a.ActionPoints > 0);

            if (playersHaveActions)
            {
                Debug.Log("[TURN_SYSTEM] More players have actions. Returning to player selection phase.");
                StartPlayerSelectionPhase(false);
                return;
            }

            Debug.Log("[TURN_SYSTEM] No more players with actions. Switching to Enemy team.");
            _enemyIndex = 0;
            _currentTeam = Team.Enemy;
            NextTurn();
        }
        else 
        {
            // Tick a todo el equipo jugador
            foreach(var player in playerTeam.Where(p => p != null)) 
            {
                TickCooldowns(player); 
                TickStatusEffects(player); 
                player.ApplyTurnDamageEffects();
            }
            
            NextTurn();
        }
    }
    
    // MÉTODO SetCurrentActor (se mantiene)
    public bool SetCurrentActor(CharacterActor actor)
    {
        if (actor == null)
        {
            Debug.LogWarning("[TURN_SYSTEM] SetCurrentActor llamado con actor nulo. Ignorando.");
            return false;
        }
        
        if (_currentTeam != Team.Player)
        {
            Debug.LogWarning($"[TURN_SYSTEM] SetCurrentActor: _currentTeam ({_currentTeam}) != Team.Player. Cannot select {actor.CharacterName}.");
            return false;
        }
        
        if (actor.Team != Team.Player)
        {
            Debug.LogWarning($"[TURN_SYSTEM] SetCurrentActor: {actor.CharacterName}.Team ({actor.Team}) != Team.Player. Cannot select.");
            return false;
        }
        
        if (actor.Health.IsDead)
        {
            Debug.LogWarning($"[TURN_SYSTEM] SetCurrentActor: {actor.CharacterName} is dead. Cannot select.");
            return false;
        }

        bool actorAlreadyCurrent = _current == actor;
        bool actorAlreadyActivated = _playersActivatedThisPhase.Contains(actor);
        bool isObservationSelection = actorAlreadyActivated;
        
        if (_current != null && _current.gameObject != null && _current != actor) 
        {
            _current.EndTurn();
            OnTurnEnded?.Invoke(_currentTeam, _current);
        }
        
        if (actorAlreadyCurrent) return true;

        _current = actor;
        
        if (!isObservationSelection)
        {
            actor.BeginTurn();
            _playersActivatedThisPhase.Add(actor);
        }

        OnTurnStarted?.Invoke(_currentTeam, actor);

        if (!isObservationSelection && actor.ActionPoints > 0)
        {
            MessagesSystem.Instance.ShowMessage($"Turno de {actor.CharacterName} Aliado (AP: {actor.ActionPoints})", Color.green);
        }
        else if (!isObservationSelection)
        {
            MessagesSystem.Instance.ShowMessage($"{actor.CharacterName} no puede actuar este turno.", Color.cyan);
            actor.ForceMovementPhaseActivation(); 
        }
        else
        {
            MessagesSystem.Instance.ShowMessage($"{actor.CharacterName} ya actuó. Solo visualización.", Color.grey);
        }
        
        return true;
    }

    private void StartPlayerSelectionPhase(bool resetActivatedPlayers)
    {
        if (resetActivatedPlayers)
        {
            _playersActivatedThisPhase.Clear();
        }
        _currentTeam = Team.Player;
        _current = null; 
        OnTurnStarted?.Invoke(_currentTeam, null); 
        MessagesSystem.Instance.ShowMessage("Fase de Selección del Jugador: Elige un personaje (1, 2, 3 o 4)", Color.yellow);
        Debug.Log("[vDoT_FINAL] Player Selection Phase Started.");
    }

    private void NextTurn()
    {
        if (_currentTeam == Team.Player)
        {
            StartPlayerSelectionPhase(true);
            return;
        }
        
        CleanDeadFromList(enemyTeam);
        Debug.Log($"[TURN_SYSTEM] Getting next enemy. Enemy count: {enemyTeam.Count}, Enemy index: {_enemyIndex}");
        
        _current = GetNextActor(_currentTeam);
        if (_current == null)
        {
            Debug.Log("[TURN_SYSTEM] No more enemies. Switching back to Player team.");
            SwitchTeam();
            NextTurn(); 
            return;
        }

        Debug.Log($"[TURN_SYSTEM] Enemy turn: {_current.CharacterName}");
        _current.BeginTurn();
        OnTurnStarted?.Invoke(_currentTeam, _current);
        MessagesSystem.Instance.ShowMessage($"Turno del {_current.CharacterName} Enemigo.", Color.red);
        
    }

    private bool CheckBattleEnd()
    {
        CleanDeadFromLists();
        bool playersDead = playerTeam.Count == 0;
        bool enemiesDead = enemyTeam.Count == 0;

        if (playersDead || enemiesDead)
        {
            _started = false;
            Team winner = enemiesDead ? Team.Player : Team.Enemy;
            OnBattleEnded?.Invoke(winner);
            return true;
        }
        return false;
    }

    private void SwitchTeam()
    {
        _currentTeam = _currentTeam == Team.Player ? Team.Enemy : Team.Player;
    }

    private CharacterActor GetNextActor(Team team)
    {
        var list = team == Team.Player ? playerTeam : enemyTeam;
        CleanDeadFromList(list);
        if (list.Count == 0) return null;

        if (team == Team.Enemy) 
        {
            if (_enemyIndex >= list.Count)
            {
                _enemyIndex = 0;
                return null;
            }
            return list[_enemyIndex++];
        }
        
        return null; 
    }

    private void CleanDeadFromLists()
    {
        CleanDeadFromList(playerTeam);
        CleanDeadFromList(enemyTeam);
    }

    private void CleanDeadFromList(List<CharacterActor> list)
    {
        list.RemoveAll(a => a == null || (a.gameObject != null && a.Health.IsDead) || a.gameObject == null || !a.gameObject.activeInHierarchy);
        list.RemoveAll(a => a == null); 
    }

    public IEnumerable<CharacterActor> GetOpponentsOf(Team team)
    {
        return team == Team.Player ? enemyTeam : playerTeam;
    }
    
    public IReadOnlyList<CharacterActor> PlayerTeamActors => playerTeam;

    public CharacterActor CurrentActor => _current;
    public Team CurrentTeam => _currentTeam;
    
    public void AddEnemyActor(CharacterActor newEnemy)
    {
        if (newEnemy == null || newEnemy.Team != Team.Enemy) return;
        
        if (!enemyTeam.Contains(newEnemy))
        {
            enemyTeam.Add(newEnemy);
            Debug.Log($"[vDoT_FINAL] New enemy added to the battle: {newEnemy.CharacterName}");
        }
    }
    
    private void TickStatusEffects(CharacterActor actor)
    {
        if (actor == null || actor.Health.IsDead) return;

        foreach (var effect in actor.activeEffects)
        {
            effect.Duration--;
        }
        
        actor.RemoveExpiredEffects();
    }

    private void TickCooldowns(CharacterActor actor)
    {
        if (actor.Stats == null || actor.Stats.abilities == null) return;

        foreach (var ability in actor.Stats.abilities)
        {
            if (ability != null && ability.BaseCooldownTurns > 0 && ability.currentCooldown > 0) 
            {
                ability.currentCooldown--;
                Debug.Log($"[CD TICK] {actor.CharacterName}: {ability.DisplayName} restante: {ability.currentCooldown}");
            }
        }
    }

    private void TryAutoSelectInitialPlayer()
    {
        if (_initialSelectionDone || !autoSelectInitialPlayer) return;

        var candidate = GetInitialPlayerActor();
        if (candidate == null) return;

        if (SetCurrentActor(candidate))
        {
            _initialSelectionDone = true;
        }
    }

    private CharacterActor GetInitialPlayerActor()
    {
        if (playerTeam == null || playerTeam.Count == 0) return null;

        CleanDeadFromList(playerTeam);

        // Prioridad 1: Buscar a Cualli por nombre (personaje número 1)
        var cualli = playerTeam.FirstOrDefault(a => 
            a != null && 
            a.gameObject != null && 
            !a.Health.IsDead && 
            a.CharacterName.Equals("Cualli", System.StringComparison.OrdinalIgnoreCase));
        
        if (cualli != null)
        {
            return cualli;
        }

        // Prioridad 2: Usar el índice preferido si está configurado
        if (initialPlayerIndex >= 0 && initialPlayerIndex < playerTeam.Count)
        {
            var preferred = playerTeam[initialPlayerIndex];
            if (preferred != null && preferred.gameObject != null && !preferred.Health.IsDead)
            {
                return preferred;
            }
        }

        // Prioridad 3: Primer personaje disponible
        return playerTeam.FirstOrDefault(a => a != null && a.gameObject != null && !a.Health.IsDead);
    }
}