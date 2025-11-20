// DebuffAttackAbility.cs
using UnityEngine;

[CreateAssetMenu(fileName = "DebuffAttack", menuName = "QijTikal/Abilities/Debuff Attack")]
public class DebuffAttackAbility : AbilityBase
{
    [Header("Debuff Settings")]
    [Tooltip("El tipo de afectación a aplicar.")]
    [SerializeField] private StatusEffectType effectType = StatusEffectType.Ralentizado;
    
    [Tooltip("La duración del efecto en turnos.")]
    [SerializeField] private int effectDuration = 4;

    [Header("Damage Calculation")]
    [Tooltip("Daño base asociado (ej: 25 PD).")]
    [SerializeField] private int attackDamage = 25;

    [Tooltip("Daño extra por cada unidad de distancia movida (0 para la mayoría).")]
    [SerializeField] private int bonusDamagePerUnit = 0;

    [Tooltip("Unidad de distancia para el cálculo (0.5 metros para Yaotl).")]
    [SerializeField] private float movementUnitThreshold = 1.0f;

    public override bool CanExecute(CharacterActor user, ITargetable target)
    {
        if (!base.CanExecute(user, target))
        {
            Debug.LogWarning($"[Debuff CanExecute] {user?.CharacterName}'s {DisplayName}: base.CanExecute retornó false");
            return false;
        }
        
        if (target == null)
        {
            Debug.LogWarning($"[Debuff CanExecute] {user?.CharacterName}'s {DisplayName}: target es null");
            return false;
        }
        
        // Verificar que el target sea enemigo (no aliado)
        // Solo verificar si el target es un CharacterActor (no un ExplosiveObject)
        if (target is CharacterActor targetActor)
        {
            if (user.Team == targetActor.Team)
            {
                Debug.LogWarning($"[Debuff CanExecute] {user?.CharacterName}'s {DisplayName}: target {targetActor.CharacterName} es del mismo equipo ({user.Team})");
                return false;
            }
        }
        else
        {
            // Para ExplosiveObject u otros tipos, verificar usando la propiedad Team de ITargetable
            if (user.Team == target.Team)
            {
                Debug.LogWarning($"[Debuff CanExecute] {user?.CharacterName}'s {DisplayName}: target {target.GetTransform().name} es del mismo equipo ({user.Team})");
                return false;
            }
        }
        
        Debug.Log($"[Debuff CanExecute] ✓ {user?.CharacterName}'s {DisplayName}: Todas las verificaciones pasaron para target {target.GetTransform().name}");
        return true;
    }

    public override void Execute(CharacterActor user, ITargetable target)
    {
        if (!CanExecute(user, target))
        {
            Debug.LogWarning($"[Debuff] {user.CharacterName} no puede ejecutar {DisplayName}. CanExecute retornó false.");
            return;
        }

        if (target == null)
        {
            Debug.LogError($"[Debuff] {user.CharacterName} intentó usar {DisplayName} pero el target es null!");
            return;
        }

        float distanceMoved = 0f;
        if (user.TryGetComponent<TacticalMovementController>(out var moveController))
        {
            distanceMoved = moveController.GetDistanceMovedThisTurn(user.transform.position); 
        }

        int bonusUnits = Mathf.FloorToInt(distanceMoved / movementUnitThreshold);
        int bonusDamage = bonusUnits * bonusDamagePerUnit;
        int totalDamage = Mathf.Max(1, attackDamage + bonusDamage);
        
        if (target is CharacterActor targetActor)
        {
            targetActor.Health.TakeDamage(totalDamage);
            targetActor.ApplyStatusEffect(effectType, effectDuration);
            Debug.Log($"[Debuff] {user.CharacterName} usa {DisplayName} en {targetActor.CharacterName}, Total PD: {totalDamage}, aplicando {effectType}! Vida restante: {targetActor.Health.CurrentHealth}/{targetActor.Health.MaxHealth}");
        }
        else
        {
            target.Health.TakeDamage(totalDamage);
            Debug.Log($"[Debuff] {user.CharacterName} usa {DisplayName}, Total PD: {totalDamage}, aplicando {effectType}!");
        }

        user.ConsumeActionPoints(CostAP);

        // CORRECCIÓN: Llamada a InstantiateVFX con 'user'
        InstantiateVFX(user, target, 1.0f);
    }
}