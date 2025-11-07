using System.Collections;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

public class SimpleAIController : MonoBehaviour
{
    [SerializeField] private TurnSystem turnSystem;
    [SerializeField] private float thinkDelay = 1f;
    [SerializeField] private float moveWaitTime = 2f; 
    
    [Header("AI Combat Settings")]
    [Tooltip("Índices de las habilidades que la IA puede usar (0 = Básico, 1 = Especial, etc.)")]
    [SerializeField] private List<int> availableAbilityIndices = new List<int> { 0, 1 }; 
    [Tooltip("Probabilidad (0 a 1) de que la IA intente usar una habilidad de debuff/especial antes que el ataque básico.")]
    [SerializeField, Range(0f, 1f)] private float chanceToUseSpecial = 0.5f; 

    void OnEnable()
    {
        if (turnSystem == null) turnSystem = FindFirstObjectByType<TurnSystem>();
        turnSystem.OnTurnStarted += HandleTurnStart;
    }

    void OnDisable()
    {
        if (turnSystem == null) return;
        turnSystem.OnTurnStarted -= HandleTurnStart;
    }

    private void HandleTurnStart(Team team, CharacterActor actor)
    {
        if (team != Team.Enemy) return;
        StartCoroutine(DoEnemyTurn(actor));
    }

    private IEnumerator DoEnemyTurn(CharacterActor actor)
    {
        if (actor == null) yield break;

        yield return new WaitForSeconds(thinkDelay);

        var targets = turnSystem.GetOpponentsOf(Team.Enemy).Where(o => !o.Health.IsDead).ToList();
        if (targets.Count == 0)
        {
            turnSystem.EndTurn();
            yield break;
        }

        var target = targets.OrderBy(t => Vector3.Distance(actor.transform.position, t.transform.position)).First();

        // -----------------------------------------------------------
        // 1. LÓGICA DE SELECCIÓN DE HABILIDAD
        // -----------------------------------------------------------
        AbilityBase bestAbility = null;
        int abilityIndexToUse = -1;
        
        // Obtenemos todas las habilidades disponibles y que no están en cooldown
        var availableAbilities = availableAbilityIndices
            .Select(i => new { Ability = actor.GetAbilityByIndex(i), Index = i })
            .Where(x => x.Ability != null && x.Ability.currentCooldown <= 0)
            .ToList();
            
        // Si hay habilidades disponibles, decidimos si usar una especial
        if (availableAbilities.Count > 0)
        {
            // Intentar usar la especial si existe y la suerte lo permite
            if (Random.value < chanceToUseSpecial && availableAbilities.Count > 1)
            {
                // Elegimos una habilidad aleatoria que no sea el índice 0 (ataque básico)
                var nonBasicAbilities = availableAbilities.Where(x => x.Index != 0).ToList();
                if (nonBasicAbilities.Count > 0)
                {
                    var selected = nonBasicAbilities[Random.Range(0, nonBasicAbilities.Count)];
                    bestAbility = selected.Ability;
                    abilityIndexToUse = selected.Index;
                }
            }

            // Si no elegimos una especial, usamos la básica (Índice 0)
            if (bestAbility == null)
            {
                var basic = availableAbilities.FirstOrDefault(x => x.Index == 0);
                if (basic != null)
                {
                    bestAbility = basic.Ability;
                    abilityIndexToUse = basic.Index;
                }
            }
        }

        if (bestAbility == null)
        {
            Debug.Log("[vAI_Fix] AI has no valid attack ability");
            turnSystem.EndTurn();
            yield break;
        }
        
        // -----------------------------------------------------------
        // 2. LÓGICA DE MOVIMIENTO (Aproximación)
        // -----------------------------------------------------------
        float distanceToTarget = Vector3.Distance(actor.transform.position, target.transform.position);
        float attackRange = bestAbility.Range + 0.1f; // Usar el margen de error

        if (!bestAbility.CanExecute(actor, target))
        {
            // Intentamos movernos si estamos fuera de rango
            if (distanceToTarget > attackRange)
            {
                Vector3 directionToTarget = (target.transform.position - actor.transform.position).normalized;
                
                float desiredDistanceToMove = distanceToTarget - attackRange + 0.1f; 
                float moveDistance = Mathf.Min(actor.MovementRange, desiredDistanceToMove);
                
                Vector3 desiredPosition = actor.transform.position + directionToTarget * moveDistance;

                // Ahora, llamamos a actor.MoveTo que existe en CharacterActor.cs
                if (actor.CanMoveTo(desiredPosition)) 
                {
                    actor.MoveTo(desiredPosition);

                    yield return new WaitForSeconds(moveWaitTime);
                }
            }
        }

        // -----------------------------------------------------------
        // 3. EJECUCIÓN DE HABILIDAD
        // -----------------------------------------------------------
        if (bestAbility.CanExecute(actor, target))
        {
            actor.TryUseAbility(abilityIndexToUse, target);
            yield return new WaitForSeconds(0.5f);
        }
        
        turnSystem.EndTurn();
    }
}