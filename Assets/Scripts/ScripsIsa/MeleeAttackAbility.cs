using UnityEngine;

[CreateAssetMenu(fileName = "MeleeAttack", menuName = "QijTikal/Abilities/Melee Attack")]
public class MeleeAttackAbility : AbilityBase
{
    [Header("Progression")]
    [SerializeField] private int killXP = 10; 

    public override bool CanExecute(CharacterActor user, ITargetable target)
    {
        // Esta habilidad solo debe usarse en enemigos o en objetos explosivos (target.Team == Enemy)
        if (!base.CanExecute(user, target)) return false;
        
        // Si el target es un explosivo, el rango es 2m para activarlo.
        return true; 
    }

    public override void Execute(CharacterActor user, ITargetable target)
    {
        if (!CanExecute(user, target)) return;

        // -------------------------------------------------------------
        // NUEVA LÓGICA: DETONAR EL OBJETO EXPLOSIVO
        // -------------------------------------------------------------
        if (target is MonoBehaviour targetMono && targetMono.TryGetComponent<ExplosiveObject>(out var explosiveObject))
        {
            explosiveObject.Explode(user);
            user.ConsumeActionPoints(CostAP);
            return; // Terminar la ejecución aquí
        }
        
        // Lógica de ataque normal si no es un objeto explosivo
        int damage = Mathf.Max(1, user.AttackPower);
        target.Health.TakeDamage(damage);
        
        // Lógica de XP por eliminación (si es un enemigo vivo)
        if (target.Health.IsDead && user.Team == Team.Player)
        {
            user.GrantExperience(killXP);
            Debug.Log($"{user.CharacterName} eliminó a {((CharacterActor)target).CharacterName} y ganó {killXP} XP!");
        }
        
        user.ConsumeActionPoints(CostAP);

        if (AbilityParticles != null)
        {
            GameObject particles = Instantiate(AbilityParticles, target.GetTransform().position, Quaternion.identity);
            Object.Destroy(particles, 3f);
        }

        Debug.Log($"{user.CharacterName} attacks {((CharacterActor)target).CharacterName} for {damage} damage!");
    }
}