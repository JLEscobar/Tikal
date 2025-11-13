using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ProgressionManager : MonoBehaviour
{
    public static ProgressionManager Instance { get; private set; }

    [Header("Level 1 Rewards (Fixed in ratios)")]
    [Tooltip("XP fija que gana cada personaje sobreviviente al cumplir el objetivo principal.")]
    [SerializeField] private int fixedXPPerCharacter = 50; 

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // MÉTODO CLAVE: Llamado por GameManager al final de la batalla (solo si el jugador gana)
    public void GrantLevelCompletionRewards(List<CharacterActor> playerTeam)
    {
        Debug.Log($"[Progreso] Granting fixed rewards for level completion to {playerTeam.Count} actors.");
        
        // 1. Recompensa Fija de Experiencia (XP por Objetivo Principal)
        foreach (var actor in playerTeam)
        {
            // Solo se recompensa a los personajes que sobrevivieron
            if (actor != null && !actor.Health.IsDead)
            {
                actor.GrantExperience(fixedXPPerCharacter);
            }
        }

        // 2. Desbloqueo de Historia (Transición Narrativa)
        if (MessagesSystem.Instance != null)
        {
            MessagesSystem.Instance.ShowMessage("¡Objetivo Principal Cumplido! Historia desbloqueada.", Color.magenta);
        }
        
        // 3. Transicionar a la siguiente escena (damos un pequeño tiempo para ver los mensajes)
        Invoke(nameof(ProceedToNextLevel), 1.5f);
    }
    
    private void ProceedToNextLevel()
    {
        // Esto llama al método de transición que ya está en GameManager.cs
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GoToNextLevel();
        }
    }
    
    // Método para XP de Objetivos Secundarios (Para uso futuro en Campamento/Secretos)
    public void GrantObjectiveXP(CharacterActor actor, int amount)
    {
        if (actor != null)
        {
            actor.GrantExperience(amount);
            if (MessagesSystem.Instance != null)
            {
                MessagesSystem.Instance.ShowMessage($"+{amount} XP ({actor.CharacterName}) por Objetivo Secundario.", Color.cyan);
            }
        }
    }
}