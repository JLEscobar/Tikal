using UnityEngine;

public class Objetivos : MonoBehaviour
{
    private bool objetivo1 = false;
    private bool objetivo2 = false;

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
    }

    // Método para completar el segundo objetivo
    public void CompletarObjetivo2()
    {
        objetivo2 = true;
        Debug.Log("Objetivo 2 completado");
    }
}
