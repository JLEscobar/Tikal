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

    public string Id => name; // Use asset name as ID
    public string DisplayName => displayName;
    public string Description => description;
    public int CostAP => costAP;
    public float Range => range;
    public GameObject AbilityParticles => abilityParticles;

    public virtual bool CanExecute(CharacterActor user, ITargetable target)
    {
        if (user == null || target == null) return false;
        if (user.ActionPoints < costAP) return false;
        if (target.Health.IsDead) return false;

        float distance = Vector3.Distance(user.transform.position, target.GetTransform().position);
        return distance <= range;
    }

    public abstract void Execute(CharacterActor user, ITargetable target);
}
