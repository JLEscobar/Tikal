using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "MeleeAttack", menuName = "QijTikal/Abilities/Melee Attack")]
public class MeleeAttackAbility : AbilityBase
{
    [Header("Progression")]
    [SerializeField] private int killXP = 10; 

    public override bool CanExecute(CharacterActor user, ITargetable target)
    {
        if (!base.CanExecute(user, target)) return false;
        return user.Team != target.Team; 
    }

    public override void Execute(CharacterActor user, ITargetable target)
    {
        if (!CanExecute(user, target)) return;

        // Lógica de detonación del objeto explosivo
        if (target is MonoBehaviour targetMono && targetMono.TryGetComponent<ExplosiveObject>(out var explosiveObject))
        {
            explosiveObject.Explode(user);
            user.ConsumeActionPoints(CostAP);
            return; 
        }
        
        // Lógica de ataque normal si no es un objeto explosivo
        int damage = Mathf.Max(1, user.AttackPower);
        target.Health.TakeDamage(damage);
        
        // Lógica de XP por eliminación
        if (target.Health.IsDead && user.Team == Team.Player)
        {
            user.GrantExperience(killXP);
            Debug.Log($"{user.CharacterName} eliminó a {((CharacterActor)target).CharacterName} y ganó {killXP} XP!");
        }
        
        user.ConsumeActionPoints(CostAP);

        // **********************************
        // * ZONA DE DIAGNÓSTICO DE VFX *
        // **********************************
        if (AbilityParticles != null)
        {
            // NUEVO: Mensaje de diagnóstico CRÍTICO
            Debug.Log($"[VFX DEBUG] Intentando instanciar {AbilityParticles.name} en {target.GetTransform().name}.");

            GameObject particles = Instantiate(AbilityParticles, target.GetTransform().position, Quaternion.identity);
            Object.Destroy(particles, 3f);
        }
        else
        {
            Debug.Log("[VFX DEBUG] AbilityParticles es NULL. No se intentó instanciar.");
        }
        // **********************************

        Debug.Log($"{user.CharacterName} attacks {((CharacterActor)target).CharacterName} for {damage} damage!");
    }
}