using UnityEngine;
using System.Linq;
using System.Collections.Generic;

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
        if (!base.CanExecute(user, target)) return false;
        return user.Team != target.Team;
    }

    public override void Execute(CharacterActor user, ITargetable target)
    {
        if (!CanExecute(user, target)) return; 
        
        Vector3 userPosition = user.transform.position;
        Vector3 targetPosition = target.GetTransform().position;

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

        if (moveUserToTarget) 
        {
            Vector3 teleportPosition = targetPosition - direction * teleportOffset; 
            user.ForceTeleportToPosition(teleportPosition); 
        }

        user.ConsumeActionPoints(CostAP);

        // CORRECCIÓN: Llamada a InstantiateVFX con 'user'
        InstantiateVFX(user, target, 2.0f);

        Debug.Log($"[DESPLAZAMIENTO] {user.CharacterName} usa Desplazamiento Letal, golpeando a {validTargets.Count} targets.");
    }
}