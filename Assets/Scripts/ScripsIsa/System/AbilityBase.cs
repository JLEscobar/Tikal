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
            Debug.Log($"[VFX DEBUG] {DisplayName}: Usando VFX del ScriptableObject: {this.abilityParticles.name} (GUID: {this.abilityParticles.GetInstanceID()})");
            return this.abilityParticles;
        }
        
        // 2. Inyección dinámica desde el CharacterActor (override por ranura)
        if (user != null)
        {
            var injectedVFX = user.GetAbilityVFXOverride(this);
            if (injectedVFX != null)
            {
                Debug.Log($"[VFX DEBUG] {DisplayName}: Usando override de {user.CharacterName}: {injectedVFX.name} (GUID: {injectedVFX.GetInstanceID()})");
                return injectedVFX;
            }
            else
            {
                Debug.LogWarning($"[VFX DEBUG] {DisplayName}: No se encontró override en {user.CharacterName} para esta habilidad.");
            }
        }
        
        // 3. Fallo total.
        Debug.LogError($"[VFX DEBUG] {DisplayName}: NO HAY VFX ASIGNADO. Ni en SO ni en override.");
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
            
            Debug.Log($"[VFX DEBUG] Instanciando prefab: {particlesPrefab.name} en posición {spawnPosition}");
            GameObject particlesGO = Instantiate(particlesPrefab, spawnPosition, Quaternion.identity);
            Debug.Log($"[VFX DEBUG] Instancia creada: {particlesGO.name} (GUID: {particlesGO.GetInstanceID()})");

            // Búsqueda robusta y forzada de la reproducción - REPRODUCIR TODOS LOS PARTICLE SYSTEMS
            ParticleSystem[] allParticleSystems = particlesGO.GetComponentsInChildren<ParticleSystem>(true);
            Debug.Log($"[VFX DEBUG] Encontrados {allParticleSystems.Length} ParticleSystem(s) en el prefab instanciado.");
            
            if (allParticleSystems.Length > 0)
            {
                foreach (var ps in allParticleSystems)
                {
                    ps.Play();
                    Debug.Log($"[VFX DEBUG] ParticleSystem '{ps.name}' reproducido. Main module startColor: {ps.main.startColor.color}, Start Lifetime: {ps.main.startLifetime.constant}");
                }
            }
            else
            {
                Debug.LogError($"[VFX DEBUG] No se encontró ningún ParticleSystem en el Prefab {particlesPrefab.name}.");
            }
            
            Object.Destroy(particlesGO, 3f);
        }
        else
        {
            Debug.LogWarning($"[VFX DEBUG] INYECCIÓN FALLIDA: No hay prefab asignado en SO ni en Actor para {DisplayName}.");
        }
    }
}