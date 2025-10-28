using UnityEngine;

[CreateAssetMenu(fileName = "MeleeAttack", menuName = "QijTikal/Abilities/Melee Attack")]
public class MeleeAttackAbility : AbilityBase
{
    public override bool CanExecute(CharacterActor user, ITargetable target)
    {
        if (!base.CanExecute(user, target)) return false;
        return user.Team != target.Team;
    }

    public override void Execute(CharacterActor user, ITargetable target)
    {
        if (!CanExecute(user, target)) return;

        int damage = Mathf.Max(1, user.AttackPower);
        target.Health.TakeDamage(damage);
        user.ConsumeActionPoints(CostAP);

        if (AbilityParticles != null)
        {
            GameObject particles = Instantiate(AbilityParticles, target.GetTransform().position, Quaternion.identity);
            Object.Destroy(particles, 3f);
        }

        Debug.Log($"{user.CharacterName} attacks {((CharacterActor)target).CharacterName} for {damage} damage!");
    }
}
