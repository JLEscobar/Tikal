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
        if (!CanExecute(user, target))
        {
            Debug.LogWarning($"[Support] {user.CharacterName} no puede ejecutar {DisplayName} en {target?.GetTransform()?.name ?? "null"}. CanExecute retornó false.");
            return;
        }
        
        if (target == null)
        {
            Debug.LogError($"[Support] {user.CharacterName} intentó usar {DisplayName} pero el target es null!");
            return;
        }

        if (target is CharacterActor targetActor)
        {
            // Verificar que el target sea del mismo equipo
            if (user.Team != targetActor.Team)
            {
                Debug.LogError($"[Support] {user.CharacterName} intentó usar {DisplayName} en {targetActor.CharacterName} pero son de equipos diferentes! User: {user.Team}, Target: {targetActor.Team}");
                return;
            }

            if (healAmount > 0)
            {
                int healthBefore = targetActor.Health.CurrentHealth;
                targetActor.Health.Heal(healAmount);
                int healthAfter = targetActor.Health.CurrentHealth;
                MessagesSystem.Instance.ShowMessage($"{targetActor.CharacterName} se cura {healAmount} PV ({healthBefore} -> {healthAfter}).", Color.green);
                Debug.Log($"[Support] {user.CharacterName} cura a {targetActor.CharacterName} por {healAmount} PV. Vida: {healthBefore} -> {healthAfter}");
            }
            
            if (buffType != StatusEffectType.None)
            {
                targetActor.ApplyStatusEffect(buffType, buffDuration);
                Debug.Log($"[Support] {user.CharacterName} aplica {buffType} a {targetActor.CharacterName} por {buffDuration} turnos.");
            }
        }
        else
        {
            // Fallback para otros tipos de ITargetable
            if (healAmount > 0)
            {
                target.Health.Heal(healAmount);
                MessagesSystem.Instance.ShowMessage($"{target.GetTransform().name} se cura {healAmount} PV.", Color.green);
            }
        }

        user.ConsumeActionPoints(CostAP);

        // CORRECCIÓN: Llamada a InstantiateVFX con 'user'
        InstantiateVFX(user, target, 1.0f);

        Debug.Log($"[Support] {user.CharacterName} usa {DisplayName} en {target.GetTransform().name} exitosamente.");
    }
}