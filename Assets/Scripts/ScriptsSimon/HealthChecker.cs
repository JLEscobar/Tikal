using UnityEngine;

public class HealthChecker : MonoBehaviour
{
    public string personajeTag = "Boss";

    private GameObject bossObject;
    private Health bossHealth;
    private Objetivos objetivos;
    private bool objetivo2Notificado;

    void Start()
    {
        TryInicializarReferencias();
        LoggearEstadoActual();
    }

    void Update()
    {
        if (bossHealth == null)
        {
            TryInicializarReferencias();
            return;
        }

        if (!objetivo2Notificado && bossHealth.CurrentHealth <= 1)
        {
            if (objetivos == null)
            {
                objetivos = FindObjectOfType<Objetivos>();
            }

            if (objetivos != null)
            {
                objetivos.CompletarObjetivo2();
                objetivo2Notificado = true;
                Debug.Log("Objetivo 2 completado (HealthChecker)");
            }
            else
            {
                Debug.LogWarning("No se encontró un componente Objetivos en la escena.");
            }
        }
    }

    public void SetBossHealthTo150()
    {
        if (bossHealth == null)
        {
            TryInicializarReferencias();
            if (bossHealth == null) return;
        }

        bossHealth._currentHealth = 150;
        bossHealth.maxHealth = Mathf.Max(bossHealth.maxHealth, 150);
        Debug.Log($"Vida del boss {bossObject.name} establecida a 150.");
    }

    private void TryInicializarReferencias()
    {
        bossObject = GameObject.FindWithTag(personajeTag);
        if (bossObject == null)
        {
            Debug.LogWarning($"No encontré un objeto con la tag {personajeTag}");
            bossHealth = null;
            return;
        }

        bossHealth = bossObject.GetComponent<Health>();
        if (bossHealth == null)
        {
            Debug.LogWarning($"El objeto con tag {personajeTag} no tiene componente Health");
        }
    }

    private void LoggearEstadoActual()
    {
        if (bossObject != null && bossHealth != null)
        {
            Debug.Log($"Vida actual de {bossObject.name}: {bossHealth.CurrentHealth}/{bossHealth.MaxHealth}");
        }
    }
}
