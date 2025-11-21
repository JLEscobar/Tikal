using UnityEngine;
using UnityEngine.SceneManagement;

public class UI_PauseManager : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("GameObject del menú de pausa")]
    [SerializeField] private GameObject menuPausa;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (menuPausa != null)
        {
            menuPausa.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            TogglePauseMenu();
        }
    }

    public void TogglePauseMenu()
    {
        if (menuPausa != null)
        {
            bool isCurrentlyActive = menuPausa.activeSelf;
            
            // Si el menú está activo, desactivarlo y reanudar el juego
            if (isCurrentlyActive)
            {
                menuPausa.SetActive(false);
                PauseService.SetPaused(false);
            }
            // Si el menú está inactivo, activarlo y pausar el juego
            else
            {
                menuPausa.SetActive(true);
                PauseService.SetPaused(true);
            }
        }
    }

    /// <summary>
    /// Desactiva el menú de pausa y reanuda el tiempo del juego
    /// </summary>
    public void DesactivarMenuYReanudar()
    {
        if (menuPausa != null)
        {
            menuPausa.SetActive(false);
        }
        PauseService.SetPaused(false);
    }

    /// <summary>
    /// Reinicia el nivel actual (recarga la escena actual)
    /// </summary>
    public void LoadActualScene()
    {
        PauseService.SetPaused(false);
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }

    /// <summary>
    /// Carga la escena del mapa (escena 3)
    /// </summary>
    public void loadMapa()
    {
        PauseService.SetPaused(false);
        SceneManager.LoadScene(2);
    }

    /// <summary>
    /// Sale del juego
    /// </summary>
    public void SalirDelJuego()
    {
        Application.Quit();
    }
}
