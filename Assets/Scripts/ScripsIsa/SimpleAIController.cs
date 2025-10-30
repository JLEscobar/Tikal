using System.Collections;
using System.Linq;
using UnityEngine;

public class SimpleAIController : MonoBehaviour
{
    [SerializeField] private TurnSystem turnSystem;
    [SerializeField] private float thinkDelay = 1f;
    [SerializeField] private float moveWaitTime = 2f; // Time to wait for movement to complete

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

        Debug.Log($"[v0] AI turn started for {actor.CharacterName}");

        yield return new WaitForSeconds(thinkDelay);

        var targets = turnSystem.GetOpponentsOf(Team.Enemy).Where(o => !o.Health.IsDead).ToList();
        if (targets.Count == 0)
        {
            Debug.Log("[v0] AI: No targets available, ending turn");
            turnSystem.EndTurn();
            yield break;
        }

        var target = targets.OrderBy(t => Vector3.Distance(actor.transform.position, t.transform.position)).First();
        Debug.Log($"[v0] AI selected target: {target.CharacterName}");

        var attack = actor.GetAbilityByIndex(0);
        if (attack == null)
        {
            Debug.Log("[v0] AI has no attack ability");
            turnSystem.EndTurn();
            yield break;
        }

        float distanceToTarget = Vector3.Distance(actor.transform.position, target.transform.position);
        float attackRange = attack.Range;

        Debug.Log($"[v0] AI distance to target: {distanceToTarget:F2}, attack range: {attackRange:F2}");

        if (!attack.CanExecute(actor, target))
        {
            Debug.Log("[v0] AI cannot attack yet, attempting to move closer");

            // If we're out of attack range, try to move closer
            if (distanceToTarget > attackRange)
            {
                // Calculate direction to target
                Vector3 directionToTarget = (target.transform.position - actor.transform.position).normalized;

                // Calculate desired position (move as close as possible within movement range)
                float moveDistance = Mathf.Min(actor.MovementRange, distanceToTarget - attackRange + 0.5f);
                Vector3 desiredPosition = actor.transform.position + directionToTarget * moveDistance;

                Debug.Log($"[v0] AI calculated move: distance={moveDistance:F2}, from={actor.transform.position}, to={desiredPosition}");

                // Check if we can move to that position
                if (actor.CanMoveTo(desiredPosition))
                {
                    Debug.Log($"[v0] AI moving closer to target");
                    actor.MoveTo(desiredPosition);

                    // Wait for movement to complete
                    Debug.Log($"[v0] AI waiting {moveWaitTime}s for movement to complete");
                    yield return new WaitForSeconds(moveWaitTime);

                    Debug.Log($"[v0] AI movement complete. New position: {actor.transform.position}");
                }
                else
                {
                    Debug.Log($"[v0] AI cannot move to desired position (out of movement range: {actor.MovementRange})");
                }
            }
            else
            {
                Debug.Log($"[v0] AI is within attack range but cannot execute (might be AP issue)");
            }
        }

        if (attack.CanExecute(actor, target))
        {
            Debug.Log($"[v0] AI attacking {target.CharacterName}");
            attack.Execute(actor, target);
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            Debug.Log("[v0] AI still cannot attack (out of range or no AP)");
        }

        Debug.Log("[v0] AI ending turn");
        turnSystem.EndTurn();
    }
}
