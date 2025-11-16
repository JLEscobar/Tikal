using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "LineAttack", menuName = "QijTikal/Abilities/Line Attack")]
public class LineAttackAbility : AbilityBase
{
    [Header("Line Damage Settings")]
    [Tooltip("Daño fijo (15 PD para Desplazamiento Letal).")]
    [SerializeField] private int fixedDamage = 15;
    
    [Tooltip("Capa de máscara que contiene a los enemigos (CRUCIAL).")]
    [SerializeField] private LayerMask targetLayer;

    [Header("Teleport Settings (Ollin)")]
    [Tooltip("Si es True, el usuario se mueve a la posición del objetivo después del ataque.")]
    [SerializeField] private bool moveUserToTarget = false;
    [Tooltip("Distancia detrás del objetivo a la que aparecerá Ollin (0.5m por ejemplo).")]
    [SerializeField] private float teleportOffset = 0.5f;


    public override bool CanExecute(CharacterActor user, ITargetable target)
    {
        // El target debe ser el objetivo clickeado
        if (user == null || target == null) return false;
        if (user.ActionPoints < CostAP) return false;
        if (target.Health.IsDead) return false;

        // La distancia se basa en el objetivo final (7 metros).
        float distance = Vector3.Distance(user.transform.position, target.GetTransform().position);
        
        return distance <= Range + 0.1f; 
    }

    public override void Execute(CharacterActor user, ITargetable target)
    {
        if (!CanExecute(user, target)) return;
        
        Vector3 userPosition = user.transform.position;
        Vector3 targetPosition = target.GetTransform().position;

        // 1. Lógica de Raycast y daño
        Vector3 direction = (targetPosition - userPosition).normalized;
        float maxDistance = Range; 

        RaycastHit[] hits = Physics.RaycastAll(userPosition, direction, maxDistance, targetLayer);

        var validTargets = hits
            .Select(hit => hit.collider.GetComponentInParent<CharacterActor>())
            .Where(t => t != null && t.Team != user.Team && !t.Health.IsDead)
            .Distinct()
            .ToList();

        int finalDamage = Mathf.Max(1, fixedDamage);
        
        foreach (var t in validTargets)
        {
            t.Health.TakeDamage(finalDamage);
        }

        // 2. Teleport (para Ollin)
        if (moveUserToTarget && user.CharacterName == "Ollin") // Solo Ollin debería hacer esto
        {
            // Calcula la posición para aparecer detrás del objetivo, simulando el +3 teleport
            Vector3 teleportPosition = targetPosition - direction * teleportOffset; 

            // Esta es la llamada crítica a la nueva función:
            user.ForceTeleportToPosition(teleportPosition); 
            Debug.Log($"{user.CharacterName} se teletransporta a la posición del target.");
        }

        // 3. Consumir AP
        user.ConsumeActionPoints(CostAP);

        if (AbilityParticles != null)
        {
            GameObject particles = Instantiate(AbilityParticles, targetPosition, Quaternion.identity);
            Object.Destroy(particles, 3f);
        }

        Debug.Log($"[DESPLAZAMIENTO] {user.CharacterName} usa Desplazamiento Letal, golpeando a {validTargets.Count} targets.");
    }
}