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
    [SerializeField] private float enemyTurnDelay = 1f; // Delay entre turnos de enemigos (segundos)
    
    [Header("UI Components")]
    [Tooltip("UI que se activa cuando es turno de los aliados")]
    [SerializeField] private GameObject UITurnoJugador;
    [Tooltip("UI que se activa cuando es turno de los enemigos")]
    [SerializeField] private GameObject UITurnoEnemigo;

    public event Action<Team, CharacterActor> OnTurnStarted;
    public event Action<Team, CharacterActor> OnTurnEnded;
    public event Action<Team> OnBattleEnded;

    private int _playerIndex; 
    private int _enemyIndex;
    private Team _currentTeam;
    private CharacterActor _current;
    private bool _started;
    private bool _isFirstEnemyTurn = true; // Para no aplicar delay en el primer turno de enemigo
    private bool _isProcessingEndTurn = false; // Flag para prevenir múltiples llamadas simultáneas a EndTurn
    private Coroutine _enemyTurnCoroutine = null; // Referencia a la corrutina de delay de enemigos

    void Start()
    {
        if (autoStartOnPlay) StartBattle();
    }

    public void StartBattle()
    {
        if (_started) return;
        CleanDeadFromLists();
        _started = true;
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
        
        // Inicializar UI: desactivar ambos UI al inicio (se activarán según el turno)
        if (UITurnoJugador != null) UITurnoJugador.SetActive(false);
        if (UITurnoEnemigo != null) UITurnoEnemigo.SetActive(false);
        
        StartPlayerSelectionPhase();
    }

    public void Pause() => PauseService.SetPaused(true);
    public void Resume() => PauseService.SetPaused(false);

    public void EndTurn(bool forcePlayerPhaseEnd = false)
    {
        if (!_started) return;
        
        // Protección contra múltiples llamadas simultáneas
        if (_isProcessingEndTurn)
        {
            Debug.LogWarning($"[TURN_SYSTEM] EndTurn ya está siendo procesado. Ignorando llamada duplicada.");
            return;
        }
        
        _isProcessingEndTurn = true;
        
        try
        {
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
                _isFirstEnemyTurn = true; // Marcar que es el primer turno de enemigo en esta ronda
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
                StartPlayerSelectionPhase();
                return;
            }

            Debug.Log("[TURN_SYSTEM] No more players with actions. Switching to Enemy team.");
            _enemyIndex = 0;
            _currentTeam = Team.Enemy;
            _isFirstEnemyTurn = true; // Marcar que es el primer turno de enemigo en esta ronda
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
        finally
        {
            // Siempre resetear la flag al finalizar, incluso si hay errores
            _isProcessingEndTurn = false;
        }
    }
    
    // MÉTODO SetCurrentActor (se mantiene)
    public bool SetCurrentActor(CharacterActor actor)
    {
        if (actor == null)
        {
            Debug.LogWarning("SetCurrentActor llamado con actor nulo. Ignorando.");
            return false;
        }
        
        if (_currentTeam != Team.Player || actor.Team != Team.Player || actor.Health.IsDead)
        {
            return false;
        }
        
        if (_current != null && _current.gameObject != null && _current != actor) 
        {
            _current.EndTurn();
            OnTurnEnded?.Invoke(_currentTeam, _current);
        }
        
        if (_current == actor) return true;

        _current = actor;
        
        actor.BeginTurn();
        OnTurnStarted?.Invoke(_currentTeam, actor);

        if (actor.ActionPoints > 0)
        {
            MessagesSystem.Instance.ShowMessage($"Turno de {actor.CharacterName} Aliado (AP: {actor.ActionPoints})", Color.green);
        }
        else
        {
            MessagesSystem.Instance.ShowMessage($"{actor.CharacterName} no puede actuar este turno.", Color.cyan);
            actor.ForceMovementPhaseActivation(); 
        }
        
        return true;
    }

    private void StartPlayerSelectionPhase()
    {
        _currentTeam = Team.Player;
        
        // Activar/desactivar UI según el turno
        UpdateTurnUI();
        
        // Limpiar jugadores muertos antes de buscar el primero
        CleanDeadFromList(playerTeam);
        
        // IMPORTANTE: Invocar OnTurnStarted con null primero para que todos los jugadores detecten el cambio de turno global
        // Esto permite que HandleTurnStarted restaure los AP antes de buscar el primer jugador
        OnTurnStarted?.Invoke(_currentTeam, null);
        
        // Buscar el primer jugador válido (sin importar sus AP, porque se restaurarán en HandleTurnStarted o BeginTurn)
        CharacterActor firstPlayer = playerTeam.FirstOrDefault(p => p != null && !p.Health.IsDead);
        
        if (firstPlayer != null)
        {
            // Activar automáticamente el turno del primer jugador
            // BeginTurn() y HandleTurnStarted() restaurarán los AP si es necesario
            _current = firstPlayer;
            firstPlayer.BeginTurn();
            OnTurnStarted?.Invoke(_currentTeam, firstPlayer);
            
            if (firstPlayer.ActionPoints > 0)
            {
                MessagesSystem.Instance.ShowMessage($"Turno de {firstPlayer.CharacterName} Aliado (AP: {firstPlayer.ActionPoints})", Color.green);
            }
            else
            {
                MessagesSystem.Instance.ShowMessage($"{firstPlayer.CharacterName} no puede actuar este turno.", Color.cyan);
                firstPlayer.ForceMovementPhaseActivation();
            }
            
            Debug.Log($"[vDoT_FINAL] Player Selection Phase Started. Automatically activated: {firstPlayer.CharacterName}");
        }
        else
        {
            // No hay jugadores válidos, cambiar directamente a enemigos
            Debug.Log("[TURN_SYSTEM] No valid players found. Switching to Enemy team.");
            _current = null;
            _enemyIndex = 0;
            _currentTeam = Team.Enemy;
            _isFirstEnemyTurn = true;
            NextTurn();
        }
    }

    private void NextTurn()
    {
        if (_currentTeam == Team.Player)
        {
            StartPlayerSelectionPhase();
            return;
        }
        
        // Si es turno de enemigo y no es el primer turno, aplicar delay
        if (!_isFirstEnemyTurn)
        {
            // Cancelar corrutina anterior si existe (protección contra múltiples llamadas)
            if (_enemyTurnCoroutine != null)
            {
                StopCoroutine(_enemyTurnCoroutine);
                Debug.LogWarning("[TURN_SYSTEM] Corrutina de delay anterior cancelada.");
            }
            
            _enemyTurnCoroutine = StartCoroutine(NextEnemyTurnWithDelay());
            return;
        }
        
        // Primer turno de enemigo o después del delay: iniciar inmediatamente
        _isFirstEnemyTurn = false;
        StartEnemyTurn();
    }
    
    private System.Collections.IEnumerator NextEnemyTurnWithDelay()
    {
        Debug.Log($"[TURN_SYSTEM] Esperando {enemyTurnDelay} segundos antes del siguiente turno de enemigo...");
        yield return new WaitForSeconds(enemyTurnDelay);
        
        // Verificar que aún estamos en el turno de enemigos antes de iniciar
        // (por si se canceló o cambió el turno durante el delay)
        if (_currentTeam == Team.Enemy)
        {
            StartEnemyTurn();
        }
        else
        {
            Debug.LogWarning($"[TURN_SYSTEM] Corrutina de delay cancelada - el turno cambió a {_currentTeam}");
        }
        
        _enemyTurnCoroutine = null;
    }
    
    private void StartEnemyTurn()
    {
        // Activar/desactivar UI según el turno
        UpdateTurnUI();
        
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
    
    /// <summary>
    /// Actualiza los componentes UI según el equipo actual
    /// </summary>
    private void UpdateTurnUI()
    {
        if (_currentTeam == Team.Player)
        {
            // Activar UI de jugador y desactivar UI de enemigo
            if (UITurnoJugador != null)
            {
                UITurnoJugador.SetActive(true);
                Debug.Log("[TURN_SYSTEM] UITurnoJugador activado");
            }
            if (UITurnoEnemigo != null)
            {
                UITurnoEnemigo.SetActive(false);
                Debug.Log("[TURN_SYSTEM] UITurnoEnemigo desactivado");
            }
        }
        else if (_currentTeam == Team.Enemy)
        {
            // Activar UI de enemigo y desactivar UI de jugador
            if (UITurnoEnemigo != null)
            {
                UITurnoEnemigo.SetActive(true);
                Debug.Log("[TURN_SYSTEM] UITurnoEnemigo activado");
            }
            if (UITurnoJugador != null)
            {
                UITurnoJugador.SetActive(false);
                Debug.Log("[TURN_SYSTEM] UITurnoJugador desactivado");
            }
        }
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
}