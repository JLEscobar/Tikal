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
        
        if (turnSystem == null) turnSystem = FindFirstObjectByType<TurnSystem>();
        if (mainCamera == null) mainCamera = Camera.main;
        if (virtualCamera == null) virtualCamera = FindFirstObjectByType<CinemachineCamera>();
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

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PauseService.TogglePause();
        }
    }
    
    public Camera GetMainCamera()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        return mainCamera;
    }

    private void HandlePauseChanged(bool isPaused)
    {
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(isPaused);
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
    
    // NUEVO MÉTODO: Llamado directamente por el botón UI
    public void EndTurnFromButton()
    {
        if (turnSystem == null)
        {
            Debug.LogError("TurnSystem no está asignado en GameManager. No se puede finalizar el turno.");
            return;
        }
        
        // Verificamos si es el turno del jugador y si hay un actor activo
        if (turnSystem.CurrentTeam == Team.Player && turnSystem.CurrentActor != null)
        {
            // Lógica de consumo de AP (asumiendo que gasta 1 AP para finalizar el turno, 
            // aunque el GDD permite finalizar el turno sin gastar AP si no hay más acciones)
            
            // Llamamos a EndTurn del TurnSystem. El TurnSystem se encargará de resetear el AP
            // del personaje actual y pasar al siguiente estado/equipo.
            turnSystem.EndTurn();
            Debug.Log("[EndTurnButton] Turno del actor actual finalizado por botón.");
        }
        else if (turnSystem.CurrentTeam == Team.Player && turnSystem.CurrentActor == null)
        {
            // Esto ocurre cuando se presiona EndTurn en la Fase de Selección, 
            // indicando que el jugador quiere finalizar la fase de todo el equipo.
            turnSystem.EndTurn(); 
            Debug.Log("[EndTurnButton] Fase de Selección finalizada por botón.");
        }
    }

    // Método asumido que establece Follow y LookAt de la CinemachineCamera
    private void UpdateCameraTarget(Transform target)
    {
        if (virtualCamera != null)
        {
            virtualCamera.Follow = target;
            virtualCamera.LookAt = target;
        }
    }

    private void HandleTurnStarted(Team team, CharacterActor actor)
    {
        if (virtualCamera == null) return;
        
        if (team == Team.Player)
        {
            if (actor != null)
            {
                UpdateCameraTarget(actor.transform);
            }
            else
            {
                // Fase de Selección (actor es null): No tocamos el target para que el mouse pueda moverse.
            }
        }
        else // Equipo Enemigo
        {
            if (actor != null)
            {
                UpdateCameraTarget(actor.transform);
            }
        }
    }


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