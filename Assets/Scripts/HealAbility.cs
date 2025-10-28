using UnityEngine;

[CreateAssetMenu(fileName = "Heal", menuName = "QijTikal/Abilities/Heal")]
public class HealAbility : AbilityBase
{
    [Header("Heal Settings")]
    [SerializeField] private int healAmount = 20;

    public override bool CanExecute(CharacterActor user, ITargetable target)
    {
        if (!base.CanExecute(user, target)) return false;
        return user.Team == target.Team;
    }

    public override void Execute(CharacterActor user, ITargetable target)
    {
        if (!CanExecute(user, target)) return;

        target.Health.Heal(healAmount);
        user.ConsumeActionPoints(CostAP);

        if (AbilityParticles != null)
        {
            GameObject particles = Instantiate(AbilityParticles, target.GetTransform().position, Quaternion.identity);
            Object.Destroy(particles, 3f);
        }

        Debug.Log($"{user.CharacterName} heals {((CharacterActor)target).CharacterName} for {healAmount} HP!");
    }
}
