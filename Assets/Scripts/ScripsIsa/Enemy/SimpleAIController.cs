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
        Debug.Log($"[ENEMY_AI] Turn started for enemy: {actor?.CharacterName}");
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

        // CORRECCIÓN CLAVE: Elegir un objetivo aleatorio de la lista para distribuir el daño
        var targetIndex = Random.Range(0, targets.Count);
        var target = targets[targetIndex];
        
        Debug.Log($"[vAI_RANDOM] AI selected target: {target.CharacterName} (Randomized)");

        // -----------------------------------------------------------
        // 1. LÓGICA DE SELECCIÓN DE HABILIDAD
        // -----------------------------------------------------------
        AbilityBase bestAbility = null;
        int abilityIndexToUse = -1;
        
        var availableAbilities = availableAbilityIndices
            .Select(i => new { Ability = actor.GetAbilityByIndex(i), Index = i })
            .Where(x => x.Ability != null && x.Ability.currentCooldown <= 0)
            .ToList();
            
        if (availableAbilities.Count > 0)
        {
            if (Random.value < chanceToUseSpecial && availableAbilities.Count > 1)
            {
                var nonBasicAbilities = availableAbilities.Where(x => x.Index != 0).ToList();
                if (nonBasicAbilities.Count > 0)
                {
                    var selected = nonBasicAbilities[Random.Range(0, nonBasicAbilities.Count)];
                    bestAbility = selected.Ability;
                    abilityIndexToUse = selected.Index;
                }
            }

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
            turnSystem.EndTurn();
            yield break;
        }
        
        // -----------------------------------------------------------
        // 2. LÓGICA DE MOVIMIENTO (Aproximación)
        // -----------------------------------------------------------
        float distanceToTarget = Vector3.Distance(actor.transform.position, target.transform.position);
        float attackRange = bestAbility.Range + 0.1f; // Usar el margen de error

        Debug.Log($"[ENEMY_AI] {actor.CharacterName}: Distance to target: {distanceToTarget:F2}, Attack range: {attackRange:F2}");

        if (!bestAbility.CanExecute(actor, target))
        {
            if (distanceToTarget > attackRange)
            {
                Vector3 directionToTarget = (target.transform.position - actor.transform.position).normalized;
                
                float desiredDistanceToMove = distanceToTarget - attackRange + 0.1f; 
                float moveDistance = Mathf.Min(actor.MovementRange, desiredDistanceToMove);
                
                Vector3 desiredPosition = actor.transform.position + directionToTarget * moveDistance;

                Debug.Log($"[ENEMY_AI] {actor.CharacterName}: Need to move closer. Moving {moveDistance:F2} units towards target.");

                if (actor.CanMoveTo(desiredPosition)) 
                {
                    actor.MoveTo(desiredPosition);
                    
                    // Esperar a que el movimiento termine
                    var movement = actor.GetComponent<CharacterMovement>();
                    if (movement != null)
                    {
                        float waitTime = 0f;
                        while (movement.IsMoving && waitTime < moveWaitTime * 2f) // Timeout de seguridad
                        {
                            yield return new WaitForSeconds(0.1f);
                            waitTime += 0.1f;
                        }
                    }
                    else
                    {
                        yield return new WaitForSeconds(moveWaitTime);
                    }
                }
                else
                {
                    Debug.LogWarning($"[ENEMY_AI] {actor.CharacterName}: Cannot move to desired position (out of range).");
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