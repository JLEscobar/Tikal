using UnityEngine;
using System.Linq;

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

            // Solo chequeamos salud si el target es un CharacterActor (no un ExplosiveObject)
            if (target is CharacterActor targetActor)
            {
                if (targetActor.Health.IsDead) return false;
            }

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

    // MÉTODO CLAVE: Lógica de doble inyección de VFX (Fallback)
    protected GameObject GetVFXPrefab(CharacterActor user)
    {
        // 1. Prioridad: Intentamos usar la referencia asignada en este ScriptableObject
        if (this.abilityParticles != null)
        {
            return this.abilityParticles;
        }
        
        // 2. Si el SO está corrupto, usamos la inyección del CharacterActor.
        if (user != null)
        {
            // Determinamos si es una Habilidad Especial (Index 1 o Superior)
            // (LineAttack, DebuffAttack, Support, o cualquier otra habilidad compleja)
            if (this is LineAttackAbility || this is DebuffAttackAbility || this is SupportAbility) 
            {
                if (user.specialAbilityVFXPrefab != null)
                {
                    Debug.LogWarning($"[VFX INJECTION] Usando respaldo especial ({user.specialAbilityVFXPrefab.name}).");
                    return user.specialAbilityVFXPrefab;
                }
            }
            
            // 3. Fallback al Ataque Básico (Index 0 o si no se encontró VFX especial)
            if (user.defaultAbilityVFXPrefab != null)
            {
                Debug.LogWarning($"[VFX INJECTION] Usando respaldo básico ({user.defaultAbilityVFXPrefab.name}).");
                return user.defaultAbilityVFXPrefab;
            }
        }
        
        // 4. Fallo total.
        return null;
    }

    // Método auxiliar para instanciar VFX
    protected void InstantiateVFX(CharacterActor user, ITargetable target, float yOffset = 1.0f)
    {
        GameObject particlesPrefab = GetVFXPrefab(user);
        
        if (particlesPrefab != null)
        {
            Vector3 targetPosition = target.GetTransform().position;
            // Spawnea con el offset dado (1.0f por defecto para visibilidad)
            Vector3 spawnPosition = new Vector3(targetPosition.x, targetPosition.y + yOffset, targetPosition.z); 
            
            GameObject particlesGO = Instantiate(particlesPrefab, spawnPosition, Quaternion.identity);

            // Búsqueda robusta y forzada de la reproducción
            ParticleSystem ps = particlesGO.GetComponent<ParticleSystem>();
            if (ps == null) 
            { 
                ps = particlesGO.GetComponentInChildren<ParticleSystem>(true); 
            }

            if (ps != null)
            { 
                ps.Play(); 
                Debug.Log($"[VFX DEBUG] Instanciado y reproducido {particlesPrefab.name} en Y={spawnPosition.y}.");
            }
            else
            {
                Debug.LogError($"[VFX DEBUG] No se encontró ParticleSystem en el Prefab {particlesPrefab.name}.");
            }
            
            Object.Destroy(particlesGO, 3f);
        }
        else
        {
            Debug.LogWarning($"[VFX DEBUG] INYECCIÓN FALLIDA: No hay prefab asignado en SO ni en Actor para {DisplayName}.");
        }
    }
}