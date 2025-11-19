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
    [SerializeField] private float cameraHeightOffset = 1.5f; // Altura adicional para el target de la cámara
    [SerializeField] private string cameraTargetChildName = "CameraTarget"; // Nombre del hijo a buscar como target

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
        // Update camera for both player and enemy teams
        if (virtualCamera != null && actor != null)
        {
            Transform cameraTarget = GetCameraTarget(actor.transform);
            UpdateCameraTarget(cameraTarget);
            Debug.Log($"[CAMERA] Following {team} character: {actor.CharacterName}");
        }
    }

    private Transform GetCameraTarget(Transform characterTransform)
    {
        if (characterTransform == null) return null;

        // Primero intentar buscar un hijo específico para la cámara
        Transform cameraTargetChild = characterTransform.Find(cameraTargetChildName);
        if (cameraTargetChild != null)
        {
            return cameraTargetChild;
        }

        // Si no existe, buscar por nombre común alternativo
        foreach (Transform child in characterTransform)
        {
            if (child.name.Contains("Camera") || child.name.Contains("Head") || child.name.Contains("LookAt"))
            {
                return child;
            }
        }

        // Si no hay hijo específico, crear o usar un Transform con offset
        // Buscar si ya existe un objeto temporal para este personaje
        GameObject offsetObject = GameObject.Find($"CameraTarget_{characterTransform.name}");
        if (offsetObject == null)
        {
            offsetObject = new GameObject($"CameraTarget_{characterTransform.name}");
            offsetObject.transform.SetParent(characterTransform);
            offsetObject.transform.localPosition = new Vector3(0, cameraHeightOffset, 0);
        }
        
        return offsetObject.transform;
    }

    private void UpdateCameraTarget(Transform target)
    {
        if (virtualCamera == null || target == null) return;
        
        virtualCamera.Follow = target;
        virtualCamera.LookAt = target;
    }
}