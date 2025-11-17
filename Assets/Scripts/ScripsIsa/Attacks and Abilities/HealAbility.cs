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

        // CORRECCIÓN: Llamada a InstantiateVFX con 'user'
        InstantiateVFX(user, target, 1.0f);

        Debug.Log($"{user.CharacterName} heals {target.GetTransform().name} for {healAmount} HP!");
    }
}
