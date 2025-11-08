using UnityEngine;
using System.Linq;

public class ExplosiveObject : MonoBehaviour, ITargetable
{
    [Header("Explosion Configuration")]
    [Tooltip("El rango del radio de la explosión (ej. 3 metros).")]
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private int fixedDamage = 35; 
    [SerializeField] private StatusEffectType effectType = StatusEffectType.Quemado;
    [SerializeField] private int effectDuration = 2; 
    [SerializeField] private LayerMask targetLayer; 

    [Header("Visual Effects")] // NUEVO: Sección para VFX
    [Tooltip("Prefab del efecto de partículas de explosión.")]
    [SerializeField] private GameObject explosionVFXPrefab; 
    [Tooltip("Tiempo que duran los VFX antes de ser destruidos.")]
    [SerializeField] private float vfxDuration = 2.0f; 

    public Team Team => Team.Enemy; 
    public IHealth Health { get; private set; } 

    void Awake()
    {
        Health = GetComponent<Health>(); 
        if (Health == null)
        {
            Debug.LogWarning($"ExplosiveObject en {name} no tiene Health. Será destruido al primer impacto.");
        }
    }

    public Transform GetTransform() => transform;
    
    public void Explode(CharacterActor attacker)
    {
        Vector3 center = transform.position;
        
        // 1. Detección de objetivos
        Collider[] colliders = Physics.OverlapSphere(center, explosionRadius, targetLayer);
        
        // ... (resto de la lógica de daño) ...

        // 2. Instanciar los VFX de la explosión (NUEVO)
        if (explosionVFXPrefab != null)
        {
            GameObject vfxInstance = Instantiate(explosionVFXPrefab, center, Quaternion.identity);
            Destroy(vfxInstance, vfxDuration); // Destruir el VFX después de su duración
        }

        // 3. Destruir el objeto explosivo
        Destroy(gameObject);
        
        // ... (resto de la lógica de XP) ...
    }
}