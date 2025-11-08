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
        if (!base.CanExecute(user, target)) return false;
        return user.Team != target.Team; // Solo aplica debuffs a enemigos
    }

    public override void Execute(CharacterActor user, ITargetable target)
    {
        if (!CanExecute(user, target)) return;

        // 1. Obtener la distancia total movida (para Yaotl)
        float distanceMoved = 0f;
        if (user.TryGetComponent<TacticalMovementController>(out var moveController))
        {
            distanceMoved = moveController.GetDistanceMovedThisTurn(user.transform.position); 
        }

        // 2. Calcular el daño adicional
        int bonusUnits = Mathf.FloorToInt(distanceMoved / movementUnitThreshold);
        int bonusDamage = bonusUnits * bonusDamagePerUnit;
        int totalDamage = Mathf.Max(1, attackDamage + bonusDamage);
        
        // 3. Aplicar daño y afectación
        target.Health.TakeDamage(totalDamage);
        
        if (target is CharacterActor targetActor)
        {
            targetActor.ApplyStatusEffect(effectType, effectDuration); 
        }

        user.ConsumeActionPoints(CostAP);

        if (AbilityParticles != null)
        {
            GameObject particles = Instantiate(AbilityParticles, target.GetTransform().position, Quaternion.identity);
            Object.Destroy(particles, 3f);
        }

        Debug.Log($"[Debuff] {user.CharacterName} uses {DisplayName}, Total PD: {totalDamage}, applying {effectType}!");
    }
}