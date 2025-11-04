using System;
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

    public event Action<Team, CharacterActor> OnTurnStarted;
    public event Action<Team, CharacterActor> OnTurnEnded;
    public event Action<Team> OnBattleEnded;

    private int _playerIndex; 
    private int _enemyIndex;
    private Team _currentTeam;
    private CharacterActor _current;
    private bool _started;

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
        Debug.Log("[vFinal] Battle started!");
        
        StartPlayerSelectionPhase();
    }

    public void Pause() => PauseService.SetPaused(true);
    public void Resume() => PauseService.SetPaused(false);

    public void EndTurn()
    {
        if (!_started) return;

        if (_current == null && _currentTeam == Team.Player)
        {
            Debug.Log("[vFinal] Forced end of Player Selection Phase. Switching to Enemy team.");
            SwitchTeam();
            NextTurn();
            return;
        }
        
        // 1. Finalizar turno del actor actual
        if (_current != null)
        {
            TickCooldowns(_current); // NUEVO: Tick del cooldown del actor actual
            _current.EndTurn();
            OnTurnEnded?.Invoke(_currentTeam, _current);
            Debug.Log($"[vFinal] Turn ended for {_currentTeam}: {_current.CharacterName}");
        }

        if (CheckBattleEnd())
        {
            return;
        }

        // 2. Reducir Cooldowns del equipo que va a esperar
        if (_currentTeam == Team.Player)
        {
            _current = null;
            
            // Tick a todo el equipo enemigo al final del turno del jugador
            foreach(var enemy in enemyTeam) TickCooldowns(enemy); // NUEVO: Tick al equipo enemigo
            
            if (!playerTeam.Any(a => !a.Health.IsDead && a.ActionPoints > 0))
            {
                SwitchTeam();
                NextTurn();
            }
            else
            {
                StartPlayerSelectionPhase();
            }
        }
        else 
        {
            // Tick a todo el equipo jugador al final del turno enemigo
            foreach(var player in playerTeam) TickCooldowns(player); // NUEVO: Tick al equipo jugador
            
            SwitchTeam();
            NextTurn();
        }
    }
    
    // MÉTODO MODIFICADO: Contador de cooldowns
    private void TickCooldowns(CharacterActor actor)
    {
        if (actor.Stats == null || actor.Stats.abilities == null) return;

        foreach (var ability in actor.Stats.abilities)
        {
            // Solo si la habilidad tiene un cooldown base y el contador actual es mayor a cero
            if (ability != null && ability.BaseCooldownTurns > 0 && ability.currentCooldown > 0) 
            {
                ability.currentCooldown--;
                Debug.Log($"[CD TICK] {actor.CharacterName}: {ability.DisplayName} restante: {ability.currentCooldown}");
            }
        }
    }
    
    // MÉTODO SetCurrentActor (se mantiene la lógica de re-selección)
    public bool SetCurrentActor(CharacterActor actor)
    {
        if (_currentTeam != Team.Player || actor.Team != Team.Player || actor.Health.IsDead)
        {
            return false;
        }
        
        if (_current != null && _current != actor)
        {
            _current.EndTurn();
            OnTurnEnded?.Invoke(_currentTeam, _current);
        }
        
        if (_current == actor) return true;

        _current = actor;
        
        if (_current.ActionPoints > 0)
        {
            _current.BeginTurn(); 
            OnTurnStarted?.Invoke(_currentTeam, _current);
            MessagesSystem.Instance.ShowMessage($"Turno de {_current.CharacterName} Aliado (AP: {_current.ActionPoints})", Color.green);
        }
        else
        {
            OnTurnStarted?.Invoke(_currentTeam, _current); 
            MessagesSystem.Instance.ShowMessage($"Seleccionado {_current.CharacterName}, pero no le quedan Puntos de Acción.", Color.cyan);
            
            _current.ForceMovementPhaseActivation(); 
        }
        
        return true;
    }

    private void StartPlayerSelectionPhase()
    {
        _currentTeam = Team.Player;
        _current = null; 
        OnTurnStarted?.Invoke(_currentTeam, null); 
        MessagesSystem.Instance.ShowMessage("Fase de Selección del Jugador: Elige un personaje (1, 2, 3 o 4)", Color.yellow);
        Debug.Log("[vFinal] Player Selection Phase Started.");
    }

    private void NextTurn()
    {
        if (_currentTeam == Team.Player)
        {
            StartPlayerSelectionPhase();
            return;
        }
        
        _current = GetNextActor(_currentTeam);
        if (_current == null)
        {
            SwitchTeam();
            NextTurn(); 
            return;
        }

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
            if (_enemyIndex >= list.Count) _enemyIndex = 0;
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
        list.RemoveAll(a => a == null || a.Health.IsDead || !a.gameObject.activeInHierarchy);
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
            Debug.Log($"[vFinal] New enemy added to the battle: {newEnemy.CharacterName}");
        }
    }
}