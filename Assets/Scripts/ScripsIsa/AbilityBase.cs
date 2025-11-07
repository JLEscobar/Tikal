using UnityEngine;

public abstract class AbilityBase : ScriptableObject, IAbility
{
    [Header("Ability Info")]
    [SerializeField] private string displayName = "Ability";
    [SerializeField] private string description = "";

    [Header("Costs & Range")]
    [SerializeField] private int costAP = 1;
    [SerializeField] private float range = 2f;

    [Header("Particles")]
    [SerializeField] private GameObject abilityParticles;

    // Cooldown
    [Header("Cooldown")]
    [Tooltip("Turnos que debe esperar antes de volver a usar la habilidad.")]
    [SerializeField] private int baseCooldownTurns = 0; 
    [System.NonSerialized] public int currentCooldown = 0;

    public string Id => name; 
    public string DisplayName => displayName;
    public string Description => description;
    public int CostAP => costAP;
    public float Range => range;
    public GameObject AbilityParticles => abilityParticles;
    public int BaseCooldownTurns => baseCooldownTurns; 

    // MÉTODO CORREGIDO: Soluciona el NullReference cuando el target es un ExplosiveObject.
    public virtual bool CanExecute(CharacterActor user, ITargetable target)
    {
        if (user == null) return false;

        // Cooldown Check
        if (currentCooldown > 0) 
        {
            Debug.Log($"[CD] {user.CharacterName}'s {DisplayName} is on cooldown for {currentCooldown} turns.");
            return false;
        }

        if (user.ActionPoints < costAP) return false;

        // Chequeo de Target: Solo si NO es AoE
        if (!(this is AreaAttackAbility))
        {
            if (target == null) return false;

            // ***** CORRECCIÓN CLAVE *****
            // Solo chequeamos salud si el target es un CharacterActor (no un ExplosiveObject)
            if (target is CharacterActor targetActor)
            {
                if (targetActor.Health.IsDead) return false;
            }
            // ***************************

            float distance = Vector3.Distance(user.transform.position, target.GetTransform().position);
            
            // Rango Check (con margen de 0.1f)
            if (distance > range + 0.1f) 
            {
                Debug.Log($"[Range Fail] Distance: {distance:F1}, Max Range: {range}. Out of range.");
                return false;
            }
        }
        
        return true;
    }

    public abstract void Execute(CharacterActor user, ITargetable target);
}