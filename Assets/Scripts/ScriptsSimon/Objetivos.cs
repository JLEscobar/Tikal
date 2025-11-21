using UnityEngine;

public class Objetivos : MonoBehaviour
{
    private bool objetivo1 = false;
    private bool objetivo2 = false;

    [Header("Referencia al componente CambiarColorYActivar")]
    [Tooltip("Arrastra aquí el GameObject que tiene el componente CambiarColorYActivar")]
    [SerializeField] private CambiarColorYActivar cambiarColorYActivar;
    
    [Header("Referencia al componente ResultadosUI")]
    [Tooltip("Arrastra aquí el GameObject que tiene el componente ResultadosUI (opcional, se busca automáticamente)")]
    [SerializeField] private ResultadosUI resultadosUI;
    
    [Header("Referencia al componente UI_PauseManager")]
    [Tooltip("Arrastra aquí el GameObject que tiene el componente UI_PauseManager (opcional, se busca automáticamente)")]
    [SerializeField] private UI_PauseManager uiPauseManager;

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
        
        // Buscar ResultadosUI y establecer objetivoSecundarioCumplido en true
        if (resultadosUI == null)
        {
            resultadosUI = FindObjectOfType<ResultadosUI>();
        }
        
        if (resultadosUI != null)
        {
            resultadosUI.SetSecundario(true);
            Debug.Log("objetivoSecundarioCumplido establecido en true");
        }
        else
        {
            Debug.LogWarning("No se encontró el componente ResultadosUI. Asegúrate de que existe en la escena.");
        }
        
        // Verificar si ambos objetivos están completos
        VerificarObjetivosCompletos();
    }

    // Método para completar el segundo objetivo
    public void CompletarObjetivo2()
    {
        objetivo2 = true;
        Debug.Log("Objetivo 2 completado");
        
        // Buscar ResultadosUI y establecer objetivoPrincipalCumplido en true
        if (resultadosUI == null)
        {
            resultadosUI = FindObjectOfType<ResultadosUI>();
        }
        
        if (resultadosUI != null)
        {
            resultadosUI.SetPrincipal(true);
            Debug.Log("objetivoPrincipalCumplido establecido en true");
        }
        else
        {
            Debug.LogWarning("No se encontró el componente ResultadosUI. Asegúrate de que existe en la escena.");
        }
        
        // Buscar UI_PauseManager y activar UI_VictoriaDerrota
        if (uiPauseManager == null)
        {
            uiPauseManager = FindObjectOfType<UI_PauseManager>();
        }
        
        if (uiPauseManager != null)
        {
            uiPauseManager.SetActiveUI_VictoriaDerrota(true);
            Debug.Log("UI_VictoriaDerrota activado");
        }
        else
        {
            Debug.LogWarning("No se encontró el componente UI_PauseManager. Asegúrate de que existe en la escena.");
        }
        
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
