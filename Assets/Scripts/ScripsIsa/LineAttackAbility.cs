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

    public override bool CanExecute(CharacterActor user, ITargetable target)
    {
        // El target debe ser el objetivo clickeado
        if (user == null || target == null) return false;
        if (user.ActionPoints < CostAP) return false;
        if (target.Health.IsDead) return false;

        // La distancia se basa en el objetivo final (7 metros).
        float distance = Vector3.Distance(user.transform.position, target.GetTransform().position);
        
        return distance <= Range;
    }

    public override void Execute(CharacterActor user, ITargetable target)
    {
        if (!CanExecute(user, target)) return;
        
        Vector3 userPosition = user.transform.position;
        Vector3 targetPosition = target.GetTransform().position;

        // 1. Definir la trayectoria y la distancia
        Vector3 direction = (targetPosition - userPosition).normalized;
        float maxDistance = Range; 

        // 2. Usar Physics.RaycastAll para detectar todos los colliders en la trayectoria.
        RaycastHit[] hits = Physics.RaycastAll(userPosition, direction, maxDistance, targetLayer);

        // 3. Filtrar y aplicar daño
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

        // 4. Consumir AP
        user.ConsumeActionPoints(CostAP);

        // 5. LÓGICA DE PARTÍCULAS: Se instancian en el target clickeado (final de la línea)
        if (AbilityParticles != null)
        {
            // Instancia las partículas en la posición del TARGET clickeado (final de la línea)
            GameObject particles = Instantiate(AbilityParticles, targetPosition, Quaternion.identity);
            Object.Destroy(particles, 3f);
        }

        Debug.Log($"[DESPLAZAMIENTO] {user.CharacterName} usa Desplazamiento Letal, golpeando a {validTargets.Count} targets.");
    }
}