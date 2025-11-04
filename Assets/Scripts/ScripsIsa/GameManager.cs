using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine; 

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Systems")]
    [SerializeField] private TurnSystem turnSystem;

    [Header("UI")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private GameObject defeatScreen;
    
    [Header("World UI (for TacticalMovementController)")]
    public Canvas canvasWorldObject;
    public UnityEngine.UI.Button endTurnButton;
    
    [Header("Camera")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private CinemachineCamera virtualCamera; // Added Cinemachine virtual camera reference

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Búsqueda segura de referencias
        if (turnSystem == null) turnSystem = FindFirstObjectByType<TurnSystem>();
        if (mainCamera == null) mainCamera = Camera.main;
        if (virtualCamera == null) virtualCamera = FindFirstObjectByType<CinemachineCamera>();

        // Si la cámara virtual es una CinemachineFreeLook, por defecto en Awake,
        // podrías querer que siga un objeto vacío (pivot) si el juego no empieza inmediatamente
        // en combate, pero la corrección principal está en el manejo de eventos.
    }

    void OnEnable()
    {
        PauseService.OnPauseChanged += HandlePauseChanged;

        if (turnSystem != null)
        {
            turnSystem.OnBattleEnded += HandleBattleEnded;
            turnSystem.OnTurnStarted += HandleTurnStarted;
        }
    }

    void OnDisable()
    {
        PauseService.OnPauseChanged -= HandlePauseChanged;

        if (turnSystem != null)
        {
            turnSystem.OnBattleEnded -= HandleBattleEnded;
            turnSystem.OnTurnStarted -= HandleTurnStarted;
        }
    }

    private void HandlePauseChanged(bool isPaused)
    {
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(isPaused);
        }
    }
    
    // MÉTODO CORREGIDO: SOLO actualiza la cámara si hay un actor activo.
    // Esto permite que el control de la cámara libre (por mouse) funcione durante la Fase de Selección (actor == null).
    private void HandleTurnStarted(Team team, CharacterActor actor)
    {
        if (virtualCamera == null) return;
        
        // Solo actualizamos la cámara para el equipo jugador
        if (team == Team.Player)
        {
            if (actor != null)
            {
                // Hay un actor activo: la cámara lo sigue
                UpdateCameraTarget(actor.transform);
                Debug.Log($"[vCorregido] Camera following: {actor.CharacterName}");
            }
            else
            {
                // Fase de Selección (actor es null): NO tocamos el target de la cámara.
                // Esto debería permitir que el componente CinemachineFreeLook recupere el control
                // del mouse y la cámara mantenga su última posición.
                Debug.Log("[vCorregido] Camera in Selection Phase. Maintaining current view.");
            }
        }
        else // Equipo Enemigo
        {
            if (actor != null)
            {
                // El equipo enemigo siempre debe seguir a su actor
                UpdateCameraTarget(actor.transform);
            }
        }
    }

    private void HandleBattleEnded(Team winner)
    {
        if (winner == Team.Player)
        {
            ShowVictory();
        }
        else
        {
            ShowDefeat();
        }
    }

    // Método asumido que establece Follow y LookAt de la CinemachineCamera
    private void UpdateCameraTarget(Transform target)
    {
        if (virtualCamera != null)
        {
            // Esto asume que virtualCamera es un CinemachineCamera (o FreeLook)
            // y que establecer Follow/LookAt al objetivo es lo que quieres para el modo de seguimiento.
            virtualCamera.Follow = target;
            virtualCamera.LookAt = target;
        }
    }

    // ... (El resto de métodos de UI permanecen sin cambios)

    public Camera GetMainCamera() => mainCamera;

    public void ReturnToMenu()
    {
        PauseService.SetPaused(false);
        SceneManager.LoadScene("Menu");
    }
    public void TogglePause()
    {
        PauseService.TogglePause();
    }

    public void ShowVictory()
    {
        if (victoryScreen != null) victoryScreen.SetActive(true);
    }
    public void GoToNextLevel()
    {
        PauseService.SetPaused(false);

        if (SceneManager.GetActiveScene().buildIndex + 1 >= SceneManager.sceneCountInBuildSettings)
        {
            ReturnToMenu();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
    }

    public void ShowDefeat()
    {
        if (defeatScreen != null) defeatScreen.SetActive(true);
    }

    public void RestartBattle()
    {
        PauseService.SetPaused(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}