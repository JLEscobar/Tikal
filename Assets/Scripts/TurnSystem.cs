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
        _currentTeam = Team.Player;
        _playerIndex = _enemyIndex = 0;
        Debug.Log("[v0] Battle started!");
        NextTurn();
    }

    public void Pause() => PauseService.SetPaused(true);
    public void Resume() => PauseService.SetPaused(false);

    public void EndTurn()
    {
        if (!_started || _current == null) return;
        _current.EndTurn();
        OnTurnEnded?.Invoke(_currentTeam, _current);
        Debug.Log($"[v0] Turn ended for {_currentTeam}: {_current.CharacterName}");

        if (CheckBattleEnd())
        {
            return;
        }

        SwitchTeam();
        NextTurn();
    }

    private void NextTurn()
    {
        _current = GetNextActor(_currentTeam);
        if (_current == null)
        {
            Debug.Log("[v0] Battle finished! No more actors.");
            _started = false;
            CheckBattleEnd();
            return;
        }

        _current.BeginTurn();
        OnTurnStarted?.Invoke(_currentTeam, _current);
        
        Debug.Log($"[v0] Turn started for {_currentTeam}: {_current.CharacterName}");
        if (_currentTeam == Team.Enemy)
        {
            MessagesSystem.Instance.ShowMessage($"Turno del {_current.CharacterName} Enemigo.", Color.red);
        }
        else
        {
            MessagesSystem.Instance.ShowMessage($"Turno del {_current.CharacterName} Aliado.", Color.green);
        }
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
            Debug.Log($"[v0] Battle ended! Winner: {winner}");
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

        if (team == Team.Player)
        {
            if (_playerIndex >= list.Count) _playerIndex = 0;
            return list[_playerIndex++];
        }
        else
        {
            if (_enemyIndex >= list.Count) _enemyIndex = 0;
            return list[_enemyIndex++];
        }
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

    public CharacterActor CurrentActor => _current;
    public Team CurrentTeam => _currentTeam;
}
