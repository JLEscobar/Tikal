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

        Vector3 center = user.transform.position;
        float radius = Range; 
        
        Collider[] colliders = Physics.OverlapSphere(center, radius, targetLayer);
        
        var validTargets = colliders
            .Select(col => col.GetComponentInParent<CharacterActor>())
            .Where(t => t != null && t.Team != user.Team && !t.Health.IsDead) 
            .Distinct()
            .ToList();

        int finalDamage = Mathf.Max(1, fixedDamage);
        
        foreach (var t in validTargets)
        {
            t.Health.TakeDamage(finalDamage);
        }

        user.ConsumeActionPoints(CostAP);

        // CORRECCIÓN: Llamada a InstantiateVFX con 'user' y target 'user' (centro del AoE)
        InstantiateVFX(user, user, 2.0f);

        Debug.Log($"[PISOTÓN] {user.CharacterName} usa Pisotón Sísmico, golpeando a {validTargets.Count} targets por {finalDamage} PD.");
    }
}