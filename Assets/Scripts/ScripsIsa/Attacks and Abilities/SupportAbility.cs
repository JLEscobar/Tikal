using UnityEngine;

[CreateAssetMenu(fileName = "SupportAbility", menuName = "QijTikal/Abilities/Support")]
public class SupportAbility : AbilityBase
{
    [Header("Support Effects")]
    [SerializeField] private int healAmount = 0; 
    [SerializeField] private StatusEffectType buffType = StatusEffectType.None;
    [SerializeField] private int buffDuration = 0; 

    public override bool CanExecute(CharacterActor user, ITargetable target)
    {
        if (!base.CanExecute(user, target)) return false;
        
        if (user.Team != target.Team) return false;
        if (target is CharacterActor targetActor && targetActor.Health.IsDead) return false;
        
        return true;
    }

    public override void Execute(CharacterActor user, ITargetable target)
    {
        if (!CanExecute(user, target)) return;
        
        if (healAmount > 0)
        {
            target.Health.Heal(healAmount);
            MessagesSystem.Instance.ShowMessage($"{target.GetTransform().name} se cura {healAmount} PV.", Color.green);
        }
        
        if (buffType != StatusEffectType.None && target is CharacterActor targetActor)
        {
            targetActor.ApplyStatusEffect(buffType, buffDuration);
        }

        user.ConsumeActionPoints(CostAP);

        // CORRECCIÓN: Llamada a InstantiateVFX con 'user'
        InstantiateVFX(user, target, 1.0f);

        Debug.Log($"{user.CharacterName} usa {DisplayName} en {target.GetTransform().name}.");
    }
}