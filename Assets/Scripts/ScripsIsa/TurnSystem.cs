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

    private int _playerIndex; // Se mantiene por si alguna lógica lo usa, pero no para iterar.
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
        _currentTeam = Team.Player;
        _playerIndex = _enemyIndex = 0;
        Debug.Log("[v2] Battle started!");
        
        // Empezamos directamente la fase de selección
        StartPlayerSelectionPhase();
    }

    public void Pause() => PauseService.SetPaused(true);
    public void Resume() => PauseService.SetPaused(false);

    public void EndTurn()
    {
        if (!_started) return;

        // Si no hay un actor actual (estamos en fase de selección), el EndTurn es forzar el fin de la fase del equipo
        if (_current == null && _currentTeam == Team.Player)
        {
            Debug.Log("[v2] Forced end of Player Selection Phase. Switching to Enemy team.");
            SwitchTeam();
            NextTurn();
            return;
        }
        
        // Comportamiento normal: terminar el turno del actor actual
        if (_current != null)
        {
            _current.EndTurn();
            OnTurnEnded?.Invoke(_currentTeam, _current);
            Debug.Log($"[v2] Turn ended for {_currentTeam}: {_current.CharacterName}");
        }

        if (CheckBattleEnd())
        {
            return;
        }

        // Si es el turno del jugador, volvemos a la fase de selección para elegir otro personaje.
        if (_currentTeam == Team.Player)
        {
            _current = null;
            
            // Si nadie tiene AP disponible, pasamos al enemigo.
            if (!playerTeam.Any(a => !a.Health.IsDead && a.ActionPoints > 0))
            {
                SwitchTeam();
                NextTurn();
            }
            else
            {
                // Volvemos a la fase de selección
                StartPlayerSelectionPhase();
            }
        }
        else // Si es el turno del enemigo, se avanza automáticamente
        {
            SwitchTeam();
            NextTurn();
        }
    }
    
    // MÉTODO CLAVE: Permite al PlayerTurnController activar un personaje.
    public bool SetCurrentActor(CharacterActor actor)
    {
        if (_currentTeam != Team.Player || actor.Team != Team.Player || actor.Health.IsDead || actor.ActionPoints <= 0)
        {
            return false;
        }
        
        // Finaliza el turno del actor anterior si estaba activo.
        if (_current != null && _current != actor)
        {
            _current.EndTurn();
            OnTurnEnded?.Invoke(_currentTeam, _current);
        }
        
        // Si ya es el mismo actor, no hacemos nada más que retornar true
        if (_current == actor) return true;

        // Activa el nuevo actor
        _current = actor;
        _current.BeginTurn();
        OnTurnStarted?.Invoke(_currentTeam, _current);
        MessagesSystem.Instance.ShowMessage($"Turno de {_current.CharacterName} Aliado (AP: {_current.ActionPoints})", Color.green);
        
        return true;
    }

    // NUEVO MÉTODO: Inicia la fase de selección del jugador.
    private void StartPlayerSelectionPhase()
    {
        _currentTeam = Team.Player;
        _current = null; // Actor nulo -> estamos en fase de selección
        
        // Dispara el evento con actor null. Los listeners sabrán que es fase de selección.
        OnTurnStarted?.Invoke(_currentTeam, null); 
        MessagesSystem.Instance.ShowMessage("Fase de Selección del Jugador: Elige un personaje (1, 2, 3 o 4)", Color.yellow);
        Debug.Log("[v2] Player Selection Phase Started.");
    }


    private void NextTurn()
    {
        // Si el equipo actual es el jugador, volvemos a la fase de selección
        if (_currentTeam == Team.Player)
        {
            StartPlayerSelectionPhase();
            return;
        }
        
        // Si es el enemigo, se sigue el comportamiento automático de tu código original
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
        // ... (El resto se mantiene igual)
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

        if (team == Team.Enemy) // Solo iteramos automáticamente al enemigo
        {
            if (_enemyIndex >= list.Count) _enemyIndex = 0;
            return list[_enemyIndex++];
        }
        
        // Para el jugador, devolvemos nulo en el flujo automático
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
    
    // PROPIEDAD CLAVE: Exponer la lista del equipo del jugador para la selección
    public IReadOnlyList<CharacterActor> PlayerTeamActors => playerTeam;

    public CharacterActor CurrentActor => _current;
    public Team CurrentTeam => _currentTeam;
}