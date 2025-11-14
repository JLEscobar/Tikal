using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "MeleeAttack", menuName = "QijTikal/Abilities/Melee Attack")]
public class MeleeAttackAbility : AbilityBase
{
    [Header("Progression")]
    [SerializeField] private int killXP = 10; 

    public override bool CanExecute(CharacterActor user, ITargetable target)
    {
        // Delega la verificación de rango, AP y target muerto a AbilityBase
        if (!base.CanExecute(user, target)) return false;
        return user.Team != target.Team; // Solo enemigos
    }

    public override void Execute(CharacterActor user, ITargetable target)
    {
        // Vuelve a verificar que las condiciones se cumplan
        if (!CanExecute(user, target)) return; 

        // Lógica de detonación del objeto explosivo 
        if (target is MonoBehaviour targetMono && targetMono.TryGetComponent<ExplosiveObject>(out var explosiveObject))
        {
            explosiveObject.Explode(user);
            user.ConsumeActionPoints(CostAP); 
            
            // LLAMADA AL NUEVO SISTEMA DE INYECCIÓN DE VFX
            InstantiateVFX(user, target, 1.0f);
            return; // Sale temprano, lo cual es correcto para explosivos
        }
        
        // Lógica de ataque normal
        int damage = Mathf.Max(1, user.AttackPower);
        target.Health.TakeDamage(damage);
        
        // Lógica de XP por eliminación
        if (target.Health.IsDead && user.Team == Team.Player)
        {
            user.GrantExperience(killXP);
        }
        
        // CONSUMO DE AP SÓLO SI EL ATAQUE FUE EXITOSO
        user.ConsumeActionPoints(CostAP);

        // **********************************************************
        // * ENFOQUE FINAL: AHORA EL VFX SE INSTANCIA Y LUEGO HACE EL LOG *
        // **********************************************************
        InstantiateVFX(user, target, 1.0f);

        Debug.Log($"{user.CharacterName} attacks {target.GetTransform().name} for {damage} damage!");
    }
}