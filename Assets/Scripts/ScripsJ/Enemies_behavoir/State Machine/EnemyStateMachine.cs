using UnityEngine;

public class EnemyStateMachine
{
    public EnemyState currentState { get; private set; }
    private Enemys enemy; // Referencia al enemigo para logs

    public EnemyStateMachine(Enemys enemyRef = null)
    {
        enemy = enemyRef;
    }

    public void Initialize(EnemyState startingState)
    {
        currentState = startingState;
        string enemyName = enemy != null ? enemy.gameObject.name : "Unknown";
        string stateName = startingState != null ? startingState.GetType().Name : "Null";
        Debug.Log($"[ENEMY_STATE_MACHINE] {enemyName}: Inicializando máquina de estados con estado: {stateName}");
        currentState.EnterState();
    }

    public void ChangeState(EnemyState newState)
    {
        if (currentState == newState)
        {
            Debug.LogWarning($"[ENEMY_STATE_MACHINE] {GetEnemyName()}: Intento de cambiar al mismo estado ({newState?.GetType().Name}). Ignorando.");
            return;
        }

        string oldStateName = currentState != null ? currentState.GetType().Name : "Null";
        string newStateName = newState != null ? newState.GetType().Name : "Null";
        
        Debug.Log($"[ENEMY_STATE_MACHINE] {GetEnemyName()}: Cambiando de estado [{oldStateName}] -> [{newStateName}]");
        
        if (currentState != null)
        {
            currentState.ExitState();
        }
        
        currentState = newState;
        
        if (currentState != null)
        {
            currentState.EnterState();
        }
        else
        {
            Debug.LogError($"[ENEMY_STATE_MACHINE] {GetEnemyName()}: ERROR - Nuevo estado es null!");
        }
    }

    private string GetEnemyName()
    {
        return enemy != null ? enemy.gameObject.name : "Unknown";
    }
}
