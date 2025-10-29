using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine; // Added Cinemachine namespace

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
            turnSystem.OnTurnStarted += HandleTurnStarted; // Subscribe to turn started event
        }
    }

    void OnDisable()
    {
        PauseService.OnPauseChanged -= HandlePauseChanged;

        if (turnSystem != null)
        {
            turnSystem.OnBattleEnded -= HandleBattleEnded;
            turnSystem.OnTurnStarted -= HandleTurnStarted; // Unsubscribe from turn started event
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
        Debug.Log($"[v0] GameManager: Battle ended, winner is {winner}");
        
        if (winner == Team.Player)
        {
            ShowVictory();
        }
        else
        {
            ShowDefeat();
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

    private void HandleTurnStarted(Team team, CharacterActor actor)
    {
        // Only update camera for player team
        if (team == Team.Player && virtualCamera != null && actor != null)
        {
            UpdateCameraTarget(actor.transform);
            Debug.Log($"[v0] Camera following: {actor.CharacterName}");
        }
    }

    private void UpdateCameraTarget(Transform target)
    {
        if (virtualCamera == null) return;
        
        virtualCamera.Follow = target;
        virtualCamera.LookAt = target;
    }
}
