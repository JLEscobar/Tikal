using UnityEngine;

public class TagPlayerChecker : MonoBehaviour
{
    [Header("Referencia al componente UI_PauseManager")]
    [Tooltip("Arrastra aquí el GameObject que tiene el componente UI_PauseManager (opcional, se busca automáticamente)")]
    [SerializeField] private UI_PauseManager uiPauseManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Verificar si hay objetos con el tag "Player"
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
        
        // Si no encuentra objetos con el tag "Player"
        if (playerObjects == null || playerObjects.Length == 0)
        {
            Debug.Log("No se encontraron objetos con el tag Player");
            
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
        }
    }
}
