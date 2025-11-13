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
        if (user == null) return false;
        if (user.ActionPoints < CostAP) return false;
        
        // Cooldown y otros checks
        if (currentCooldown > 0) return false;
        
        return !user.Health.IsDead;
    }

    public override void Execute(CharacterActor user, ITargetable target)
    {
        if (!CanExecute(user, target)) return;

        Vector3 center = user.transform.position;
        float radius = Range; 
        
        // 1. Lógica de detección y daño
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

        // **********************************
        // * CORRECCIÓN FINAL: ACTIVACIÓN FORZADA DEL VFX *
        // **********************************
        if (AbilityParticles != null)
        {
            Vector3 spawnPosition = new Vector3(center.x, center.y + 2.0f, center.z); 
            
            GameObject particlesGO = Instantiate(AbilityParticles, spawnPosition, Quaternion.identity);
            
            // OBTENER Y FORZAR REPRODUCCIÓN
            if (particlesGO.TryGetComponent<ParticleSystem>(out var ps))
            {
                ps.Play(); // Forzamos el inicio de la emisión
            }

            Debug.Log($"[VFX CRITICAL] Instanciando {AbilityParticles.name} en Y={spawnPosition.y}.");
            
            // Destruir el GameObject instanciado (que contiene el sistema de partículas)
            Object.Destroy(particlesGO, 3f);
        }
        // **********************************

        Debug.Log($"[PISOTÓN] {user.CharacterName} usa Pisotón Sísmico, golpeando a {validTargets.Count} targets por {finalDamage} PD.");
    }
}