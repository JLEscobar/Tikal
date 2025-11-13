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
        // 1. Revisa condiciones base (rango, AP, target vivo)
        if (!base.CanExecute(user, target)) return false;
        
        // 2. CHEQUEO CRÍTICO: Debe ser un aliado
        if (user.Team != target.Team)
        {
             Debug.Log("Soporte Fallido: Solo se puede usar en aliados.");
             return false;
        }

        // 3. Chequeo de estado (no tiene sentido curar/buffear a alguien muerto)
        if (target is CharacterActor targetActor && targetActor.Health.IsDead)
        {
             Debug.Log("Soporte Fallido: El objetivo está fuera de combate.");
             return false;
        }
        
        return true;
    }

    public override void Execute(CharacterActor user, ITargetable target)
    {
        if (!CanExecute(user, target)) return;
        
        // 1. Curación
        if (healAmount > 0)
        {
            target.Health.Heal(healAmount);
            MessagesSystem.Instance.ShowMessage($"{target.GetTransform().name} se cura {healAmount} PV.", Color.green);
        }
        
        // 2. Aplicar Buff/Status Effect
        if (buffType != StatusEffectType.None && target is CharacterActor targetActor)
        {
            targetActor.ApplyStatusEffect(buffType, buffDuration);
        }

        user.ConsumeActionPoints(CostAP);

        if (AbilityParticles != null)
        {
            GameObject particles = Instantiate(AbilityParticles, target.GetTransform().position, Quaternion.identity);
            Object.Destroy(particles, 3f);
        }

        Debug.Log($"{user.CharacterName} usa {DisplayName} en {target.GetTransform().name}.");
    }
}