using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "AreaAttack", menuName = "QijTikal/Abilities/Area Attack")]
public class AreaAttackAbility : AbilityBase
{
    [Header("Area Damage Settings")]
    [Tooltip("Daño fijo (50 PD para Cualli).")]
    [SerializeField] private int fixedDamage = 50;
    [Tooltip("Capa de máscara que contiene a los enemigos (IMPORTANTE configurar).")]
    [SerializeField] private LayerMask targetLayer;

    public override bool CanExecute(CharacterActor user, ITargetable target)
    {
        if (!base.CanExecute(user, target)) return false;
        return true;
    }

    public override void Execute(CharacterActor user, ITargetable target)
    {
        if (!CanExecute(user, target)) return;

        // Usar la posición del usuario como centro (sin ajuste de altura para AoE)
        Vector3 center = user.transform.position;
        float radius = Range; 
        
        Debug.Log($"[PISOTÓN] {user.CharacterName} usa {DisplayName} desde posición {center} con radio {radius}m, targetLayer: {targetLayer.value}");
        
        // Intentar primero con OverlapSphere usando el layer
        Collider[] colliders = Physics.OverlapSphere(center, radius, targetLayer);
        Debug.Log($"[PISOTÓN] OverlapSphere encontró {colliders.Length} colliders con targetLayer {targetLayer.value}");
        
        var validTargets = colliders
            .Select(col => col.GetComponentInParent<CharacterActor>())
            .Where(t => t != null && t != user && t.Team != user.Team && !t.Health.IsDead) 
            .Distinct()
            .ToList();

        // FALLBACK: Si no se encontraron targets con el layer, buscar manualmente todos los enemigos en rango
        if (validTargets.Count == 0)
        {
            Debug.LogWarning($"[PISOTÓN] No se encontraron targets con OverlapSphere. Buscando manualmente enemigos en rango...");
            
            // Buscar todos los CharacterActors en la escena y filtrar por distancia y equipo
            CharacterActor[] allActors = Object.FindObjectsOfType<CharacterActor>();
            foreach (var actor in allActors)
            {
                if (actor == null || actor == user || actor.Team == user.Team || actor.Health.IsDead) continue;
                
                float distance = Vector3.Distance(center, actor.transform.position);
                if (distance <= radius)
                {
                    validTargets.Add(actor);
                    Debug.Log($"[PISOTÓN] Target encontrado manualmente: {actor.CharacterName} a {distance:F2}m de distancia");
                }
            }
        }

        Debug.Log($"[PISOTÓN] Targets válidos encontrados: {validTargets.Count}");
        foreach (var t in validTargets)
        {
            Debug.Log($"[PISOTÓN] Target válido: {t.CharacterName} (Team: {t.Team}, User Team: {user.Team})");
        }

        int finalDamage = Mathf.Max(1, fixedDamage);
        
        foreach (var t in validTargets)
        {
            int healthBefore = t.Health.CurrentHealth;
            t.Health.TakeDamage(finalDamage);
            int healthAfter = t.Health.CurrentHealth;
            Debug.Log($"[PISOTÓN] {user.CharacterName} inflige {finalDamage} de daño a {t.CharacterName}. Vida: {healthBefore} -> {healthAfter}");
        }

        user.ConsumeActionPoints(CostAP);

        // CORRECCIÓN: Llamada a InstantiateVFX con 'user' y target 'user' (centro del AoE)
        InstantiateVFX(user, user, 2.0f);

        Debug.Log($"[PISOTÓN] {user.CharacterName} usa Pisotón Sísmico, golpeando a {validTargets.Count} targets por {finalDamage} PD.");
    }
}