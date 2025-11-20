using UnityEngine;

public class Objetivos : MonoBehaviour
{
    private bool objetivo1 = false;
    private bool objetivo2 = false;

    [Header("Referencia al componente CambiarColorYActivar")]
    [Tooltip("Arrastra aquí el GameObject que tiene el componente CambiarColorYActivar")]
    [SerializeField] private CambiarColorYActivar cambiarColorYActivar;

    // Método para completar el primer objetivo
    public void CompletarObjetivo1()
    {
        objetivo1 = true;
        Debug.Log("Objetivo 1 completado");
        HealthChecker healthChecker = FindObjectOfType<HealthChecker>();
        if (healthChecker != null)
        {
            healthChecker.SetBossHealthTo150();
        }
        else
        {
            Debug.LogWarning("No se encontró un healthCheker");
        }
        
        // Verificar si ambos objetivos están completos
        VerificarObjetivosCompletos();
    }

    // Método para completar el segundo objetivo
    public void CompletarObjetivo2()
    {
        objetivo2 = true;
        Debug.Log("Objetivo 2 completado");
        
        // Verificar si ambos objetivos están completos
        VerificarObjetivosCompletos();
    }

    /// <summary>
    /// Verifica si ambos objetivos están completos y activa seCumplio en CambiarColorYActivar
    /// </summary>
    private void VerificarObjetivosCompletos()
    {
        if (objetivo1 && objetivo2)
        {
            Debug.Log("¡Ambos objetivos completados!");
            
            // Si no hay referencia asignada, intentar buscarla
            if (cambiarColorYActivar == null)
            {
                cambiarColorYActivar = FindObjectOfType<CambiarColorYActivar>();
                if (cambiarColorYActivar == null)
                {
                    Debug.LogWarning("No se encontró el componente CambiarColorYActivar. Buscando en la escena...");
                    return;
                }
            }
            
            // Activar seCumplio
            cambiarColorYActivar.seCumplio = true;
            Debug.Log("seCumplio activado en CambiarColorYActivar");
        }
    }
}
