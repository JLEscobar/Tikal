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

    // NUEVO: Cooldown inicial y de runtime
    [Header("Cooldown")]
    [Tooltip("Turnos que debe esperar antes de volver a usar la habilidad.")]
    [SerializeField] private int baseCooldownTurns = 0; 
    [System.NonSerialized] public int currentCooldown = 0; // Valor que se cuenta hacia abajo

    public string Id => name; 
    public string DisplayName => displayName;
    public string Description => description;
    public int CostAP => costAP;
    public float Range => range;
    public GameObject AbilityParticles => abilityParticles;
    public int BaseCooldownTurns => baseCooldownTurns; // Nueva propiedad

    // MÉTODO MODIFICADO: Agrega chequeo de cooldown
    public virtual bool CanExecute(CharacterActor user, ITargetable target)
    {
        if (user == null || target == null) return false;
        if (user.ActionPoints < costAP) return false;
        if (target.Health.IsDead) return false;

        // NUEVO CHEQUEO: No se puede usar si está en cooldown
        if (currentCooldown > 0) 
        {
            Debug.Log($"[CD] {user.CharacterName}'s {DisplayName} is on cooldown for {currentCooldown} turns.");
            return false;
        }

        float distance = Vector3.Distance(user.transform.position, target.GetTransform().position);
        return distance <= range;
    }

    public abstract void Execute(CharacterActor user, ITargetable target);
}